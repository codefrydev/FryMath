using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Lie;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class LieAlgebraCanvas : Control
{
    public static readonly StyledProperty<string> LieTypeProperty =
        AvaloniaProperty.Register<LieAlgebraCanvas, string>(nameof(LieType), "A2");

    public static readonly StyledProperty<bool> ShowDynkinDiagramProperty =
        AvaloniaProperty.Register<LieAlgebraCanvas, bool>(nameof(ShowDynkinDiagram), true);

    public static readonly StyledProperty<bool> ShowCartanMatrixProperty =
        AvaloniaProperty.Register<LieAlgebraCanvas, bool>(nameof(ShowCartanMatrix), true);

    public static readonly StyledProperty<bool> ShowCoxeterCirclesProperty =
        AvaloniaProperty.Register<LieAlgebraCanvas, bool>(nameof(ShowCoxeterCircles), true);

    public static readonly StyledProperty<bool> ShowRootLabelsProperty =
        AvaloniaProperty.Register<LieAlgebraCanvas, bool>(nameof(ShowRootLabels), true);

    public static readonly StyledProperty<double> CameraYawProperty =
        AvaloniaProperty.Register<LieAlgebraCanvas, double>(nameof(CameraYaw), 0.6);

    public static readonly StyledProperty<double> CameraPitchProperty =
        AvaloniaProperty.Register<LieAlgebraCanvas, double>(nameof(CameraPitch), 0.4);

    public string LieType
    {
        get => GetValue(LieTypeProperty);
        set => SetValue(LieTypeProperty, value);
    }

    public bool ShowDynkinDiagram
    {
        get => GetValue(ShowDynkinDiagramProperty);
        set => SetValue(ShowDynkinDiagramProperty, value);
    }

    public bool ShowCartanMatrix
    {
        get => GetValue(ShowCartanMatrixProperty);
        set => SetValue(ShowCartanMatrixProperty, value);
    }

    public bool ShowCoxeterCircles
    {
        get => GetValue(ShowCoxeterCirclesProperty);
        set => SetValue(ShowCoxeterCirclesProperty, value);
    }

    public bool ShowRootLabels
    {
        get => GetValue(ShowRootLabelsProperty);
        set => SetValue(ShowRootLabelsProperty, value);
    }

    public double CameraYaw
    {
        get => GetValue(CameraYawProperty);
        set => SetValue(CameraYawProperty, value);
    }

    public double CameraPitch
    {
        get => GetValue(CameraPitchProperty);
        set => SetValue(CameraPitchProperty, value);
    }

    private LieAlgebraInfo _info = LieAlgebraEngine.BuildLieAlgebra(Core.Lie.LieType.A2);
    private string _cachedLieType = "A2";
    private Point? _lastMousePos;
    private bool _isDragging;

    static LieAlgebraCanvas()
    {
        AffectsRender<LieAlgebraCanvas>(
            LieTypeProperty,
            ShowDynkinDiagramProperty,
            ShowCartanMatrixProperty,
            ShowCoxeterCirclesProperty,
            ShowRootLabelsProperty,
            CameraYawProperty,
            CameraPitchProperty);
    }

    public LieAlgebraCanvas()
    {
        ClipToBounds = true;
        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _lastMousePos = e.GetPosition(this);
        _isDragging = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging && _lastMousePos.HasValue)
        {
            var cur = e.GetPosition(this);
            double dx = cur.X - _lastMousePos.Value.X;
            double dy = cur.Y - _lastMousePos.Value.Y;
            _lastMousePos = cur;

            CameraYaw += dx * 0.01;
            CameraPitch = Math.Clamp(CameraPitch + dy * 0.01, -1.4, 1.4);
            InvalidateVisual();
        }
    }

    private void EnsureLieAlgebra()
    {
        if (_cachedLieType != LieType)
        {
            var t = LieType.ToUpperInvariant() switch
            {
                "B2" or "SO5" => Core.Lie.LieType.B2,
                "G2" => Core.Lie.LieType.G2,
                "A3" or "SU4" => Core.Lie.LieType.A3,
                "B3" or "SO7" => Core.Lie.LieType.B3,
                "E8" => Core.Lie.LieType.E8,
                _ => Core.Lie.LieType.A2
            };
            _info = LieAlgebraEngine.BuildLieAlgebra(t);
            _cachedLieType = LieType;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        EnsureLieAlgebra();

        var tokens = MathThemeTokens.DarkElectric;
        context.FillRectangle(tokens.SurfaceBrush, new Rect(0, 0, w, h));

        double cx = w * 0.48;
        double cy = h * 0.52;
        double radius = Math.Min(w, h) * 0.38;

        // Draw Coxeter concentric reference rings
        if (ShowCoxeterCircles)
        {
            RenderCoxeterCircles(context, cx, cy, radius, tokens);
        }

        // Draw Root System Vectors & Vertices
        RenderRoots(context, cx, cy, radius, tokens);

        // Render Dynkin Diagram Overlay in top-right
        if (ShowDynkinDiagram)
        {
            RenderDynkinDiagram(context, w - 240, 56, 220, 110, tokens);
        }

        // Render Cartan Matrix HUD in bottom-left
        if (ShowCartanMatrix)
        {
            RenderCartanMatrixHUD(context, 20, h - 140, tokens);
        }

        // Render Theory Header HUD in top-left
        RenderHeaderHUD(context, 20, 20, tokens);
    }

    private void RenderCoxeterCircles(DrawingContext context, double cx, double cy, double maxRadius, MathThemeTokens tokens)
    {
        var ringPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 60, 90, 140)), 1, DashStyle.Dash);
        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 80, 110, 160)), 1);

        // Concentric circles
        double[] ratios = _info.Type == Core.Lie.LieType.E8
            ? new[] { 0.25, 0.45, 0.65, 0.85, 1.0 }
            : (_info.Type == Core.Lie.LieType.G2 ? new[] { 1.0 / Math.Sqrt(3.0), 1.0 } : new[] { 0.5, 1.0 });

        foreach (var r in ratios)
        {
            context.DrawEllipse(null, ringPen, new Point(cx, cy), maxRadius * r, maxRadius * r);
        }

        // Crosshairs
        context.DrawLine(axisPen, new Point(cx - maxRadius * 1.1, cy), new Point(cx + maxRadius * 1.1, cy));
        context.DrawLine(axisPen, new Point(cx, cy - maxRadius * 1.1), new Point(cx, cy + maxRadius * 1.1));
    }

    private void RenderRoots(DrawingContext context, double cx, double cy, double radius, MathThemeTokens tokens)
    {
        bool is3D = (_info.Type == Core.Lie.LieType.A3 || _info.Type == Core.Lie.LieType.B3);
        double maxCoord = _info.Roots.Max(r => Math.Sqrt(r.X2D * r.X2D + r.Y2D * r.Y2D + (is3D ? r.Z3D * r.Z3D : 0)));
        if (maxCoord < 1e-4) maxCoord = 1.0;
        double scale = radius / maxCoord;

        // 3D rotation matrix
        double cosYaw = Math.Cos(CameraYaw);
        double sinYaw = Math.Sin(CameraYaw);
        double cosPitch = Math.Cos(CameraPitch);
        double sinPitch = Math.Sin(CameraPitch);

        // Pens
        var rootPosPen = new Pen(tokens.AccentCyanBrush, 1.5);
        var rootNegPen = new Pen(tokens.AccentPurpleBrush, 1.2);
        var simplePen = new Pen(tokens.AccentRoseBrush, 2.5);
        var simpleBrush = tokens.AccentRoseBrush;
        var posBrush = tokens.AccentCyanBrush;
        var negBrush = tokens.AccentPurpleBrush;

        // Sort roots by projected Z for 3D depth
        var projectedRoots = _info.Roots.Select(r =>
        {
            double px, py, pz;
            if (is3D)
            {
                // Rotate around Y (yaw)
                double x1 = r.X2D * cosYaw + r.Z3D * sinYaw;
                double z1 = -r.X2D * sinYaw + r.Z3D * cosYaw;
                // Rotate around X (pitch)
                double y1 = r.Y2D * cosPitch - z1 * sinPitch;
                double z2 = r.Y2D * sinPitch + z1 * cosPitch;
                px = x1;
                py = y1;
                pz = z2;
            }
            else
            {
                px = r.X2D;
                py = r.Y2D;
                pz = 0;
            }
            return new { Root = r, SX = cx + px * scale, SY = cy - py * scale, Depth = pz };
        }).OrderBy(p => p.Depth).ToList();

        // Connect nearby roots with lattice lines
        var edgePen = new Pen(new SolidColorBrush(Color.FromArgb(35, 100, 180, 255)), 0.8);
        for (int i = 0; i < projectedRoots.Count; i++)
        {
            for (int j = i + 1; j < projectedRoots.Count; j++)
            {
                double d2 = Math.Pow(projectedRoots[i].Root.X2D - projectedRoots[j].Root.X2D, 2) +
                            Math.Pow(projectedRoots[i].Root.Y2D - projectedRoots[j].Root.Y2D, 2) +
                            Math.Pow(projectedRoots[i].Root.Z3D - projectedRoots[j].Root.Z3D, 2);

                if (_info.Type == Core.Lie.LieType.E8 ? (d2 < 2.05 && d2 > 1.95) : (d2 < 2.1 && d2 > 0.9))
                {
                    context.DrawLine(edgePen, new Point(projectedRoots[i].SX, projectedRoots[i].SY),
                                              new Point(projectedRoots[j].SX, projectedRoots[j].SY));
                }
            }
        }

        // Draw vectors from origin and root nodes
        foreach (var p in projectedRoots)
        {
            var r = p.Root;
            var pen = r.IsSimple ? simplePen : (r.IsPositive ? rootPosPen : rootNegPen);
            var brush = r.IsSimple ? simpleBrush : (r.IsPositive ? posBrush : negBrush);

            // Vector ray from origin
            context.DrawLine(pen, new Point(cx, cy), new Point(p.SX, p.SY));

            // Node sphere
            double nodeR = r.IsSimple ? 6.0 : (r.IsPositive ? 4.0 : 3.2);
            context.DrawEllipse(brush, null, new Point(p.SX, p.SY), nodeR, nodeR);

            // Labels
            if (ShowRootLabels && (r.IsSimple || _info.Type == Core.Lie.LieType.A2 || _info.Type == Core.Lie.LieType.B2 || _info.Type == Core.Lie.LieType.G2))
            {
                string txt = r.IsSimple ? $"α_{r.Id}" : (r.IsPositive ? $"+r_{r.Id}" : $"-r_{r.Id}");
                var ft = new FormattedText(
                    txt,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(FontFamily.Default, FontStyle.Normal, r.IsSimple ? FontWeight.Bold : FontWeight.Normal),
                    r.IsSimple ? 11 : 9,
                    r.IsSimple ? tokens.AccentRoseBrush : tokens.TextMutedBrush);

                context.DrawText(ft, new Point(p.SX + 7, p.SY - 7));
            }
        }
    }

    private void RenderDynkinDiagram(DrawingContext context, double x, double y, double w, double h, MathThemeTokens tokens)
    {
        // Container background
        var bg = new SolidColorBrush(Color.FromArgb(200, 16, 20, 32));
        var borderPen = new Pen(tokens.BorderBrush, 1);
        context.DrawRectangle(bg, borderPen, new Rect(x, y, w, h), 8, 8);

        var titleText = new FormattedText(
            "DYNKIN DIAGRAM",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            10,
            tokens.TextMutedBrush);
        context.DrawText(titleText, new Point(x + 12, y + 8));

        int n = _info.DynkinNodes.Count;
        if (n == 0) return;

        double startX = x + 24;
        double spacingX = (w - 48) / Math.Max(1, n - (n == 8 ? 2 : 1));
        double nodeY = y + 54;

        Point[] nodePositions = new Point[n];
        for (int i = 0; i < n; i++)
        {
            if (_info.Type == Core.Lie.LieType.E8 && i == 7)
            {
                // Branch node 8 attaches above node 3
                nodePositions[i] = new Point(nodePositions[2].X, nodeY - 26);
            }
            else
            {
                nodePositions[i] = new Point(startX + i * spacingX, nodeY);
            }
        }

        // Draw Edges
        var edgePen = new Pen(tokens.AccentCyanBrush, 2.0);
        foreach (var e in _info.DynkinEdges)
        {
            var p1 = nodePositions[e.Node1];
            var p2 = nodePositions[e.Node2];

            if (e.Multiplicity == 1)
            {
                context.DrawLine(edgePen, p1, p2);
            }
            else if (e.Multiplicity == 2)
            {
                context.DrawLine(edgePen, new Point(p1.X, p1.Y - 2.5), new Point(p2.X, p2.Y - 2.5));
                context.DrawLine(edgePen, new Point(p1.X, p1.Y + 2.5), new Point(p2.X, p2.Y + 2.5));
            }
            else if (e.Multiplicity == 3)
            {
                context.DrawLine(edgePen, new Point(p1.X, p1.Y - 4), new Point(p2.X, p2.Y - 4));
                context.DrawLine(edgePen, p1, p2);
                context.DrawLine(edgePen, new Point(p1.X, p1.Y + 4), new Point(p2.X, p2.Y + 4));
            }

            // Arrow direction for non-simply laced algebras (B2, G2, B3)
            if (e.ArrowDirection != 0)
            {
                double midX = (p1.X + p2.X) * 0.5;
                double midY = (p1.Y + p2.Y) * 0.5;
                double dir = (p2.X > p1.X ? 1.0 : -1.0) * (e.ArrowDirection > 0 ? 1.0 : -1.0);
                context.DrawLine(new Pen(tokens.AccentAmberBrush, 2), new Point(midX - 5 * dir, midY - 4), new Point(midX + 2 * dir, midY));
                context.DrawLine(new Pen(tokens.AccentAmberBrush, 2), new Point(midX - 5 * dir, midY + 4), new Point(midX + 2 * dir, midY));
            }
        }

        // Draw Nodes
        for (int i = 0; i < n; i++)
        {
            var pt = nodePositions[i];
            context.DrawEllipse(tokens.AccentCyanBrush, new Pen(tokens.SurfaceBrush, 2), pt, 6, 6);

            var lbl = new FormattedText(
                _info.DynkinNodes[i].Label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
                9,
                tokens.TextSecondaryBrush);
            context.DrawText(lbl, new Point(pt.X - 6, pt.Y + 9));
        }
    }

    private void RenderCartanMatrixHUD(DrawingContext context, double x, double y, MathThemeTokens tokens)
    {
        var bg = new SolidColorBrush(Color.FromArgb(200, 16, 20, 32));
        var borderPen = new Pen(tokens.BorderBrush, 1);
        int r = _info.Rank;
        double w = Math.Max(160, r * 28 + 36);
        double h = r * 18 + 40;

        context.DrawRectangle(bg, borderPen, new Rect(x, y - (h - 90), w, h), 8, 8);

        var titleText = new FormattedText(
            $"CARTAN MATRIX A_{{ij}} (Rank {r})",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            10,
            tokens.TextMutedBrush);
        context.DrawText(titleText, new Point(x + 12, y - (h - 90) + 8));

        for (int i = 0; i < r; i++)
        {
            for (int j = 0; j < r; j++)
            {
                int val = _info.CartanMatrix[i, j];
                var brush = val == 2 ? tokens.AccentEmeraldBrush : (val < 0 ? tokens.AccentRoseBrush : tokens.TextMutedBrush);
                var num = new FormattedText(
                    val.ToString("+0;-0;0"),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas, Menlo, monospace", FontStyle.Normal, FontWeight.Bold),
                    10,
                    brush);
                context.DrawText(num, new Point(x + 16 + j * 26, y - (h - 90) + 26 + i * 16));
            }
        }
    }

    private void RenderHeaderHUD(DrawingContext context, double x, double y, MathThemeTokens tokens)
    {
        var symbolText = new FormattedText(
            _info.Symbol,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
            16,
            tokens.AccentCyanBrush);
        context.DrawText(symbolText, new Point(x, y));

        string stats = $"dim 𝔤 = {_info.Dimension}  |  Rank = {_info.Rank}  |  Roots |Φ| = {_info.TotalRoots} ({_info.PositiveRoots} pos)  |  Coxeter h = {_info.CoxeterNumber}";
        var statsText = new FormattedText(
            stats,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold),
            11,
            tokens.TextSecondaryBrush);
        context.DrawText(statsText, new Point(x, y + 22));

        string details = _info.Type switch
        {
            Core.Lie.LieType.A2 => "Hexagonal A₂ root lattice. Symmetries of quarks in Gell-Mann's Eightfold Way.",
            Core.Lie.LieType.B2 => "Octagonal B₂ root lattice with short roots (length 1) and long roots (length √2).",
            Core.Lie.LieType.G2 => "Smallest exceptional Lie algebra, preserving the octonionic cross-product on Im(𝕆).",
            Core.Lie.LieType.A3 => "Rotatable 3D root polytope: cuboctahedron with 12 vertices. Drag mouse to rotate.",
            Core.Lie.LieType.B3 => "Rotatable 3D root polytope: rhombic dodecahedron with 18 vertices. Drag mouse to rotate.",
            Core.Lie.LieType.E8 => "240 roots of E₈ projected onto the 2D Coxeter plane, forming 8 concentric 30-gons.",
            _ => string.Empty
        };

        var detailsText = new FormattedText(
            details,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal),
            10,
            tokens.TextMutedBrush);
        context.DrawText(detailsText, new Point(x, y + 40));
    }
}
