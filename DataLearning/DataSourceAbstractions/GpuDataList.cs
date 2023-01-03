using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public sealed class GpuDataList : IList<IData>
{
    /// <summary>
    /// Here IData will always be GpuData
    /// </summary>
    IList<IData> Storage;
    public GpuDataList(IDataAccess<float> storage, IDataAccess<byte> indices)
    {
        this.DataStorage = new DataStorage<float>(storage,indices);
        Storage = new List<IData>();
    }
    public IData this[int index] { 
        get => Storage[index]; 
        set{
            #pragma warning disable
            var vec = Storage[index].Input as ArrayedVector;
            vec?.CopyValuesFromVector(value.Input);
            Storage[index] = new GpuData(vec);
            #pragma warning enable
        }
    }

    public DataStorage<float> DataStorage { get; }

    public int Count => Storage.Count;

    public bool IsReadOnly => Storage.IsReadOnly;

    public void Add(IData item)
    {
        var vec = new ArrayedVector(DataStorage);
        vec.CopyValuesFromVector(item.Input);
        Storage.Add(new GpuData(vec));
    }

    public void Clear()
    {
        Storage.Clear();
        DataStorage.Indices.Fill(0);
    }

    public bool Contains(IData item)
    {
        return Storage.Contains(item);
    }

    public void CopyTo(IData[] array, int arrayIndex)
    {
        Storage.CopyTo(array, arrayIndex);
    }

    public IEnumerator<IData> GetEnumerator()
    {
        return Storage.GetEnumerator();
    }

    public int IndexOf(IData item)
    {
        return Storage.IndexOf(item);
    }

    public void Insert(int index, IData item)
    {
        Storage.Insert(index, item);
    }

    public bool Remove(IData item)
    {
        return Storage.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Storage.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Storage).GetEnumerator();
    }
}