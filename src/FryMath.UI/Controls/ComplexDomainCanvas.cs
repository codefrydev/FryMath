using System;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FryMath.Core.ComplexAnalysis;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

/// <summary>
/// Interactive canvas displaying Complex Domain Coloring and Conformal Mapping:
/// Maps z = x + iy in C to f(z) = u + iv, visualizing Phase (Hue) and Modulus (Brightness / Log Contours).
/// </summary>
public class ComplexDomainCanvas : Control
{
    public static readonly StyledProperty<ComplexPreset> PresetProperty =
        AvaloniaProperty.Register<ComplexDomainCanvas, ComplexPreset>(nameof(Preset), ComplexPreset.ZSquared);

    public static readonly StyledProperty<bool> ShowModulusContoursProperty =
        AvaloniaProperty.Register<ComplexDomainCanvas, bool>(nameof(ShowModulusContours), true);

    public static readonly StyledProperty<bool> ShowConformalGridProperty =
        AvaloniaProperty.Register<ComplexDomainCanvas, bool>(nameof(ShowConformalGrid), true);

    public static readonly StyledProperty<string> CustomFormulaProperty =
        AvaloniaProperty.Register<ComplexDomainCanvas, string>(nameof(CustomFormula), "z^2");

    public ComplexPreset Preset
    {
        get => GetValue(PresetProperty);
        set => SetValue(PresetProperty, value);
    }

    public bool ShowModulusContours
    {
        get => GetValue(ShowModulusContoursProperty);
        set => SetValue(ShowModulusContoursProperty, value);
    }

    public bool ShowConformalGrid
    {
        get => GetValue(ShowConformalGridProperty);
        set => SetValue(ShowConformalGridProperty, value);
    }

    public string CustomFormula
    {
        get => GetValue(CustomFormulaProperty);
        set => SetValue(CustomFormulaProperty, value);
    }

    private readonly ComplexExpression _expr = new(ComplexPreset.ZSquared);

    // Pan & Zoom state
    private double _centerX = 0.0;
    private double _centerY = 0.0;
    private double _scale = 90.0; // pixels per unit
    private bool _isDragging;
    private Point _lastDragPoint;

    // Hover probe state
    private bool _isHovered;
    private Point _hoverScreen;
    private Complex _hoverZ;
    private Complex _hoverW;
    private double _hoverDerivativeModulus;

    // Bitmap cache for fast rendering
    private WriteableBitmap? _bitmap;
    private int _bmpWidth;
    private int _bmpHeight;
    private bool _needsBitmapUpdate = true;

    static ComplexDomainCanvas()
    {
        AffectsRender<ComplexDomainCanvas>(PresetProperty, ShowModulusContoursProperty, ShowConformalGridProperty, CustomFormulaProperty);
    }

    public ComplexDomainCanvas()
    {
        ClipToBounds = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PresetProperty)
        {
            _expr.SetPreset(Preset);
            _needsBitmapUpdate = true;
            InvalidateVisual();
        }
        else if (change.Property == CustomFormulaProperty)
        {
            _expr.CompileCustomFormula(CustomFormula);
            _needsBitmapUpdate = true;
            InvalidateVisual();
        }
        else if (change.Property == ShowModulusContoursProperty || change.Property == ShowConformalGridProperty)
        {
            _needsBitmapUpdate = true;
            InvalidateVisual();
        }
    }

    public void SetPreset(ComplexPreset preset)
    {
        Preset = preset;
        _expr.SetPreset(preset);
        _needsBitmapUpdate = true;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double zoomFactor = e.Delta.Y > 0 ? 1.15 : 0.85;
        var mousePos = e.GetPosition(this);
        var mathBefore = ScreenToMath(mousePos);

        _scale = Math.Clamp(_scale * zoomFactor, 20.0, 600.0);

        // Keep point under cursor stable
        _centerX = mathBefore.X - (mousePos.X - Bounds.Width * 0.5) / _scale;
        _centerY = mathBefore.Y + (mousePos.Y - Bounds.Height * 0.5) / _scale;

        _needsBitmapUpdate = true;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastDragPoint = e.GetPosition(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);

        if (_isDragging)
        {
            var delta = pos - _lastDragPoint;
            _lastDragPoint = pos;
            _centerX -= delta.X / _scale;
            _centerY += delta.Y / _scale;
            _needsBitmapUpdate = true;
            InvalidateVisual();
        }

        _isHovered = true;
        _hoverScreen = pos;
        var math = ScreenToMath(pos);
        _hoverZ = new Complex(math.X, math.Y);
        _hoverW = _expr.Evaluate(_hoverZ);
        var deriv = _expr.Derivative(_hoverZ);
        _hoverDerivativeModulus = deriv.Magnitude;

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHovered = false;
        InvalidateVisual();
    }

    public Point MathToScreen(double x, double y)
    {
        double ox = Bounds.Width * 0.5;
        double oy = Bounds.Height * 0.5;
        return new Point(ox + (x - _centerX) * _scale, oy - (y - _centerY) * _scale);
    }

    public Point ScreenToMath(Point screen)
    {
        double ox = Bounds.Width * 0.5;
        double oy = Bounds.Height * 0.5;
        return new Point(_centerX + (screen.X - ox) / _scale, _centerY - (screen.Y - oy) / _scale);
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        var tokens = MathThemeTokens.DarkElectric;

        // Background dark base
        context.FillRectangle(tokens.SurfaceBrush, new Rect(0, 0, w, h));

        // 1. Render Domain Coloring Bitmap
        UpdateDomainColoringBitmap((int)w, (int)h);
        if (_bitmap != null)
        {
            context.DrawImage(_bitmap, new Rect(0, 0, w, h));
        }

        // 2. Complex Axes and Unit Scale Markings
        RenderAxes(context, w, h, tokens);

        // 3. Hover Probe HUD
        if (_isHovered)
        {
            RenderProbeHud(context, tokens);
        }

        // 4. Color Legend Badge (Hue circle + Modulus mapping)
        RenderLegendBadge(context, tokens);
    }

    private void UpdateDomainColoringBitmap(int screenW, int screenH)
    {
        // Internal render resolution: step down slightly for smooth 60 FPS while scaling up
        int targetW = Math.Clamp(screenW / 2, 100, 640);
        int targetH = Math.Clamp(screenH / 2, 80, 480);

        if (_bitmap == null || _bmpWidth != targetW || _bmpHeight != targetH)
        {
            _bmpWidth = targetW;
            _bmpHeight = targetH;
            _bitmap = new WriteableBitmap(
                new PixelSize(_bmpWidth, _bmpHeight),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
            _needsBitmapUpdate = true;
        }

        if (!_needsBitmapUpdate) return;
        _needsBitmapUpdate = false;

        using var fb = _bitmap.Lock();
        unsafe
        {
            uint* ptr = (uint*)fb.Address;
            int stride = fb.RowBytes / 4;

            double halfW = screenW * 0.5;
            double halfH = screenH * 0.5;
            bool showContours = ShowModulusContours;
            bool showGrid = ShowConformalGrid;

            for (int py = 0; py < _bmpHeight; py++)
            {
                double screenY = (py / (double)_bmpHeight) * screenH;
                double mathY = _centerY - (screenY - halfH) / _scale;

                uint* row = ptr + (py * stride);

                for (int px = 0; px < _bmpWidth; px++)
                {
                    double screenX = (px / (double)_bmpWidth) * screenW;
                    double mathX = _centerX + (screenX - halfW) / _scale;

                    Complex z = new Complex(mathX, mathY);
                    Complex w = _expr.Evaluate(z);

                    // Phase theta in [-pi, pi]
                    double theta = Math.Atan2(w.Imaginary, w.Real);
                    if (double.IsNaN(theta)) theta = 0;

                    // Hue: 0 to 1
                    double hue = (theta + Math.PI) / (2.0 * Math.PI);

                    // Modulus r = |w|
                    double r = w.Magnitude;
                    if (double.IsNaN(r)) r = 0;

                    // Lightness calculation
                    // Bright near poles (r -> infty), dark near roots (r -> 0)
                    double logR = Math.Log(Math.Max(r, 1e-6));
                    double baseL = 0.5 + 0.35 * Math.Tanh(logR * 0.5);

                    // Modulus contour rings: fractional part of log2(r)
                    if (showContours && r > 0.05)
                    {
                        double log2r = Math.Log2(r);
                        double frac = log2r - Math.Floor(log2r);
                        double ring = Math.Abs(frac - 0.5) * 2.0; // 0 at edges, 1 at center
                        ring = Math.Pow(ring, 0.4);
                        baseL *= (0.75 + 0.25 * ring);
                    }

                    // Conformal grid lines in w-plane: real and imag integer grid
                    if (showGrid)
                    {
                        double gridU = Math.Abs(w.Real - Math.Round(w.Real));
                        double gridV = Math.Abs(w.Imaginary - Math.Round(w.Imaginary));
                        if (gridU < 0.06 || gridV < 0.06)
                        {
                            baseL = Math.Min(1.0, baseL * 1.35 + 0.15);
                        }
                    }

                    // Convert HSL (hue, sat=0.88, lightness=baseL) to BGRA32
                    uint color = HslToBgra(hue, 0.88, Math.Clamp(baseL, 0.05, 0.95));
                    row[px] = color;
                }
            }
        }
    }

    private static uint HslToBgra(double h, double s, double l)
    {
        double r, g, b;

        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
            double p = 2.0 * l - q;
            r = HueToRgb(p, q, h + 1.0 / 3.0);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1.0 / 3.0);
        }

        byte byteR = (byte)Math.Clamp((int)(r * 255), 0, 255);
        byte byteG = (byte)Math.Clamp((int)(g * 255), 0, 255);
        byte byteB = (byte)Math.Clamp((int)(b * 255), 0, 255);

        // BGRA format
        return (uint)(255 << 24 | (byteR << 16) | (byteG << 8) | byteB);
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }

    private void RenderAxes(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        var origin = MathToScreen(0, 0);

        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.4);
        var subPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), 0.8);

        // Real Axis: horizontal
        if (origin.Y >= 0 && origin.Y <= h)
        {
            context.DrawLine(axisPen, new Point(0, origin.Y), new Point(w, origin.Y));
        }

        // Imag Axis: vertical
        if (origin.X >= 0 && origin.X <= w)
        {
            context.DrawLine(axisPen, new Point(origin.X, 0), new Point(origin.X, h));
        }

        // Axis tick labels
        double minMathX = ScreenToMath(new Point(0, 0)).X;
        double maxMathX = ScreenToMath(new Point(w, 0)).X;
        double minMathY = ScreenToMath(new Point(0, h)).Y;
        double maxMathY = ScreenToMath(new Point(0, 0)).Y;

        double step = 1.0;
        if (_scale > 180) step = 0.5;
        if (_scale < 50) step = 2.0;

        int startX = (int)Math.Floor(minMathX / step);
        int endX = (int)Math.Ceiling(maxMathX / step);

        for (int k = startX; k <= endX; k++)
        {
            if (k == 0) continue;
            double mathVal = k * step;
            var pt = MathToScreen(mathVal, 0);
            if (pt.X >= 0 && pt.X <= w)
            {
                context.DrawLine(subPen, new Point(pt.X, origin.Y - 4), new Point(pt.X, origin.Y + 4));

                var text = new FormattedText(
                    mathVal.ToString("G3", CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
                    10,
                    tokens.TextMutedBrush);
                context.DrawText(text, new Point(pt.X - text.Width * 0.5, Math.Clamp(origin.Y + 6, 4, h - 18)));
            }
        }

        int startY = (int)Math.Floor(minMathY / step);
        int endY = (int)Math.Ceiling(maxMathY / step);

        for (int k = startY; k <= endY; k++)
        {
            if (k == 0) continue;
            double mathVal = k * step;
            var pt = MathToScreen(0, mathVal);
            if (pt.Y >= 0 && pt.Y <= h)
            {
                context.DrawLine(subPen, new Point(origin.X - 4, pt.Y), new Point(origin.X + 4, pt.Y));

                string label = (mathVal == 1.0 ? "i" : (mathVal == -1.0 ? "-i" : $"{mathVal:G3}i"));
                var text = new FormattedText(
                    label,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
                    10,
                    tokens.AccentCyanBrush);
                context.DrawText(text, new Point(Math.Clamp(origin.X + 8, 4, w - 30), pt.Y - text.Height * 0.5));
            }
        }

        // Labels at axis extremes
        var realLbl = new FormattedText("Re(z)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold), 11, tokens.TextPrimaryBrush);
        context.DrawText(realLbl, new Point(w - realLbl.Width - 14, Math.Clamp(origin.Y - 18, 10, h - 30)));

        var imagLbl = new FormattedText("Im(z)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold), 11, tokens.AccentCyanBrush);
        context.DrawText(imagLbl, new Point(Math.Clamp(origin.X + 10, 10, w - 50), 10));
    }

    private void RenderProbeHud(DrawingContext context, MathThemeTokens tokens)
    {
        double px = _hoverScreen.X + 16;
        double py = _hoverScreen.Y + 16;
        if (px + 230 > Bounds.Width) px = _hoverScreen.X - 240;
        if (py + 120 > Bounds.Height) py = _hoverScreen.Y - 130;

        var hudRect = new Rect(px, py, 230, 115);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)), hudRect, 6);
        context.DrawRectangle(new Pen(tokens.AccentCyanBrush, 1.2), hudRect, 6);

        // Crosshairs at cursor
        var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1.0);
        context.DrawLine(crossPen, new Point(_hoverScreen.X - 8, _hoverScreen.Y), new Point(_hoverScreen.X + 8, _hoverScreen.Y));
        context.DrawLine(crossPen, new Point(_hoverScreen.X, _hoverScreen.Y - 8), new Point(_hoverScreen.X, _hoverScreen.Y + 8));

        // Format values
        string signZ = _hoverZ.Imaginary >= 0 ? "+" : "-";
        string strZ = $"{_hoverZ.Real:F2} {signZ} {Math.Abs(_hoverZ.Imaginary):F2}i";

        string signW = _hoverW.Imaginary >= 0 ? "+" : "-";
        string strW = $"{_hoverW.Real:F2} {signW} {Math.Abs(_hoverW.Imaginary):F2}i";

        double modW = _hoverW.Magnitude;
        double argW = Math.Atan2(_hoverW.Imaginary, _hoverW.Real);
        double argDeg = argW * (180.0 / Math.PI);

        string[] lines =
        {
            $"z  = {strZ}",
            $"f(z) = {strW}",
            $"|f(z)| = {modW:F3}  (Modulus)",
            $"arg(f(z)) = {argW:F2} rad ({argDeg:F1}°)",
            $"|f'(z)| = {_hoverDerivativeModulus:F3} (Scale Factor)"
        };

        IBrush[] brushes =
        {
            tokens.TextPrimaryBrush,
            tokens.AccentCyanBrush,
            tokens.AccentEmeraldBrush,
            tokens.AccentAmberBrush,
            tokens.AccentPurpleBrush
        };

        double textY = py + 8;
        for (int i = 0; i < lines.Length; i++)
        {
            var text = new FormattedText(
                lines[i],
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, i <= 1 ? FontWeight.Bold : FontWeight.Normal),
                10,
                brushes[i]);
            context.DrawText(text, new Point(px + 10, textY));
            textY += 19;
        }
    }

    private void RenderLegendBadge(DrawingContext context, MathThemeTokens tokens)
    {
        double badgeX = 14;
        double badgeY = 14;
        var rect = new Rect(badgeX, badgeY, 175, 52);

        context.FillRectangle(new SolidColorBrush(Color.FromArgb(210, 15, 23, 42)), rect, 6);
        context.DrawRectangle(new Pen(tokens.BorderBrush, 1.0), rect, 6);

        var title = new FormattedText("DOMAIN COLORING", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold), 9, tokens.TextMutedBrush);
        context.DrawText(title, new Point(badgeX + 8, badgeY + 6));

        // Color ramp bar for phase hue
        int rampW = 158;
        int rampH = 8;
        double rx = badgeX + 8;
        double ry = badgeY + 22;

        for (int i = 0; i < rampW; i++)
        {
            double hue = i / (double)rampW;
            uint c = HslToBgra(hue, 0.9, 0.5);
            byte b = (byte)(c & 0xFF);
            byte g = (byte)((c >> 8) & 0xFF);
            byte r = (byte)((c >> 16) & 0xFF);
            var color = Color.FromRgb(r, g, b);
            context.FillRectangle(new SolidColorBrush(color), new Rect(rx + i, ry, 1.2, rampH));
        }

        var sub = new FormattedText("-π (Cyan)   0 (Red)   +π (Cyan)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold), 8, tokens.TextMutedBrush);
        context.DrawText(sub, new Point(badgeX + 8, badgeY + 34));
    }
}
