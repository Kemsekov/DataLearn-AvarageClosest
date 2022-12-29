using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Storage;

//TODO: add tests for it
//test that it occupies space in a right way
//access it and frees it

public class ArrayedVectorStorage<T> : VectorStorage<T>, IDisposable
where T : unmanaged, System.IEquatable<T>, System.IFormattable
{
    public DataStorage<T> DataStorage { get; }
    public int StartIndex { get; }

    public ArrayedVectorStorage(DataStorage<T> dataStorage,int length) : base(length)
    {
        this.DataStorage = dataStorage;
        this.StartIndex = dataStorage.Insert(new T[length]);
        if(this.StartIndex==-1)
            throw new IndexOutOfRangeException("Could not create new ArrayedVector because corresponding data storage out of space");
    }

    public override bool IsDense => true;
    public override T At(int index)
    {
        if(disposed)
            throw new ObjectDisposedException("Could not access data of disposed vector");
        return DataStorage.Get(StartIndex)[index];
    }
    public override void At(int index, T value)
    {
        if(disposed)
            throw new ObjectDisposedException("Could not access data of disposed vector");
        DataStorage.Get(StartIndex+index)[index] = value;
    }
    ~ArrayedVectorStorage(){
        Dispose();
    }
    bool disposed = false;
    public void Dispose()
    {
        if(disposed) return;
        DataStorage.Free(StartIndex);
        disposed = true;
    }
}