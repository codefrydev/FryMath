using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Quantum;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class QuantumCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<QuantumCanvas, string>(nameof(PresetName), "tunneling");

    public static readonly StyledProperty<bool> IsSimulatingProperty =
        AvaloniaProperty.Register<QuantumCanvas, bool>(nameof(IsSimulating), true);

    public static readonly StyledProperty<bool> ShowPhaseColoringProperty =
        AvaloniaProperty.Register<QuantumCanvas, bool>(nameof(ShowPhaseColoring), true);

    public static readonly StyledProperty<bool> ShowProbabilityDensityProperty =
        AvaloniaProperty.Register<QuantumCanvas, bool>(nameof(ShowProbabilityDensity), true);

    public static readonly StyledProperty<bool> ShowWaveComponentsProperty =
        AvaloniaProperty.Register<QuantumCanvas, bool>(nameof(ShowWaveComponents), true);

    public static readonly StyledProperty<bool> ShowPotentialProperty =
        AvaloniaProperty.Register<QuantumCanvas, bool>(nameof(ShowPotential), true);

    public static readonly StyledProperty<double> TimeStepProperty =
        AvaloniaProperty.Register<QuantumCanvas, double>(nameof(TimeStep), 0.04);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public bool IsSimulating
    {
        get => GetValue(IsSimulatingProperty);
        set => SetValue(IsSimulatingProperty, value);
    }

    public bool ShowPhaseColoring
    {
        get => GetValue(ShowPhaseColoringProperty);
        set => SetValue(ShowPhaseColoringProperty, value);
    }

    public bool ShowProbabilityDensity
    {
        get => GetValue(ShowProbabilityDensityProperty);
        set => SetValue(ShowProbabilityDensityProperty, value);
    }

    public bool ShowWaveComponents
    {
        get => GetValue(ShowWaveComponentsProperty);
        set => SetValue(ShowWaveComponentsProperty, value);
    }

    public bool ShowPotential
    {
        get => GetValue(ShowPotentialProperty);
        set => SetValue(ShowPotentialProperty, value);
    }

    public double TimeStep
    {
        get => GetValue(TimeStepProperty);
        set => SetValue(TimeStepProperty, value);
    }

    private readonly QuantumSchrodingerEngine _engine = new(320, -15.0, 15.0);
    private readonly DispatcherTimer _timer;
    private string? _loadedPreset;
    private Point? _hoverPos;
    private bool _isDrawingBarrier;

    static QuantumCanvas()
    {
        AffectsRender<QuantumCanvas>(
            PresetNameProperty,
            IsSimulatingProperty,
            ShowPhaseColoringProperty,
            ShowProbabilityDensityProperty,
            ShowWaveComponentsProperty,
            ShowPotentialProperty,
            TimeStepProperty);
    }

    public QuantumCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            if (IsSimulating)
            {
                _engine.Dt = TimeStep;
                _engine.Step();
                InvalidateVisual();
            }
        };
        _timer.Start();

        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetPosition(this);
        _hoverPos = pt;

        if (_isDrawingBarrier)
        {
            ApplyBarrierAtPointer(pt);
        }
        else
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        _isDrawingBarrier = true;
        ApplyBarrierAtPointer(pt);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDrawingBarrier = false;
    }

    private void ApplyBarrierAtPointer(Point pt)
    {
        double u = Math.Clamp(pt.X / Bounds.Width, 0.05, 0.95);
        double x = _engine.XMin + u * (_engine.XMax - _engine.XMin);
        _engine.AddPotentialFeature(x, 2.5, 0.9);
        InvalidateVisual();
    }

    private void EnsurePreset()
    {
        if (_loadedPreset != PresetName)
        {
            var p = PresetName.ToLowerInvariant() switch
            {
                "harmonic" => QuantumPreset.HarmonicOscillator,
                "doublewell" => QuantumPreset.DoubleWell,
                "step" => QuantumPreset.StepPotential,
                "free" => QuantumPreset.FreeWavepacket,
                _ => QuantumPreset.TunnelingBarrier
            };

            _engine.LoadPreset(p);
            _loadedPreset = PresetName;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        EnsurePreset();

        var tokens = MathThemeTokens.DarkElectric;
        context.FillRectangle(tokens.SurfaceBrush, new Rect(0, 0, w, h));

        // Coordinate transforms
        double baselineY = h * 0.72;
        double probScale = h * 0.48;
        double waveScale = h * 0.22;
        double potScale = h * 0.045;

        // 1. Draw Grid lines and Axis
        RenderGridAndAxes(context, w, h, baselineY, tokens);

        // 2. Draw Potential Barrier V(x)
        if (ShowPotential)
        {
            RenderPotential(context, w, baselineY, potScale, tokens);
        }

        // 3. Draw Probability Density |ψ(x)|² with Phase Spectrum coloring
        if (ShowProbabilityDensity)
        {
            RenderProbabilityDensity(context, w, baselineY, probScale, tokens);
        }

        // 4. Draw Re(ψ) and Im(ψ) wave components
        if (ShowWaveComponents)
        {
            RenderWaveComponents(context, w, baselineY, waveScale, tokens);
        }

        // 5. Draw Observables & Hover Probe HUD
        RenderHUD(context, w, h, baselineY, tokens);
    }

    private void RenderGridAndAxes(DrawingContext context, double w, double h, double baselineY, MathThemeTokens tokens)
    {
        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 1.2);
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), 0.8, DashStyle.Dash);

        // Baseline (x-axis)
        context.DrawLine(axisPen, new Point(0, baselineY), new Point(w, baselineY));

        // Spatial grid vertical lines
        for (double x = Math.Ceiling(_engine.XMin); x <= Math.Floor(_engine.XMax); x += 2.0)
        {
            double u = (x - _engine.XMin) / (_engine.XMax - _engine.XMin);
            double px = u * w;
            context.DrawLine(gridPen, new Point(px, 40), new Point(px, h - 30));

            if (Math.Abs(x) < 1e-6)
            {
                // Origin marker line
                context.DrawLine(new Pen(tokens.AccentCyanBrush, 1.0, DashStyle.Dash), new Point(px, 30), new Point(px, h - 20));
            }
        }
    }

    private void RenderPotential(DrawingContext context, double w, double baselineY, double potScale, MathThemeTokens tokens)
    {
        var potGeo = new StreamGeometry();
        using (var ctx = potGeo.Open())
        {
            ctx.BeginFigure(new Point(0, baselineY), isFilled: true);
            for (int i = 0; i < _engine.Points; i++)
            {
                double px = (double)i / (_engine.Points - 1) * w;
                double vVal = Math.Min(_engine.Potential[i], 12.0);
                double py = Math.Clamp(baselineY - vVal * potScale, 30.0, baselineY);
                ctx.LineTo(new Point(px, py));
            }
            ctx.LineTo(new Point(w, baselineY));
            ctx.EndFigure(isClosed: true);
        }

        var fillBrush = new SolidColorBrush(Color.FromArgb(50, 245, 158, 11)); // Amber translucent
        var strokePen = new Pen(tokens.AccentAmberBrush, 1.8);

        context.DrawGeometry(fillBrush, strokePen, potGeo);
    }

    private void RenderProbabilityDensity(DrawingContext context, double w, double baselineY, double probScale, MathThemeTokens tokens)
    {
        int n = _engine.Points;

        // Translucent density fill
        var densityGeo = new StreamGeometry();
        using (var ctx = densityGeo.Open())
        {
            ctx.BeginFigure(new Point(0, baselineY), isFilled: true);
            for (int i = 0; i < n; i++)
            {
                double px = (double)i / (n - 1) * w;
                double prob = _engine.PsiR[i] * _engine.PsiR[i] + _engine.PsiI[i] * _engine.PsiI[i];
                double py = baselineY - prob * probScale;
                ctx.LineTo(new Point(px, py));
            }
            ctx.LineTo(new Point(w, baselineY));
            ctx.EndFigure(isClosed: true);
        }

        var gradBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromArgb(140, 6, 182, 212), 0.0), // Glowing cyan
                new GradientStop(Color.FromArgb(60, 168, 85, 247), 0.6), // Purple
                new GradientStop(Color.FromArgb(15, 15, 23, 42), 1.0)
            }
        };
        context.DrawGeometry(gradBrush, null, densityGeo);

        // Phase colored top outline segments
        for (int i = 0; i < n - 1; i++)
        {
            double px1 = (double)i / (n - 1) * w;
            double prob1 = _engine.PsiR[i] * _engine.PsiR[i] + _engine.PsiI[i] * _engine.PsiI[i];
            double py1 = baselineY - prob1 * probScale;

            double px2 = (double)(i + 1) / (n - 1) * w;
            double prob2 = _engine.PsiR[i + 1] * _engine.PsiR[i + 1] + _engine.PsiI[i + 1] * _engine.PsiI[i + 1];
            double py2 = baselineY - prob2 * probScale;

            IBrush segBrush;
            if (ShowPhaseColoring)
            {
                double phase = Math.Atan2(_engine.PsiI[i], _engine.PsiR[i]); // [-π, π]
                segBrush = PhaseToBrush(phase);
            }
            else
            {
                segBrush = tokens.AccentCyanBrush;
            }

            var pen = new Pen(segBrush, 2.2);
            context.DrawLine(pen, new Point(px1, py1), new Point(px2, py2));
        }
    }

    private void RenderWaveComponents(DrawingContext context, double w, double baselineY, double waveScale, MathThemeTokens tokens)
    {
        int n = _engine.Points;
        var rePen = new Pen(new SolidColorBrush(Color.FromArgb(180, 56, 189, 248)), 1.2, DashStyle.Dash); // Light cyan
        var imPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 244, 63, 94)), 1.2, DashStyle.DashDot); // Rose

        for (int i = 0; i < n - 1; i++)
        {
            double px1 = (double)i / (n - 1) * w;
            double px2 = (double)(i + 1) / (n - 1) * w;

            // Re(ψ)
            double reY1 = baselineY - _engine.PsiR[i] * waveScale;
            double reY2 = baselineY - _engine.PsiR[i + 1] * waveScale;
            context.DrawLine(rePen, new Point(px1, reY1), new Point(px2, reY2));

            // Im(ψ)
            double imY1 = baselineY - _engine.PsiI[i] * waveScale;
            double imY2 = baselineY - _engine.PsiI[i + 1] * waveScale;
            context.DrawLine(imPen, new Point(px1, imY1), new Point(px2, imY2));
        }
    }

    private static IBrush PhaseToBrush(double phase)
    {
        // Map phase in [-π, π] to HSV hue [0, 360]
        double norm = (phase + Math.PI) / (2.0 * Math.PI);
        double h = norm * 360.0;
        return new SolidColorBrush(HsvToColor(h, 0.85, 0.95));
    }

    private static Color HsvToColor(double hue, double sat, double val)
    {
        int hi = (int)(Math.Floor(hue / 60.0)) % 6;
        double f = (hue / 60.0) - Math.Floor(hue / 60.0);

        byte v = (byte)(val * 255.0);
        byte p = (byte)(val * (1.0 - sat) * 255.0);
        byte q = (byte)(val * (1.0 - f * sat) * 255.0);
        byte t = (byte)(val * (1.0 - (1.0 - f) * sat) * 255.0);

        return hi switch
        {
            0 => Color.FromRgb(v, t, p),
            1 => Color.FromRgb(q, v, p),
            2 => Color.FromRgb(p, v, t),
            3 => Color.FromRgb(p, q, v),
            4 => Color.FromRgb(t, p, v),
            _ => Color.FromRgb(v, p, q)
        };
    }

    private void RenderHUD(DrawingContext context, double w, double h, double baselineY, MathThemeTokens tokens)
    {
        var tfBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var tfNorm = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

        double norm = _engine.CalculateNorm();
        double expX = _engine.CalculateExpectedPosition();
        double expP = _engine.CalculateExpectedMomentum();

        string hudTitle =
            $"SCHRÖDINGER WAVE MECHANICS: iħ ∂ψ/∂t = (-ħ²/2m ∇² + V(x))ψ\n" +
            $"Preset: {PresetName.ToUpperInvariant()}  |  Unitarity ∫|ψ|²dx = {norm:F4}  |  ⟨X⟩ = {expX:F2}  |  ⟨P⟩ = {expP:F2}\n" +
            $"(Click & Drag anywhere to inject potential barriers in real-time)";

        var titleText = new FormattedText(
            hudTitle,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tfBold,
            11,
            tokens.TextPrimaryBrush);

        context.DrawText(titleText, new Point(16.0, 16.0));

        // Legend: Re(psi), Im(psi), |psi|^2, V(x)
        string legend = "Cyan Dashed: Re(ψ)   |   Rose Dashed: Im(ψ)   |   Spectrum Fill: |ψ|² (Phase Arg)   |   Amber: V(x)";
        var legendText = new FormattedText(
            legend,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tfNorm,
            10,
            tokens.TextSecondaryBrush);

        context.DrawText(legendText, new Point(16.0, 68.0));

        // Hover probe
        if (_hoverPos.HasValue)
        {
            var hp = _hoverPos.Value;
            double u = Math.Clamp(hp.X / w, 0.0, 1.0);
            double xVal = _engine.XMin + u * (_engine.XMax - _engine.XMin);
            int idx = _engine.GetIndex(xVal);

            double psiR = _engine.PsiR[idx];
            double psiI = _engine.PsiI[idx];
            double prob = psiR * psiR + psiI * psiI;
            double phase = Math.Atan2(psiI, psiR);
            double phaseDeg = phase * 180.0 / Math.PI;
            double vVal = _engine.Potential[idx];
            double jCurr = _engine.CalculateProbabilityCurrent(idx);

            context.DrawEllipse(null, new Pen(Brushes.White, 1.5), hp, 8.0, 8.0);
            context.DrawEllipse(tokens.AccentCyanBrush, null, hp, 3.5, 3.5);

            string probeMsg =
                $"x = {xVal:F2}\n" +
                $"|ψ(x)|² = {prob:F4}\n" +
                $"ψ = {psiR:F3} + {psiI:F3}i\n" +
                $"Phase θ = {phaseDeg:F1}°\n" +
                $"Current J(x) = {jCurr:F3}\n" +
                $"Potential V(x) = {vVal:F2}";

            var probeText = new FormattedText(
                probeMsg,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                tfBold,
                11,
                tokens.TextPrimaryBrush);

            double cardW = 190;
            double cardH = 110;
            double cardX = Math.Min(hp.X + 16, w - cardW - 12);
            double cardY = Math.Min(hp.Y + 16, h - cardH - 12);

            var cardRect = new Rect(cardX, cardY, cardW, cardH);
            context.FillRectangle(tokens.SurfaceHighBrush, cardRect);
            context.DrawRectangle(new Pen(tokens.BorderBrush, 1.0), cardRect);
            context.DrawText(probeText, new Point(cardX + 10, cardY + 8));
        }
    }
}
