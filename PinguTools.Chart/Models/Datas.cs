/*
   This code is based on the original implementation from:
   https://github.com/inonote/MargreteOnline
*/

namespace PinguTools.Chart.Models;

public readonly record struct Time(int Original) : IComparable<Time>
{
    public bool Equals(Time other)
    {
        return Original == other.Original;
    }

    public override int GetHashCode()
    {
        return Original;
    }

    public const int MarResolution = 1920;
    public const int CtsResolution = 384;
    public const int SingleTick = MarResolution / CtsResolution;
    private const decimal FACTOR = (decimal)CtsResolution / MarResolution;

    public int Rounded { get; } = (int)Math.Round((decimal)Original / SingleTick) * SingleTick;
    public int Measure => Rounded / MarResolution;
    public int Offset => (int)(Rounded % MarResolution * FACTOR);
    public int Scaled => (int)(Rounded * FACTOR);

    public int CompareTo(Time other)
    {
        return Original.CompareTo(other.Original);
    }

    public static Time operator -(Time a, Time b)
    {
        return a.Rounded - b.Rounded;
    }

    public static bool operator <(Time a, Time b)
    {
        return a.Rounded < b.Rounded;
    }

    public static bool operator >(Time a, Time b)
    {
        return a.Rounded > b.Rounded;
    }

    public static implicit operator Time(int value)
    {
        return new Time(value);
    }

    public static implicit operator int(Time value)
    {
        return value.Rounded;
    }
}

public readonly record struct Height(decimal Value) : IComparable<Height>
{
    public readonly decimal Scaled = Math.Round(Math.Max(0m, Value * 0.5m + 1m), 1);

    public int CompareTo(Height other)
    {
        return Value.CompareTo(other.Value);
    }

    public static Height operator -(Height a, Height b)
    {
        return a.Value - b.Value;
    }

    public static implicit operator Height(decimal value)
    {
        return new Height(value);
    }

    public static implicit operator decimal(Height value)
    {
        return value.Value;
    }
}