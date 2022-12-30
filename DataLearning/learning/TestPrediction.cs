using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public class TestPrediction
{
    public Vector Difference {get;set;}
    public Vector AbsDifference {get;set;}
    public Vector MaxDifference {get;set;}
    public float AbsError => AbsDifference.Sum();
    public float Error => Difference.PointwiseAbs().Sum();
    public float MaxError => MaxDifference.PointwiseAbs().Sum();
    public TestPrediction(Vector difference, Vector absDifferencem, Vector maxDifference)
    {
        Difference = difference;
        AbsDifference = absDifferencem;
        MaxDifference = maxDifference;
    }
}