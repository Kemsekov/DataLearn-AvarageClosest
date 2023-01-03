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
    public override Func<Vector, Vector> GetInput => x=>{
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
    public Render2D(Canvas canvas)
    {
        Canvas = canvas;
        var drawer = new CanvasShapeDrawer(Canvas);
        this.CanvasDrawer = drawer;
        this.ApproximationSize = 40*40;
        this.InputVectorLength = 5;
        this.DataSet = new DataSet(InputVectorLength);
        this.DataLearning = new DataLearning(DataSet);
        this.Approximation = DataHelper.GetApproximationSet(ApproximationSize,2,1,new DenseVector(new float[2]));
        ExpandApproximationSet();
        
        this.AdaptiveDataSet = new AdaptiveDataSet(DataLearning,100);
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
        if (e.Key == Key.Q)
        {
            ShiftColor();
        }
        base.OnKeyDown(sender,e);
    }

    void ShiftColor()
    {
        int bound(int value){
            if(value>255) return 255;
            if(value<0) return 0;
            return value;
        }
        var n1 = Random.Shared.NextSingle() * 0.4f + 0.80f;
        var n2 = Random.Shared.NextSingle() * 0.4f + 0.80f;
        var n3 = Random.Shared.NextSingle() * 0.4f + 0.80f;

        var r = (int)(ChosenColor.R * n1);
        var g = (int)(ChosenColor.G * n2);
        var b = (int)(ChosenColor.B * n3);

        r = bound(r);
        g = bound(g);
        b = bound(b);

        ChosenColor = Color.FromArgb(r,g,b);
    }

    void DrawData(IDataSet dataSet, Func<Vector, Color> getColor, int size = 10)
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
        var pos = e.GetPosition(Canvas);
        var input = new DenseVector(new float[] { (float)pos.X / WindowSize, (float)pos.Y / WindowSize, (float)(ChosenColor.R) / 255, (float)(ChosenColor.G) / 255, (float)(ChosenColor.B) / 255});
        var toAdd = new Data(){Input = input};
        lock (DataLearning){
            DataSet.Data.Add(toAdd);
            // AdaptiveDataSet.AddByMergingWithClosest(toAdd);
        }
    }
    public override async void RenderStuff()
    {
        Func<Vector, Color> colorPick = x => Color.FromArgb((int)(255 * x[2] % 256), (int)(255 * x[3] % 256), (int)(255 * x[4] % 256));
        while (true)
        {
        CanvasDrawer.Clear(System.Drawing.Color.Empty);
            if(!Pause)
            DrawData(Approximation, colorPick,20);
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
        CanvasDrawer.DrawText($"Press Q to shift color a bit",new(pos.X-size+10,pos.Y-size),Color.Azure,17);
        CanvasDrawer.DrawText($"Press E to change color",new(pos.X-size+10,pos.Y-size+20),Color.Azure,17);

    }

}