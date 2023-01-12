using System.Drawing;
using GraphSharp;

public class DataEdge : IEdge
{
    public DataEdge(DataNode source, DataNode target, VectorMask mask)
    {
        this.Source = source;
        this.Target = target;
        this.mask = mask;
        Weight = DataHelper.Distance(source.Data.Input,target.Data.Input,mask);
    }
    public int SourceId{get=>Source.Id;set=>throw new NotImplementedException();}
    public int TargetId{get=>Target.Id;set=>throw new NotImplementedException();}
    public double Weight {get;set;}
    public Color Color { 
        get => throw new NotImplementedException(); 
        set => throw new NotImplementedException(); 
    }
    public DataNode Source { get; }
    public DataNode Target { get; }

    private VectorMask mask;

    public IEdge Clone()
    {
        return new DataEdge(Source,Target,mask);
    }
}
