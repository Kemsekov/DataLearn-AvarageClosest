using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLearning.Tests;
public class ArrayDataAccessTests
{
    private IDataAccess<float>  DataAccess;
    public DataAccessTests Tests { get; }
    public ArrayDataAccessTests()
    {
        var array = new float[1000];
        this.DataAccess = new ArrayDataAccess<float>(array);
        this.Tests = new DataAccessTests(DataAccess);
    }
    [Fact]
    public void Enumerable_Works() 
        => Tests.Enumerable_Works();
    [Fact]
    public void HaveRightLength(){
        Assert.Equal(DataAccess.Length,1000);
    }
    [Fact]
    public void OutOfRangeIndexing_Throws()
    {
        Tests.OutOfRangeIndexing_Throws();
    }
    [Fact]
    public void GetByRange_Works(){
        Tests.GetByRange_Works();
    }
    [Fact]
    public void ReadWriteInLegalRange_Works()
    {
        Tests.ReadWriteInLegalRange_Works();
    }
}