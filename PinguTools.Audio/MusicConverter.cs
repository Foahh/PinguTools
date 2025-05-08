using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PinguTools.Audio.Localization;
using PinguTools.Audio.Properties;
using PinguTools.Common;
using PinguTools.Common.Localization;

namespace PinguTools.Audio;

public sealed class MusicConverter(LoudNormalizer? loudNormalizer) : IConverter<MusicConverter.Context>
{
    private static readonly WaveFormat Pcm48K16 = new(48000, 16, 2);
    private LoudNormalizer? LoudNormalizer { get; } = loudNormalizer;
    private CriwareConverter CriwareConverter { get; } = new() { Key = 32931609366120192UL };

    public bool CanConvert(Context context, IDiagnostic diag)
    {
        if (context.SongId is null) diag.Report(DiagnosticSeverity.Error, CommonStrings.Error_song_id_is_not_set);
        if (context.PreviewStop < context.PreviewStart) diag.Report(DiagnosticSeverity.Error, Strings.Error_preview_stop_greater_than_start);
        if (!File.Exists(context.InputPath)) diag.Report(DiagnosticSeverity.Error, string.Format(CommonStrings.Error_file_not_found, context.InputPath));
        return !diag.HasErrors;
    }

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!CanConvert(context, diag)) return;
        progress?.Report(CommonStrings.Status_converting_audio);

        progress?.Report(Strings.Status_normalizing);
        await using var reader = CreateReader(context.InputPath);

        var upstream = LoudNormalizer is null ? reader.ToSampleProvider() : LoudNormalizer.Match(reader);
        upstream = ApplyOffset(upstream, context.Offset);
        using var resampler = new MediaFoundationResampler(upstream.ToWaveProvider(), Pcm48K16);

        await using var mem = new MemoryStream();
        WaveFileWriter.WriteWavFileToStream(mem, resampler);
        mem.Position = 0;
        var waveBytes = mem.ToArray();

        ct.ThrowIfCancellationRequested();

        progress?.Report(CommonStrings.Status_converting);

        var xml = ChartXmlBuilder.BuildCueFileXml(context.SongId);
        var cueFileData = xml.Descendants("CueFileData").First();
        var musicName = cueFileData.Descendants("name").First().Value;

        var (acb, awb) = await CriwareConverter.CreateAsync(musicName, Resources.dummy_acb, waveBytes, context.PreviewStart, context.PreviewStop);
        ct.ThrowIfCancellationRequested();

        progress?.Report(CommonStrings.Status_writing);

        var workdirName = cueFileData.Descendants("dataName").First().Value;
        var acbFileName = cueFileData.Descendants("acbFile").First().Value;
        var awbFileName = cueFileData.Descendants("awbFile").First().Value;
        var outputDir = Path.Combine(context.OutputFolder, workdirName);
        Directory.CreateDirectory(outputDir);
        await File.WriteAllBytesAsync(Path.Combine(outputDir, acbFileName), acb, ct);
        await File.WriteAllBytesAsync(Path.Combine(outputDir, awbFileName), awb, ct);
        xml.Save(Path.Combine(outputDir, "CueFile.xml"));
    }

    private static ISampleProvider ApplyOffset(ISampleProvider provider, double offset)
    {
        if (offset <= 0.0000001) return provider;
        var offsetProvider = new OffsetSampleProvider(provider);
        if (offset > 0)
        {
            offsetProvider.DelayBy = TimeSpan.FromSeconds(offset);
            offsetProvider.SkipOver = TimeSpan.Zero;
        }
        else if (offset < 0)
        {
            offsetProvider.DelayBy = TimeSpan.Zero;
            offsetProvider.SkipOver = TimeSpan.FromSeconds(-offset);
        }
        return offsetProvider;
    }

    private static WaveStream CreateReader(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".ogg" => new VorbisWaveReader(path),
            _ => new AudioFileReader(path)
        };
    }

    public record Context(string InputPath, string OutputFolder, int? SongId, double Offset, double PreviewStart, double PreviewStop);
}