using NAudio.Wave;

namespace PinguTools.Common.Audio;

public sealed class GainProvider : ISampleProvider
{
    private readonly float gain;
    private readonly ISampleProvider src;

    public GainProvider(ISampleProvider source, double gain)
    {
        src = source;
        this.gain = (float)AudioHelper.DecibelsToLinear(gain);
        WaveFormat = src.WaveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        var n = src.Read(buffer, offset, count);
        for (var i = 0; i < n; i++) buffer[offset + i] *= gain;
        return n;
    }
}