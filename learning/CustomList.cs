using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CustomList<T> : IList<T>
{
    public T this[int index] { get => ((IList<T>)BaseList)[index]; set => ((IList<T>)BaseList)[index] = value; }

    public IList<T> BaseList { get; }
    Func<int> getCount;
    public int Count => getCount();
    public bool IsReadOnly => ((ICollection<T>)BaseList).IsReadOnly;

    public CustomList(IList<T> baseList)
    {
        BaseList = baseList;
        getCount = () => ((ICollection<T>)BaseList).Count;
    }
    public void FreezeCount(int freezedCount)
    {
        getCount = () => freezedCount;
    }
    public void UnfreezeCount()
    {
        getCount = () => ((ICollection<T>)BaseList).Count;
    }
    public void Add(T item)
    {
        ((ICollection<T>)BaseList).Add(item);
    }

    public void Clear()
    {
        ((ICollection<T>)BaseList).Clear();
    }

    public bool Contains(T item)
    {
        return ((ICollection<T>)BaseList).Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        ((ICollection<T>)BaseList).CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)BaseList).GetEnumerator();
    }

    public int IndexOf(T item)
    {
        return ((IList<T>)BaseList).IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        ((IList<T>)BaseList).Insert(index, item);
    }

    public bool Remove(T item)
    {
        return ((ICollection<T>)BaseList).Remove(item);
    }

    public void RemoveAt(int index)
    {
        ((IList<T>)BaseList).RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)BaseList).GetEnumerator();
    }
}