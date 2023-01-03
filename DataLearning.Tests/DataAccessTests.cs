using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLearning.Tests;

public class DataAccessTests : IClassFixture<IDataAccess<float>>
{
    public DataAccessTests(IDataAccess<float> dataAccess)
    {
        DataAccess = dataAccess;
    }
    public IDataAccess<float> DataAccess { get; }
    public void OutOfRangeIndexing_Throws(){
        #pragma warning disable
        Assert.Throws<IndexOutOfRangeException>(()=>{
            DataAccess[-1] = 0;
        });
        Assert.Throws<IndexOutOfRangeException>(()=>{
            var t = DataAccess[-1];
        });
        Assert.Throws<IndexOutOfRangeException>(()=>{
            DataAccess[DataAccess.Length] = 0;
        });
        Assert.Throws<IndexOutOfRangeException>(()=>{
            var t = DataAccess[DataAccess.Length];
        });
        Assert.Throws<IndexOutOfRangeException>(()=>{
            DataAccess[DataAccess.Length+Random.Shared.Next(10)] = default;
        });
        Assert.Throws<IndexOutOfRangeException>(()=>{
            var t = DataAccess[DataAccess.Length+Random.Shared.Next(10)];
        });
        #pragma warning enable
    }
    public void ReadWriteInLegalRange_Works(){
        for(var i = 0;i<DataAccess.Length;i++){
            var value = DataAccess[i];
            var newValue = value;
            while(newValue==value)
                newValue = Random.Shared.NextSingle();
            DataAccess[i] = newValue;
            var updatedValue = DataAccess[i];
            Assert.Equal(newValue,updatedValue);
        }
    }
    void FillWithRandom(){
        for(int i = 0;i<DataAccess.Length;i++){
            DataAccess[i] = Random.Shared.NextSingle();
        }
    }
    public void GetByRange_Works(){
        if(DataAccess.Length<2) return;
        FillWithRandom();
        for(int i = 0;i<20;i++){
            var id1 = Random.Shared.Next(DataAccess.Length);
            var id2 = id1;
            while(id2==id1)
                id2 = Random.Shared.Next(DataAccess.Length);
            (id1,id2) = (Math.Min(id1,id2),Math.Max(id1,id2));
            var range = DataAccess[id1..id2];
            Assert.Equal(id2-id1,range.Length);
            for(int k = 0;k<range.Length;k++){
                Assert.Equal(DataAccess[k+id1],range[k]);
            }
        }
    }
    public void Fill_Works(){
        var value = Random.Shared.NextSingle();
        DataAccess.Fill(value);
        for(int i = 0;i<DataAccess.Length;i++)
            Assert.Equal(value,DataAccess[i]);
    }
}