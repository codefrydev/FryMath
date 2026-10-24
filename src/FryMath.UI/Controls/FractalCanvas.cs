using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FryMath.Core.Fractals;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class FractalCanvas : Control
{
    public static readonly StyledProperty<FractalType> FractalTypeProperty =
        AvaloniaProperty.Register<FractalCanvas, FractalType>(nameof(FractalType), FractalType.Mandelbrot);

    public static readonly StyledProperty<int> MaxIterationsProperty =
        AvaloniaProperty.Register<FractalCanvas, int>(nameof(MaxIterations), 120);

    public static readonly StyledProperty<FractalPalette> PaletteProperty =
        AvaloniaProperty.Register<FractalCanvas, FractalPalette>(nameof(Palette), FractalPalette.ElectricNeon);

    public static readonly StyledProperty<double> JuliaCrProperty =
        AvaloniaProperty.Register<FractalCanvas, double>(nameof(JuliaCr), -0.7);

    public static readonly StyledProperty<double> JuliaCiProperty =
        AvaloniaProperty.Register<FractalCanvas, double>(nameof(JuliaCi), 0.27015);

    public static readonly StyledProperty<int> NewtonDegreeProperty =
        AvaloniaProperty.Register<FractalCanvas, int>(nameof(NewtonDegree), 3);

    public FractalType FractalType
    {
        get => GetValue(FractalTypeProperty);
        set => SetValue(FractalTypeProperty, value);
    }

    public int MaxIterations
    {
        get => GetValue(MaxIterationsProperty);
        set => SetValue(MaxIterationsProperty, value);
    }

    public FractalPalette Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public double JuliaCr
    {
        get => GetValue(JuliaCrProperty);
        set => SetValue(JuliaCrProperty, value);
    }

    public double JuliaCi
    {
        get => GetValue(JuliaCiProperty);
        set => SetValue(JuliaCiProperty, value);
    }

    public int NewtonDegree
    {
        get => GetValue(NewtonDegreeProperty);
        set => SetValue(NewtonDegreeProperty, value);
    }

    // View state
    private double _centerX = -0.5;
    private double _centerY = 0.0;
    private double _zoom = 1.0;

    private bool _isDragging;
    private Point _lastDragPoint;

    // Hover state
    private bool _isHovered;
    private Point _hoverScreen;
    private double _hoverMathX;
    private double _hoverMathY;
    private double _hoverIteration;
    private int _hoverRoot;

    // Bitmap cache
    private WriteableBitmap? _bitmap;
    private int _bmpWidth;
    private int _bmpHeight;
    private uint[]? _pixelBuffer;
    private bool _needsRedraw = true;

    // Mini Julia thumbnail cache for Mandelbrot duality preview
    private WriteableBitmap? _juliaMiniBitmap;
    private const int MiniSize = 90;
    private readonly uint[] _miniBuffer = new uint[MiniSize * MiniSize];

    static FractalCanvas()
    {
        AffectsRender<FractalCanvas>(
            FractalTypeProperty,
            MaxIterationsProperty,
            PaletteProperty,
            JuliaCrProperty,
            JuliaCiProperty,
            NewtonDegreeProperty);
    }

    public FractalCanvas()
    {
        ClipToBounds = true;
    }

    public void ResetView()
    {
        if (FractalType == FractalType.Mandelbrot || FractalType == FractalType.BurningShip)
        {
            _centerX = -0.5;
            _centerY = 0.0;
            _zoom = 1.0;
        }
        else
        {
            _centerX = 0.0;
            _centerY = 0.0;
            _zoom = 1.0;
        }
        _needsRedraw = true;
        InvalidateVisual();
    }

    public void SetView(double cx, double cy, double zoom)
    {
        _centerX = cx;
        _centerY = cy;
        _zoom = zoom;
        _needsRedraw = true;
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FractalTypeProperty)
        {
            ResetView();
        }
        else if (change.Property == MaxIterationsProperty ||
                 change.Property == PaletteProperty ||
                 change.Property == JuliaCrProperty ||
                 change.Property == JuliaCiProperty ||
                 change.Property == NewtonDegreeProperty)
        {
            _needsRedraw = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double zoomMul = e.Delta.Y > 0 ? 1.25 : 0.8;
        var mousePos = e.GetPosition(this);
        var mathBefore = ScreenToMath(mousePos);

        _zoom = Math.Clamp(_zoom * zoomMul, 0.1, 1e12);

        // Keep point under cursor fixed during zoom
        double aspect = Bounds.Width / Math.Max(1.0, Bounds.Height);
        double viewHeight = 3.0 / _zoom;
        double viewWidth = viewHeight * aspect;

        _centerX = mathBefore.X - (mousePos.X - Bounds.Width * 0.5) * (viewWidth / Bounds.Width);
        _centerY = mathBefore.Y + (mousePos.Y - Bounds.Height * 0.5) * (viewHeight / Bounds.Height);

        _needsRedraw = true;
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

            double aspect = Bounds.Width / Math.Max(1.0, Bounds.Height);
            double viewHeight = 3.0 / _zoom;
            double viewWidth = viewHeight * aspect;

            _centerX -= delta.X * (viewWidth / Bounds.Width);
            _centerY += delta.Y * (viewHeight / Bounds.Height);

            _needsRedraw = true;
            InvalidateVisual();
        }

        _isHovered = true;
        _hoverScreen = pos;
        var math = ScreenToMath(pos);
        _hoverMathX = math.X;
        _hoverMathY = math.Y;

        var p = BuildParams();
        var (iter, root) = FractalEngine.EvaluatePoint(_hoverMathX, _hoverMathY, p);
        _hoverIteration = iter;
        _hoverRoot = root;

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
        double aspect = Bounds.Width / Math.Max(1.0, Bounds.Height);
        double viewHeight = 3.0 / _zoom;
        double viewWidth = viewHeight * aspect;

        double sx = (x - (_centerX - viewWidth * 0.5)) / viewWidth * Bounds.Width;
        double sy = ((_centerY + viewHeight * 0.5) - y) / viewHeight * Bounds.Height;
        return new Point(sx, sy);
    }

    public Point ScreenToMath(Point screen)
    {
        double aspect = Bounds.Width / Math.Max(1.0, Bounds.Height);
        double viewHeight = 3.0 / _zoom;
        double viewWidth = viewHeight * aspect;

        double mx = (_centerX - viewWidth * 0.5) + (screen.X / Bounds.Width) * viewWidth;
        double my = (_centerY + viewHeight * 0.5) - (screen.Y / Bounds.Height) * viewHeight;
        return new Point(mx, my);
    }

    private FractalParams BuildParams()
    {
        return new FractalParams
        {
            Type = FractalType,
            MaxIterations = MaxIterations,
            Palette = Palette,
            JuliaCr = JuliaCr,
            JuliaCi = JuliaCi,
            NewtonDegree = NewtonDegree
        };
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        var tokens = MathThemeTokens.DarkElectric;
        context.FillRectangle(tokens.SurfaceBrush, new Rect(0, 0, w, h));

        // 1. Render fractal bitmap
        UpdateBitmap((int)w, (int)h);
        if (_bitmap != null)
        {
            context.DrawImage(_bitmap, new Rect(0, 0, w, h));
        }

        // 2. Axes overlay if zoomed out enough
        if (_zoom <= 10.0)
        {
            RenderAxes(context, tokens);
        }

        // 3. Mini Julia duality thumbnail (if Mandelbrot mode)
        if (FractalType == FractalType.Mandelbrot && _isHovered)
        {
            RenderMiniJulia(context, tokens);
        }

        // 4. Hover HUD Probe
        if (_isHovered)
        {
            RenderProbeHud(context, tokens);
        }

        // 5. Bottom Navigation & Zoom Info
        RenderFooterInfo(context, tokens, w, h);
    }

    private void UpdateBitmap(int screenW, int screenH)
    {
        // Render resolution: half resolution scaled up for smooth 60 FPS exploration
        int targetW = Math.Clamp(screenW / 2, 80, 720);
        int targetH = Math.Clamp(screenH / 2, 60, 540);

        if (_bitmap == null || _bmpWidth != targetW || _bmpHeight != targetH)
        {
            _bmpWidth = targetW;
            _bmpHeight = targetH;
            _bitmap = new WriteableBitmap(
                new PixelSize(_bmpWidth, _bmpHeight),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
            _pixelBuffer = new uint[_bmpWidth * _bmpHeight];
            _needsRedraw = true;
        }

        if (!_needsRedraw || _pixelBuffer == null) return;
        _needsRedraw = false;

        var p = BuildParams();
        FractalEngine.RenderToBuffer(_pixelBuffer, _bmpWidth, _bmpHeight, _centerX, _centerY, _zoom, p);

        using var fb = _bitmap.Lock();
        unsafe
        {
            uint* dest = (uint*)fb.Address;
            int stride = fb.RowBytes / 4;

            for (int y = 0; y < _bmpHeight; y++)
            {
                int srcOffset = y * _bmpWidth;
                uint* rowDest = dest + (y * stride);
                for (int x = 0; x < _bmpWidth; x++)
                {
                    rowDest[x] = _pixelBuffer[srcOffset + x];
                }
            }
        }
    }

    private void RenderAxes(DrawingContext context, MathThemeTokens tokens)
    {
        var origin = MathToScreen(0, 0);
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1);

        if (origin.Y >= 0 && origin.Y <= Bounds.Height)
        {
            context.DrawLine(pen, new Point(0, origin.Y), new Point(Bounds.Width, origin.Y));
        }

        if (origin.X >= 0 && origin.X <= Bounds.Width)
        {
            context.DrawLine(pen, new Point(origin.X, 0), new Point(origin.X, Bounds.Height));
        }
    }

    private void RenderMiniJulia(DrawingContext context, MathThemeTokens tokens)
    {
        // Render a 90x90 Julia set corresponding to hovered c
        if (_juliaMiniBitmap == null)
        {
            _juliaMiniBitmap = new WriteableBitmap(
                new PixelSize(MiniSize, MiniSize),
                new Avalonia.Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
        }

        var miniParams = new FractalParams
        {
            Type = FractalType.Julia,
            MaxIterations = Math.Min(60, MaxIterations),
            Palette = Palette,
            JuliaCr = _hoverMathX,
            JuliaCi = _hoverMathY
        };

        FractalEngine.RenderToBuffer(_miniBuffer, MiniSize, MiniSize, 0.0, 0.0, 0.85, miniParams);

        using (var fb = _juliaMiniBitmap.Lock())
        unsafe
        {
            uint* dest = (uint*)fb.Address;
            int stride = fb.RowBytes / 4;
            for (int y = 0; y < MiniSize; y++)
            {
                int src = y * MiniSize;
                uint* row = dest + (y * stride);
                for (int x = 0; x < MiniSize; x++)
                {
                    row[x] = _miniBuffer[src + x];
                }
            }
        }

        // Draw card in top-right corner below omnibar
        double boxX = Bounds.Width - MiniSize - 24;
        double boxY = 20;

        var cardRect = new Rect(boxX - 6, boxY - 6, MiniSize + 12, MiniSize + 36);
        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(220, 13, 16, 28)),
            new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 229, 255)), 1),
            cardRect, 8, 8);

        context.DrawImage(_juliaMiniBitmap, new Rect(boxX, boxY, MiniSize, MiniSize));

        var text = new FormattedText(
            $"Julia J(c)\n{_hoverMathX:F2}+{_hoverMathY:F2}i",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Roboto, sans-serif"),
            10,
            new SolidColorBrush(Color.FromRgb(0, 229, 255)));

        context.DrawText(text, new Point(boxX, boxY + MiniSize + 4));
    }

    private void RenderProbeHud(DrawingContext context, MathThemeTokens tokens)
    {
        double hudX = 20;
        double hudY = 20;
        double hudW = 280;
        double hudH = 100;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(215, 13, 16, 28)),
            new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 179, 0)), 1),
            hudRect, 8, 8);

        string sign = _hoverMathY >= 0 ? "+" : "-";
        string coordStr = $"{_hoverMathX:F5} {sign} {Math.Abs(_hoverMathY):F5}i";

        string iterStr = _hoverIteration >= MaxIterations
            ? "Status: Bounded Interior (Inside M)"
            : $"Escape: ν = {_hoverIteration:F2} iters";

        if (FractalType == FractalType.NewtonBasin && _hoverRoot >= 0)
        {
            iterStr = $"Root #{_hoverRoot + 1} (converged in {_hoverIteration:F0} iters)";
        }

        string typeStr = FractalType switch
        {
            FractalType.Mandelbrot => "Mandelbrot Set: z_{n+1} = z_n² + c",
            FractalType.Julia => $"Julia Set: c = {JuliaCr:F3} + {JuliaCi:F3}i",
            FractalType.NewtonBasin => $"Newton Basin: z^{NewtonDegree} - 1 = 0",
            FractalType.BurningShip => "Burning Ship: (|Re(z)| + i|Im(z)|)² + c",
            _ => "Fractal"
        };

        var textType = new FormattedText(typeStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif", FontStyle.Normal, FontWeight.SemiBold), 11,
            new SolidColorBrush(Color.FromRgb(255, 179, 0)));
        context.DrawText(textType, new Point(hudX + 12, hudY + 10));

        var textCoord = new FormattedText($"c = {coordStr}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 12,
            new SolidColorBrush(Color.FromRgb(0, 229, 255)));
        context.DrawText(textCoord, new Point(hudX + 12, hudY + 34));

        var textIter = new FormattedText(iterStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 11,
            new SolidColorBrush(Color.FromRgb(220, 220, 230)));
        context.DrawText(textIter, new Point(hudX + 12, hudY + 56));

        var textZoom = new FormattedText($"Magnification: {_zoom:N1}×  (Max Iter: {MaxIterations})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 10,
            new SolidColorBrush(Color.FromArgb(180, 160, 170, 200)));
        context.DrawText(textZoom, new Point(hudX + 12, hudY + 76));

        // Draw crosshair at cursor
        var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1);
        context.DrawLine(crossPen, new Point(_hoverScreen.X - 8, _hoverScreen.Y), new Point(_hoverScreen.X + 8, _hoverScreen.Y));
        context.DrawLine(crossPen, new Point(_hoverScreen.X, _hoverScreen.Y - 8), new Point(_hoverScreen.X, _hoverScreen.Y + 8));
    }

    private void RenderFooterInfo(DrawingContext context, MathThemeTokens tokens, double w, double h)
    {
        var barRect = new Rect(20, h - 34, w - 40, 24);
        var text = new FormattedText(
            $"Center: ({_centerX:F6}, {_centerY:F6}) • Zoom: {_zoom:N1}× • Scroll to zoom • Drag to pan • Hover over Mandelbrot for live Julia duality",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"),
            10,
            new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)));

        context.DrawText(text, new Point(barRect.X, barRect.Y + 4));
    }
}
