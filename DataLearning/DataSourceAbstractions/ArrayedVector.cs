using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Storage;

public class ArrayedVector : MathNet.Numerics.LinearAlgebra.Single.Vector, IDisposable
{
    public ArrayedVector(ArrayedVectorStorage<float> storage) : base(storage)
    {
        this.DataStorage = storage.DataStorage;
    }
    public ArrayedVector(DataStorage<float> dataStorage) : base(new ArrayedVectorStorage<float>(dataStorage))
    {
        DataStorage = dataStorage;
    }
    /// <summary>
    /// Place where this vector reside is
    /// </summary>
    public DataStorage<float> DataStorage { get; }
    ~ArrayedVector() => Dispose();
    public void Dispose()
    {
        if(this.Storage is IDisposable d)
            d.Dispose();
    }
    /// <summary>
    /// Replaces values from current vector to values from given vector
    /// </summary>
    public void CopyValuesFromVector(Vector vec){
        var size = Math.Min(vec.Count,Count);
        for(int i = 0;i<size;i++)
            this[i] = vec[i];
    }
}