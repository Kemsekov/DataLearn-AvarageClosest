using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DataHelper
{
    /// <summary>
    /// Moves data points from each other to normally fill input vector space
    /// </summary>
    public static void DistributeData(IDataSet dataSet, float CoordinatesScale, Vector CoordinatesShift)
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
        foreach (var d in dataSet.Data)
        {
            d.Input = (Vector)(d.Input * CoordinatesScale + CoordinatesShift);
        }
    }
    /// <summary>
    /// Makes sure that input(non missing) part of data is filling bounds
    /// in range [0,1]. 
    /// Like apply linear transformation to input part of vectors in data that it
    /// fills [0,1] space.
    /// </summary>
    public static void NormalizeCoordinates(IDataSet dataSet, Vector? input = null)
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
    /// <summary>
    /// Returns dataset that corresponds to given <paramref name="dataSet"/>, 
    /// that evenly distributed on n-dimensional(where n is input length) identity cube and 
    /// scaled/shifted by <paramref name="CoordinatesScale"/> and <paramref name="CoordinatesShift"/>
    /// </summary>
    /// <param name="approximationSize">How many dots to create in space?</param>
    public static IDataSet GetApproximationSet(int approximationSize, int InputVectorLength, float CoordinatesScale, Vector CoordinatesShift)
    {
        var Approximation = new DataSet(InputVectorLength, new List<IData>(approximationSize));
        for (int i = 0; i < approximationSize; i++)
        {
            var input = new float[InputVectorLength];
            Array.Fill(input, 0.5f);
            Approximation.Data.Add(new Data(input));
        }
        DataHelper.DistributeData(Approximation, CoordinatesScale, CoordinatesShift);
        return Approximation;
    }
    /// <summary>
    /// Computes L2 distance between two vectors on their subvectors.<br/>
    /// Subvectors is taken in such a way, that if any of two vectors have a missing value at
    /// given index it will not consider this index while computing distance.<br/>
    /// For example for vectors <see langword="(1,2,3,-2)"/> and <see langword="(-2,1,5,-4)"/> <br/>
    /// result will be <see langword="sqrt((2-1)^2+(5-3)^2)"/>
    /// </summary>
    public static float Distance(Vector n1, Vector n2)
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
}