using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Utilities;

public class GpuDataLearningInitialization : ApplicationBase
{
    public DeviceBuffer DataInformationBuffer;
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
        GraphicsDevice.UpdateBuffer(DataInformationBuffer,0,new int[]{maxVectorsCount, vectorSize,0,0});
        GraphicsDevice.UpdateBuffer(IndicesDataBuffer,0,new byte[maxVectorsCount]);
        StorageBuffer = new GpuDataAccess<float>(VectorDataBuffer,GraphicsDevice);
        IndicesBuffer = new GpuDataAccess<byte>(IndicesDataBuffer,GraphicsDevice);
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

        DataInformationBuffer = factory.CreateBuffer(
            new(
                sizeInBytes: (uint)(sizeof(int)*4),
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
                new ResourceLayoutElementDescription("Vectors",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute),
                new ResourceLayoutElementDescription("Indices",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute),
                new ResourceLayoutElementDescription("DataInformation",ResourceKind.UniformBuffer,ShaderStages.Compute)
            )
        );
    }

    public override void CreateResourceSets(DisposeCollectorResourceFactory factory)
    {
        ResourceSet =  factory.CreateResourceSet(
            new ResourceSetDescription(
                Layout,VectorDataBuffer,IndicesDataBuffer,DataInformationBuffer
            )
        );
    }

    public override void UpdateDeviceResources(GraphicsDevice graphicsDevice)
    {
    }
}