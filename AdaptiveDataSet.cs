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

    public AdaptiveDataSet(DataSet dataSet, DataLearning dataLearning, int maxElements)
    {
        this.DataSet = dataSet;
        this.MaxElements = maxElements;
        this.DataLearning = dataLearning;
    }
    (Data data, int id) GetClosest(Data element){
        return DataSet.Data
            .Select((data, id) => (data, id))
            .MinBy(x => (x.data.Input - element.Input).L1Norm());
    }
    /// <summary>
    /// For given element it finds closest element from dataset by input vector,
    /// averages their input vectors, finds a prediction for that input vector
    /// and averages prediction with output vector.<br/>
    /// Replaces closest found element.
    /// </summary>
    public void AddByMergingWithPrediction(Data element)
    {
        if (DataSet.Data.Count < MaxElements)
        {
            DataSet.Data.Add(element);
            return;
        }
        var toReplace = GetClosest(element);

        element.Input = (Vector)(toReplace.data.Input + element.Input).Divide(2);

        var prediction = DataLearning.Diffuse(DataSet, ref element.Input);
        
        element.Output = (Vector)(prediction + element.Output).Divide(2);

        DataSet.Data[toReplace.id] = element;
    }
    /// <summary>
    /// For given element, finds another closest by input vector element
    /// and replace it with given.
    /// </summary>
    public void AddByReplacingClosest(Data element)
    {
        if (DataSet.Data.Count < MaxElements)
        {
            DataSet.Data.Add(element);
            return;
        }
        var toReplace = GetClosest(element);
        DataSet.Data[toReplace.id] = element;
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