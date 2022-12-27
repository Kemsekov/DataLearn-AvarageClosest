using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Single;


//Add autoencoder support https://github.com/SciSharp/TensorFlow.NET

//make that IData Input and Output can kinda change in a sense
//that IData will have one single array as underlying data and you can pick some parts
//of it to treat as input and output. It will be useful when combined with Diffuse method
//in which you can feed not full Input vector, but mark missing all values with
//unknown output as OutputVector, and diffuse will treat them accordingly  

public interface IData
{
    Vector Input{get;set;}
    public static IData operator +(IData left, IData right)
    {
        return new Data(){Input = (Vector)(left.Input+right.Input)};
    }

    public static IData operator *(IData left, float scalar)
    {
        return new Data(){Input=(Vector)(left.Input*scalar)};
    }
    IData Divide(float scalar);
    IData Clone();
}

public struct Data : IData
{
    public Data(Vector input)
    {
        Input = input;
    }
    public Data(Vector<float> input)
    {
        Input = (Vector)input;
    }
    public Data(float[] input){
        Input = new DenseVector(input);
    }
    public Vector Input{get;set;}
    public IData Clone()
    {
        return new Data((Vector)Input.Clone());
    }

    public IData Divide(float scalar)
    {
        return new Data(){
        Input = (Vector)Input.Divide(scalar)
        };
    }
}

public class DataSet
{
    public int InputVectorLength { get; protected set;}
    public IList<IData> Data { get;set; }
    public DataSet(int inputVectorLength, IList<IData>? data = null)
    {
        this.InputVectorLength = inputVectorLength;
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
    /// <summary>
    /// Computes L2 distance between two vectors on their subvectors.<br/>
    /// Subvectors is taken in such a way, that if any of two vectors have a missing value at
    /// given index it will not consider this index while computing distance.<br/>
    /// For example for vectors <see langword="(1,2,3,-2)"/> and <see langword="(-2,1,5,-4)"/> <br/>
    /// result will be <see langword="sqrt((2-1)^2+(5-3)^2)"/>
    /// </summary>
    public float Distance(Vector n1, Vector n2)
    {
        float distance = 0;
        float holder = 0;
        var len = Math.Min(n1.Count,n2.Count);
        for(int i = 0;i<len;i++){
            if(n1[i]<-1 || n2[i]<-1) continue;
            holder = n1[i]-n2[i];
            distance+=holder*holder;
        }
        return MathF.Sqrt(distance);
    }

    /// <summary>
    /// Diffuses <paramref name="data"/> on <paramref name="input"/> vector.<br/>
    /// By default <paramref name="inputDistance"/> does following: <br/>
    /// Where input vector values less than -1 it is considered missing, and
    /// would not be used in computing prediction.
    /// </summary>
    /// <returns>
    /// Diffused vector which corresponds to 'average' of local known(not missing) data
    /// </returns>
    public Vector Diffuse(DataSet data, Vector input)
    {
        var inputLength = data.InputVectorLength;
        Vector averageOutputData = new DenseVector(new float[inputLength]);
        Vector buffer = new DenseVector(new float[inputLength]);

        float addedCoeff = 0;
        float coeff;
        float distSquared;
        for (int i = 0; i < data.Data.Count; i++)
        {
            var dt = data.Data[i];
            distSquared = MathF.Pow(Distance(input, dt.Input), DiffusionCoefficient);
            distSquared = Math.Max(distSquared, DiffusionTheta);
            coeff = ActivationFunction(distSquared);
            addedCoeff += coeff;
            //because data from dataset itself can contain missing values
            //we need to transform our vector so these values will not
            //spoil total result
            dt.Input.Map(x=>{
                if(x<-1) return 0;
                return x*coeff;
            },buffer);
            averageOutputData = (Vector)(averageOutputData + buffer);
        }
        if (addedCoeff < DiffusionTheta) addedCoeff = 1;
        averageOutputData = (Vector)averageOutputData.Divide(addedCoeff);
        return averageOutputData;
    }
    /// <summary>
    /// Method to do some computations on closest on input vector elements from data set
    /// </summary>
    /// <param name="OnElementWithCoefficient">
    /// For each element in dataset there is a coefficient of how "close" 
    /// this particular element is. 
    /// This coefficient is computed using 
    /// <see cref="DiffusionTheta"/> and <see cref="DiffusionCoefficient"/>
    /// and can be used to define how important given element is relative to input vector.
    /// The closer element is to input vector, the bigger this coefficient is.
    /// </param>
    /// <param name="OnEndWithTotalCoefficient">
    /// When we completed iterating over whole dataset we can do some finishing actions by accepting sum of all coefficients
    /// </param>
    void DoOnClosest(DataSet data, Vector input, Action<IData,float> OnElementWithCoefficient, Action<float> OnEndWithTotalCoefficient){
        float addedCoeff = 0;
        float coeff;
        float distSquared;
        for (int i = 0; i < data.Data.Count; i++)
        {
            var dt = data.Data[i];
            distSquared = MathF.Pow(Distance(input, dt.Input), DiffusionCoefficient);
            distSquared = Math.Max(distSquared, DiffusionTheta);
            coeff = ActivationFunction(distSquared);
            OnElementWithCoefficient(dt,coeff);
            addedCoeff += coeff;
            
        }
        if (addedCoeff < DiffusionTheta) addedCoeff = 1;
        OnEndWithTotalCoefficient(addedCoeff);
    }
    /// <summary>
    /// Diffuses error vector on prediction from input vector so
    /// prediction will produce better results
    /// </summary>
    /// <param name="input">We will diffuse error on prediction from this vector</param>
    /// <param name="error">
    /// How much we need to alter prediction on given input. <br/>
    /// Basically difference between prediction and required output
    /// </param>
    public void DiffuseError(DataSet data, Vector input, Vector error){
        float totalSum = 0;
        var dict = new ConcurrentDictionary<IData,float>(Environment.ProcessorCount,data.Data.Count);
        DoOnClosest(data,input,(a,b)=>dict[a]=b,sum=>totalSum=sum);
        Parallel.ForEach(dict,element=>
        {
            var dt = element.Key;
            var coef = element.Value;
            dt.Input.MapIndexed((index,x)=>{
                if(x<-1) return x;
                var y = error[index];
                if(y<-1) return x;
                return x-y*coef/totalSum;
            },dt.Input);
        });
    }
    public Vector DiffuseOnNClosest(DataSet data, Vector input, int n){
        var inputLength = data.InputVectorLength;
        var mins = data.Data.FindNMinimal(n,x=>Distance(x.Input,input));
        return Diffuse(new DataSet(data.InputVectorLength,mins),input);
    }
    /// <summary>
    /// Returns dataset that corresponds to given <paramref name="dataSet"/>, 
    /// that evenly distributed on n-dimensional(where n is input length) identity cube and 
    /// scaled/shifted by <paramref name="CoordinatesScale"/> and <paramref name="CoordinatesShift"/>
    /// </summary>
    /// <param name="approximationSize">How many dots to create in space?</param>
    public DataSet GetApproximationSet(int approximationSize, int InputVectorLength, float CoordinatesScale, Vector CoordinatesShift)
    {
        var Approximation = new DataSet(InputVectorLength, new List<IData>(approximationSize));
        for (int i = 0; i < approximationSize; i++)
        {
            var input = new float[InputVectorLength];
            Array.Fill(input, 0.5f);
            Approximation.Data.Add(new Data(input));
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
    /// Moves data points from each other to normally fill input vector space
    /// </summary>
    public void DistributeData(DataSet dataSet, float CoordinatesScale, Vector CoordinatesShift)
    {
        var InputVectorLength = dataSet.InputVectorLength;
        var data = dataSet.Data;
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
        var chunkSize = MathF.Pow(data.Count, 1f / InputVectorLength);
        var step = 1f / chunkSize;
        var position = new DenseVector(new float[InputVectorLength]);

        for (int i = 0; i < data.Count; i++)
        {
            data[i].Input = (Vector)position.Clone();
            position[0] += step;
            normalizeVector(ref position, step);
        }
        NormalizeCoordinates(dataSet);
        foreach(var d in dataSet.Data){
            d.Input=(Vector)(d.Input*CoordinatesScale+CoordinatesShift);
        }
    }
    /// <summary>
    /// Makes sure that input(non missing) part of data is filling bounds
    /// in range [0,1]. 
    /// Like apply linear transformation to input part of vectors in data that it
    /// fills [0,1] space.
    /// </summary>
    public void NormalizeCoordinates(DataSet dataSet, Vector? input = null)
    {
        if(dataSet.Data.Count==0) return;
        input ??= new DenseVector(new float[dataSet.InputVectorLength]);
        var data = dataSet.Data;
        var maxArray = new float[dataSet.InputVectorLength];
        var minArray = new float[dataSet.InputVectorLength];;
        Array.Fill(maxArray,float.MinValue);
        Array.Fill(minArray,float.MaxValue);
        Vector max = new DenseVector(maxArray);
        Vector min = new DenseVector(minArray);

        for (int i = 0; i < data.Count; i++)
        {
            var dt = data[i].Input;
            max = (Vector)(max.PointwiseMaximum(dt));
            min = (Vector)(min.PointwiseMinimum(dt));
        }
        var diff = max-min;
        for (int i = 0; i < data.Count; i++)
        {
            var dt = data[i].Input;

            dt.MapIndexed((index,x)=>{
                if(input[index]<-1) return x;
                return (x-min[index])/(diff[index]);
            },dt);
        }
    }
}