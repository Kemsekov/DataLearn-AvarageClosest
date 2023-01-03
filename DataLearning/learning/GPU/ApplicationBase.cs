#pragma warning disable
using System;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

public abstract class ApplicationBase : IDataOperation
{
    public Sdl2Window Window;
    public GraphicsDevice GraphicsDevice;
    public DisposeCollectorResourceFactory Factory;
    public ApplicationBase()
    {
        CreateWindowAndGraphicsDevice();
        CreateResourceFactory();
        
    }
    public virtual void InitResources()
    {
        CreateDeviceResources(Factory);
        CreateResourceLayouts(Factory);
        CreatePipelines(Factory);
        UpdateDeviceResources(GraphicsDevice);
        CreateResourceSets(Factory);
    }
    public abstract void CreateResourceSets(DisposeCollectorResourceFactory factory);
    public abstract void CreatePipelines(DisposeCollectorResourceFactory factory);
    public abstract void CreateResourceLayouts(DisposeCollectorResourceFactory factory);
    /// <summary>
    /// Update here : <see cref="DeviceBuffer"/>, <see cref="Texture"/>, <see cref="CommandList"/>
    /// </summary>
    public abstract void UpdateDeviceResources(GraphicsDevice graphicsDevice);
    /// <summary>
    /// Create here : <see cref="DeviceBuffer"/>, <see cref="Texture"/>, <see cref="CommandList"/>, <see cref="Shader"/>
    /// </summary>
    public abstract void CreateDeviceResources(DisposeCollectorResourceFactory factory);
    public virtual void CreateResourceFactory()
    {
        Factory = new DisposeCollectorResourceFactory(GraphicsDevice.ResourceFactory);
    }
    public virtual void CreateWindowAndGraphicsDevice()
    {
        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo
            {
                WindowInitialState = WindowState.Hidden,
            },
            new GraphicsDeviceOptions() { ResourceBindingModel = ResourceBindingModel.Improved },
            out Sdl2Window window,
            out GraphicsDevice gd);
        Window = window;
        GraphicsDevice = gd;
    }

    public virtual void Dispose()
    {
        GraphicsDevice.Dispose();
        Factory.DisposeCollector.DisposeAll();
    }
}