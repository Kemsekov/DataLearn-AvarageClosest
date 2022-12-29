using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Storage;

public class ArrayedVector : MathNet.Numerics.LinearAlgebra.Single.Vector
{
    public ArrayedVector(ArrayedVectorStorage<float> storage) : base(storage)
    {
    }
}