using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Cryptography;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class EllipticCurveCanvas : Control
{
    public static readonly StyledProperty<string> DisplayModeProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, string>(nameof(DisplayMode), "real");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, string>(nameof(PresetName), "secp256k1");

    public static readonly StyledProperty<double> ParamAProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, double>(nameof(ParamA), -3.0);

    public static readonly StyledProperty<double> ParamBProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, double>(nameof(ParamB), 3.0);

    public static readonly StyledProperty<long> PrimePProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, long>(nameof(PrimeP), 97);

    public static readonly StyledProperty<long> FiniteAProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, long>(nameof(FiniteA), -3);

    public static readonly StyledProperty<long> FiniteBProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, long>(nameof(FiniteB), 7);

    public static readonly StyledProperty<bool> ShowTangentSecantProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, bool>(nameof(ShowTangentSecant), true);

    public static readonly StyledProperty<bool> ShowReflectionLinesProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, bool>(nameof(ShowReflectionLines), true);

    public static readonly StyledProperty<long> AlicePrivateKeyProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, long>(nameof(AlicePrivateKey), 5);

    public static readonly StyledProperty<long> BobPrivateKeyProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, long>(nameof(BobPrivateKey), 7);

    public static readonly StyledProperty<long> ScalarKProperty =
        AvaloniaProperty.Register<EllipticCurveCanvas, long>(nameof(ScalarK), 12);

    public string DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
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

    public long PrimeP
    {
        get => GetValue(PrimePProperty);
        set => SetValue(PrimePProperty, value);
    }

    public long FiniteA
    {
        get => GetValue(FiniteAProperty);
        set => SetValue(FiniteAProperty, value);
    }

    public long FiniteB
    {
        get => GetValue(FiniteBProperty);
        set => SetValue(FiniteBProperty, value);
    }

    public bool ShowTangentSecant
    {
        get => GetValue(ShowTangentSecantProperty);
        set => SetValue(ShowTangentSecantProperty, value);
    }

    public bool ShowReflectionLines
    {
        get => GetValue(ShowReflectionLinesProperty);
        set => SetValue(ShowReflectionLinesProperty, value);
    }

    public long AlicePrivateKey
    {
        get => GetValue(AlicePrivateKeyProperty);
        set => SetValue(AlicePrivateKeyProperty, value);
    }

    public long BobPrivateKey
    {
        get => GetValue(BobPrivateKeyProperty);
        set => SetValue(BobPrivateKeyProperty, value);
    }

    public long ScalarK
    {
        get => GetValue(ScalarKProperty);
        set => SetValue(ScalarKProperty, value);
    }

    private readonly EllipticCurveEngine _engine = new();

    // Interaction state
    private int _draggedPoint = 0; // 0=none, 1=P, 2=Q
    private Point? _lastMousePos;
    private double _viewCenterX = 0.0;
    private double _viewCenterY = 0.0;
    private double _unitsPerPixel = 0.018; // Scale

    // Typography
    private readonly Typeface _fontRegular = new(FontFamily.Default, FontStyle.Normal, FontWeight.Regular);
    private readonly Typeface _fontBold = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
    private readonly Typeface _fontCode = new("Consolas, Courier New, monospace");

    static EllipticCurveCanvas()
    {
        AffectsRender<EllipticCurveCanvas>(
            DisplayModeProperty,
            PresetNameProperty,
            ParamAProperty,
            ParamBProperty,
            PrimePProperty,
            FiniteAProperty,
            FiniteBProperty,
            ShowTangentSecantProperty,
            ShowReflectionLinesProperty,
            AlicePrivateKeyProperty,
            BobPrivateKeyProperty,
            ScalarKProperty);
    }

    public EllipticCurveCanvas()
    {
        ClipToBounds = true;
        SyncEngine();
    }

    private void SyncEngine()
    {
        _engine.A = ParamA;
        _engine.B = ParamB;
        _engine.FiniteA = FiniteA;
        _engine.FiniteB = FiniteB;
        _engine.PrimeP = PrimeP;
        _engine.AlicePrivateKey = AlicePrivateKey;
        _engine.BobPrivateKey = BobPrivateKey;
        _engine.ScalarK = ScalarK;

        if (DisplayMode == "finite") _engine.Mode = CurveDisplayMode.FiniteFieldGrid;
        else if (DisplayMode == "ecdh") _engine.Mode = CurveDisplayMode.EcdhKeyExchange;
        else if (DisplayMode == "ecdlp") _engine.Mode = CurveDisplayMode.EcdlpScalarScatter;
        else _engine.Mode = CurveDisplayMode.RealWeierstrass;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        if (props.IsLeftButtonPressed)
        {
            if (DisplayMode == "real")
            {
                // Check if clicking near P or Q
                var screenP = MathToScreen(_engine.PointP.X, _engine.PointP.Y);
                var screenQ = MathToScreen(_engine.PointQ.X, _engine.PointQ.Y);

                if (Distance(pt, screenP) < 24)
                {
                    _draggedPoint = 1;
                    e.Handled = true;
                    return;
                }
                if (Distance(pt, screenQ) < 24)
                {
                    _draggedPoint = 2;
                    e.Handled = true;
                    return;
                }

                // If clicked elsewhere, drag closest point
                var math = ScreenToMath(pt.X, pt.Y);
                var snapped = _engine.FindPointClosestToX(math.X, positiveY: math.Y >= 0);
                if (!snapped.IsInfinity)
                {
                    if (Distance(pt, screenP) <= Distance(pt, screenQ))
                    {
                        _engine.PointP = snapped;
                        _draggedPoint = 1;
                    }
                    else
                    {
                        _engine.PointQ = snapped;
                        _draggedPoint = 2;
                    }
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
            }
        }
        else if (props.IsRightButtonPressed)
        {
            _lastMousePos = pt;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggedPoint = 0;
        _lastMousePos = null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        if (_draggedPoint > 0 && DisplayMode == "real")
        {
            var math = ScreenToMath(pt.X, pt.Y);
            var snapped = _engine.FindPointClosestToX(math.X, positiveY: math.Y >= 0);
            if (!snapped.IsInfinity)
            {
                if (_draggedPoint == 1) _engine.PointP = snapped;
                else if (_draggedPoint == 2) _engine.PointQ = snapped;
                InvalidateVisual();
            }
            e.Handled = true;
            return;
        }

        if (props.IsRightButtonPressed && _lastMousePos.HasValue)
        {
            double dx = pt.X - _lastMousePos.Value.X;
            double dy = pt.Y - _lastMousePos.Value.Y;
            _viewCenterX -= dx * _unitsPerPixel;
            _viewCenterY += dy * _unitsPerPixel;
            _lastMousePos = pt;
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double zoomFactor = e.Delta.Y > 0 ? 0.9 : 1.1;
        _unitsPerPixel = Math.Clamp(_unitsPerPixel * zoomFactor, 0.002, 0.2);
        InvalidateVisual();
        e.Handled = true;
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private Point MathToScreen(double x, double y)
    {
        double cx = Math.Max(100, (Bounds.Width - 360) * 0.50);
        double cy = Bounds.Height * 0.50;
        double sx = cx + (x - _viewCenterX) / _unitsPerPixel;
        double sy = cy - (y - _viewCenterY) / _unitsPerPixel;
        return new Point(sx, sy);
    }

    private Point ScreenToMath(double sx, double sy)
    {
        double cx = Math.Max(100, (Bounds.Width - 360) * 0.50);
        double cy = Bounds.Height * 0.50;
        double x = _viewCenterX + (sx - cx) * _unitsPerPixel;
        double y = _viewCenterY - (sy - cy) * _unitsPerPixel;
        return new Point(x, y);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        SyncEngine();

        var bounds = Bounds;
        if (bounds.Width < 50 || bounds.Height < 50) return;

        var tokens = new MathThemeTokens();
        // Deep background fill
        context.FillRectangle(tokens.BgBrush, bounds);

        switch (_engine.Mode)
        {
            case CurveDisplayMode.RealWeierstrass:
                RenderRealWeierstrassMode(context, bounds, tokens);
                break;
            case CurveDisplayMode.FiniteFieldGrid:
                RenderFiniteFieldGridMode(context, bounds, tokens);
                break;
            case CurveDisplayMode.EcdhKeyExchange:
                RenderEcdhMode(context, bounds, tokens);
                break;
            case CurveDisplayMode.EcdlpScalarScatter:
                RenderEcdlpScatterMode(context, bounds, tokens);
                break;
        }

        // Render top status capsule
        RenderTopPillHud(context, bounds, tokens);
    }

    #region Real Weierstrass Rendering

    private void RenderRealWeierstrassMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        // 1. Draw Cartesian Coordinate Grid
        DrawGrid(context, bounds, tokens);

        // 2. Draw Real Curve branches
        DrawRealCurve(context, bounds, tokens);

        // 3. Compute Group Law Sum and Draw Tangent/Secant
        if (!_engine.PointP.IsInfinity && !_engine.PointQ.IsInfinity)
        {
            var (sum, slope, intercept, rPrime) = _engine.AddReal(_engine.PointP, _engine.PointQ);

            // Secant/Tangent line
            if (ShowTangentSecant && !double.IsInfinity(slope) && !double.IsNaN(slope))
            {
                double xMin = ScreenToMath(0, 0).X - 1.0;
                double xMax = ScreenToMath(bounds.Width - 380, 0).X + 1.0;
                double y1 = slope * xMin + intercept;
                double y2 = slope * xMax + intercept;

                var pt1 = MathToScreen(xMin, y1);
                var pt2 = MathToScreen(xMax, y2);
                var penSecant = new Pen(new SolidColorBrush(Color.FromArgb(200, 245, 158, 11)), 2.0, DashStyle.Dash);
                context.DrawLine(penSecant, pt1, pt2);
            }

            // Intermediate intersection point R'
            if (!rPrime.IsInfinity && ShowReflectionLines)
            {
                var screenRPrime = MathToScreen(rPrime.X, rPrime.Y);
                var screenR = MathToScreen(sum.X, sum.Y);

                // Reflection vertical line connecting R' and R
                var penReflect = new Pen(new SolidColorBrush(Color.FromArgb(180, 168, 85, 247)), 1.5, DashStyle.Dash);
                context.DrawLine(penReflect, screenRPrime, screenR);

                // Draw R' badge
                var brushRPrime = new SolidColorBrush(Color.FromArgb(160, 200, 200, 220));
                context.DrawEllipse(brushRPrime, null, screenRPrime, 5, 5);
                DrawText(context, "R' = -(P+Q)", screenRPrime.X + 8, screenRPrime.Y - 14, tokens.TextSecondaryBrush, 11, true);
            }

            // Draw Sum Point R = P + Q
            if (!sum.IsInfinity)
            {
                var screenR = MathToScreen(sum.X, sum.Y);
                // Glowing aura
                context.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 16, 185, 129)), null, screenR, 14, 14);
                context.DrawEllipse(tokens.AccentEmeraldBrush, new Pen(new SolidColorBrush(Colors.White), 2.0), screenR, 7, 7);
                DrawText(context, $"R = P + Q ({sum.X:F2}, {sum.Y:F2})", screenR.X + 10, screenR.Y - 8, tokens.AccentEmeraldBrush, 13, true);
            }
        }

        // 4. Draw interactive Points P and Q
        if (!_engine.PointP.IsInfinity)
        {
            var sp = MathToScreen(_engine.PointP.X, _engine.PointP.Y);
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 244, 63, 94)), null, sp, 14, 14);
            context.DrawEllipse(tokens.AccentRoseBrush, new Pen(new SolidColorBrush(Colors.White), 2.0), sp, 7, 7);
            DrawText(context, $"P ({_engine.PointP.X:F2}, {_engine.PointP.Y:F2})", sp.X + 10, sp.Y - 8, tokens.AccentRoseBrush, 13, true);
        }

        if (!_engine.PointQ.IsInfinity)
        {
            var sq = MathToScreen(_engine.PointQ.X, _engine.PointQ.Y);
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 6, 182, 212)), null, sq, 14, 14);
            context.DrawEllipse(tokens.AccentCyanBrush, new Pen(new SolidColorBrush(Colors.White), 2.0), sq, 7, 7);
            DrawText(context, $"Q ({_engine.PointQ.X:F2}, {_engine.PointQ.Y:F2})", sq.X + 10, sq.Y - 8, tokens.AccentCyanBrush, 13, true);
        }

        // 5. Draw Right Textbook Card
        RenderWeierstrassTheoryCard(context, bounds, tokens);
    }

    private void DrawGrid(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double xMin = ScreenToMath(0, 0).X;
        double xMax = ScreenToMath(bounds.Width - 380, 0).X;
        double yMin = ScreenToMath(0, bounds.Height).Y;
        double yMax = ScreenToMath(0, 0).Y;

        var penMajor = new Pen(new SolidColorBrush(Color.FromArgb(40, 168, 85, 247)), 1.0);
        var penAxis = new Pen(new SolidColorBrush(Color.FromArgb(160, 243, 244, 246)), 1.5);

        double step = 1.0;
        if ((xMax - xMin) > 20) step = 2.0;
        if ((xMax - xMin) > 40) step = 5.0;

        double startX = Math.Floor(xMin / step) * step;
        for (double x = startX; x <= xMax; x += step)
        {
            var p1 = MathToScreen(x, yMin);
            var p2 = MathToScreen(x, yMax);
            context.DrawLine(penMajor, p1, p2);
            if (Math.Abs(x) > 1e-4)
            {
                var labelPos = MathToScreen(x, 0);
                DrawText(context, x.ToString("G3"), labelPos.X - 8, labelPos.Y + 4, tokens.TextMutedBrush, 10);
            }
        }

        double startY = Math.Floor(yMin / step) * step;
        for (double y = startY; y <= yMax; y += step)
        {
            var p1 = MathToScreen(xMin, y);
            var p2 = MathToScreen(xMax, y);
            context.DrawLine(penMajor, p1, p2);
            if (Math.Abs(y) > 1e-4)
            {
                var labelPos = MathToScreen(0, y);
                DrawText(context, y.ToString("G3"), labelPos.X + 4, labelPos.Y - 6, tokens.TextMutedBrush, 10);
            }
        }

        // Draw Axes X and Y
        var axisX1 = MathToScreen(xMin, 0);
        var axisX2 = MathToScreen(xMax, 0);
        context.DrawLine(penAxis, axisX1, axisX2);

        var axisY1 = MathToScreen(0, yMin);
        var axisY2 = MathToScreen(0, yMax);
        context.DrawLine(penAxis, axisY1, axisY2);

        DrawText(context, "x", axisX2.X - 16, axisX2.Y + 6, tokens.TextSecondaryBrush, 12, true);
        DrawText(context, "y", axisY2.X + 8, axisY2.Y + 4, tokens.TextSecondaryBrush, 12, true);
    }

    private void DrawRealCurve(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double xMin = ScreenToMath(0, 0).X - 0.5;
        double xMax = ScreenToMath(bounds.Width - 380, 0).X + 0.5;
        double dx = _unitsPerPixel * 1.5; // pixel step


        var penCurve = new Pen(new SolidColorBrush(Color.FromRgb(6, 182, 212)), 2.5);
        var penGlow = new Pen(new SolidColorBrush(Color.FromArgb(40, 6, 182, 212)), 7.0);

        List<Point> currentUpper = new();
        List<Point> currentLower = new();

        for (double x = xMin; x <= xMax; x += dx)
        {
            double rhs = _engine.EvaluateRhs(x);
            if (rhs >= 0)
            {
                double y = Math.Sqrt(rhs);
                currentUpper.Add(MathToScreen(x, y));
                currentLower.Add(MathToScreen(x, -y));
            }
            else
            {
                if (currentUpper.Count > 1)
                {
                    DrawPolyline(context, penGlow, currentUpper);
                    DrawPolyline(context, penCurve, currentUpper);
                    DrawPolyline(context, penGlow, currentLower);
                    DrawPolyline(context, penCurve, currentLower);
                    currentUpper.Clear();
                    currentLower.Clear();
                }
            }
        }

        if (currentUpper.Count > 1)
        {
            DrawPolyline(context, penGlow, currentUpper);
            DrawPolyline(context, penCurve, currentUpper);
            DrawPolyline(context, penGlow, currentLower);
            DrawPolyline(context, penCurve, currentLower);
        }
    }

    private static void DrawPolyline(DrawingContext context, Pen pen, List<Point> points)
    {
        var geom = new StreamGeometry();
        using (var sgc = geom.Open())
        {
            sgc.BeginFigure(points[0], false);
            for (int i = 1; i < points.Count; i++)
            {
                sgc.LineTo(points[i]);
            }
            sgc.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geom);
    }

    private void RenderWeierstrassTheoryCard(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double cardW = 340;
        double cardX = bounds.Width - cardW - 20;
        double cardY = 70;
        double cardH = bounds.Height - 90;

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 24, 20, 39)), cardRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 168, 85, 247)), 1.5), cardRect);

        double ty = cardY + 16;
        DrawText(context, "WEIERSTRASS ELLIPTIC CURVE", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 22;
        DrawText(context, $"y² = x³ + ({_engine.A:F1})x + ({_engine.B:F1})", cardX + 16, ty, tokens.AccentCyanBrush, 16, true);
        ty += 28;

        // Discriminant badge
        double delta = _engine.Discriminant;
        string statusText = Math.Abs(delta) < 1e-4 ? "SINGULAR (Nodal/Cusp)" : (delta > 0 ? "SMOOTH (2 Components: Egg & Branch)" : "SMOOTH (1 Open Component)");
        var statusColor = Math.Abs(delta) < 1e-4 ? tokens.AccentRoseBrush : (delta > 0 ? tokens.AccentAmberBrush : tokens.AccentEmeraldBrush);

        DrawText(context, $"Discriminant Δ = -16(4a³ + 27b²) = {delta:F1}", cardX + 16, ty, tokens.TextPrimaryBrush, 12);
        ty += 18;
        DrawText(context, $"Classification: {statusText}", cardX + 16, ty, statusColor, 12, true);
        ty += 26;

        // Group law equations
        DrawText(context, "ABELIAN GROUP LAW (CHORD & TANGENT):", cardX + 16, ty, tokens.AccentPurpleBrush, 12, true);
        ty += 20;
        DrawText(context, "• Identity Element: Point at Infinity O", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Inverse: -P(x, y) = (x, -y)", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Secant Slope (P ≠ Q): m = (y₂ - y₁) / (x₂ - x₁)", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Tangent Slope (P = Q): m = (3x₁² + a) / (2y₁)", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Sum Point: x₃ = m² - x₁ - x₂, y₃ = m(x₁ - x₃) - y₁", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 26;

        // Current points table
        DrawText(context, "CURRENT EVALUATION:", cardX + 16, ty, tokens.TextSecondaryBrush, 12, true);
        ty += 20;
        DrawText(context, $"Point P: ({_engine.PointP.X:F3}, {_engine.PointP.Y:F3})", cardX + 16, ty, tokens.AccentRoseBrush, 12, true);
        ty += 18;
        DrawText(context, $"Point Q: ({_engine.PointQ.X:F3}, {_engine.PointQ.Y:F3})", cardX + 16, ty, tokens.AccentCyanBrush, 12, true);
        ty += 18;

        var (sum, slope, intercept, rPrime) = _engine.AddReal(_engine.PointP, _engine.PointQ);
        if (sum.IsInfinity)
        {
            DrawText(context, "Sum R = P + Q: O (Point at Infinity)", cardX + 16, ty, tokens.AccentEmeraldBrush, 12, true);
        }
        else
        {
            DrawText(context, $"Sum R = P + Q: ({sum.X:F3}, {sum.Y:F3})", cardX + 16, ty, tokens.AccentEmeraldBrush, 12, true);
            ty += 18;
            DrawText(context, $"Slope m = {slope:F3} | Intercept c = {intercept:F3}", cardX + 16, ty, tokens.AccentAmberBrush, 11);
        }
        ty += 28;

        // Interactive hints
        DrawText(context, "INTERACTION CONTROLS:", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 18;
        DrawText(context, "• Left-Drag Rose Handle: Move Point P", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Left-Drag Cyan Handle: Move Point Q", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Right-Drag: Pan Canvas Viewport", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Scroll Wheel: Smooth Zoom", cardX + 16, ty, tokens.TextMutedBrush, 10);
    }

    #endregion

    #region Finite Field Grid Rendering

    private void RenderFiniteFieldGridMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double plotSize = Math.Min(bounds.Width - 380 - 60, bounds.Height - 120);
        double left = 40;
        double top = 80;
        var gridRect = new Rect(left, top, plotSize, plotSize);

        // Grid background
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 20, 16, 34)), gridRect);
        context.DrawRectangle(new Pen(tokens.AccentPurpleBrush, 1.5), gridRect);

        long p = _engine.PrimeP;
        var points = EllipticCurveEngine.GetFinitePoints(_engine.FiniteA, _engine.FiniteB, p);
        var nonInfPoints = points.Where(pt => !pt.IsInfinity).ToList();

        // Draw toroidal grid lines
        var penGrid = new Pen(new SolidColorBrush(Color.FromArgb(25, 168, 85, 247)), 1.0);
        int numLines = (int)Math.Min(p, 20);
        for (int i = 0; i <= numLines; i++)
        {
            double frac = (double)i / numLines;
            context.DrawLine(penGrid, new Point(left + frac * plotSize, top), new Point(left + frac * plotSize, top + plotSize));
            context.DrawLine(penGrid, new Point(left, top + frac * plotSize), new Point(left + plotSize, top + frac * plotSize));
        }

        // Draw horizontal reflection centerline y = p/2
        var penCenter = new Pen(new SolidColorBrush(Color.FromArgb(70, 245, 158, 11)), 1.0, DashStyle.Dash);
        context.DrawLine(penCenter, new Point(left, top + plotSize * 0.5), new Point(left + plotSize, top + plotSize * 0.5));

        // Draw scatter points
        var brushPoint = tokens.AccentCyanBrush;
        var penHalo = new Pen(new SolidColorBrush(Color.FromArgb(50, 6, 182, 212)), 4.0);

        foreach (var pt in nonInfPoints)
        {
            double sx = left + ((double)pt.X / (p - 1)) * plotSize;
            double sy = top + (1.0 - (double)pt.Y / (p - 1)) * plotSize; // Invert Y
            var sp = new Point(sx, sy);

            context.DrawEllipse(null, penHalo, sp, 4.5, 4.5);
            context.DrawEllipse(brushPoint, null, sp, 3.0, 3.0);
        }

        // Draw selected P and Q and their sum
        if (nonInfPoints.Count > 4)
        {
            var pPt = nonInfPoints[1];
            var qPt = nonInfPoints[3];
            var sumPt = EllipticCurveEngine.AddFinite(pPt, qPt, _engine.FiniteA, p);

            DrawFiniteMarker(context, left, top, plotSize, p, pPt, tokens.AccentRoseBrush, "P");
            DrawFiniteMarker(context, left, top, plotSize, p, qPt, tokens.AccentCyanBrush, "Q");
            if (!sumPt.IsInfinity)
            {
                DrawFiniteMarker(context, left, top, plotSize, p, sumPt, tokens.AccentEmeraldBrush, "P + Q");
            }
        }

        // Theory card on right
        RenderFiniteTheoryCard(context, bounds, tokens, points.Count, p);
    }

    private void DrawFiniteMarker(DrawingContext context, double left, double top, double plotSize, long p, FinitePoint pt, IBrush brush, string label)
    {
        double sx = left + ((double)pt.X / (p - 1)) * plotSize;
        double sy = top + (1.0 - (double)pt.Y / (p - 1)) * plotSize;
        var sp = new Point(sx, sy);

        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), null, sp, 10, 10);
        context.DrawEllipse(brush, new Pen(new SolidColorBrush(Colors.White), 1.5), sp, 5.5, 5.5);
        DrawText(context, $"{label}({pt.X}, {pt.Y})", sx + 8, sy - 8, brush, 11, true);
    }

    private void RenderFiniteTheoryCard(DrawingContext context, Rect bounds, MathThemeTokens tokens, int pointCount, long p)
    {
        double cardW = 340;
        double cardX = bounds.Width - cardW - 20;
        double cardY = 70;
        double cardH = bounds.Height - 90;

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 24, 20, 39)), cardRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 168, 85, 247)), 1.5), cardRect);

        double ty = cardY + 16;
        DrawText(context, "FINITE FIELD ELLIPTIC CURVE", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 22;
        DrawText(context, $"y² ≡ x³ + ({_engine.FiniteA})x + ({_engine.FiniteB}) (mod {p})", cardX + 16, ty, tokens.AccentCyanBrush, 15, true);
        ty += 28;

        // Hasse's Theorem
        double bound = 2.0 * Math.Sqrt(p);
        double diff = Math.Abs(pointCount - (p + 1));
        bool hasseHolds = diff <= bound;

        DrawText(context, "HASSE'S BOUND THEOREM:", cardX + 16, ty, tokens.AccentPurpleBrush, 12, true);
        ty += 20;
        DrawText(context, $"|#E(F_p) - (p + 1)| ≤ 2√p", cardX + 16, ty, tokens.AccentAmberBrush, 13, true);
        ty += 18;
        DrawText(context, $"• Field Order p = {p}", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, $"• Group Order #E(F_p) = {pointCount} (with O)", cardX + 16, ty, tokens.TextPrimaryBrush, 11, true);
        ty += 16;
        DrawText(context, $"• Deviation: |{pointCount} - {p + 1}| = {diff:F1} ≤ {bound:F2}", cardX + 16, ty, tokens.AccentEmeraldBrush, 11, true);
        ty += 24;

        DrawText(context, "MODULAR ARITHMETIC GROUP LAW:", cardX + 16, ty, tokens.TextSecondaryBrush, 12, true);
        ty += 18;
        DrawText(context, "• Slope m ≡ (y₂ - y₁)(x₂ - x₁)⁻¹ (mod p)", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Doubling m ≡ (3x₁² + a)(2y₁)⁻¹ (mod p)", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Modular Inverse via Extended Euclidean", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Point addition wraps across Torus grid", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 26;

        DrawText(context, "CRYPTOGRAPHIC PROPERTIES:", cardX + 16, ty, tokens.TextSecondaryBrush, 12, true);
        ty += 18;
        DrawText(context, "• Abelian group under modular addition", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• No known sub-exponential discrete log attack", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• 256-bit ECC matches 3072-bit RSA security", cardX + 16, ty, tokens.AccentEmeraldBrush, 10, true);
    }

    #endregion

    #region ECDH Key Exchange Rendering

    private void RenderEcdhMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        var (alicePub, bobPub, sharedAlice, sharedBob, success) = _engine.RunEcdh();

        double colW = (bounds.Width - 80) / 3.0;
        double colY = 80;
        double colH = bounds.Height - 190;

        // Alice Column
        var aliceRect = new Rect(30, colY, colW, colH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 28, 20, 36)), aliceRect);
        context.DrawRectangle(new Pen(tokens.AccentRoseBrush, 1.5), aliceRect);

        double ay = colY + 16;
        DrawText(context, "ALICE (SENDER)", aliceRect.X + 16, ay, tokens.AccentRoseBrush, 14, true);
        ay += 26;
        DrawText(context, "Private Secret Key:", aliceRect.X + 16, ay, tokens.TextSecondaryBrush, 11);
        ay += 18;
        DrawText(context, $"d_A = {_engine.AlicePrivateKey}", aliceRect.X + 16, ay, tokens.AccentRoseBrush, 18, true);
        ay += 28;
        DrawText(context, "Public Key Computation:", aliceRect.X + 16, ay, tokens.TextSecondaryBrush, 11);
        ay += 18;
        DrawText(context, $"Q_A = d_A · G = ({alicePub.X}, {alicePub.Y})", aliceRect.X + 16, ay, tokens.AccentCyanBrush, 14, true);
        ay += 32;
        DrawText(context, "Shared Secret Derivation:", aliceRect.X + 16, ay, tokens.TextSecondaryBrush, 11);
        ay += 18;
        DrawText(context, $"S = d_A · Q_B = ({sharedAlice.X}, {sharedAlice.Y})", aliceRect.X + 16, ay, tokens.AccentEmeraldBrush, 14, true);

        // Bob Column
        var bobRect = new Rect(bounds.Width - colW - 30, colY, colW, colH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 20, 26, 36)), bobRect);
        context.DrawRectangle(new Pen(tokens.AccentCyanBrush, 1.5), bobRect);

        double by = colY + 16;
        DrawText(context, "BOB (RECIPIENT)", bobRect.X + 16, by, tokens.AccentCyanBrush, 14, true);
        by += 26;
        DrawText(context, "Private Secret Key:", bobRect.X + 16, by, tokens.TextSecondaryBrush, 11);
        by += 18;
        DrawText(context, $"d_B = {_engine.BobPrivateKey}", bobRect.X + 16, by, tokens.AccentCyanBrush, 18, true);
        by += 28;
        DrawText(context, "Public Key Computation:", bobRect.X + 16, by, tokens.TextSecondaryBrush, 11);
        by += 18;
        DrawText(context, $"Q_B = d_B · G = ({bobPub.X}, {bobPub.Y})", bobRect.X + 16, by, tokens.AccentRoseBrush, 14, true);
        by += 32;
        DrawText(context, "Shared Secret Derivation:", bobRect.X + 16, by, tokens.TextSecondaryBrush, 11);
        by += 18;
        DrawText(context, $"S = d_B · Q_A = ({sharedBob.X}, {sharedBob.Y})", bobRect.X + 16, by, tokens.AccentEmeraldBrush, 14, true);

        // Center Insecure Channel Column
        var centerRect = new Rect(aliceRect.Right + 10, colY, colW - 20, colH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(200, 24, 20, 39)), centerRect);
        context.DrawRectangle(new Pen(tokens.AccentAmberBrush, 1.5), centerRect);

        double cy = colY + 16;
        DrawText(context, "PUBLIC INSECURE CHANNEL", centerRect.X + 16, cy, tokens.AccentAmberBrush, 13, true);
        cy += 24;
        DrawText(context, $"Curve: y² ≡ x³ + {_engine.FiniteA}x + {_engine.FiniteB} (mod {_engine.PrimeP})", centerRect.X + 16, cy, tokens.TextPrimaryBrush, 11);
        cy += 18;
        DrawText(context, $"Generator Point G = ({_engine.GeneratorG.X}, {_engine.GeneratorG.Y})", centerRect.X + 16, cy, tokens.AccentPurpleBrush, 12, true);
        cy += 30;

        // Exchange arrows
        DrawText(context, "─── Public Q_A ───►", centerRect.X + 16, cy, tokens.AccentCyanBrush, 13, true);
        cy += 24;
        DrawText(context, "◄─── Public Q_B ───", centerRect.X + 16, cy, tokens.AccentRoseBrush, 13, true);
        cy += 30;

        DrawText(context, "Eavesdropper sees:", centerRect.X + 16, cy, tokens.TextMutedBrush, 11);
        cy += 16;
        DrawText(context, $"G, Q_A, Q_B (mod {_engine.PrimeP})", centerRect.X + 16, cy, tokens.TextPrimaryBrush, 11);
        cy += 18;
        DrawText(context, "Cannot deduce d_A or d_B", centerRect.X + 16, cy, tokens.AccentRoseBrush, 11, true);
        cy += 16;
        DrawText(context, "without solving ECDLP!", centerRect.X + 16, cy, tokens.AccentRoseBrush, 11, true);

        // Bottom Result Capsule
        var resultRect = new Rect(30, bounds.Height - 90, bounds.Width - 60, 60);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(240, 16, 26, 22)), resultRect);
        context.DrawRectangle(new Pen(tokens.AccentEmeraldBrush, 2.0), resultRect);

        string matchMsg = success
            ? $"✓ SHARED SECRET AGREED: S = d_A · (d_B · G) = d_B · (d_A · G) = ({sharedAlice.X}, {sharedAlice.Y})"
            : "SHARED SECRET CALCULATION FAILED";
        DrawText(context, matchMsg, resultRect.X + 24, resultRect.Y + 18, tokens.AccentEmeraldBrush, 13, true);
    }

    #endregion

    #region ECDLP Scatter Trajectory Rendering

    private void RenderEcdlpScatterMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double plotSize = Math.Min(bounds.Width - 380 - 60, bounds.Height - 120);
        double left = 40;
        double top = 80;
        var gridRect = new Rect(left, top, plotSize, plotSize);

        // Background
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 20, 16, 34)), gridRect);
        context.DrawRectangle(new Pen(tokens.AccentPurpleBrush, 1.5), gridRect);

        long p = _engine.PrimeP;
        var trajectory = _engine.GetScalarHopTrajectory(_engine.GeneratorG, _engine.ScalarK);

        // Grid lines
        var penGrid = new Pen(new SolidColorBrush(Color.FromArgb(25, 168, 85, 247)), 1.0);
        int numLines = (int)Math.Min(p, 20);
        for (int i = 0; i <= numLines; i++)
        {
            double frac = (double)i / numLines;
            context.DrawLine(penGrid, new Point(left + frac * plotSize, top), new Point(left + frac * plotSize, top + plotSize));
            context.DrawLine(penGrid, new Point(left, top + frac * plotSize), new Point(left + plotSize, top + frac * plotSize));
        }

        // Draw hopping vectors
        for (int i = 0; i < trajectory.Count - 1; i++)
        {
            var p1 = trajectory[i];
            var p2 = trajectory[i + 1];
            if (p1.IsInfinity || p2.IsInfinity) continue;

            double x1 = left + ((double)p1.X / (p - 1)) * plotSize;
            double y1 = top + (1.0 - (double)p1.Y / (p - 1)) * plotSize;
            double x2 = left + ((double)p2.X / (p - 1)) * plotSize;
            double y2 = top + (1.0 - (double)p2.Y / (p - 1)) * plotSize;

            double t = (double)i / Math.Max(1, trajectory.Count - 1);
            byte r = (byte)(168 + t * (6 - 168));
            byte g = (byte)(85 + t * (182 - 85));
            byte b = (byte)(247 + t * (212 - 247));
            var hopBrush = new SolidColorBrush(Color.FromArgb(180, r, g, b));
            var hopPen = new Pen(hopBrush, 1.5, DashStyle.Dash);

            context.DrawLine(hopPen, new Point(x1, y1), new Point(x2, y2));
        }

        // Draw points with step index labels
        for (int i = 0; i < trajectory.Count; i++)
        {
            var pt = trajectory[i];
            if (pt.IsInfinity) continue;

            double sx = left + ((double)pt.X / (p - 1)) * plotSize;
            double sy = top + (1.0 - (double)pt.Y / (p - 1)) * plotSize;
            var sp = new Point(sx, sy);

            IBrush ptBrush = (i == 0) ? tokens.AccentRoseBrush : ((i == trajectory.Count - 1) ? tokens.AccentEmeraldBrush : tokens.AccentCyanBrush);

            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), null, sp, 8, 8);
            context.DrawEllipse(ptBrush, new Pen(new SolidColorBrush(Colors.White), 1.0), sp, 4.5, 4.5);

            DrawText(context, $"{i + 1}G", sx + 6, sy - 6, ptBrush, 9, true);
        }

        // Theory card on right
        RenderEcdlpTheoryCard(context, bounds, tokens, trajectory.Count, p);
    }

    private void RenderEcdlpTheoryCard(DrawingContext context, Rect bounds, MathThemeTokens tokens, int stepCount, long p)
    {
        double cardW = 340;
        double cardX = bounds.Width - cardW - 20;
        double cardY = 70;
        double cardH = bounds.Height - 90;

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 24, 20, 39)), cardRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 168, 85, 247)), 1.5), cardRect);

        double ty = cardY + 16;
        DrawText(context, "ELLIPTIC CURVE DISCRETE LOGARITHM", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 22;
        DrawText(context, "THE ONE-WAY TRAPDOOR FUNCTION", cardX + 16, ty, tokens.AccentEmeraldBrush, 14, true);
        ty += 28;

        DrawText(context, "THE FORWARD COMPUTATION (EASY):", cardX + 16, ty, tokens.AccentCyanBrush, 12, true);
        ty += 18;
        DrawText(context, "• Given scalar k and point G, compute Q = k · G", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Double-and-Add Algorithm takes O(log k) steps", cardX + 16, ty, tokens.TextPrimaryBrush, 11);
        ty += 16;
        DrawText(context, $"• Illustrated steps: 1G → {stepCount}G", cardX + 16, ty, tokens.AccentAmberBrush, 11, true);
        ty += 26;

        DrawText(context, "THE REVERSE PROBLEM (INTRACTABLE):", cardX + 16, ty, tokens.AccentRoseBrush, 12, true);
        ty += 18;
        DrawText(context, "• Given G and Q = k · G, find k = log_G(Q)", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Best known general algorithm: Pollard's rho", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Computational complexity: O(√n) group ops", cardX + 16, ty, tokens.AccentRoseBrush, 11, true);
        ty += 16;
        DrawText(context, "• For 256-bit curves (secp256k1): ~2¹²⁸ ops", cardX + 16, ty, tokens.AccentRoseBrush, 11, true);
        ty += 16;
        DrawText(context, "  Takes billions of years for all supercomputers!", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 26;

        DrawText(context, "PSEUDO-RANDOM SCATTER:", cardX + 16, ty, tokens.AccentPurpleBrush, 12, true);
        ty += 18;
        DrawText(context, "Notice how the trajectory 1G, 2G, 3G jumps", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "chaotically across the discrete torus without any", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "discernible algebraic pattern. This guarantees security!", cardX + 16, ty, tokens.AccentEmeraldBrush, 10, true);
    }

    #endregion

    private void RenderTopPillHud(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double hudW = bounds.Width - 60;
        double hudH = 34;
        double hudX = 30;
        double hudY = 56;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 24, 20, 39)), hudRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 6, 182, 212)), 1.5), hudRect);

        string hudText;
        if (DisplayMode == "real")
        {
            var (sum, slope, _, _) = _engine.AddReal(_engine.PointP, _engine.PointQ);
            string sumStr = sum.IsInfinity ? "O (Infinity)" : $"({sum.X:F2}, {sum.Y:F2})";
            hudText = $"Elliptic Curve: y² = x³ + ({_engine.A:F1})x + ({_engine.B:F1})  |  Δ = {_engine.Discriminant:F1}  |  P = ({_engine.PointP.X:F2}, {_engine.PointP.Y:F2})  |  Q = ({_engine.PointQ.X:F2}, {_engine.PointQ.Y:F2})  |  Sum R = P + Q = {sumStr}";
        }
        else if (DisplayMode == "finite")
        {
            var pts = EllipticCurveEngine.GetFinitePoints(_engine.FiniteA, _engine.FiniteB, _engine.PrimeP);
            hudText = $"Finite Field E(F_{_engine.PrimeP}): y² ≡ x³ + {_engine.FiniteA}x + {_engine.FiniteB} (mod {_engine.PrimeP})  |  Order #E(F_p) = {pts.Count}  |  Hasse Bound 2√p = {2.0 * Math.Sqrt(_engine.PrimeP):F2}";
        }
        else if (DisplayMode == "ecdh")
        {
            var (qA, qB, sA, _, success) = _engine.RunEcdh();
            hudText = $"Diffie-Hellman Key Exchange on E(F_{_engine.PrimeP})  |  Alice Secret d_A={_engine.AlicePrivateKey}  |  Bob Secret d_B={_engine.BobPrivateKey}  |  Shared Key = ({sA.X}, {sA.Y})  |  Status: {(success ? "Verified Identical ✓" : "Failed")}";
        }
        else
        {
            hudText = $"ECDLP Scalar Trajectory: 1G → {_engine.ScalarK}G on E(F_{_engine.PrimeP})  |  One-Way Trapdoor  |  Complexity: O(√n) vs O(log k)";
        }

        DrawText(context, hudText, hudX + 16, hudY + 8, tokens.AccentCyanBrush, 12, true);
    }

    private void DrawText(DrawingContext context, string text, double x, double y, IBrush brush, double fontSize, bool bold = false)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            bold ? _fontBold : _fontRegular,
            fontSize,
            brush);
        context.DrawText(formatted, new Point(x, y));
    }
}
