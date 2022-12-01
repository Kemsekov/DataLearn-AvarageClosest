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
    public AdaptiveDataSet(int inputVectorLength, int outputVectorLength, int maxElements){
        this.DataSet = new DataSet(inputVectorLength,outputVectorLength);
        this.MaxElements = maxElements;
        this.DataLearning = new DataLearning();
    }
    (Data data, int id) GetClosest(Data element){
        return DataSet.Data
            .Select((data, id) => (data, id))
            .MinBy(x => (x.data.Input - element.Input).L1Norm());
    }
    (Data data, int id) GetRandom(Random? rand = null){
        var id = GetRandomId(rand);
        return (DataSet.Data[id],id);
    }
    int GetRandomId(Random? rand = null){
        rand ??= Random.Shared;
        var count = DataSet.Data.Count;
        var id = rand.Next(count);
        return id;
    }
    
    public Vector Predict(Vector input){
        return DataLearning.Diffuse(DataSet,ref input);
    }

    /// <summary>
    /// Merges <see langword="element.Output"/> vector with data prediction on <see langword="element.Input"/> vector
    /// </summary>
    public void MergeWithPrediction(ref Data element){
        var prediction = DataLearning.Diffuse(DataSet, ref element.Input);
        element.Output = (Vector)(prediction + element.Output).Divide(2);
    }
    /// <summary>
    /// For given element, finds another closest by input vector element
    /// and average their input and output vectors.<br/>
    /// Replaces closest found element.
    /// </summary>
    /// <param name="element"></param>
    public void AddByMergingWithClosest(Data element){
        if (DataSet.Data.Count < MaxElements)
        {
            DataSet.Data.Add(element);
            return;
        }
        var toReplace = GetClosest(element);
        element.Input = (Vector)(element.Input+toReplace.data.Input).Divide(2);
        element.Output = (Vector)(element.Output+toReplace.data.Output).Divide(2);

        DataSet.Data[toReplace.id] = element;
    }
}