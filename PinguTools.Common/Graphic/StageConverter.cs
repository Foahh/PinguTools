using BCnEncoder.Shared;
using PinguTools.Common.Asset;
using PinguTools.Common.Graphic.Locator;
using PinguTools.Common.Resources;
using PinguTools.Common.Xml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PinguTools.Common.Graphic;

public sealed class StageConverter(AssetManager asm) : IConverter<StageConverter.Context>
{
    private readonly DdsChunkLocator chunks = new();

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(context, diag)) return;
        if (context.StageId is not { } stageId) throw new DiagnosticException(CommonStrings.Error_stage_id_is_not_set);

        progress?.Report(CommonStrings.Status_processing_background);
        var tempBgDdsPath = Path.Combine(ResourceManager.TempPath, $"{stageId}_bg.dds");
        await ConvertBackgroundAsync(context.BgPath, tempBgDdsPath);
        ResourceManager.Register(tempBgDdsPath);

        ct.ThrowIfCancellationRequested();

        progress?.Report(CommonStrings.Status_processing_effects);
        var tempFxDdsPath = Path.Combine(ResourceManager.TempPath, $"{stageId}_fx.dds");
        await ConvertEffectAsync(context.FxPaths, tempFxDdsPath);
        ResourceManager.Register(tempFxDdsPath);

        ct.ThrowIfCancellationRequested();

        progress?.Report(CommonStrings.Status_merging);

        var templateChunks = chunks.Locate(CommonResources.st_dummy_afb);

        progress?.Report(CommonStrings.Status_writing);
        var xml = new StageXml(stageId, context.NoteFieldLane);
        context.Result = xml.Name;
        var outputDir = Path.Combine(context.OutputFolder, xml.DataName);
        await xml.SaveAsync(outputDir);

        await using (var bgFile = File.OpenRead(tempBgDdsPath))
        {
            await using var fxFile = File.OpenRead(tempFxDdsPath);
            var baseFilePath = Path.Combine(outputDir, xml.BaseFile);
            await chunks.ReplaceAsync(CommonResources.st_dummy_afb, baseFilePath, templateChunks, [bgFile, fxFile]);
        }
        await File.WriteAllBytesAsync(Path.Combine(outputDir, xml.NotesFieldFile), CommonResources.nf_dummy_afb, ct);
    }

    public async Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        var duplicates = asm.StageNames.Where(p => p.Id == context.StageId);
        foreach (var d in duplicates) diag.Report(Severity.Warning, string.Format(CommonStrings.Diag_stage_already_exists, d, context.StageId));

        if (context.StageId is null) diag.Report(Severity.Error, string.Format(CommonStrings.Error_stage_id_is_not_set));
        if (!File.Exists(context.BgPath)) diag.Report(Severity.Error, CommonStrings.Error_file_not_found, context.BgPath);

        if (!await ImageHelper.IsValidImageAsync(context.BgPath)) diag.Report(Severity.Error, CommonStrings.Error_invalid_bg_image, context.BgPath);
        foreach (var p in context.FxPaths)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (!File.Exists(p)) diag.Report(Severity.Error, CommonStrings.Error_file_not_found, p);
            if (!await ImageHelper.IsValidImageAsync(p)) diag.Report(Severity.Error, CommonStrings.Error_invalid_bg_fx_image, p);
        }

        return !diag.HasErrors;
    }

    private async static Task ConvertBackgroundAsync(string inPath, string outPath)
    {
        var img = await Image.LoadAsync<Rgba32>(inPath);
        await ImageHelper.ConvertDdsAsync(img, outPath, 1920, 1080);
    }

    private async static Task ConvertEffectAsync(string[] inFiles, string outPath)
    {
        const int tile = 256;
        const int canvas = tile * 2;

        using Image<Rgba32> img = new(canvas, canvas);

        for (var i = 0; i < 4; i++)
        {
            if (i >= inFiles.Length || string.IsNullOrWhiteSpace(inFiles[i])) continue;
            var x = i % 2 * tile;
            var y = i / 2 * tile;
            var idx = i;
            img.Mutate(c =>
            {
                using var tileImg = Image.Load<Rgba32>(inFiles[idx]);
                if (tileImg.Width != tile || tileImg.Height != tile) tileImg.Mutate(t => t.Resize(tile, tile));
                c.DrawImage(tileImg, new Point(x, y), 1f);
            });
        }

        await ImageHelper.ConvertDdsAsync(img, outPath, 512, 512, CompressionFormat.Bc3);
    }

    public record Context(string BgPath, string[] FxPaths, int? StageId, string OutputFolder, Entry NoteFieldLane)
    {
        public Entry Result { get; set; } = Entry.Default;
    }
}