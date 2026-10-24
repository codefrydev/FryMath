using System;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Geometry;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class SpaceCurveCanvas : Control
{
    public static readonly StyledProperty<SpaceCurvePreset> PresetProperty =
        AvaloniaProperty.Register<SpaceCurveCanvas, SpaceCurvePreset>(nameof(Preset), SpaceCurvePreset.CircularHelix);

    public static readonly StyledProperty<double> ParamTProperty =
        AvaloniaProperty.Register<SpaceCurveCanvas, double>(nameof(ParamT), 2.5);

    public static readonly StyledProperty<bool> ShowFrenetFrameProperty =
        AvaloniaProperty.Register<SpaceCurveCanvas, bool>(nameof(ShowFrenetFrame), true);

    public static readonly StyledProperty<bool> ShowOsculatingPlaneProperty =
        AvaloniaProperty.Register<SpaceCurveCanvas, bool>(nameof(ShowOsculatingPlane), true);

    public static readonly StyledProperty<bool> ShowOsculatingCircleProperty =
        AvaloniaProperty.Register<SpaceCurveCanvas, bool>(nameof(ShowOsculatingCircle), true);

    public SpaceCurvePreset Preset
    {
        get => GetValue(PresetProperty);
        set => SetValue(PresetProperty, value);
    }

    public double ParamT
    {
        get => GetValue(ParamTProperty);
        set => SetValue(ParamTProperty, value);
    }

    public bool ShowFrenetFrame
    {
        get => GetValue(ShowFrenetFrameProperty);
        set => SetValue(ShowFrenetFrameProperty, value);
    }

    public bool ShowOsculatingPlane
    {
        get => GetValue(ShowOsculatingPlaneProperty);
        set => SetValue(ShowOsculatingPlaneProperty, value);
    }

    public bool ShowOsculatingCircle
    {
        get => GetValue(ShowOsculatingCircleProperty);
        set => SetValue(ShowOsculatingCircleProperty, value);
    }

    // 3D Camera Orbit State
    private double _yaw = 0.75;
    private double _pitch = 0.45;
    private double _zoom = 1.0;

    private bool _isOrbiting;
    private Point _lastOrbitPos;

    static SpaceCurveCanvas()
    {
        AffectsRender<SpaceCurveCanvas>(
            PresetProperty,
            ParamTProperty,
            ShowFrenetFrameProperty,
            ShowOsculatingPlaneProperty,
            ShowOsculatingCircleProperty);
    }

    public SpaceCurveCanvas()
    {
        ClipToBounds = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double factor = e.Delta.Y > 0 ? 1.15 : 0.85;
        _zoom = Math.Clamp(_zoom * factor, 0.25, 4.0);
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsLeftButtonPressed)
        {
            _isOrbiting = true;
            _lastOrbitPos = e.GetPosition(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isOrbiting)
        {
            var pos = e.GetPosition(this);
            var delta = pos - _lastOrbitPos;
            _lastOrbitPos = pos;

            _yaw += delta.X * 0.008;
            _pitch = Math.Clamp(_pitch + delta.Y * 0.008, -1.4, 1.4);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isOrbiting = false;
    }

    private (Point Pt, double Depth) Project3D(Vector3 v, double w, double h)
    {
        double cosY = Math.Cos(_yaw), sinY = Math.Sin(_yaw);
        double cosP = Math.Cos(_pitch), sinP = Math.Sin(_pitch);

        double x1 = v.X * cosY - v.Z * sinY;
        double z1 = v.X * sinY + v.Z * cosY;

        double y2 = v.Y * cosP - z1 * sinP;
        double z2 = v.Y * sinP + z1 * cosP;

        double depth = z2 + 10.0;
        if (depth < 0.5) depth = 0.5;

        double scale = (10.0 / depth) * _zoom * (Math.Min(w, h) / 7.5);

        return (new Point(w * 0.5 + x1 * scale, h * 0.5 - y2 * scale), depth);
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        var tokens = MathThemeTokens.DarkElectric;
        context.FillRectangle(tokens.SurfaceBrush, new Rect(0, 0, w, h));

        // 1. Draw 3D Coordinate Axes
        RenderAxes(context, w, h, tokens);

        // 2. Draw Space Curve Ribbon
        RenderCurve(context, w, h);

        // 3. Compute Frenet-Serret Frame at ParamT
        var frame = SpaceCurveEngine.ComputeFrenetFrame(Preset, ParamT);

        // 4. Draw Osculating Plane
        if (ShowOsculatingPlane)
        {
            RenderOsculatingPlane(context, frame, w, h);
        }

        // 5. Draw Osculating Circle of Curvature
        if (ShowOsculatingCircle && frame.RadiusOfCurvature < 8.0)
        {
            RenderOsculatingCircle(context, frame, w, h);
        }

        // 6. Draw Frenet Trihedron (T, N, B vectors)
        if (ShowFrenetFrame)
        {
            RenderFrenetFrame(context, frame, w, h);
        }

        // 7. Draw HUD Diagnostic Card
        RenderDiagnosticHud(context, frame, tokens);

        // 8. Footer tip
        RenderFooter(context, tokens, w, h);
    }

    private void RenderAxes(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        var origin = Project3D(Vector3.Zero, w, h).Pt;
        var pX = Project3D(new Vector3(3.0f, 0, 0), w, h).Pt;
        var pY = Project3D(new Vector3(0, 3.0f, 0), w, h).Pt;
        var pZ = Project3D(new Vector3(0, 0, 3.0f), w, h).Pt;

        var penX = new Pen(new SolidColorBrush(Color.FromArgb(140, 255, 68, 68)), 1.5);
        var penY = new Pen(new SolidColorBrush(Color.FromArgb(140, 0, 230, 118)), 1.5);
        var penZ = new Pen(new SolidColorBrush(Color.FromArgb(140, 0, 229, 255)), 1.5);

        context.DrawLine(penX, origin, pX);
        context.DrawLine(penY, origin, pY);
        context.DrawLine(penZ, origin, pZ);
    }

    private void RenderCurve(DrawingContext context, double w, double h)
    {
        var (tMin, tMax) = SpaceCurveEngine.GetParameterRange(Preset);
        int segments = 220;
        double dt = (tMax - tMin) / segments;

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            Vector3 startPt = SpaceCurveEngine.EvaluateCurve(Preset, tMin);
            ctx.BeginFigure(Project3D(startPt, w, h).Pt, false);

            for (int i = 1; i <= segments; i++)
            {
                double ti = tMin + i * dt;
                Vector3 pt = SpaceCurveEngine.EvaluateCurve(Preset, ti);
                ctx.LineTo(Project3D(pt, w, h).Pt);
            }
        }

        var curvePen = new Pen(new SolidColorBrush(Color.FromArgb(240, 0, 229, 255)), 2.5);
        context.DrawGeometry(null, curvePen, geo);
    }

    private void RenderOsculatingPlane(DrawingContext context, FrenetFrame frame, double w, double h)
    {
        // Spanned by Tangent T and Normal N with size ~ 1.8
        float sz = 1.4f;
        Vector3 c = frame.Position;
        Vector3 T = frame.Tangent * sz;
        Vector3 N = frame.Normal * sz;

        Vector3 p1 = c + T + N;
        Vector3 p2 = c - T + N;
        Vector3 p3 = c - T - N;
        Vector3 p4 = c + T - N;

        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(Project3D(p1, w, h).Pt, true);
            ctx.LineTo(Project3D(p2, w, h).Pt);
            ctx.LineTo(Project3D(p3, w, h).Pt);
            ctx.LineTo(Project3D(p4, w, h).Pt);
        }

        var planeBrush = new SolidColorBrush(Color.FromArgb(45, 255, 179, 0)); // Translucent amber
        var planePen = new Pen(new SolidColorBrush(Color.FromArgb(140, 255, 179, 0)), 1);
        context.DrawGeometry(planeBrush, planePen, geo);
    }

    private void RenderOsculatingCircle(DrawingContext context, FrenetFrame frame, double w, double h)
    {
        // Circle of radius R = 1 / kappa centered at C = r + R * N in the TN osculating plane
        float R = (float)frame.RadiusOfCurvature;
        Vector3 center = frame.Position + frame.Normal * R;

        int circleSegments = 40;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            // First point (angle = 0 -> along -N touching curve at r)
            Vector3 pt0 = center - frame.Normal * R;
            ctx.BeginFigure(Project3D(pt0, w, h).Pt, true);

            for (int i = 1; i < circleSegments; i++)
            {
                double theta = (i / (double)circleSegments) * (2.0 * Math.PI);
                // Along TN plane: center + R (cos theta * -N + sin theta * T)
                Vector3 pt = center - frame.Normal * (float)(R * Math.Cos(theta)) + frame.Tangent * (float)(R * Math.Sin(theta));
                ctx.LineTo(Project3D(pt, w, h).Pt);
            }
        }

        var circlePen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 215, 64)), 1.5);
        context.DrawGeometry(null, circlePen, geo);
    }

    private void RenderFrenetFrame(DrawingContext context, FrenetFrame frame, double w, double h)
    {
        var originProj = Project3D(frame.Position, w, h).Pt;

        float len = 1.6f;
        var pT = Project3D(frame.Position + frame.Tangent * len, w, h).Pt;
        var pN = Project3D(frame.Position + frame.Normal * len, w, h).Pt;
        var pB = Project3D(frame.Position + frame.Binormal * len, w, h).Pt;

        // Tangent T (Red / Coral)
        var penT = new Pen(new SolidColorBrush(Color.FromRgb(255, 68, 68)), 2.5);
        context.DrawLine(penT, originProj, pT);

        // Normal N (Emerald Green)
        var penN = new Pen(new SolidColorBrush(Color.FromRgb(0, 230, 118)), 2.5);
        context.DrawLine(penN, originProj, pN);

        // Binormal B (Electric Cyan / Royal Blue)
        var penB = new Pen(new SolidColorBrush(Color.FromRgb(0, 229, 255)), 2.5);
        context.DrawLine(penB, originProj, pB);

        // Draw particle dot at origin
        var dotBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        context.DrawEllipse(dotBrush, null, originProj, 4, 4);
    }

    private void RenderDiagnosticHud(DrawingContext context, FrenetFrame frame, MathThemeTokens tokens)
    {
        double hudX = 20;
        double hudY = 20;
        double hudW = 320;
        double hudH = 140;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(220, 13, 16, 28)),
            new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 229, 255)), 1),
            hudRect, 8, 8);

        string titleStr = Preset switch
        {
            SpaceCurvePreset.CircularHelix => "Circular Helix: r(t) = <a cos t, a sin t, b t>",
            SpaceCurvePreset.TrefoilKnot => "Trefoil Knot: Non-Trivial Knot (Topology)",
            SpaceCurvePreset.VivianiCurve => "Viviani's Curve: Sphere ∩ Cylinder",
            SpaceCurvePreset.TorusKnot => "Torus Knot (p=2, q=3): Winding On Torus",
            SpaceCurvePreset.TwistedCubic => "Twisted Cubic: Rational Algebraic Curve",
            SpaceCurvePreset.ConicalSpiral => "Conical Spiral: Linear Radius Expansion",
            _ => "Space Curve"
        };

        var textTitle = new FormattedText(titleStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif", FontStyle.Normal, FontWeight.SemiBold), 11,
            new SolidColorBrush(Color.FromRgb(0, 229, 255)));
        context.DrawText(textTitle, new Point(hudX + 12, hudY + 10));

        var textR = new FormattedText(
            $"r(t) = <{frame.Position.X:F2}, {frame.Position.Y:F2}, {frame.Position.Z:F2}>  (t = {ParamT:F2}, s = {frame.ArcLength:F2})",
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 11,
            new SolidColorBrush(Color.FromRgb(220, 220, 230)));
        context.DrawText(textR, new Point(hudX + 12, hudY + 32));

        var textKappa = new FormattedText(
            $"Curvature: κ = {frame.Curvature:F3}  (Radius R = 1/κ = {frame.RadiusOfCurvature:F2})",
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif", FontStyle.Normal, FontWeight.Bold), 11,
            new SolidColorBrush(Color.FromRgb(255, 179, 0)));
        context.DrawText(textKappa, new Point(hudX + 12, hudY + 54));

        var textTau = new FormattedText(
            $"Torsion: τ = {frame.Torsion:F3}  (Twisting out of osculating plane)",
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 11,
            new SolidColorBrush(Color.FromRgb(255, 68, 122)));
        context.DrawText(textTau, new Point(hudX + 12, hudY + 76));

        var textSpeed = new FormattedText(
            $"Speed: v = ||r'(t)|| = {frame.Speed:F3}  |  TNB Frame: ⟨T,N,B⟩ Orthonormal",
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 10,
            new SolidColorBrush(Color.FromArgb(180, 160, 170, 200)));
        context.DrawText(textSpeed, new Point(hudX + 12, hudY + 98));

        // Legend: Red T, Green N, Blue B
        var textLegend = new FormattedText(
            "■ Tangent T (Red)  ■ Normal N (Green)  ■ Binormal B (Cyan)",
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif", FontStyle.Normal, FontWeight.SemiBold), 10,
            new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)));
        context.DrawText(textLegend, new Point(hudX + 12, hudY + 118));
    }

    private void RenderFooter(DrawingContext context, MathThemeTokens tokens, double w, double h)
    {
        var text = new FormattedText(
            "Drag mouse to orbit 3D view • Scroll to zoom • Moving Frenet trihedron & osculating plane glide along space curve",
            CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, sans-serif"), 10,
            new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)));

        context.DrawText(text, new Point(20, h - 30));
    }
}
