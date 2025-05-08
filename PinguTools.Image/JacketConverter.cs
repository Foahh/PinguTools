using PinguTools.Common;
using PinguTools.Common.Localization;

namespace PinguTools.Image;

public class JacketConverter : IConverter<JacketConverter.Context>
{
    public int JacketWidth { get; set; } = 300;
    public int JacketHeight { get; set; } = 300;

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!CanConvert(context, diag)) return;
        progress?.Report(CommonStrings.Status_converting_jacket);

        var data = await File.ReadAllBytesAsync(context.InputPath, ct);
        ct.ThrowIfCancellationRequested();

        var jkFile = await Helper.ConvertDdsAsync(data, JacketWidth, JacketHeight);

        ct.ThrowIfCancellationRequested();
        await File.WriteAllBytesAsync(context.OutputPath, jkFile, ct);
    }

    public bool CanConvert(Context context, IDiagnostic diag)
    {
        if (!File.Exists(context.InputPath)) diag.Report(DiagnosticSeverity.Error, string.Format(CommonStrings.Error_file_not_found, context.InputPath));
        return !diag.HasErrors;
    }

    public record Context(string InputPath, string OutputPath);
}