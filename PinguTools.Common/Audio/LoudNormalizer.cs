using libebur128;
using NAudio.Wave;

namespace PinguTools.Common.Audio;

public sealed class LoudNormalizer
{
    public const double TargetLufs = -8.0; // LUFS
    public const double Tolerance = 0.5; // LU
    public const double MaxTruePeakDbTp = -1.0; // dBTP
    public const bool TruePeakLimiting = true;
    public const int LookAheadMs = 12;
    public const int ReleaseMs = 200;

    public ISampleProvider Match(WaveStream stream)
    {
        var (lufs, mTpDb) = Analyze(stream);

        var gain = TargetLufs - lufs;
        var withinTolerance = Math.Abs(gain) <= Tolerance;

        var prospectivePeak = mTpDb + gain;
        var doClipping = prospectivePeak > MaxTruePeakDbTp;

        var upstream = stream.ToSampleProvider();
        if (!withinTolerance) upstream = new GainProvider(upstream, gain);
        if (TruePeakLimiting && doClipping) upstream = new LookAheadLimiter(upstream, LookAheadMs, ReleaseMs);

        return upstream;
    }

    private static (double lufs, double mTpDb) Analyze(WaveStream waveStream)
    {
        var ch = (uint)waveStream.WaveFormat.Channels;
        var rate = (uint)waveStream.WaveFormat.SampleRate;

        using var ebu = new EbuR128Handler(ch, rate, EbuR128Modes.EBUR128_MODE_I | EbuR128Modes.EBUR128_MODE_TRUE_PEAK);
        var reader = waveStream.ToSampleProvider();
        var buf = new float[rate * ch];
        int read;
        while ((read = reader.Read(buf, 0, buf.Length)) > 0) ebu.AddFramesFloat(buf, (uint)(read / ch));
        waveStream.Seek(0, SeekOrigin.Begin);

        var lufs = ebu.LoudnessGlobal();
        var maxTp = ebu.AbsoluteTruePeak();
        var maxTpDb = AudioHelper.LinearToDecibels(maxTp);
        return (lufs, maxTpDb);
    }
}