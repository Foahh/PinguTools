using PinguTools.Common.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PinguTools.Common.Graphic;

public class JacketConverter : IConverter<JacketConverter.Context>
{
    public int JacketWidth { get; set; } = 300;
    public int JacketHeight { get; set; } = 300;

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(context, diag)) return;
        progress?.Report(CommonStrings.Status_converting_jacket);
        ct.ThrowIfCancellationRequested();

        using var image = await Image.LoadAsync<Rgba32>(context.InputPath, ct);
        await ImageHelper.ConvertDdsAsync(image, context.OutputPath, JacketWidth, JacketHeight, ct: ct);
        ct.ThrowIfCancellationRequested();
    }

    public Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        if (!File.Exists(context.InputPath)) diag.Report(Severity.Error, CommonStrings.Error_file_not_found, context.InputPath);
        return Task.FromResult(!diag.HasErrors);
    }

    public record Context(string InputPath, string OutputPath);
}