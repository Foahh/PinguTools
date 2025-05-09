using PinguTools.Chart.Localization;
using PinguTools.Chart.Models;
using PinguTools.Common;
using mgxc = PinguTools.Chart.Models.mgxc;
using c2s = PinguTools.Chart.Models.c2s;

// ReSharper disable RedundantNameQualifier

namespace PinguTools.Chart;

public partial class ChartConverter
{
    private readonly Dictionary<mgxc.NegativeNote, c2s.Note> nMap = new();
    private readonly Dictionary<mgxc.PositiveNote, c2s.Note> pMap = new();

    private void TryPairingNegative(mgxc.PositiveNote source)
    {
        if (source.PairNote != null && nMap.TryGetValue(source.PairNote, out var pair) && pair is c2s.IPairable children)
        {
            children.Parent = pair;
        }
    }

    private void TryPairingPositive(mgxc.NegativeNote source)
    {
        if (source.PairNote != null && pMap.TryGetValue(source.PairNote, out var pair) && pair is c2s.IPairable children)
        {
            children.Parent = pair;
        }
    }

    private T CreateNote<TSource, T>(Dictionary<TSource, c2s.Note> noteMap, TSource source, Action<T>? action = null) where TSource : mgxc.Note where T : c2s.Note, new()
    {
        var note = new T
        {
            Timeline = source.Timeline,
            Tick = source.Tick,
            Lane = source.Lane,
            Width = source.Width
        };

        action?.Invoke(note);
        Notes.Add(note);
        noteMap[source] = note;

        if (source is mgxc.PositiveNote pNote) TryPairingNegative(pNote);
        else if (source is mgxc.NegativeNote nNote) TryPairingPositive(nNote);

        return note;
    }

    private T CreateNote<T>(mgxc.Note source, Action<T>? extra = null) where T : c2s.Note, new()
    {
        var note = new T
        {
            Timeline = source.Timeline,
            Tick = source.Tick,
            Lane = source.Lane,
            Width = source.Width
        };
        extra?.Invoke(note);
        Notes.Add(note);
        return note;
    }

    private void ConvertNote(mgxc.Note e)
    {
        switch (e)
        {
            case mgxc.SoflanArea sla:
                ProcessSoflanArea(sla);
                break;
            case mgxc.Tap tap:
                CreateNote<mgxc.PositiveNote, c2s.Tap>(pMap, tap);
                break;
            case mgxc.ExTap exTap:
                CreateNote<mgxc.PositiveNote, c2s.ExTap>(pMap, exTap, x => x.Effect = exTap.Effect);
                break;
            case mgxc.Flick flick:
                CreateNote<mgxc.PositiveNote, c2s.Flick>(pMap, flick);
                break;
            case mgxc.Damage damage:
                CreateNote<mgxc.PositiveNote, c2s.Damage>(pMap, damage);
                break;
            case mgxc.Hold hold:
                ProcessHold(hold);
                break;
            case mgxc.Slide slide:
                ProcessSlide(slide);
                break;
            case mgxc.Air airNote:
                ProcessAir(airNote);
                break;
            case mgxc.AirSlide airSlide:
                ProcessAirSlide(airSlide);
                break;
            case mgxc.AirCrash airCrash:
                if (airCrash.Joint == Joint.C) ProcessAirTrace(airCrash);
                else ProcessAirCrash(airCrash);
                break;
        }
    }

    private void FinalizeAirCrashJoint(mgxc.AirCrash parent, mgxc.AirCrashJoint start, mgxc.AirCrashJoint end, int jointCount)
    {
        var length = end.Tick - start.Tick;
        if (end.Joint == Joint.D) jointCount -= 1;

        Time density = jointCount == 0 ? 0 : length / jointCount;
        if (end.Joint == Joint.C && jointCount > 0)
        {
            if (length <= Time.SingleTick) density += Time.SingleTick;
            else length -= Time.SingleTick;
        }

        if (density == Time.SingleTick && end.Joint == Joint.C)
        {
            diagnostic.Report(DiagnosticSeverity.Warning, string.Format(Strings.Diag_AirCrush_min_density_but_end_Control, density), new[] { start, end });
        }

        CreateNote<c2s.AirCrash>(start, x =>
        {
            x.SetLengthSafe(length, diagnostic);
            x.Density = density;
            x.Color = parent.Color;
            x.Height = start.Height;
            x.EndLane = end.Lane;
            x.EndWidth = end.Width;
            x.EndHeight = end.Height;
        });
    }

    private void ProcessAirCrash(mgxc.AirCrash airCrash)
    {
        var joints = airCrash.Children.OfType<mgxc.AirCrashJoint>().Prepend(airCrash.AsChild()).ToList();
        var jointCount = 0;
        var curr = joints[0];
        foreach (var joint in joints)
        {
            if (joint.Joint == Joint.C)
            {
                FinalizeAirCrashJoint(airCrash, curr, joint, jointCount);
                curr = joint;
                jointCount = 0;
            }
            else jointCount++;
        }
        if (curr.Joint != Joint.D) return;
        FinalizeAirCrashJoint(airCrash, curr, joints[^1], jointCount);
    }

    private void ProcessAirTrace(mgxc.AirCrash airCrash)
    {
        var joints = airCrash.Children.OfType<mgxc.AirCrashJoint>().Prepend(airCrash.AsChild()).ToList();
        for (var i = 0; i < joints.Count - 1; i++)
        {
            var curr = joints[i];
            var next = joints[i + 1];
            CreateNote<c2s.AirCrash>(curr, x =>
            {
                x.SetLengthSafe(next.Tick - curr.Tick, diagnostic);
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.Height = curr.Height;
                x.EndHeight = next.Height;
                x.Density = 0;
                x.Color = airCrash.Color;
            });
        }
    }

    private void ProcessAirSlide(mgxc.AirSlide airSlide)
    {
        if (airSlide.PairNote?.PairNote != airSlide) throw new DiagnosticException(Strings.Error_invalid_AirSlide_parent, airSlide);

        var parent = pMap.GetValueOrDefault(airSlide.PairNote);
        var joints = airSlide.Children.OfType<mgxc.AirSlideJoint>().Prepend(airSlide.AsChild()).ToList();
        for (var i = 0; i < joints.Count - 1; i++)
        {
            var prev = parent;
            var curr = joints[i];
            var next = joints[i + 1];
            parent = CreateNote<c2s.AirSlide>(curr, x =>
            {
                x.Parent = prev;
                x.Color = airSlide.Color;
                x.Height = curr.Height;
                x.SetLengthSafe(next.Tick - curr.Tick, diagnostic);
                x.Joint = next.Joint;
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.EndHeight = next.Height;
            });

            // pair the first joint with ground note
            if (i == 0)
            {
                nMap[airSlide] = parent;
                TryPairingPositive(airSlide);
            }
        }
    }

    private void ProcessAir(mgxc.Air airNote)
    {
        if (airNote.PairNote?.PairNote != airNote) throw new DiagnosticException(Strings.Error_invalid_Air_parent, airNote);

        var note = CreateNote<mgxc.NegativeNote, c2s.Air>(nMap, airNote, x =>
        {
            x.Parent = pMap.GetValueOrDefault(airNote.PairNote);
            x.Direction = airNote.Direction;
            x.Color = airNote.Color;
        });
        nMap[airNote] = note;
    }

    private void ProcessSlide(mgxc.Slide slide)
    {
        var joints = slide.Children.OfType<mgxc.SlideJoint>().Prepend(slide.AsChild()).ToList();
        for (var i = 0; i < joints.Count - 1; i++)
        {
            var curr = joints[i];
            var next = joints[i + 1];
            var index = i;
            var note = CreateNote<c2s.Slide>(curr, x =>
            {
                x.SetLengthSafe(next.Tick - curr.Tick, diagnostic);
                x.Joint = next.Joint;
                x.EndLane = next.Lane;
                x.EndWidth = next.Width;
                x.Effect = index == 0 ? slide.Effect : null;
            });
            // pair the last joint with air
            if (i == joints.Count - 2)
            {
                pMap[next] = note;
                TryPairingNegative(next);
            }
        }
    }

    private void ProcessSoflanArea(mgxc.SoflanArea sla)
    {
        if (sla.LastChild is not mgxc.SoflanAreaJoint tail) throw new DiagnosticException(Strings.Error_soflanArea_has_no_tail, sla);

        CreateNote<c2s.Sla>(sla, x =>
        {
            x.Length = tail.Tick - sla.Tick;
        });
    }

    private void ProcessHold(mgxc.Hold hold)
    {
        if (hold.LastChild is not mgxc.HoldJoint tail) throw new DiagnosticException(Strings.Error_hold_has_no_tail, hold);

        var note = CreateNote<c2s.Hold>(hold, x =>
        {
            x.SetLengthSafe(tail.Tick - hold.Tick, diagnostic);
            x.Effect = hold.Effect;
        });
        pMap[tail] = note;
        TryPairingNegative(tail);
    }


    private void ConvertEvent(mgxc.Chart mgxc)
    {
        var events = mgxc.Events.Children;
        foreach (var e in events.OfType<mgxc.BpmEvent>().OrderBy(e => e.Tick))
        {
            Events.Add(new c2s.Bpm
            {
                Tick = e.Tick,
                Value = e.Bpm
            });
        }

        foreach (var e in events.OfType<mgxc.BeatEvent>().OrderBy(e => e.Tick))
        {
            Events.Add(new c2s.Met
            {
                Tick = e.Tick,
                Numerator = e.Numerator,
                Denominator = e.Denominator
            });
        }

        var convertSlp = new List<c2s.Slp>();

        var tilGroups = new Dictionary<int, List<mgxc.TimelineEvent>>();
        foreach (var til in events.OfType<mgxc.TimelineEvent>())
        {
            if (!tilGroups.ContainsKey(til.Timeline)) tilGroups[til.Timeline] = [];
            tilGroups[til.Timeline].Add(til);
        }

        foreach (var (id, tilEvents) in tilGroups)
        {
            var lastTilTick = mgxc.GetLastTick(p => p.Timeline == id);

            if (tilEvents.Count <= 0) continue;
            for (var i = 0; i < tilEvents.Count - 1; i++)
            {
                var curr = tilEvents[i];
                var next = tilEvents[i + 1];
                convertSlp.Add(new c2s.Slp
                {
                    Timeline = id,
                    Tick = curr.Tick,
                    Length = next.Tick - curr.Tick,
                    Speed = curr.Speed
                });
            }
            var lastEvent = tilEvents[^1];
            convertSlp.Add(new c2s.Slp
            {
                Timeline = id,
                Tick = lastEvent.Tick,
                Length = Math.Max(lastTilTick - lastEvent.Tick, Time.SingleTick),
                Speed = lastEvent.Speed
            });
        }

        convertSlp.RemoveAll(e => e.Speed == 1m);
        Events.AddRange(convertSlp);
    }
}