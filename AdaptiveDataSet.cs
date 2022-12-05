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
    public AdaptiveDataSet(DataSet dataSet, DataLearning dataLearning, int maxElements)
    {
        this.DataSet = dataSet;
        this.MaxElements = maxElements;
        this.DataLearning = dataLearning;
    }
    public AdaptiveDataSet(int inputVectorLength, int outputVectorLength, int maxElements)
    {
        this.DataSet = new DataSet(inputVectorLength, outputVectorLength);
        this.MaxElements = maxElements;
        this.DataLearning = new DataLearning();
    }
    (Data data, int id) GetClosest(Data element)
    {
        return DataSet.Data
            .Select((data, id) => (data, id))
            .MinBy(x => (x.data.Input - element.Input).L1Norm());
    }
    (Data data, int id) GetRandom(Random? rand = null)
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

    public Vector Predict(Vector input)
    {
        return DataLearning.Diffuse(DataSet, ref input);
    }
    /// <summary>
    /// Accepts data with missing values and computes all it's other values.<br/>
    /// Index of missingData is id of column, value == 1 if given column is missing.<br/>
    /// For example with vector (1,2,_,3) missingColumns would be [0,0,1,0]
    /// </summary>
    /// <returns>Data which contains both input and output with filled missing values</returns>
    public Data FillMissingValues(Data dataWithMissingValues, byte[] inputMissingColumns, byte[] outputMissingColumns)
    {
        var concated = inputMissingColumns.Concat(outputMissingColumns);
        var outputLength = concated.Count(x=>x==1);
        var inputLength = concated.Count(x=>x==0);
        var getInput = (Data dt) =>
        {
            var res = new float[inputLength];
            int next = 0;
            for (int i = 0; i < InputVectorLength; i++)
            {
                if (inputMissingColumns[i] == 0)
                    res[next++] = dt.Input[i];
            }
            for (int i = 0; i < OutputVectorLength; i++)
            {
                if (outputMissingColumns[i] == 0)
                    res[next++] = dt.Output[i];
            }
            return new DenseVector(res);
        };
        var getOutput = (Data dt) =>
        {
            var res = new float[outputLength];
            int next = 0;
            for (int i = 0; i < InputVectorLength; i++)
            {
                if (inputMissingColumns[i] != 0)
                    res[next++] = dt.Input[i];
            }
            for (int i = 0; i < OutputVectorLength; i++)
            {
                if (outputMissingColumns[i] != 0)
                    res[next++] = dt.Output[i];
            }
            return new DenseVector(res);
        };
        var result = DataLearning.Diffuse(DataSet, getInput(dataWithMissingValues), getInput, getOutput);
        var resultData = dataWithMissingValues * 1;
        var next = 0;
        for (int i = 0; i < InputVectorLength; i++)
        {
            if (inputMissingColumns[i] != 0)
            {
                resultData.Input[i] = result[next++];
            }
        }
        for (int i = 0; i < OutputVectorLength; i++)
        {
            if (outputMissingColumns[i] != 0)
            {
                resultData.Output[i] = result[next++];
            }
        }
        return resultData;
    }

    /// <summary>
    /// Merges <see langword="element.Output"/> vector with data prediction on <see langword="element.Input"/> vector
    /// </summary>
    public void MergeWithPrediction(ref Data element)
    {
        var prediction = DataLearning.Diffuse(DataSet, ref element.Input);
        element.Output = (Vector)(prediction + element.Output).Divide(2);
    }
    /// <summary>
    /// For given element, finds another closest by input vector element
    /// and average their input and output vectors.<br/>
    /// Replaces closest found element.
    /// </summary>
    /// <param name="element"></param>
    public void AddByMergingWithClosest(Data element)
    {
        if (DataSet.Data.Count < MaxElements)
        {
            DataSet.Data.Add(element);
            return;
        }
        var toReplace = GetClosest(element);
        element.Input = (Vector)(element.Input + toReplace.data.Input).Divide(2);
        element.Output = (Vector)(element.Output + toReplace.data.Output).Divide(2);

        DataSet.Data[toReplace.id] = element;
    }
    public (float error, float absError, float maxError) ComputeError(IEnumerable<Vector> test, Func<Vector, Data> getData)
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

            //--- TODO: remove
            var predId = prediction.MaximumIndex();
            var actualId = actual.MaximumIndex();
            var predVal = prediction[predId];
            //---

            if (maxDifference.PointwiseAbs().Sum() < diff.PointwiseAbs().Sum())
                maxDifference = (Vector)diff;
            difference = (Vector)(difference + diff);
            absDifference = (Vector)(absDifference + diff.PointwiseAbs());
        }
        difference = (Vector)difference.Divide(test.Count());
        absDifference = (Vector)absDifference.Divide(test.Count());
        var absError = absDifference.Sum();
        var error = difference.PointwiseAbs().Sum();
        var maxError = maxDifference.PointwiseAbs().Sum();
        return (error, absError, maxError);
    }
}