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
    public static IEnumerable<Vector> LoadCsv(string path, Func<string, int, float> toData)
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
                .Select((x, index) => toData(x, index))
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
    public static void Normalize(Vector v, Vector normalizer){
        for(int i = 0;i<v.Count;i++){
            if(v[i]>-1)
                v[i]/=normalizer[i];
        }
    }
    public static void Normalize(IEnumerable<Vector> vectors, Vector normalizer){
        foreach(var v in vectors)
            Normalize(v,normalizer);
    }
    public static void Modify(Vector[] data, Func<Vector, Vector> modification)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] = modification(data[i]);
    }

    public static void Run(Vector[] trainData, Vector[] testData, Func<Vector, Vector> input, Func<Vector, Vector> output,int adaptiveDataSetLength, float diffusionTheta = 0.001f, float diffusionCoefficient = 2)
    {

        var normalizer = GetNormalizer(trainData);
        var inputLength = input(trainData[0]).Count;
        var outputLength = output(trainData[0]).Count;

        var adaptiveDataSet = new AdaptiveDataSet(inputLength, outputLength, adaptiveDataSetLength);
        
        // adaptiveDataSet.Distance = (x1,x2)=>((float)((x1.Input-x2.Input).L2Norm()+(x1.Output-x2.Output).L2Norm()));
        
        adaptiveDataSet.DataLearning.DiffusionCoefficient = diffusionCoefficient;
        adaptiveDataSet.DataLearning.DiffusionTheta = diffusionTheta;
        var watch = new Stopwatch();
        watch.Start();
        var trainOrder = trainData.Select(data=>new{Data = data,MissingColumns=data.Count(x=>x<-1)}).OrderBy(x=>x.MissingColumns);
        foreach(var element in trainOrder)
        {
            var t = element.Data;
            IData d = new Data() { Input = input(t), Output = output(t) };
            if(element.MissingColumns>0){
                d = adaptiveDataSet.Restore(d);
            }
            adaptiveDataSet.AddByMergingWithClosest(d);
        };
        System.Console.WriteLine($"Added data in {watch.ElapsedMilliseconds} ms");
        ComputeError(testData, adaptiveDataSet, v => new() { Input = input(v), Output = output(v) });
        System.Console.WriteLine("-------------");
    }
    public static void Example1(){
        var file = "possum.csv";
        System.Console.WriteLine(file);
        var data = LoadCsv(file,(data,index)=>{
            if(data=="Vic") return 1;
            if(data=="other") return 0;
            if(data=="f") return 1;
            if(data=="m") return 0;
            if(data=="NA") return -2;
            return float.Parse(data);
        })
        .Select(x=>(Vector)x.SubVector(1,13))
        .ToArray();
        var normalizer = GetNormalizer(data);
        Normalize(data,normalizer);
        Shuffle(Random.Shared,data);
        
        var train = data[20..];
        var test = data[..20];

        var input = (Vector v)=>(Vector)v.SubVector(0,11);
        var output = (Vector v)=>(Vector)v.SubVector(11,2);
        Run(train,test,input,output,30);
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
        var normalizer = GetNormalizer(data);
        Normalize(data,normalizer);
        Shuffle(new Random(), data);

        var input = (Vector v) => (Vector)v.SubVector(0, 7);
        var output = (Vector v) =>
        {
            var vec = new SparseVector(races);
            var id = (int)v.First();
            vec[id] = 1;
            return vec;
        };
        var test = data[..500];
        var train = data[500..];
        Run(train,test,input,output,1000,0.000000001f,8);
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
        Normalize(data,normalizer);
        var test = data[..100];
        var train = data[100..];

        var input = (Vector v) => (Vector)v.SubVector(1, 8);
        var output = (Vector v) => (Vector)v.SubVector(0, 1);

        Run(train,test,input,output,500);

    }
    public static void ComputeError(Vector[] test, AdaptiveDataSet dataSet, Func<Vector, Data> getData)
    {

        var watch = new Stopwatch();
        watch.Start();
        var result = dataSet.ComputePredictionError(test, getData);
        var missingValuesPercent = 0.2f;
        var restoreError = dataSet.ComputeRestoreError(test,getData,missingValuesPercent);
        watch.Stop();
        System.Console.WriteLine("Test size : " + test.Length);
        System.Console.WriteLine("Time to compute error : " + watch.ElapsedMilliseconds + " ms");
        System.Console.WriteLine("Error is " + result.error);
        System.Console.WriteLine("Absolute Error is " + result.absError);
        System.Console.WriteLine("MaxError is " + result.maxError);
        System.Console.WriteLine($"Restore error with {missingValuesPercent.ToString("0.00")} missing values is "+restoreError);
    }
}