using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class Extensions
{
    public static void Fill(this Vector vec,float value){
        for(int i = 0;i<vec.Count;i++)
            vec[i] = value;
    }
    /// <summary>
    /// Finds N minimal elements in O(N log n) time, 
    /// where N is count of elements in collection and n is a number of elements to retrieve
    /// </summary>
    public static List<T> FindNMinimal<T,TMeasure>(this IEnumerable<T> collection,int n,Func<T,TMeasure> getMeasure){
        var queue = new System.Collections.Generic.PriorityQueue<T,TMeasure>(n);
        foreach(var el in collection)
            queue.Enqueue(el,getMeasure(el));
        return Enumerable.Range(0,Math.Min(n,queue.Count)).Select(x=>queue.Dequeue()).ToList();
    }
    /// <summary>
    /// Shuffles array
    /// </summary>
    public static void Shuffle<T>(this T[] array, Random? rng = null)
    {
        rng ??= Random.Shared;
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}