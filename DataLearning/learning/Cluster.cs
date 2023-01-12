using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Cluster : IData
{
    public Vector AverageVector{get;init;}
    public IEnumerable<IData> Elements { get; }
    public Vector Input { 
        get => AverageVector; 
        set => throw new NotImplementedException(); }

    public Cluster(IEnumerable<IData> clusterElements)
    {
        this.Elements = clusterElements;
        var size = clusterElements.First().Input.Count;
        AverageVector = new DenseVector(new float[size]);
        foreach(var e in clusterElements)
            AverageVector=(Vector)(e.Input+AverageVector);
    }
}