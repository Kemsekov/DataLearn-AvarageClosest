using GraphSharp.Graphs;

public class DataConfiguration : IGraphConfiguration<DataNode, DataEdge>
{
    private VectorMask mask;

    public DataConfiguration(VectorMask mask)
    {
        this.mask = mask;
    }
    public Random Rand {get;set;} = new Random();

    public DataEdge CreateEdge(DataNode source, DataNode target)
    {
        return new(source,target,mask);
    }

    public IEdgeSource<DataEdge> CreateEdgeSource()
    {
        return new DefaultEdgeSource<DataEdge>();
    }

    public DataNode CreateNode(int nodeId)
    {
        return new DataNode(nodeId,new Data(new DenseVector(new float[0])));
    }

    public INodeSource<DataNode> CreateNodeSource()
    {
        return new DefaultNodeSource<DataNode>();
    }
}
