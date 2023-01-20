using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLearning.Tests;

public class GpuDataAccessTests : IDisposable
{
    public GpuDataAccessTests()
    {
        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo
            {
                WindowInitialState = WindowState.Hidden,
            },
            new GraphicsDeviceOptions() { ResourceBindingModel = ResourceBindingModel.Improved },
            out Sdl2Window window,
            out GraphicsDevice gd);
        this.GD = gd;
        this.Factory = new DisposeCollectorResourceFactory(gd.ResourceFactory);
        this.Buffer = Factory.CreateBuffer(
            new(
                sizeInBytes: (uint)(1000*sizeof(float)),
                BufferUsage.StructuredBufferReadWrite,
                structureByteStride: sizeof(float)
            )
        );
        this.DataAccess = new GpuDataAccess<float>(Buffer,gd);
        this.Tests = new DataAccessTests(DataAccess);
    }
    public GraphicsDevice GD { get; }
    public DisposeCollectorResourceFactory Factory { get; }
    public DeviceBuffer Buffer { get; }
    public GpuDataAccess<float> DataAccess;
    public DataAccessTests Tests { get; }
    [Fact]
    public void Enumerable_Works() 
        => Tests.Enumerable_Works();
    [Fact]
    public void HaveRightLength(){
        Assert.Equal(DataAccess.Length,1000);
    }
    [Fact]
    public void OutOfRangeIndexing_Throws()
    {
        Tests.OutOfRangeIndexing_Throws();
    }
    [Fact]
    public void GetByRange_Works(){
        Tests.GetByRange_Works();
    }
    [Fact]
    public void ReadWriteInLegalRange_Works()
    {
        Tests.ReadWriteInLegalRange_Works();
    }

    public void Dispose()
    {
        Factory.DisposeCollector.DisposeAll();
        DataAccess.Dispose();
    }
}