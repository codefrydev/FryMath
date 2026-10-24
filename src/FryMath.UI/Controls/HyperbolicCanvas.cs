using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Hyperbolic;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class HyperbolicCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<HyperbolicCanvas, string>(nameof(PresetName), "heptagonal");

    public static readonly StyledProperty<string> ModelNameProperty =
        AvaloniaProperty.Register<HyperbolicCanvas, string>(nameof(ModelName), "poincare");

    public static readonly StyledProperty<int> TilingDepthProperty =
        AvaloniaProperty.Register<HyperbolicCanvas, int>(nameof(TilingDepth), 2);

    public static readonly StyledProperty<bool> ShowTessellationProperty =
        AvaloniaProperty.Register<HyperbolicCanvas, bool>(nameof(ShowTessellation), true);

    public static readonly StyledProperty<bool> ShowVerticesProperty =
        AvaloniaProperty.Register<HyperbolicCanvas, bool>(nameof(ShowVertices), true);

    public static readonly StyledProperty<bool> ShowInteractiveTriangleProperty =
        AvaloniaProperty.Register<HyperbolicCanvas, bool>(nameof(ShowInteractiveTriangle), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public string ModelName
    {
        get => GetValue(ModelNameProperty);
        set => SetValue(ModelNameProperty, value);
    }

    public int TilingDepth
    {
        get => GetValue(TilingDepthProperty);
        set => SetValue(TilingDepthProperty, value);
    }

    public bool ShowTessellation
    {
        get => GetValue(ShowTessellationProperty);
        set => SetValue(ShowTessellationProperty, value);
    }

    public bool ShowVertices
    {
        get => GetValue(ShowVerticesProperty);
        set => SetValue(ShowVerticesProperty, value);
    }

    public bool ShowInteractiveTriangle
    {
        get => GetValue(ShowInteractiveTriangleProperty);
        set => SetValue(ShowInteractiveTriangleProperty, value);
    }

    private readonly HyperbolicGeometryEngine _engine = new();

    // Interactive Triangle Vertices
    private HyperbolicPoint _triA = new(-0.35, -0.25);
    private HyperbolicPoint _triB = new(0.45, -0.2);
    private HyperbolicPoint _triC = new(0.0, 0.45);
    private int _draggedPointIndex = -1; // 0 for A, 1 for B, 2 for C

    // Navigation
    private double _zoom = 1.0;
    private double _panX = 0;
    private double _panY = 0;
    private Point? _lastMousePos;
    private bool _isRightMouseDown;

    // Hover Probe
    private Point? _hoverScreenPos;
    private HyperbolicPoint? _hoverHyperbolicPos;

    static HyperbolicCanvas()
    {
        AffectsRender<HyperbolicCanvas>(
            PresetNameProperty,
            ModelNameProperty,
            TilingDepthProperty,
            ShowTessellationProperty,
            ShowVerticesProperty,
            ShowInteractiveTriangleProperty);
    }

    public HyperbolicCanvas()
    {
        ClipToBounds = true;
        _engine.SetInteractiveTriangle(_triA, _triB, _triC);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PresetNameProperty || change.Property == TilingDepthProperty)
        {
            string name = PresetName?.ToLowerInvariant() ?? "heptagonal";
            var preset = name switch
            {
                "octagonal" or "8_3" => HyperbolicTilingPreset.Octagonal8_3,
                "square5" or "4_5" => HyperbolicTilingPreset.Order5Square4_5,
                "triangular" or "3_7" => HyperbolicTilingPreset.Triangular3_7,
                "ideal" => HyperbolicTilingPreset.IdealTriangle,
                "holonomy" => HyperbolicTilingPreset.GeodesicHolonomy,
                _ => HyperbolicTilingPreset.Heptagonal7_3
            };

            _engine.TilingDepth = Math.Clamp(TilingDepth, 1, 3);
            _engine.LoadPreset(preset);

            if (preset == HyperbolicTilingPreset.GeodesicHolonomy || preset == HyperbolicTilingPreset.IdealTriangle)
            {
                if (_engine.ActiveTriangle != null)
                {
                    _triA = _engine.ActiveTriangle.A;
                    _triB = _engine.ActiveTriangle.B;
                    _triC = _engine.ActiveTriangle.C;
                }
            }
            else
            {
                _engine.SetInteractiveTriangle(_triA, _triB, _triC);
            }
        }
        else if (change.Property == ModelNameProperty)
        {
            _engine.Model = (ModelName?.ToLowerInvariant() == "klein")
                ? HyperbolicModel.KleinBeltrami
                : HyperbolicModel.PoincareDisk;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        _lastMousePos = pt.Position;

        if (pt.Properties.IsRightButtonPressed)
        {
            _isRightMouseDown = true;
            return;
        }

        if (pt.Properties.IsLeftButtonPressed && ShowInteractiveTriangle)
        {
            // Check if clicking near triangle vertices A, B, or C
            double cx = Bounds.Width * 0.5 + _panX;
            double cy = Bounds.Height * 0.5 + _panY;
            double diskRadius = Math.Min(Bounds.Width, Bounds.Height) * 0.42 * _zoom;

            var screenA = HyperbolicToScreen(_triA, cx, cy, diskRadius);
            var screenB = HyperbolicToScreen(_triB, cx, cy, diskRadius);
            var screenC = HyperbolicToScreen(_triC, cx, cy, diskRadius);

            double pickupRadius = 18.0;
            if (Distance(pt.Position, screenA) < pickupRadius) _draggedPointIndex = 0;
            else if (Distance(pt.Position, screenB) < pickupRadius) _draggedPointIndex = 1;
            else if (Distance(pt.Position, screenC) < pickupRadius) _draggedPointIndex = 2;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);
        var pos = pt.Position;

        double cx = Bounds.Width * 0.5 + _panX;
        double cy = Bounds.Height * 0.5 + _panY;
        double diskRadius = Math.Min(Bounds.Width, Bounds.Height) * 0.42 * _zoom;

        if (_isRightMouseDown && _lastMousePos.HasValue)
        {
            var delta = pos - _lastMousePos.Value;
            _panX += delta.X;
            _panY += delta.Y;
            InvalidateVisual();
        }
        else if (_draggedPointIndex >= 0)
        {
            // Drag triangle vertex
            var hyp = ScreenToHyperbolic(pos, cx, cy, diskRadius);
            double distSq = hyp.X * hyp.X + hyp.Y * hyp.Y;
            if (distSq < 0.96) // Stay strictly inside unit disk
            {
                if (_draggedPointIndex == 0) _triA = hyp;
                else if (_draggedPointIndex == 1) _triB = hyp;
                else if (_draggedPointIndex == 2) _triC = hyp;

                _engine.SetInteractiveTriangle(_triA, _triB, _triC);
                InvalidateVisual();
            }
        }

        _lastMousePos = pos;

        // Hover Probe calculation
        _hoverScreenPos = pos;
        var hPt = ScreenToHyperbolic(pos, cx, cy, diskRadius);
        if (hPt.X * hPt.X + hPt.Y * hPt.Y < 1.0)
        {
            _hoverHyperbolicPos = hPt;
            InvalidateVisual();
        }
        else
        {
            _hoverHyperbolicPos = null;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggedPointIndex = -1;
        _isRightMouseDown = false;
        _lastMousePos = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _zoom = Math.Clamp(_zoom + e.Delta.Y * 0.12, 0.4, 4.0);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width <= 10 || height <= 10) return;

        // Background
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(10, 10, 20)), new Rect(0, 0, width, height));

        double cx = width * 0.5 + _panX;
        double cy = height * 0.5 + _panY;
        double diskRadius = Math.Min(width, height) * 0.42 * _zoom;

        // 1. Poincaré Disk Interior Glow & Boundary Horizon
        DrawDiskBoundary(context, cx, cy, diskRadius);

        // 2. Tessellation Geodesics
        if (ShowTessellation)
        {
            DrawTessellation(context, cx, cy, diskRadius);
        }

        // 3. Interactive Hyperbolic Triangle & Gauss-Bonnet Defect
        if (ShowInteractiveTriangle && _engine.ActiveTriangle != null)
        {
            DrawInteractiveTriangle(context, cx, cy, diskRadius);
        }

        // 4. Vertices
        if (ShowVertices)
        {
            DrawVertices(context, cx, cy, diskRadius);
        }

        // 5. Diagnostics HUD & Hover Probe
        DrawDiagnosticsHUD(context, width, height);
    }

    private void DrawDiskBoundary(DrawingContext context, double cx, double cy, double diskRadius)
    {
        // Dark indigo disc interior
        var diskBrush = new SolidColorBrush(Color.FromArgb(180, 15, 23, 42));
        context.DrawEllipse(diskBrush, null, new Point(cx, cy), diskRadius, diskRadius);

        // Radiant boundary circle (horizon at infinity)
        var boundaryPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 6, 182, 212)), 2.5); // Cyan
        var outerGlowPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 168, 85, 247)), 6.0); // Purple glow
        context.DrawEllipse(null, outerGlowPen, new Point(cx, cy), diskRadius, diskRadius);
        context.DrawEllipse(null, boundaryPen, new Point(cx, cy), diskRadius, diskRadius);
    }

    private void DrawTessellation(DrawingContext context, double cx, double cy, double diskRadius)
    {
        var edgePen = new Pen(new SolidColorBrush(Color.FromArgb(140, 99, 102, 241)), 1.2); // Indigo/Purple
        bool isKlein = (_engine.Model == HyperbolicModel.KleinBeltrami);

        foreach (var arc in _engine.Geodesics)
        {
            if (isKlein)
            {
                // In Klein model, geodesics are straight Euclidean chords!
                var kp1 = PoincareToKlein(arc.P1);
                var kp2 = PoincareToKlein(arc.P2);
                var s1 = HyperbolicToScreen(kp1, cx, cy, diskRadius);
                var s2 = HyperbolicToScreen(kp2, cx, cy, diskRadius);
                context.DrawLine(edgePen, s1, s2);
            }
            else
            {
                // Poincaré disk model
                DrawPoincareArc(context, arc, cx, cy, diskRadius, edgePen);
            }
        }
    }

    private void DrawPoincareArc(DrawingContext context, HyperbolicArc arc, double cx, double cy, double diskRadius, Pen pen)
    {
        if (arc.IsDiameter)
        {
            var s1 = HyperbolicToScreen(arc.P1, cx, cy, diskRadius);
            var s2 = HyperbolicToScreen(arc.P2, cx, cy, diskRadius);
            context.DrawLine(pen, s1, s2);
            return;
        }

        // Circular arc sampled into short segments for exact curvature
        int segments = 24;
        Point? prev = null;

        for (int i = 0; i <= segments; i++)
        {
            double t = (double)i / segments;
            double angle = arc.StartAngle + t * arc.SweepAngle;
            double hx = arc.CenterX + arc.Radius * Math.Cos(angle);
            double hy = arc.CenterY + arc.Radius * Math.Sin(angle);

            var s = HyperbolicToScreen(new HyperbolicPoint(hx, hy), cx, cy, diskRadius);
            if (prev.HasValue)
            {
                context.DrawLine(pen, prev.Value, s);
            }
            prev = s;
        }
    }

    private void DrawInteractiveTriangle(DrawingContext context, double cx, double cy, double diskRadius)
    {
        var tri = _engine.ActiveTriangle!;
        var arcAB = HyperbolicGeometryEngine.ConstructGeodesicArc(tri.A, tri.B);
        var arcBC = HyperbolicGeometryEngine.ConstructGeodesicArc(tri.B, tri.C);
        var arcCA = HyperbolicGeometryEngine.ConstructGeodesicArc(tri.C, tri.A);

        var triPen = new Pen(new SolidColorBrush(Color.FromRgb(251, 191, 36)), 2.4); // Gold

        DrawPoincareArc(context, arcAB, cx, cy, diskRadius, triPen);
        DrawPoincareArc(context, arcBC, cx, cy, diskRadius, triPen);
        DrawPoincareArc(context, arcCA, cx, cy, diskRadius, triPen);

        // Draw handles A, B, C
        DrawHandle(context, tri.A, "A", cx, cy, diskRadius, Color.FromRgb(244, 63, 94)); // Rose
        DrawHandle(context, tri.B, "B", cx, cy, diskRadius, Color.FromRgb(16, 185, 129)); // Emerald
        DrawHandle(context, tri.C, "C", cx, cy, diskRadius, Color.FromRgb(6, 182, 212));  // Cyan
    }

    private void DrawHandle(DrawingContext context, HyperbolicPoint pt, string label, double cx, double cy, double diskRadius, Color color)
    {
        var s = HyperbolicToScreen(pt, cx, cy, diskRadius);
        var brush = new SolidColorBrush(color);
        var whitePen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1.5);

        context.DrawEllipse(brush, whitePen, s, 6.5, 6.5);

        var lblFormatted = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter,sans-serif", FontStyle.Normal, FontWeight.Bold),
            11,
            new SolidColorBrush(Color.FromRgb(255, 255, 255))
        );
        context.DrawText(lblFormatted, new Point(s.X + 9, s.Y - 7));
    }

    private void DrawVertices(DrawingContext context, double cx, double cy, double diskRadius)
    {
        var vertexBrush = new SolidColorBrush(Color.FromArgb(190, 6, 182, 212));
        foreach (var v in _engine.Vertices)
        {
            var s = HyperbolicToScreen(v, cx, cy, diskRadius);
            context.DrawEllipse(vertexBrush, null, s, 2.0, 2.0);
        }
    }

    private void DrawDiagnosticsHUD(DrawingContext context, double width, double height)
    {
        // Top Badging Banner
        string presetName = _engine.Preset switch
        {
            HyperbolicTilingPreset.Heptagonal7_3 => "Heptagonal Tiling {7, 3}",
            HyperbolicTilingPreset.Octagonal8_3 => "Octagonal Tiling {8, 3}",
            HyperbolicTilingPreset.Order5Square4_5 => "Order-5 Square {4, 5}",
            HyperbolicTilingPreset.Triangular3_7 => "Triangular Tiling {3, 7}",
            HyperbolicTilingPreset.IdealTriangle => "Ideal Triangle (Area = π)",
            HyperbolicTilingPreset.GeodesicHolonomy => "Geodesic Holonomy Defect",
            _ => "Hyperbolic Geometry"
        };

        string hudText = $"{presetName} | Depth = {_engine.TilingDepth} | Geodesics = {_engine.Geodesics.Count} | K = -1";
        if (_engine.ActiveTriangle != null)
        {
            var t = _engine.ActiveTriangle;
            hudText += $" | Area = {t.HyperbolicArea:F3} | Defect = {t.AngleDefect:F3}";
        }

        var hudFormatted = new FormattedText(
            hudText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas,Menlo,monospace", FontStyle.Normal, FontWeight.Bold),
            11.5,
            new SolidColorBrush(Color.FromRgb(6, 182, 212))
        );

        double badgeW = hudFormatted.Width + 24;
        double badgeH = 26;
        double badgeX = Math.Max(12, (width - badgeW) * 0.5);
        double badgeY = 12;

        var badgeBg = new SolidColorBrush(Color.FromArgb(210, 15, 23, 42));
        var badgeBorder = new Pen(new SolidColorBrush(Color.FromArgb(140, 6, 182, 212)), 1);
        context.DrawRectangle(badgeBg, badgeBorder, new Rect(badgeX, badgeY, badgeW, badgeH), 6, 6);
        context.DrawText(hudFormatted, new Point(badgeX + 12, badgeY + 5));

        // Gauss-Bonnet Interactive Triangle Card (Bottom Left)
        if (ShowInteractiveTriangle && _engine.ActiveTriangle != null)
        {
            var t = _engine.ActiveTriangle;
            double sumDeg = (t.AngleA + t.AngleB + t.AngleC) * 180.0 / Math.PI;

            string gbText = "HYPERBOLIC GAUSS-BONNET THEOREM:\n" +
                            $"• Angle α: {t.AngleA * 180 / Math.PI:F1}° ({t.AngleA:F3} rad)\n" +
                            $"• Angle β: {t.AngleB * 180 / Math.PI:F1}° ({t.AngleB:F3} rad)\n" +
                            $"• Angle γ: {t.AngleC * 180 / Math.PI:F1}° ({t.AngleC:F3} rad)\n" +
                            $"• Angle Sum: {sumDeg:F1}° < 180°\n" +
                            $"• Area Defect: π - Σα = {t.AngleDefect:F4} sr\n" +
                            $"• Hyperbolic Area: ∬ dA = {t.HyperbolicArea:F4}";

            var gbFormatted = new FormattedText(
                gbText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas,Menlo,monospace", FontStyle.Normal, FontWeight.SemiBold),
                10.5,
                new SolidColorBrush(Color.FromRgb(241, 245, 249))
            );

            double gbW = gbFormatted.Width + 20;
            double gbH = gbFormatted.Height + 16;
            double gbX = 16;
            double gbY = height - gbH - 16;

            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 15, 23, 42)), new Pen(new SolidColorBrush(Color.FromRgb(251, 191, 36)), 1), new Rect(gbX, gbY, gbW, gbH), 6, 6);
            context.DrawText(gbFormatted, new Point(gbX + 10, gbY + 8));
        }

        // Hover probe HUD (Bottom Right)
        if (_hoverHyperbolicPos != null)
        {
            var p = _hoverHyperbolicPos;
            double d0 = HyperbolicGeometryEngine.HyperbolicDistance(new HyperbolicPoint(0, 0), p);

            string probe = $"PROBE: z = ({p.X:F3}, {p.Y:F3})\n" +
                           $"|z| = {Math.Sqrt(p.X * p.X + p.Y * p.Y):F3}\n" +
                           $"d_hyp(0, z) = {d0:F3}";

            var probeFormatted = new FormattedText(
                probe,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas,Menlo,monospace", FontStyle.Normal, FontWeight.Normal),
                10.0,
                new SolidColorBrush(Color.FromRgb(203, 213, 225))
            );

            double pW = probeFormatted.Width + 16;
            double pH = probeFormatted.Height + 14;
            double pX = width - pW - 16;
            double pY = height - pH - 16;

            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)), new Pen(new SolidColorBrush(Color.FromRgb(148, 163, 184)), 1), new Rect(pX, pY, pW, pH), 4, 4);
            context.DrawText(probeFormatted, new Point(pX + 8, pY + 7));
        }
    }

    private static Point HyperbolicToScreen(HyperbolicPoint p, double cx, double cy, double r)
    {
        return new Point(cx + p.X * r, cy - p.Y * r);
    }

    private static HyperbolicPoint ScreenToHyperbolic(Point s, double cx, double cy, double r)
    {
        return new HyperbolicPoint((s.X - cx) / r, -(s.Y - cy) / r);
    }

    private static HyperbolicPoint PoincareToKlein(HyperbolicPoint p)
    {
        // Projective mapping: w = 2z / (1 + |z|²)
        double normSq = p.X * p.X + p.Y * p.Y;
        double factor = 2.0 / (1.0 + normSq);
        return new HyperbolicPoint(p.X * factor, p.Y * factor);
    }

    private static double Distance(Point p1, Point p2)
    {
        double dx = p1.X - p2.X;
        double dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
