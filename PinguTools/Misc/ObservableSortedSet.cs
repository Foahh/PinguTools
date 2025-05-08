using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PinguTools.Misc;

public class ObservableSortedSet<T> : IReadOnlySet<T>, ISet<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly SortedSet<T> collection = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public IEnumerator<T> GetEnumerator()
    {
        return collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(T item)
    {
        return collection.Contains(item);
    }

    public int Count => collection.Count;

    public void UnionWith(IEnumerable<T> other)
    {
        collection.UnionWith(other);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    bool ISet<T>.Add(T item)
    {
        if (!collection.Add(item)) return false;
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        return true;
    }

    public void Add(T item)
    {
        collection.Add(item);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    public void Clear()
    {
        collection.Clear();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        collection.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        collection.Remove(item);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        return true;
    }

    public bool IsReadOnly => ((ICollection<T>)collection).IsReadOnly;

    public static implicit operator ObservableSortedSet<T>(SortedSet<T> set)
    {
        var observableSet = new ObservableSortedSet<T>();
        foreach (var item in set) observableSet.Add(item);
        return observableSet;
    }

    #region Not Implemented ISet<T> Members

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        throw new NotSupportedException();
    }

    #endregion
}