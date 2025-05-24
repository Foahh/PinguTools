using System.Runtime.InteropServices;
using static libebur128.EbuR128Native;

namespace libebur128;

[Flags]
public enum EbuR128Modes
{
    EBUR128_MODE_M = 1 << 0,
    EBUR128_MODE_S = 1 << 1 | EBUR128_MODE_M,
    EBUR128_MODE_I = 1 << 2 | EBUR128_MODE_M,
    EBUR128_MODE_LRA = 1 << 3 | EBUR128_MODE_S,
    EBUR128_MODE_SAMPLE_PEAK = 1 << 4 | EBUR128_MODE_M,
    EBUR128_MODE_TRUE_PEAK = 1 << 5 | EBUR128_MODE_M | EBUR128_MODE_SAMPLE_PEAK,
    EBUR128_MODE_HISTOGRAM = 1 << 6
};

public enum EbuR128Result
{
    EBUR128_SUCCESS = 0,
    EBUR128_ERROR_NOMEM,
    EBUR128_ERROR_INVALID_MODE,
    EBUR128_ERROR_INVALID_CHANNEL_INDEX,
    EBUR128_ERROR_NO_CHANGE
}

public sealed class EbuR128Handler : SafeHandle
{
    public EbuR128Handler(uint channels, uint samplerate, EbuR128Modes mode) : base(IntPtr.Zero, ownsHandle: true)
    {
        unsafe
        {
            handle = (IntPtr)ebur128_init(channels, new CULong(samplerate), (int)mode);
        }
        SetHandle(handle);
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        unsafe
        {
            var ptr = (State*)handle;
            ebur128_destroy(&ptr);
            return true;
        }
    }

    public void AddFramesFloat(float[] src, nuint frames)
    {
        unsafe
        {
            fixed (float* pSrc = src)
            {
                CheckResult(ebur128_add_frames_float((State*)handle, pSrc, frames));
            }
        }
    }

    public double LoudnessGlobal()
    {
        var result = 0.0;
        unsafe
        {
            CheckResult(ebur128_loudness_global((State*)handle, &result));
        }
        return result;
    }

    public double AbsoluteTruePeak()
    {
        var result = 0.0;
        unsafe
        {
            CheckResult(ebur128_true_peak((State*)handle, 0, &result));
        }
        return result;
    }


    private static void CheckResult(int result)
    {
        if ((EbuR128Result)result == EbuR128Result.EBUR128_SUCCESS) return;
        throw new InvalidOperationException($"EbuR128 Error: {result.ToString()}");
    }
}