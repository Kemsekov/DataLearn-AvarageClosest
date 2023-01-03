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
    public Diffusor(GpuDataLearningInitialization init, string pathToShader)
    {
        this.Init = init;
        this.ShaderText = File.ReadAllText(pathToShader).Trim();
    }

    public GpuDataLearningInitialization Init { get; }
    public string ShaderText { get; }
    public DeviceBuffer InputVectorBuffer { get; private set; }
    public CommandList CommandList { get; private set; }
    public Shader Shader { get; private set; }
    public ResourceLayout InputVectorLayout { get; private set; }
    public ResourceSet ResourceSet { get; private set; }
    public Pipeline Pipeline { get; private set; }

    public Vector Compute(Vector input){
        Init.GraphicsDevice.UpdateBuffer(InputVectorBuffer,0,input.ToArray());
        CommandList.Begin();
        CommandList.SetPipeline(Pipeline);
        CommandList.SetComputeResourceSet(0,Init.ResourceSet);
        CommandList.SetComputeResourceSet(1,ResourceSet);
        CommandList.Dispatch((uint)this.Init.MaxVectorsCount,1,1);
        
        CommandList.End();
        Init.GraphicsDevice.SubmitCommands(CommandList);
        Init.GraphicsDevice.WaitForIdle();
        var map = Init.GraphicsDevice.Map(InputVectorBuffer,MapMode.Read);
        var ptr = (float*)map.Data;
        var result = input.Clone();
        input.MapIndexed((index,value)=>{
            return ptr[index];
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
                new ResourceLayoutElementDescription("InputVector",ResourceKind.StructuredBufferReadWrite,ShaderStages.Compute)
            )
        );
    }

    public void CreateResourceSets(DisposeCollectorResourceFactory factory)
    {
        ResourceSet =  factory.CreateResourceSet(
            new ResourceSetDescription(
                InputVectorLayout,InputVectorBuffer
            )
        );
    }
}