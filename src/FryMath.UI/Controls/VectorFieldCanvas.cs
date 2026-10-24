using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Dynamics;
using FryMath.Core.LinearAlgebra;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class VectorFieldCanvas : Control
{
    public static readonly StyledProperty<string> ExprPProperty =
        AvaloniaProperty.Register<VectorFieldCanvas, string>(nameof(ExprP), "-y");

    public static readonly StyledProperty<string> ExprQProperty =
        AvaloniaProperty.Register<VectorFieldCanvas, string>(nameof(ExprQ), "x");

    public static readonly StyledProperty<bool> ShowArrowsProperty =
        AvaloniaProperty.Register<VectorFieldCanvas, bool>(nameof(ShowArrows), true);

    public static readonly StyledProperty<bool> ShowParticlesProperty =
        AvaloniaProperty.Register<VectorFieldCanvas, bool>(nameof(ShowParticles), true);

    public static readonly StyledProperty<double> ParticleSpeedProperty =
        AvaloniaProperty.Register<VectorFieldCanvas, double>(nameof(ParticleSpeed), 1.0);

    public string ExprP
    {
        get => GetValue(ExprPProperty);
        set => SetValue(ExprPProperty, value);
    }

    public string ExprQ
    {
        get => GetValue(ExprQProperty);
        set => SetValue(ExprQProperty, value);
    }

    public bool ShowArrows
    {
        get => GetValue(ShowArrowsProperty);
        set => SetValue(ShowArrowsProperty, value);
    }

    public bool ShowParticles
    {
        get => GetValue(ShowParticlesProperty);
        set => SetValue(ShowParticlesProperty, value);
    }

    public double ParticleSpeed
    {
        get => GetValue(ParticleSpeedProperty);
        set => SetValue(ParticleSpeedProperty, value);
    }

    public double ScalePixelsPerUnit { get; set; } = 48.0;
    public double OriginX { get; set; } = 0.0;
    public double OriginY { get; set; } = 0.0;

    private readonly VectorField2D _field = new("-y", "x");
    private readonly ParticleAdvectionEngine _particles = new(320);
    private readonly DispatcherTimer _timer;

    private string _compiledP = "-y";
    private string _compiledQ = "x";

    private Point? _hoverPos;
    private bool _isPanning;
    private Point _panStart;
    private double _panStartOx;
    private double _panStartOy;

    private Vector2D? _userSeedPoint;
    private List<Vector2D>? _userTrajectory;

    static VectorFieldCanvas()
    {
        AffectsRender<VectorFieldCanvas>(
            ExprPProperty,
            ExprQProperty,
            ShowArrowsProperty,
            ShowParticlesProperty,
            ParticleSpeedProperty);
    }

    public VectorFieldCanvas()
    {
        ClipToBounds = true;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            EnsureCompiled();

            if (ShowParticles)
            {
                double w = Bounds.Width;
                double h = Bounds.Height;
                if (w > 40 && h > 40)
                {
                    var topLeft = ScreenToMath(new Point(0, 0));
                    var botRight = ScreenToMath(new Point(w, h));
                    _particles.Step(_field, 0.016, topLeft.X, botRight.X, botRight.Y, topLeft.Y, ParticleSpeed);
                }
            }

            InvalidateVisual();
        };
        _timer.Start();

        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    private void EnsureCompiled()
    {
        if (_compiledP != ExprP || _compiledQ != ExprQ)
        {
            _field.Compile(ExprP, ExprQ);
            _compiledP = ExprP;
            _compiledQ = ExprQ;

            if (_userSeedPoint.HasValue)
            {
                _userTrajectory = _field.TraceTrajectory(_userSeedPoint.Value, 0.02, 350);
            }
        }
    }

    public void SetPreset(string exprP, string exprQ)
    {
        ExprP = exprP;
        ExprQ = exprQ;
        _userSeedPoint = null;
        _userTrajectory = null;
        EnsureCompiled();

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w > 40 && h > 40)
        {
            var topLeft = ScreenToMath(new Point(0, 0));
            var botRight = ScreenToMath(new Point(w, h));
            _particles.Reset(topLeft.X, botRight.X, botRight.Y, topLeft.Y);
        }
        InvalidateVisual();
    }

    private Point MathToScreen(double x, double y)
    {
        double ox = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        double oy = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
        return new Point(ox + x * ScalePixelsPerUnit, oy - y * ScalePixelsPerUnit);
    }

    private Vector2D ScreenToMath(Point pt)
    {
        double ox = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        double oy = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
        return new Vector2D((pt.X - ox) / ScalePixelsPerUnit, (oy - pt.Y) / ScalePixelsPerUnit);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        if (props.IsLeftButtonPressed)
        {
            // Drop interactive streamline seed
            var mathPt = ScreenToMath(pos);
            _userSeedPoint = mathPt;
            _userTrajectory = _field.TraceTrajectory(mathPt, 0.02, 350);
            InvalidateVisual();
        }
        else if (props.IsRightButtonPressed)
        {
            // Start pan
            _isPanning = true;
            _panStart = pos;
            _panStartOx = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
            _panStartOy = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);
        _hoverPos = pos;

        if (_isPanning)
        {
            OriginX = _panStartOx + (pos.X - _panStart.X);
            OriginY = _panStartOy + (pos.Y - _panStart.Y);
            InvalidateVisual();
            return;
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hoverPos = null;
        _isPanning = false;
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pos = e.GetPosition(this);
        var mathBefore = ScreenToMath(pos);

        double factor = e.Delta.Y > 0 ? 1.15 : 1.0 / 1.15;
        ScalePixelsPerUnit = Math.Clamp(ScalePixelsPerUnit * factor, 12.0, 300.0);

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

        EnsureCompiled();
        var tokens = MathThemeService.Instance.Tokens;

        // 1. Background
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        // 2. Coordinate Grid
        RenderGridAndAxes(context, w, h, tokens);

        // 3. Vector Field Arrows
        if (ShowArrows)
        {
            RenderVectorArrows(context, w, h, tokens);
        }

        // 4. Fluid Particle Advection Streaks
        if (ShowParticles)
        {
            RenderParticles(context, tokens);
        }

        // 5. User Interactive Click Trajectory (Streamline)
        if (_userTrajectory != null && _userTrajectory.Count > 1)
        {
            RenderUserTrajectory(context, tokens);
        }

        // 6. Hover Probe & Vector Calculus HUD
        if (_hoverPos.HasValue)
        {
            RenderProbeAndHud(context, _hoverPos.Value, tokens);
        }
    }

    private void RenderGridAndAxes(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        double ox = OriginX;
        double oy = OriginY;

        double targetStep = 55.0;
        double mathStep = Math.Pow(10, Math.Floor(Math.Log10(targetStep / ScalePixelsPerUnit)));
        if (mathStep * ScalePixelsPerUnit < targetStep * 0.5) mathStep *= 2.0;
        if (mathStep * ScalePixelsPerUnit < targetStep * 0.75) mathStep *= 2.5;

        double pixelStep = mathStep * ScalePixelsPerUnit;

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

        // Primary axes
        if (ox >= 0 && ox <= w)
        {
            context.DrawLine(tokens.GridAxisPen, new Point(ox, 0), new Point(ox, h));
        }
        if (oy >= 0 && oy <= h)
        {
            context.DrawLine(tokens.GridAxisPen, new Point(0, oy), new Point(w, oy));
        }
    }

    private void RenderVectorArrows(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        double spacing = 38.0; // pixel spacing between arrows
        double maxArrowLen = spacing * 0.75;

        for (double py = spacing * 0.5; py < h; py += spacing)
        {
            for (double px = spacing * 0.5; px < w; px += spacing)
            {
                var math = ScreenToMath(new Point(px, py));
                var v = _field.Evaluate(math.X, math.Y);
                double speed = v.Length;
                if (speed < 1e-6) continue;

                // Normalize direction and scale length logarithmically
                double angle = Math.Atan2(-v.Y, v.X);
                double normLen = Math.Min(maxArrowLen, 5.0 + Math.Log(1.0 + speed) * 7.0);

                double endX = px + Math.Cos(angle) * normLen;
                double endY = py + Math.Sin(angle) * normLen;

                // Color based on velocity magnitude
                var arrowColor = GetSpeedColor(speed);
                var arrowPen = new Pen(new SolidColorBrush(arrowColor), 1.2);

                context.DrawLine(arrowPen, new Point(px, py), new Point(endX, endY));

                // Arrow head
                double headAngle1 = angle + Math.PI * 0.82;
                double headAngle2 = angle - Math.PI * 0.82;
                double headLen = Math.Min(5.5, normLen * 0.35);

                context.DrawLine(arrowPen, new Point(endX, endY), new Point(endX + Math.Cos(headAngle1) * headLen, endY + Math.Sin(headAngle1) * headLen));
                context.DrawLine(arrowPen, new Point(endX, endY), new Point(endX + Math.Cos(headAngle2) * headLen, endY + Math.Sin(headAngle2) * headLen));
            }
        }
    }

    private static Color GetSpeedColor(double speed)
    {
        if (speed < 0.8) return Color.FromArgb(130, 56, 189, 248);  // Cyan faint
        if (speed < 2.5) return Color.FromArgb(210, 34, 211, 238);  // Cyan bright
        if (speed < 5.0) return Color.FromArgb(230, 245, 158, 11);  // Amber
        return Color.FromArgb(240, 244, 63, 94);                    // Rose
    }

    private void RenderParticles(DrawingContext context, MathThemeTokens tokens)
    {
        var particleList = _particles.Particles;
        var dotBrush = tokens.AccentCyanBrush;

        for (int i = 0; i < particleList.Count; i++)
        {
            var p = particleList[i];
            double alpha = Math.Sin(Math.PI * Math.Clamp(p.Age / p.MaxAge, 0.0, 1.0));
            if (alpha < 0.05) continue;

            var currScreen = MathToScreen(p.X, p.Y);
            var prevScreen = MathToScreen(p.PrevX, p.PrevY);

            byte a = (byte)(alpha * 220);
            var streakPen = new Pen(new SolidColorBrush(Color.FromArgb(a, 34, 211, 238)), 1.4);
            context.DrawLine(streakPen, prevScreen, currScreen);

            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(a, 168, 85, 247)), null, currScreen, 1.8, 1.8);
        }
    }

    private void RenderUserTrajectory(DrawingContext context, MathThemeTokens tokens)
    {
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            var p0 = MathToScreen(_userTrajectory![0].X, _userTrajectory[0].Y);
            ctx.BeginFigure(p0, false);

            for (int i = 1; i < _userTrajectory.Count; i++)
            {
                var pt = MathToScreen(_userTrajectory[i].X, _userTrajectory[i].Y);
                ctx.LineTo(pt);
            }
        }

        var trailPen = new Pen(tokens.AccentRoseBrush, 2.2);
        context.DrawGeometry(null, trailPen, geom);

        // Seed handle
        if (_userSeedPoint.HasValue)
        {
            var seedScreen = MathToScreen(_userSeedPoint.Value.X, _userSeedPoint.Value.Y);
            context.DrawEllipse(tokens.AccentRoseBrush, null, seedScreen, 5.0, 5.0);
            context.DrawEllipse(null, new Pen(Brushes.White, 1.8), seedScreen, 7.5, 7.5);
        }
    }

    private void RenderProbeAndHud(DrawingContext context, Point hoverScreen, MathThemeTokens tokens)
    {
        var mathPos = ScreenToMath(hoverScreen);
        var v = _field.Evaluate(mathPos.X, mathPos.Y);
        double speed = v.Length;
        double div = _field.ComputeDivergence(mathPos.X, mathPos.Y);
        double curl = _field.ComputeCurl(mathPos.X, mathPos.Y);

        // Crosshair & Target Dot
        var dotPen = new Pen(tokens.AccentCyanBrush, 1.2, dashStyle: DashStyle.Dash);
        context.DrawLine(dotPen, new Point(hoverScreen.X - 10, hoverScreen.Y), new Point(hoverScreen.X + 10, hoverScreen.Y));
        context.DrawLine(dotPen, new Point(hoverScreen.X, hoverScreen.Y - 10), new Point(hoverScreen.X, hoverScreen.Y + 10));
        context.DrawEllipse(null, new Pen(tokens.AccentCyanBrush, 1.5), hoverScreen, 6.0, 6.0);

        // HUD Readout
        string divType = div > 0.05 ? "Source (+, Expanding)" : div < -0.05 ? "Sink (-, Contracting)" : "Incompressible (0)";
        string curlType = curl > 0.05 ? "Counter-Clockwise (↺)" : curl < -0.05 ? "Clockwise (↻)" : "Irrotational (0)";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var hudText = new FormattedText(
            $"Vector Field: F(x, y) = < {ExprP},  {ExprQ} >\n" +
            $"Probe Position: ({mathPos.X:F2}, {mathPos.Y:F2})  |  Velocity: < {v.X:F2}, {v.Y:F2} >  |  Speed: {speed:F2}\n" +
            $"Divergence ∇·F = {div:F3}  [{divType}]\n" +
            $"Vorticity ∇×F = {curl:F3}  [{curlType}]\n" +
            $"Left Click: Drop RK4 Streamline Orbit  |  Right Drag: Pan Canvas  |  Scroll: Zoom",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            11,
            tokens.TextPrimaryBrush);

        var bgRect = new Rect(14.0, 14.0, hudText.Width + 24.0, hudText.Height + 16.0);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(200, 15, 17, 26)), bgRect, 8.0f);
        context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.0), bgRect, 8.0f);
        context.DrawText(hudText, new Point(26.0, 22.0));
    }
}
