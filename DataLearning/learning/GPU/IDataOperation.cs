using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Veldrid.Utilities;

public interface IDataOperation
{
    void CreateDeviceResources(DisposeCollectorResourceFactory factory);
    void CreatePipelines(DisposeCollectorResourceFactory factory);
    void CreateResourceLayouts(DisposeCollectorResourceFactory factory);
    void CreateResourceSets(DisposeCollectorResourceFactory factory);
    
}