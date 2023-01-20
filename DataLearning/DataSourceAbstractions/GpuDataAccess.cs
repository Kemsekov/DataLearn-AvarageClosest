using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Veldrid;
public unsafe class GpuDataAccess<T> : IDataAccess<T>, IDisposable
where T : unmanaged
{
    public GpuDataAccess(DeviceBuffer GpuDataBuffer, GraphicsDevice gd)
    {
        this.Device = GpuDataBuffer;
        Length = (int)(GpuDataBuffer.SizeInBytes / Unsafe.SizeOf<T>());
        this.StagingBuffer = gd.ResourceFactory.CreateBuffer(
            new(
                sizeInBytes: (uint)sizeof(T),
                BufferUsage.Staging
            )
        );
        
        this.GD = gd;
        this.ptr = (T*)GD.Map(StagingBuffer, MapMode.Read).Data;
        this.Factory = gd.ResourceFactory;
    }
    void CheckIndex(int index){
        if(index<0 || index>=Length) throw new IndexOutOfRangeException();
    }
    public T this[int index]
    {
        get
        {
            CheckIndex(index);
            using var CommandList = Factory.CreateCommandList();
            CommandList.Begin();
            CommandList.CopyBuffer(Device, (uint)(index * StagingBuffer.SizeInBytes), StagingBuffer, 0, StagingBuffer.SizeInBytes);
            CommandList.End();
            lock(StagingBuffer){
                GD.SubmitCommands(CommandList);
                GD.WaitForIdle();
                return *ptr;
            }
        }
        set
        {
            CheckIndex(index);
            GD.UpdateBuffer(Device, (uint)(index * sizeof(T)), value);
        }
    }
    public DeviceBuffer Device { get; }
    public int Length { get; init; }
    DeviceBuffer StagingBuffer { get; }
    GraphicsDevice GD { get; }

    public ReadOnlyMemory<T> this[Range range]
    {
        get
        {
            var start = range.Start.Value;
            var end = range.End.Value;
            CheckIndex(start);
            CheckIndex(end);
            
            var size = (uint)(sizeof(T) * (end - start));
            using var CommandList = Factory.CreateCommandList();
            using var stagingBuffer = Factory.CreateBuffer(
                new(
                    sizeInBytes: size,
                    BufferUsage.Staging
                )
            );
            CommandList.Begin();
            CommandList.CopyBuffer(Device,(uint)(start*sizeof(T)),stagingBuffer,0,size);
            CommandList.End();
            GD.SubmitCommands(CommandList);
            GD.WaitForIdle();
            var result = (T*)GD.Map(stagingBuffer,MapMode.Read).Data;
            return new Span<T>(result,end - start).ToArray();
        }
    }
    T* ptr;
    ResourceFactory Factory { get; }
    ~GpuDataAccess()
    {
        Dispose();
    }
    public void Dispose()
    {
        StagingBuffer.Dispose();
        ptr = null;
    }

    public IEnumerator<T> GetEnumerator()
    {
        int size = (int)Math.Ceiling(Length*1.0f/128);
        int left = 0;
        for (int i = 0; i < size; i++){
            left = (i*128);
            if(left>=Length) break;
            var range = left..Math.Min(Length-1,(i+1)*128);
            var part = this[range];
            for(int b = 0;b<part.Length;b++)
                yield return part.Span[b];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Fill(T element)
    {
        for(int i = 0;i<Length;i++)
            this[i] = element;
    }
}