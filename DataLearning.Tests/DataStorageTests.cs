namespace DataLearning.Tests;

public class DataStorageTests
{
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
        Assert.Equal(d.Length,0);
        Assert.Equal(d.MaxLength,25);
        Assert.Equal(d.Storage.Length,25);
        Assert.Equal(d.Indices.Length,5);
        Assert.True(d.Storage.All(x=>x==0));
        Assert.True(d.Indices.All(x=>x==0));
        Assert.True(Enumerable.Range(0,5).All(x=>d.IsFree(x)));
    }
    [Fact]
    public void Insert_Works(){
        var length = 10;
        var size = 6;
        var d = new DataStorage<float>(length,size);
        var inserted = Enumerable.Range(0,length).Select(x=>RandomArray(size)).ToArray();
        for(int i = 0;i<length;i++){
            var id = d.Insert(inserted[i]);
            Assert.Equal(id,i);
        }
        for(int i = 0;i<length;i++){
            var inserted_ = inserted[i];
            var retrieved = d.Get(i);
            Assert.Equal(retrieved.ToArray(),inserted_.ToArray());
        }

        var mustFailToInsert = RandomArray(size);
        Assert.Equal(-1,d.Insert(mustFailToInsert));
    }

    [Fact]
    public void Free_Works(){
        var length = 10;
        var size = 7;
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
            Assert.Equal(d.Get(i).ToArray(),toInsert);
        }
        Assert.Equal(length,d.Length);

    }
}