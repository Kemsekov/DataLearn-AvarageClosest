using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using MathNet.Numerics.LinearAlgebra.Single;
namespace Test;
public class Render2D
{
    public Canvas Canvas;
    public CanvasShapeDrawer CanvasDrawer { get; }
    public int ApproximationSize { get; }
    public int InputVectorLength { get; }
    public int OutputVectorLength { get; }
    public DataSet DataSet { get; }
    public DataSet Approximation { get; }
    public DataLearning DataLearning { get; }
    int renderIntervalMilliseconds = 100;
    int computeIntervalMilliseconds = 100;
    float WindowSize = 1000f;
    void Init()
    {

    }
    bool Pause = false;

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
        System.Console.WriteLine(this.Approximation.Data.Count);
        var n1 = Random.Shared.Next(256);
        var n2 = Random.Shared.Next(256);
        var n3 = Random.Shared.Next(256);
        ChosenColor = Color.FromArgb(n1, n2, n3);
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
    public void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            Pause = !Pause;
        }
        if (e.Key == Key.E)
        {
            var n1 = Random.Shared.Next(256);
            var n2 = Random.Shared.Next(256);
            var n3 = Random.Shared.Next(256);
            ChosenColor = Color.FromArgb(n1, n2, n3);
        }
        if (e.Key == Key.R)
        {
            lock (DataLearning)
            {
                DataSet.Data.Clear();
            }
        }
        if (e.Key == Key.Up)
        {
            DataLearning.DiffusionTheta *= 2;
            System.Console.WriteLine($"Diffusion theta is {DataLearning.DiffusionTheta}");
        }
        if (e.Key == Key.Down)
        {
            DataLearning.DiffusionTheta /= 2;
            System.Console.WriteLine($"Diffusion theta is {DataLearning.DiffusionTheta}");
        }

    }
    public void PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var pos = e.GetPosition(Canvas);
        var input = new DenseVector(new float[] { (float)pos.X / WindowSize, (float)pos.Y / WindowSize});
        var output = new DenseVector(new float[] { (float)(ChosenColor.R) / 255, (float)(ChosenColor.G) / 255, (float)(ChosenColor.B) / 255 });
        lock (DataLearning)
            DataSet.Data.Add(new Data(){Input = input, Output = output});
    }
    Color ChosenColor;
    public async void RenderStuff()
    {
        Func<Vector, Color> colorPick = x => Color.FromArgb((int)(255 * x[0] % 256), (int)(255 * x[1] % 256), (int)(255 * x[2] % 256));
        while (true)
        {
            CanvasDrawer.Clear(System.Drawing.Color.Empty);
            DrawData(Approximation, colorPick);
            if(!Pause)
            DrawData(DataSet, colorPick);
            CanvasDrawer.FillEllipse(new System.Numerics.Vector2(1.1f, 0.1f) * WindowSize, 0.1f * WindowSize, 0.1f * WindowSize, ChosenColor);
            CanvasDrawer.Dispatch();
            await Task.Delay(renderIntervalMilliseconds);
        }
    }
    public async void ComputeStuff()
    {
        var watch = new Stopwatch();
        int counter = 0;
        int setps = 10;
        while (true)
        {
            if(counter%setps==0)
            watch.Restart();
            if(!Pause)
            lock (DataLearning){

                DataLearning.Diffuse(DataSet,Approximation,2);
            }
            if(counter%setps==0){
                System.Console.WriteLine($"Compute approximation {watch.ElapsedMilliseconds/10}");
                counter = 0;
            }
            counter++;
            await Task.Delay(computeIntervalMilliseconds);
        }
    }
}