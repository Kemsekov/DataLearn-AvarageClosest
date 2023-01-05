namespace DataLearning.Tests;

public class DataStorageTests
{
    public int Length { get; }
    public int Size { get; }

    public DataStorageTests()
    {
        this.Length = 100;
        this.Size = 15;
    }
    float[] RandomArray(int elementSize){
        var result = new float[elementSize];
        var r = Random.Shared;
        for(int i = 0;i<elementSize;i++){
            result[i] = r.NextSingle();
        }
        return result;
    }
    DataStorage<float> GetFilledDataStorage(int length, int elementSize){
        var d = new DataStorage<float>(length,elementSize);
        var inserted = Enumerable.Range(0,length).Select(x=>RandomArray(elementSize)).ToArray();
        for(int i = 0;i<length;i++){
            d.Insert(inserted[i]);
        }
        return d;
    }
    [Fact]
    public void Constructor1_Works()
    {
        var d = new DataStorage<float>(5,5);
        Assert.Equal(0,d.Length);
        Assert.Equal(5,d.ElementSize);
        Assert.Equal(5,d.MaxLength);
        Assert.Equal(25,d.Storage.Length);
        Assert.Equal(5,d.Indices.Length);
        Assert.True(d.Storage.All(x=>x==0));
        Assert.True(d.Indices.All(x=>x==0));
        Assert.True(Enumerable.Range(0,5).All(x=>d.IsFree(x)));
    }
    [Fact]
    public void Constructor2_Works(){
        var valueStorage = new ArrayDataAccess<float>(new float[100]);
        var indicesStorage = new ArrayDataAccess<byte>(new byte[20]);
        var d = new DataStorage<float>(valueStorage,indicesStorage);
        Assert.Equal(d.Storage,valueStorage);
        Assert.Equal(d.Indices,indicesStorage);
        Assert.Equal(d.ElementSize,5);
        Assert.True(d.Storage.All(x=>x==0));
        Assert.True(d.Indices.All(x=>x==0));
        Assert.True(Enumerable.Range(0,20).All(x=>d.IsFree(x)));
    }
    [Fact]
    public void Insert_Works(){
        var length = 100;
        var size = 15;
        var d = new DataStorage<float>(length,size);
        var inserted = Enumerable.Range(0,length).Select(x=>RandomArray(size)).ToArray();
        for(int i = 0;i<length;i++){
            var id = d.Insert(inserted[i]);
            Assert.Equal(id,i);
        }
        for(int i = 0;i<length;i++){
            var inserted_ = inserted[i];
            var retrieved = d[i];
            Assert.Equal(retrieved.ToArray(),inserted_.ToArray());
        }

        var mustFailToInsert = RandomArray(size);
        Assert.Equal(-1,d.Insert(mustFailToInsert));
    }
    [Fact]
    public void Free_Works(){
        var length = 100;
        var size = 70;
        var d = GetFilledDataStorage(length,size);
        Assert.Equal(length,d.Length);
        Assert.Equal(d.MaxLength,d.Length);
        var toReplace = new int[]{5,3,6};
        foreach(var i in toReplace){
            d.Free(i);
            Assert.True(d.IsFree(i));
        }
        Assert.Equal(length-toReplace.Length,d.Length);

        foreach(var i in toReplace.Order()){
            var toInsert = RandomArray(size);
            Assert.Equal(i,d.Insert(toInsert));
            Assert.Equal(d[i].ToArray(),toInsert);
        }
        Assert.Equal(length,d.Length);

    }
    [Fact]
    public void Set_Works(){
        var d = GetFilledDataStorage(Length,Size);
        for(int i = 0;i<100;i++){
            var randIndex = Random.Shared.Next(Length);
            var randPosition = Random.Shared.Next(Size);
            var before = d[randIndex][randPosition];
            var after = Random.Shared.NextSingle();
            d.Set(randIndex,randPosition,after);
            Assert.Equal(after,d[randIndex][randPosition]);
        }
        
    }
}