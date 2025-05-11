namespace PinguTools.Common;

public record TimePosition(int BarIndex, int BeatIndex, int TickOffset) : IComparable<TimePosition>
{
    public int CompareTo(TimePosition? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        var barIndexComparison = BarIndex.CompareTo(other.BarIndex);
        if (barIndexComparison != 0) return barIndexComparison;
        var beatIndexComparison = BeatIndex.CompareTo(other.BeatIndex);
        if (beatIndexComparison != 0) return beatIndexComparison;
        return TickOffset.CompareTo(other.TickOffset);
    }

    public override string ToString()
    {
        return $"{BarIndex}:{BeatIndex}.{TickOffset}";
    }
}