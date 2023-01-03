
// TODO: add tests for it
//Test every single method and property(except for Storage and Indices)
public class ArrayDataAccess<T> : IDataAccess<T>
{
    public ArrayDataAccess(T[] array)
    {
        this.Array = array;
    }
    public T this[int index]{get=>Array[index];set=>Array[index]=value;}

    public T[] this[Range range] => Array[range];

    public int Length => Array.Length;

    public T[] Array { get; }

    public void Fill(T element)
    {
        Array.AsSpan().Fill(element);
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach(var a in Array)
            yield return a;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Array.GetEnumerator();
    }
}
