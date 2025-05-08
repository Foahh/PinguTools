namespace PinguTools.Audio;

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

    public static decimal CalculateOffset(decimal bpm, int numerator, int denominator, int bar = 1) // in seconds
    {
        var beatsPerSecond = bpm / 60;
        var beatLength = 1 / beatsPerSecond;

        var measureLength = beatLength * numerator;
        var fractionOfMeasure = measureLength * (4m / denominator);

        var offset = bar * fractionOfMeasure;
        return offset;
    }
}