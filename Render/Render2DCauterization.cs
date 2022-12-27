using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using MathNet.Numerics.LinearAlgebra.Single;
public class Render2DCauterization : Render
{
    public override Func<Vector, Vector> GetInput => x =>
    {
        x = (Vector)x.Clone();
        x[2] = -2;
        x[3] = -2;
        x[4] = -2;
        return x;
    };

    public int IterationsCount { get; private set; }

    public Func<Vector, Vector> ClusterInput = (Vector x) =>
        {
            x = (Vector)x.Clone();
            x[0] = -2;
            x[1] = -2;
            return x;
        };
    public Func<Vector, Vector> ClusterOutput = (Vector x) =>
    {
        x = (Vector)x.Clone();
        x[2] = -2;
        x[3] = -2;
        x[4] = -2;
        return x;
    };

    void Init()
    {
    }
    Color ChosenColor;
    public Render2DCauterization(Canvas canvas)
    {
        Canvas = canvas;
        var drawer = new CanvasShapeDrawer(Canvas);
        this.CanvasDrawer = drawer;
        this.ApproximationSize = 40 * 40;
        this.InputVectorLength = 5;
        this.DataSet = new DataSet(InputVectorLength);
        this.DataLearning = new DataLearning();
        this.Approximation = DataLearning.GetApproximationSet(ApproximationSize, 2, 1, new DenseVector(new float[2]));
        ExpandApproximationSet();
        this.AdaptiveDataSet = new AdaptiveDataSet(DataSet, DataLearning, 100);
        var n1 = Random.Shared.Next(256);
        var n2 = Random.Shared.Next(256);
        var n3 = Random.Shared.Next(256);
        ChosenColor = Color.FromArgb(n1, n2, n3);
        Init();
    }
    Color RandomColor()
    {
        var n1 = Random.Shared.Next(256);
        var n2 = Random.Shared.Next(256);
        var n3 = Random.Shared.Next(256);
        var color = Color.FromArgb(n1, n2, n3);
        return color;
    }
    public override void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.E)
        {
            ChosenColor = RandomColor();
        }
        if (e.Key == Key.Q)
        {
            FillSpace(300);
        }
        if (e.Key == Key.S)
        {
            ShufflePositions();
        }
        if (e.Key == Key.W)
        {
            // Cluster();
            Cluster2();
        }
        base.OnKeyDown(sender, e);
    }

    private void FillSpace(int n)
    {
        ChosenColor = ShiftColor(ChosenColor, 0.3f);
        DataSet.Data.Clear();
        var colors = new[]{
                Color.Red,
                Color.Blue,
                Color.Green,
                Color.Yellow,
                Color.Orange,
                Color.Aqua,
                Color.Violet
            };
        var randomColor = () =>
        {
            var choose = Random.Shared.Next(colors.Length);
            return ShiftColor(colors[choose], 1f);
        };
        var toAdd = Enumerable.Range(0, n).Select(x =>
        {
            var color = randomColor();
            var v = new DenseVector(new float[5] { 0, 0, color.R * 1.0f / 255, color.G * 1.0f / 255, color.B * 1.0f / 255 });
            return v;
        });
        foreach (var i in toAdd)
        {
            i[0] = Random.Shared.NextSingle();
            i[1] = Random.Shared.NextSingle();
            DataSet.Data.Add(new Data(i));
        }
    }

    void ShufflePositions()
    {
        foreach (var element in DataSet.Data)
        {
            element.Input[0] = Random.Shared.NextSingle();
            element.Input[1] = Random.Shared.NextSingle();
        }
    }
    Color ShiftColor(Color color, float percent)
    {
        int bound(int value)
        {
            if (value > 255) return 255;
            if (value < 0) return 0;
            return value;
        }
        var n1 = Random.Shared.NextSingle() * percent + 1f - percent / 2;
        var n2 = Random.Shared.NextSingle() * percent + 1f - percent / 2;
        var n3 = Random.Shared.NextSingle() * percent + 1f - percent / 2;

        var r = (int)(color.R * n1);
        var g = (int)(color.G * n2);
        var b = (int)(color.B * n3);

        r = bound(r);
        g = bound(g);
        b = bound(b);

        return Color.FromArgb(r, g, b);
    }

    void DrawData(DataSet dataSet, Func<Vector, Color> getColor, int size = 10)
    {
        var data = dataSet.Data;

        for (int i = 0; i < data.Count; i++)
        {
            var n = data[i];
            var x = n.Input[0];
            var y = n.Input[1];
            CanvasDrawer.FillEllipse(WindowSize * new System.Numerics.Vector2(((float)x), ((float)y)), size, size, getColor(n.Input));
        }
    }
    public override void PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var getInput = (Vector x) =>
        {
            x = (Vector)x.Clone();
            x[0] = -2;
            x[1] = -2;
            return x;
        };
        var getOutput = (Vector x) =>
        {
            x = (Vector)x.Clone();
            x[2] = -2;
            x[3] = -2;
            x[4] = -2;
            return x;
        };
        var pos = e.GetPosition(Canvas);
        var input = new DenseVector(new float[] { (float)pos.X / WindowSize, (float)pos.Y / WindowSize, (float)(ChosenColor.R) / 255, (float)(ChosenColor.G) / 255, (float)(ChosenColor.B) / 255 });
        var toAdd = new Data() { Input = input };
        lock (DataLearning)
        {
            DataSet.Data.Add(toAdd);
            Cluster(DataSet.Data.Count - 1, ClusterInput, ClusterOutput);
            // AdaptiveDataSet.AddByMergingWithClosest(toAdd);
        }
    }
    public override async void RenderStuff()
    {
        Func<Vector, Color> colorPick = x => Color.FromArgb((int)Math.Abs(255 * x[2] % 256), (int)Math.Abs(255 * x[3] % 256), (int)Math.Abs(255 * x[4] % 256));
        while (true)
        {
            CanvasDrawer.Clear(System.Drawing.Color.Empty);
            if (!Pause)
                DrawData(Approximation, colorPick);
            DrawData(DataSet, colorPick, 12);
            DrawColorPicker();
            RenderInterface();
            CanvasDrawer.Dispatch();
            await Task.Delay(renderIntervalMilliseconds);
        }
    }
    public override void Compute()
    {
    }
    bool clustering = false;
    public void Cluster2()
    {
        if(clustering) return;
        clustering = true;
        //итерируемся по данным
        //аутпут заполнен случайно
        //для данного элемента input/output вектора
        //предсказываем его output через input
        //считаем ошибку как разность output элемента от предсказания
        //диффюзим ошибку и идём дальше
        //так делаем несколько итераций
        AdaptiveDataSet.Cluster(ClusterInput,ClusterOutput);
        clustering = false;
    }
    public void Cluster()
    {

        // var clone = DataSet.Data.ToList();
        var data = DataSet.Data;
        var customList = new CustomList<IData>(data);
        AdaptiveDataSet.DataSet.Data = customList;
        var count = data.Count;
        // data.Clear();
        for (int i = 0; i < count; i++)
        {
            customList.FreezeCount(i + 1);

            if (!Cluster(i, ClusterInput, ClusterOutput))
            {
            }
        }
        AdaptiveDataSet.DataSet.Data = data;
    }
    bool Cluster(int i, Func<Vector, Vector> getInput, Func<Vector, Vector> getOutput)
    {
        var data = DataSet.Data;
        var vector = (Vector)data[i].Input;
        var input = getInput(vector);
        var output = getOutput(vector);

        var result = AdaptiveDataSet.PredictPure(input);
        var outputResult = AdaptiveDataSet.PredictPure(getOutput(result));

        var distOnInput1 = DataLearning.Distance(input, outputResult);
        var distOnInput2 = DataLearning.Distance(input, result);
        // System.Console.WriteLine(distOnInput);
        if (distOnInput1 > distOnInput2)
        {
            data[i].Input = AdaptiveDataSet.FillMissingValues(input, result);
            return true;
        }
        return false;
    }
    void DrawColorPicker()
    {
        var pos = new System.Numerics.Vector2(1.1f, 0.9f) * WindowSize;
        var size = 0.1f * WindowSize;
        CanvasDrawer.FillEllipse(pos, size, size, ChosenColor);
        CanvasDrawer.DrawText($"Press W to cluster", new(pos.X - size + 10, pos.Y - size - 40), Color.Azure, 17);
        CanvasDrawer.DrawText($"Press S to shuffle elements positions", new(pos.X - size + 10, pos.Y - size - 20), Color.Azure, 17);
        CanvasDrawer.DrawText($"Press Q to fill 300 elements", new(pos.X - size + 10, pos.Y - size), Color.Azure, 17);
        CanvasDrawer.DrawText($"Press E to change color", new(pos.X - size + 10, pos.Y - size + 20), Color.Azure, 17);

    }

}