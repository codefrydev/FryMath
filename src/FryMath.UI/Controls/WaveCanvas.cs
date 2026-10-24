using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FryMath.Core.Waves;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class WaveCanvas : Control
{
    public static readonly StyledProperty<WaveSimulationMode> SimulationModeProperty =
        AvaloniaProperty.Register<WaveCanvas, WaveSimulationMode>(nameof(SimulationMode), WaveSimulationMode.ChladniPlate);

    public static readonly StyledProperty<int> ModeNProperty =
        AvaloniaProperty.Register<WaveCanvas, int>(nameof(ModeN), 3);

    public static readonly StyledProperty<int> ModeMProperty =
        AvaloniaProperty.Register<WaveCanvas, int>(nameof(ModeM), 5);

    public static readonly StyledProperty<double> DampingProperty =
        AvaloniaProperty.Register<WaveCanvas, double>(nameof(Damping), 0.005);

    public static readonly StyledProperty<bool> ShowSandParticlesProperty =
        AvaloniaProperty.Register<WaveCanvas, bool>(nameof(ShowSandParticles), true);

    public WaveSimulationMode SimulationMode
    {
        get => GetValue(SimulationModeProperty);
        set => SetValue(SimulationModeProperty, value);
    }

    public int ModeN
    {
        get => GetValue(ModeNProperty);
        set => SetValue(ModeNProperty, value);
    }

    public int ModeM
    {
        get => GetValue(ModeMProperty);
        set => SetValue(ModeMProperty, value);
    }

    public double Damping
    {
        get => GetValue(DampingProperty);
        set => SetValue(DampingProperty, value);
    }

    public bool ShowSandParticles
    {
        get => GetValue(ShowSandParticlesProperty);
        set => SetValue(ShowSandParticlesProperty, value);
    }

    private readonly Wave2DEngine _engine = new(450);
    private readonly DispatcherTimer _timer;

    // Bitmap for ripple tank / field heatmap
    private WriteableBitmap? _bitmap;
    private const int BmpSize = 100;
    private readonly uint[] _pixelBuffer = new uint[BmpSize * BmpSize];

    // Hover state
    private bool _isHovered;
    private Point _hoverScreen;
    private double _hoverX;
    private double _hoverY;

    static WaveCanvas()
    {
        AffectsRender<WaveCanvas>(
            SimulationModeProperty,
            ModeNProperty,
            ModeMProperty,
            DampingProperty,
            ShowSandParticlesProperty);
    }

    public WaveCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += (s, e) =>
        {
            _engine.Mode = SimulationMode;
            _engine.ModeN = ModeN;
            _engine.ModeM = ModeM;
            _engine.Damping = Damping;
            _engine.StepSimulation(0.016);
            InvalidateVisual();
        };
        _timer.Start();
    }

    public void DropPluck(double mathX, double mathY)
    {
        int gx = (int)Math.Clamp((mathX + 1.0) * 0.5 * Wave2DEngine.GridSize, 1, Wave2DEngine.GridSize - 2);
        int gy = (int)Math.Clamp((mathY + 1.0) * 0.5 * Wave2DEngine.GridSize, 1, Wave2DEngine.GridSize - 2);
        _engine.DropImpulse(gx, gy, 4.0, 4);
    }

    public void ResetSand()
    {
        _engine.ResetParticles();
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SimulationModeProperty)
        {
            if (SimulationMode == WaveSimulationMode.InteractiveRippleTank)
            {
                _engine.DropImpulse(Wave2DEngine.GridSize / 3, Wave2DEngine.GridSize / 2, 4.5, 4);
                _engine.DropImpulse(2 * Wave2DEngine.GridSize / 3, Wave2DEngine.GridSize / 2, 3.8, 4);
                for (int i = 0; i < 28; i++) _engine.StepSimulation(0.016);
            }
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var math = ScreenToMath(pt);
        DropPluck(math.X, math.Y);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        _isHovered = true;
        _hoverScreen = pt;
        var math = ScreenToMath(pt);
        _hoverX = math.X;
        _hoverY = math.Y;

        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsLeftButtonPressed && SimulationMode == WaveSimulationMode.InteractiveRippleTank)
        {
            DropPluck(math.X, math.Y);
        }

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHovered = false;
        InvalidateVisual();
    }

    private Point MathToScreen(double x, double y)
    {
        double size = Math.Min(Bounds.Width, Bounds.Height) * 0.85;
        double ox = Bounds.Width * 0.5;
        double oy = Bounds.Height * 0.5;
        return new Point(ox + x * (size * 0.5), oy - y * (size * 0.5));
    }

    private Point ScreenToMath(Point screen)
    {
        double size = Math.Min(Bounds.Width, Bounds.Height) * 0.85;
        double ox = Bounds.Width * 0.5;
        double oy = Bounds.Height * 0.5;
        return new Point((screen.X - ox) / (size * 0.5), -(screen.Y - oy) / (size * 0.5));
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        var tokens = MathThemeTokens.DarkElectric;
        context.FillRectangle(tokens.SurfaceBrush, new Rect(0, 0, w, h));

        double plateSize = Math.Min(w, h) * 0.85;
        double plateLeft = (w - plateSize) * 0.5;
        double plateTop = (h - plateSize) * 0.5;
        var plateRect = new Rect(plateLeft, plateTop, plateSize, plateSize);

        if (SimulationMode == WaveSimulationMode.InteractiveRippleTank)
        {
            RenderRippleTank(context, plateRect, tokens);
        }
        else if (SimulationMode == WaveSimulationMode.CircularDrum)
        {
            RenderCircularDrum(context, plateRect, tokens);
        }
        else
        {
            RenderChladniPlate(context, plateRect, tokens);
        }

        // Hover Probe HUD
        if (_isHovered)
        {
            RenderProbeHud(context, tokens);
        }

        // Footer navigation tip
        RenderFooter(context, tokens, w, h);
    }

    private void RenderChladniPlate(DrawingContext context, Rect plateRect, MathThemeTokens tokens)
    {
        // 1. Resonating plate body (brushed carbon plate)
        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(240, 16, 20, 32)),
            new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 229, 255)), 2),
            plateRect, 12, 12);

        // 2. Faint Nodal Line Guide Grid / Heatmap
        UpdateChladniBitmap();
        if (_bitmap != null)
        {
            context.DrawImage(_bitmap, plateRect);
        }

        // 3. Golden Sand Particles
        if (ShowSandParticles)
        {
            var sandBrush = new SolidColorBrush(Color.FromArgb(230, 255, 215, 64)); // Brilliant gold
            var sandPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 179, 0)), 1);

            foreach (var p in _engine.SandParticles)
            {
                var screenPt = MathToScreen(p.X, p.Y);
                context.DrawEllipse(sandBrush, sandPen, screenPt, 2.2, 2.2);
            }
        }
    }

    private void RenderCircularDrum(DrawingContext context, Rect plateRect, MathThemeTokens tokens)
    {
        double radius = plateRect.Width * 0.5;
        var center = new Point(plateRect.X + radius, plateRect.Y + radius);

        context.DrawEllipse(
            new SolidColorBrush(Color.FromArgb(240, 16, 20, 32)),
            new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 0, 122)), 3),
            center, radius, radius);

        UpdateCircularDrumBitmap();
        if (_bitmap != null)
        {
            context.DrawImage(_bitmap, plateRect);
        }

        if (ShowSandParticles)
        {
            var sandBrush = new SolidColorBrush(Color.FromArgb(230, 255, 215, 64));
            var sandPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 179, 0)), 1);

            foreach (var p in _engine.SandParticles)
            {
                if (p.X * p.X + p.Y * p.Y <= 0.95)
                {
                    var screenPt = MathToScreen(p.X, p.Y);
                    context.DrawEllipse(sandBrush, sandPen, screenPt, 2.2, 2.2);
                }
            }
        }
    }

    private void RenderRippleTank(DrawingContext context, Rect plateRect, MathThemeTokens tokens)
    {
        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(240, 10, 18, 30)),
            new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 229, 255)), 2),
            plateRect, 10, 10);

        UpdateRippleBitmap();
        if (_bitmap != null)
        {
            context.DrawImage(_bitmap, plateRect);
        }
    }

    private void EnsureBitmap()
    {
        if (_bitmap == null)
        {
            _bitmap = new WriteableBitmap(
                new PixelSize(BmpSize, BmpSize),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
        }
    }

    private void UpdateChladniBitmap()
    {
        EnsureBitmap();
        for (int y = 0; y < BmpSize; y++)
        {
            double my = 1.0 - (y / (double)BmpSize) * 2.0;
            int row = y * BmpSize;
            for (int x = 0; x < BmpSize; x++)
            {
                double mx = (x / (double)BmpSize) * 2.0 - 1.0;
                double val = _engine.EvaluateChladni(mx, my);
                double absVal = Math.Abs(val);

                // Near nodal lines (absVal ~ 0), glow cyan
                if (absVal < 0.12)
                {
                    double glow = 1.0 - (absVal / 0.12);
                    byte a = (byte)(glow * 140);
                    _pixelBuffer[row + x] = ((uint)a << 24) | 0x00E5FF;
                }
                else
                {
                    // Dark plate with subtle amplitude shading
                    byte blue = (byte)Math.Clamp(30 + absVal * 25, 0, 70);
                    _pixelBuffer[row + x] = 0x80081020 | ((uint)blue);
                }
            }
        }

        CopyBufferToBitmap();
    }

    private void UpdateCircularDrumBitmap()
    {
        EnsureBitmap();
        for (int y = 0; y < BmpSize; y++)
        {
            double my = 1.0 - (y / (double)BmpSize) * 2.0;
            int row = y * BmpSize;
            for (int x = 0; x < BmpSize; x++)
            {
                double mx = (x / (double)BmpSize) * 2.0 - 1.0;
                double rSq = mx * mx + my * my;
                if (rSq > 1.0)
                {
                    _pixelBuffer[row + x] = 0x00000000;
                    continue;
                }

                double val = _engine.EvaluateCircularDrum(mx, my);
                double absVal = Math.Abs(val);

                if (absVal < 0.08)
                {
                    double glow = 1.0 - (absVal / 0.08);
                    byte a = (byte)(glow * 160);
                    _pixelBuffer[row + x] = ((uint)a << 24) | 0xFF007A;
                }
                else
                {
                    byte mag = (byte)Math.Clamp(20 + absVal * 40, 0, 80);
                    _pixelBuffer[row + x] = 0x90100820 | ((uint)mag << 8);
                }
            }
        }

        CopyBufferToBitmap();
    }

    private void UpdateRippleBitmap()
    {
        EnsureBitmap();
        for (int y = 0; y < BmpSize; y++)
        {
            double my = 1.0 - (y / (double)BmpSize) * 2.0;
            int row = y * BmpSize;
            for (int x = 0; x < BmpSize; x++)
            {
                double mx = (x / (double)BmpSize) * 2.0 - 1.0;
                double u = _engine.GetDisplacement(mx, my);

                // Map displacement to water reflection colors:
                // Equil (u == 0): Deep navy water (r=12, g=22, b=45)
                // Crests (u > 0): Electric cyan to brilliant white highlights
                // Troughs (u < 0): Deep abyss indigo
                if (u >= 0)
                {
                    double t = Math.Clamp(u * 0.5, 0.0, 1.0);
                    byte r = (byte)(12 + t * 243);
                    byte g = (byte)(22 + t * 233);
                    byte b = (byte)(45 + t * 210);
                    _pixelBuffer[row + x] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
                }
                else
                {
                    double t = Math.Clamp(-u * 0.5, 0.0, 1.0);
                    byte r = (byte)(12 * (1.0 - t * 0.7));
                    byte g = (byte)(22 * (1.0 - t * 0.8));
                    byte b = (byte)(45 * (1.0 - t * 0.5));
                    _pixelBuffer[row + x] = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }
        }

        CopyBufferToBitmap();
    }

    private void CopyBufferToBitmap()
    {
        if (_bitmap == null) return;
        using var fb = _bitmap.Lock();
        unsafe
        {
            uint* dest = (uint*)fb.Address;
            int stride = fb.RowBytes / 4;
            for (int y = 0; y < BmpSize; y++)
            {
                int src = y * BmpSize;
                uint* row = dest + (y * stride);
                for (int x = 0; x < BmpSize; x++)
                {
                    row[x] = _pixelBuffer[src + x];
                }
            }
        }
    }

    private void RenderProbeHud(DrawingContext context, MathThemeTokens tokens)
    {
        double hudX = 20;
        double hudY = 20;
        double hudW = 280;
        double hudH = 90;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(220, 13, 16, 28)),
            new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 229, 255)), 1),
            hudRect, 8, 8);

        double u = _engine.GetDisplacement(_hoverX, _hoverY);
        string modeStr = SimulationMode switch
        {
            WaveSimulationMode.ChladniPlate => $"Chladni Plate Eigenmode (n={ModeN}, m={ModeM})",
            WaveSimulationMode.CircularDrum => $"Circular Drum Bessel Mode (n={ModeN}, m={ModeM})",
            WaveSimulationMode.InteractiveRippleTank => "2D Wave Equation PDE (Leapfrog)",
            _ => "Wave Simulation"
        };

        var textTitle = new FormattedText(modeStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif", FontStyle.Normal, FontWeight.SemiBold), 11,
            new SolidColorBrush(Color.FromRgb(0, 229, 255)));
        context.DrawText(textTitle, new Point(hudX + 12, hudY + 10));

        var textCoords = new FormattedText($"Position: ({_hoverX:F2}, {_hoverY:F2})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 11,
            new SolidColorBrush(Color.FromRgb(220, 220, 230)));
        context.DrawText(textCoords, new Point(hudX + 12, hudY + 32));

        var textU = new FormattedText($"Displacement: u(x,y,t) = {u:F4}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif", FontStyle.Normal, FontWeight.Bold), 12,
            new SolidColorBrush(Color.FromRgb(255, 179, 0)));
        context.DrawText(textU, new Point(hudX + 12, hudY + 52));

        string nodalStatus = Math.Abs(u) < 0.05 ? "Nodal Line (Zero Vibration)" : "Vibrating Antinode";
        var textNode = new FormattedText(nodalStatus, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 10,
            new SolidColorBrush(Math.Abs(u) < 0.05 ? Color.FromRgb(0, 229, 255) : Color.FromArgb(180, 255, 0, 122)));
        context.DrawText(textNode, new Point(hudX + 12, hudY + 70));
    }

    private void RenderFooter(DrawingContext context, MathThemeTokens tokens, double w, double h)
    {
        var text = new FormattedText(
            "Click or drag to pluck / drop wave ripples • Sand grains migrate to nodal lines where vibration acceleration is zero",
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 10,
            new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)));

        context.DrawText(text, new Point(20, h - 30));
    }
}
