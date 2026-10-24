using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Catastrophe;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class CatastropheCanvas : Control
{
    public static readonly StyledProperty<string> CatastropheModeProperty =
        AvaloniaProperty.Register<CatastropheCanvas, string>(nameof(CatastropheMode), "cusp");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<CatastropheCanvas, string>(nameof(PresetName), "cusp");

    public static readonly StyledProperty<double> ParamAProperty =
        AvaloniaProperty.Register<CatastropheCanvas, double>(nameof(ParamA), -2.5);

    public static readonly StyledProperty<double> ParamBProperty =
        AvaloniaProperty.Register<CatastropheCanvas, double>(nameof(ParamB), 0.0);

    public static readonly StyledProperty<double> StateXProperty =
        AvaloniaProperty.Register<CatastropheCanvas, double>(nameof(StateX), 1.58);

    public static readonly StyledProperty<bool> ShowBifurcationSetProperty =
        AvaloniaProperty.Register<CatastropheCanvas, bool>(nameof(ShowBifurcationSet), true);

    public static readonly StyledProperty<bool> ShowPotentialWellProperty =
        AvaloniaProperty.Register<CatastropheCanvas, bool>(nameof(ShowPotentialWell), true);

    public static readonly StyledProperty<bool> ShowEquilibriumSurfaceProperty =
        AvaloniaProperty.Register<CatastropheCanvas, bool>(nameof(ShowEquilibriumSurface), true);

    public string CatastropheMode
    {
        get => GetValue(CatastropheModeProperty);
        set => SetValue(CatastropheModeProperty, value);
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

    public double StateX
    {
        get => GetValue(StateXProperty);
        set => SetValue(StateXProperty, value);
    }

    public bool ShowBifurcationSet
    {
        get => GetValue(ShowBifurcationSetProperty);
        set => SetValue(ShowBifurcationSetProperty, value);
    }

    public bool ShowPotentialWell
    {
        get => GetValue(ShowPotentialWellProperty);
        set => SetValue(ShowPotentialWellProperty, value);
    }

    public bool ShowEquilibriumSurface
    {
        get => GetValue(ShowEquilibriumSurfaceProperty);
        set => SetValue(ShowEquilibriumSurfaceProperty, value);
    }

    private readonly CatastropheEngine _engine = new();
    private readonly MathThemeTokens _tokens = MathThemeTokens.DarkElectric;
    private bool _isDraggingControl = false;
    private readonly List<Point> _hysteresisPath = new();

    // Fonts
    private readonly Typeface _fontRegular = new(FontFamily.Default, FontStyle.Normal, FontWeight.Regular);
    private readonly Typeface _fontBold = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
    private readonly Typeface _fontCode = new("Consolas, Courier New, monospace");

    static CatastropheCanvas()
    {
        AffectsRender<CatastropheCanvas>(
            CatastropheModeProperty,
            PresetNameProperty,
            ParamAProperty,
            ParamBProperty,
            StateXProperty,
            ShowBifurcationSetProperty,
            ShowPotentialWellProperty,
            ShowEquilibriumSurfaceProperty
        );
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var prop = e.GetCurrentPoint(this).Properties;

        if (prop.IsLeftButtonPressed)
        {
            _isDraggingControl = true;
            UpdateControlFromPoint(pt);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDraggingControl)
        {
            var pt = e.GetPosition(this);
            UpdateControlFromPoint(pt);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDraggingControl = false;
    }

    private void UpdateControlFromPoint(Point p)
    {
        var bounds = Bounds;
        if (bounds.Width < 100 || bounds.Height < 100) return;

        double ctrlW = bounds.Width * 0.44;
        double ctrlH = bounds.Height - 80;
        double ctrlX0 = 30;
        double ctrlY0 = 50;

        if (p.X >= ctrlX0 && p.X <= ctrlX0 + ctrlW && p.Y >= ctrlY0 && p.Y <= ctrlY0 + ctrlH)
        {
            // Map p.X in [ctrlX0, ctrlX0+ctrlW] to a in [-4, 1.5]
            // Map p.Y in [ctrlY0, ctrlY0+ctrlH] to b in [3.0, -3.0]
            double normX = (p.X - ctrlX0) / ctrlW;
            double normY = (p.Y - ctrlY0) / ctrlH;

            double newA = -4.0 + normX * 5.5;
            double newB = 3.0 - normY * 6.0;

            ParamA = Math.Round(newA, 2);
            ParamB = Math.Round(newB, 2);

            // Relax state x to reflect dynamical jump
            double currentX = StateX;
            double nextX = _engine.RelaxState(currentX, ParamA, ParamB, dt: 0.05, iterations: 120);
            StateX = Math.Round(nextX, 3);

            _hysteresisPath.Add(new Point(ParamA, ParamB));
            if (_hysteresisPath.Count > 120) _hysteresisPath.RemoveAt(0);

            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = Bounds;
        if (bounds.Width < 50 || bounds.Height < 50) return;

        // Background
        context.FillRectangle(_tokens.BgBrush, new Rect(0, 0, bounds.Width, bounds.Height));

        double leftPanelW = bounds.Width * 0.44;
        double rightPanelX = leftPanelW + 20;
        double rightPanelW = bounds.Width - rightPanelX - 20;

        // 1. Draw Control Space (a, b) and Cusp Bifurcation Neil Parabola
        DrawControlSpace(context, new Rect(20, 40, leftPanelW, bounds.Height - 60));

        // 2. Draw Dynamic Potential Well V(x) (Top Right)
        double potH = (bounds.Height - 90) * 0.48;
        DrawPotentialWell(context, new Rect(rightPanelX, 40, rightPanelW, potH));

        // 3. Draw Pleated Equilibrium Manifold x³ + ax + b = 0 (Bottom Right)
        double surfaceY = 40 + potH + 16;
        double surfaceH = bounds.Height - surfaceY - 20;
        DrawEquilibriumSurface(context, new Rect(rightPanelX, surfaceY, rightPanelW, surfaceH));

        // 4. Draw Header Banner
        DrawHeaderBanner(context, bounds);
    }

    private void DrawHeaderBanner(DrawingContext context, Rect bounds)
    {
        double disc = _engine.ComputeCuspDiscriminant(ParamA, ParamB);
        string regime = disc < -1e-5 ? "BISTABLE (3 EQUILIBRIA, DUAL POTENTIAL WELLS)" : disc > 1e-5 ? "MONOSTABLE (SINGLE GLOBAL MINIMUM)" : "BIFURCATION BOUNDARY (FOLD SINGULARITY)";
        var regimeColor = disc < -1e-5 ? _tokens.AccentRose : disc > 1e-5 ? _tokens.AccentCyan : _tokens.AccentAmber;

        string header = $"René Thom's Cusp Catastrophe (A₃) | a = {ParamA:F2}, b = {ParamB:F2} | State x = {StateX:F3} | {regime}";
        var ft = new FormattedText(
            header,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            _fontBold,
            12,
            new SolidColorBrush(regimeColor)
        );

        var bgRect = new Rect(20, 10, bounds.Width - 40, 24);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(50, 15, 23, 42)), bgRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)), 1), bgRect);
        context.DrawText(ft, new Point(30, 14));
    }

    private void DrawControlSpace(DrawingContext context, Rect rect)
    {
        // Panel Background
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)), rect);
        context.DrawRectangle(new Pen(_tokens.BorderBrush, 1), rect);

        // Grid & Coordinates: a in [-4, 1.5], b in [-3, 3]
        double aMin = -4.0, aMax = 1.5;
        double bMin = -3.0, bMax = 3.0;

        Point ToScreen(double a, double b)
        {
            double sx = rect.X + ((a - aMin) / (aMax - aMin)) * rect.Width;
            double sy = rect.Y + ((bMax - b) / (bMax - bMin)) * rect.Height;
            return new Point(sx, sy);
        }

        // Axes
        var origin = ToScreen(0, 0);
        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 148, 163, 184)), 1);
        // Vertical axis (a = 0)
        context.DrawLine(axisPen, new Point(origin.X, rect.Y), new Point(origin.X, rect.Bottom));
        // Horizontal axis (b = 0)
        context.DrawLine(axisPen, new Point(rect.X, origin.Y), new Point(rect.Right, origin.Y));

        // Axis labels
        var labelA = new FormattedText("Control Parameter a (Splitting)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontRegular, 10, _tokens.TextMutedBrush);
        context.DrawText(labelA, new Point(rect.X + 12, origin.Y + 4));

        var labelB = new FormattedText("Control Parameter b (Normal)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontRegular, 10, _tokens.TextMutedBrush);
        context.DrawText(labelB, new Point(rect.X + 12, rect.Y + 8));

        // Draw Bifurcation Set: Neil's Semicubical Parabola 27b² + 4a³ = 0 => b = ± sqrt(-4a³/27)
        if (ShowBifurcationSet)
        {
            var upperPoints = new List<Point>();
            var lowerPoints = new List<Point>();
            int steps = 120;
            for (int i = 0; i <= steps; i++)
            {
                double a = aMin + (0.0 - aMin) * (i / (double)steps);
                double bMag = Math.Sqrt(-4.0 * Math.Pow(a, 3) / 27.0);
                upperPoints.Add(ToScreen(a, bMag));
                lowerPoints.Add(ToScreen(a, -bMag));
            }

            // Fill bistable cusp region
            var streamGeom = new StreamGeometry();
            using (var ctx = streamGeom.Open())
            {
                ctx.BeginFigure(upperPoints[0], true);
                for (int i = 1; i < upperPoints.Count; i++) ctx.LineTo(upperPoints[i]);
                for (int i = lowerPoints.Count - 1; i >= 0; i--) ctx.LineTo(lowerPoints[i]);
                ctx.EndFigure(true);
            }
            context.DrawGeometry(new SolidColorBrush(Color.FromArgb(40, 244, 63, 94)), null, streamGeom);

            // Draw bifurcation boundary curves
            var cuspPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 244, 63, 94)), 2);
            for (int i = 0; i < upperPoints.Count - 1; i++)
            {
                context.DrawLine(cuspPen, upperPoints[i], upperPoints[i + 1]);
                context.DrawLine(cuspPen, lowerPoints[i], lowerPoints[i + 1]);
            }

            // Label Cusp Point (0, 0)
            var cuspPtText = new FormattedText("Cusp Point (0,0)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontBold, 10, new SolidColorBrush(_tokens.AccentRose));
            context.DrawText(cuspPtText, new Point(origin.X + 8, origin.Y - 16));

            // Bistable region text
            var biText = new FormattedText("Bistable Region (4a³+27b² < 0)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontBold, 10, new SolidColorBrush(Color.FromArgb(180, 244, 63, 94)));
            var biPos = ToScreen(-2.5, 0.0);
            context.DrawText(biText, new Point(biPos.X - 60, biPos.Y - 24));
        }

        // Draw Hysteresis Path Trail
        if (_hysteresisPath.Count > 1)
        {
            var trailPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 245, 158, 11)), 1.5, lineCap: PenLineCap.Round);
            for (int i = 0; i < _hysteresisPath.Count - 1; i++)
            {
                var p1 = ToScreen(_hysteresisPath[i].X, _hysteresisPath[i].Y);
                var p2 = ToScreen(_hysteresisPath[i + 1].X, _hysteresisPath[i + 1].Y);
                context.DrawLine(trailPen, p1, p2);
            }
        }

        // Current Control Handle (ParamA, ParamB)
        var curScreen = ToScreen(ParamA, ParamB);
        // Outer halo
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)), null, curScreen, 12, 12);
        // Inner core
        context.DrawEllipse(new SolidColorBrush(_tokens.AccentCyan), new Pen(new SolidColorBrush(Colors.White), 2), curScreen, 6, 6);

        // Coordinate text
        var coordText = new FormattedText($"(a={ParamA:F2}, b={ParamB:F2})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontCode, 10, new SolidColorBrush(Colors.White));
        context.DrawText(coordText, new Point(curScreen.X + 10, curScreen.Y - 8));
    }

    private void DrawPotentialWell(DrawingContext context, Rect rect)
    {
        // Panel Background
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)), rect);
        context.DrawRectangle(new Pen(_tokens.BorderBrush, 1), rect);

        // Title
        var titleText = new FormattedText("POTENTIAL LANDSCAPE V(x) = ¼x⁴ + ½ax² + bx", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontBold, 11, _tokens.AccentCyanBrush);
        context.DrawText(titleText, new Point(rect.X + 12, rect.Y + 8));

        // Compute range for x in [-2.5, 2.5]
        double xMin = -2.5, xMax = 2.5;
        double a = ParamA;
        double b = ParamB;

        // Sample V(x) to determine Vmin and Vmax
        double vMin = double.MaxValue;
        double vMax = double.MinValue;
        int samples = 100;
        var curvePts = new List<Point>();

        for (int i = 0; i <= samples; i++)
        {
            double x = xMin + (xMax - xMin) * (i / (double)samples);
            double v = _engine.ComputeCuspPotential(x, a, b);
            if (v < vMin) vMin = v;
            if (v > vMax) vMax = v;
            curvePts.Add(new Point(x, v));
        }

        vMin = Math.Min(vMin, -2.0);
        vMax = Math.Max(vMax, 2.0);
        double vPad = (vMax - vMin) * 0.15;
        vMin -= vPad;
        vMax += vPad;

        Point ToScreen(double x, double v)
        {
            double sx = rect.X + 20 + ((x - xMin) / (xMax - xMin)) * (rect.Width - 40);
            double sy = rect.Bottom - 16 - ((v - vMin) / (vMax - vMin)) * (rect.Height - 45);
            return new Point(sx, sy);
        }

        // Zero potential dashed line
        if (vMin <= 0 && vMax >= 0)
        {
            var zeroY = ToScreen(0, 0).Y;
            context.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(60, 148, 163, 184)), 1, DashStyle.Dash), new Point(rect.X + 20, zeroY), new Point(rect.Right - 20, zeroY));
        }

        // Draw potential curve V(x)
        var potPen = new Pen(new SolidColorBrush(Color.FromArgb(240, 56, 189, 248)), 2.5);
        for (int i = 0; i < curvePts.Count - 1; i++)
        {
            var p1 = ToScreen(curvePts[i].X, curvePts[i].Y);
            var p2 = ToScreen(curvePts[i + 1].X, curvePts[i + 1].Y);
            context.DrawLine(potPen, p1, p2);
        }

        // Draw equilibria (minima in emerald, maxima in amber)
        var equilibria = _engine.SolveCuspEquilibria(a, b);
        foreach (var eq in equilibria)
        {
            var p = ToScreen(eq.X, eq.Potential);
            if (eq.IsStable)
            {
                // Stable Minimum (Green)
                context.DrawEllipse(new SolidColorBrush(_tokens.AccentEmerald), null, p, 5, 5);
                var eqText = new FormattedText($"Min ({eq.X:F2})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontRegular, 9, _tokens.AccentEmeraldBrush);
                context.DrawText(eqText, new Point(p.X - 16, p.Y + 6));
            }
            else
            {
                // Unstable Maximum Barrier (Amber)
                context.DrawEllipse(new SolidColorBrush(_tokens.AccentAmber), null, p, 4, 4);
                var eqText = new FormattedText($"Barrier ({eq.X:F2})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontRegular, 9, _tokens.AccentAmberBrush);
                context.DrawText(eqText, new Point(p.X - 20, p.Y - 14));
            }
        }

        // Draw dynamic ball at StateX
        double currentV = _engine.ComputeCuspPotential(StateX, a, b);
        var ballPos = ToScreen(StateX, currentV);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 245, 158, 11)), null, ballPos, 10, 10);
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(251, 191, 36)), new Pen(new SolidColorBrush(Colors.White), 2), ballPos, 5, 5);

        // State label
        var stateText = new FormattedText($"State x = {StateX:F3}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontBold, 10, new SolidColorBrush(Color.FromRgb(251, 191, 36)));
        context.DrawText(stateText, new Point(ballPos.X + 10, ballPos.Y - 6));
    }

    private void DrawEquilibriumSurface(DrawingContext context, Rect rect)
    {
        // Panel Background
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)), rect);
        context.DrawRectangle(new Pen(_tokens.BorderBrush, 1), rect);

        using (context.PushClip(rect))
        {
            // Title
            var titleText = new FormattedText("PLEATED EQUILIBRIUM MANIFOLD x³ + ax + b = 0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontBold, 11, _tokens.AccentPurpleBrush);
            context.DrawText(titleText, new Point(rect.X + 12, rect.Y + 8));

        // We render an isometric projection of the pleated surface (a, b, x)
        // a in [-3.5, 0.5], b in [-2.5, 2.5]
        double centerX = rect.X + rect.Width * 0.45;
        double centerY = rect.Y + rect.Height * 0.55;

        // Isometric projection vectors
        // a-axis: [-0.85, 0.35]
        // b-axis: [0.85, 0.35]
        // x-axis: [0.0, -0.90]
        double scaleA = rect.Width * 0.16;
        double scaleB = rect.Width * 0.16;
        double scaleX = rect.Height * 0.22;

        Point Project3D(double a, double b, double x)
        {
            double px = centerX + (b * 0.7 - a * 0.7) * scaleA;
            double py = centerY + (b * 0.3 + a * 0.3) * scaleB - x * scaleX;
            return new Point(px, py);
        }

        // Draw mesh grid of surface: vary a from -3.5 to 0.5, vary x from -2.0 to 2.0, compute b = -(x³ + ax)
        var meshPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 168, 85, 247)), 1);
        int aSteps = 10;
        int xSteps = 24;

        for (int i = 0; i <= aSteps; i++)
        {
            double aVal = -3.5 + 4.0 * (i / (double)aSteps);
            var prevPt = Project3D(aVal, -(Math.Pow(-2.0, 3) + aVal * (-2.0)), -2.0);
            for (int j = 1; j <= xSteps; j++)
            {
                double xVal = -2.0 + 4.0 * (j / (double)xSteps);
                double bVal = -(Math.Pow(xVal, 3) + aVal * xVal);
                var curPt = Project3D(aVal, bVal, xVal);
                if (curPt.X >= rect.X && curPt.X <= rect.Right && curPt.Y >= rect.Y && curPt.Y <= rect.Bottom)
                {
                    context.DrawLine(meshPen, prevPt, curPt);
                }
                prevPt = curPt;
            }
        }

        // Highlight the Fold Curves (where 3x² + a = 0 => x = ± sqrt(-a/3))
        var foldPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 244, 63, 94)), 2);
        for (int i = 0; i < 30; i++)
        {
            double a1 = -3.5 + 3.5 * (i / 30.0);
            double a2 = -3.5 + 3.5 * ((i + 1) / 30.0);

            double xFold1 = Math.Sqrt(-a1 / 3.0);
            double xFold2 = Math.Sqrt(-a2 / 3.0);
            double bFold1 = -(Math.Pow(xFold1, 3) + a1 * xFold1);
            double bFold2 = -(Math.Pow(xFold2, 3) + a2 * xFold2);

            // Upper fold
            context.DrawLine(foldPen, Project3D(a1, bFold1, xFold1), Project3D(a2, bFold2, xFold2));
            // Lower fold
            context.DrawLine(foldPen, Project3D(a1, -bFold1, -xFold1), Project3D(a2, -bFold2, -xFold2));
        }

        // Current Operating State Point (ParamA, ParamB, StateX) on the 3D sheet
        var state3D = Project3D(ParamA, ParamB, StateX);
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 56, 189, 248)), null, state3D, 10, 10);
        context.DrawEllipse(new SolidColorBrush(_tokens.AccentCyan), new Pen(new SolidColorBrush(Colors.White), 2), state3D, 5, 5);

            // Label current state
            var label3D = new FormattedText($"P({ParamA:F1}, {ParamB:F1}, {StateX:F2})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _fontBold, 10, _tokens.AccentCyanBrush);
            context.DrawText(label3D, new Point(state3D.X + 8, state3D.Y - 6));
        }
    }
}
