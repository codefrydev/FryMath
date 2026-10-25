using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.CAS;
using FryMath.Core.Calculus;
using FryMath.UI.Theming;
using FryMath.UI.ViewModels;

namespace FryMath.UI.Controls;

public class MathCanvas2D : Control
{
    public static readonly StyledProperty<string> ExpressionProperty =
        AvaloniaProperty.Register<MathCanvas2D, string>(nameof(Expression), "x^3 - 3*x");

    public static readonly StyledProperty<double> ParamAProperty =
        AvaloniaProperty.Register<MathCanvas2D, double>(nameof(ParamA), 1.0);

    public static readonly StyledProperty<double> ParamBProperty =
        AvaloniaProperty.Register<MathCanvas2D, double>(nameof(ParamB), 0.0);

    public static readonly StyledProperty<double> ParamCProperty =
        AvaloniaProperty.Register<MathCanvas2D, double>(nameof(ParamC), 0.0);

    public static readonly StyledProperty<bool> ShowProbeProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowProbe), true);

    public static readonly StyledProperty<bool> ShowTangentProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowTangent), true);

    public static readonly StyledProperty<bool> ShowOsculatingProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowOsculating), true);

    public static readonly StyledProperty<bool> ShowRiemannProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowRiemann), false);

    public static readonly StyledProperty<bool> ShowSecantProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowSecant), false);

    public static readonly StyledProperty<double> SecantDeltaXProperty =
        AvaloniaProperty.Register<MathCanvas2D, double>(nameof(SecantDeltaX), 1.0);

    public static readonly StyledProperty<bool> ShowCriticalPointsProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowCriticalPoints), true);

    public static readonly StyledProperty<bool> ShowSlopeFieldProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowSlopeField), false);

    public static readonly StyledProperty<bool> ShowAreaBetweenCurvesProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowAreaBetweenCurves), false);

    public static readonly StyledProperty<int> RiemannNProperty =
        AvaloniaProperty.Register<MathCanvas2D, int>(nameof(RiemannN), 14);

    public static readonly StyledProperty<RiemannRule> RiemannRuleProperty =
        AvaloniaProperty.Register<MathCanvas2D, RiemannRule>(nameof(RiemannRule), RiemannRule.Midpoint);

    public static readonly StyledProperty<double> IntegrationAProperty =
        AvaloniaProperty.Register<MathCanvas2D, double>(nameof(IntegrationA), -1.5, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> IntegrationBProperty =
        AvaloniaProperty.Register<MathCanvas2D, double>(nameof(IntegrationB), 1.5, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable<FunctionItemViewModel>?> FunctionsProperty =
        AvaloniaProperty.Register<MathCanvas2D, IEnumerable<FunctionItemViewModel>?>(nameof(Functions));

    public static readonly StyledProperty<bool> ShowTaylorSeriesProperty =
        AvaloniaProperty.Register<MathCanvas2D, bool>(nameof(ShowTaylorSeries), false);

    public static readonly StyledProperty<int> TaylorDegreeProperty =
        AvaloniaProperty.Register<MathCanvas2D, int>(nameof(TaylorDegree), 4);

    public static readonly StyledProperty<double> TaylorCenterProperty =
        AvaloniaProperty.Register<MathCanvas2D, double>(nameof(TaylorCenter), 0.0);

    public string Expression
    {
        get => GetValue(ExpressionProperty);
        set => SetValue(ExpressionProperty, value);
    }

    public double ParamA
    {
        get => GetValue(ParamAProperty);
        set => SetValue(ParamAProperty, value);
    }

    public double ParamB
    {
        get => GetValue(ParamBProperty);
        set => SetValue(ParamBProperty, value);
    }

    public double ParamC
    {
        get => GetValue(ParamCProperty);
        set => SetValue(ParamCProperty, value);
    }

    public bool ShowProbe
    {
        get => GetValue(ShowProbeProperty);
        set => SetValue(ShowProbeProperty, value);
    }

    public bool ShowTangent
    {
        get => GetValue(ShowTangentProperty);
        set => SetValue(ShowTangentProperty, value);
    }

    public bool ShowOsculating
    {
        get => GetValue(ShowOsculatingProperty);
        set => SetValue(ShowOsculatingProperty, value);
    }

    public bool ShowRiemann
    {
        get => GetValue(ShowRiemannProperty);
        set => SetValue(ShowRiemannProperty, value);
    }

    public bool ShowSecant
    {
        get => GetValue(ShowSecantProperty);
        set => SetValue(ShowSecantProperty, value);
    }

    public double SecantDeltaX
    {
        get => GetValue(SecantDeltaXProperty);
        set => SetValue(SecantDeltaXProperty, value);
    }

    public bool ShowCriticalPoints
    {
        get => GetValue(ShowCriticalPointsProperty);
        set => SetValue(ShowCriticalPointsProperty, value);
    }

    public bool ShowSlopeField
    {
        get => GetValue(ShowSlopeFieldProperty);
        set => SetValue(ShowSlopeFieldProperty, value);
    }

    public bool ShowAreaBetweenCurves
    {
        get => GetValue(ShowAreaBetweenCurvesProperty);
        set => SetValue(ShowAreaBetweenCurvesProperty, value);
    }

    public int RiemannN
    {
        get => GetValue(RiemannNProperty);
        set => SetValue(RiemannNProperty, value);
    }

    public RiemannRule RiemannRule
    {
        get => GetValue(RiemannRuleProperty);
        set => SetValue(RiemannRuleProperty, value);
    }

    public double IntegrationA
    {
        get => GetValue(IntegrationAProperty);
        set => SetValue(IntegrationAProperty, value);
    }

    public double IntegrationB
    {
        get => GetValue(IntegrationBProperty);
        set => SetValue(IntegrationBProperty, value);
    }

    public IEnumerable<FunctionItemViewModel>? Functions
    {
        get => GetValue(FunctionsProperty);
        set => SetValue(FunctionsProperty, value);
    }

    public bool ShowTaylorSeries
    {
        get => GetValue(ShowTaylorSeriesProperty);
        set => SetValue(ShowTaylorSeriesProperty, value);
    }

    public int TaylorDegree
    {
        get => GetValue(TaylorDegreeProperty);
        set => SetValue(TaylorDegreeProperty, value);
    }

    public double TaylorCenter
    {
        get => GetValue(TaylorCenterProperty);
        set => SetValue(TaylorCenterProperty, value);
    }

    // Navigation (Pan & Zoom)
    public double OriginX { get; set; } = 0.0;
    public double OriginY { get; set; } = 0.0;
    public double ScalePixelsPerUnit { get; set; } = 55.0;

    private Point? _hoverScreenPos;
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartOriginX;
    private double _panStartOriginY;
    private int _draggedBound = 0; // 0 = none, 1 = A, 2 = B

    private MathNode? _cachedAst;
    private Func<double, double>? _cachedFunc;
    private string? _lastParsedExpr;
    private double _lastA = double.NaN;
    private double _lastB = double.NaN;
    private double _lastC = double.NaN;

    static MathCanvas2D()
    {
        AffectsRender<MathCanvas2D>(
            ExpressionProperty,
            ParamAProperty,
            ParamBProperty,
            ParamCProperty,
            ShowProbeProperty,
            ShowTangentProperty,
            ShowOsculatingProperty,
            ShowRiemannProperty,
            ShowSecantProperty,
            SecantDeltaXProperty,
            ShowCriticalPointsProperty,
            ShowSlopeFieldProperty,
            ShowAreaBetweenCurvesProperty,
            RiemannNProperty,
            RiemannRuleProperty,
            IntegrationAProperty,
            IntegrationBProperty,
            FunctionsProperty,
            ShowTaylorSeriesProperty,
            TaylorDegreeProperty,
            TaylorCenterProperty);
    }

    public MathCanvas2D()
    {
        ClipToBounds = true;
        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    public void ResetView()
    {
        OriginX = Bounds.Width * 0.5;
        OriginY = Bounds.Height * 0.5;
        ScalePixelsPerUnit = 55.0;
        InvalidateVisual();
    }

    public void ZoomIn()
    {
        double factor = 1.25;
        double oldScale = ScalePixelsPerUnit;
        ScalePixelsPerUnit = Math.Clamp(ScalePixelsPerUnit * factor, 8.0, 600.0);
        double ratio = ScalePixelsPerUnit / oldScale;
        double cx = Bounds.Width * 0.5;
        double cy = Bounds.Height * 0.5;
        OriginX = cx - (cx - OriginX) * ratio;
        OriginY = cy - (cy - OriginY) * ratio;
        InvalidateVisual();
    }

    public void ZoomOut()
    {
        double factor = 1.0 / 1.25;
        double oldScale = ScalePixelsPerUnit;
        ScalePixelsPerUnit = Math.Clamp(ScalePixelsPerUnit * factor, 8.0, 600.0);
        double ratio = ScalePixelsPerUnit / oldScale;
        double cx = Bounds.Width * 0.5;
        double cy = Bounds.Height * 0.5;
        OriginX = cx - (cx - OriginX) * ratio;
        OriginY = cy - (cy - OriginY) * ratio;
        InvalidateVisual();
    }

    public Func<double, double> GetOrCompilePrimaryFunction()
    {
        // If Functions collection exists and has items, use the first visible function
        if (Functions != null)
        {
            var first = Functions.FirstOrDefault(f => f.IsVisible && f.CompiledFunc != null);
            if (first?.CompiledFunc != null)
            {
                return first.CompiledFunc;
            }
        }

        if (_cachedFunc == null || _lastParsedExpr != Expression || _lastA != ParamA || _lastB != ParamB || _lastC != ParamC)
        {
            try
            {
                _cachedAst = MathParser.Parse(Expression);
                _lastParsedExpr = Expression;
            }
            catch
            {
                _cachedAst = new NumberNode(0);
            }

            var constants = new Dictionary<string, double>
            {
                ["a"] = ParamA,
                ["b"] = ParamB,
                ["c"] = ParamC,
                ["k"] = ParamA
            };

            _cachedFunc = ExpressionCompiler.Compile1D(_cachedAst, "x", constants);
            _lastA = ParamA;
            _lastB = ParamB;
            _lastC = ParamC;
        }

        return _cachedFunc;
    }

    private MathNode? GetOrParseAst()
    {
        if (Functions != null)
        {
            var first = Functions.FirstOrDefault(f => f.IsVisible && f.Ast != null);
            if (first?.Ast != null) return first.Ast;
        }

        if (_cachedAst == null && !string.IsNullOrWhiteSpace(Expression))
        {
            try { _cachedAst = MathParser.Parse(Expression); } catch { }
        }
        return _cachedAst;
    }

    // Coordinate conversions
    public Point MathToScreen(double x, double y)
    {
        double ox = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        double oy = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
        return new Point(ox + x * ScalePixelsPerUnit, oy - y * ScalePixelsPerUnit);
    }

    public Point ScreenToMath(Point screen)
    {
        double ox = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        double oy = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
        return new Point((screen.X - ox) / ScalePixelsPerUnit, (oy - screen.Y) / ScalePixelsPerUnit);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);
        _hoverScreenPos = pos;

        if (_isPanning)
        {
            OriginX = _panStartOriginX + (pos.X - _panStartPoint.X);
            OriginY = _panStartOriginY + (pos.Y - _panStartPoint.Y);
            InvalidateVisual();
            return;
        }

        if (_draggedBound == 1)
        {
            var math = ScreenToMath(pos);
            IntegrationA = Math.Round(math.X, 2);
            InvalidateVisual();
            return;
        }

        if (_draggedBound == 2)
        {
            var math = ScreenToMath(pos);
            IntegrationB = Math.Round(math.X, 2);
            InvalidateVisual();
            return;
        }

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hoverScreenPos = null;
        _isPanning = false;
        _draggedBound = 0;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);

        var screenA = MathToScreen(IntegrationA, 0);
        var screenB = MathToScreen(IntegrationB, 0);

        if (Math.Abs(pos.X - screenA.X) < 14.0)
        {
            _draggedBound = 1;
            return;
        }
        if (Math.Abs(pos.X - screenB.X) < 14.0)
        {
            _draggedBound = 2;
            return;
        }

        _isPanning = true;
        _panStartPoint = pos;
        _panStartOriginX = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        _panStartOriginY = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
        _draggedBound = 0;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pos = e.GetPosition(this);
        var mathBefore = ScreenToMath(pos);

        double factor = e.Delta.Y > 0 ? 1.15 : 1.0 / 1.15;
        ScalePixelsPerUnit = Math.Clamp(ScalePixelsPerUnit * factor, 8.0, 600.0);

        OriginX = pos.X - mathBefore.X * ScalePixelsPerUnit;
        OriginY = pos.Y + mathBefore.Y * ScalePixelsPerUnit;

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        if (OriginX == 0.0 && OriginY == 0.0)
        {
            OriginX = w * 0.5;
            OriginY = h * 0.5;
        }

        var tokens = MathThemeService.Instance.Tokens;

        // 1. Canvas Background
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        // 2. Coordinate Grid & Axes
        RenderGridAndAxes(context, w, h, tokens);

        // 3. Slope Field (if enabled)
        var primaryF = GetOrCompilePrimaryFunction();
        if (ShowSlopeField)
        {
            RenderSlopeField(context, primaryF, w, h, tokens);
        }

        // 4. Multi-Function or Single Function Curves
        var visibleFuncs = Functions?.Where(f => f.IsVisible && f.CompiledFunc != null).ToList();

        if (visibleFuncs != null && visibleFuncs.Count > 0)
        {
            // Area between curves (if 2+ functions active and toggle enabled)
            if (ShowAreaBetweenCurves && visibleFuncs.Count >= 2)
            {
                RenderAreaBetweenCurves(context, visibleFuncs[0].CompiledFunc!, visibleFuncs[1].CompiledFunc!, tokens);
            }

            // Draw Riemann sums on primary
            if (ShowRiemann && visibleFuncs.Count > 0)
            {
                RenderRiemannPartitions(context, visibleFuncs[0].CompiledFunc!, tokens);
            }

            // Draw all active curves
            foreach (var fn in visibleFuncs)
            {
                RenderCurveWithStyle(context, fn.CompiledFunc!, w, h, fn.Color, 2.4, false);

                if (fn.ShowDerivative && fn.CompiledDerivative != null)
                {
                    RenderCurveWithStyle(context, fn.CompiledDerivative, w, h, fn.Color, 1.5, true);
                }
            }

            // Intersections
            if (visibleFuncs.Count >= 2)
            {
                RenderIntersections(context, visibleFuncs[0].CompiledFunc!, visibleFuncs[1].CompiledFunc!, w, tokens);
            }

            // Taylor Series Polynomial Approximation (if enabled)
            if (ShowTaylorSeries)
            {
                var primaryItem = visibleFuncs.FirstOrDefault(f => f.Ast != null);
                if (primaryItem?.Ast != null)
                {
                    var (taylorFunc, _) = TaylorSeries.GenerateTaylorSeries(primaryItem.Ast, TaylorCenter, TaylorDegree);
                    RenderCurveWithStyle(context, taylorFunc, w, h, tokens.AccentAmberBrush, 2.0, true);

                    double cy = primaryItem.CompiledFunc?.Invoke(TaylorCenter) ?? 0.0;
                    var centerPt = MathToScreen(TaylorCenter, cy);
                    context.DrawEllipse(tokens.AccentAmberBrush, null, centerPt, 5.0, 5.0);
                    context.DrawEllipse(null, new Pen(Brushes.White, 1.8), centerPt, 7.5, 7.5);
                }
            }
        }
        else
        {
            // Fallback: single primary expression
            if (ShowRiemann)
            {
                RenderRiemannPartitions(context, primaryF, tokens);
            }
            RenderCurveWithStyle(context, primaryF, w, h, tokens.AccentCyanBrush, 2.5, false);

            if (ShowTaylorSeries)
            {
                var ast = GetOrParseAst();
                if (ast != null)
                {
                    var (taylorFunc, _) = TaylorSeries.GenerateTaylorSeries(ast, TaylorCenter, TaylorDegree);
                    RenderCurveWithStyle(context, taylorFunc, w, h, tokens.AccentAmberBrush, 2.0, true);

                    double cy = primaryF(TaylorCenter);
                    var centerPt = MathToScreen(TaylorCenter, cy);
                    context.DrawEllipse(tokens.AccentAmberBrush, null, centerPt, 5.0, 5.0);
                    context.DrawEllipse(null, new Pen(Brushes.White, 1.8), centerPt, 7.5, 7.5);
                }
            }
        }

        // 5. Critical Points Scanner (Roots, Max, Min, Inflection)
        if (ShowCriticalPoints)
        {
            RenderCriticalPoints(context, primaryF, w, tokens);
        }

        // 6. Integration Bound Handles (a, b)
        RenderIntegrationBounds(context, tokens, h);

        // 7. Interactive Math Probe & Secant Tangent Visualizer
        if (_hoverScreenPos.HasValue)
        {
            if (ShowSecant)
            {
                RenderSecantVisualizer(context, primaryF, _hoverScreenPos.Value, tokens);
            }
            else if (ShowProbe)
            {
                RenderMathProbe(context, primaryF, _hoverScreenPos.Value, tokens);
            }
        }
    }

    private void RenderGridAndAxes(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        double ox = OriginX;
        double oy = OriginY;

        double targetPixelStep = 60.0;
        double mathStep = Math.Pow(10, Math.Floor(Math.Log10(targetPixelStep / ScalePixelsPerUnit)));
        if (mathStep * ScalePixelsPerUnit < targetPixelStep * 0.5) mathStep *= 2.0;
        if (mathStep * ScalePixelsPerUnit < targetPixelStep * 0.75) mathStep *= 2.5;

        double pixelStep = mathStep * ScalePixelsPerUnit;

        // Subgrid lines
        double subPixelStep = pixelStep / 5.0;
        if (subPixelStep > 12.0)
        {
            double subStartX = (ox % subPixelStep);
            for (double x = subStartX; x < w; x += subPixelStep)
            {
                context.DrawLine(tokens.GridMinorPen, new Point(x, 0), new Point(x, h));
            }
            double subStartY = (oy % subPixelStep);
            for (double y = subStartY; y < h; y += subPixelStep)
            {
                context.DrawLine(tokens.GridMinorPen, new Point(0, y), new Point(w, y));
            }
        }

        // Major grid lines
        double startX = (ox % pixelStep);
        for (double x = startX; x < w; x += pixelStep)
        {
            context.DrawLine(tokens.GridMajorPen, new Point(x, 0), new Point(x, h));
        }

        double startY = (oy % pixelStep);
        for (double y = startY; y < h; y += pixelStep)
        {
            context.DrawLine(tokens.GridMajorPen, new Point(0, y), new Point(w, y));
        }

        // Primary X and Y axes
        if (ox >= 0 && ox <= w)
        {
            context.DrawLine(tokens.GridAxisPen, new Point(ox, 0), new Point(ox, h));
        }
        if (oy >= 0 && oy <= h)
        {
            context.DrawLine(tokens.GridAxisPen, new Point(0, oy), new Point(w, oy));
        }

        // Numerical Labels on Axes
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Regular);
        for (double x = startX; x < w; x += pixelStep)
        {
            double val = (x - ox) / ScalePixelsPerUnit;
            if (Math.Abs(val) < 1e-6) continue;
            var text = new FormattedText(
                val.ToString("G3", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                10,
                tokens.TextMutedBrush);

            double labelY = Math.Clamp(oy + 4.0, 4.0, h - 16.0);
            context.DrawText(text, new Point(x - text.Width * 0.5, labelY));
        }

        for (double y = startY; y < h; y += pixelStep)
        {
            double val = (oy - y) / ScalePixelsPerUnit;
            if (Math.Abs(val) < 1e-6) continue;
            var text = new FormattedText(
                val.ToString("G3", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                10,
                tokens.TextMutedBrush);

            double labelX = Math.Clamp(ox + 6.0, 4.0, w - 30.0);
            context.DrawText(text, new Point(labelX, y - text.Height * 0.5));
        }
    }

    private void RenderCurveWithStyle(DrawingContext context, Func<double, double> f, double w, double h, IBrush brush, double thickness, bool isDashed)
    {
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            bool hasStarted = false;
            double stepPx = 1.5;

            for (double px = 0; px <= w; px += stepPx)
            {
                double mx = (px - OriginX) / ScalePixelsPerUnit;
                double my = f(mx);

                if (double.IsNaN(my) || double.IsInfinity(my))
                {
                    hasStarted = false;
                    continue;
                }

                double py = OriginY - my * ScalePixelsPerUnit;

                if (py < -h * 2.0 || py > h * 3.0)
                {
                    hasStarted = false;
                    continue;
                }

                var pt = new Point(px, py);
                if (!hasStarted)
                {
                    ctx.BeginFigure(pt, false);
                    hasStarted = true;
                }
                else
                {
                    ctx.LineTo(pt);
                }
            }
        }

        var pen = new Pen(brush, thickness);
        if (isDashed)
        {
            pen.DashStyle = DashStyle.Dash;
        }
        context.DrawGeometry(null, pen, geom);
    }

    private void RenderSlopeField(DrawingContext context, Func<double, double> f, double w, double h, MathThemeTokens tokens)
    {
        double needleSpacing = 36.0;
        var needlePen = new Pen(tokens.AccentPurpleBrush, 1.2);

        for (double px = 18.0; px < w; px += needleSpacing)
        {
            for (double py = 18.0; py < h; py += needleSpacing)
            {
                var math = ScreenToMath(new Point(px, py));
                double slope = FunctionAnalyzer.FirstDerivative(f, math.X);
                if (double.IsNaN(slope) || double.IsInfinity(slope)) continue;

                double angle = Math.Atan(slope);
                double halfLen = 10.0;
                double dx = Math.Cos(angle) * halfLen;
                double dy = -Math.Sin(angle) * halfLen;

                context.DrawLine(needlePen, new Point(px - dx, py - dy), new Point(px + dx, py + dy));
            }
        }
    }

    private void RenderRiemannPartitions(DrawingContext context, Func<double, double> f, MathThemeTokens tokens)
    {
        double a = Math.Min(IntegrationA, IntegrationB);
        double b = Math.Max(IntegrationA, IntegrationB);
        int n = Math.Max(RiemannN, 1);
        double dx = (b - a) / n;

        var fillBrush = new SolidColorBrush(Color.FromArgb(48, 124, 77, 255));
        var strokePen = new Pen(tokens.AccentPurpleBrush, 1.0);

        for (int i = 0; i < n; i++)
        {
            double xLeft = a + i * dx;
            double xRight = xLeft + dx;

            double sampleX = RiemannRule switch
            {
                RiemannRule.Left => xLeft,
                RiemannRule.Right => xRight,
                RiemannRule.Midpoint => (xLeft + xRight) * 0.5,
                RiemannRule.Trapezoid => (xLeft + xRight) * 0.5,
                RiemannRule.Simpson => (xLeft + xRight) * 0.5,
                _ => (xLeft + xRight) * 0.5
            };

            double yVal = f(sampleX);
            if (double.IsNaN(yVal) || double.IsInfinity(yVal)) continue;

            var pTopLeft = MathToScreen(xLeft, Math.Max(yVal, 0.0));
            var pBottomRight = MathToScreen(xRight, Math.Min(yVal, 0.0));

            double rectX = Math.Min(pTopLeft.X, pBottomRight.X);
            double rectY = Math.Min(pTopLeft.Y, pBottomRight.Y);
            double rectW = Math.Abs(pBottomRight.X - pTopLeft.X);
            double rectH = Math.Abs(pBottomRight.Y - pTopLeft.Y);

            var rect = new Rect(rectX, rectY, rectW, rectH);
            context.FillRectangle(fillBrush, rect);
            context.DrawRectangle(null, strokePen, rect);
        }
    }

    private void RenderAreaBetweenCurves(DrawingContext context, Func<double, double> f1, Func<double, double> f2, MathThemeTokens tokens)
    {
        double a = Math.Min(IntegrationA, IntegrationB);
        double b = Math.Max(IntegrationA, IntegrationB);
        double step = (b - a) / 100.0;
        if (step <= 0) return;

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            var startTop = MathToScreen(a, f1(a));
            ctx.BeginFigure(startTop, true);

            // Forward along f1
            for (double x = a + step; x <= b; x += step)
            {
                ctx.LineTo(MathToScreen(x, f1(x)));
            }
            ctx.LineTo(MathToScreen(b, f1(b)));

            // Backward along f2
            for (double x = b; x >= a; x -= step)
            {
                ctx.LineTo(MathToScreen(x, f2(x)));
            }
        }

        var dualBrush = new SolidColorBrush(Color.FromArgb(55, 6, 182, 212));
        context.DrawGeometry(dualBrush, new Pen(tokens.AccentCyanBrush, 1.0), geom);
    }

    private void RenderIntersections(DrawingContext context, Func<double, double> f1, Func<double, double> f2, double w, MathThemeTokens tokens)
    {
        double xMin = ScreenToMath(new Point(0, 0)).X;
        double xMax = ScreenToMath(new Point(w, 0)).X;

        var points = FunctionAnalyzer.FindIntersections(f1, f2, Math.Min(xMin, xMax), Math.Max(xMin, xMax));
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

        foreach (var p in points)
        {
            var screenPt = MathToScreen(p.X, p.Y);
            context.DrawEllipse(tokens.AccentEmeraldBrush, null, screenPt, 5.0, 5.0);
            context.DrawEllipse(null, new Pen(Brushes.White, 1.5), screenPt, 7.5, 7.5);

            var label = new FormattedText($"({p.X:F2}, {p.Y:F2})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, tokens.AccentEmeraldBrush);
            context.DrawText(label, new Point(screenPt.X + 8.0, screenPt.Y - 14.0));
        }
    }

    private void RenderCriticalPoints(DrawingContext context, Func<double, double> f, double w, MathThemeTokens tokens)
    {
        double xMin = ScreenToMath(new Point(0, 0)).X;
        double xMax = ScreenToMath(new Point(w, 0)).X;

        var points = FunctionAnalyzer.FindCriticalPoints(f, Math.Min(xMin, xMax), Math.Max(xMin, xMax));
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

        foreach (var p in points)
        {
            var screenPt = MathToScreen(p.X, p.Y);
            if (screenPt.X < 0 || screenPt.X > w || screenPt.Y < 0 || screenPt.Y > Bounds.Height) continue;

            IBrush pointBrush = p.Kind switch
            {
                CriticalPointKind.Root => tokens.AccentCyanBrush,
                CriticalPointKind.LocalMaximum => tokens.AccentEmeraldBrush,
                CriticalPointKind.LocalMinimum => tokens.AccentRoseBrush,
                CriticalPointKind.InflectionPoint => tokens.AccentAmberBrush,
                _ => tokens.TextPrimaryBrush
            };

            // Distinct marker shape
            if (p.Kind == CriticalPointKind.Root)
            {
                context.DrawEllipse(pointBrush, null, screenPt, 4.0, 4.0);
                context.DrawEllipse(null, new Pen(Brushes.White, 1.2), screenPt, 6.0, 6.0);
            }
            else if (p.Kind == CriticalPointKind.LocalMaximum)
            {
                // Triangle Up
                var g = new StreamGeometry();
                using (var c = g.Open())
                {
                    c.BeginFigure(new Point(screenPt.X, screenPt.Y - 6.0), true);
                    c.LineTo(new Point(screenPt.X + 5.0, screenPt.Y + 4.0));
                    c.LineTo(new Point(screenPt.X - 5.0, screenPt.Y + 4.0));
                }
                context.DrawGeometry(pointBrush, new Pen(Brushes.White, 1.0), g);
            }
            else if (p.Kind == CriticalPointKind.LocalMinimum)
            {
                // Triangle Down
                var g = new StreamGeometry();
                using (var c = g.Open())
                {
                    c.BeginFigure(new Point(screenPt.X, screenPt.Y + 6.0), true);
                    c.LineTo(new Point(screenPt.X + 5.0, screenPt.Y - 4.0));
                    c.LineTo(new Point(screenPt.X - 5.0, screenPt.Y - 4.0));
                }
                context.DrawGeometry(pointBrush, new Pen(Brushes.White, 1.0), g);
            }
            else
            {
                // Diamond for inflection
                var g = new StreamGeometry();
                using (var c = g.Open())
                {
                    c.BeginFigure(new Point(screenPt.X, screenPt.Y - 5.0), true);
                    c.LineTo(new Point(screenPt.X + 5.0, screenPt.Y));
                    c.LineTo(new Point(screenPt.X, screenPt.Y + 5.0));
                    c.LineTo(new Point(screenPt.X - 5.0, screenPt.Y));
                }
                context.DrawGeometry(pointBrush, new Pen(Brushes.White, 1.0), g);
            }

            var text = new FormattedText(p.Label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, pointBrush);
            context.DrawText(text, new Point(screenPt.X + 7.0, screenPt.Y - 8.0));
        }
    }

    private void RenderIntegrationBounds(DrawingContext context, MathThemeTokens tokens, double h)
    {
        var ptA = MathToScreen(IntegrationA, 0);
        var ptB = MathToScreen(IntegrationB, 0);

        var boundPenA = new Pen(tokens.AccentCyanBrush, 1.5) { DashStyle = DashStyle.Dash };
        var boundPenB = new Pen(tokens.AccentPurpleBrush, 1.5) { DashStyle = DashStyle.Dash };

        context.DrawLine(boundPenA, new Point(ptA.X, 0), new Point(ptA.X, h));
        context.DrawLine(boundPenB, new Point(ptB.X, 0), new Point(ptB.X, h));

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

        void DrawHandle(Point screenPt, string label, IBrush brush)
        {
            double r = 14.0;
            var handleRect = new Rect(screenPt.X - r, h - 34.0, r * 2.0, 24.0);
            context.FillRectangle(brush, handleRect, 4f);
            var text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 11, Brushes.White);
            context.DrawText(text, new Point(screenPt.X - text.Width * 0.5, h - 30.0));
        }

        DrawHandle(ptA, $"a={IntegrationA:F1}", tokens.AccentCyanBrush);
        DrawHandle(ptB, $"b={IntegrationB:F1}", tokens.AccentPurpleBrush);
    }

    private void RenderSecantVisualizer(DrawingContext context, Func<double, double> f, Point mousePos, MathThemeTokens tokens)
    {
        var math = ScreenToMath(mousePos);
        double x0 = math.X;
        double y0 = f(x0);
        double dx = Math.Max(SecantDeltaX, 0.001);
        double x1 = x0 + dx;
        double y1 = f(x1);

        if (double.IsNaN(y0) || double.IsNaN(y1)) return;

        var p0 = MathToScreen(x0, y0);
        var p1 = MathToScreen(x1, y1);
        var pCorner = MathToScreen(x1, y0);

        // 1. Slope Triangle: horizontal run (dx) and vertical rise (dy)
        var triPenX = new Pen(tokens.AccentCyanBrush, 1.5) { DashStyle = DashStyle.Dot };
        var triPenY = new Pen(tokens.AccentRoseBrush, 1.5) { DashStyle = DashStyle.Dot };
        context.DrawLine(triPenX, p0, pCorner);
        context.DrawLine(triPenY, pCorner, p1);

        // 2. Full extended Secant Line
        double slope = (y1 - y0) / dx;
        double extentX = 140.0 / ScalePixelsPerUnit;
        var pSecLeft = MathToScreen(x0 - extentX, y0 - slope * extentX);
        var pSecRight = MathToScreen(x1 + extentX, y1 + slope * extentX);
        context.DrawLine(new Pen(tokens.AccentAmberBrush, 2.0), pSecLeft, pSecRight);

        // 3. Dots on curve
        context.DrawEllipse(tokens.AccentCyanBrush, null, p0, 4.5, 4.5);
        context.DrawEllipse(tokens.AccentRoseBrush, null, p1, 4.5, 4.5);

        // 4. Secant HUD Card
        double trueTangent = FunctionAnalyzer.FirstDerivative(f, x0);
        string hud = $"Secant Slope Δy/Δx = {slope:F3}\n" +
                     $"True Tangent f'(x) = {trueTangent:F3}\n" +
                     $"Δx = {dx:F3}  |  Error: {Math.Abs(slope - trueTangent):F4}";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var formatted = new FormattedText(hud, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 11, tokens.TextPrimaryBrush);

        var cardRect = new Rect(p0.X + 16.0, p0.Y - 60.0, formatted.Width + 20.0, formatted.Height + 14.0);
        context.FillRectangle(tokens.SurfaceHighBrush, cardRect, 6f);
        context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.0), cardRect, 6f, 6f);
        context.DrawText(formatted, new Point(cardRect.X + 10.0, cardRect.Y + 7.0));
    }

    private void RenderMathProbe(DrawingContext context, Func<double, double> f, Point mousePos, MathThemeTokens tokens)
    {
        var math = ScreenToMath(mousePos);
        double x0 = math.X;
        double y0 = f(x0);

        if (double.IsNaN(y0) || double.IsInfinity(y0)) return;

        var curveScreenPt = MathToScreen(x0, y0);

        // 1. Probe dot on curve
        context.DrawEllipse(tokens.AccentAmberBrush, null, curveScreenPt, 5.0, 5.0);
        context.DrawEllipse(null, new Pen(Brushes.White, 1.5), curveScreenPt, 7.0, 7.0);

        // 2. Tangent Line
        if (ShowTangent)
        {
            double slope = FunctionAnalyzer.FirstDerivative(f, x0);
            double dx = 80.0 / ScalePixelsPerUnit;
            var pLeft = MathToScreen(x0 - dx, y0 - slope * dx);
            var pRight = MathToScreen(x0 + dx, y0 + slope * dx);
            context.DrawLine(tokens.TangentPen, pLeft, pRight);
        }

        // 3. Osculating Circle
        if (ShowOsculating)
        {
            var osc = FunctionAnalyzer.ComputeOsculatingCircle(f, x0);
            if (!double.IsInfinity(osc.Radius) && osc.Radius * ScalePixelsPerUnit < 800.0)
            {
                var centerScreen = MathToScreen(osc.CenterX, osc.CenterY);
                double rPixel = osc.Radius * ScalePixelsPerUnit;
                context.DrawEllipse(null, tokens.OsculatingPen, centerScreen, rPixel, rPixel);
            }
        }

        // 4. Probe HUD Readout Card
        double d1 = FunctionAnalyzer.FirstDerivative(f, x0);
        double d2 = FunctionAnalyzer.SecondDerivative(f, x0);
        double angleDeg = Math.Atan(d1) * (180.0 / Math.PI);

        string hudText = $"x: {x0:F3}  y: {y0:F3}\n" +
                         $"f'(x) = {d1:F3} (θ = {angleDeg:F1}°)\n" +
                         $"f''(x) = {d2:F3}";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Medium);
        var formatted = new FormattedText(
            hudText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            11,
            tokens.TextPrimaryBrush);

        double cardW = formatted.Width + 20.0;
        double cardH = formatted.Height + 14.0;
        double cardX = Math.Clamp(curveScreenPt.X + 16.0, 10.0, Bounds.Width - cardW - 10.0);
        double cardY = Math.Clamp(curveScreenPt.Y - cardH - 12.0, 10.0, Bounds.Height - cardH - 10.0);

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(tokens.SurfaceHighBrush, cardRect, 6f);
        context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.0), cardRect, 6f, 6f);
        context.DrawText(formatted, new Point(cardX + 10.0, cardY + 7.0));
    }
}
