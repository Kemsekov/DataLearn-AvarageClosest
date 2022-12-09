using System;
using System.Collections.Generic;
using System.Linq;
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
    public int OutputVectorLength => DataSet.OutputVectorLength;
    /// <summary>
    /// Function that used to calculate distance between two data elements.
    /// </summary>
    public Func<IData, IData, float> Distance { get; set; }
    public AdaptiveDataSet(DataSet dataSet, DataLearning dataLearning, int maxElements)
    {
        Distance = (a, b) => ((float)(a.Input - b.Input).L2Norm());
        this.DataSet = dataSet;
        this.MaxElements = maxElements;
        this.DataLearning = dataLearning;
    }
    public AdaptiveDataSet(int inputVectorLength, int outputVectorLength, int maxElements)
    {
        Distance = (a, b) => ((float)(a.Input - b.Input).L2Norm());
        this.DataSet = new DataSet(inputVectorLength, outputVectorLength);
        this.MaxElements = maxElements;
        this.DataLearning = new DataLearning();
    }
    (IData data, int id) GetClosest(IData element, Func<IData, IData, float> distance)
    {
        int minId = 0;
        float minDist = float.MaxValue;
        IData? closest = null;
        for (int i = 0; i < DataSet.Data.Count; i++)
        {
            var x = DataSet.Data[i];
            var dist = Distance(x, element);
            if (dist < minDist)
            {
                minId = i;
                minDist = dist;
                closest = x;
            }
        }
        if (closest is null)
            throw new ArgumentException("There is no data in dataset");
        return (closest, minId);
    }
    (IData data, int id) GetRandom(Random? rand = null)
    {
        var id = GetRandomId(rand);
        return (DataSet.Data[id], id);
    }
    int GetRandomId(Random? rand = null)
    {
        rand ??= Random.Shared;
        var count = DataSet.Data.Count;
        var id = rand.Next(count);
        return id;
    }
    /// <summary>
    /// Restores input vector with missing values. <br/>
    /// Value is missing it is less than -1 <br/>
    /// For example (1,0.5,0.3,-2,-2,0.2) is a vector where values with index 3 and 4 are missing <br/>
    /// And when restored only missing values will be filled
    /// </summary>
    /// <returns>Restored vector</returns>
    public Vector Restore(Vector input)
    {
        var length = input.Count;
        var result = DataLearning.Diffuse(DataSet, input, getOutput: x => x.Input, inputDistance: (x, y) =>
        {
            float res = 0;
            float holder = 0;
            for (int i = 0; i < length; i++)
            {
                if (input[i] < -1) continue;
                holder = (x[i] - y[i]);
                res += holder * holder;
            }
            return MathF.Sqrt(res);
        });
        foreach (var e in input.Select((value, index) => (value, index)).Where(x => x.value > -1))
        {
            result[e.index] = e.value;
        }
        return result;
    }
    public Vector Predict(Vector input)
    {
        return DataLearning.Diffuse(DataSet, input);
    }

    /// <summary>
    /// Restores missing data input/output values.<br/>
    /// Value is missing it is less than -1 <br/>
    /// For example (1,0.5,0.3,-2,-2,0.2) is a vector where values with index 3 and 4 are missing <br/>
    /// And when restored only missing values will be filled
    /// </summary>
    /// <returns>Restored data</returns>
    public IData Restore(IData dataWithMissingValues)
    {
        var inputLength = dataWithMissingValues.Input.Count;
        var outputLength = dataWithMissingValues.Output.Count;
        
        var missingInput = new bool[inputLength];
        var missingOutput = new bool[outputLength];

        for(int i = 0;i<missingInput.Length;i++){
            missingInput[i] = dataWithMissingValues.Input[i]<-1;
        }
        for(int i = 0;i<missingOutput.Length;i++){
            missingOutput[i] = dataWithMissingValues.Output[i]<-1;
        }

        var distance = (IData d1, IData d2) =>
        {
            var res = 0f;
            var holder = 0f;
            for (int i = 0; i < inputLength; i++)
            {
                if (missingInput[i]) continue;
                holder = d1.Input[i] - d2.Input[i];
                res += holder * holder;
            }
            for (int i = 0; i < outputLength; i++)
            {
                if (missingOutput[i]) continue;
                holder = d1.Output[i] - d2.Output[i];
                res += holder * holder;
            }
            return MathF.Sqrt(res);
        };
        var result = DataLearning.Diffuse(DataSet, dataWithMissingValues, distance);
        for (int i = 0; i < inputLength; i++)
        {
            if (missingInput[i]) continue;
            result.Input[i] = dataWithMissingValues.Input[i];
        }
        for (int i = 0; i < outputLength; i++)
        {
            if (missingOutput[i]) continue;
            result.Output[i] = dataWithMissingValues.Output[i];
        }
        return result;
    }

    /// <summary>
    /// Merges <see langword="element.Output"/> vector with data prediction on <see langword="element.Input"/> vector
    /// </summary>
    public void MergeWithPrediction(IData element)
    {
        var prediction = DataLearning.Diffuse(DataSet, element.Input);
        element.Output = (Vector)(prediction + element.Output).Divide(2);
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
        element.Output = (Vector)(element.Output + toReplace.data.Output).Divide(2);

        DataSet.Data[toReplace.id] = element;
    }
    public (float error, float absError, float maxError) ComputePredictionError(Vector[] test, Func<Vector, Data> getData)
    {
        Vector difference = new DenseVector(new float[OutputVectorLength]);
        Vector absDifference = new DenseVector(new float[OutputVectorLength]);
        Vector maxDifference = new DenseVector(new float[OutputVectorLength]);
        foreach (var t in test)
        {
            var data = getData(t);
            var input = data.Input;
            var actual = data.Output;

            var prediction = Predict(input);

            var diff = (prediction - actual);
  
            lock (this)
            {
                if (maxDifference.PointwiseAbs().Sum() < diff.PointwiseAbs().Sum())
                    maxDifference = (Vector)diff;
                difference = (Vector)(difference + diff);
                absDifference = (Vector)(absDifference + diff.PointwiseAbs());
            }
        };
        difference = (Vector)difference.Divide(test.Count());
        absDifference = (Vector)absDifference.Divide(test.Count());
        var absError = absDifference.Sum();
        var error = difference.PointwiseAbs().Sum();
        var maxError = maxDifference.PointwiseAbs().Sum();
        return (error, absError, maxError);
    }
    public float ComputeRestoreError(Vector[] test, Func<Vector, Data> getData, float percentOfMissingValues = 0.2f)
    {
        var missingValuesError = 0f;
        foreach (var t in test)
        {
            var input = getData(t).Input;
            var inputWithMissingValues = (Vector)input.Clone();
            input.Map(x =>
            {
                if (Random.Shared.NextSingle() < percentOfMissingValues)
                    return -2;
                return x;
            }, inputWithMissingValues);
            var restoredByVector = Restore(inputWithMissingValues);
            var diff = (input - restoredByVector);
            missingValuesError += ((float)diff.L2Norm());
        }
        return missingValuesError / test.Length;
    }
}