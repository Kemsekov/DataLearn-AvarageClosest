using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;


//make that IData Input and Output can kinda change in a sense
//that IData will have one single array as underlying data and you can pick some parts
//of it to treat as input and output. It will be useful when combined with Diffuse method
//in which you can feed not full Input vector, but mark missing all values with
//unknown output as OutputVector, and diffuse will treat them accordingly  

public interface IData{
    Vector Input{get;set;}
    Vector Output{get;set;}
    int InputVectorLength => Input.Count;
    int OutputVectorLength => Input.Count;
}

public struct Data : IData
{
    public Vector Input{get;set;}
    public Vector Output{get;set;}
}

public class DataSet
{
    public int InputVectorLength { get; protected set;}
    public int OutputVectorLength { get; protected set; }
    public List<IData> Data { get; }
    public DataSet(int inputVectorLength, int outputVectorLength, List<IData>? data = null)
    {
        this.InputVectorLength = inputVectorLength;
        this.OutputVectorLength = outputVectorLength;
        this.Data = data ?? new List<IData>();
    }

}

/// <summary>
/// Model that uses multidimensional diffusion to find 'average on closest' output on 
/// some inputs and outputs data set and builds approximation space.
/// Something like regression in machine learning.
/// </summary>
public class DataLearning
{
    /// <summary>
    /// Lowest value for data changing. 
    /// When big <see langword="Diffuse"/> method will 
    /// take more average values from all dataset, 
    /// when small <see langword="Diffuse"/> method will consider local values more valuable.
    /// </summary>
    public float DiffusionTheta = 0.001f;
    /// <summary>
    /// How strong local values is
    /// </summary>
    public float DiffusionCoefficient = 2;

    public DataLearning()
    {
    }

    float Distance(Vector n1, Vector n2)
    {
        return ((float)(n1 - n2).L2Norm());
    }

    /// <summary>
    /// Diffuses <paramref name="data"/> on <paramref name="input"/> vector
    /// </summary>
    /// <returns>
    /// Diffused vector which corresponds to 'average' of local data
    /// </returns>
    public Vector Diffuse(DataSet data, Vector input, Func<IData,Vector>? getInput = null, Func<IData,Vector>? getOutput = null)
    {
        getInput ??= x=>x.Input;
        getOutput ??= x=>x.Output;
        var outputLength = getOutput(data.Data.First()).Count;
        Vector averageOutputData = new DenseVector(new float[outputLength]);
        float addedCoeff = 0;
        float coeff;
        float distSquared;
        for (int i = 0; i < data.Data.Count; i++)
        {
            var dt = data.Data[i];
            distSquared = MathF.Pow(Distance(input, getInput(dt)), DiffusionCoefficient);
            distSquared = Math.Max(distSquared, DiffusionTheta);
            coeff = ActivationFunction(distSquared);
            addedCoeff += coeff;
            averageOutputData = (Vector)(averageOutputData + getOutput(dt) * coeff);
        }
        if (addedCoeff < DiffusionTheta) addedCoeff = 1;
        averageOutputData = (Vector)averageOutputData.Divide(addedCoeff);
        return averageOutputData;
    }

    /// <summary>
    /// Returns dataset that corresponds to given <paramref name="dataSet"/>, 
    /// that evenly distributed on n-dimensional(where n is input length) identity cube and 
    /// scaled/shifted by <paramref name="CoordinatesScale"/> and <paramref name="CoordinatesShift"/>
    /// </summary>
    /// <param name="approximationSize">How many dots to create in space?</param>
    public DataSet GetApproximationSet(int approximationSize, DataSet dataSet, float CoordinatesScale, Vector CoordinatesShift)
    {
        var Approximation = new DataSet(dataSet.InputVectorLength, dataSet.OutputVectorLength, new List<IData>(approximationSize));
        for (int i = 0; i < approximationSize; i++)
        {
            var input = new float[dataSet.InputVectorLength];
            var output = new float[dataSet.OutputVectorLength];
            Array.Fill(input, 0.5f);
            Array.Fill(output, 0.5f);
            Approximation.Data.Add(new Data() { Input = new DenseVector(input), Output = new DenseVector(output) });
        }
        DistributeData(Approximation, CoordinatesScale, CoordinatesShift);
        return Approximation;
    }

    /// <summary>
    /// In general when <paramref name="distSquared"/> is small must 
    /// return big number, when <paramref name="distSquared"/> is big must return small number.
    /// <paramref name="distSquared"/> is >= 0
    /// </summary>
    /// <param name="distSquared"></param>
    public virtual float ActivationFunction(float distSquared)
    {
        return 1 / (distSquared * distSquared);
    }
    /// <summary>
    /// Diffuses <paramref name="dataSet"/> on <paramref name="Approximation"/> dataset.
    /// </summary>
    public void Diffuse(DataSet dataSet, DataSet Approximation)
    {
        if (dataSet.Data.Count == 0) return;
        Parallel.For(0, Approximation.Data.Count, (i, _) =>
        {
            var approximation = Approximation.Data[i];
            approximation.Output = Diffuse(dataSet, approximation.Input);
            Approximation.Data[i] = approximation;
        });
    }

    /// <summary>
    /// Moves data points from each other to normally fill input vector space
    /// </summary>
    public void DistributeData(DataSet dataSet, float CoordinatesScale, Vector CoordinatesShift)
    {
        var InputVectorLength = dataSet.InputVectorLength;
        var data = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(dataSet.Data);
        void normalizeVector(ref DenseVector position, float step)
        {
            for (int k = 0; k < InputVectorLength; k++)
            {
                if (position[k] >= 1 - step / 2)
                {
                    var tmp = position[k] - 1;
                    position[k] = 0;
                    if (k + 1 >= InputVectorLength) break;
                    position[k + 1] += step;
                }
            }
        }
        var chunkSize = MathF.Pow(data.Length, 1f / InputVectorLength);
        var step = 1f / chunkSize;
        var position = new DenseVector(new float[InputVectorLength]);

        for (int i = 0; i < data.Length; i++)
        {
            data[i].Input = (Vector)position.Clone();
            position[0] += step;
            normalizeVector(ref position, step);
        }
        NormalizeCoordinates(dataSet, CoordinatesScale, CoordinatesShift);
    }
    /// <summary>
    /// Makes sure that <paramref name="InputVectorLength"/> part of data is filling bounds
    /// in range [0,1]. 
    /// Like apply linear transformation to input part of vectors in data that it
    /// fills [0,1] space.
    /// </summary>
    void NormalizeCoordinates(DataSet dataSet, float scaleCoefficient = 1, Vector? shift = null)
    {
        var data = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(dataSet.Data);
        var max = dataSet.Data.Max(x => x.Input.Max());
        shift ??= new DenseVector(new float[dataSet.InputVectorLength]);
        for (int i = 0; i < data.Length; i++)
        {
            var scaled = data[i].Input.Divide(max) * scaleCoefficient + shift;
            data[i].Input = (Vector)scaled;
        }
    }
}