using PinguTools.Common;

namespace PinguTools.Chart.Models.mgxc;

public class Chart
{
    public ChartMeta Meta { get; set; } = new();
    public Note Notes { get; set; } = new();
    public Event Events { get; set; } = new();

    public int GetLastTick(Func<Note, bool>? noteFilter = null)
    {
        var p = noteFilter != null ? Notes.Children.Where(noteFilter) : Notes.Children;
        return p.Select(note => note.GetLastTick()).Prepend(0).Max();
    }
}