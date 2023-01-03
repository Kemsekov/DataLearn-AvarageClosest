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
}

public class GpuDataSet : IDataSet
{
    public GpuDataSet(int inputVectorLength,IDataAccess<float> storage, IDataAccess<byte> indices)
    {
        this.InputVectorLength = inputVectorLength;
        this.Data = new GpuDataList(storage,indices);
    }
    public int InputVectorLength{get;}
    public GpuDataList Data{get;}
    IList<IData> IDataSet.Data => Data;
}