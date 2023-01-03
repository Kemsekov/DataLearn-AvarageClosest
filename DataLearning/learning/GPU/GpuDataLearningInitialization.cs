using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Utilities;

public class GpuDataLearningInitialization : ApplicationBase
{
    public DeviceBuffer MaxVectorsCountBuffer;
    public DeviceBuffer VectorSizeBuffer;
    public DeviceBuffer VectorDataBuffer;
    public DeviceBuffer IndicesDataBuffer;
    public CommandList CommandList;
    public int MaxVectorsCount { get; }
    public int VectorSize { get; }
    public ResourceLayout Layout { get; private set; }
    public ResourceSet ResourceSet { get; private set; }
    public IDataAccess<float> StorageBuffer;
    public IDataAccess<byte> IndicesBuffer;

    #pragma warning disable
    public GpuDataLearningInitialization(int vectorSize,int maxVectorsCount) : base()
    {
        this.MaxVectorsCount = maxVectorsCount;
        this.VectorSize = vectorSize;
        InitResources();
        GraphicsDevice.UpdateBuffer(MaxVectorsCountBuffer,0,maxVectorsCount);
        GraphicsDevice.UpdateBuffer(VectorSizeBuffer,0,vectorSize);
        StorageBuffer = new GpuDataAccess<float>(VectorDataBuffer,GraphicsDevice,Factory);
        IndicesBuffer = new GpuDataAccess<byte>(IndicesDataBuffer,GraphicsDevice,Factory);
    }
    #pragma warning enable
    
    public override void CreateDeviceResources(DisposeCollectorResourceFactory factory)
    {
        var vectorsInputSize = VectorSize*MaxVectorsCount*sizeof(float);
        vectorsInputSize=(int)MathF.Ceiling(vectorsInputSize*1.0f/16)*16;
        
        var indicesInputSize = MaxVectorsCount*sizeof(byte);
        indicesInputSize = (int)MathF.Ceiling(indicesInputSize*1.0f/16)*16;

        VectorDataBuffer = factory.CreateBuffer(
            new(
                sizeInBytes:         (uint)(vectorsInputSize),
                usage:               BufferUsage.StructuredBufferReadWrite,
                structureByteStride: (uint)sizeof(float)
            )
        );

        IndicesDataBuffer = factory.CreateBuffer(
            new(
                sizeInBytes:            (uint)indicesInputSize,
                usage:                  BufferUsage.StructuredBufferReadWrite,
                structureByteStride :   (uint)sizeof(int)
            )
        );

        MaxVectorsCountBuffer = factory.CreateBuffer(
            new(
                sizeInBytes: sizeof(int),
                BufferUsage.UniformBuffer
            )
        );
        VectorSizeBuffer = factory.CreateBuffer(
            new(
                sizeInBytes: sizeof(int),
                BufferUsage.UniformBuffer
            )
        );
        CommandList = factory.CreateCommandList(new CommandListDescription());
    }

    public override void CreatePipelines(DisposeCollectorResourceFactory factory)
    {
    }

    public override void CreateResourceLayouts(DisposeCollectorResourceFactory factory)
    {
        Layout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("VectorsBuffer",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute),
                new ResourceLayoutElementDescription("IndicesBuffer",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute)
            )
        );
    }

    public override void CreateResourceSets(DisposeCollectorResourceFactory factory)
    {
        ResourceSet =  factory.CreateResourceSet(
            new ResourceSetDescription(
                Layout,VectorDataBuffer,IndicesDataBuffer
            )
        );
    }

    public override void UpdateDeviceResources(GraphicsDevice graphicsDevice)
    {
    }
}