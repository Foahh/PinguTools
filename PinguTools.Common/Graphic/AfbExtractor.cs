using PinguTools.Common.Graphic.Locator;
using PinguTools.Common.Resources;

namespace PinguTools.Common.Graphic;

public class ChunkExtractor(IChunkLocator locator, string extension) : IConverter<ChunkExtractor.Options>
{
    public async Task ConvertAsync(Options options, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(options, diag)) return;

        var data = await File.ReadAllBytesAsync(options.InputPath, ct);
        ct.ThrowIfCancellationRequested();

        progress?.Report(CommonStrings.Status_extracting);

        var chunks = locator.Locate(data);
        if (chunks.Length == 0) throw new DiagnosticException(string.Format(CommonStrings.Error_no_chunks_found, extension));

        var baseName = Path.GetFileNameWithoutExtension(options.InputPath);
        await locator.ExtractAsync(data, options.OutputFolder, baseName, extension, chunks);

        ct.ThrowIfCancellationRequested();
        progress?.Report(CommonStrings.Status_writing);
    }

    public Task<bool> CanConvertAsync(Options options, IDiagnostic diag)
    {
        if (!File.Exists(options.InputPath)) diag.Report(Severity.Error, CommonStrings.Error_file_not_found, options.InputPath);
        return Task.FromResult(!diag.HasErrors);
    }

    public record Options(string InputPath, string OutputFolder);
}