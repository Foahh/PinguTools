using NAudio.Wave;

namespace PinguTools.Common.Audio;

public sealed class LookAheadLimiter : ISampleProvider
{
    private readonly float[] delayBuffer;
    private readonly float releaseCoeff;
    private readonly ISampleProvider src;
    private readonly float threshold;

    private float gain = 1f;
    private float[]? tempBuffer;
    private int writePos;

    public LookAheadLimiter(ISampleProvider src, double lookAheadMs = 5.0, double releaseMs = 50.0, double maxDbTp = -1.0)
    {
        this.src = src;
        WaveFormat = src.WaveFormat;
        var channels = WaveFormat.Channels;
        var sampleRate = WaveFormat.SampleRate;

        var lookAheadSamples = (int)Math.Ceiling(sampleRate * lookAheadMs / 1000.0);
        delayBuffer = new float[lookAheadSamples * channels];

        var relTimeSec = releaseMs / 1000.0;
        releaseCoeff = (float)Math.Exp(-1.0 / (relTimeSec * sampleRate));
        threshold = (float)AudioHelper.DecibelsToLinear(maxDbTp);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        if (tempBuffer == null || tempBuffer.Length < count) tempBuffer = new float[count];

        var read = src.Read(tempBuffer, 0, count);
        if (read == 0) return 0;

        for (var n = 0; n < read; ++n)
        {
            var inSample = tempBuffer[n];
            var outSample = delayBuffer[writePos];

            delayBuffer[writePos] = inSample;
            writePos++;
            if (writePos >= delayBuffer.Length) writePos = 0;

            var absIn = Math.Abs(inSample);
            var desiredGain = 1f;

            if (absIn is > 0f and > 0.0000001f && absIn > threshold) desiredGain = threshold / absIn;

            if (desiredGain < gain) gain = desiredGain;
            else gain = gain * releaseCoeff + (1f - releaseCoeff);

            buffer[offset + n] = outSample * gain;
        }

        return read;
    }
}