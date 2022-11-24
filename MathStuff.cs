using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class MathStuff
{
    public static List<T> QuickSelect<T>(this IList<T> data,int count,Comparison<T> compare){
        var points = new List<T>(count+1);
        points.Add(data.First());
        for(int i = 0;i<data.Count;i++){
            var t = data[i];
            if(compare(t,points.Last())>0){
                points.Add(t);
                if(points.Count>count)
                    points.RemoveAt(0);
            }
        }
        return points;
    }
    public static float Sigmoid(float x){
        return 1f/(1+MathF.Exp(-x));
    }
}