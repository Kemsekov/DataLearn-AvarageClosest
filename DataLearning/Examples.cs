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
    public static (Vector MinVector, Vector Difference) GetNormalizer(IEnumerable<Vector> data)
    {
        var maxVector = new DenseVector(new float[data.First().Count]);
        var minVector = new DenseVector(new float[data.First().Count]);
        Array.Fill(minVector.AsArray(),float.MaxValue);
        foreach (var d in data)
        {
            for (int b = 0; b < d.Count; b++)
            {
                var value = d[b];
                if(value<-1) continue;
                value = Math.Abs(value);
                maxVector[b] = Math.Max(maxVector[b], value);
                minVector[b] = Math.Min(minVector[b], value);
            }
        }
        return (minVector,maxVector-minVector);
    }
    public static void Normalize(Vector v, (Vector MinVector, Vector Difference) normalizer){
        for(int i = 0;i<v.Count;i++){
            if(v[i]>-1){
                v[i]-=normalizer.MinVector[i];
                v[i]/=normalizer.Difference[i];
            }
        }
    }
    public static void Normalize(IEnumerable<Vector> vectors, (Vector MinVector,Vector Difference) normalizer){
        foreach(var v in vectors)
            Normalize(v,normalizer);
    }
    public static void RestoreToOriginal(Vector vec, (Vector MinVector,Vector Difference) normalizer){
        for(int i = 0;i<vec.Count;i++){
            if(vec[i]>-1){
                vec[i]*=normalizer.Difference[i];
                vec[i]+=normalizer.MinVector[i];
            }
        }
    }
    public static void RestoreToOriginal(IEnumerable<Vector> vec, (Vector MinVector,Vector Difference) normalizer){
        foreach(var c in vec){
            RestoreToOriginal(c,normalizer);
        }
    }
    public static AdaptiveDataSet Run(Vector[] trainData, Vector[] testData, Func<Vector, IData> getInput,int adaptiveDataSetLength, float diffusionTheta = 0.001f, float diffusionCoefficient = 2, bool restoreMissingValues = false)
    {
        var normalizer = GetNormalizer(trainData);
        var inputLength = trainData[0].Count;

        var adaptiveDataSet = new AdaptiveDataSet(inputLength, adaptiveDataSetLength,new VectorMask((x,index)=>x>=-1));
        
        // adaptiveDataSet.Distance = (x1,x2)=>((float)((x1.Input-x2.Input).L2Norm()+(x1.Output-x2.Output).L2Norm()));
        
        adaptiveDataSet.DataLearning.DiffusionCoefficient = diffusionCoefficient;
        adaptiveDataSet.DataLearning.DiffusionTheta = diffusionTheta;
        var watch = new Stopwatch();
        watch.Start();
        var trainOrder = trainData.Select(data=>new{Data = data,MissingColumns=data.Count(x=>x<-1)}).OrderBy(x=>x.MissingColumns);
        foreach(var element in trainOrder)
        {
            var t = element.Data;
            IData d = new Data() { Input = t};
            if(restoreMissingValues && element.MissingColumns>0){
                d.Input = adaptiveDataSet.PredictPure(d.Input);
            }
            adaptiveDataSet.AddByMergingWithClosest(d);
        };
        System.Console.WriteLine($"Added data in {watch.ElapsedMilliseconds} ms");
        ComputeError(testData, adaptiveDataSet, getInput);
        System.Console.WriteLine("-------------");
        return adaptiveDataSet;
    }
    public static void Example1(){
        var file = "datasets/possum.csv";
        System.Console.WriteLine(file);
        var data = LoadCsv(file,(data,index)=>{
            if(data=="Vic") return 1;
            if(data=="other") return 0;
            if(data=="f") return 1;
            if(data=="m") return 0;
            if(data=="NA") return -2;
            return float.Parse(data);
        })
        .ToArray();
        var normalizer = GetNormalizer(data);
        Normalize(data,normalizer);
        data.Shuffle();
        
        var train = data[20..];
        var test = data[..20];

        var input = (Vector v)=>{
            var res = v.Clone();
            res[12] = -2;
            return new Data(res) as IData;
        };
        Run(train,test,input,100,restoreMissingValues:false);
    }
    public static void Example3()
    {
        var file = "datasets/stats.csv";
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
        //first 9 elements are values from height to charisma
        //later N elements are class which this character belongs to
        var totalRaces = 9;
        var data = 
            LoadCsv(file, toData)
            .Select(x=>{
                var res = new DenseVector(totalRaces+x.Count-1);
                res.SetSubVector(0,x.Count-1,x.SubVector(1,x.Count-1));
                var id = (int)x.First()+x.Count-1;
                res[id] = 1;
                return res;
            })
            .ToArray();
        
        data.Shuffle();
        var normalizer = GetNormalizer(data);
        Normalize(data,normalizer);

        var input = (Vector v) =>{
            var clone = v.Clone();
            for(int i = 9;i<v.Count;i++)
                clone[i] = -2;
            return new Data(clone) as IData;
        };
        var test = data[..500];
        var train = data[500..];
        Run(train,test,input,1000,0.0000001f,6);
    }
    public static void Example2()
    {
        var file = "datasets/concrete_data.csv";
        System.Console.WriteLine(file);
        var toData =
        (string x) =>
            {
                if (x != "")
                    return float.Parse(x);
                return -2;
            };
        var data = LoadCsv(file, toData).ToArray();
        data.Shuffle();

        var normalizer = GetNormalizer(data);
        Normalize(data,normalizer);
        var test = data[..100];
        var train = data[100..];

        var input = (Vector v) =>{
            var clone = v.Clone();
            clone[0] = -2;
            return new Data(clone) as IData;
        };

        Run(train,test,input,500);

    }
    public static void Example4(){
        var file = "datasets/kc_house_data.csv";
        System.Console.WriteLine(file);
        var data = LoadCsv(file,(data,index)=>{
            var value = float.Parse(data);
            if(index==9 && value==0) return -2;
            if(index==7 && value==0) return -2;
            return MathF.Abs(value);
        })
        .Take(10000)
        .ToArray();
        var normalizer = GetNormalizer(data);
        Normalize(data,normalizer);
        data.Shuffle();
        var test = data[..1000];
        var train = data[1000..];
        var input = (Vector v)=>{
            var clone = v.Clone();
            clone[15] = -2;
            clone[16] = -2;
            return new Data(clone) as IData;
        };
        var result = Run(train,test,input,500,0.000001f,6,restoreMissingValues:false);
        
    }
    public static void ComputeError(Vector[] test, AdaptiveDataSet dataSet, Func<Vector,IData> getInput)
    {
        var watch = new Stopwatch();
        watch.Start();
        var result = dataSet.ComputePredictionError(test, getInput);
        var missingValuesPercent = 0.2f;
        var restoreError = dataSet.ComputePurePredictionError(test,missingValuesPercent);
        watch.Stop();
        System.Console.WriteLine("Test size : " + test.Length);
        System.Console.WriteLine("Time to compute error : " + watch.ElapsedMilliseconds + " ms");
        System.Console.WriteLine("Error is " + result.Error);
        System.Console.WriteLine("Absolute Error is " + result.AbsError);
        System.Console.WriteLine("MaxError is " + result.MaxError);
        System.Console.WriteLine("Absolute error vector:");
        System.Console.WriteLine("Index\t:\tAbs error");
        // from this output we can clearly see where what values our model predicts best
        // and what values predicts worst
        //index is a value's index we trying to predict
        //value is a mean absolute error our model has while predicting it
        for(int i = 0;i<result.AbsDifference.Count;i++){
            var diff = result.AbsDifference[i];
            if(diff==0) continue;
            System.Console.WriteLine($"{i}\t:\t{diff}");
        }
        System.Console.WriteLine($"Restore error with {missingValuesPercent.ToString("0.00")} missing values is "+restoreError);
    }
}