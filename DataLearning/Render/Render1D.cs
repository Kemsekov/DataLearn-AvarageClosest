using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using MathNet.Numerics.LinearAlgebra.Single;
public class Render1D : Render
{
    public override Func<Vector, Vector> GetInput => x=>{
        x = (Vector)x.Clone();
        x[1] = -2;
        return x;
    };
    void Init()
    {

    }
    public Render1D(Canvas canvas)
    {
        Canvas = canvas;
        var drawer = new CanvasShapeDrawer(Canvas);
        this.CanvasDrawer = drawer;
        this.ApproximationSize = 256;
        this.InputVectorLength = 2;
        this.DataSet = new DataSet(InputVectorLength);
        this.DataLearning = new DataLearning(DataSet,new VectorMask((x,index)=>x>=-1));
        this.AdaptiveDataSet = new AdaptiveDataSet(DataLearning,20);
        this.Approximation = DataHelper.GetApproximationSet(ApproximationSize,1,1,new DenseVector(new float[1]));
        ExpandApproximationSet();
    }



    void DrawData(IDataSet dataSet, Func<Vector, Color> getColor)
    {
        var data = dataSet.Data;
        var count = data.Count;
        for (int i = 0; i < count; i++)
        {
            var n = data[i];
            var x = n.Input[0];
            var y = n.Input[1];
            CanvasDrawer.FillEllipse(WindowSize * new System.Numerics.Vector2(((float)x), ((float)y)), 10, 10, getColor(n.Input));
        }
    }

    public override void PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var pos = e.GetPosition(Canvas);
        var input = new DenseVector(new float[] { (float)pos.X / WindowSize, (float)pos.Y / WindowSize});
        lock (DataLearning){
            // AdaptiveDataSet.AddByMergingWithClosest(new Data(){Input = input});
            DataSet.Data.Add(new Data(){Input = input});
        }
    }
    void DrawFunction(IDataSet dataSet, Color color)
    {
        var data = dataSet.Data;
        lock(DataLearning)
        data.Aggregate((n1, n2) =>
        {
            var x1 = n1.Input.At(0);
            var x2 = n2.Input.At(0);
            var y1 = n1.Input.At(1);
            var y2 = n2.Input.At(1);
            var p1 = WindowSize * new System.Numerics.Vector2(((float)x1), ((float)y1));
            var p2 = WindowSize * new System.Numerics.Vector2(((float)x2), ((float)y2));
            CanvasDrawer.DrawLine(p1, p2, color, 3);
            return n2;
        });
    }
    public override async void RenderStuff()
    {
        while (true)
        {
            CanvasDrawer.Clear(System.Drawing.Color.Empty);
            DrawFunction(Approximation,Color.Blue);
            if(!Pause)
            DrawData(DataSet, x => Color.Red);
            RenderInterface();
            CanvasDrawer.Dispatch();
            await Task.Delay(renderIntervalMilliseconds);
        }
    }
}