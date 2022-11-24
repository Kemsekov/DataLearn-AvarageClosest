using System.Linq;
using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using System.Diagnostics;
using Avalonia.Input;

namespace Test;
public partial class MainWindow : Window
{
    public Canvas Canvas { get; }

    event Action<KeyEventArgs> OnKeyDownEvent;
    public MainWindow()
    {
        InitializeComponent();
        this.Canvas = this.FindControl<Canvas>("MyCanvas");
        OnKeyDownEvent = e=>{};
        RunRenderAfterAppActivation();
    }

    async void RunRenderAfterAppActivation(){
        #pragma warning disable
        while(!IsActive) await Task.Delay(10);
        var r = new Render2D(Canvas);
        // var r = new Render1D(Canvas);
        r.RenderStuff();
        Task.Run(r.ComputeStuff);
        this.KeyDown += r.OnKeyDown;
        this.PointerWheelChanged += r.PointerWheelChanged;
        // this.PointerPressed += r.PointerPressed;
        #pragma warning enable

    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
    }
}