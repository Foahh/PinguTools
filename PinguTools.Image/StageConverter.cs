using BCnEncoder.Shared;
using PinguTools.Common;
using PinguTools.Common.Localization;
using PinguTools.Image.Localization;
using PinguTools.Image.Properties;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PinguTools.Image;

public sealed class StageConverter(DdsChunkLocator chunks) : IConverter<StageConverter.Context>
{
    private readonly byte[] stageTemplate = Resources.st_dummy_afb;

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!CanConvert(context, diag)) return;
        if (context.StageId is not { } stageId) throw new OperationCanceledException(CommonStrings.Error_stage_id_is_not_set);

        progress?.Report(Strings.Status_processing_background);
        var bgData = await File.ReadAllBytesAsync(context.BgPath, ct);
        var bgFile = await ConvertBackgroundAsync(bgData);

        ct.ThrowIfCancellationRequested();

        progress?.Report(Strings.Status_processing_effects);
        var fxFiles = await Task.WhenAll(context.FxPaths.Select(async p => string.IsNullOrEmpty(p) ? null : await File.ReadAllBytesAsync(p, ct)));
        var fxFile = await ConvertEffectAsync(fxFiles);

        ct.ThrowIfCancellationRequested();

        progress?.Report(Strings.Status_merging);
        var templateChunks = chunks.Locate(stageTemplate);
        var stFile = chunks.Replace(stageTemplate, templateChunks, [bgFile, fxFile]);

        progress?.Report(CommonStrings.Status_writing);
        var xml = ChartXmlBuilder.BuildStageXml(stageId, context.NoteFieldLane);
        var stageData = xml.Descendants("StageData").First();
        var nameTag = stageData.Descendants("name").First().Value;
        var workdirName = stageData.Descendants("dataName").First().Value;
        var stFileName = stageData.Descendants("baseFile").First().Value;
        var nfFileName = stageData.Descendants("notesFieldFile").First().Value;

        context.Result = new Entry(stageId, nameTag, null);

        var outputDir = Path.Combine(context.OutputFolder, workdirName);
        Directory.CreateDirectory(outputDir);
        await File.WriteAllBytesAsync(Path.Combine(outputDir, stFileName), stFile, ct);
        await File.WriteAllBytesAsync(Path.Combine(outputDir, nfFileName), Resources.nf_dummy_afb, ct);
        xml.Save(Path.Combine(outputDir, "Stage.xml"));
    }

    public bool CanConvert(Context context, IDiagnostic diag)
    {
        var duplicates = context.StageNames.Where(p => p.Id == context.StageId);
        foreach (var d in duplicates) diag.Report(DiagnosticSeverity.Warning, string.Format(Strings.Diag_stage_already_exists, d, context.StageId));

        if (context.StageId is null) diag.Report(DiagnosticSeverity.Error, string.Format(CommonStrings.Error_stage_id_is_not_set));
        if (!File.Exists(context.BgPath)) diag.Report(DiagnosticSeverity.Error, string.Format(CommonStrings.Error_file_not_found, context.BgPath));
        return !diag.HasErrors;
    }

    private static Task<byte[]> ConvertBackgroundAsync(byte[] img)
    {
        return Helper.ConvertDdsAsync(img, 1920, 1080);
    }

    private async static Task<byte[]> ConvertEffectAsync(byte[]?[] files)
    {
        const int tile = 256;
        const int canvas = tile * 2;

        using var ms = new MemoryStream();

        await Task.Run(() =>
        {
            using Image<Rgba32> img = new(canvas, canvas);

            for (var i = 0; i < 4; i++)
            {
                if (i >= files.Length || files[i] is null) continue;
                var imgFile = files[i];

                var x = i % 2 * tile;
                var y = i / 2 * tile;
                img.Mutate(c =>
                {
                    using var tileImg = SixLabors.ImageSharp.Image.Load<Rgba32>(imgFile);
                    if (tileImg.Width != tile || tileImg.Height != tile) tileImg.Mutate(t => t.Resize(tile, tile));
                    c.DrawImage(tileImg, new Point(x, y), 1f);
                });
            }
            img.SaveAsPng(ms);
        });

        return await Helper.ConvertDdsAsync(ms.ToArray(), 512, 512, CompressionFormat.Bc3).ConfigureAwait(false);
    }

    public record Context(string BgPath, string[] FxPaths, int? StageId, string OutputFolder, Entry NoteFieldLane, IReadOnlyCollection<Entry> StageNames)
    {
        public Entry Result { get; set; } = Entry.Default;
    }
}