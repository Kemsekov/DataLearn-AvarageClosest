using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

namespace DataLearning.Tests;
public class RandomTests
{
    [Fact]
    public void Test()
    {
        void FillWithRandom(Vector vec)
        {
            var r = Random.Shared;
            for (int i = 0; i < vec.Count; i++)
            {
                vec[i] = r.NextSingle();
            }
        }

        var dataStorage = new DataStorage<float>(5, 5);
        var vec1 = new ArrayedVector(dataStorage);
        var vec2 = new ArrayedVector(dataStorage);
        var vec3 = new ArrayedVector(dataStorage);
        FillWithRandom(vec1);
        FillWithRandom(vec2);
        FillWithRandom(vec3);
        System.Console.WriteLine("???");
    }
}