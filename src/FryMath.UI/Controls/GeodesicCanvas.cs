using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Geometry;
using FryMath.Core.LinearAlgebra;
using FryMath.UI.Theming;
using Vector3D = FryMath.Core.LinearAlgebra.Vector3D;

namespace FryMath.UI.Controls;

public class GeodesicCanvas : Control
{
    public static readonly StyledProperty<string> SurfaceTypeProperty =
        AvaloniaProperty.Register<GeodesicCanvas, string>(nameof(SurfaceType), "torus");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<GeodesicCanvas, string>(nameof(PresetName), "torus");

    public static readonly StyledProperty<double> StartUProperty =
        AvaloniaProperty.Register<GeodesicCanvas, double>(nameof(StartU), 0.0);

    public static readonly StyledProperty<double> StartVProperty =
        AvaloniaProperty.Register<GeodesicCanvas, double>(nameof(StartV), 0.0);

    public static readonly StyledProperty<double> HeadingAngleProperty =
        AvaloniaProperty.Register<GeodesicCanvas, double>(nameof(HeadingAngle), 0.785); // 45 deg

    public static readonly StyledProperty<double> DeformationAngleProperty =
        AvaloniaProperty.Register<GeodesicCanvas, double>(nameof(DeformationAngle), 0.0); // Bonnet parameter

    public static readonly StyledProperty<bool> ShowCurvatureHeatmapProperty =
        AvaloniaProperty.Register<GeodesicCanvas, bool>(nameof(ShowCurvatureHeatmap), true);

    public static readonly StyledProperty<bool> ShowGeodesicRayProperty =
        AvaloniaProperty.Register<GeodesicCanvas, bool>(nameof(ShowGeodesicRay), true);

    public static readonly StyledProperty<bool> ShowNormalVectorProperty =
        AvaloniaProperty.Register<GeodesicCanvas, bool>(nameof(ShowNormalVector), true);

    public static readonly StyledProperty<bool> ShowChristoffelMatrixProperty =
        AvaloniaProperty.Register<GeodesicCanvas, bool>(nameof(ShowChristoffelMatrix), true);

    public string SurfaceType
    {
        get => GetValue(SurfaceTypeProperty);
        set => SetValue(SurfaceTypeProperty, value);
    }

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double StartU
    {
        get => GetValue(StartUProperty);
        set => SetValue(StartUProperty, value);
    }

    public double StartV
    {
        get => GetValue(StartVProperty);
        set => SetValue(StartVProperty, value);
    }

    public double HeadingAngle
    {
        get => GetValue(HeadingAngleProperty);
        set => SetValue(HeadingAngleProperty, value);
    }

    public double DeformationAngle
    {
        get => GetValue(DeformationAngleProperty);
        set => SetValue(DeformationAngleProperty, value);
    }

    public bool ShowCurvatureHeatmap
    {
        get => GetValue(ShowCurvatureHeatmapProperty);
        set => SetValue(ShowCurvatureHeatmapProperty, value);
    }

    public bool ShowGeodesicRay
    {
        get => GetValue(ShowGeodesicRayProperty);
        set => SetValue(ShowGeodesicRayProperty, value);
    }

    public bool ShowNormalVector
    {
        get => GetValue(ShowNormalVectorProperty);
        set => SetValue(ShowNormalVectorProperty, value);
    }

    public bool ShowChristoffelMatrix
    {
        get => GetValue(ShowChristoffelMatrixProperty);
        set => SetValue(ShowChristoffelMatrixProperty, value);
    }

    private readonly MathThemeTokens _tokens = MathThemeTokens.DarkElectric;

    private double _yaw = 0.75;
    private double _pitch = 0.50;
    private double _zoom = 1.0;
    private bool _isOrbiting;
    private Point _lastMouse;

    static GeodesicCanvas()
    {
        AffectsRender<GeodesicCanvas>(
            SurfaceTypeProperty, PresetNameProperty,
            StartUProperty, StartVProperty, HeadingAngleProperty, DeformationAngleProperty,
            ShowCurvatureHeatmapProperty, ShowGeodesicRayProperty,
            ShowNormalVectorProperty, ShowChristoffelMatrixProperty
        );
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var prop = e.GetCurrentPoint(this).Properties;

        if (prop.IsLeftButtonPressed && pt.X < Bounds.Width * 0.62)
        {
            _isOrbiting = true;
            _lastMouse = pt;
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isOrbiting)
        {
            var pt = e.GetPosition(this);
            double dx = pt.X - _lastMouse.X;
            double dy = pt.Y - _lastMouse.Y;
            _yaw += dx * 0.008;
            _pitch = Math.Clamp(_pitch + dy * 0.008, -1.4, 1.4);
            _lastMouse = pt;
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isOrbiting = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _zoom = Math.Clamp(_zoom + e.Delta.Y * 0.08, 0.4, 2.8);
        InvalidateVisual();
        e.Handled = true;
    }

    private (Point Pt, double Depth) Project3D(Vector3D v, Rect bounds)
    {
        double cosY = Math.Cos(_yaw), sinY = Math.Sin(_yaw);
        double cosP = Math.Cos(_pitch), sinP = Math.Sin(_pitch);

        double x1 = v.X * cosY - v.Z * sinY;
        double z1 = v.X * sinY + v.Z * cosY;

        double y2 = v.Y * cosP - z1 * sinP;
        double z2 = v.Y * sinP + z1 * cosP;

        double depth = z2 + 10.0;
        if (depth < 0.2) depth = 0.2;

        double scale = (10.0 / depth) * _zoom * (Math.Min(bounds.Width, bounds.Height) / 5.6);
        double px = bounds.X + bounds.Width * 0.5 + x1 * scale;
        double py = bounds.Y + bounds.Height * 0.52 - y2 * scale;

        return (new Point(px, py), depth);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width < 200 || bounds.Height < 150) return;

        context.FillRectangle(_tokens.BgBrush, bounds);

        double splitX = bounds.Width * 0.60;
        Rect leftRect = new(0, 0, splitX, bounds.Height);
        Rect rightTopRect = new(splitX, 0, bounds.Width - splitX, bounds.Height * 0.50);
        Rect rightBottomRect = new(splitX, bounds.Height * 0.50, bounds.Width - splitX, bounds.Height * 0.50);

        SurfaceType surface = ParseSurfaceType(SurfaceType);

        // 1. Draw 3D Curved Surface & Geodesics
        Render3DSurfaceSpace(context, leftRect, surface);

        // 2. Draw Metric & Theorema Egregium Inspector
        RenderMetricInspector(context, rightTopRect, surface);

        // 3. Draw Christoffel & Geodesic Equations Inspector
        RenderChristoffelInspector(context, rightBottomRect, surface);
    }

    private static SurfaceType ParseSurfaceType(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "sphere" => Core.Geometry.SurfaceType.Sphere,
            "pseudosphere" => Core.Geometry.SurfaceType.Pseudosphere,
            "enneper" => Core.Geometry.SurfaceType.Enneper,
            "bonnet" or "catenoid" or "helicoid" => Core.Geometry.SurfaceType.CatenoidHelicoid,
            "torus" or _ => Core.Geometry.SurfaceType.Torus
        };
    }

    private void Render3DSurfaceSpace(DrawingContext context, Rect rect, SurfaceType surface)
    {
        using (context.PushClip(rect))
        {
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), rect);

            // Ground shadow grid
            DrawGroundGrid(context, rect);

            // Surface Wireframe / Heatmap Mesh
            DrawSurfaceMesh(context, rect, surface);

            // Geodesic Trajectory
            if (ShowGeodesicRay)
            {
                DrawGeodesicCurve(context, rect, surface);
            }

            // Start Point Probe & Normal Vector
            DrawStartProbeAndNormal(context, rect, surface);

            // Title badge
            string surfaceTitle = surface switch
            {
                Core.Geometry.SurfaceType.Sphere => "SPHERE S² (CONSTANT K = +1/R² > 0)",
                Core.Geometry.SurfaceType.Pseudosphere => "BELTRAMI PSEUDOSPHERE (CONSTANT K = -1 < 0)",
                Core.Geometry.SurfaceType.Enneper => "ENNEPER MINIMAL SURFACE (H = 0, K ≤ 0)",
                Core.Geometry.SurfaceType.CatenoidHelicoid => "BONNET ISOMETRIC FAMILY (CATENOID ↔ HELICOID)",
                Core.Geometry.SurfaceType.Torus or _ => "TORUS T² (MIXED CURVATURE K > 0, K = 0, K < 0)"
            };
            DrawBadge(context, new Point(rect.X + 16, rect.Y + 16), surfaceTitle, _tokens.AccentCyanBrush);

            // Camera stats HUD
            string hudText = $"Yaw: {_yaw * 180 / Math.PI:F0}° | Pitch: {_pitch * 180 / Math.PI:F0}° | Zoom: {_zoom:F2}x (Drag to orbit, scroll to zoom)";
            FormattedText ftHud = new(
                hudText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif"),
                11,
                _tokens.TextMutedBrush
            );
            context.DrawText(ftHud, new Point(rect.X + 16, rect.Bottom - 26));
        }
    }

    private void DrawGroundGrid(DrawingContext context, Rect rect)
    {
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)), 1);
        int steps = 4;
        double sz = 1.0;

        for (int i = -steps; i <= steps; i++)
        {
            double val = i * sz;
            var (p1, _) = Project3D(new Vector3D(val, -1.8, -steps * sz), rect);
            var (p2, _) = Project3D(new Vector3D(val, -1.8, steps * sz), rect);
            context.DrawLine(gridPen, p1, p2);

            var (q1, _) = Project3D(new Vector3D(-steps * sz, -1.8, val), rect);
            var (q2, _) = Project3D(new Vector3D(steps * sz, -1.8, val), rect);
            context.DrawLine(gridPen, q1, q2);
        }
    }

    private void DrawSurfaceMesh(DrawingContext context, Rect rect, SurfaceType surface)
    {
        int uSteps = 28;
        int vSteps = 18;

        var (uMin, uMax, vMin, vMax) = GetParameterBounds(surface);

        for (int i = 0; i < uSteps; i++)
        {
            double u1 = uMin + (uMax - uMin) * (i / (double)uSteps);
            double u2 = uMin + (uMax - uMin) * ((i + 1) / (double)uSteps);

            for (int j = 0; j < vSteps; j++)
            {
                double v1 = vMin + (vMax - vMin) * (j / (double)vSteps);
                double v2 = vMin + (vMax - vMin) * ((j + 1) / (double)vSteps);

                Vector3D p1 = CurvedSurfaceEngine.EvaluatePosition(surface, u1, v1, DeformationAngle);
                Vector3D p2 = CurvedSurfaceEngine.EvaluatePosition(surface, u2, v1, DeformationAngle);
                Vector3D p3 = CurvedSurfaceEngine.EvaluatePosition(surface, u2, v2, DeformationAngle);
                Vector3D p4 = CurvedSurfaceEngine.EvaluatePosition(surface, u1, v2, DeformationAngle);

                var (pt1, d1) = Project3D(p1, rect);
                var (pt2, d2) = Project3D(p2, rect);
                var (pt3, d3) = Project3D(p3, rect);
                var (pt4, d4) = Project3D(p4, rect);

                // Backface culling check or depth ordering
                double avgDepth = (d1 + d2 + d3 + d4) * 0.25;

                // Color based on Gaussian Curvature K
                Color quadColor = GetCurvatureColor(surface, (u1 + u2) * 0.5, (v1 + v2) * 0.5);
                var fillBrush = new SolidColorBrush(Color.FromArgb(55, quadColor.R, quadColor.G, quadColor.B));
                var wirePen = new Pen(new SolidColorBrush(Color.FromArgb(90, quadColor.R, quadColor.G, quadColor.B)), 0.8);

                var streamGeom = new StreamGeometry();
                using (var ctx = streamGeom.Open())
                {
                    ctx.BeginFigure(pt1, isFilled: true);
                    ctx.LineTo(pt2);
                    ctx.LineTo(pt3);
                    ctx.LineTo(pt4);
                    ctx.EndFigure(isClosed: true);
                }

                context.DrawGeometry(ShowCurvatureHeatmap ? fillBrush : null, wirePen, streamGeom);
            }
        }
    }

    private static (double uMin, double uMax, double vMin, double vMax) GetParameterBounds(SurfaceType surface)
    {
        return surface switch
        {
            Core.Geometry.SurfaceType.Sphere => (0.0, Math.PI * 2.0, 0.15, Math.PI - 0.15),
            Core.Geometry.SurfaceType.Pseudosphere => (0.0, Math.PI * 2.0, 0.2, 2.6),
            Core.Geometry.SurfaceType.Enneper => (-1.3, 1.3, -1.3, 1.3),
            Core.Geometry.SurfaceType.CatenoidHelicoid => (0.0, Math.PI * 2.0, -1.2, 1.2),
            Core.Geometry.SurfaceType.Torus or _ => (0.0, Math.PI * 2.0, 0.0, Math.PI * 2.0)
        };
    }

    private Color GetCurvatureColor(SurfaceType surface, double u, double v)
    {
        var pt = CurvedSurfaceEngine.EvaluatePoint(surface, u, v, DeformationAngle);
        double k = pt.GaussianCurvature;

        if (k > 0.05)
        {
            // Elliptic / Dome (Emerald -> Amber)
            double t = Math.Clamp(k * 1.5, 0.0, 1.0);
            return Color.FromRgb(
                (byte)(16 + t * (245 - 16)),
                (byte)(185 - t * (185 - 158)),
                (byte)(129 - t * (129 - 11))
            );
        }
        else if (k < -0.05)
        {
            // Hyperbolic / Saddle (Indigo -> Rose)
            double t = Math.Clamp(-k * 1.2, 0.0, 1.0);
            return Color.FromRgb(
                (byte)(99 + t * (244 - 99)),
                (byte)(102 - t * (102 - 63)),
                (byte)(241 - t * (241 - 94))
            );
        }
        else
        {
            // Flat / Parabolic (Cyan)
            return _tokens.AccentCyan;
        }
    }

    private void DrawGeodesicCurve(DrawingContext context, Rect rect, SurfaceType surface)
    {
        var path = CurvedSurfaceEngine.TraceGeodesic(
            surface, StartU, StartV, HeadingAngle, steps: 140, stepSize: 0.06, paramA: DeformationAngle);

        if (path.Count < 2) return;

        var geoPen = new Pen(_tokens.AccentAmberBrush, 3.0);
        Point prevPt = Project3D(path[0], rect).Pt;

        for (int i = 1; i < path.Count; i++)
        {
            Point curPt = Project3D(path[i], rect).Pt;
            context.DrawLine(geoPen, prevPt, curPt);
            prevPt = curPt;
        }
    }

    private void DrawStartProbeAndNormal(DrawingContext context, Rect rect, SurfaceType surface)
    {
        var pt = CurvedSurfaceEngine.EvaluatePoint(surface, StartU, StartV, DeformationAngle);
        var (screenPt, _) = Project3D(pt.Position, rect);

        // Gold sphere at start position
        context.DrawEllipse(_tokens.AccentAmberBrush, new Pen(new SolidColorBrush(Colors.White), 2), screenPt, 6, 6);

        // Normal Vector
        if (ShowNormalVector)
        {
            Vector3D normalTip = pt.Position + pt.Normal * 0.9;
            var (normalScreenTip, _) = Project3D(normalTip, rect);
            DrawArrow3D(context, screenPt, normalScreenTip, _tokens.AccentRoseBrush, 2.5, "n");
        }

        // Heading Tangent Vector
        Vector3D pU = (CurvedSurfaceEngine.EvaluatePosition(surface, StartU + 1e-4, StartV, DeformationAngle) -
                       CurvedSurfaceEngine.EvaluatePosition(surface, StartU - 1e-4, StartV, DeformationAngle)) / 2e-4;
        Vector3D pV = (CurvedSurfaceEngine.EvaluatePosition(surface, StartU, StartV + 1e-4, DeformationAngle) -
                       CurvedSurfaceEngine.EvaluatePosition(surface, StartU, StartV - 1e-4, DeformationAngle)) / 2e-4;

        Vector3D tanDir = (Math.Cos(HeadingAngle) * pU.Normalize() + Math.Sin(HeadingAngle) * pV.Normalize()) * 0.8;
        var (tanScreenTip, _) = Project3D(pt.Position + tanDir, rect);
        DrawArrow3D(context, screenPt, tanScreenTip, _tokens.AccentEmeraldBrush, 2.5, "v₀ (Heading)");
    }

    private void RenderMetricInspector(DrawingContext context, Rect rect, SurfaceType surface)
    {
        using (context.PushClip(rect))
        {
            context.FillRectangle(_tokens.SurfaceBrush, rect);
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), rect);

            DrawBadge(context, new Point(rect.X + 16, rect.Y + 14),
                "RIEMANNIAN METRIC & GAUSS'S THEOREMA EGREGIUM", _tokens.AccentCyanBrush);

            var pt = CurvedSurfaceEngine.EvaluatePoint(surface, StartU, StartV, DeformationAngle);
            var m = pt.Metric;

            double y = rect.Y + 44;

            // First Fundamental Form
            string fffText = $"First Fundamental Form I:  E = {m.E:F3},  F = {m.F:F3},  G = {m.G:F3}  (det g = {m.Determinant:F3})";
            DrawDataRow(context, rect.X + 16, y, "Metric Tensor g_ij (ds² = E du² + 2F du dv + G dv²):", fffText, _tokens.AccentEmeraldBrush);

            y += 44;
            // Gaussian Curvature K
            string regime = pt.GaussianCurvature > 0.02 ? "Elliptic (Positive, Sphere/Dome)" :
                            pt.GaussianCurvature < -0.02 ? "Hyperbolic (Negative, Saddle/Neck)" : "Parabolic (Zero Flat, Cylinder/Cone)";
            IBrush kBrush = pt.GaussianCurvature > 0.02 ? _tokens.AccentEmeraldBrush :
                            pt.GaussianCurvature < -0.02 ? _tokens.AccentRoseBrush : _tokens.AccentCyanBrush;

            string kText = $"Gaussian Curvature K = {pt.GaussianCurvature:F4}  [{regime}]";
            DrawDataRow(context, rect.X + 16, y, "Gauss's Theorema Egregium (Intrinsic Invariant K = det II / det I):", kText, kBrush);

            y += 44;
            // Mean Curvature & Principal Curvatures
            string meanText = $"Mean Curvature H = {pt.MeanCurvature:F4} | Principal Curvatures: κ₁ = {pt.PrincipalK1:F3}, κ₂ = {pt.PrincipalK2:F3}";
            DrawDataRow(context, rect.X + 16, y, "Extrinsic Curvature & Shape Operator (H = ½(κ₁ + κ₂)):", meanText, _tokens.AccentAmberBrush);

            y += 44;
            // Minimality badge if H ≈ 0
            if (Math.Abs(pt.MeanCurvature) < 0.05)
            {
                Rect minBadge = new(rect.X + 16, y, 320, 24);
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(60, 16, 185, 129)), minBadge);
                context.DrawRectangle(null, new Pen(_tokens.AccentEmeraldBrush, 1), minBadge);

                FormattedText ftMin = new(
                    "MINIMAL SURFACE CONFIRMED: H = 0 (Vanishing Mean Curvature)",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
                    10,
                    _tokens.AccentEmeraldBrush
                );
                context.DrawText(ftMin, new Point(minBadge.X + 8, minBadge.Y + 4));
            }
        }
    }

    private void RenderChristoffelInspector(DrawingContext context, Rect rect, SurfaceType surface)
    {
        using (context.PushClip(rect))
        {
            context.FillRectangle(_tokens.SurfaceBrush, rect);
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), rect);

            DrawBadge(context, new Point(rect.X + 16, rect.Y + 14),
                "CHRISTOFFEL SYMBOLS Γᵏᵢⱼ & GEODESIC DYNAMICS", _tokens.AccentAmberBrush);

            var pt = CurvedSurfaceEngine.EvaluatePoint(surface, StartU, StartV, DeformationAngle);
            var c = pt.Christoffel;

            double y = rect.Y + 44;

            string g1Text = $"Γ¹_11 = {c.Gamma1_11:F3},   Γ¹_12 = {c.Gamma1_12:F3},   Γ¹_22 = {c.Gamma1_22:F3}";
            DrawDataRow(context, rect.X + 16, y, "Christoffel Connection Γ¹_ij (u-coordinate acceleration):", g1Text, _tokens.AccentCyanBrush);

            y += 42;
            string g2Text = $"Γ²_11 = {c.Gamma2_11:F3},   Γ²_12 = {c.Gamma2_12:F3},   Γ²_22 = {c.Gamma2_22:F3}";
            DrawDataRow(context, rect.X + 16, y, "Christoffel Connection Γ²_ij (v-coordinate acceleration):", g2Text, _tokens.AccentCyanBrush);

            y += 44;
            // Geodesic ODE system box
            Rect odeBox = new(rect.X + 14, y, rect.Width - 28, 86);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), odeBox);
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), odeBox);

            string odeTitle = "GEODESIC DIFFERENTIAL EQUATIONS (Euler-Lagrange of Arc Length):";
            FormattedText ftOdeTitle = new(
                odeTitle,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
                11,
                _tokens.AccentAmberBrush
            );
            context.DrawText(ftOdeTitle, new Point(odeBox.X + 10, odeBox.Y + 8));

            string odeDesc = "  d²u/ds² + Γ¹_11 (u')² + 2Γ¹_12 u' v' + Γ¹_22 (v')² = 0\n" +
                            "  d²v/ds² + Γ²_11 (u')² + 2Γ²_12 u' v' + Γ²_22 (v')² = 0\n" +
                            "Gauss-Bonnet Holonomy: ∮ v·dw = ∬ K dA = Δθ (Parallel transport angle deficit)";
            FormattedText ftOdeDesc = new(
                odeDesc,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas, Menlo, monospace"),
                10,
                _tokens.TextSecondaryBrush
            );
            context.DrawText(ftOdeDesc, new Point(odeBox.X + 10, odeBox.Y + 28));
        }
    }

    private void DrawDataRow(DrawingContext context, double x, double y, string header, string value, IBrush accent)
    {
        FormattedText ftHeader = new(
            header,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
            11,
            _tokens.TextMutedBrush
        );
        context.DrawText(ftHeader, new Point(x, y));

        FormattedText ftVal = new(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Courier New, monospace", FontStyle.Normal, FontWeight.SemiBold),
            11,
            accent
        );
        context.DrawText(ftVal, new Point(x, y + 16));
    }

    private void DrawArrow3D(DrawingContext context, Point from, Point to, IBrush brush, double thickness, string label)
    {
        var pen = new Pen(brush, thickness);
        context.DrawLine(pen, from, to);

        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len > 8)
        {
            double ux = dx / len;
            double uy = dy / len;
            Point p1 = new(to.X - 12 * ux + 6 * uy, to.Y - 12 * uy - 6 * ux);
            Point p2 = new(to.X - 12 * ux - 6 * uy, to.Y - 12 * uy + 6 * ux);

            var streamGeom = new StreamGeometry();
            using (var ctx = streamGeom.Open())
            {
                ctx.BeginFigure(to, isFilled: true);
                ctx.LineTo(p1);
                ctx.LineTo(p2);
                ctx.EndFigure(isClosed: true);
            }
            context.DrawGeometry(brush, null, streamGeom);
        }

        if (!string.IsNullOrEmpty(label))
        {
            FormattedText ft = new(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
                11,
                brush
            );
            context.DrawText(ft, new Point(to.X + 8, to.Y - 8));
        }
    }

    private void DrawBadge(DrawingContext context, Point origin, string text, IBrush accent)
    {
        FormattedText ft = new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
            11,
            accent
        );

        Rect badgeRect = new(origin.X, origin.Y, ft.Width + 16, ft.Height + 8);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)), badgeRect);
        context.DrawRectangle(null, new Pen(accent, 1), badgeRect);
        context.DrawText(ft, new Point(origin.X + 8, origin.Y + 4));
    }
}
