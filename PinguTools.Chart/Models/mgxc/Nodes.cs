/*
   This model is based on the original implementation from:
   https://github.com/inonote/MargreteOnline
*/

using System.Text.Json.Serialization;

namespace PinguTools.Chart.Models.mgxc;

public abstract class TimeNode<T> where T : TimeNode<T>
{
    protected List<T> ChildNodes { get; } = [];

    public IReadOnlyList<T> Children => ChildNodes;

    [JsonIgnore] public T? NextSibling { get; protected set; }
    [JsonIgnore] public T? Parent { get; protected set; }
    [JsonIgnore] public bool IsVirtual { get; protected set; }
    [JsonIgnore] public T? PreviousSibling { get; protected set; }

    [JsonIgnore] public T? FirstChild => ChildNodes.Count > 0 ? ChildNodes[0] : null;
    [JsonIgnore] public T? LastChild => ChildNodes.Count > 0 ? ChildNodes[^1] : null;

    public virtual Time Tick { get; set; }

    public override string ToString()
    {
        return $"{GetType().Name} [Tick: {Tick}]";
    }

    public T? AppendChild(T newNode)
    {
        var parent = newNode.Parent;
        if (parent != null && !parent.RemoveChild(newNode)) return null;
        newNode.Parent = (T)this;
        ChildNodes.Add(newNode);

        ArrangeSibling();
        return newNode;
    }

    public T? InsertBefore(T newNode, T? referenceNode)
    {
        if (referenceNode == null) return AppendChild(newNode);
        var parent = newNode.Parent;
        if (parent != null && !parent.RemoveChild(newNode)) return null;

        var beforeIndex = ChildNodes.FindIndex(x => x == referenceNode);
        if (beforeIndex == -1) return AppendChild(newNode);

        newNode.Parent = (T)this;
        ChildNodes.Insert(beforeIndex, newNode);

        ArrangeSibling();
        return newNode;
    }

    public T? InsertAfter(T newNode, T? referenceNode)
    {
        if (referenceNode == null) return AppendChild(newNode);
        var parent = newNode.Parent;
        if (parent != null && !parent.RemoveChild(newNode)) return null;

        var afterIndex = ChildNodes.FindIndex(x => x == referenceNode);
        if (afterIndex == -1) return AppendChild(newNode);

        newNode.Parent = (T)this;
        ChildNodes.Insert(afterIndex + 1, newNode);

        ArrangeSibling();
        return newNode;
    }

    public bool RemoveChild(T child)
    {
        var childIndex = ChildNodes.FindIndex(x => x == child);
        if (childIndex == -1) return false;

        ChildNodes.RemoveAt(childIndex);

        child.Parent = null;
        child.PreviousSibling = null;
        child.NextSibling = null;

        ArrangeSibling();
        return true;
    }

    protected void ArrangeSibling()
    {
        if (ChildNodes.Count == 0) return;
        ChildNodes[0].PreviousSibling = null;
        for (var i = 1; i < ChildNodes.Count; i++)
        {
            ChildNodes[i - 1].NextSibling = ChildNodes[i];
            ChildNodes[i].PreviousSibling = ChildNodes[i - 1];
        }
        ChildNodes[^1].NextSibling = null;
    }

    public void MakeVirtual(T? parent)
    {
        IsVirtual = true;
        Parent = parent;
    }

    public int GetLastTick()
    {
        return ChildNodes.Select(n => n.GetLastTick()).Prepend(Tick).Max();
    }

    public void Offset(int offset)
    {
        Tick = Math.Max(Tick + offset, 0);
        foreach (var child in ChildNodes) child.Offset(offset);
    }
    
    protected void SortChild(Comparison<T> comparison)
    {
        ChildNodes.Sort(comparison);
        ArrangeSibling();
        foreach (var child in ChildNodes) child.SortChild(comparison);
    }

    public abstract void Sort();
}

public class Note : TimeNode<Note>
{
    public virtual int Lane { get; set; }
    public virtual int Width { get; set; } = 1;
    public virtual int Timeline { get; set; }

    public override string ToString()
    {
        return $"{base.ToString()} [Lane: {Lane}, Width: {Width}, Timeline: {Timeline}]";
    }

    public bool IsInside(Note other, out bool isSmaller)
    {
        isSmaller = Width < other.Width;
        return Tick == other.Tick && Lane + Width <= other.Lane + other.Width;
    }

    public bool IsViolate(Note other)
    {
        return this != other && Tick == other.Tick && Lane == other.Lane && Width == other.Width && Timeline != other.Timeline;
    }

    public override void Sort()
    {
        SortChild((x, y) =>
        {
            var result = x.Tick.CompareTo(y.Tick);
            if (result != 0) return result;
            result = x.Lane.CompareTo(y.Lane);
            if (result != 0) return result;
            result = x.Width.CompareTo(y.Width);
            if (result != 0) return result;
            return x.Timeline.CompareTo(y.Timeline);
        });

        // move negative notes after the paired positive notes
        foreach (var child in Children.OfType<NegativeNote>().ToList())
        {
            if (child.PairNote == null) continue;
            Note? target = child.PairNote;
            while (target != null && target.Parent != this) target = target.Parent;
            if (target == null || target.Parent != this) continue;
            InsertAfter(child, target);
        }
    }
}

public abstract class ExTapableNote : Note
{
    public abstract ExEffect? Effect { get; set; }
}

// I am a bad person who just want people getting confused
public abstract class PairableNote<TSelf, TPair> : Note where TSelf : PairableNote<TSelf, TPair> where TPair : PairableNote<TPair, TSelf>
{
    public TPair? PairNote { get; set; }

    public void MakePair(TPair? targetNote)
    {
        if ((targetNote?.IsVirtual ?? false) || IsVirtual) throw new InvalidOperationException("Cannot pair virtual notes");
        if (PairNote != null) PairNote.PairNote = null;
        if (targetNote == null)
        {
            PairNote = null;
        }
        else
        {
            if (targetNote.PairNote != null) targetNote.PairNote.PairNote = null;
            targetNote.PairNote = (TSelf)this;
            PairNote = targetNote;
        }
    }
}

// positive note: note that can have air 
public abstract class PositiveNote : PairableNote<PositiveNote, NegativeNote>;

// negative note: air note
public abstract class NegativeNote : PairableNote<NegativeNote, PositiveNote>;