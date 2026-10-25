using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Calculus;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class Surface3DCanvas : Control
{
    public static readonly StyledProperty<SurfacePreset> PresetProperty =
        AvaloniaProperty.Register<Surface3DCanvas, SurfacePreset>(nameof(Preset), SurfacePreset.Paraboloid);

    public static readonly StyledProperty<string> CustomFormulaProperty =
        AvaloniaProperty.Register<Surface3DCanvas, string>(nameof(CustomFormula), "0.5 * (x^2 + y^2)");

    public static readonly StyledProperty<double> ProbeXProperty =
        AvaloniaProperty.Register<Surface3DCanvas, double>(nameof(ProbeX), 1.0);

    public static readonly StyledProperty<double> ProbeYProperty =
        AvaloniaProperty.Register<Surface3DCanvas, double>(nameof(ProbeY), 0.8);

    public static readonly StyledProperty<bool> ShowTangentPlaneProperty =
        AvaloniaProperty.Register<Surface3DCanvas, bool>(nameof(ShowTangentPlane), true);

    public static readonly StyledProperty<bool> ShowWireframeProperty =
        AvaloniaProperty.Register<Surface3DCanvas, bool>(nameof(ShowWireframe), true);

    public static readonly StyledProperty<bool> ShowNormalVectorProperty =
        AvaloniaProperty.Register<Surface3DCanvas, bool>(nameof(ShowNormalVector), true);

    public SurfacePreset Preset
    {
        get => GetValue(PresetProperty);
        set => SetValue(PresetProperty, value);
    }

    public string CustomFormula
    {
        get => GetValue(CustomFormulaProperty);
        set => SetValue(CustomFormulaProperty, value);
    }

    public double ProbeX
    {
        get => GetValue(ProbeXProperty);
        set => SetValue(ProbeXProperty, value);
    }

    public double ProbeY
    {
        get => GetValue(ProbeYProperty);
        set => SetValue(ProbeYProperty, value);
    }

    public bool ShowTangentPlane
    {
        get => GetValue(ShowTangentPlaneProperty);
        set => SetValue(ShowTangentPlaneProperty, value);
    }

    public bool ShowWireframe
    {
        get => GetValue(ShowWireframeProperty);
        set => SetValue(ShowWireframeProperty, value);
    }

    public bool ShowNormalVector
    {
        get => GetValue(ShowNormalVectorProperty);
        set => SetValue(ShowNormalVectorProperty, value);
    }

    private readonly Surface3D _surface = new(SurfacePreset.Paraboloid);

    // Orbit Camera State
    private double _yaw = 0.75;
    private double _pitch = 0.55;
    private double _zoomScale = 1.0;
    private bool _isOrbiting;
    private Point _lastOrbitPos;

    static Surface3DCanvas()
    {
        AffectsRender<Surface3DCanvas>(
            PresetProperty,
            CustomFormulaProperty,
            ProbeXProperty,
            ProbeYProperty,
            ShowTangentPlaneProperty,
            ShowWireframeProperty,
            ShowNormalVectorProperty);
    }

    public Surface3DCanvas()
    {
        ClipToBounds = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PresetProperty)
        {
            _surface.SetPreset(Preset);
            InvalidateVisual();
        }
        else if (change.Property == CustomFormulaProperty)
        {
            _surface.CompileCustomFormula(CustomFormula);
            InvalidateVisual();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        double factor = e.Delta.Y > 0 ? 1.12 : 0.88;
        _zoomScale = Math.Clamp(_zoomScale * factor, 0.4, 3.5);
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
            double dx = (pos.X - _lastOrbitPos.X) * 0.008;
            double dy = (pos.Y - _lastOrbitPos.Y) * 0.008;
            _lastOrbitPos = pos;

            _yaw += dx;
            _pitch = Math.Clamp(_pitch + dy, -Math.PI * 0.45, Math.PI * 0.45);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isOrbiting = false;
    }

    private SurfacePoint3D ProjectPoint(double x, double y, double z, double ox, double oy, double scale)
    {
        double cosY = Math.Cos(_yaw);
        double sinY = Math.Sin(_yaw);
        double cosP = Math.Cos(_pitch);
        double sinP = Math.Sin(_pitch);

        double rx = x * cosY - y * sinY;
        double ry = x * sinY + y * cosY;

        double zc = z;
        double yScreen = zc * cosP - ry * sinP;
        double depth = ry * cosP + zc * sinP;

        double sx = ox + rx * scale;
        double sy = oy - yScreen * scale;

        return new SurfacePoint3D(x, y, z, depth, sx, sy);
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var tokens = MathThemeTokens.DarkElectric;
        context.FillRectangle(tokens.SurfaceBrush, new Rect(0, 0, w, h));

        double ox = w * 0.5;
        double oy = h * 0.55;
        double baseScale = Math.Min(w, h) * 0.12 * _zoomScale;

        // 1. Draw Coordinate Axes in 3D
        RenderAxes3D(context, ox, oy, baseScale, tokens);

        // 2. Generate and Render Surface Mesh Quads
        RenderSurfaceMesh(context, ox, oy, baseScale, tokens);

        // 3. Render Tangent Plane & Normal Vector
        if (ShowTangentPlane)
        {
            RenderTangentPlane(context, ox, oy, baseScale, tokens);
        }

        // 4. Render HUD Diagnostics
        RenderHUD(context, w, h, tokens);
    }

    private void RenderAxes3D(DrawingContext context, double ox, double oy, double scale, MathThemeTokens tokens)
    {
        double axisLen = 3.5;
        var o = ProjectPoint(0, 0, 0, ox, oy, scale);
        var px = ProjectPoint(axisLen, 0, 0, ox, oy, scale);
        var py = ProjectPoint(0, axisLen, 0, ox, oy, scale);
        var pz = ProjectPoint(0, 0, axisLen, ox, oy, scale);

        var penX = new Pen(tokens.AccentRoseBrush, 1.4);
        var penY = new Pen(tokens.AccentEmeraldBrush, 1.4);
        var penZ = new Pen(tokens.AccentCyanBrush, 1.4);

        context.DrawLine(penX, new Point(o.ScreenX, o.ScreenY), new Point(px.ScreenX, px.ScreenY));
        context.DrawLine(penY, new Point(o.ScreenX, o.ScreenY), new Point(py.ScreenX, py.ScreenY));
        context.DrawLine(penZ, new Point(o.ScreenX, o.ScreenY), new Point(pz.ScreenX, pz.ScreenY));

        var tf = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        context.DrawText(new FormattedText("X", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 10, tokens.AccentRoseBrush),
            new Point(px.ScreenX + 4, px.ScreenY - 6));
        context.DrawText(new FormattedText("Y", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 10, tokens.AccentEmeraldBrush),
            new Point(py.ScreenX + 4, py.ScreenY - 6));
        context.DrawText(new FormattedText("Z", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 10, tokens.AccentCyanBrush),
            new Point(pz.ScreenX + 4, pz.ScreenY - 6));
    }

    private void RenderSurfaceMesh(DrawingContext context, double ox, double oy, double scale, MathThemeTokens tokens)
    {
        int gridN = 22; // 22x22 grid
        double domain = 3.0;
        double step = (2.0 * domain) / gridN;

        var quads = new List<SurfaceQuad>(gridN * gridN);

        for (int i = 0; i < gridN; i++)
        {
            double x0 = -domain + i * step;
            double x1 = x0 + step;

            for (int j = 0; j < gridN; j++)
            {
                double y0 = -domain + j * step;
                double y1 = y0 + step;

                double z00 = _surface.Evaluate(x0, y0);
                double z10 = _surface.Evaluate(x1, y0);
                double z11 = _surface.Evaluate(x1, y1);
                double z01 = _surface.Evaluate(x0, y1);

                var p0 = ProjectPoint(x0, y0, z00, ox, oy, scale);
                var p1 = ProjectPoint(x1, y0, z10, ox, oy, scale);
                var p2 = ProjectPoint(x1, y1, z11, ox, oy, scale);
                var p3 = ProjectPoint(x0, y1, z01, ox, oy, scale);

                double avgDepth = (p0.Depth + p1.Depth + p2.Depth + p3.Depth) * 0.25;
                double avgZ = (z00 + z10 + z11 + z01) * 0.25;

                quads.Add(new SurfaceQuad(p0, p1, p2, p3, avgZ, avgDepth));
            }
        }

        // Painter's algorithm: sort back-to-front
        quads.Sort((a, b) => a.AvgDepth.CompareTo(b.AvgDepth));

        var wirePen = ShowWireframe ? new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 0.6) : null;

        foreach (var quad in quads)
        {
            var brush = GetAltitudeBrush(quad.AvgZ);

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(new Point(quad.P0.ScreenX, quad.P0.ScreenY), true);
                ctx.LineTo(new Point(quad.P1.ScreenX, quad.P1.ScreenY));
                ctx.LineTo(new Point(quad.P2.ScreenX, quad.P2.ScreenY));
                ctx.LineTo(new Point(quad.P3.ScreenX, quad.P3.ScreenY));
            }

            context.DrawGeometry(brush, wirePen, geom);
        }
    }

    private static IBrush GetAltitudeBrush(double z)
    {
        // Smooth colormap from deep indigo (-2) to cyan (0) to emerald (1) to amber/rose (3+)
        double t = Math.Clamp((z + 1.5) / 4.0, 0.0, 1.0);

        byte r, g, b;
        if (t < 0.33)
        {
            double k = t / 0.33;
            r = (byte)(30 + 10 * k);
            g = (byte)(40 + 140 * k);
            b = (byte)(180 + 40 * k);
        }
        else if (t < 0.66)
        {
            double k = (t - 0.33) / 0.33;
            r = (byte)(40 + 170 * k);
            g = (byte)(180 + 20 * k);
            b = (byte)(220 - 150 * k);
        }
        else
        {
            double k = (t - 0.66) / 0.34;
            r = (byte)(210 + 35 * k);
            g = (byte)(200 - 100 * k);
            b = (byte)(70 + 40 * k);
        }

        return new SolidColorBrush(Color.FromArgb(175, r, g, b));
    }

    private void RenderTangentPlane(DrawingContext context, double ox, double oy, double scale, MathThemeTokens tokens)
    {
        double x0 = ProbeX;
        double y0 = ProbeY;
        double z0 = _surface.Evaluate(x0, y0);

        var (fx, fy) = _surface.ComputeGradient(x0, y0);

        // Tangent Plane patch size
        double d = 0.9;
        double[] dxs = { -d, d, d, -d };
        double[] dys = { -d, -d, d, d };

        var pts = new Point[4];
        for (int i = 0; i < 4; i++)
        {
            double tx = x0 + dxs[i];
            double ty = y0 + dys[i];
            double tz = z0 + fx * dxs[i] + fy * dys[i];
            var p = ProjectPoint(tx, ty, tz, ox, oy, scale);
            pts[i] = new Point(p.ScreenX, p.ScreenY);
        }

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(pts[0], true);
            ctx.LineTo(pts[1]);
            ctx.LineTo(pts[2]);
            ctx.LineTo(pts[3]);
        }

        // Translucent golden tangent plane
        var planeBrush = new SolidColorBrush(Color.FromArgb(120, 245, 158, 11));
        var planePen = new Pen(tokens.AccentAmberBrush, 1.5);
        context.DrawGeometry(planeBrush, planePen, geom);

        // Center contact point orb
        var centerPt = ProjectPoint(x0, y0, z0, ox, oy, scale);
        var cScreen = new Point(centerPt.ScreenX, centerPt.ScreenY);
        context.DrawEllipse(tokens.AccentAmberBrush, null, cScreen, 5.0, 5.0);
        context.DrawEllipse(null, new Pen(Brushes.White, 1.8), cScreen, 8.0, 8.0);

        // Normal Vector arrow: n = <-fx, -fy, 1>
        if (ShowNormalVector)
        {
            double nLen = Math.Sqrt(fx * fx + fy * fy + 1.0);
            double nx = (-fx / nLen) * 1.5;
            double ny = (-fy / nLen) * 1.5;
            double nz = (1.0 / nLen) * 1.5;

            var tipPt = ProjectPoint(x0 + nx, y0 + ny, z0 + nz, ox, oy, scale);
            var tipScreen = new Point(tipPt.ScreenX, tipPt.ScreenY);

            var normPen = new Pen(tokens.AccentCyanBrush, 2.2);
            context.DrawLine(normPen, cScreen, tipScreen);
            context.DrawEllipse(tokens.AccentCyanBrush, null, tipScreen, 3.5, 3.5);
        }
    }

    private void RenderHUD(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        double x0 = ProbeX;
        double y0 = ProbeY;
        double z0 = _surface.Evaluate(x0, y0);
        var (fx, fy) = _surface.ComputeGradient(x0, y0);
        var (fxx, fyy, fxy, d) = _surface.ComputeHessian(x0, y0);
        string classification = _surface.ClassifyPoint(x0, y0);

        double gradMag = Math.Sqrt(fx * fx + fy * fy);

        double hudW = 270;
        double hudH = 135;
        double hudX = w - hudW - 16;
        double hudY = 56;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 15, 23, 42)), hudRect, 6);
        context.DrawRectangle(new Pen(tokens.AccentAmberBrush, 1.2), hudRect, 6);

        string[] lines =
        {
            $"Surface: z = {_surface.Formula}",
            $"Point: ({x0:F2}, {y0:F2}, {z0:F2})",
            $"∇f: <{fx:F2}, {fy:F2}>  (||∇f|| = {gradMag:F2})",
            $"Tangent: z = {z0:F2} + {fx:F2}(x - {x0:F1}) + {fy:F2}(y - {y0:F1})",
            $"Hessian: D = {d:F2}  (f_xx = {fxx:F2})",
            $"Status: {classification}"
        };

        IBrush[] brushes =
        {
            tokens.TextPrimaryBrush,
            tokens.AccentCyanBrush,
            tokens.AccentEmeraldBrush,
            tokens.AccentAmberBrush,
            tokens.AccentPurpleBrush,
            tokens.AccentRoseBrush
        };

        double textY = hudY + 8;
        for (int i = 0; i < lines.Length; i++)
        {
            var text = new FormattedText(
                lines[i],
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, i == 0 || i == 5 ? FontWeight.Bold : FontWeight.Normal),
                10,
                brushes[i]);
            context.DrawText(text, new Point(hudX + 10, textY));
            textY += 19;
        }

        // Camera hint badge
        var hint = new FormattedText(
            "Drag mouse to orbit 3D view  •  Scroll to zoom",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
            10,
            tokens.TextMutedBrush);
        context.DrawText(hint, new Point(16, h - 26));
    }
}
