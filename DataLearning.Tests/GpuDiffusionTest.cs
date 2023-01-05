using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

namespace DataLearning.Tests;
public class GpuDiffusionTest
{
    public GpuDiffusionTest()
    {
        
    }
    [Fact]
    public void Test1(){
        var init = new GpuDataLearningInitialization(5,100);
        var diffuser = new Diffusor(init,Shaders.DiffuseShader);
        var result = diffuser.Compute(new DenseVector(new float[]{0,0,0,0,0}));
        Assert.Equal(result[0],100);
        Assert.Equal(result[1],5);
    }
}