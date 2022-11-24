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
   
    void Init()
    {

    }
    public Render1D(Canvas canvas)
    {
        Canvas = canvas;
        var drawer = new CanvasShapeDrawer(Canvas);
        this.CanvasDrawer = drawer;
        this.ApproximationSize = 256;
        this.InputVectorLength = 1;
        this.OutputVectorLength = 1;
        this.DataSet = new DataSet(InputVectorLength,OutputVectorLength);
        this.DataLearning = new DataLearning();
        this.Approximation = DataLearning.GetApproximationSet(ApproximationSize,DataSet,1,new DenseVector(new float[DataSet.InputVectorLength]));
    }

    void DrawData(DataSet dataSet, Func<Vector, Color> getColor)
    {
        var data = dataSet.Data;
        var count = data.Count;
        for (int i = 0; i < count; i++)
        {
            var n = data[i];
            var x = n.Input[0];
            var y = n.Output[0];
            CanvasDrawer.FillEllipse(WindowSize * new System.Numerics.Vector2(x, y), 10, 10, getColor(n.Output));
        }
    }

    public override void PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var pos = e.GetPosition(Canvas);
        var input = new DenseVector(new float[] { (float)pos.X / WindowSize});
        var output = new DenseVector(new float[] { (float)pos.Y / WindowSize});
        lock (DataLearning)
            DataSet.Data.Add(new Data(){Input = input, Output = output});
    }
    void DrawFunction(DataSet dataSet, Color color)
    {
        var data = dataSet.Data;
        lock(DataLearning)
        data.Aggregate((n1, n2) =>
        {
            var x1 = n1.Input.At(0);
            var x2 = n2.Input.At(0);
            var y1 = n1.Output.At(0);
            var y2 = n2.Output.At(0);
            var p1 = WindowSize * new System.Numerics.Vector2(x1, y1);
            var p2 = WindowSize * new System.Numerics.Vector2(x2, y2);
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