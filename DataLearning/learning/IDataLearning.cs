using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IDataLearning
{
    DataSet DataSet{get;}
    float DiffusionTheta{get;set;}       
    float DiffusionCoefficient{get;set;}
    (IData data, int id) GetClosest(IData element);
    Vector Diffuse(Vector input);
    void DiffuseError(Vector input, Vector error);
    Vector DiffuseOnNClosest(Vector input, int n);
    void NormalizeCoordinates(Vector? input = null);
}