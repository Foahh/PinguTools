using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PinguTools.Common.Resources;
using PinguTools.Common.Xml;

namespace PinguTools.Common.Audio;

public sealed class MusicConverter : IConverter<MusicConverter.Context>
{
    private static readonly WaveFormat Pcm48K16 = new(48000, 16, 2);
    private LoudNormalizer LoudNormalizer { get; } = new();
    private CriwareConverter CriwareConverter { get; } = new() { Key = 32931609366120192UL };

    public Task<bool> CanConvertAsync(Context context, IDiagnostic diag)
    {
        if (context.Meta.Id is null) diag.Report(Severity.Error, CommonStrings.Error_song_id_is_not_set);
        if (context.Meta.BgmPreviewStop < context.Meta.BgmPreviewStart) diag.Report(Severity.Error, CommonStrings.Error_preview_stop_greater_than_start);
        var path = context.Meta.FullBgmFilePath;
        if (!File.Exists(path)) diag.Report(Severity.Error, CommonStrings.Error_file_not_found, path);
        if (!AudioHelper.IsValidAudio(path)) diag.Report(Severity.Error, CommonStrings.Error_invalid_audio, path);
        return Task.FromResult(!diag.HasErrors);
    }

    public async Task ConvertAsync(Context context, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!await CanConvertAsync(context, diag)) return;
        progress?.Report(CommonStrings.Status_converting_audio);

        var path = context.Meta.FullBgmFilePath;
        if (context.Meta.NormalizeBgm)
        {
            progress?.Report(CommonStrings.Status_normalizing);
            await using var reader = CreateReader(path);

            var upstream = LoudNormalizer.Match(reader);
            upstream = ApplyOffset(upstream, (double)context.Meta.BgmRealOffset);
            using var resampler = new MediaFoundationResampler(upstream.ToWaveProvider(), Pcm48K16);

            path = Path.Combine(ResourceManager.TempPath, $"norm_{Path.GetFileNameWithoutExtension(path)}.wav");
            await using (var fs = File.Create(path)) WaveFileWriter.WriteWavFileToStream(fs, resampler);

            ResourceManager.Register(path);
            ct.ThrowIfCancellationRequested();
        }

        progress?.Report(CommonStrings.Status_reading);
        await using var waveStream = File.OpenRead(path);

        ct.ThrowIfCancellationRequested();

        progress?.Report(CommonStrings.Status_Convert_cue_file);

        var songId = context.Meta.Id ?? throw new DiagnosticException(CommonStrings.Error_song_id_is_not_set);
        var xml = new CueFileXml(songId);

        var pvStart = (double)context.Meta.BgmPreviewStart;
        var pvStop = (double)context.Meta.BgmPreviewStop;

        if (pvStart > 120) diag.Report(Severity.Warning, CommonStrings.Diag_pv_laterthan_120);

        progress?.Report(CommonStrings.Status_writing);
        var outputDir = Path.Combine(context.OutputFolder, xml.DataName);
        await xml.SaveAsync(outputDir);

        var acbPath = Path.Combine(outputDir, xml.AcbFile);
        var awbPath = Path.Combine(outputDir, xml.AwbFile);
        await CriwareConverter.CreateAsync(acbPath, awbPath, xml.DataName, waveStream, pvStart, pvStop, ct);
        ct.ThrowIfCancellationRequested();
    }

    private static WaveStream CreateReader(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".ogg" => new VorbisWaveReader(path), // NAudio does not support Ogg Vorbis
            _ => new AudioFileReader(path)
        };
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

    public record Context(Meta Meta, string OutputFolder);
}