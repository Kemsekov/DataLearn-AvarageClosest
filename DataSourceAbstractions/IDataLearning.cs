using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IDataLearning
{
    float DiffusionTheta{get;set;}       
    float DiffusionCoefficient{get;set;}
    (IData data, int id) GetClosest(DataSet dataSet, IData element);
    Vector Diffuse(DataSet data, Vector input);
    void DiffuseError(DataSet data, Vector input, Vector error);
    Vector DiffuseOnNClosest(DataSet data, Vector input, int n);
    void NormalizeCoordinates(DataSet dataSet, Vector? input = null);
}