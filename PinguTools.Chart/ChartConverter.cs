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

        if (mgxc.Meta.BgmEnableBarOffset)
        {
            var sig = mgxc.Meta.BgmInitialTimeSignature;
            var offset = (int)Math.Round((decimal)Time.MarResolution / sig.Denominator * sig.Numerator);
            foreach (var e in Events.Where(e => e.Tick != 0)) e.Tick = e.Tick.Original + offset;
            foreach (var n in Notes) n.Tick = n.Tick.Original + offset;
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

    public record Context(string ChartPath, string OutputPath, ChartMeta? Meta);
}