using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.LinearAlgebra;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class MatrixTransformCanvas : Control
{
    public static readonly StyledProperty<Matrix2x2> MatrixProperty =
        AvaloniaProperty.Register<MatrixTransformCanvas, Matrix2x2>(nameof(Matrix), Matrix2x2.Identity, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> MorphProgressProperty =
        AvaloniaProperty.Register<MatrixTransformCanvas, double>(nameof(MorphProgress), 1.0);

    public static readonly StyledProperty<bool> ShowUnitCircleProperty =
        AvaloniaProperty.Register<MatrixTransformCanvas, bool>(nameof(ShowUnitCircle), true);

    public static readonly StyledProperty<bool> ShowTestVectorProperty =
        AvaloniaProperty.Register<MatrixTransformCanvas, bool>(nameof(ShowTestVector), true);

    public static readonly StyledProperty<Vector2D> TestVectorProperty =
        AvaloniaProperty.Register<MatrixTransformCanvas, Vector2D>(nameof(TestVector), new Vector2D(1.5, 1.0), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public Matrix2x2 Matrix
    {
        get => GetValue(MatrixProperty);
        set => SetValue(MatrixProperty, value);
    }

    public double MorphProgress
    {
        get => GetValue(MorphProgressProperty);
        set => SetValue(MorphProgressProperty, value);
    }

    public bool ShowUnitCircle
    {
        get => GetValue(ShowUnitCircleProperty);
        set => SetValue(ShowUnitCircleProperty, value);
    }

    public bool ShowTestVector
    {
        get => GetValue(ShowTestVectorProperty);
        set => SetValue(ShowTestVectorProperty, value);
    }

    public Vector2D TestVector
    {
        get => GetValue(TestVectorProperty);
        set => SetValue(TestVectorProperty, value);
    }

    public double Scale { get; set; } = 65.0; // 65 pixels per unit
    public double OriginX { get; set; } = 0.0;
    public double OriginY { get; set; } = 0.0;

    private int _draggedHandle = 0; // 0 = none, 1 = i-hat, 2 = j-hat, 3 = test vector u
    private bool _isPanning = false;
    private Point _panStartPoint;
    private double _panStartOriginX;
    private double _panStartOriginY;

    static MatrixTransformCanvas()
    {
        AffectsRender<MatrixTransformCanvas>(
            MatrixProperty,
            MorphProgressProperty,
            ShowUnitCircleProperty,
            ShowTestVectorProperty,
            TestVectorProperty);
    }

    public MatrixTransformCanvas()
    {
        ClipToBounds = true;
        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    public void ResetToIdentity()
    {
        Matrix = Matrix2x2.Identity;
        MorphProgress = 1.0;
        OriginX = Bounds.Width * 0.5;
        OriginY = Bounds.Height * 0.5;
        Scale = 65.0;
        InvalidateVisual();
    }

    private Point MathToScreen(Vector2D v)
    {
        double ox = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        double oy = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
        return new Point(ox + v.X * Scale, oy - v.Y * Scale);
    }

    private Vector2D ScreenToMath(Point screen)
    {
        double ox = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        double oy = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
        return new Vector2D((screen.X - ox) / Scale, (oy - screen.Y) / Scale);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);

        var currentM = Matrix2x2.Interpolate(Matrix2x2.Identity, Matrix, MorphProgress);
        var tipI = MathToScreen(new Vector2D(currentM.M11, currentM.M21));
        var tipJ = MathToScreen(new Vector2D(currentM.M12, currentM.M22));
        var tipU = MathToScreen(TestVector);

        if (GetDistance(pos, tipI) < 18.0)
        {
            _draggedHandle = 1;
            return;
        }
        if (GetDistance(pos, tipJ) < 18.0)
        {
            _draggedHandle = 2;
            return;
        }
        if (ShowTestVector && GetDistance(pos, tipU) < 18.0)
        {
            _draggedHandle = 3;
            return;
        }

        // Start Panning
        _isPanning = true;
        _panStartPoint = pos;
        _panStartOriginX = OriginX > 0 ? OriginX : Bounds.Width * 0.5;
        _panStartOriginY = OriginY > 0 ? OriginY : Bounds.Height * 0.5;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);

        if (_isPanning)
        {
            OriginX = _panStartOriginX + (pos.X - _panStartPoint.X);
            OriginY = _panStartOriginY + (pos.Y - _panStartPoint.Y);
            InvalidateVisual();
            return;
        }

        if (_draggedHandle == 1)
        {
            var math = ScreenToMath(pos);
            Matrix = new Matrix2x2(Math.Round(math.X, 2), Matrix.M12, Math.Round(math.Y, 2), Matrix.M22);
            InvalidateVisual();
        }
        else if (_draggedHandle == 2)
        {
            var math = ScreenToMath(pos);
            Matrix = new Matrix2x2(Matrix.M11, Math.Round(math.X, 2), Matrix.M21, Math.Round(math.Y, 2));
            InvalidateVisual();
        }
        else if (_draggedHandle == 3)
        {
            var math = ScreenToMath(pos);
            TestVector = new Vector2D(Math.Round(math.X, 2), Math.Round(math.Y, 2));
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggedHandle = 0;
        _isPanning = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pos = e.GetPosition(this);
        var mathBefore = ScreenToMath(pos);

        double factor = e.Delta.Y > 0 ? 1.15 : 1.0 / 1.15;
        Scale = Math.Clamp(Scale * factor, 15.0, 300.0);

        OriginX = pos.X - mathBefore.X * Scale;
        OriginY = pos.Y + mathBefore.Y * Scale;

        InvalidateVisual();
    }

    private static double GetDistance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
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

        var tokens = MathThemeService.Instance.Tokens;

        // 1. Background
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        // Effective morphed matrix A_t
        var effectiveM = Matrix2x2.Interpolate(Matrix2x2.Identity, Matrix, MorphProgress);
        var basisI = new Vector2D(effectiveM.M11, effectiveM.M21);
        var basisJ = new Vector2D(effectiveM.M12, effectiveM.M22);

        // 2. Transformed Grid Lines
        RenderTransformedGrid(context, effectiveM, tokens);

        // 3. Unit Circle & Transformed Ellipse (SVD Visualizer)
        if (ShowUnitCircle)
        {
            RenderUnitCircleAndEllipse(context, effectiveM, tokens);
        }

        // 4. Shaded Unit Parallelogram (Determinant Area)
        RenderDeterminantParallelogram(context, basisI, basisJ, effectiveM.Determinant, tokens);

        // 5. Eigenvector Direction Rays
        RenderEigenvectorRays(context, Matrix, tokens);

        // 6. Basis Vectors Arrows (i-hat cyan, j-hat rose)
        RenderVectorArrow(context, Vector2D.Zero, basisI, tokens.BasisVectorIColor, "î", 1);
        RenderVectorArrow(context, Vector2D.Zero, basisJ, tokens.BasisVectorJColor, "ĵ", 2);

        // 7. Test Vector u and u' = A u
        if (ShowTestVector)
        {
            var uTransformed = effectiveM.Transform(TestVector);
            RenderVectorArrow(context, Vector2D.Zero, TestVector, Color.FromRgb(245, 158, 11), "u (input)", 3, isDashed: true);
            RenderVectorArrow(context, Vector2D.Zero, uTransformed, Color.FromRgb(16, 185, 129), $"u' = A u ({uTransformed.X:F1}, {uTransformed.Y:F1})", 0);
        }

        // 8. Transformation HUD Card
        RenderTransformationHud(context, Matrix, effectiveM, tokens);
    }

    private void RenderTransformedGrid(DrawingContext context, Matrix2x2 m, MathThemeTokens tokens)
    {
        var gridPen = new Pen(new SolidColorBrush(tokens.GridLineMinorColor), 1.0);
        var axisPen = tokens.GridAxisPen;

        int range = 14;
        for (int i = -range; i <= range; i++)
        {
            var p1 = MathToScreen(m.Transform(new Vector2D(i, -range)));
            var p2 = MathToScreen(m.Transform(new Vector2D(i, range)));
            context.DrawLine(i == 0 ? axisPen : gridPen, p1, p2);

            var q1 = MathToScreen(m.Transform(new Vector2D(-range, i)));
            var q2 = MathToScreen(m.Transform(new Vector2D(range, i)));
            context.DrawLine(i == 0 ? axisPen : gridPen, q1, q2);
        }
    }

    private void RenderUnitCircleAndEllipse(DrawingContext context, Matrix2x2 m, MathThemeTokens tokens)
    {
        int samples = 80;
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            for (int i = 0; i <= samples; i++)
            {
                double theta = i * 2.0 * Math.PI / samples;
                var circlePt = new Vector2D(Math.Cos(theta), Math.Sin(theta));
                var transformedPt = m.Transform(circlePt);
                var screenPt = MathToScreen(transformedPt);

                if (i == 0) ctx.BeginFigure(screenPt, true);
                else ctx.LineTo(screenPt);
            }
        }

        var ellipsePen = new Pen(tokens.AccentAmberBrush, 1.4, dashStyle: DashStyle.Dash);
        var fillBrush = new SolidColorBrush(Color.FromArgb(20, 245, 158, 11));
        context.DrawGeometry(fillBrush, ellipsePen, geom);
    }

    private void RenderDeterminantParallelogram(DrawingContext context, Vector2D i, Vector2D j, double det, MathThemeTokens tokens)
    {
        var p00 = MathToScreen(Vector2D.Zero);
        var p10 = MathToScreen(i);
        var p11 = MathToScreen(i + j);
        var p01 = MathToScreen(j);

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(p00, true);
            ctx.LineTo(p10);
            ctx.LineTo(p11);
            ctx.LineTo(p01);
        }

        var fillBrush = det >= 0 ? new SolidColorBrush(tokens.DetPositiveColor) : new SolidColorBrush(tokens.DetNegativeColor);
        var borderPen = new Pen(det >= 0 ? tokens.AccentCyanBrush : tokens.AccentAmberBrush, 1.5, dashStyle: DashStyle.Dash);

        context.DrawGeometry(fillBrush, borderPen, geom);
    }

    private void RenderEigenvectorRays(DrawingContext context, Matrix2x2 m, MathThemeTokens tokens)
    {
        var eigenPairs = m.ComputeEigenPairs();
        var eigenPen = new Pen(tokens.AccentEmeraldBrush, 1.8, dashStyle: DashStyle.Dash);

        foreach (var p in eigenPairs)
        {
            if (!p.IsReal) continue;

            var ray1 = MathToScreen(p.Vector * -14.0);
            var ray2 = MathToScreen(p.Vector * 14.0);
            context.DrawLine(eigenPen, ray1, ray2);
        }
    }

    private void RenderVectorArrow(DrawingContext context, Vector2D start, Vector2D end, Color color, string label, int handleId, bool isDashed = false)
    {
        var pStart = MathToScreen(start);
        var pEnd = MathToScreen(end);

        var brush = new SolidColorBrush(color);
        var pen = new Pen(brush, 2.5);
        if (isDashed) pen.DashStyle = DashStyle.Dot;
        context.DrawLine(pen, pStart, pEnd);

        // Arrow head / Draggable Grab Handle
        context.DrawEllipse(brush, null, pEnd, 6.0, 6.0);
        context.DrawEllipse(null, new Pen(Brushes.White, 1.5), pEnd, 8.5, 8.5);

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 11, brush);
        context.DrawText(text, new Point(pEnd.X + 10.0, pEnd.Y - 6.0));
    }

    private void RenderTransformationHud(DrawingContext context, Matrix2x2 targetM, Matrix2x2 effectiveM, MathThemeTokens tokens)
    {
        double det = targetM.Determinant;
        double tr = targetM.Trace;
        string orientation = det > 1e-4 ? "Orientation Preserved (+)" : det < -1e-4 ? "Inverted Reflection (-)" : "Singular / Collapsed (0)";

        var pairs = targetM.ComputeEigenPairs();
        string eigenStr = pairs.Count > 0 && pairs[0].IsReal
            ? $"λ₁ = {pairs[0].Lambda:F2}" + (pairs.Count > 1 ? $", λ₂ = {pairs[1].Lambda:F2}" : "")
            : "λ: Complex Rotational (No real invariant lines)";

        string hud = $"Matrix A = [[{targetM.M11:F2}, {targetM.M12:F2}], [{targetM.M21:F2}, {targetM.M22:F2}]]\n" +
                     $"det(A) = {det:F3} ({orientation})  |  tr(A) = {tr:F2}\n" +
                     $"{eigenStr}\n" +
                     $"Interpolation t = {MorphProgress:F2}  |  Drag î / ĵ / u handles to morph space!";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var formatted = new FormattedText(hud, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 11, tokens.TextPrimaryBrush);

        double cardW = formatted.Width + 24.0;
        double cardH = formatted.Height + 16.0;
        var cardRect = new Rect(16.0, 16.0, cardW, cardH);

        context.FillRectangle(tokens.SurfaceHighBrush, cardRect, 6f);
        context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.0), cardRect, 6f, 6f);
        context.DrawText(formatted, new Point(28.0, 24.0));
    }
}
