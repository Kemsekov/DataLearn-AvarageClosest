using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Veldrid;

public unsafe class GpuDataAccess<T> : IDataAccess<T>
where T : unmanaged
{
    public GpuDataAccess(MappedResource mappedResource)
    {
        this.Pointer = (T*)mappedResource.Data;
        Length = (int)(mappedResource.SizeInBytes/Unsafe.SizeOf<T>());
    }
    public T this[int index] { get => Pointer[index]; set => Pointer[index] = value; }
    public int Length{get;init;}
    public T* Pointer { get; }
    public Span<T> AsSpan(Range range)
    {
        return new Span<T>(Pointer,Length);
    }
}