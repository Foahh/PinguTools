using libebur128_net;
using libebur128_net.libebur128;
using NAudio.Wave;

namespace PinguTools.Audio;

public sealed class LoudNormalizer
{
    public double TargetLufs { get; init; } = -9.0; // LUFS
    public double Tolerance { get; init; } = 0.5; // LU
    public double MaxTruePeakDbTp { get; init; } = -1.0; // dBTP
    public bool TruePeakLimiting { get; init; } = true;
    public int LookAheadMs { get; init; } = 12;
    public int ReleaseMs { get; init; } = 200;

    public ISampleProvider Match(WaveStream stream)
    {
        var (lufs, mTpDb) = Analyze(stream);

        var gain = TargetLufs - lufs;
        var withinTolerance = Math.Abs(gain) <= Tolerance;

        var prospectivePeak = mTpDb + gain;
        var clipping = prospectivePeak > MaxTruePeakDbTp;

        var upstream = stream.ToSampleProvider();
        if (!withinTolerance) upstream = new GainProvider(upstream, gain);
        if (TruePeakLimiting && clipping) upstream = new LookAheadLimiter(upstream, LookAheadMs, ReleaseMs, MaxTruePeakDbTp);

        return upstream;
    }

    private static (double lufs, double mTpDb) Analyze(WaveStream waveStream)
    {
        var ch = (uint)waveStream.WaveFormat.Channels;
        var rate = (uint)waveStream.WaveFormat.SampleRate;

        using var ebu = Ebur128.Init(ch, rate, libebur128Native.mode.EBUR128_MODE_I | libebur128Native.mode.EBUR128_MODE_TRUE_PEAK);

        var reader = waveStream.ToSampleProvider();
        var buf = new float[rate * ch];
        int read;
        while ((read = reader.Read(buf, 0, buf.Length)) > 0) ebu.AddFramesFloat(buf, (uint)(read / ch));
        waveStream.Position = 0;

        var lufs = ebu.LoudnessGlobal();
        var maxTp = ebu.AbsoluteTruePeak();
        var maxTpDb = AudioHelper.LinearToDecibels(maxTp);
        return (lufs, maxTpDb);
    }
}