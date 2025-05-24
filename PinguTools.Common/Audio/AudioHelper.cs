using NAudio.Wave;

namespace PinguTools.Common.Audio;

public static class AudioHelper
{
    public static double DecibelsToLinear(double decibels)
    {
        return Math.Pow(10, decibels / 20);
    }

    public static double LinearToDecibels(double linear)
    {
        return 20 * Math.Log10(linear);
    }

    public static bool IsValidAudio(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;
            using var reader = new AudioFileReader(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}