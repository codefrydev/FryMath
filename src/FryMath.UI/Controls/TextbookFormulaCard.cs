using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FryMath.Core.Formatting;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public enum TextbookCardMode
{
    Integral,
    Derivative,
    Matrix,
    Fourier
}

public class TextbookFormulaCard : Control
{
    public static readonly StyledProperty<TextbookCardMode> ModeProperty =
        AvaloniaProperty.Register<TextbookFormulaCard, TextbookCardMode>(nameof(Mode), TextbookCardMode.Integral);

    public static readonly StyledProperty<IntegralTypesetInfo?> IntegralInfoProperty =
        AvaloniaProperty.Register<TextbookFormulaCard, IntegralTypesetInfo?>(nameof(IntegralInfo));

    public static readonly StyledProperty<DerivativeTypesetInfo?> DerivativeInfoProperty =
        AvaloniaProperty.Register<TextbookFormulaCard, DerivativeTypesetInfo?>(nameof(DerivativeInfo));

    public static readonly StyledProperty<MatrixTypesetInfo?> MatrixInfoProperty =
        AvaloniaProperty.Register<TextbookFormulaCard, MatrixTypesetInfo?>(nameof(MatrixInfo));

    public TextbookCardMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public IntegralTypesetInfo? IntegralInfo
    {
        get => GetValue(IntegralInfoProperty);
        set => SetValue(IntegralInfoProperty, value);
    }

    public DerivativeTypesetInfo? DerivativeInfo
    {
        get => GetValue(DerivativeInfoProperty);
        set => SetValue(DerivativeInfoProperty, value);
    }

    public MatrixTypesetInfo? MatrixInfo
    {
        get => GetValue(MatrixInfoProperty);
        set => SetValue(MatrixInfoProperty, value);
    }

    static TextbookFormulaCard()
    {
        AffectsRender<TextbookFormulaCard>(ModeProperty, IntegralInfoProperty, DerivativeInfoProperty, MatrixInfoProperty);
        AffectsMeasure<TextbookFormulaCard>(ModeProperty);
    }

    public TextbookFormulaCard()
    {
        ClipToBounds = true;
        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double h = Mode switch
        {
            TextbookCardMode.Integral => 125.0,
            TextbookCardMode.Derivative => 125.0,
            TextbookCardMode.Matrix => 120.0,
            _ => 100.0
        };
        return new Size(Math.Max(availableSize.Width, 240.0), h);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var tokens = MathThemeService.Instance.Tokens;
        var bounds = Bounds;
        if (bounds.Width < 10 || bounds.Height < 10) return;

        // Outer Card Container
        var cardRect = new Rect(0, 0, bounds.Width, bounds.Height);
        context.FillRectangle(tokens.SurfaceHighBrush, cardRect, 8f);
        context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.2), cardRect, 8f, 8f);

        switch (Mode)
        {
            case TextbookCardMode.Integral:
                RenderIntegralFormula(context, bounds, tokens);
                break;
            case TextbookCardMode.Derivative:
                RenderDerivativeFormula(context, bounds, tokens);
                break;
            case TextbookCardMode.Matrix:
                RenderMatrixFormula(context, bounds, tokens);
                break;
        }
    }

    private void RenderIntegralFormula(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        var info = IntegralInfo;
        if (info == null) return;

        var fontRegular = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Regular);
        var fontBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var fontItalic = new Typeface(FontFamily.Default, FontStyle.Italic, FontWeight.SemiBold);

        // Header label
        var header = new FormattedText("DEFINITE INTEGRAL & FUNDAMENTAL THEOREM", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 10, tokens.TextMutedBrush);
        context.DrawText(header, new Point(12, 10));

        // 1. Vector Tall Integral Sign: ∫
        double startX = 16.0;
        double startY = 32.0;

        var intSymbol = new FormattedText("∫", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontRegular, 44, tokens.TextPrimaryBrush);
        context.DrawText(intSymbol, new Point(startX, startY - 6.0));

        // Upper Bound (b)
        var upperText = new FormattedText($"{info.UpperBound:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 10, tokens.TextSecondaryBrush);
        context.DrawText(upperText, new Point(startX + 18.0, startY - 4.0));

        // Lower Bound (a)
        var lowerText = new FormattedText($"{info.LowerBound:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 10, tokens.TextSecondaryBrush);
        context.DrawText(lowerText, new Point(startX + 18.0, startY + 28.0));

        // Integrand f(x) dx
        double exprX = startX + 22.0 + Math.Max(upperText.Width, lowerText.Width);
        string integrandStr = string.IsNullOrWhiteSpace(info.IntegrandText) ? "f(x)" : info.IntegrandText;
        var integrandText = new FormattedText($"({integrandStr})", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontItalic, 15, tokens.TextPrimaryBrush);
        context.DrawText(integrandText, new Point(exprX, startY + 12.0));

        var dxText = new FormattedText("dx", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 14, tokens.TextSecondaryBrush);
        double dxX = exprX + integrandText.Width + 4.0;
        context.DrawText(dxText, new Point(dxX, startY + 13.0));

        // Equals Sign
        double eqX = dxX + dxText.Width + 8.0;
        var eqText = new FormattedText("=", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 16, tokens.TextMutedBrush);
        context.DrawText(eqText, new Point(eqX, startY + 11.0));

        // Result Badge
        double resX = eqX + eqText.Width + 8.0;
        var resText = new FormattedText(info.FormattedResult, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 16, tokens.TextPrimaryBrush);
        var badgeRect = new Rect(resX - 4.0, startY + 8.0, resText.Width + 12.0, resText.Height + 6.0);
        context.FillRectangle(tokens.SurfaceBrush, badgeRect, 4f);
        context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.2), badgeRect, 4f, 4f);
        context.DrawText(resText, new Point(resX + 2.0, startY + 11.0));

        // Sub-Formula 1: Antiderivative closed form
        double subY = startY + 44.0;
        if (!string.IsNullOrWhiteSpace(info.AntiderivativeText))
        {
            string antiStr = $"Antiderivative: ∫ f(x)dx = {info.AntiderivativeText}";
            var antiText = new FormattedText(antiStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontRegular, 9.5, tokens.TextSecondaryBrush);
            context.DrawText(antiText, new Point(startX, subY));
            subY += 15.0;
        }

        // Sub-Formula 2: Riemann Partition Approximation
        string riemannStr = $"Riemann Sum ({info.RiemannRuleName}):  Σ f(xᵢ*) Δx ≈ {info.RiemannValue:F3}  (N = {info.RiemannN}, Δx = {(info.UpperBound - info.LowerBound) / Math.Max(info.RiemannN, 1):F3})";
        var riemannText = new FormattedText(riemannStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontRegular, 9.5, tokens.TextMutedBrush);
        context.DrawText(riemannText, new Point(startX, subY));
    }

    private void RenderDerivativeFormula(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        var info = DerivativeInfo;
        if (info == null) return;

        var fontRegular = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Regular);
        var fontBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var fontItalic = new Typeface(FontFamily.Default, FontStyle.Italic, FontWeight.SemiBold);

        // Header label
        var header = new FormattedText("DIFFERENTIATION & GEOMETRIC SLOPE", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 10, tokens.TextMutedBrush);
        context.DrawText(header, new Point(12, 10));

        // 1. Leibniz Fraction Bar d/dx
        double startX = 16.0;
        double startY = 32.0;

        var numD = new FormattedText("d", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, tokens.TextSecondaryBrush);
        var denDx = new FormattedText("dx", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, tokens.TextSecondaryBrush);
        double fracW = Math.Max(numD.Width, denDx.Width) + 4.0;

        context.DrawText(numD, new Point(startX + (fracW - numD.Width) * 0.5, startY));
        context.DrawLine(new Pen(tokens.TextPrimaryBrush, 1.5), new Point(startX, startY + 16.0), new Point(startX + fracW, startY + 16.0));
        context.DrawText(denDx, new Point(startX + (fracW - denDx.Width) * 0.5, startY + 18.0));

        // Expression [f(x)]
        double bracketX = startX + fracW + 6.0;
        string fnStr = string.IsNullOrWhiteSpace(info.FunctionText) ? "f(x)" : info.FunctionText;
        var fnText = new FormattedText($"[{fnStr}]", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontItalic, 14, tokens.TextPrimaryBrush);
        context.DrawText(fnText, new Point(bracketX, startY + 8.0));

        // Equals
        double eqX = bracketX + fnText.Width + 6.0;
        var eqText = new FormattedText("=", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 15, tokens.TextMutedBrush);
        context.DrawText(eqText, new Point(eqX, startY + 8.0));

        // Derivative Result
        double derivX = eqX + eqText.Width + 6.0;
        var derivText = new FormattedText(info.DerivativeText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 14, tokens.TextPrimaryBrush);
        context.DrawText(derivText, new Point(derivX, startY + 8.0));

        // 2. Evaluated at Probe x0
        double row2Y = startY + 36.0;
        string probeStr = $"At x = {info.ProbeX:F2}:  f'({info.ProbeX:F2}) = {info.Slope:F3} (m),  f''({info.ProbeX:F2}) = {info.SecondDerivative:F3}";
        var probeText = new FormattedText(probeStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 10, tokens.TextSecondaryBrush);
        context.DrawText(probeText, new Point(startX, row2Y));

        // 3. Tangent & Curvature
        double row3Y = row2Y + 16.0;
        string geomStr = $"Tangent: {info.TangentEquation}  |  Curvature κ = {info.Curvature:F3}  (Radius R = {(double.IsInfinity(info.OsculatingRadius) ? "∞" : info.OsculatingRadius.ToString("F2"))})";
        var geomText = new FormattedText(geomStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontRegular, 10, tokens.TextMutedBrush);
        context.DrawText(geomText, new Point(startX, row3Y));
    }

    private void RenderMatrixFormula(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        var info = MatrixInfo;
        if (info == null) return;

        var fontRegular = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Regular);
        var fontBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

        // Header label
        var header = new FormattedText("MATRIX TRANSFORMATION & EIGENSYSTEM", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 10, tokens.TextMutedBrush);
        context.DrawText(header, new Point(12, 10));

        // Matrix A = [ [a, b], [c, d] ] with real bracket drawing
        double startX = 16.0;
        double startY = 32.0;

        var aLabel = new FormattedText("A =", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 15, tokens.TextPrimaryBrush);
        context.DrawText(aLabel, new Point(startX, startY + 12.0));

        double mX = startX + aLabel.Width + 8.0;
        double mW = 90.0;
        double mH = 44.0;

        // Draw left square bracket [
        var bracketPen = new Pen(tokens.BorderBrush, 2.0);
        context.DrawLine(bracketPen, new Point(mX + 6, startY), new Point(mX, startY));
        context.DrawLine(bracketPen, new Point(mX, startY), new Point(mX, startY + mH));
        context.DrawLine(bracketPen, new Point(mX, startY + mH), new Point(mX + 6, startY + mH));

        // Elements
        var m11Text = new FormattedText($"{info.M11:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, tokens.TextPrimaryBrush);
        var m12Text = new FormattedText($"{info.M12:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, tokens.TextPrimaryBrush);
        var m21Text = new FormattedText($"{info.M21:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, tokens.TextPrimaryBrush);
        var m22Text = new FormattedText($"{info.M22:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, tokens.TextPrimaryBrush);

        context.DrawText(m11Text, new Point(mX + 10, startY + 4));
        context.DrawText(m12Text, new Point(mX + 52, startY + 4));
        context.DrawText(m21Text, new Point(mX + 10, startY + 24));
        context.DrawText(m22Text, new Point(mX + 52, startY + 24));

        // Draw right square bracket ]
        double rX = mX + mW;
        context.DrawLine(bracketPen, new Point(rX - 6, startY), new Point(rX, startY));
        context.DrawLine(bracketPen, new Point(rX, startY), new Point(rX, startY + mH));
        context.DrawLine(bracketPen, new Point(rX, startY + mH), new Point(rX - 6, startY + mH));

        // Determinant and Trace
        double detX = rX + 16.0;
        string detStr = $"det(A) = {info.Determinant:F3}  ({info.OrientationDescription})\ntr(A)  = {info.Trace:F2}";
        var detText = new FormattedText(detStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontBold, 12, tokens.TextPrimaryBrush);
        context.DrawText(detText, new Point(detX, startY + 4.0));

        // Bottom Row: Eigenvalues & Inverse
        double row2Y = startY + mH + 8.0;
        string eigenStr = $"Char. Poly: {info.CharacteristicPolynomial}  |  {info.EigenvaluesSummary}";
        var eigenText = new FormattedText(eigenStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontRegular, 10, tokens.TextMutedBrush);
        context.DrawText(eigenText, new Point(startX, row2Y));

        if (!string.IsNullOrEmpty(info.InverseDescription))
        {
            var invText = new FormattedText(info.InverseDescription, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, fontRegular, 10, tokens.TextSecondaryBrush);
            context.DrawText(invText, new Point(startX, row2Y + 16.0));
        }
    }
}
