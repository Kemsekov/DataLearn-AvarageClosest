using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using GraphSharp;

public class DataNode : INode
{
    public IData Data{get;}
    public int Id{get;set;}
    public Color Color { 
        get => throw new NotImplementedException(); 
        set => throw new NotImplementedException(); 
    }
    public double Weight { 
        get => throw new NotImplementedException(); 
        set => throw new NotImplementedException(); 
    }
    public DataNode(int id, IData data)
    {
        Id = id;
        Data = data;
    }
    public INode Clone()
    {
        return new DataNode(Id,Data);
    }
    public bool Equals(INode? other)
    {
        return Id==other?.Id;
    }
}