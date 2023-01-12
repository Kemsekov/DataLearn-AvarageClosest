using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphSharp.Graphs;

public class DataGraph : Graph<DataNode,DataEdge>
{
    public DataGraph(DataConfiguration configuration, IEnumerable<IData> dataSet) : base(configuration)
    {
        this.SetSources(dataSet.Select((data,index)=>new DataNode(index,data)));
    }
}