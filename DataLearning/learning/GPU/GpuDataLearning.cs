public class GpuDataLearning : IDataLearning, IDisposable
{
    public GpuDataLearning(int inputVectorLength, int maxVectorsCount){
        this.Init = new GpuDataLearningInitialization(inputVectorLength,maxVectorsCount); //somehow inits from inputVectorLength and maxVectorsCount
                             //and creates arrays required size of gpu 
                             //as StorageBuffer and IndicesBuffer
        this.DataSet = new GpuDataSet(inputVectorLength,Init.StorageBuffer, Init.IndicesBuffer);

        //each of following classes will have full access to vector data
        //stored in memory and will update/change it
        this.Diffusor = new Diffusor(Init,"learning/GPU/shaders/diffuse.comp");
        // this.ErrorDiffusor = new ErrorDiffusor(Init);
        // this.NClosestDiffusor = new NClosestDiffusor(Init);
        // this.ClosestGetter = new ClosestGetter(Init);
        // this.Normalizer = new Normalizer(Init);
    }

    public GpuDataLearningInitialization Init { get; }
    public GpuDataSet DataSet{get;}
    public Diffusor Diffusor { get; }
    public float DiffusionTheta{get;set;} = 0.001f;
    public float DiffusionCoefficient{get;set;} = 2;

    IDataSet IDataLearning.DataSet => DataSet;

    public Vector Diffuse(Vector input)
    {
        return Diffusor.Compute(input);
    }

    public void DiffuseError(Vector input, Vector error)
    {
        // return ErrorDiffusor.Compute(input,error);
        throw new NotImplementedException();
    }

    public Vector DiffuseOnNClosest(Vector input, int n)
    {
        // return NClosestDiffusor.Compute(input,n);
        throw new NotImplementedException();
    }

    public (IData data, int id) GetClosest(IData element)
    {
        // return ClosestGetter.Compute(element.Input);
        throw new NotImplementedException();
    }

    public void NormalizeCoordinates(Vector? input = null)
    {
        // Normalizer.Compute(input);
        throw new NotImplementedException();
        // var t = new DataLearning(this.DataSet);
    }

    public void Dispose()
    {
        Init.Dispose();//will clean all memory from RAM and VRAM
    }
}