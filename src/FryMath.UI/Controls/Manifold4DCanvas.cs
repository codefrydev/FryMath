using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Topology4D;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class Manifold4DCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<Manifold4DCanvas, string>(nameof(PresetName), "tesseract");

    public static readonly StyledProperty<double> RotationSpeedProperty =
        AvaloniaProperty.Register<Manifold4DCanvas, double>(nameof(RotationSpeed), 1.0);

    public static readonly StyledProperty<bool> ShowVerticesProperty =
        AvaloniaProperty.Register<Manifold4DCanvas, bool>(nameof(ShowVertices), true);

    public static readonly StyledProperty<bool> ShowDepthColoringProperty =
        AvaloniaProperty.Register<Manifold4DCanvas, bool>(nameof(ShowDepthColoring), true);

    public static readonly StyledProperty<bool> ShowStereographicSphereProperty =
        AvaloniaProperty.Register<Manifold4DCanvas, bool>(nameof(ShowStereographicSphere), true);

    public static readonly StyledProperty<bool> IsRotatingProperty =
        AvaloniaProperty.Register<Manifold4DCanvas, bool>(nameof(IsRotating), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double RotationSpeed
    {
        get => GetValue(RotationSpeedProperty);
        set => SetValue(RotationSpeedProperty, value);
    }

    public bool ShowVertices
    {
        get => GetValue(ShowVerticesProperty);
        set => SetValue(ShowVerticesProperty, value);
    }

    public bool ShowDepthColoring
    {
        get => GetValue(ShowDepthColoringProperty);
        set => SetValue(ShowDepthColoringProperty, value);
    }

    public bool ShowStereographicSphere
    {
        get => GetValue(ShowStereographicSphereProperty);
        set => SetValue(ShowStereographicSphereProperty, value);
    }

    public bool IsRotating
    {
        get => GetValue(IsRotatingProperty);
        set => SetValue(IsRotatingProperty, value);
    }

    private readonly Manifold4DEngine _engine = new();
    private readonly DispatcherTimer _timer;
    private Point? _lastMousePos;
    private bool _isLeftMouseDown;
    private bool _isRightMouseDown;
    private int _hoveredVertexIndex = -1;

    static Manifold4DCanvas()
    {
        AffectsRender<Manifold4DCanvas>(
            PresetNameProperty,
            RotationSpeedProperty,
            ShowVerticesProperty,
            ShowDepthColoringProperty,
            ShowStereographicSphereProperty,
            IsRotatingProperty);
    }

    public Manifold4DCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            if (IsRotating)
            {
                _engine.Step(0.016 * RotationSpeed);
                InvalidateVisual();
            }
        };
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PresetNameProperty)
        {
            string name = change.GetNewValue<string>() ?? "tesseract";
            var presetType = name.ToLowerInvariant() switch
            {
                "clifford" or "torus" => ManifoldPresetType.CliffordTorus,
                "klein" or "kleinbottle" => ManifoldPresetType.KleinBottle4D,
                "hopf" or "hopffibration" => ManifoldPresetType.HopfFibration,
                "cell24" or "24cell" => ManifoldPresetType.Cell24,
                _ => ManifoldPresetType.Tesseract
            };
            _engine.LoadPreset(presetType);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        _lastMousePos = pt.Position;
        if (pt.Properties.IsLeftButtonPressed) _isLeftMouseDown = true;
        if (pt.Properties.IsRightButtonPressed) _isRightMouseDown = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);

        if (_lastMousePos.HasValue)
        {
            double dx = pt.Position.X - _lastMousePos.Value.X;
            double dy = pt.Position.Y - _lastMousePos.Value.Y;

            if (_isLeftMouseDown)
            {
                // Orbit 3D camera
                _engine.CameraYaw += dx * 0.01;
                _engine.CameraPitch = Math.Clamp(_engine.CameraPitch - dy * 0.01, -1.5, 1.5);
                InvalidateVisual();
            }
            else if (_isRightMouseDown)
            {
                // Manual 4D XW plane hyper-rotation
                _engine.AngleXW += dx * 0.015;
                _engine.AngleYW += dy * 0.015;
                InvalidateVisual();
            }
        }

        _lastMousePos = pt.Position;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isLeftMouseDown = false;
        _isRightMouseDown = false;
        _lastMousePos = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _engine.ProjectionDistance = Math.Clamp(_engine.ProjectionDistance - e.Delta.Y * 0.15, 1.4, 4.5);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var tokens = new MathThemeTokens();

        // 1. Background Fill
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        // 2. Stereographic Reference Coordinate Sphere
        double scale = Math.Min(w, h) * 0.28;
        if (ShowStereographicSphere)
        {
            DrawStereographicReference(context, w / 2.0, h / 2.0, scale * 1.05);
        }

        // 3. Project 4D Vertices to Screen
        var projected = _engine.GetProjectedVertices(w, h, scale);

        // Find closest vertex to cursor for hover
        _hoveredVertexIndex = -1;
        if (_lastMousePos.HasValue)
        {
            double bestDist = 20.0;
            for (int i = 0; i < projected.Count; i++)
            {
                double d = Math.Sqrt(Math.Pow(projected[i].ScreenX - _lastMousePos.Value.X, 2) +
                                     Math.Pow(projected[i].ScreenY - _lastMousePos.Value.Y, 2));
                if (d < bestDist)
                {
                    bestDist = d;
                    _hoveredVertexIndex = i;
                }
            }
        }

        // 4. Draw 4D Edges with 4th-Dimensional Depth Shading
        DrawEdges(context, projected);

        // 5. Draw 4D Vertices
        if (ShowVertices)
        {
            DrawVertices(context, projected);
        }

        // 6. Draw Hover Probe HUD
        if (_hoveredVertexIndex >= 0 && _hoveredVertexIndex < _engine.BaseVertices.Count)
        {
            var p = projected[_hoveredVertexIndex];
            var baseV = _engine.BaseVertices[_hoveredVertexIndex];
            DrawHoverHUD(context, new Point(p.ScreenX, p.ScreenY), _hoveredVertexIndex, baseV, p.DepthW, tokens);
        }

        // 7. Top-Right Status Badge
        DrawStatusBadge(context, w, tokens);
    }

    private void DrawStereographicReference(DrawingContext context, double ox, double oy, double r)
    {
        // Translucent equator circles representing the 3-sphere equator
        var equatorPen = new Pen(new SolidColorBrush(Color.FromArgb(25, 168, 85, 247)), 1.2,
            new DashStyle([4, 6], 0));
        context.DrawEllipse(null, equatorPen, new Point(ox, oy), r, r);
        context.DrawEllipse(null, equatorPen, new Point(ox, oy), r, r * 0.35);
    }

    private void DrawEdges(DrawingContext context, System.Collections.Generic.List<(double ScreenX, double ScreenY, double DepthW)> pts)
    {
        foreach (var edge in _engine.Edges)
        {
            if (edge.V1 >= pts.Count || edge.V2 >= pts.Count) continue;

            var p1 = pts[edge.V1];
            var p2 = pts[edge.V2];

            double avgW = (p1.DepthW + p2.DepthW) * 0.5;

            Color edgeColor;
            double thickness;

            if (ShowDepthColoring)
            {
                // Map 4D coordinate W to color:
                // Positive W (interior 4D hyperspace): cyan / emerald
                // Negative W (exterior 4D shell): rose / amber
                double normW = Math.Clamp(avgW * 1.2, -1.0, 1.0);
                if (normW >= 0)
                {
                    // Cyan #06B6D4 to Emerald #10B981
                    edgeColor = Color.FromArgb(200, 6, (byte)(182 + normW * (185 - 182)), (byte)(212 - normW * (212 - 129)));
                }
                else
                {
                    // Amber #F59E0B to Rose #F43F5E
                    double t = -normW;
                    edgeColor = Color.FromArgb(190, 244, (byte)(158 - t * (158 - 63)), (byte)(11 + t * (94 - 11)));
                }
                thickness = Math.Clamp(1.2 + (avgW + 1.0) * 1.0, 0.8, 3.2);
            }
            else
            {
                edgeColor = Color.FromArgb(160, 6, 182, 212);
                thickness = 1.4;
            }

            var pen = new Pen(new SolidColorBrush(edgeColor), thickness);
            context.DrawLine(pen, new Point(p1.ScreenX, p1.ScreenY), new Point(p2.ScreenX, p2.ScreenY));
        }
    }

    private void DrawVertices(DrawingContext context, System.Collections.Generic.List<(double ScreenX, double ScreenY, double DepthW)> pts)
    {
        for (int i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            bool isHovered = (i == _hoveredVertexIndex);

            Color nodeColor = p.DepthW >= 0 ? Color.FromRgb(6, 182, 212) : Color.FromRgb(244, 63, 94);
            double radius = isHovered ? 6.5 : (p.DepthW >= 0 ? 3.5 : 2.5);

            var brush = new SolidColorBrush(nodeColor);
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), isHovered ? 2.0 : 1.0);

            context.DrawEllipse(brush, pen, new Point(p.ScreenX, p.ScreenY), radius, radius);

            if (isHovered)
            {
                context.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, nodeColor.R, nodeColor.G, nodeColor.B)),
                    null, new Point(p.ScreenX, p.ScreenY), radius + 5, radius + 5);
            }
        }
    }

    private void DrawHoverHUD(DrawingContext context, Point hoverPt, int vIdx, Vector4D baseV, double currentW, MathThemeTokens tokens)
    {
        string line1 = $"Vertex V{vIdx}:   4D Base Coordinates";
        string line2 = $"Base:        X={baseV.X:+0.00;-0.00}, Y={baseV.Y:+0.00;-0.00}, Z={baseV.Z:+0.00;-0.00}, W={baseV.W:+0.00;-0.00}";
        string line3 = $"Rotated W:   w(t) = {currentW:+0.000;-0.000} ({(currentW >= 0 ? "Hyperspace Inner" : "Hyperspace Outer")})";
        string line4 = $"Projection:  Stereographic S³ → ℝ³ (d = {_engine.ProjectionDistance:F2})";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var monoTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

        var ft1 = new FormattedText(line1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 11, tokens.TextPrimaryBrush);
        var ft2 = new FormattedText(line2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11, tokens.TextSecondaryBrush);
        var ft3 = new FormattedText(line3, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11,
            new SolidColorBrush(currentW >= 0 ? Color.FromRgb(6, 182, 212) : Color.FromRgb(244, 63, 94)));
        var ft4 = new FormattedText(line4, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11, tokens.TextPrimaryBrush);

        double hudW = Math.Max(Math.Max(ft1.Width, ft2.Width), Math.Max(ft3.Width, ft4.Width)) + 24;
        double hudH = 88;

        double hudX = hoverPt.X + 16;
        double hudY = hoverPt.Y - hudH - 8;
        if (hudX + hudW > Bounds.Width) hudX = hoverPt.X - hudW - 16;
        if (hudY < 10) hudY = hoverPt.Y + 16;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 24, 20, 39)), hudRect, 8);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(140, 168, 85, 247)), 1.2), hudRect, 8);

        context.DrawText(ft1, new Point(hudX + 12, hudY + 8));
        context.DrawText(ft2, new Point(hudX + 12, hudY + 26));
        context.DrawText(ft3, new Point(hudX + 12, hudY + 44));
        context.DrawText(ft4, new Point(hudX + 12, hudY + 62));
    }

    private void DrawStatusBadge(DrawingContext context, double w, MathThemeTokens tokens)
    {
        string badgeText = $"4D Topology: {_engine.CurrentPreset}   |   V = {_engine.BaseVertices.Count}, E = {_engine.Edges.Count}   |   Rot XW = {_engine.AngleXW * 180 / Math.PI % 360:F0}°";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var ft = new FormattedText(badgeText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            typeface, 11, new SolidColorBrush(Color.FromRgb(6, 182, 212)));

        double bw = ft.Width + 20;
        double bh = 28;
        double bx = w - bw - 16;
        double by = 16;

        var bRect = new Rect(bx, by, bw, bh);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(210, 24, 20, 39)), bRect, 6);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(120, 6, 182, 212)), 1.2), bRect, 6);
        context.DrawText(ft, new Point(bx + 10, by + 6));
    }
}
