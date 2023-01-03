using System;
using System.Buffers;
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
}

public interface IDataSet
{
    int InputVectorLength { get;}
    IList<IData> Data { get;}
}

public class DataSet : IDataSet
{
    public int InputVectorLength { get; protected set;}
    public virtual IList<IData> Data { get;}
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
public class DataLearning : IDataLearning
{
    /// <summary>
    /// Lowest value for data changing. 
    /// When big <see langword="Diffuse"/> method will 
    /// take more average values from all dataset, 
    /// when small <see langword="Diffuse"/> method will consider local values more valuable.
    /// </summary>
    public float DiffusionTheta{get;set;} = 0.001f;
    /// <summary>
    /// How strong local values is
    /// </summary>
    public float DiffusionCoefficient{get;set;} = 2;

    public IDataSet DataSet{get;set;}

    public DataLearning(IDataSet dataSet)
    {
        this.DataSet = dataSet;
    }
    public (IData data, int id) GetClosest(IDataSet dataSet, IData element)
    {
        int minId = 0;
        float minDist = float.MaxValue;
        IData? closest = null;
        Parallel.For(0,dataSet.Data.Count,i=>
        {
            var x = dataSet.Data[i];
            var dist = DataHelper.Distance(x.Input, element.Input);
            lock(dataSet)
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
    /// Diffuses <paramref name="data"/> on <paramref name="input"/> vector.<br/>
    /// By default <paramref name="inputDistance"/> does following: <br/>
    /// Where input vector values less than -1 it is considered missing, and
    /// would not be used in computing prediction.
    /// </summary>
    /// <returns>
    /// Diffused vector which corresponds to 'average' of local known(not missing) data
    /// </returns>
    public Vector Diffuse(IDataSet data, Vector input)
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
            distSquared = MathF.Pow(DataHelper.Distance(input, dt.Input), DiffusionCoefficient);
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
    void DoOnClosest(IDataSet data, Vector input, Action<IData,float> OnElementWithCoefficient, Action<float> OnEndWithTotalCoefficient){
        float addedCoeff = 0;
        float coeff;
        float distSquared;
        for (int i = 0; i < data.Data.Count; i++)
        {
            var dt = data.Data[i];
            distSquared = MathF.Pow(DataHelper.Distance(input, dt.Input), DiffusionCoefficient);
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
    public void DiffuseError(IDataSet data, Vector input, Vector error){
        float totalSum = 0;
        var pool = ArrayPool<(IData element, float coefficient)>.Shared;
        var values = pool.Rent(data.Data.Count);
        int lastUsedIndex = 0;
        DoOnClosest(data,input,(a,b)=>{
            lock(values){
                values[lastUsedIndex++] = (a,b);
            }
        },sum=>totalSum=sum);
        Parallel.ForEach(values,value=>
        {
            var dt = value.element;
            if(dt is null) return;
            var coef = value.coefficient;
            dt.Input.MapIndexed((index,x)=>{
                if(x<-1) return x;
                var y = error[index];
                if(y<-1) return x;
                return x-y*coef/totalSum;
            },dt.Input);
        });
        pool.Return(values);
    }
    public Vector DiffuseOnNClosest(IDataSet data, Vector input, int n){
        var inputLength = data.InputVectorLength;
        var mins = data.Data.FindNMinimal(n,x=>DataHelper.Distance(x.Input,input));
        return Diffuse(new DataSet(data.InputVectorLength,mins),input);
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
    /// Makes sure that input(non missing) part of data is filling bounds
    /// in range [0,1]. 
    /// Like apply linear transformation to input part of vectors in data that it
    /// fills [0,1] space.
    /// </summary>
    public void NormalizeCoordinates(IDataSet dataSet, Vector? input = null)
    {
        DataHelper.NormalizeCoordinates(dataSet,input);
    }

    public (IData data, int id) GetClosest(IData element)
    {
        return GetClosest(DataSet,element);
    }

    public Vector Diffuse(Vector input)
    {
        return Diffuse(DataSet,input);
    }

    public void DiffuseError(Vector input, Vector error)
    {
        DiffuseError(DataSet,input,error);
    }

    public Vector DiffuseOnNClosest(Vector input, int n)
    {
        return DiffuseOnNClosest(DataSet,input,n);
    }

    public void NormalizeCoordinates(Vector? input = null)
    {
        NormalizeCoordinates(DataSet,input);
    }
}