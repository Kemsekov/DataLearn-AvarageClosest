using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;
using Microsoft.VisualBasic.FileIO;

public class DataLearnerHelper
{
    public DataLearnerHelper()
    {

    }
}

public class Examples
{
    public static void Shuffle<T>(Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
    public static IEnumerable<Vector> LoadCsv(string path, Func<string, float> toData)
    {
        using var file = File.OpenRead(path);
        using var reader = new StreamReader(file);
        //we skip names for columns
        reader.ReadLine();
        bool end = false;
        while (!end)
        {
            //Processing row
            var splitted = reader.ReadLine()?.Split(",") ?? Enumerable.Empty<string>();
            var row =
                splitted
                .Select(x => toData(x))
                .ToArray();
            if (row.Length == 0) break;
            yield return new DenseVector(row);
        }
    }

    public static Vector GetNormalizer(IEnumerable<Vector> data)
    {
        var normalizer = new DenseVector(new float[data.First().Count]);
        foreach (var d in data)
        {
            for (int b = 0; b < d.Count; b++)
            {
                normalizer[b] = Math.Max(normalizer[b], Math.Abs(d[b]));
            }
        }
        return normalizer;
    }
    public static void Modify(Vector[] data, Func<Vector,Vector> modification){
        for(int i = 0;i<data.Length;i++)
            data[i] = modification(data[i]);
    }
    
    public static void Example3()
    {
        var file = "stats.csv";
        System.Console.WriteLine(file);
        var race = new Dictionary<string, float>();
        int races = 0;
        var toData =
         (string x) =>
         {
             if (float.TryParse(x, out var result)) return result;
             if (!race.ContainsKey(x))
                 race[x] = races++;
             return race[x];
         };
        var data = LoadCsv(file, toData).ToArray();
        Shuffle(new Random(), data);

        var normalizer = GetNormalizer(data);
        var input = (Vector v) => (Vector)v.PointwiseDivide(normalizer).SubVector(0, 7);
        var output = (Vector v) => {
               var vec = new DenseVector(new float[races]);
               var id = (int)v.First();
               vec[id] = 1;
               return vec;
           };
        var inputLength = input(data[0]).Count;
        var outputLength = output(data[0]).Count;

        var test = data[..500];
        var train = data[500..];

        var adaptiveDataSet = new AdaptiveDataSet(inputLength, outputLength, 1000);
        adaptiveDataSet.DataLearning.DiffusionCoefficient=8;
        adaptiveDataSet.DataLearning.DiffusionTheta=0.000000001f;
        var watch = new Stopwatch();
        watch.Start();
        foreach (var t in train)
        {
            var d = new Data() { Input = input(t), Output = output(t) };
            adaptiveDataSet.AddByMergingWithClosest(d);
        }
        System.Console.WriteLine($"Added data in {watch.ElapsedMilliseconds} ms");
        (var error, var absError, var maxError) = ComputeError(test, adaptiveDataSet, v => new() { Input = input(v), Output = output(v) });
        System.Console.WriteLine("Error is " + error);
        System.Console.WriteLine("Absolute Error is " + absError);
        System.Console.WriteLine("MaxError is " + maxError);
    }
    public static void Example2()
    {
        var file = "concrete_data.csv";
        System.Console.WriteLine(file);
        var toData =
        (string x) =>
            {
                if (x != "")
                    return float.Parse(x);
                return 0;
            };
        var data = LoadCsv(file, toData).ToArray();
        Shuffle(new Random(), data);

        var normalizer = GetNormalizer(data);

        var test = data[..100];
        var train = data[100..];

        var input = (Vector v) => (Vector)v.PointwiseDivide(normalizer).SubVector(1, 8);
        var output = (Vector v) => (Vector)v.PointwiseDivide(normalizer).SubVector(0, 1);

        var inputLength = input(data[0]).Count;
        var outputLength = output(data[0]).Count;

        var adaptiveDataSet = new AdaptiveDataSet(inputLength, outputLength, 1000);

        foreach (var t in train)
        {
            var d = new Data() { Input = input(t), Output = output(t) };
            // dataSet.Data.Add(d);
            adaptiveDataSet.AddByMergingWithClosest(d);
            // adaptiveDataSet.AddByMergingAverageWithPrediction(d);
            // adaptiveDataSet.AddByReplacingClosest(d);
        }
        (var error, var absError, var maxError) = ComputeError(test, adaptiveDataSet, v => new() { Input = input(v), Output = output(v) });
        System.Console.WriteLine("Error is " + error);
        System.Console.WriteLine("Absolute Error is " + absError);
        System.Console.WriteLine("MaxError is " + maxError);
    }
    public static (float error, float absError, float maxError) ComputeError(IEnumerable<Vector> test, AdaptiveDataSet dataSet, Func<Vector, Data> getData)
    {

        var watch = new Stopwatch();
        watch.Start();
        var result = dataSet.ComputeError(test, getData);
        System.Console.WriteLine("Test size : " + test.Count());
        System.Console.WriteLine("Time to compute error : " + watch.ElapsedMilliseconds + " ms");
        return result;
    }
}