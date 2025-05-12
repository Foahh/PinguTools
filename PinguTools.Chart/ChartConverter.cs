using PinguTools.Chart.Localization;
using PinguTools.Chart.Models;
using PinguTools.Common;
using PinguTools.Common.Localization;
using System.Text;
using c2s = PinguTools.Chart.Models.c2s;

// ReSharper disable RedundantNameQualifier

namespace PinguTools.Chart;

public partial class ChartConverter(MgxcParser parser) : IConverter<ChartConverter.Context>
{
    private IDiagnostic diagnostic = new DiagnosticReporter();
    private List<c2s.Note> Notes { get; set; } = [];
    private List<c2s.Event> Events { get; set; } = [];

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!CanConvert(context, diag)) return;
        progress?.Report(CommonStrings.Status_converting_chart);
        diagnostic = diag;

        progress?.Report(Strings.Status_parsing);
        var mgxc = await parser.Parse(context.ChartPath, diagnostic);
        mgxc.Meta = context.Meta ?? mgxc.Meta;

        ct.ThrowIfCancellationRequested();
        progress?.Report(CommonStrings.Status_converting);

        Reset();
        foreach (var note in mgxc.Notes.Children) ConvertNote(note);
        ConvertEvent(mgxc);

        progress?.Report(CommonStrings.Status_Validate);
        ValidateChart();

        if (mgxc.Meta.BgmEnableBarOffset)
        {
            var sig = mgxc.Meta.BgmInitialTimeSignature;
            var offset = (int)Math.Round((decimal)Time.MarResolution / sig.Denominator * sig.Numerator);
            foreach (var e in Events.Where(e => e.Tick != 0)) e.Tick = e.Tick.Original + offset;
            foreach (var n in Notes)
            {
                n.Tick = n.Tick.Original + offset;
                if (n is c2s.LongNote longNote) longNote.EndTick = longNote.EndTick.Original + offset;
            }
        }

        progress?.Report(CommonStrings.Status_writing);

        var sb = new StringBuilder();
        sb.AppendLine("VERSION\t1.13.00\t1.13.00");
        sb.AppendLine("MUSIC\t0");
        sb.AppendLine("SEQUENCEID\t0");
        sb.AppendLine("DIFFICULT\t0");
        sb.AppendLine("LEVEL\t0.0");
        sb.AppendLine($"CREATOR\t{mgxc.Meta.Designer}");
        sb.AppendLine($"BPM_DEF\t{mgxc.Meta.MainBpm:F3}\t{mgxc.Meta.MainBpm:F3}\t{mgxc.Meta.MainBpm:F3}\t{mgxc.Meta.MainBpm:F3}");
        sb.AppendLine($"MET_DEF\t{mgxc.Meta.BgmInitialTimeSignature.Denominator}\t{mgxc.Meta.BgmInitialTimeSignature.Numerator}");
        sb.AppendLine("RESOLUTION\t384");
        sb.AppendLine("CLK_DEF\t384");
        sb.AppendLine("PROGJUDGE_BPM\t240.000");
        sb.AppendLine("PROGJUDGE_AER\t  0.999");
        sb.AppendLine("TUTORIAL\t0");
        sb.AppendLine();

        foreach (var e in Events)
        {
            try
            {
                sb.AppendLine(e.Text);
            }
            catch (Exception ex)
            {
                diagnostic.Report(DiagnosticSeverity.Error, ex.Message, e.Tick.Original, e);
            }
        }
        sb.AppendLine();
        foreach (var n in Notes)
        {
            try
            {
                sb.AppendLine(n.Text);
            }
            catch (Exception ex)
            {
                diagnostic.Report(DiagnosticSeverity.Error, ex.Message, n.Tick.Original, n);
            }
        }

        if (diag.HasErrors) return;
        await File.WriteAllTextAsync(context.OutputPath, sb.ToString(), ct);
    }

    public bool CanConvert(Context context, IDiagnostic diag)
    {
        if (!File.Exists(context.ChartPath)) diag.Report(DiagnosticSeverity.Error, string.Format(CommonStrings.Error_file_not_found, context.ChartPath));
        return !diag.HasErrors;
    }

    private void Reset()
    {
        pMap.Clear();
        nMap.Clear();
        Notes = [];
        Events = [];
    }

    public async Task<ChartMeta> ParseMeta(string path, IDiagnostic diag)
    {
        var result = await parser.ParseMeta(path, diag);
        return result.Meta;
    }

    protected void ValidateChart()
    {
        var allSlides = Notes.OfType<c2s.Slide>().ToList();
        var allAirs = Notes.OfType<c2s.IPairable>().Where(p => p.Parent is c2s.Slide).Cast<c2s.Note>().ToList();

        var airsLookup = allAirs.GroupBy(a => (a.Tick, a.Lane, a.Width)).ToDictionary(g => g.Key, g => g.Count());
        var slidesLookup = allSlides.GroupBy(s => (s.EndTick, s.EndLane, s.EndWidth)).ToDictionary(g => g.Key, g => g.Count());

        foreach (var (pos, airsCount) in airsLookup)
        {
            var slidesCount = slidesLookup.GetValueOrDefault(pos, 0);
            if (airsCount >= slidesCount) continue;
            diagnostic.Report(DiagnosticSeverity.Information, Strings.Overlapping_Air_Parent_Slide, pos.Tick.Original);
        }

        foreach (var longNote in Notes.OfType<c2s.LongNote>())
        {
            var length = longNote.Length.Original;
            if (length >= Time.SingleTick) continue;

            var tick = longNote.Tick.Original;
            var msg = string.Format(Strings.Diag_set_length_smaller_than_unit, length, Time.SingleTick);
            diagnostic.Report(DiagnosticSeverity.Warning, msg, tick, longNote);
        }
    }

    public record Context(string ChartPath, string OutputPath, ChartMeta? Meta);
}