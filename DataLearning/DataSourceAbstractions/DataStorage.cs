/// <summary>
/// Provides a chunk-type data storage.
/// </summary>
public class DataStorage<T>
where T : unmanaged
{
    public DataStorage(int length,int elementSize)
    {
        this.Storage = new ArrayDataAccess<T>(new T[length*elementSize]);
        this.Indices = new ArrayDataAccess<byte>(new byte[length]);
        this.ElementSize = elementSize;
    }
    public DataStorage(IDataAccess<T> storage, IDataAccess<byte> indices){
        Storage = storage;
        Indices = indices;
        this.ElementSize = storage.Length/indices.Length;
        Indices.AsSpan(0..Indices.Length).Fill(0);
    }
    public Span<T> Get(int index)   
    {
        var shift = index*ElementSize;
        if (IsFree(index))
            throw new KeyNotFoundException($"There is no element under index {index}");
        return Storage.AsSpan(shift..(shift+ElementSize));
    }
    public int MaxLength => Storage.Length/ElementSize;
    public int Length{get;private set;} = 0;
    public IDataAccess<T> Storage { get; }
    public IDataAccess<byte> Indices { get; }
    public int ElementSize { get; }
    /// <returns>
    /// Index of inserted element or -1 if there is not enough space
    /// to insert new element
    /// </returns>
    public int Insert(ReadOnlySpan<T> element)
    {
        for (int i = 0; i < Indices.Length; i++)
        {
            if (IsFree(i))
            {
                var shift = i*ElementSize;
                Length++;
                for(int b = 0;b<ElementSize;b++)
                    Storage[shift+b] = element[b];
                Indices[i] = 1;
                return i;
            }
        }
        return -1;
    }
    /// <summary>
    /// Marks element under given index as free
    /// </summary>
    public void Free(int index)
    {
        if(Indices[index]==1) Length--;
        Indices[index] = 0;
    }
    /// <returns>true if element under given index is free, else false</returns>
    public bool IsFree(int index)
    {
        return Indices[index] == 0;
    }
}
