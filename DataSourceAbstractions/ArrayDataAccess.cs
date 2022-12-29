
// TODO: add tests for it
//Test every single method and property(except for Storage and Indices)
public class ArrayDataAccess<T> : IDataAccess<T>
{
    public ArrayDataAccess(T[] array)
    {
        this.Array = array;
    }
    public T this[int index]{get=>Array[index];set=>Array[index]=value;}

    public int Length => Array.Length;

    public T[] Array { get; }

    public Span<T> AsSpan(Range range)
    {
        return Array.AsSpan(range);
    }
}
