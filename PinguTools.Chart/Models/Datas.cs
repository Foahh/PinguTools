/*
   This code is based on the original implementation from:
   https://github.com/inonote/MargreteOnline
*/

namespace PinguTools.Chart.Models;

public readonly record struct Position(int Measure, int Offset);

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

    public int Round { get; } = (int)Math.Round((decimal)Original / SingleTick) * SingleTick;
    public int Result => (int)(Round * FACTOR);

    public Position Position => new(Round / MarResolution, (int)(Round % MarResolution * FACTOR));

    public override string ToString()
    {
        return Original == Round ? $"[{Original}→{Result}]" : $"[{Original}→{Round}→{Result}]";
    }

    public int CompareTo(Time other)
    {
        return Original.CompareTo(other.Original);
    }

    public static Time operator -(Time a, Time b)
    {
        return a.Round - b.Round;
    }

    public static bool operator <(Time a, Time b)
    {
        return a.Round < b.Round;
    }

    public static bool operator >(Time a, Time b)
    {
        return a.Round > b.Round;
    }

    public static implicit operator Time(int value)
    {
        return new Time(value);
    }

    public static implicit operator int(Time value)
    {
        return value.Round;
    }
}

public readonly record struct Height(decimal Original) : IComparable<Height>
{
    public readonly decimal Result = Math.Round(Math.Max(0m, Original * 0.5m + 1m), 1);

    public int CompareTo(Height other)
    {
        return Original.CompareTo(other.Original);
    }

    public static Height operator -(Height a, Height b)
    {
        return a.Original - b.Original;
    }

    public static implicit operator Height(decimal value)
    {
        return new Height(value);
    }

    public static implicit operator decimal(Height value)
    {
        return value.Original;
    }
}