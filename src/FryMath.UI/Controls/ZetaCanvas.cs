using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Zeta;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public enum ZetaViewMode
{
    CriticalLine,
    PrimeStaircase
}

public class ZetaCanvas : Control
{
    public static readonly StyledProperty<ZetaViewMode> ViewModeProperty =
        AvaloniaProperty.Register<ZetaCanvas, ZetaViewMode>(nameof(ViewMode), ZetaViewMode.CriticalLine);

    public static readonly StyledProperty<double> ProbeTProperty =
        AvaloniaProperty.Register<ZetaCanvas, double>(nameof(ProbeT), 14.1347);

    public static readonly StyledProperty<int> ZeroCountProperty =
        AvaloniaProperty.Register<ZetaCanvas, int>(nameof(ZeroCount), 10);

    public static readonly StyledProperty<double> XMaxProperty =
        AvaloniaProperty.Register<ZetaCanvas, double>(nameof(XMax), 50.0);

    public static readonly StyledProperty<bool> ShowZetaZerosProperty =
        AvaloniaProperty.Register<ZetaCanvas, bool>(nameof(ShowZetaZeros), true);

    public static readonly StyledProperty<bool> ShowSmoothLiProperty =
        AvaloniaProperty.Register<ZetaCanvas, bool>(nameof(ShowSmoothLi), true);

    public static readonly StyledProperty<bool> ShowExplicitWaveProperty =
        AvaloniaProperty.Register<ZetaCanvas, bool>(nameof(ShowExplicitWave), true);

    public ZetaViewMode ViewMode
    {
        get => GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public double ProbeT
    {
        get => GetValue(ProbeTProperty);
        set => SetValue(ProbeTProperty, value);
    }

    public int ZeroCount
    {
        get => GetValue(ZeroCountProperty);
        set => SetValue(ZeroCountProperty, value);
    }

    public double XMax
    {
        get => GetValue(XMaxProperty);
        set => SetValue(XMaxProperty, value);
    }

    public bool ShowZetaZeros
    {
        get => GetValue(ShowZetaZerosProperty);
        set => SetValue(ShowZetaZerosProperty, value);
    }

    public bool ShowSmoothLi
    {
        get => GetValue(ShowSmoothLiProperty);
        set => SetValue(ShowSmoothLiProperty, value);
    }

    public bool ShowExplicitWave
    {
        get => GetValue(ShowExplicitWaveProperty);
        set => SetValue(ShowExplicitWaveProperty, value);
    }

    static ZetaCanvas()
    {
        AffectsRender<ZetaCanvas>(ViewModeProperty, ProbeTProperty, ZeroCountProperty, XMaxProperty,
                                  ShowZetaZerosProperty, ShowSmoothLiProperty, ShowExplicitWaveProperty);
    }

    private bool _isDraggingProbe;
    private Point? _hoverPos;

    public ZetaCanvas()
    {
        ClipToBounds = true;
        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        if (ViewMode == ZetaViewMode.CriticalLine)
        {
            double plotW = Bounds.Width * 0.65;
            if (pt.X <= plotW)
            {
                _isDraggingProbe = true;
                UpdateProbeFromX(pt.X, plotW);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        _hoverPos = pt;

        if (_isDraggingProbe && ViewMode == ZetaViewMode.CriticalLine)
        {
            double plotW = Bounds.Width * 0.65;
            UpdateProbeFromX(pt.X, plotW);
        }
        else
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDraggingProbe = false;
    }

    private void UpdateProbeFromX(double mouseX, double plotW)
    {
        double marginL = 60.0;
        double marginR = 20.0;
        double graphW = Math.Max(plotW - marginL - marginR, 1.0);
        double tNorm = Math.Clamp((mouseX - marginL) / graphW, 0.0, 1.0);
        ProbeT = tNorm * 80.0;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 80 || h < 80) return;

        var tokens = MathThemeTokens.DarkElectric;
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        if (ViewMode == ZetaViewMode.CriticalLine)
        {
            RenderCriticalLineAndSpiral(context, w, h, tokens);
        }
        else
        {
            RenderPrimeStaircase(context, w, h, tokens);
        }
    }

    private void RenderCriticalLineAndSpiral(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        double splitX = w * 0.64;

        // Split divider
        context.DrawLine(new Pen(tokens.BorderBrush, 1.5), new Point(splitX, 0), new Point(splitX, h));

        // 1. Left Plot: Riemann-Siegel Z(t) function for t in [0, 80]
        double marginL = 60.0;
        double marginR = 20.0;
        double marginT = 40.0;
        double marginB = 50.0;

        double plotW = splitX - marginL - marginR;
        double plotH = h - marginT - marginB;
        double zeroY = marginT + plotH * 0.5;

        // Grid lines
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), 1.0);
        context.DrawLine(gridPen, new Point(marginL, zeroY), new Point(marginL + plotW, zeroY));

        for (double tVal = 10.0; tVal <= 80.0; tVal += 10.0)
        {
            double gx = marginL + (tVal / 80.0) * plotW;
            context.DrawLine(gridPen, new Point(gx, marginT), new Point(gx, marginT + plotH));

            var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
            var tLabel = new FormattedText($"t={tVal:F0}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 9, tokens.TextMutedBrush);
            context.DrawText(tLabel, new Point(gx - 12.0, marginT + plotH + 8.0));
        }

        // Plot Z(t) curve
        int samples = 350;
        var zGeom = new StreamGeometry();
        double zScale = plotH * 0.12; // Z(t) typically oscillates between -3 and +3

        using (var ctx = zGeom.Open())
        {
            bool started = false;
            for (int i = 0; i <= samples; i++)
            {
                double t = (double)i / samples * 80.0;
                double zVal = RiemannZetaEngine.EvaluateSiegelZ(t);
                double sx = marginL + (t / 80.0) * plotW;
                double sy = zeroY - Math.Clamp(zVal, -4.5, 4.5) * zScale;

                if (!started)
                {
                    ctx.BeginFigure(new Point(sx, sy), false);
                    started = true;
                }
                else
                {
                    ctx.LineTo(new Point(sx, sy));
                }
            }
        }
        context.DrawGeometry(null, new Pen(tokens.AccentCyanBrush, 2.2), zGeom);

        // Highlight Riemann Zeros on Z(t)
        if (ShowZetaZeros)
        {
            var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
            for (int k = 0; k < RiemannZetaEngine.KnownZeros.Length; k++)
            {
                double gamma = RiemannZetaEngine.KnownZeros[k];
                if (gamma > 80.0) break;

                double zx = marginL + (gamma / 80.0) * plotW;
                // Zero crossing dot
                context.DrawEllipse(tokens.AccentAmberBrush, null, new Point(zx, zeroY), 4.5, 4.5);
                context.DrawEllipse(null, new Pen(Brushes.White, 1.2), new Point(zx, zeroY), 6.5, 6.5);

                if (k < 8)
                {
                    var zLabel = new FormattedText($"γ{k + 1}={gamma:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 8, tokens.AccentAmberBrush);
                    context.DrawText(zLabel, new Point(zx - 16.0, zeroY - (k % 2 == 0 ? 22.0 : -10.0)));
                }
            }
        }

        // Draw Probe Line at ProbeT
        double probeX = marginL + (Math.Clamp(ProbeT, 0.0, 80.0) / 80.0) * plotW;
        double probeZ = RiemannZetaEngine.EvaluateSiegelZ(ProbeT);
        double probeY = zeroY - Math.Clamp(probeZ, -4.5, 4.5) * zScale;

        var probePen = new Pen(tokens.AccentEmeraldBrush, 1.5, DashStyle.Dash);
        context.DrawLine(probePen, new Point(probeX, marginT), new Point(probeX, marginT + plotH));
        context.DrawEllipse(tokens.AccentEmeraldBrush, null, new Point(probeX, probeY), 6.0, 6.0);
        context.DrawEllipse(null, new Pen(Brushes.White, 2.0), new Point(probeX, probeY), 8.5, 8.5);

        // Left HUD Title
        var tfBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var titleLeft = new FormattedText(
            $"RIEMANN-SIEGEL FUNCTION Z(t) = e^(i θ(t)) · ζ(1/2 + it)\n" +
            $"Probe: t = {ProbeT:F3}  |  Z(t) = {probeZ:F4}  |  θ(t) = {RiemannZetaEngine.RiemannSiegelTheta(ProbeT):F3} rad",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tfBold,
            11,
            tokens.TextPrimaryBrush);
        context.DrawText(titleLeft, new Point(marginL, 12.0));

        // 2. Right Side: Complex Polar Plane of ζ(1/2 + it)
        double planeW = w - splitX;
        double pCenterX = splitX + planeW * 0.5;
        double pCenterY = h * 0.52;
        double planeScale = Math.Min(planeW, h) * 0.22; // scale for complex plane

        // Polar grid circles
        var polarPen = new Pen(new SolidColorBrush(Color.FromArgb(35, 168, 85, 247)), 1.0);
        context.DrawEllipse(null, polarPen, new Point(pCenterX, pCenterY), planeScale * 1.0, planeScale * 1.0);
        context.DrawEllipse(null, polarPen, new Point(pCenterX, pCenterY), planeScale * 2.0, planeScale * 2.0);
        context.DrawLine(polarPen, new Point(pCenterX - planeScale * 2.2, pCenterY), new Point(pCenterX + planeScale * 2.2, pCenterY));
        context.DrawLine(polarPen, new Point(pCenterX, pCenterY - planeScale * 2.2), new Point(pCenterX, pCenterY + planeScale * 2.2));

        // Origin Zero Target
        context.DrawEllipse(tokens.AccentRoseBrush, null, new Point(pCenterX, pCenterY), 4.0, 4.0);
        var originLabel = new FormattedText("Origin (0, 0)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tfBold, 9, tokens.AccentRoseBrush);
        context.DrawText(originLabel, new Point(pCenterX + 8.0, pCenterY + 4.0));

        // Trace polar spiral ζ(1/2 + it) up to ProbeT
        int spiralSteps = Math.Min(400, (int)(ProbeT * 12));
        if (spiralSteps > 5)
        {
            var spiralGeom = new StreamGeometry();
            using (var ctx = spiralGeom.Open())
            {
                bool sStarted = false;
                for (int i = 0; i <= spiralSteps; i++)
                {
                    double st = (double)i / spiralSteps * ProbeT;
                    Complex zeta = RiemannZetaEngine.EvaluateZeta(new Complex(0.5, st));

                    double px = pCenterX + zeta.Real * planeScale;
                    double py = pCenterY - zeta.Imaginary * planeScale;

                    if (!sStarted)
                    {
                        ctx.BeginFigure(new Point(px, py), false);
                        sStarted = true;
                    }
                    else
                    {
                        ctx.LineTo(new Point(px, py));
                    }
                }
            }
            context.DrawGeometry(null, new Pen(tokens.AccentPurpleBrush, 1.8), spiralGeom);
        }

        // Current Polar Tip
        Complex probeZeta = RiemannZetaEngine.EvaluateZeta(new Complex(0.5, ProbeT));
        double tipX = pCenterX + probeZeta.Real * planeScale;
        double tipY = pCenterY - probeZeta.Imaginary * planeScale;

        context.DrawLine(new Pen(tokens.AccentEmeraldBrush, 1.5), new Point(pCenterX, pCenterY), new Point(tipX, tipY));
        context.DrawEllipse(tokens.AccentEmeraldBrush, null, new Point(tipX, tipY), 5.5, 5.5);
        context.DrawEllipse(null, new Pen(Brushes.White, 1.8), new Point(tipX, tipY), 7.5, 7.5);

        // Right HUD Title
        var titleRight = new FormattedText(
            $"COMPLEX PHASOR: ζ(1/2 + it)\n" +
            $"Re = {probeZeta.Real:F3}, Im = {probeZeta.Imaginary:F3}\n" +
            $"|ζ| = {probeZeta.Magnitude:F4} (Zero crossing when tip hits origin)",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tfBold,
            10,
            tokens.TextSecondaryBrush);
        context.DrawText(titleRight, new Point(splitX + 16.0, 16.0));
    }

    private void RenderPrimeStaircase(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        double marginL = 70.0;
        double marginR = 30.0;
        double marginT = 40.0;
        double marginB = 60.0;

        double plotW = w - marginL - marginR;
        double plotH = h - marginT - marginB;

        double maxPrimeX = Math.Clamp(XMax, 10.0, 100.0);
        int maxPi = RiemannZetaEngine.ExactPrimeCount(maxPrimeX);
        double maxPrimeY = Math.Max(maxPi + 3, 10.0);

        // Coordinate Grid
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), 1.0);
        var tf = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

        for (double xVal = 10.0; xVal <= maxPrimeX; xVal += 10.0)
        {
            double sx = marginL + (xVal / maxPrimeX) * plotW;
            context.DrawLine(gridPen, new Point(sx, marginT), new Point(sx, marginT + plotH));
            var xLabel = new FormattedText($"x={xVal:F0}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9, tokens.TextMutedBrush);
            context.DrawText(xLabel, new Point(sx - 10.0, marginT + plotH + 8.0));
        }

        for (double yVal = 5.0; yVal <= maxPrimeY; yVal += 5.0)
        {
            double sy = marginT + plotH - (yVal / maxPrimeY) * plotH;
            context.DrawLine(gridPen, new Point(marginL, sy), new Point(marginL + plotW, sy));
            var yLabel = new FormattedText($"π={yVal:F0}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 9, tokens.TextMutedBrush);
            context.DrawText(yLabel, new Point(marginL - 38.0, sy - 6.0));
        }

        // 1. Gauss Smooth Approximation R(x)
        if (ShowSmoothLi)
        {
            var rGeom = new StreamGeometry();
            using (var ctx = rGeom.Open())
            {
                bool started = false;
                int steps = 200;
                for (int i = 0; i <= steps; i++)
                {
                    double x = 2.0 + (double)i / steps * (maxPrimeX - 2.0);
                    double rVal = RiemannZetaEngine.RiemannR(x);
                    double sx = marginL + (x / maxPrimeX) * plotW;
                    double sy = marginT + plotH - (rVal / maxPrimeY) * plotH;

                    if (!started)
                    {
                        ctx.BeginFigure(new Point(sx, sy), false);
                        started = true;
                    }
                    else
                    {
                        ctx.LineTo(new Point(sx, sy));
                    }
                }
            }
            context.DrawGeometry(null, new Pen(tokens.AccentPurpleBrush, 2.0), rGeom);
        }

        // 2. Riemann Explicit Formula Superposition (with ZeroCount zeros)
        if (ShowExplicitWave && ZeroCount > 0)
        {
            var expGeom = new StreamGeometry();
            using (var ctx = expGeom.Open())
            {
                bool started = false;
                int steps = 300;
                for (int i = 0; i <= steps; i++)
                {
                    double x = 2.0 + (double)i / steps * (maxPrimeX - 2.0);
                    double expVal = RiemannZetaEngine.ExplicitPrimeFormula(x, ZeroCount);
                    double sx = marginL + (x / maxPrimeX) * plotW;
                    double sy = marginT + plotH - (expVal / maxPrimeY) * plotH;

                    if (!started)
                    {
                        ctx.BeginFigure(new Point(sx, sy), false);
                        started = true;
                    }
                    else
                    {
                        ctx.LineTo(new Point(sx, sy));
                    }
                }
            }
            context.DrawGeometry(null, new Pen(tokens.AccentAmberBrush, 2.4), expGeom);
        }

        // 3. True Step Function π(x)
        var stepGeom = new StreamGeometry();
        using (var ctx = stepGeom.Open())
        {
            double prevY = 0.0;
            double prevX = 2.0;
            double sx0 = marginL + (prevX / maxPrimeX) * plotW;
            double sy0 = marginT + plotH;
            ctx.BeginFigure(new Point(sx0, sy0), false);

            for (int p = 2; p <= (int)maxPrimeX; p++)
            {
                if (RiemannZetaEngine.IsPrime(p))
                {
                    double sxStep = marginL + (p / maxPrimeX) * plotW;
                    double syOld = marginT + plotH - (prevY / maxPrimeY) * plotH;
                    double syNew = marginT + plotH - ((prevY + 1) / maxPrimeY) * plotH;

                    ctx.LineTo(new Point(sxStep, syOld));
                    ctx.LineTo(new Point(sxStep, syNew));
                    prevY += 1.0;
                }
            }
            double sxFinal = marginL + plotW;
            double syFinal = marginT + plotH - (prevY / maxPrimeY) * plotH;
            ctx.LineTo(new Point(sxFinal, syFinal));
        }
        context.DrawGeometry(null, new Pen(tokens.AccentCyanBrush, 2.2), stepGeom);

        // Highlight Primes with dots on x-axis
        for (int p = 2; p <= (int)maxPrimeX; p++)
        {
            if (RiemannZetaEngine.IsPrime(p))
            {
                double px = marginL + (p / maxPrimeX) * plotW;
                int piVal = RiemannZetaEngine.ExactPrimeCount(p);
                double py = marginT + plotH - (piVal / maxPrimeY) * plotH;

                context.DrawEllipse(tokens.AccentCyanBrush, null, new Point(px, py), 3.0, 3.0);
            }
        }

        // Diagnostic Header HUD
        var tfBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var header = new FormattedText(
            $"RIEMANN'S EXPLICIT FORMULA: PRIMES FROM ZETA ZEROS\n" +
            $"• Cyan Steps: True Prime Count π(x)  |  • Purple: Smooth R(x)  |  • Gold: Superposition ({ZeroCount} Riemann Zeros)\n" +
            $"π₀(x) ≈ R(x) - Σ_{{k=1}}^{{{ZeroCount}}} 2 Re( x^(1/2 + i γₖ) / ((1/2 + i γₖ) ln x) )",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tfBold,
            11,
            tokens.TextPrimaryBrush);
        context.DrawText(header, new Point(marginL, 12.0));
    }
}
