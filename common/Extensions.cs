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
}