using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;
using Veldrid.Utilities;

public unsafe class Diffusor : IDataOperation
{
    public Diffusor(GpuDataLearningInitialization init, string shaderText)
    {
        this.Init = init;
        this.ShaderText = shaderText.Trim();
        InitResources();
        this.InputVectorAccess = new GpuDataAccess<float>(InputVectorBuffer,Init.GraphicsDevice);
    }

    public GpuDataLearningInitialization Init { get; }
    public string ShaderText { get; }
    public DeviceBuffer InputVectorBuffer { get; private set; }
    public DeviceBuffer OutputVectorBuffer { get; private set; }
    public DeviceBuffer AddedCoefficientsBuffer { get; private set; }
    public CommandList CommandList { get; private set; }
    public Shader Shader { get; private set; }
    public ResourceLayout InputVectorLayout { get; private set; }
    public ResourceSet ResourceSet { get; private set; }
    public Pipeline Pipeline { get; private set; }
    public GpuDataAccess<float> InputVectorAccess { get; }

    public void InitResources()
    {
        CreateDeviceResources(Init.Factory);
        CreateResourceLayouts(Init.Factory);
        CreatePipelines(Init.Factory);
        CreateResourceSets(Init.Factory);
    }
    public Vector Compute(Vector input, float DiffusionCoefficient, float DiffusionTheta){
        Init.GraphicsDevice.UpdateBuffer(InputVectorBuffer,0,input.ToArray());
        Init.GraphicsDevice.UpdateBuffer(OutputVectorBuffer,0,new byte[OutputVectorBuffer.SizeInBytes]);
        Init.GraphicsDevice.UpdateBuffer(AddedCoefficientsBuffer,0,new float[4]);
        Init.GraphicsDevice.UpdateBuffer(Init.DataInformationBuffer,2*sizeof(int),new float[]{DiffusionCoefficient,DiffusionTheta});
        
        CommandList.Begin();
        CommandList.SetPipeline(Pipeline);
        CommandList.SetComputeResourceSet(0,Init.ResourceSet);
        CommandList.SetComputeResourceSet(1,ResourceSet);
        CommandList.Dispatch((uint)this.Init.MaxVectorsCount,1,1);
        
        CommandList.End();
        Init.GraphicsDevice.SubmitCommands(CommandList);
        Init.GraphicsDevice.WaitForIdle();
        var result = input.Clone();
        input.MapIndexed((index,value)=>{
            return InputVectorAccess[index];
        },result);
        return (Vector)result;
    }

    public void CreateDeviceResources(DisposeCollectorResourceFactory factory)
    {
        var size = this.Init.VectorSize*sizeof(float);
        size = (int)MathF.Ceiling(size*1.0f/16)*16;
        InputVectorBuffer = factory.CreateBuffer(
            new(
                sizeInBytes:         (uint)(size),
                usage:               BufferUsage.StructuredBufferReadWrite,
                structureByteStride: (uint)sizeof(float)
            )
        );
        OutputVectorBuffer = factory.CreateBuffer(
            new(
                sizeInBytes:         (uint)(size),
                usage:               BufferUsage.StructuredBufferReadWrite,
                structureByteStride: (uint)sizeof(float)
            )
        );
        AddedCoefficientsBuffer = factory.CreateBuffer(
            new(
                sizeInBytes:         16,
                usage:               BufferUsage.StructuredBufferReadWrite,
                structureByteStride: (uint)sizeof(float)
            )
        );
        CommandList = factory.CreateCommandList(new CommandListDescription());
        Shader = factory.CreateFromSpirv(
            new ShaderDescription(
                ShaderStages.Compute,
                Encoding.UTF8.GetBytes(ShaderText),
                "main"
        ));
    }

    public void CreatePipelines(DisposeCollectorResourceFactory factory)
    {
        var pipeDesc = new ComputePipelineDescription(Shader,new[]{Init.Layout,InputVectorLayout},1,1,1);
        Pipeline = factory.CreateComputePipeline(ref pipeDesc);
    }

    public void CreateResourceLayouts(DisposeCollectorResourceFactory factory)
    {
        InputVectorLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Input",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute),
                new ResourceLayoutElementDescription("Output",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute),
                new ResourceLayoutElementDescription("AddedCoefficients",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute)
            )
        );
    }

    public void CreateResourceSets(DisposeCollectorResourceFactory factory)
    {
        ResourceSet =  factory.CreateResourceSet(
            new ResourceSetDescription(
                InputVectorLayout,InputVectorBuffer, OutputVectorBuffer, AddedCoefficientsBuffer
            )
        );
    }
}