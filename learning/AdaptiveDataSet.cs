using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

/// <summary>
/// Adaptive data set that have constant amount of elements and
/// changes over time as new elements added by merging old data with new one.
/// It basically limit your model size, meanwhile trying to retain ability to learn
/// </summary>
public class AdaptiveDataSet
{
    public DataSet DataSet { get; }
    public int MaxElements { get; }
    public DataLearning DataLearning { get; }
    public int InputVectorLength => DataSet.InputVectorLength;
    /// <summary>
    /// Function that used to calculate distance between two data elements. Used for adding new elements.
    /// </summary>
    public Func<IData, IData, float> Distance { get; set; }
    public AdaptiveDataSet(DataSet dataSet, DataLearning dataLearning, int maxElements)
    {
        this.Distance = (x1,x2)=>(float)(x1.Input-x2.Input).L2Norm();
        this.DataSet = dataSet;
        this.MaxElements = maxElements;
        this.DataLearning = dataLearning;
    }
    public AdaptiveDataSet(int inputVectorLength, int maxElements) : this(new(inputVectorLength),new DataLearning(),maxElements)
    {
    }
    (IData data, int id) GetClosest(IData element, Func<IData, IData, float> distance)
    {
        int minId = 0;
        float minDist = float.MaxValue;
        IData? closest = null;
        Parallel.For(0,DataSet.Data.Count,i=>
        {
            var x = DataSet.Data[i];
            var dist = Distance(x, element);
            lock(DataSet)
            if (dist < minDist)
            {
                minId = i;
                minDist = dist;
                closest = x;
            }
        });
        if (closest is null)
            throw new ArgumentException("There is no data in dataset");
        return (closest, minId);
    }
    /// <summary>
    /// Restores input vector with missing values by predicting them <br/>
    /// Value is missing if it is less than -1 <br/>
    /// For example (1,0.5,0.3,-2,-2,0.2) is a vector where values with index 3 and 4 are missing <br/>
    /// And when restored only missing values will be filled by prediction
    /// </summary>
    /// <returns>Restored input vector</returns>
    public Vector Predict(Vector input)
    {
        if(DataSet.Data.Count==0) return input;
        var result = DataLearning.Diffuse(DataSet, input);
        foreach (var e in input.Select((value, index) => (value, index)).Where(x => x.value >= -1))
        {
            result[e.index] = e.value;
        }
        return result;
    }
    /// <summary>
    /// Computes 'pure' prediction for input. <br/>
    /// In such case not only missing values will be filled, but returning value is what
    /// model thinks should be at the exact position given, so this method
    /// can be used to find anomaly among data
    /// </summary>
    /// <returns>
    /// Model pure prediction. 
    /// When input is good and model is well-fitted for it, result will not much differ from given
    /// input(except for all missing values will be restored by prediction). <br/>
    /// But when given input is really out of what model predicts it will produce something completely different.
    /// </returns>
    public Vector PurePrediction(Vector input)
    {
        return DataLearning.Diffuse(DataSet, input);
    }
    /// <summary>
    /// For given element, finds another closest by input vector element
    /// and average their input and output vectors.<br/>
    /// Replaces closest found element.
    /// </summary>
    public void AddByMergingWithClosest(IData element)
    {
        if (DataSet.Data.Count < MaxElements)
        {
            DataSet.Data.Add(element);
            return;
        }
        var toReplace = GetClosest(element, Distance);
        element.Input = (Vector)(element.Input + toReplace.data.Input).Divide(2);

        DataSet.Data[toReplace.id] = element;
    }
    Vector CreateMissingValues(Vector vec, float percentOfMissingValues = 0.2f){
        if(vec.Any(x=>x<-1)) return vec;
        var r = new Random();
        var res = vec.Clone();
        for(int i = 0;i<vec.Count;i++){
            if(r.NextSingle()<percentOfMissingValues)
                res[i] = -2;
        }
        return (Vector)res;
    }
    /// <summary>
    /// Computes model prediction error
    /// </summary>
    /// <param name="test">Test data</param>
    /// <param name="getInput">How to get input from data. Note that input must contain missing values(less than -1) for model to do anything useful with it</param>
    /// <returns></returns>
    public TestPrediction ComputePredictionError(Vector[] test, Func<Vector,IData> getInput)
    {
        Vector difference = new DenseVector(new float[InputVectorLength]);
        Vector absDifference = new DenseVector(new float[InputVectorLength]);
        Vector maxDifference = new DenseVector(new float[InputVectorLength]);
        foreach (var t in test)
        {
            var actual = t;
            var input = getInput(actual);

            var prediction = Predict(input.Input);

            //because some of test data can have missing values itself
            //we cannot measure difference between missing values so
            //any prediction on these missing values is invalid
            //and while computing difference we just ignore them
            var diff = actual.Clone();
            for (int i = 0; i < diff.Count; i++)
            {
                if (actual[i] < -1)
                    diff[i] = 0;
                else
                    diff[i] = actual[i] - prediction[i];
            }
            if (maxDifference.PointwiseAbs().Sum() < diff.PointwiseAbs().Sum())
                maxDifference = (Vector)diff;
            difference = (Vector)(difference + diff);
            absDifference = (Vector)(absDifference + diff.PointwiseAbs());
            // PrintDiff(diff);
        };
        difference = (Vector)difference.Divide(test.Count());
        absDifference = (Vector)absDifference.Divide(test.Count());
        return new(difference,absDifference,maxDifference);
    }
    /// <summary>
    /// Diffuses <paramref name="dataSet"/> on <paramref name="Approximation"/> dataset.
    /// </summary>
    public void Diffuse(DataSet Approximation,Func<Vector,Vector> getInput)
    {
        if (DataSet.Data.Count == 0) return;
        Parallel.For(0, Approximation.Data.Count, (i, _) =>
        {
            var approximation = Approximation.Data[i];
            var input = getInput(approximation.Input);
            approximation.Input = Predict(input);
            Approximation.Data[i] = approximation;
        });
    }
    /// <summary>
    /// Pretty prints difference vector
    /// </summary>
    /// <param name="diff"></param>
    static void PrintDiff(Vector<float> diff)
    {
        diff.Map(x => x < 0.01 ? 0 : x, diff);
        bool printed = false;
        for(int i = 0;i<diff.Count;i++){
            var d = diff[i];
            if(d==0) continue;
            printed = true;
            System.Console.WriteLine($"{i}\t:\t{d}");
        }
        if(printed)
            System.Console.WriteLine("---------");
    }

    public float ComputePurePredictionError(Vector[] test, float percentOfMissingValues = 0.2f)
    {
        var missingValuesError = 0f;
        foreach (var t in test)
        {
            if (t.Count(x => x < -1) > 0) continue;
            var input = t;
            var inputWithMissingValues = (Vector)input.Clone();
            input.Map(x =>
            {
                if (Random.Shared.NextSingle() < percentOfMissingValues)
                    return -2;
                return x;
            }, inputWithMissingValues);
            var restoredByVector = PurePrediction(inputWithMissingValues);
            var diff = (input - restoredByVector);
            missingValuesError += ((float)diff.L2Norm());
        }
        return missingValuesError / test.Length;
    }
}