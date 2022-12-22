using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
#pragma warning disable
public abstract class Render
{
    public Canvas Canvas;
    public CanvasShapeDrawer CanvasDrawer;
    public int ApproximationSize;
    public int InputVectorLength;
    public DataSet DataSet;
    public AdaptiveDataSet AdaptiveDataSet;
    public DataSet Approximation;
    public DataLearning DataLearning;
    public int renderIntervalMilliseconds = 100;
    public int computeIntervalMilliseconds = 100;
    public float WindowSize = 1000f;
    public bool Pause = false;
    float ComputeTime = 0;
    public abstract Func<Vector, Vector> GetInput { get; }

    public virtual void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            Pause = !Pause;
        }
        if (e.Key == Key.R)
        {
            lock (DataLearning)
            {
                DataSet.Data.Clear();
                DataSet.Data.Clear();
            }
        }
        if (e.Key == Key.Up)
        {
            DataLearning.DiffusionTheta *= 2;
        }
        if (e.Key == Key.Down)
        {
            DataLearning.DiffusionTheta /= 2;
        }
        if (e.Key == Key.Left)
        {
            DataLearning.DiffusionCoefficient -= 1;
        }
        if (e.Key == Key.Right)
        {
            DataLearning.DiffusionCoefficient += 1;
        }
    }
    protected void ExpandApproximationSet()
    {
        for (int i = 0; i < this.Approximation.Data.Count; i++)
        {
            var d = this.Approximation.Data[i].Input;
            var vec = new DenseVector(InputVectorLength);
            vec.Fill(0.5f);
            vec.SetSubVector(0, d.Count, d);
            Approximation.Data[i].Input = vec;
        }
    }
    public abstract void PointerWheelChanged(object? sender, PointerWheelEventArgs e);
    public abstract void RenderStuff();
    public void RenderInterface()
    {
        var x = WindowSize + 10;
        CanvasDrawer.DrawText($"Diffusion theta is {DataLearning.DiffusionTheta}", new(x, 20), Color.Azure, 17);
        CanvasDrawer.DrawText($"Diffusion coefficient is {DataLearning.DiffusionCoefficient}", new(x, 40), Color.Azure, 17);
        CanvasDrawer.DrawText($"Compute time is {ComputeTime} sec", new(x, 60), Color.Azure, 17);
        CanvasDrawer.DrawText("Press Up/Down to change DiffusionTheta", new(x, 80), Color.Azure, 17);
        CanvasDrawer.DrawText("Press Left/Right to change DiffusionCoefficient", new(x, 100), Color.Azure, 17);
        CanvasDrawer.DrawText("Press R to clear data", new(x, 120), Color.Azure, 17);
        CanvasDrawer.DrawText("Press Space to hide data", new(x, 140), Color.Azure, 17);
        CanvasDrawer.DrawText("Use wheel to place elements", new(x, 160), Color.Azure, 17);

    }
    public async void ComputeStuff()
    {
        var watch = new Stopwatch();
        while (true)
        {
            watch.Restart();
            if (!Pause)
                lock (DataLearning)
                {
                    AdaptiveDataSet.Predict(Approximation,GetInput);
                    // AdaptiveDataSet.PredictOnNClosest(Approximation,GetInput,10);
                }
            ComputeTime = watch.ElapsedMilliseconds * 1f / 1000;
            await Task.Delay(computeIntervalMilliseconds);
        }
    }
}