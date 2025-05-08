namespace PinguTools.Chart.Models.mgxc;

public class Event : TimeNode<Event>
{
    public T Clone<T>(Event? parent = null) where T : Event
    {
        var e = (T)MemberwiseClone();
        e.MakeVirtual(parent);
        return e;
    }

    public override void Sort()
    {
        SortChild((a, b) => a.Tick.CompareTo(b.Tick));
    }
}

public class BpmEvent : Event
{
    public decimal Bpm { get; set; }
}

public class BeatEvent : Event
{
    public int Bar { get; set; }
    public int Denominator { get; set; } = 4;
    public int Numerator { get; set; } = 4;
}

public class SpeedEvent : Event
{
    public decimal Speed { get; set; }
}

public class TimelineEvent : SpeedEvent
{
    public int Timeline { get; set; }
}

public class NoteSpeedEvent : SpeedEvent;