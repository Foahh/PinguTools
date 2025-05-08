/*
   This code is based on the original implementation from:
   https://github.com/inonote/MargreteOnline
*/

using PinguTools.Chart.Localization;
using PinguTools.Chart.Models;
using PinguTools.Common;
using mgxc = PinguTools.Chart.Models.mgxc;

namespace PinguTools.Chart;

public partial class MgxcParser(IReadOnlyCollection<Entry>? weTags)
{
    private readonly Dictionary<int, List<mgxc.Note>> noteGroups = new();
    private readonly Dictionary<(int Tick, int ReferenceTimeline), int> tickTimelineMap = new();

    // int = TimelineIds
    private readonly Dictionary<int, List<mgxc.SpeedEvent>> tilGroups = new();

    private int dcmTilId = -1;

    private IDiagnostic diagnostic = null!;
    private mgxc.Note? lastNote;
    private mgxc.Note? lastParentNote;

    public async Task<mgxc.Chart> ParseMeta(string path, IDiagnostic diag)
    {
        diagnostic = diag;
        var chart = new mgxc.Chart();
        chart.Meta.ReleaseDate = new FileInfo(path).LastWriteTime;

        using var reader = new StreamReader(path);
        await reader.EnumerateAsync((block, args) =>
        {
            if (block == Block.META)
            {
                ParseMeta(chart, args);
            }
            else if (block == Block.HEADER)
            {
                ParseHeader(chart, args);
            }
        });

        PostProcessEvent(chart);
        return chart;
    }

    public async Task<mgxc.Chart> Parse(string path, IDiagnostic diag)
    {
        diagnostic = diag;
        var chart = new mgxc.Chart();
        chart.Meta.ReleaseDate = new FileInfo(path).LastWriteTime;

        using var reader = new StreamReader(path);
        await reader.EnumerateAsync((block, args) =>
        {
            if (block == Block.META)
            {
                ParseMeta(chart, args);
            }
            else if (block == Block.HEADER)
            {
                ParseHeader(chart, args);
            }
            else if (block == Block.NOTES)
            {
                ParseNotes(chart, args);
            }
        });

        lastParentNote = null;
        lastNote = null;

        PostProcessEvent(chart);
        PostProcessNote(chart);
        PostProcessTil(chart);
        return chart;
    }

    protected void PostProcessEvent(mgxc.Chart mgxc)
    {
        var bpmEvents = mgxc.Events.Children.OfType<mgxc.BpmEvent>().OrderBy(e => e.Tick).ToList();
        if (bpmEvents.Count <= 0 || bpmEvents[0].Tick != 0) diagnostic.Throw(Strings.Diag_BPM_change_event_not_found_at_0);

        var beatEvents = mgxc.Events.Children.OfType<mgxc.BeatEvent>().OrderBy(e => e.Bar).ToList();
        if (beatEvents.Count <= 0 || beatEvents[0].Bar != 0)
        {
            mgxc.Events.InsertBefore(new mgxc.BeatEvent
            {
                Bar = 0,
                Numerator = 4,
                Denominator = 4
            }, bpmEvents.FirstOrDefault());
            beatEvents = mgxc.Events.Children.OfType<mgxc.BeatEvent>().OrderBy(e => e.Bar).ToList();
            diagnostic.Report(DiagnosticSeverity.Information, Strings.Diag_time_Signature_event_not_found_at_0);
        }

        var initBeat = beatEvents[0];
        mgxc.Meta.BgmInitialBpm = bpmEvents[0].Bpm;
        mgxc.Meta.BgmInitialTimeSignature = new ChartMeta.Beat(initBeat.Numerator, initBeat.Denominator);

        // calculate tick for each beat event
        if (beatEvents.Count > 1)
        {
            var ticks = 0;
            for (var i = 0; i < beatEvents.Count - 1; i++)
            {
                var curr = beatEvents[i];
                var next = beatEvents[i + 1];
                ticks += Time.MarResolution * curr.Numerator / curr.Denominator * (next.Bar - curr.Bar);
                next.Tick = ticks;
            }
        }

        // offset all nodes by the initial beat event
        if (mgxc.Meta.BgmEnableBarOffset)
        {
            var offset = (int)Math.Round((decimal)Time.MarResolution / initBeat.Denominator * initBeat.Numerator);
            var initialEvents = mgxc.Events.Children.Where(p => p.Tick == 0).Select(p => p.Clone<mgxc.Event>()).ToList();
            foreach (var e in mgxc.Events.Children)
            {
                e.Offset(offset);
                if (e is mgxc.BeatEvent beatEvent) beatEvent.Bar += 1;
            }
            foreach (var note in mgxc.Notes.Children)
            {
                note.Offset(offset);
            }
            foreach (var e in initialEvents)
            {
                mgxc.Events.AppendChild(e);
            }
        }

        mgxc.Events.Sort();
    }

    protected void PostProcessNote(mgxc.Chart mgxc)
    {
        var noteGroup = mgxc.Notes.Children.OfType<mgxc.ExTapableNote>().GroupBy(note => (note.Tick, note.Lane, note.Width)).ToDictionary(g => g.Key, g => g.ToList());
        var exEffects = new Dictionary<Time, HashSet<ExEffect>>();
        var remove = new List<mgxc.ExTap>();

        foreach (var exTap in mgxc.Notes.Children.OfType<mgxc.ExTap>())
        {
            if (!exEffects.TryGetValue(exTap.Tick, out var effectSet))
            {
                effectSet = [];
                exEffects[exTap.Tick] = effectSet;
            }
            effectSet.Add(exTap.Effect);

            var key = (exTap.Tick, exTap.Lane, exTap.Width);
            if (!noteGroup.TryGetValue(key, out var matchingNotes)) continue;
            foreach (var note in matchingNotes) note.Effect = exTap.Effect;
            if (exTap.Children.Count <= 0 && exTap.PairNote == null) remove.Add(exTap);
        }
        foreach (var exTap in remove) mgxc.Notes.RemoveChild(exTap);

        foreach (var (tick, effects) in exEffects)
        {
            if (effects.Count <= 1) continue;
            var str = string.Join(", ", effects.Select(e => e.ToString()));
            diagnostic.Report(DiagnosticSeverity.Information, string.Format(Strings.Diag_concurrent_ex_effects, tick.Original, str), tick);
        }
        
        mgxc.Notes.Sort();
    }


    protected static HashSet<mgxc.Note> FindViolations(mgxc.Note notes)
    {
        var violations = new HashSet<mgxc.Note>();
        foreach (var note in notes.Children)
        {
            foreach (var v in FindViolations(note)) violations.Add(v);
            foreach (var other in notes.Children.Where(p => p.IsViolate(note))) violations.Add(other);
        }
        return violations;
    }

    // thanks to @tångent 90°
    protected void PostProcessTil(mgxc.Chart mgxc)
    {
        var mainTil = mgxc.Meta.MainTil + 1;
        var dcmEvents = mgxc.Events.Children.OfType<mgxc.NoteSpeedEvent>().ToList();
        GroupEventByTimeline(mgxc.Events);
        GroupNoteByTimeline(mgxc.Notes, dcmEvents);

        AbsolutizeNoteSpeed();

        if (!tilGroups.ContainsKey(mainTil))
        {
            tilGroups[mainTil] =
            [
                new mgxc.TimelineEvent
                {
                    Tick = 0,
                    Timeline = mainTil,
                    Speed = 1m
                }
            ];
            diagnostic.Report(DiagnosticSeverity.Information, string.Format(Strings.Diag_main_timeline_not_found, mgxc.Meta.MainTil));
        }

        ChangeGroupId(mainTil, 0);
        MoveNegativeGroups();
        ClearEmptyGroups();

        // TODO: Find conflicting note, compare priority and put them in separate group (SLA with larger TIL => larger priority when applying on note)

        foreach (var tils in tilGroups.Values.ToList()) tils.Sort((a, b) => a.Tick.CompareTo(b.Tick));

        var slaSet = new HashSet<(int Tick, int Timeline, int Lane, int Width)>();

        foreach (var (id, notes) in noteGroups)
        {
            if (id == 0) continue;
            var events = tilGroups[id];
            foreach (var note in notes)
            {
                note.Timeline = id;

                // magic optimization
                if (note is mgxc.AirCrashJoint { Parent: mgxc.AirCrash { Color: Color.NON }, Joint: Joint.C }) continue;

                // find the speed that is just before the note
                var prevTil = events.Where(p => p.Tick <= note.Tick).OrderByDescending(p => p.Tick).FirstOrDefault();
                if (prevTil?.Speed is null) continue;
                if (slaSet.Contains((note.Tick, id, note.Lane, note.Width))) continue;

                var head = new mgxc.SoflanArea
                {
                    Tick = note.Tick,
                    Timeline = id,
                    Lane = note.Lane,
                    Width = note.Width
                };
                var tail = new mgxc.SoflanAreaJoint { Tick = note.Tick + Time.SingleTick };

                slaSet.Add((note.Tick, id, note.Lane, note.Width));
                head.AppendChild(tail);
                mgxc.Notes.AppendChild(head);
            }
        }

        foreach (var e in mgxc.Events.Children.OfType<mgxc.SpeedEvent>().ToList()) mgxc.Events.RemoveChild(e);

        foreach (var (tilId, events) in tilGroups)
        {
            foreach (var e in events)
            {
                var newEvent = new mgxc.TimelineEvent
                {
                    Tick = e.Tick,
                    Timeline = tilId,
                    Speed = e.Speed
                };
                mgxc.Events.AppendChild(newEvent);
            }
        }

        var violations = FindViolations(mgxc.Notes);
        foreach (var note in violations)
        {
            diagnostic.Report(DiagnosticSeverity.Warning, Strings.Diag_note_overlapped_in_different_TIL, note);
        }

        tilGroups.Clear();
        noteGroups.Clear();
        tickTimelineMap.Clear();
    }

    // group events by timeline
    protected void GroupEventByTimeline(mgxc.Event events)
    {
        foreach (var til in events.Children.OfType<mgxc.TimelineEvent>())
        {
            if (til.Timeline < 0) diagnostic.Throw(Strings.Diag_timeline_event_must_have_non_negative_timeline, til);
            var timelineId = til.Timeline + 1; // +1 for simplicity
            TryCreateGroup(timelineId);
            tilGroups[timelineId].Add(til);
        }
    }

    // group notes by timeline
    protected void GroupNoteByTimeline(mgxc.Note parent, List<mgxc.NoteSpeedEvent> dcmEvents)
    {
        if (parent.Children.Count == 0) return;
        foreach (var note in parent.Children)
        {
            GroupNoteByTimeline(note, dcmEvents);
            var timeline = note.Timeline + 1; // +1 for simplicity
            if (timeline < 0) diagnostic.Throw(Strings.Diag_timeline_event_must_have_non_negative_timeline);

            // finding note speed event that affect the note
            var baseDcm = dcmEvents.Where(p => p.Tick <= note.Tick).OrderByDescending(p => p.Tick).FirstOrDefault();

            if (baseDcm != null && baseDcm.Speed != 1m)
            {
                var keyPair = (baseDcm.Tick, timeline);
                if (tickTimelineMap.TryGetValue(keyPair, out var existingId))
                {
                    noteGroups[existingId].Add(note);
                }
                else
                {
                    var newId = dcmTilId--;
                    TryCreateGroup(newId);

                    tickTimelineMap[keyPair] = newId;
                    tilGroups[newId].Add(baseDcm);
                    noteGroups[newId].Add(note);
                }
            }
            else
            {
                TryCreateGroup(timeline);
                noteGroups[timeline].Add(note);
            }
        }
    }

    // Convert the note speed event to the timeline event for each dcm group
    protected void AbsolutizeNoteSpeed()
    {
        foreach (var ((_, refTil), dcmTil) in tickTimelineMap.ToList())
        {
            var notes = noteGroups[dcmTil];
            var lastTick = notes.Select(p => p.Tick).Append(0).Max() + Time.SingleTick;

            var dcmTimeline = tilGroups[dcmTil];
            var dcmEvent = dcmTimeline[0];
            dcmTimeline.Clear();

            var refTils = tilGroups[refTil].Where(p => p.Tick <= lastTick).ToList();
            foreach (var tils in refTils)
            {
                var newSpeed = new mgxc.TimelineEvent
                {
                    Tick = tils.Tick,
                    Timeline = dcmTil,
                    Speed = dcmEvent.Speed * tils.Speed
                };
                dcmTimeline.Add(newSpeed);
            }

            if (!dcmTimeline.Exists(p => p.Tick == dcmEvent.Tick))
            {
                var newSpeed = new mgxc.TimelineEvent
                {
                    Tick = dcmEvent.Tick,
                    Timeline = dcmTil,
                    Speed = dcmEvent.Speed
                };
                dcmTimeline.Add(newSpeed);
            }

            if (!dcmTimeline.Exists(p => p.Tick == 0))
            {
                var newSpeed = new mgxc.TimelineEvent
                {
                    Tick = 0,
                    Timeline = dcmTil,
                    Speed = dcmEvent.Speed
                };
                dcmTimeline.Add(newSpeed);
            }
        }
    }

    // remove empty groups
    protected void ClearEmptyGroups()
    {
        foreach (var (id, events) in tilGroups.ToList())
        {
            var mappedNotes = noteGroups[id];
            var maxTick = mappedNotes.Select(p => p.Tick).Append(0).Max();
            if (mappedNotes.Count == 0) tilGroups.Remove(id);
            else if (events.Count > 0 && maxTick > 0) events.RemoveAll(p => p.Tick > maxTick + Time.SingleTick);
        }

        foreach (var (id, notes) in noteGroups.ToList())
        {
            if (notes.Count == 0) noteGroups.Remove(id);
        }
    }

    // dcm group = negative timeline
    protected void MoveNegativeGroups()
    {
        var maxId = tilGroups.Keys.Max();
        foreach (var id in tilGroups.Keys.ToList())
        {
            if (id >= 0) continue;
            var newId = -id + maxId;
            ChangeGroupId(id, newId);
        }
    }

    protected bool TryCreateGroup(int id)
    {
        if (tilGroups.ContainsKey(id)) return false;
        if (noteGroups.ContainsKey(id)) return false;

        tilGroups[id] = [];
        noteGroups[id] = [];
        return true;
    }

    protected void ChangeGroupId(int oldId, int newId)
    {
        var events = tilGroups[oldId];
        tilGroups.Remove(oldId);
        tilGroups.Add(newId, events);

        var notes = noteGroups[oldId];
        noteGroups.Remove(oldId);
        noteGroups.Add(newId, notes);
    }
}