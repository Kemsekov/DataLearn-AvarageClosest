using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using MathNet.Numerics.LinearAlgebra.Single;
public class Render2D : Render
{
    void Init()
    {

    }
    Color ChosenColor;
    public Render2D(Canvas canvas)
    {
        Canvas = canvas;
        var drawer = new CanvasShapeDrawer(Canvas);
        this.CanvasDrawer = drawer;
        this.ApproximationSize = 32*32;
        this.InputVectorLength = 2;
        this.OutputVectorLength = 3;
        this.DataSet = new DataSet(InputVectorLength,OutputVectorLength);
        this.DataLearning = new DataLearning();
        this.Approximation = DataLearning.GetApproximationSet(ApproximationSize,DataSet,1,new DenseVector(new float[DataSet.InputVectorLength]));
        this.AdaptiveDataSet = new AdaptiveDataSet(DataSet,DataLearning,100);
        var n1 = Random.Shared.Next(256);
        var n2 = Random.Shared.Next(256);
        var n3 = Random.Shared.Next(256);
        ChosenColor = Color.FromArgb(n1, n2, n3);
    }
    public override void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.E)
        {
            var n1 = Random.Shared.Next(256);
            var n2 = Random.Shared.Next(256);
            var n3 = Random.Shared.Next(256);
            ChosenColor = Color.FromArgb(n1, n2, n3);
        }
        base.OnKeyDown(sender,e);
    }

    void DrawData(DataSet dataSet, Func<Vector, Color> getColor)
    {
        var data = dataSet.Data;
        for (int i = 0; i < data.Count; i++)
        {
            var n = data[i];
            var x = n.Input[0];
            var y = n.Input[1];
            CanvasDrawer.FillEllipse(WindowSize * new System.Numerics.Vector2(x, y), 10, 10, getColor(n.Output));
        }
    }
    public override void PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var pos = e.GetPosition(Canvas);
        var input = new DenseVector(new float[] { (float)pos.X / WindowSize, (float)pos.Y / WindowSize});
        var output = new DenseVector(new float[] { (float)(ChosenColor.R) / 255, (float)(ChosenColor.G) / 255, (float)(ChosenColor.B) / 255 });
        var toAdd = new Data(){Input = input, Output = output};
        lock (DataLearning){
            DataSet.Data.Add(toAdd);
            // AdaptiveDataSet.AddByMergingWithClosest(toAdd);
            // AdaptiveDataSet.AddByMergingWithPrediction(toAdd);
            // AdaptiveDataSet.AddByReplacingClosest(toAdd);
        }
    }
    public override async void RenderStuff()
    {
        Func<Vector, Color> colorPick = x => Color.FromArgb((int)(255 * x[0] % 256), (int)(255 * x[1] % 256), (int)(255 * x[2] % 256));
        while (true)
        {
        CanvasDrawer.Clear(System.Drawing.Color.Empty);
            DrawData(Approximation, colorPick);
            if(!Pause)
            DrawData(DataSet, colorPick);
            DrawColorPicker();
            RenderInterface();
            CanvasDrawer.Dispatch();
            await Task.Delay(renderIntervalMilliseconds);
        }
    }
    void DrawColorPicker(){
        var pos = new System.Numerics.Vector2(1.1f, 0.9f) * WindowSize;
        var size = 0.1f * WindowSize;
        CanvasDrawer.FillEllipse(pos,size,size, ChosenColor);
        CanvasDrawer.DrawText($"Press E to change color",new(pos.X-size+10,pos.Y-size+20),Color.Azure,17);

    }

}