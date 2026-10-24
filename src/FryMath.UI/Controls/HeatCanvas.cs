using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FryMath.Core.Heat;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class HeatCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<HeatCanvas, string>(nameof(PresetName), "laplace");

    public static readonly StyledProperty<bool> IsSimulatingProperty =
        AvaloniaProperty.Register<HeatCanvas, bool>(nameof(IsSimulating), true);

    public static readonly StyledProperty<bool> ShowHeatFluxProperty =
        AvaloniaProperty.Register<HeatCanvas, bool>(nameof(ShowHeatFlux), true);

    public static readonly StyledProperty<bool> ShowIsothermsProperty =
        AvaloniaProperty.Register<HeatCanvas, bool>(nameof(ShowIsotherms), true);

    public static readonly StyledProperty<double> DiffusivityProperty =
        AvaloniaProperty.Register<HeatCanvas, double>(nameof(Diffusivity), 0.22);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public bool IsSimulating
    {
        get => GetValue(IsSimulatingProperty);
        set => SetValue(IsSimulatingProperty, value);
    }

    public bool ShowHeatFlux
    {
        get => GetValue(ShowHeatFluxProperty);
        set => SetValue(ShowHeatFluxProperty, value);
    }

    public bool ShowIsotherms
    {
        get => GetValue(ShowIsothermsProperty);
        set => SetValue(ShowIsothermsProperty, value);
    }

    public double Diffusivity
    {
        get => GetValue(DiffusivityProperty);
        set => SetValue(DiffusivityProperty, value);
    }

    private readonly HeatDiffusionEngine _engine = new(72, 72);
    private readonly DispatcherTimer _timer;
    private WriteableBitmap? _bitmap;
    private string? _loadedPreset;
    private Point? _hoverPos;
    private bool _isPainting;
    private double _paintTemp = 100.0;

    static HeatCanvas()
    {
        AffectsRender<HeatCanvas>(PresetNameProperty, IsSimulatingProperty, ShowHeatFluxProperty, ShowIsothermsProperty, DiffusivityProperty);
    }

    public HeatCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            if (IsSimulating)
            {
                _engine.Diffusivity = Diffusivity;
                _engine.Step(0.5);
                InvalidateVisual();
            }
        };
        _timer.Start();

        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        _paintTemp = props.IsRightButtonPressed ? -40.0 : 100.0;
        _isPainting = true;
        ApplyMouseHeat(pt.X, pt.Y, _paintTemp);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        _hoverPos = pt;

        if (_isPainting)
        {
            ApplyMouseHeat(pt.X, pt.Y, _paintTemp);
        }
        else
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPainting = false;
    }

    private void ApplyMouseHeat(double px, double py, double temp)
    {
        double u = Math.Clamp(px / Bounds.Width, 0.05, 0.95);
        double v = Math.Clamp(py / Bounds.Height, 0.05, 0.95);
        _engine.Sources.Add(new HeatSource(u, v, temp, 0.08));
        if (_engine.Sources.Count > 16) _engine.Sources.RemoveAt(0);
        InvalidateVisual();
    }

    private void EnsurePreset()
    {
        if (_loadedPreset != PresetName)
        {
            _engine.LoadPreset(PresetName);
            _loadedPreset = PresetName;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        EnsurePreset();

        int gw = _engine.Width;
        int gh = _engine.Height;

        if (_bitmap == null || _bitmap.PixelSize.Width != gw || _bitmap.PixelSize.Height != gh)
        {
            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(new PixelSize(gw, gh), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        }

        // Render thermal temperature bitmap
        using (var buf = _bitmap.Lock())
        {
            unsafe
            {
                uint* ptr = (uint*)buf.Address;
                int stride = buf.RowBytes / 4;

                for (int y = 0; y < gh; y++)
                {
                    for (int x = 0; x < gw; x++)
                    {
                        double temp = _engine.Temperature[x, y];
                        uint color = TemperatureToColor(temp);
                        ptr[y * stride + x] = color;
                    }
                }
            }
        }

        // Draw thermal field stretched across viewport
        var destRect = new Rect(0, 0, w, h);
        context.DrawImage(_bitmap, destRect);

        var tokens = MathThemeTokens.DarkElectric;

        // 1. Draw Heat Flux Vector Arrows q = -k ∇T
        if (ShowHeatFlux)
        {
            RenderHeatFluxGrid(context, w, h, tokens);
        }

        // 2. Draw Isothermal Contours
        if (ShowIsotherms)
        {
            RenderIsotherms(context, w, h, tokens);
        }

        // 3. Draw Interactive Probe and Header HUD
        RenderProbeAndHUD(context, w, h, tokens);
    }

    private static uint TemperatureToColor(double temp)
    {
        // Thermal map:
        // -50 to 0: Dark Blue -> Cyan
        // 0 to 40: Cyan -> Emerald -> Amber
        // 40 to 100: Amber -> Crimson -> Yellow
        // >100: White hot
        double norm = Math.Clamp((temp + 40.0) / 160.0, 0.0, 1.0);

        byte r, g, b;
        if (norm < 0.25)
        {
            double t = norm / 0.25;
            r = (byte)(10 + 15 * t);
            g = (byte)(15 + 80 * t);
            b = (byte)(60 + 160 * t);
        }
        else if (norm < 0.5)
        {
            double t = (norm - 0.25) / 0.25;
            r = (byte)(25 + 180 * t);
            g = (byte)(95 + 60 * t);
            b = (byte)(220 * (1.0 - t * 0.8));
        }
        else if (norm < 0.75)
        {
            double t = (norm - 0.5) / 0.25;
            r = (byte)(205 + 40 * t);
            g = (byte)(155 - 40 * t + 80 * t);
            b = (byte)(44 * (1.0 - t));
        }
        else
        {
            double t = (norm - 0.75) / 0.25;
            r = 255;
            g = (byte)(195 + 60 * t);
            b = (byte)(40 + 215 * t);
        }

        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }

    private void RenderHeatFluxGrid(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        int nx = 14;
        int ny = 14;
        var arrowPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.2);

        for (int ix = 1; ix < nx; ix++)
        {
            for (int iy = 1; iy < ny; iy++)
            {
                double u = (double)ix / nx;
                double v = (double)iy / ny;
                var flux = _engine.SampleFlux(u, v);

                double mag = Math.Sqrt(flux.Qx * flux.Qx + flux.Qy * flux.Qy);
                if (mag < 0.05) continue;

                double px = u * w;
                double py = v * h;

                double len = Math.Clamp(mag * 4.0, 4.0, 20.0);
                double dirX = (flux.Qx / mag) * len;
                double dirY = (flux.Qy / mag) * len;

                context.DrawLine(arrowPen, new Point(px, py), new Point(px + dirX, py + dirY));
                context.DrawEllipse(tokens.AccentCyanBrush, null, new Point(px + dirX, py + dirY), 2.0, 2.0);
            }
        }
    }

    private void RenderIsotherms(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        var isoPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1.2, DashStyle.Dash);
        double[] levels = { -20.0, 0.0, 20.0, 40.0, 60.0, 80.0 };
        int gw = _engine.Width;
        int gh = _engine.Height;

        double dx = w / (gw - 1);
        double dy = h / (gh - 1);

        foreach (double c in levels)
        {
            for (int y = 0; y < gh - 1; y += 2)
            {
                for (int x = 0; x < gw - 1; x += 2)
                {
                    double t00 = _engine.Temperature[x, y];
                    double t10 = _engine.Temperature[x + 1, y];
                    double t01 = _engine.Temperature[x, y + 1];

                    // Check horizontal and vertical edge crossings for contour line
                    bool crossH = (t00 <= c && t10 > c) || (t00 > c && t10 <= c);
                    bool crossV = (t00 <= c && t01 > c) || (t00 > c && t01 <= c);

                    if (crossH && crossV)
                    {
                        double fracH = (c - t00) / (t10 - t00 + 1e-9);
                        double px = (x + fracH) * dx;
                        double py = y * dy;

                        double fracV = (c - t00) / (t01 - t00 + 1e-9);
                        double qx = x * dx;
                        double qy = (y + fracV) * dy;

                        context.DrawLine(isoPen, new Point(px, py), new Point(qx, qy));
                    }
                }
            }
        }
    }

    private void RenderProbeAndHUD(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        var tfBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

        string hudTitle =
            $"2D HEAT DIFFUSION & POISSON/LAPLACE PDE: ∂T/∂t = α ∇²T + Q(x, y)\n" +
            $"Preset: {PresetName.ToUpperInvariant()}  |  Diffusivity α = {Diffusivity:F2}  |  (Left-Click: Hot flame 100°C, Right-Click: Ice sink -40°C)";

        var titleText = new FormattedText(
            hudTitle,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tfBold,
            11,
            tokens.TextPrimaryBrush);

        context.DrawText(titleText, new Point(16.0, 16.0));

        if (_hoverPos.HasValue)
        {
            var hp = _hoverPos.Value;
            double u = Math.Clamp(hp.X / w, 0.0, 1.0);
            double v = Math.Clamp(hp.Y / h, 0.0, 1.0);
            var flux = _engine.SampleFlux(u, v);
            double gradMag = Math.Sqrt(flux.Qx * flux.Qx + flux.Qy * flux.Qy);

            // Probe target
            context.DrawEllipse(null, new Pen(Brushes.White, 1.5), hp, 8.0, 8.0);
            context.DrawEllipse(tokens.AccentAmberBrush, null, hp, 3.5, 3.5);

            string probeMsg =
                $"T = {flux.Temp:F1}°C\n" +
                $"Heat Flux q = ⟨{flux.Qx:F2}, {flux.Qy:F2}⟩\n" +
                $"||∇T|| = {gradMag:F2} °C/m";

            var probeText = new FormattedText(
                probeMsg,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                tfBold,
                10,
                tokens.TextPrimaryBrush);

            double tipX = Math.Min(hp.X + 14.0, w - 160.0);
            double tipY = Math.Max(hp.Y - 45.0, 20.0);
            var cardRect = new Rect(tipX - 6.0, tipY - 4.0, 155.0, 48.0);
            context.FillRectangle(tokens.SurfaceHighBrush, cardRect, 4f);
            context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.0), cardRect, 4f, 4f);
            context.DrawText(probeText, new Point(tipX, tipY));
        }
    }
}
