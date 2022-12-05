using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;

public struct Data : 
    System.Numerics.IAdditionOperators<Data,Data,Data>,
    System.Numerics.ISubtractionOperators<Data,Data,Data>,
    System.Numerics.IMultiplyOperators<Data,float,Data>
{
    public Vector Input;
    public Vector Output;

    public static Data operator +(Data left, Data right)
    {
        return new(){
            Input = (Vector)(left.Input+right.Input),
            Output = (Vector)(left.Output+right.Output)
        };
    }

    public static Data operator -(Data left, Data right)
    {
        return new(){
            Input = (Vector)(left.Input-right.Input),
            Output = (Vector)(left.Output-right.Output)
        };
    }

    public static Data operator *(Data left, float right)
    {
        return new(){
            Input = (Vector)(left.Input*right),
            Output = (Vector)(left.Output*right)
        };
        
    }
    public Data Divide(float value){
        return new(){
            Input = (Vector)Input.Divide(value),
            Output = (Vector)Output.Divide(value)
        };
    }
}

public class DataSet
{
    public int InputVectorLength { get; }
    public int OutputVectorLength { get; }
    public List<Data> Data { get; }
    public DataSet(int inputVectorLength, int outputVectorLength, List<Data>? data = null)
    {
        this.InputVectorLength = inputVectorLength;
        this.OutputVectorLength = outputVectorLength;
        this.Data = data ?? new List<Data>();
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

    float Distance(ref Vector n1, ref Vector n2)
    {
        return ((float)(n1 - n2).L2Norm());
    }

    /// <summary>
    /// Diffuses <paramref name="data"/> on <paramref name="input"/> vector
    /// </summary>
    /// <returns>
    /// Diffused vector which corresponds to 'average' of local data
    /// </returns>
    public Vector Diffuse(DataSet data, ref Vector input)
    {
        Vector averageOutputData = new DenseVector(new float[data.OutputVectorLength]);
        float addedCoeff = 0;
        float coeff;
        float distSquared;
        for (int i = 0; i < data.Data.Count; i++)
        {
            var dt = data.Data[i];
            distSquared = MathF.Pow(Distance(ref input, ref dt.Input), DiffusionCoefficient);
            distSquared = Math.Max(distSquared, DiffusionTheta);
            coeff = ActivationFunction(distSquared);
            addedCoeff += coeff;
            averageOutputData = (Vector)(averageOutputData + dt.Output * coeff);
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
        var Approximation = new DataSet(dataSet.InputVectorLength, dataSet.OutputVectorLength, new List<Data>(approximationSize));
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
            approximation.Output = Diffuse(dataSet, ref approximation.Input);
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