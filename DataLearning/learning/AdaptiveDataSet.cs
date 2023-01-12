using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphSharp.Graphs;
using MathNet.Numerics.LinearAlgebra.Single;

/// <summary>
/// Adaptive data set that have constant amount of elements and
/// changes over time as new elements added by merging old data with new one.
/// It basically limit your model size, meanwhile trying to retain ability to learn
/// </summary>
public class AdaptiveDataSet
{
    public IDataSet DataSet { get; }
    public int MaxElements { get; }
    public IDataLearning DataLearning { get; }
    public int InputVectorLength => DataSet.InputVectorLength;
    public AdaptiveDataSet(IDataLearning dataLearning, int maxElements)
    {
        this.DataSet = dataLearning.DataSet;
        this.MaxElements = maxElements;
        this.DataLearning = dataLearning;
    }
    public AdaptiveDataSet(int inputVectorLength, int maxElements, VectorMask mask) : this(new DataLearning(new DataSet(inputVectorLength), mask), maxElements)
    {
    }
    public Vector PredictOnNClosest(Vector input, int n)
    {
        if (DataSet.Data.Count == 0) return input;
        var result = DataLearning.DiffuseOnNClosest(input, n);
        return FillMissingValues(input, result);
    }
    /// <summary>
    /// Clusters close by input elements to be close by output element.
    /// Can be used to do dimensionality reduction.
    /// Requires multiple iterations to perform well.
    /// Have a huge behavior space depending of how you change parameters for each iteration.
    /// </summary>
    /// <param name="ClusterInput"></param>
    /// <param name="ClusterOutput"></param>
    public void Cluster(Func<Vector, Vector> ClusterInput, Func<Vector, Vector> ClusterOutput)
    {
        var data = DataSet.Data;
        if (data.Count == 0) return;

        Parallel.For(0, data.Count, i =>
        {
            var element = data[i].Input;
            var input = ClusterInput(element);
            var prediction = Predict(input);
            prediction.MapIndexed((index, x) =>
            {
                if (x < -1) return x;
                return element[index] - x;
            }, prediction);
            DiffuseError(element, prediction);
        });
        DataLearning.NormalizeCoordinates(ClusterOutput(data[0].Input));
    }
    /// <summary>
    /// Get clusters from data by<br/>
    /// 1) Building minimal spanning tree on <see cref="DataHelper.Distance(Vector, Vector)"/> function <br/>
    /// 2) Repeatedly removing the longest edge in tree from graph <br/>
    /// 3) Returning connected components of a graph<br/>
    /// </summary>
    /// <returns>With each iteration the clusters count is increasing by one</returns>
    public IEnumerable<IList<Cluster>> GetClustersBySpanningTree(int connectToClosestCount, int skipIterations, VectorMask mask)
    {
        if (DataSet.Data.Count == 0)
        {
            yield break;
        }
        var g = new DataGraph(new DataConfiguration(mask), DataSet.Data);
        g.Do.ConnectToClosest(connectToClosestCount, (d1, d2) => DataHelper.Distance(d1.Data.Input, d2.Data.Input, mask));
        List<DataEdge> forest = new();
        if(skipIterations>0){
            forest = g.Do.FindSpanningForestKruskal().Forest.OrderBy(x => -x.Weight).ToList();
        }
        else
            forest = g.Do.FindSpanningForestKruskal().Forest.ToList();
        g.SetSources(edges: forest);
        using var component = g.Do.FindComponents();
        foreach (var toRemove in forest)
        {
            g.Edges.Remove(toRemove);
            if (skipIterations-- <= 0)
            {
                yield return component.Components.Select(c => new Cluster(c.Select(x => x.Data).ToList())).ToList();
            }
        }
    }

    public void ClusterBySequence(Func<Vector, Vector> ClusterInput, Func<Vector, Vector> ClusterOutput, float StartDiffusionCoefficient)
    {
        var originalDiffCoefficient = DataLearning.DiffusionCoefficient;
        DataLearning.DiffusionCoefficient = StartDiffusionCoefficient;
        Cluster(ClusterInput, ClusterOutput);
        Cluster(ClusterInput, ClusterOutput);
        Cluster(ClusterInput, ClusterOutput);
        DataLearning.DiffusionCoefficient = StartDiffusionCoefficient - 2;
        Cluster(ClusterInput, ClusterOutput);
        Cluster(ClusterInput, ClusterOutput);
        DataLearning.DiffusionCoefficient = StartDiffusionCoefficient - 4;
        Cluster(ClusterInput, ClusterOutput);
        DataLearning.DiffusionCoefficient = originalDiffCoefficient;
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
        if (DataSet.Data.Count == 0) return input;
        var result = DataLearning.Diffuse(input);
        return FillMissingValues(input, result);
    }
    public void DiffuseError(Vector input, Vector error)
    {
        DataLearning.DiffuseError(input, error);
    }
    public static Vector FillMissingValues(Vector input, Vector result)
    {
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
    public Vector PredictPure(Vector input)
    {
        return DataLearning.Diffuse(input);
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
        var toReplace = DataLearning.GetClosest(element);
        element.Input = (Vector)(element.Input + toReplace.data.Input).Divide(2);

        DataSet.Data[toReplace.id] = element;
    }
    /// <summary>
    /// Adds new element by replacing random element
    /// </summary>
    public void AddByReplacingRandom(IData element)
    {
        if (DataSet.Data.Count < MaxElements)
        {
            DataSet.Data.Add(element);
            return;
        }
        var toReplace = Random.Shared.Next(DataSet.Data.Count);
        DataSet.Data[toReplace] = element;
    }
    Vector CreateMissingValues(Vector vec, float percentOfMissingValues = 0.2f)
    {
        if (vec.Any(x => x < -1)) return vec;
        var r = new Random();
        var res = vec.Clone();
        for (int i = 0; i < vec.Count; i++)
        {
            if (r.NextSingle() < percentOfMissingValues)
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
    public TestPrediction ComputePredictionError(Vector[] test, Func<Vector, IData> getInput)
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
        return new(difference, absDifference, maxDifference);
    }
    /// <summary>
    /// Diffuses <paramref name="dataSet"/> on <paramref name="Approximation"/> dataset.
    /// </summary>
    public void Predict(IDataSet Approximation, Func<Vector, Vector> getInput)
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
    public void PredictOnNClosest(DataSet Approximation, Func<Vector, Vector> getInput, int n)
    {
        if (DataSet.Data.Count == 0) return;
        Parallel.For(0, Approximation.Data.Count, (i, _) =>
        {
            var approximation = Approximation.Data[i];
            var input = getInput(approximation.Input);
            approximation.Input = PredictOnNClosest(input, n);
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
        for (int i = 0; i < diff.Count; i++)
        {
            var d = diff[i];
            if (d == 0) continue;
            printed = true;
            System.Console.WriteLine($"{i}\t:\t{d}");
        }
        if (printed)
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
            var restoredByVector = PredictPure(inputWithMissingValues);
            var diff = (input - restoredByVector);
            missingValuesError += ((float)diff.L2Norm());
        }
        return missingValuesError / test.Length;
    }
}