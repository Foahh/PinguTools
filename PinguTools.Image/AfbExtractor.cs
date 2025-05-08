using PinguTools.Common;
using PinguTools.Common.Localization;
using PinguTools.Image.Localization;

namespace PinguTools.Image;

public class AfbExtractor(DdsChunkLocator chunks) : IConverter<AfbExtractor.Options>
{
    public async Task ConvertAsync(Options options, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!CanConvert(options, diag)) return;

        var data = await File.ReadAllBytesAsync(options.InputPath, ct);
        ct.ThrowIfCancellationRequested();

        progress?.Report(Strings.Status_extracting);
        var files = chunks.Extract(data, chunks.Locate(data));
        if (files.Length == 0)
        {
            diag.Throw(Strings.Error_no_DDS_chunks_found);
            return;
        }

        ct.ThrowIfCancellationRequested();

        progress?.Report(CommonStrings.Status_writing);
        var baseName = Path.GetFileNameWithoutExtension(options.InputPath);
        var writeTasks = files.Select((bytes, i) =>
        {
            var outName = Path.Combine(options.OutputFolder, $"{baseName}_{i + 1:0000}.dds");
            return File.WriteAllBytesAsync(outName, bytes, ct);
        });
        await Task.WhenAll(writeTasks).ConfigureAwait(false);
    }

    public bool CanConvert(Options options, IDiagnostic diag)
    {
        if (!File.Exists(options.InputPath)) diag.Report(DiagnosticSeverity.Error, string.Format(CommonStrings.Error_file_not_found, options.InputPath));
        return !diag.HasErrors;
    }

    public record Options(string InputPath, string OutputFolder);
}