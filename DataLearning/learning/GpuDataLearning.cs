using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public struct GpuData : IData
{
    public ArrayedVector Input;
    Vector IData.Input{
        get=>Input;
        set{
            var len = Math.Min(value.Count,Input.Count);
            for(int i = 0;i<len;i++){
                Input[i] = value[i];
            }
        }
        }
    public GpuData(ArrayedVector input){
        Input = input;
    }
    public IData Clone()
    {
        throw new NotImplementedException();
        // return new(Input.Clone());
    }

    public IData Divide(float scalar)
    {
        throw new NotImplementedException();
    }
}

public class GpuDataSet : DataSet
{
    
    public GpuDataSet(int inputVectorLength, IList<IData>? data = null) : base(inputVectorLength, data)
    {
    }
}

public class GpuDataLearning : IDataLearning
{
    public GpuDataLearning(GpuDataSet dataSet)
    {
        DataSet = dataSet;
    }
    public GpuDataSet DataSet{get;}
    DataSet IDataLearning.DataSet => DataSet;
    public float DiffusionTheta{get;set;} = 0.001f;
    public float DiffusionCoefficient{get;set;} = 2;
    public Vector Diffuse(Vector input)
    {
        throw new NotImplementedException();
    }

    public void DiffuseError(Vector input, Vector error)
    {
        throw new NotImplementedException();
    }

    public Vector DiffuseOnNClosest(Vector input, int n)
    {
        throw new NotImplementedException();
    }

    public (IData data, int id) GetClosest(IData element)
    {
        throw new NotImplementedException();
    }

    public void NormalizeCoordinates(Vector? input = null)
    {
        throw new NotImplementedException();
    }
}