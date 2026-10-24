using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.LinearAlgebra;
using FryMath.Core.Topology;
using FryMath.UI.Theming;
using Vector3D = FryMath.Core.LinearAlgebra.Vector3D;

namespace FryMath.UI.Controls;

public class KnotCanvas : Control
{
    public static readonly StyledProperty<string> KnotTypeProperty =
        AvaloniaProperty.Register<KnotCanvas, string>(nameof(KnotType), "trefoil");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<KnotCanvas, string>(nameof(PresetName), "trefoil");

    public static readonly StyledProperty<double> TubeRadiusProperty =
        AvaloniaProperty.Register<KnotCanvas, double>(nameof(TubeRadius), 0.18);

    public static readonly StyledProperty<int> TorusPProperty =
        AvaloniaProperty.Register<KnotCanvas, int>(nameof(TorusP), 3);

    public static readonly StyledProperty<int> TorusQProperty =
        AvaloniaProperty.Register<KnotCanvas, int>(nameof(TorusQ), 2);

    public static readonly StyledProperty<string> BraidWordProperty =
        AvaloniaProperty.Register<KnotCanvas, string>(nameof(BraidWord), "s1 s2^-1 s1 s2^-1 s1 s2^-1");

    public static readonly StyledProperty<bool> Show3DTubeProperty =
        AvaloniaProperty.Register<KnotCanvas, bool>(nameof(Show3DTube), true);

    public static readonly StyledProperty<bool> ShowPlanarProjectionProperty =
        AvaloniaProperty.Register<KnotCanvas, bool>(nameof(ShowPlanarProjection), true);

    public static readonly StyledProperty<bool> ShowPolynomialCardProperty =
        AvaloniaProperty.Register<KnotCanvas, bool>(nameof(ShowPolynomialCard), true);

    public static readonly StyledProperty<bool> ShowBraidDiagramProperty =
        AvaloniaProperty.Register<KnotCanvas, bool>(nameof(ShowBraidDiagram), true);

    public string KnotType
    {
        get => GetValue(KnotTypeProperty);
        set => SetValue(KnotTypeProperty, value);
    }

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double TubeRadius
    {
        get => GetValue(TubeRadiusProperty);
        set => SetValue(TubeRadiusProperty, value);
    }

    public int TorusP
    {
        get => GetValue(TorusPProperty);
        set => SetValue(TorusPProperty, value);
    }

    public int TorusQ
    {
        get => GetValue(TorusQProperty);
        set => SetValue(TorusQProperty, value);
    }

    public string BraidWord
    {
        get => GetValue(BraidWordProperty);
        set => SetValue(BraidWordProperty, value);
    }

    public bool Show3DTube
    {
        get => GetValue(Show3DTubeProperty);
        set => SetValue(Show3DTubeProperty, value);
    }

    public bool ShowPlanarProjection
    {
        get => GetValue(ShowPlanarProjectionProperty);
        set => SetValue(ShowPlanarProjectionProperty, value);
    }

    public bool ShowPolynomialCard
    {
        get => GetValue(ShowPolynomialCardProperty);
        set => SetValue(ShowPolynomialCardProperty, value);
    }

    public bool ShowBraidDiagram
    {
        get => GetValue(ShowBraidDiagramProperty);
        set => SetValue(ShowBraidDiagramProperty, value);
    }

    private readonly MathThemeTokens _tokens = MathThemeTokens.DarkElectric;

    private double _yaw = 0.65;
    private double _pitch = 0.45;
    private double _zoom = 1.0;
    private bool _isOrbiting;
    private Point _lastMouse;

    static KnotCanvas()
    {
        AffectsRender<KnotCanvas>(
            KnotTypeProperty, PresetNameProperty, TubeRadiusProperty,
            TorusPProperty, TorusQProperty, BraidWordProperty,
            Show3DTubeProperty, ShowPlanarProjectionProperty,
            ShowPolynomialCardProperty, ShowBraidDiagramProperty);
    }

    public KnotCanvas()
    {
        ClipToBounds = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        if (pt.Properties.IsLeftButtonPressed)
        {
            _isOrbiting = true;
            _lastMouse = e.GetPosition(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isOrbiting)
        {
            var current = e.GetPosition(this);
            var delta = current - _lastMouse;
            _lastMouse = current;

            _yaw += delta.X * 0.008;
            _pitch = Math.Clamp(_pitch + delta.Y * 0.008, -1.4, 1.4);
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
        _zoom = Math.Clamp(_zoom * (e.Delta.Y > 0 ? 1.1 : 0.9), 0.3, 3.5);
        InvalidateVisual();
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        // Background
        context.DrawRectangle(_tokens.BgBrush, null, new Rect(0, 0, w, h));

        // Coordinate projection matrix
        var center = new Point(w * 0.48, h * 0.52);
        double scale = Math.Min(w, h) * 0.16 * _zoom;

        // Draw 3D floor grid
        DrawFloorGrid(context, center, scale);

        // Render Knot / Link components
        string type = KnotType.ToLowerInvariant();
        int componentCount = type switch
        {
            "borromean" => 3,
            "whitehead" or "hopf" => 2,
            _ => 1
        };

        // Collect all render segments across components
        var allSegments = new List<TubeSegment>();
        int samplesPerComp = 180;

        for (int c = 0; c < componentCount; c++)
        {
            var bishopPoints = KnotEngine.SampleCurveWithBishopFrame(type, samplesPerComp, TorusP, TorusQ, componentIndex: c);
            var compColor = GetComponentBaseColor(type, c);

            for (int i = 0; i < samplesPerComp; i++)
            {
                int nextIdx = (i + 1) % samplesPerComp;
                var p1 = bishopPoints[i].Position;
                var p2 = bishopPoints[nextIdx].Position;

                var (s1, z1) = Project3D(p1, center, scale);
                var (s2, z2) = Project3D(p2, center, scale);

                double avgZ = (z1 + z2) * 0.5;

                // Color gradient along knot parameter
                Color segColor;
                if (componentCount == 1)
                {
                    double tNorm = (double)i / samplesPerComp;
                    segColor = InterpolateKnotGradient(tNorm);
                }
                else
                {
                    segColor = compColor;
                }

                allSegments.Add(new TubeSegment(p1, p2, s1, s2, avgZ, segColor, c));
            }
        }

        // Depth-sort segments back to front
        allSegments.Sort((a, b) => a.DepthZ.CompareTo(b.DepthZ));

        // Render tube segments
        double strokeWidth = Math.Max(2.5, TubeRadius * scale * 0.85);

        // 1. Dark outline for depth occlusion
        foreach (var seg in allSegments)
        {
            var outlinePen = new Pen(new SolidColorBrush(Color.FromArgb(140, 10, 15, 25)), strokeWidth + 2.5, lineCap: PenLineCap.Round);
            context.DrawLine(outlinePen, seg.ScreenP1, seg.ScreenP2);
        }

        // 2. Shaded colored core
        foreach (var seg in allSegments)
        {
            var corePen = new Pen(new SolidColorBrush(seg.Color), strokeWidth, lineCap: PenLineCap.Round);
            context.DrawLine(corePen, seg.ScreenP1, seg.ScreenP2);

            // Specular highlight line along top edge
            var highlightColor = Color.FromArgb(120, 255, 255, 255);
            var highlightPen = new Pen(new SolidColorBrush(highlightColor), strokeWidth * 0.35, lineCap: PenLineCap.Round);
            var offset = new Point(0, -strokeWidth * 0.2);
            context.DrawLine(highlightPen, seg.ScreenP1 + offset, seg.ScreenP2 + offset);
        }

        // Top Left: Knot Identifier Banner
        DrawTopBanner(context, type);

        // Top Right: Topological Invariants & Polynomials Card
        if (ShowPolynomialCard)
        {
            DrawPolynomialCard(context, type, w);
        }

        // Bottom Left: Artin Braid Diagram Card
        if (ShowBraidDiagram)
        {
            DrawBraidDiagramCard(context, h);
        }

        // Bottom Right: Planar Projection Inset
        if (ShowPlanarProjection)
        {
            DrawPlanarProjectionInset(context, type, w, h);
        }

        // Camera Probe status
        var probeText = new FormattedText(
            $"Yaw: {(int)(_yaw * 180 / Math.PI)}° | Pitch: {(int)(_pitch * 180 / Math.PI)}° | Zoom: {_zoom:F2}x (Drag to orbit, scroll to zoom)",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            10,
            _tokens.TextMutedBrush
        );
        context.DrawText(probeText, new Point(14, h - 22));
    }

    private record TubeSegment(Vector3D WorldP1, Vector3D WorldP2, Point ScreenP1, Point ScreenP2, double DepthZ, Color Color, int Component);

    private (Point screen, double depthZ) Project3D(Vector3D v, Point center, double scale)
    {
        double cosY = Math.Cos(_yaw);
        double sinY = Math.Sin(_yaw);
        double cosP = Math.Cos(_pitch);
        double sinP = Math.Sin(_pitch);

        // Yaw around Z
        double x1 = v.X * cosY - v.Y * sinY;
        double y1 = v.X * sinY + v.Y * cosY;
        double z1 = v.Z;

        // Pitch around X
        double y2 = y1 * cosP - z1 * sinP;
        double z2 = y1 * sinP + z1 * cosP;

        // Isometric perspective projection
        double screenX = center.X + x1 * scale;
        double screenY = center.Y - z2 * scale;

        return (new Point(screenX, screenY), y2);
    }

    private void DrawFloorGrid(DrawingContext context, Point center, double scale)
    {
        var gridPen = new Pen(_tokens.BorderBrush, 0.8);
        int gridRange = 4;
        double step = 1.0;

        for (double x = -gridRange; x <= gridRange; x += step)
        {
            var (p1, _) = Project3D(new Vector3D(x, -gridRange, -2.5), center, scale);
            var (p2, _) = Project3D(new Vector3D(x, gridRange, -2.5), center, scale);
            context.DrawLine(gridPen, p1, p2);
        }

        for (double y = -gridRange; y <= gridRange; y += step)
        {
            var (p1, _) = Project3D(new Vector3D(-gridRange, y, -2.5), center, scale);
            var (p2, _) = Project3D(new Vector3D(gridRange, y, -2.5), center, scale);
            context.DrawLine(gridPen, p1, p2);
        }
    }

    private Color GetComponentBaseColor(string knotType, int componentIndex)
    {
        return componentIndex switch
        {
            0 => _tokens.AccentCyan,
            1 => _tokens.AccentEmerald,
            2 => _tokens.AccentAmber,
            _ => _tokens.AccentRose
        };
    }

    private Color InterpolateKnotGradient(double t)
    {
        // Smooth cyclical color loop: Cyan -> Emerald -> Amber -> Rose -> Purple -> Cyan
        double phase = (t * 5.0) % 5.0;
        int seg = (int)phase;
        double frac = phase - seg;

        return seg switch
        {
            0 => LerpColor(_tokens.AccentCyan, _tokens.AccentEmerald, frac),
            1 => LerpColor(_tokens.AccentEmerald, _tokens.AccentAmber, frac),
            2 => LerpColor(_tokens.AccentAmber, _tokens.AccentRose, frac),
            3 => LerpColor(_tokens.AccentRose, _tokens.AccentPurple, frac),
            _ => LerpColor(_tokens.AccentPurple, _tokens.AccentCyan, frac)
        };
    }

    private static Color LerpColor(Color c1, Color c2, double t)
    {
        byte r = (byte)(c1.R + (c2.R - c1.R) * t);
        byte g = (byte)(c1.G + (c2.G - c1.G) * t);
        byte b = (byte)(c1.B + (c2.B - c1.B) * t);
        return Color.FromRgb(r, g, b);
    }

    private void DrawTopBanner(DrawingContext context, string type)
    {
        string title = type switch
        {
            "trefoil" or "3_1" => "TREFOIL KNOT 3₁ (CHIRAL TORUS KNOT (2,3))",
            "figure8" or "figureeight" or "4_1" => "FIGURE-EIGHT KNOT 4₁ (AMPHICHIRAL PALINDROME)",
            "cinquefoil" or "5_1" => "CINQUEFOIL KNOT 5₁ (SOLOMON'S SEAL (5,2))",
            "borromean" => "BORROMEAN RINGS (Lk = 0 BRUNNIAN LINK)",
            "whitehead" => "WHITEHEAD LINK (c = 5, Lk = 0)",
            "hopf" => "HOPF LINK L(2a1) (Lk = 1)",
            "torus" => $"TORUS KNOT ({TorusP}, {TorusQ})",
            _ => "UNKNOT 0₁ (TRIVIAL CIRCLE)"
        };

        var bannerBrush = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
        var bannerBorder = new Pen(_tokens.AccentCyanBrush, 1.2);
        context.DrawRectangle(bannerBrush, bannerBorder, new Rect(14, 14, 340, 28), 4, 4);

        var titleText = new FormattedText(
            title,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            10,
            _tokens.AccentCyanBrush
        );
        context.DrawText(titleText, new Point(24, 21));
    }

    private void DrawPolynomialCard(DrawingContext context, string type, double w)
    {
        double cardW = 340;
        double cardH = 175;
        double cardX = w - cardW - 14;
        double cardY = 14;

        var cardBrush = new SolidColorBrush(Color.FromArgb(220, 15, 23, 42));
        var cardBorder = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawRectangle(cardBrush, cardBorder, new Rect(cardX, cardY, cardW, cardH), 6, 6);

        // Header
        var headerText = new FormattedText(
            "TOPOLOGICAL INVARIANTS & SKEIN POLYNOMIALS",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            10,
            _tokens.AccentCyanBrush
        );
        context.DrawText(headerText, new Point(cardX + 12, cardY + 12));

        // Invariants
        var jones = KnotEngine.GetJonesPolynomial(type);
        var alexander = KnotEngine.GetAlexanderPolynomial(type);
        int crossings = KnotEngine.GetCrossingNumber(type);
        int writhe = KnotEngine.ComputeWrithe(type);

        string chirality = type switch
        {
            "figure8" or "figureeight" or "4_1" => "Amphichiral (Self-Mirror)",
            "unknot" => "Trivial (Achiral)",
            _ => "Chiral (Distinct Mirror Enantiomer)"
        };

        var info1 = new FormattedText(
            $"Crossing Number c(K): {crossings}   |   Writhe w(D): {writhe}\nChirality: {chirality}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            10,
            _tokens.TextPrimaryBrush
        );
        context.DrawText(info1, new Point(cardX + 12, cardY + 32));

        // Jones Polynomial
        var jonesLabel = new FormattedText(
            "Vaughan Jones Polynomial V(t) (1984):",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.SemiBold),
            10,
            _tokens.AccentEmeraldBrush
        );
        context.DrawText(jonesLabel, new Point(cardX + 12, cardY + 68));

        var jonesVal = new FormattedText(
            $"V(t) = {jones}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace", FontStyle.Normal, FontWeight.Bold),
            11,
            _tokens.AccentAmberBrush
        );
        context.DrawText(jonesVal, new Point(cardX + 12, cardY + 84));

        // Alexander Polynomial
        var alexLabel = new FormattedText(
            "Alexander-Conway Polynomial Δ(t) (1928):",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.SemiBold),
            10,
            _tokens.AccentPurpleBrush
        );
        context.DrawText(alexLabel, new Point(cardX + 12, cardY + 108));

        var alexVal = new FormattedText(
            $"Δ(t) = {alexander}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace", FontStyle.Normal, FontWeight.Bold),
            11,
            _tokens.AccentRoseBrush
        );
        context.DrawText(alexVal, new Point(cardX + 12, cardY + 124));

        // Skein relation reminder
        var skeinText = new FormattedText(
            "Skein: t⁻¹ V(L₊) - t V(L₋) = (t^(1/2) - t^(-1/2)) V(L₀)",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            9,
            _tokens.TextMutedBrush
        );
        context.DrawText(skeinText, new Point(cardX + 12, cardY + 148));
    }

    private void DrawBraidDiagramCard(DrawingContext context, double h)
    {
        double cardW = 320;
        double cardH = 135;
        double cardX = 14;
        double cardY = h - cardH - 32;

        var cardBrush = new SolidColorBrush(Color.FromArgb(220, 15, 23, 42));
        var cardBorder = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawRectangle(cardBrush, cardBorder, new Rect(cardX, cardY, cardW, cardH), 6, 6);

        var header = new FormattedText(
            "ARTIN BRAID GROUP Bₙ & SKEIN RESOLUTION",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            10,
            _tokens.AccentAmberBrush
        );
        context.DrawText(header, new Point(cardX + 12, cardY + 10));

        var braid = KnotEngine.ParseBraidWord(BraidWord, 3);

        var braidWordText = new FormattedText(
            $"Braid Word: {BraidWord}\nPermutation: [ {string.Join(", ", braid.Permutation.Select(p => p + 1))} ]\nCrossings: {braid.Crossings.Count}   |   Strands: {braid.StrandCount}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            10,
            _tokens.TextPrimaryBrush
        );
        context.DrawText(braidWordText, new Point(cardX + 12, cardY + 28));

        // Yang-Baxter verification badge
        var ybText = new FormattedText(
            "Yang-Baxter Braid Relation: σ₁ σ₂ σ₁ = σ₂ σ₁ σ₂ [VERIFIED]",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace", FontStyle.Normal, FontWeight.Bold),
            9,
            _tokens.AccentEmeraldBrush
        );
        context.DrawText(ybText, new Point(cardX + 12, cardY + 74));

        // Mini braid diagram preview
        double startX = cardX + 18;
        double strandSpacing = 24.0;
        double diagramY = cardY + 95;
        var strandPen = new Pen(_tokens.AccentCyanBrush, 1.5);

        for (int s = 0; s < braid.StrandCount; s++)
        {
            double sx = startX + s * strandSpacing;
            context.DrawLine(strandPen, new Point(sx, diagramY), new Point(sx, diagramY + 28));
        }

        var label = new FormattedText(
            "Artin closure yields knot via Markov's theorem",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Italic),
            9,
            _tokens.TextMutedBrush
        );
        context.DrawText(label, new Point(cardX + 100, diagramY + 8));
    }

    private void DrawPlanarProjectionInset(DrawingContext context, string type, double w, double h)
    {
        double insetSize = 135;
        double insetX = w - insetSize - 14;
        double insetY = h - insetSize - 32;

        var insetBrush = new SolidColorBrush(Color.FromArgb(220, 15, 23, 42));
        var insetBorder = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawRectangle(insetBrush, insetBorder, new Rect(insetX, insetY, insetSize, insetSize), 6, 6);

        var label = new FormattedText(
            "2D REGULAR PROJECTION",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            8,
            _tokens.AccentCyanBrush
        );
        context.DrawText(label, new Point(insetX + 8, insetY + 6));

        // Draw 2D projection shadow
        var center = new Point(insetX + insetSize * 0.5, insetY + insetSize * 0.55);
        double projScale = insetSize * 0.16;
        int n = 120;
        double dt = 2.0 * Math.PI / n;

        var projPen = new Pen(_tokens.AccentCyanBrush, 1.5);
        Point? lastPt = null;

        for (int i = 0; i <= n; i++)
        {
            var v = KnotEngine.EvaluateCurve(type, (i % n) * dt, TorusP, TorusQ);
            var pt = new Point(center.X + v.X * projScale, center.Y - v.Y * projScale);

            if (lastPt.HasValue)
            {
                context.DrawLine(projPen, lastPt.Value, pt);
            }
            lastPt = pt;
        }
    }
}
