using PinguTools.Chart.Localization;
using PinguTools.Chart.Models;
using PinguTools.Common;
using mgxc = PinguTools.Chart.Models.mgxc;

namespace PinguTools.Chart;

public partial class MgxcParser
{
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
                var prevTil = events.Where(p => p.Tick.Original <= note.Tick.Original).OrderByDescending(p => p.Tick).FirstOrDefault();
                if (prevTil?.Speed is null) continue;
                if (slaSet.Contains((note.Tick.Original, id, note.Lane, note.Width))) continue;

                var head = new mgxc.SoflanArea
                {
                    Tick = note.Tick,
                    Timeline = id,
                    Lane = note.Lane,
                    Width = note.Width
                };
                var tail = new mgxc.SoflanAreaJoint { Tick = note.Tick.Original + Time.SingleTick };

                slaSet.Add((note.Tick.Original, id, note.Lane, note.Width));
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
            diagnostic.Report(DiagnosticSeverity.Warning, Strings.Diag_note_overlapped_in_different_TIL, note.Tick.Original, note);
        }

        tilGroups.Clear();
        noteGroups.Clear();
        tickTilMap.Clear();
    }

    protected void GroupEventByTimeline(mgxc.Event events)
    {
        foreach (var til in events.Children.OfType<mgxc.TimelineEvent>())
        {
            if (til.Timeline < 0) throw new DiagnosticException(Strings.Error_no_negative_timeline_event, til.Tick.Original, til);
            var timelineId = til.Timeline + 1; // +1 for simplicity
            TryCreateGroup(timelineId);
            tilGroups[timelineId].Add(til);
        }
    }

    protected void GroupNoteByTimeline(mgxc.Note parent, List<mgxc.NoteSpeedEvent> dcmEvents)
    {
        if (parent.Children.Count == 0) return;
        foreach (var note in parent.Children)
        {
            GroupNoteByTimeline(note, dcmEvents);
            var timeline = note.Timeline + 1; // +1 for simplicity
            if (timeline < 0) throw new DiagnosticException(Strings.Error_note_must_have_non_negative_timeline, note.Tick.Original, note);

            // finding note speed event that affect the note
            var baseDcm = dcmEvents.Where(p => p.Tick.Original <= note.Tick.Original).OrderByDescending(p => p.Tick).FirstOrDefault();

            if (baseDcm != null && baseDcm.Speed != 1m)
            {
                var keyPair = (baseDcm.Tick.Original, timeline);
                if (tickTilMap.TryGetValue(keyPair, out var existingId))
                {
                    noteGroups[existingId].Add(note);
                }
                else
                {
                    var newId = dcmTilId--;
                    TryCreateGroup(newId);

                    tickTilMap[keyPair] = newId;
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
        foreach (var ((_, refTil), dcmTil) in tickTilMap.ToList())
        {
            var notes = noteGroups[dcmTil];
            var lastTick = notes.Select(p => p.Tick.Original).Append(0).Max() + Time.SingleTick;

            var dcmTimeline = tilGroups[dcmTil];
            var dcmEvent = dcmTimeline[0];
            dcmTimeline.Clear();

            var refTils = tilGroups[refTil].Where(p => p.Tick.Original <= lastTick).ToList();
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

    protected void ClearEmptyGroups()
    {
        foreach (var (id, events) in tilGroups.ToList())
        {
            var mappedNotes = noteGroups[id];
            var maxTick = mappedNotes.Select(p => p.Tick).Append(0).Max();
            if (mappedNotes.Count == 0) tilGroups.Remove(id);
            else if (events.Count > 0 && maxTick.Original > 0) events.RemoveAll(p => p.Tick.Original > maxTick.Original + Time.SingleTick);
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
}