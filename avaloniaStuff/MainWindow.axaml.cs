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
using Newtonsoft.Json.Linq;
using System.IO;

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
        dynamic settings = JObject.Parse(File.ReadAllText("settings.json"));
        var mode = settings.ModeSelected;

        while(!IsActive) await Task.Delay(10);
        Render r = default;
        if(mode=="2D")
        r = new Render2D(Canvas);
        if(mode=="1D")
        r = new Render1D(Canvas);
        if(r is null) throw new ArgumentException("Choose 1D or 2D as ModeSelected in settings.json");
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