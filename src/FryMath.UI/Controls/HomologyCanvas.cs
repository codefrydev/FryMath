using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Topology;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class HomologyCanvas : Control
{
    public static readonly StyledProperty<string> ComplexTypeProperty =
        AvaloniaProperty.Register<HomologyCanvas, string>(nameof(ComplexType), "torus");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<HomologyCanvas, string>(nameof(PresetName), "torus");

    public static readonly StyledProperty<double> FiltrationRadiusProperty =
        AvaloniaProperty.Register<HomologyCanvas, double>(nameof(FiltrationRadius), 0.8);

    public static readonly StyledProperty<bool> ShowGeneratorCyclesProperty =
        AvaloniaProperty.Register<HomologyCanvas, bool>(nameof(ShowGeneratorCycles), true);

    public static readonly StyledProperty<bool> ShowBoundaryMatrixProperty =
        AvaloniaProperty.Register<HomologyCanvas, bool>(nameof(ShowBoundaryMatrix), true);

    public static readonly StyledProperty<bool> ShowPersistenceBarcodesProperty =
        AvaloniaProperty.Register<HomologyCanvas, bool>(nameof(ShowPersistenceBarcodes), true);

    public static readonly StyledProperty<bool> ShowVertexLabelsProperty =
        AvaloniaProperty.Register<HomologyCanvas, bool>(nameof(ShowVertexLabels), true);

    public string ComplexType
    {
        get => GetValue(ComplexTypeProperty);
        set => SetValue(ComplexTypeProperty, value);
    }

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double FiltrationRadius
    {
        get => GetValue(FiltrationRadiusProperty);
        set => SetValue(FiltrationRadiusProperty, value);
    }

    public bool ShowGeneratorCycles
    {
        get => GetValue(ShowGeneratorCyclesProperty);
        set => SetValue(ShowGeneratorCyclesProperty, value);
    }

    public bool ShowBoundaryMatrix
    {
        get => GetValue(ShowBoundaryMatrixProperty);
        set => SetValue(ShowBoundaryMatrixProperty, value);
    }

    public bool ShowPersistenceBarcodes
    {
        get => GetValue(ShowPersistenceBarcodesProperty);
        set => SetValue(ShowPersistenceBarcodesProperty, value);
    }

    public bool ShowVertexLabels
    {
        get => GetValue(ShowVertexLabelsProperty);
        set => SetValue(ShowVertexLabelsProperty, value);
    }

    private readonly MathThemeTokens _tokens = MathThemeTokens.DarkElectric;

    private double _rotX = 0.5;
    private double _rotY = -0.6;
    private Point? _lastMousePos;

    static HomologyCanvas()
    {
        AffectsRender<HomologyCanvas>(
            ComplexTypeProperty,
            PresetNameProperty,
            FiltrationRadiusProperty,
            ShowGeneratorCyclesProperty,
            ShowBoundaryMatrixProperty,
            ShowPersistenceBarcodesProperty,
            ShowVertexLabelsProperty
        );
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        if (pt.Properties.IsLeftButtonPressed)
        {
            _lastMousePos = pt.Position;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);
        if (pt.Properties.IsLeftButtonPressed && _lastMousePos.HasValue)
        {
            var delta = pt.Position - _lastMousePos.Value;
            _rotY += delta.X * 0.008;
            _rotX -= delta.Y * 0.008;
            _lastMousePos = pt.Position;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _lastMousePos = null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        context.DrawRectangle(_tokens.BgBrush, null, new Rect(0, 0, w, h));

        string cType = ComplexType.ToLowerInvariant();
        bool isPersistence = cType.Equals("persistence", StringComparison.OrdinalIgnoreCase);

        // Split Layout: Left Panel = 3D Simplicial Complex / Point Cloud, Right Panel = Boundary Matrix / Persistence Barcode
        double panelW = (w - 28) * 0.5;
        double panelH = h - 60;
        var leftRect = new Rect(14, 45, panelW, panelH);
        var rightRect = new Rect(14 + panelW + 10, 45, panelW, panelH);

        var panelBg = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42));
        var panelBorder = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawRectangle(panelBg, panelBorder, leftRect, 6, 6);
        context.DrawRectangle(panelBg, panelBorder, rightRect, 6, 6);

        if (isPersistence)
        {
            RenderPersistenceLab(context, leftRect, rightRect);
        }
        else
        {
            RenderSimplicialHomologyLab(context, leftRect, rightRect, cType);
        }

        // Top Banner
        DrawTopBanner(context, cType, isPersistence);

        // Bottom Probe status
        var probeText = new FormattedText(
            $"Drag to Rotate 3D Complex  |  Orientation θ={_rotY * 180 / Math.PI:F0}° φ={_rotX * 180 / Math.PI:F0}°  |  Euler-Poincaré Index χ = β₀ - β₁ + β₂  |  ∂₁ ∘ ∂₂ ≡ 0 [Exact]",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            10,
            _tokens.TextMutedBrush
        );
        context.DrawText(probeText, new Point(14, h - 18));
    }

    private void DrawTopBanner(DrawingContext context, string cType, bool isPersistence)
    {
        string title = isPersistence
            ? "VIETORIS-RIPS POINT CLOUD FILTRATION & PERSISTENT BETTI BARCODES (TDA)"
            : cType switch
            {
                "sphere" => "2-SPHERE S² OCTAHEDRON TRIANGULATION [β = (1, 0, 1), χ = 2, H₁(S²) = 0]",
                "klein" => "KLEIN BOTTLE K² NON-ORIENTABLE SURFACE [Over ℚ: β = (1, 1, 0), H₁(K; ℤ) ≅ ℤ ⊕ ℤ₂]",
                "projective" or "rp2" => "REAL PROJECTIVE PLANE ℝP² MINIMAL COMPLEX [Over ℚ: β = (1, 0, 0), H₁(ℝP²; ℤ) ≅ ℤ₂]",
                "mobius" => "MÖBIUS STRIP 2D MANIFOLD WITH BOUNDARY [β = (1, 1, 0), ∂M ≅ S¹, χ = 0]",
                _ => "2-TORUS T² SIMPLICIAL CHAIN COMPLEX [β = (1, 2, 1), χ = 0, H₁(T²) ≅ ℤ ⊕ ℤ]"
            };

        var bannerBrush = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
        var bannerBorder = new Pen(_tokens.AccentCyanBrush, 1.2);
        context.DrawRectangle(bannerBrush, bannerBorder, new Rect(14, 10, 520, 26), 4, 4);

        var titleText = new FormattedText(
            title,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            10,
            _tokens.AccentCyanBrush
        );
        context.DrawText(titleText, new Point(22, 16));
    }

    // -----------------------------------------------------------------
    // 1. Simplicial Complex & Exact Boundary Matrices (Torus, Sphere, etc.)
    // -----------------------------------------------------------------
    private void RenderSimplicialHomologyLab(DrawingContext context, Rect leftRect, Rect rightRect, string cType)
    {
        var mesh = cType switch
        {
            "sphere" => SimplicialHomologyEngine.CreateSphere(),
            "klein" => SimplicialHomologyEngine.CreateKleinBottle(),
            "projective" or "rp2" => SimplicialHomologyEngine.CreateProjectivePlane(),
            "mobius" => SimplicialHomologyEngine.CreateMobiusStrip(),
            _ => SimplicialHomologyEngine.CreateTorus()
        };

        var homology = mesh.ComputeHomology();

        // Left Panel: 3D Render of Simplicial Complex
        DrawPanelHeader(context, leftRect, $"3D SIMPLICIAL COMPLEX: {mesh.Name.ToUpperInvariant()}", _tokens.AccentCyanBrush);
        Render3DMesh(context, leftRect, mesh, homology);

        // Right Panel: Boundary Operator Matrices ∂_1 and ∂_2 & Algebraic Decomposition
        DrawPanelHeader(context, rightRect, "BOUNDARY OPERATOR MATRICES ∂₁ : C₁ → C₀  AND  ∂₂ : C₂ → C₁", _tokens.AccentAmberBrush);
        RenderBoundaryMatrixInspector(context, rightRect, mesh, homology);

        // Inset Summary Card on Left
        DrawCard(context, leftRect.X + 10, leftRect.Bottom - 110, 310, 100,
            "HOMOLOGY & EULER-POINCARÉ INVARIANTS",
            $"Simplicial: V = {homology.VertexCount}, E = {homology.EdgeCount}, F = {homology.TriangleCount}\n" +
            $"Betti Numbers: β₀ = {homology.Betti0}, β₁ = {homology.Betti1}, β₂ = {homology.Betti2}\n" +
            $"Euler χ: V - E + F = {homology.EulerSimplicial}  ≡  β₀ - β₁ + β₂ = {homology.EulerHomological}\n" +
            $"Fundamental Axiom: ∂₁ ∘ ∂₂ = 0 [Rank(∂₁)={homology.RankD1}, Rank(∂₂)={homology.RankD2}]",
            _tokens.AccentEmeraldBrush);
    }

    private void Render3DMesh(DrawingContext context, Rect rect, SimplicialHomologyEngine.SimplicialMesh mesh, SimplicialHomologyEngine.HomologyResult homology)
    {
        var center = new Point(rect.X + rect.Width * 0.5, rect.Y + rect.Height * 0.52);
        double scale = Math.Min(rect.Width, rect.Height) * 0.28;

        // Project vertices to 2D screen coordinates with 3D rotation
        double cosX = Math.Cos(_rotX);
        double sinX = Math.Sin(_rotX);
        double cosY = Math.Cos(_rotY);
        double sinY = Math.Sin(_rotY);

        var projected = new List<(Point ScreenPt, double Depth)>();
        foreach (var v in mesh.Vertices)
        {
            // Rotate Y
            double x1 = v.X * cosY + v.Z * sinY;
            double z1 = -v.X * sinY + v.Z * cosY;
            // Rotate X
            double y2 = v.Y * cosX - z1 * sinX;
            double z2 = v.Y * sinX + z1 * cosX;

            var screenPt = new Point(center.X + x1 * scale, center.Y - y2 * scale);
            projected.Add((screenPt, z2));
        }

        // Depth-sort triangles (Painter's algorithm)
        var sortedTris = mesh.Triangles
            .Select((tri, idx) =>
            {
                double avgDepth = (projected[tri.V0].Depth + projected[tri.V1].Depth + projected[tri.V2].Depth) / 3.0;
                return (tri, idx, avgDepth);
            })
            .OrderBy(t => t.avgDepth)
            .ToList();

        // Render translucent triangles with lighting
        var triEdgePen = new Pen(new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)), 0.8);
        foreach (var item in sortedTris)
        {
            var p0 = projected[item.tri.V0].ScreenPt;
            var p1 = projected[item.tri.V1].ScreenPt;
            var p2 = projected[item.tri.V2].ScreenPt;

            // Simple normal-based shading
            double nx = (p1.Y - p0.Y) * (projected[item.tri.V2].Depth - projected[item.tri.V0].Depth);
            byte alpha = (byte)Math.Clamp(50 + (int)(item.avgDepth * 15), 35, 95);
            var triBrush = new SolidColorBrush(Color.FromArgb(alpha, 14, 165, 233));

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(p0, true);
                ctx.LineTo(p1);
                ctx.LineTo(p2);
                ctx.EndFigure(true);
            }
            context.DrawGeometry(triBrush, triEdgePen, geom);
        }

        // Render edges
        var edgePen = new Pen(_tokens.BorderBrush, 1.2);
        foreach (var edge in mesh.Edges)
        {
            context.DrawLine(edgePen, projected[edge.V0].ScreenPt, projected[edge.V1].ScreenPt);
        }

        // Highlight Non-Bounding 1-Cycles (Generators of H_1)
        if (ShowGeneratorCycles && homology.Independent1Cycles.Count > 0)
        {
            var cycleColors = new[] { _tokens.AccentEmeraldBrush, _tokens.AccentAmberBrush, _tokens.AccentRoseBrush };
            for (int c = 0; c < homology.Independent1Cycles.Count; c++)
            {
                var cycle = homology.Independent1Cycles[c];
                var pen = new Pen(cycleColors[c % cycleColors.Length], 2.8);

                for (int i = 0; i < cycle.Count - 1; i++)
                {
                    int u = cycle[i];
                    int v = cycle[i + 1];
                    if (u < projected.Count && v < projected.Count)
                    {
                        context.DrawLine(pen, projected[u].ScreenPt, projected[v].ScreenPt);
                    }
                }
            }
        }

        // Render vertices
        var vertBrush = new SolidColorBrush(_tokens.AccentCyan);
        var vertBorder = new Pen(_tokens.BgBrush, 1.0);
        for (int i = 0; i < projected.Count; i++)
        {
            var pt = projected[i].ScreenPt;
            context.DrawEllipse(vertBrush, vertBorder, pt, 3.5, 3.5);

            if (ShowVertexLabels)
            {
                var labelText = new FormattedText(
                    $"v{i}",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas, Menlo, monospace"),
                    9,
                    _tokens.TextPrimaryBrush
                );
                context.DrawText(labelText, new Point(pt.X + 5, pt.Y - 5));
            }
        }
    }

    private void RenderBoundaryMatrixInspector(DrawingContext context, Rect rect, SimplicialHomologyEngine.SimplicialMesh mesh, SimplicialHomologyEngine.HomologyResult homology)
    {
        double x0 = rect.X + 16;
        double y0 = rect.Y + 36;

        // Top equation overview
        var eqText = new FormattedText(
            "Chain Complex Sequence:\n" +
            "  0 ──> C₂ (Triangles) ──[ ∂₂ ]──> C₁ (Edges) ──[ ∂₁ ]──> C₀ (Vertices) ──> 0\n" +
            $"  Dim:    dim C₂ = {mesh.Triangles.Count}          dim C₁ = {mesh.Edges.Count}         dim C₀ = {mesh.Vertices.Count}\n\n" +
            "Cycle & Boundary Subspaces:\n" +
            $"  • 1-Cycles Z₁ = ker(∂₁):   dim Z₁ = |E| - rank(∂₁) = {mesh.Edges.Count} - {homology.RankD1} = {mesh.Edges.Count - homology.RankD1}\n" +
            $"  • 1-Boundaries B₁ = im(∂₂): dim B₁ = rank(∂₂) = {homology.RankD2}\n" +
            $"  • 1st Homology H₁ = Z₁ / B₁: β₁ = dim Z₁ - dim B₁ = {homology.Betti1} independent non-bounding loop(s)\n" +
            $"  • Nilpotent Composition:     ∂₁ ∘ ∂₂ ≡ 0  =>  B_k ⊆ Z_k (All boundaries are cycles!)",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            10.5,
            _tokens.TextPrimaryBrush
        );
        context.DrawText(eqText, new Point(x0, y0));

        // Draw Matrix Visualization Grid for ∂_1
        double gridY = y0 + 130;
        var gridHeader = new FormattedText(
            $"∂₁ Incidence Matrix [{mesh.Vertices.Count} vertices × {Math.Min(mesh.Edges.Count, 16)} edges preview]:",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.SemiBold),
            10,
            _tokens.AccentAmberBrush
        );
        context.DrawText(gridHeader, new Point(x0, gridY));

        double cellSize = 13.0;
        int maxE = Math.Min(mesh.Edges.Count, 16);
        int maxV = Math.Min(mesh.Vertices.Count, 9);

        for (int v = 0; v < maxV; v++)
        {
            for (int e = 0; e < maxE; e++)
            {
                var edge = mesh.Edges[e];
                int val = 0;
                if (edge.V0 == v) val = -1;
                else if (edge.V1 == v) val = 1;

                var cellRect = new Rect(x0 + e * (cellSize + 2), gridY + 18 + v * (cellSize + 2), cellSize, cellSize);
                IBrush cellBrush = val switch
                {
                    1 => new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)),
                    -1 => new SolidColorBrush(Color.FromArgb(160, 244, 63, 94)),
                    _ => new SolidColorBrush(Color.FromArgb(40, 100, 116, 139))
                };
                context.DrawRectangle(cellBrush, null, cellRect, 2, 2);

                if (val != 0)
                {
                    var valText = new FormattedText(
                        val == 1 ? "+1" : "-1",
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas, Menlo, monospace"),
                        7.5,
                        _tokens.TextPrimaryBrush
                    );
                    context.DrawText(valText, new Point(cellRect.X + 1, cellRect.Y + 2));
                }
            }
        }

        // Legend
        double legY = gridY + 18 + maxV * (cellSize + 2) + 8;
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 56, 189, 248)), null, new Rect(x0, legY, 9, 9), 1, 1);
        context.DrawText(new FormattedText("+1 (Head v1)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 8.5, _tokens.TextMutedBrush), new Point(x0 + 13, legY));

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 244, 63, 94)), null, new Rect(x0 + 95, legY, 9, 9), 1, 1);
        context.DrawText(new FormattedText("-1 (Tail v0)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 8.5, _tokens.TextMutedBrush), new Point(x0 + 108, legY));

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(40, 100, 116, 139)), null, new Rect(x0 + 185, legY, 9, 9), 1, 1);
        context.DrawText(new FormattedText("0 (Disjoint)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 8.5, _tokens.TextMutedBrush), new Point(x0 + 198, legY));

        // Inset Card for Torsion / Homology Theorem
        DrawCard(context, rect.X + 10, rect.Bottom - 85, 300, 75,
            "EMMY NOETHER HOMOLOGY ALGEBRA",
            "Homology groups are abelian groups H_k = ker ∂_k / im ∂_{k+1}.\nBetti number β_k = rank(H_k) counts k-dimensional holes.\nTorsion Tor(H_k) detects non-orientable topological twists.",
            _tokens.AccentPurpleBrush);
    }

    // -----------------------------------------------------------------
    // 2. Vietoris-Rips Point Cloud Filtration & Persistence Barcodes (TDA)
    // -----------------------------------------------------------------
    private void RenderPersistenceLab(DrawingContext context, Rect leftRect, Rect rightRect)
    {
        DrawPanelHeader(context, leftRect, "VIETORIS-RIPS SIMPLICIAL COMPLEX FILTRATION K(ε)", _tokens.AccentCyanBrush);
        DrawPanelHeader(context, rightRect, "PERSISTENCE BARCODE DIAGRAM [BIRTH, DEATH]", _tokens.AccentRoseBrush);

        double eps = FiltrationRadius;
        var centerL = new Point(leftRect.X + leftRect.Width * 0.5, leftRect.Y + leftRect.Height * 0.52);

        // Generate sample Annulus point cloud
        int numPts = 24;
        double rInner = 1.0;
        double rOuter = 1.6;
        double scaleL = Math.Min(leftRect.Width, leftRect.Height) * 0.22;

        var points = new List<Point>();
        for (int i = 0; i < numPts; i++)
        {
            double theta = i * 2.0 * Math.PI / numPts;
            double r = (i % 2 == 0) ? rInner : rOuter;
            points.Add(new Point(centerL.X + r * Math.Cos(theta) * scaleL, centerL.Y - r * Math.Sin(theta) * scaleL));
        }

        // Draw metric balls B(x, eps / 2)
        double ballR = (eps * 0.5) * scaleL;
        var ballBrush = new SolidColorBrush(Color.FromArgb(20, 56, 189, 248));
        var ballBorder = new Pen(new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)), 0.8);
        foreach (var p in points)
        {
            context.DrawEllipse(ballBrush, ballBorder, p, ballR, ballR);
        }

        // Connect edges for dist <= eps
        double distThreshold = eps * scaleL;
        var edgePen = new Pen(_tokens.AccentCyanBrush, 1.2);
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                double dx = points[i].X - points[j].X;
                double dy = points[i].Y - points[j].Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d <= distThreshold)
                {
                    context.DrawLine(edgePen, points[i], points[j]);
                }
            }
        }

        // Draw points
        var ptBrush = new SolidColorBrush(_tokens.AccentAmber);
        foreach (var p in points)
        {
            context.DrawEllipse(ptBrush, null, p, 3.0, 3.0);
        }

        // Inset Card Left
        DrawCard(context, leftRect.X + 10, leftRect.Bottom - 85, 270, 75,
            "VIETORIS-RIPS COMPLEX K_ε",
            $"Filtration Scale ε = {eps:F2}\nSimplex [v₀...v_k] ∈ K_ε iff pairwise dist ≤ ε.\nDetects persistent 1D circular hole at current scale.",
            _tokens.AccentCyanBrush);

        // Right Panel: Persistence Barcode Diagram
        var barcodes = SimplicialHomologyEngine.ComputePersistenceBarcodes("annulus", 2.0);
        RenderBarcodes(context, rightRect, barcodes, eps);
    }

    private void RenderBarcodes(DrawingContext context, Rect rect, IReadOnlyList<SimplicialHomologyEngine.BarcodeInterval> barcodes, double currentEps)
    {
        double xStart = rect.X + 45;
        double xEnd = rect.Right - 30;
        double yTop = rect.Y + 50;
        double barHeight = 14.0;
        double spacing = 10.0;
        double maxScale = 2.0;

        // Draw axis
        var axisPen = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawLine(axisPen, new Point(xStart, yTop - 15), new Point(xStart, yTop + barcodes.Count * (barHeight + spacing) + 15));
        context.DrawLine(axisPen, new Point(xStart, yTop + barcodes.Count * (barHeight + spacing) + 15), new Point(xEnd, yTop + barcodes.Count * (barHeight + spacing) + 15));

        // Axis ticks
        for (double e = 0.0; e <= maxScale; e += 0.5)
        {
            double tx = xStart + (e / maxScale) * (xEnd - xStart);
            double ty = yTop + barcodes.Count * (barHeight + spacing) + 15;
            context.DrawLine(axisPen, new Point(tx, ty), new Point(tx, ty + 4));

            var tickText = new FormattedText(
                $"{e:F1}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas, Menlo, monospace"),
                9,
                _tokens.TextMutedBrush
            );
            context.DrawText(tickText, new Point(tx - 8, ty + 6));
        }

        // Render Barcode Intervals
        for (int i = 0; i < barcodes.Count; i++)
        {
            var b = barcodes[i];
            double y = yTop + i * (barHeight + spacing);

            double bX0 = xStart + (b.Birth / maxScale) * (xEnd - xStart);
            double bX1 = xStart + (b.Death / maxScale) * (xEnd - xStart);

            IBrush barBrush = b.Dimension switch
            {
                0 => new SolidColorBrush(_tokens.AccentCyan),
                1 => new SolidColorBrush(_tokens.AccentRose),
                _ => new SolidColorBrush(_tokens.AccentEmerald)
            };

            var barRect = new Rect(bX0, y, Math.Max(2.0, bX1 - bX0), barHeight);
            context.DrawRectangle(barBrush, null, barRect, 3, 3);

            // Label
            var lblText = new FormattedText(
                $"H{b.Dimension}: {b.FeatureLabel}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas, Menlo, monospace"),
                8.5,
                _tokens.TextPrimaryBrush
            );
            context.DrawText(lblText, new Point(Math.Max(xStart + 6, bX0 + 4), y + 1));
        }

        // Scrubber line for current filtration scale eps
        double scrubberX = xStart + (currentEps / maxScale) * (xEnd - xStart);
        if (scrubberX >= xStart && scrubberX <= xEnd)
        {
            var scrubberPen = new Pen(_tokens.AccentAmberBrush, 1.8);
            context.DrawLine(scrubberPen, new Point(scrubberX, yTop - 20), new Point(scrubberX, yTop + barcodes.Count * (barHeight + spacing) + 15));

            var scrubBadge = new FormattedText(
                $"ε = {currentEps:F2}",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
                9,
                _tokens.AccentAmberBrush
            );
            context.DrawText(scrubBadge, new Point(scrubberX - 16, yTop - 34));
        }

        // Inset Card Right
        DrawCard(context, rect.X + 10, rect.Bottom - 85, 290, 75,
            "TOPOLOGICAL DATA ANALYSIS PERSISTENCE",
            "Long horizontal bars represent true topological invariants.\nShort transient bars correspond to discretization noise.\nPersistent H₁ hole born at ε = 0.40 dies when filled at ε = 1.45.",
            _tokens.AccentRoseBrush);
    }

    private void DrawPanelHeader(DrawingContext context, Rect rect, string title, IBrush brush)
    {
        var text = new FormattedText(
            title,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            9,
            brush
        );
        context.DrawText(text, new Point(rect.X + 10, rect.Y + 8));
    }

    private void DrawCard(DrawingContext context, double x, double y, double width, double height, string title, string content, IBrush accentBrush)
    {
        var bg = new SolidColorBrush(Color.FromArgb(220, 15, 23, 42));
        var border = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawRectangle(bg, border, new Rect(x, y, width, height), 6, 6);

        var titleText = new FormattedText(
            title,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            9,
            accentBrush
        );
        context.DrawText(titleText, new Point(x + 10, y + 8));

        var bodyText = new FormattedText(
            content,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            8.5,
            _tokens.TextPrimaryBrush
        );
        context.DrawText(bodyText, new Point(x + 10, y + 24));
    }
}
