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
    private readonly Dictionary<(int Tick, int ReferenceTil), int> tickTilMap = new();

    // int is the timeline
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
        if (bpmEvents.Count <= 0 || bpmEvents[0].Tick != 0) throw new DiagnosticException(Strings.Error_BPM_change_event_not_found_at_0);

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
        mgxc.Meta.BgmInitialTimeSignature = new TimeSignature(0, initBeat.Numerator, initBeat.Denominator);

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

        mgxc.Events.Sort();

        var timeSignatures = mgxc.Events.Children.OfType<mgxc.BeatEvent>().Select(e => new TimeSignature(e.Tick.Original, e.Numerator, e.Denominator));
        diagnostic.BarCalculator = new BarIndexCalculator(Time.MarResolution, timeSignatures);
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

        mgxc.Notes.Sort();


        foreach (var (tick, effects) in exEffects)
        {
            if (effects.Count <= 1) continue;
            var str = string.Join(", ", effects.Select(e => e.ToString()));
            var msg = string.Format(Strings.Diag_concurrent_ex_effects, str);
            diagnostic.Report(DiagnosticSeverity.Information, msg, tick.Original);
        }
    }
}