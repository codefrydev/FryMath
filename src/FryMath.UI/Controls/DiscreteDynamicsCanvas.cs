using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Dynamics;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class DiscreteDynamicsCanvas : Control
{
    public static readonly StyledProperty<string> DisplayModeProperty =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, string>(nameof(DisplayMode), "arnold");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, string>(nameof(PresetName), "arnold");

    public static readonly StyledProperty<double> LogisticRProperty =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, double>(nameof(LogisticR), 3.65);

    public static readonly StyledProperty<double> LogisticX0Property =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, double>(nameof(LogisticX0), 0.45);

    public static readonly StyledProperty<int> CobwebStepsProperty =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, int>(nameof(CobwebSteps), 60);

    public static readonly StyledProperty<string> IfsPresetProperty =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, string>(nameof(IfsPreset), "barnsley");

    public static readonly StyledProperty<int> CatStepProperty =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, int>(nameof(CatStep), 0);

    public static readonly StyledProperty<bool> ShowEigenvectorRaysProperty =
        AvaloniaProperty.Register<DiscreteDynamicsCanvas, bool>(nameof(ShowEigenvectorRays), true);

    public string DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double LogisticR
    {
        get => GetValue(LogisticRProperty);
        set => SetValue(LogisticRProperty, value);
    }

    public double LogisticX0
    {
        get => GetValue(LogisticX0Property);
        set => SetValue(LogisticX0Property, value);
    }

    public int CobwebSteps
    {
        get => GetValue(CobwebStepsProperty);
        set => SetValue(CobwebStepsProperty, value);
    }

    public string IfsPreset
    {
        get => GetValue(IfsPresetProperty);
        set => SetValue(IfsPresetProperty, value);
    }

    public int CatStep
    {
        get => GetValue(CatStepProperty);
        set => SetValue(CatStepProperty, value);
    }

    public bool ShowEigenvectorRays
    {
        get => GetValue(ShowEigenvectorRaysProperty);
        set => SetValue(ShowEigenvectorRaysProperty, value);
    }

    private readonly DiscreteDynamicsEngine _engine = new();

    // Interaction state
    private bool _isDraggingX0 = false;
    private int _lastInternalCatStep = -1;

    // Typography
    private readonly Typeface _fontRegular = new(FontFamily.Default, FontStyle.Normal, FontWeight.Regular);
    private readonly Typeface _fontBold = new(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
    private readonly Typeface _fontCode = new("Consolas, Courier New, monospace");

    static DiscreteDynamicsCanvas()
    {
        AffectsRender<DiscreteDynamicsCanvas>(
            DisplayModeProperty,
            PresetNameProperty,
            LogisticRProperty,
            LogisticX0Property,
            CobwebStepsProperty,
            IfsPresetProperty,
            CatStepProperty,
            ShowEigenvectorRaysProperty);
    }

    public DiscreteDynamicsCanvas()
    {
        ClipToBounds = true;
        _engine.InitializeCatPattern();
    }

    private void SyncEngine()
    {
        _engine.LogisticR = LogisticR;
        _engine.LogisticX0 = LogisticX0;
        _engine.CobwebSteps = CobwebSteps;
        _engine.IfsPreset = IfsPreset;

        if (DisplayMode == "cobweb") _engine.Mode = DiscreteDynamicsMode.LogisticCobweb;
        else if (DisplayMode == "bifurcation") _engine.Mode = DiscreteDynamicsMode.LogisticBifurcation;
        else if (DisplayMode == "ifs") _engine.Mode = DiscreteDynamicsMode.IteratedFunctionSystems;
        else _engine.Mode = DiscreteDynamicsMode.ArnoldCatMap;

        // Sync CatStep if external binding changed
        if (CatStep != _lastInternalCatStep && CatStep >= 0 && CatStep < _engine.CatPeriod)
        {
            _engine.ResetCatMap();
            for (int i = 0; i < CatStep; i++)
            {
                _engine.StepCatMap();
            }
            _lastInternalCatStep = CatStep;
        }
    }

    public void StepCat()
    {
        _engine.StepCatMap();
        _lastInternalCatStep = _engine.CatStep;
        CatStep = _engine.CatStep;
        InvalidateVisual();
    }

    public void ResetCat()
    {
        _engine.ResetCatMap();
        _lastInternalCatStep = 0;
        CatStep = 0;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        if (props.IsLeftButtonPressed)
        {
            if (DisplayMode == "arnold")
            {
                // Clicking inside canvas steps Arnold Cat Map forward
                StepCat();
                e.Handled = true;
            }
            else if (DisplayMode == "cobweb")
            {
                // Drag x0 along bottom axis
                _isDraggingX0 = true;
                UpdateX0FromScreen(pt.X);
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDraggingX0 = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDraggingX0 && DisplayMode == "cobweb")
        {
            var pt = e.GetPosition(this);
            UpdateX0FromScreen(pt.X);
            e.Handled = true;
        }
    }

    private void UpdateX0FromScreen(double screenX)
    {
        double plotSize = Math.Min(Bounds.Width - 380 - 60, Bounds.Height - 140);
        double left = 40;
        double normX = Math.Clamp((screenX - left) / plotSize, 0.01, 0.99);
        LogisticX0 = normX;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        SyncEngine();

        var bounds = Bounds;
        if (bounds.Width < 50 || bounds.Height < 50) return;

        var tokens = new MathThemeTokens();
        context.FillRectangle(tokens.BgBrush, bounds);

        switch (_engine.Mode)
        {
            case DiscreteDynamicsMode.ArnoldCatMap:
                RenderArnoldCatMode(context, bounds, tokens);
                break;
            case DiscreteDynamicsMode.LogisticCobweb:
                RenderLogisticCobwebMode(context, bounds, tokens);
                break;
            case DiscreteDynamicsMode.LogisticBifurcation:
                RenderBifurcationMode(context, bounds, tokens);
                break;
            case DiscreteDynamicsMode.IteratedFunctionSystems:
                RenderIfsMode(context, bounds, tokens);
                break;
        }

        // Render top status capsule
        RenderTopPillHud(context, bounds, tokens);
    }

    #region Arnold Cat Map Rendering

    private void RenderArnoldCatMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double plotSize = Math.Min(bounds.Width - 380 - 60, bounds.Height - 140);
        double left = 40;
        double top = 80;
        var gridRect = new Rect(left, top, plotSize, plotSize);

        // Background
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 20, 16, 34)), gridRect);
        context.DrawRectangle(new Pen(tokens.AccentPurpleBrush, 2.0), gridRect);

        int n = DiscreteDynamicsEngine.CatGridSize;
        double cellSize = plotSize / n;
        var pattern = _engine.CurrentCatPattern;

        // Render cells
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                byte val = pattern[x, y];
                if (val > 20)
                {
                    double sx = left + x * cellSize;
                    double sy = top + y * cellSize;

                    // Neon turquoise/cyan glow based on intensity
                    byte r = (byte)(val * 0.1);
                    byte g = (byte)(val * 0.85);
                    byte b = (byte)(val);
                    var cellBrush = new SolidColorBrush(Color.FromRgb(r, g, b));
                    context.FillRectangle(cellBrush, new Rect(sx, sy, Math.Max(1.0, cellSize - 0.2), Math.Max(1.0, cellSize - 0.2)));
                }
            }
        }

        // Eigenvector rays v1 = <phi, 1> and v2 = <-1/phi, 1>
        if (ShowEigenvectorRays)
        {
            double phi = (1.0 + Math.Sqrt(5.0)) / 2.0; // 1.618
            double cx = left + plotSize * 0.5;
            double cy = top + plotSize * 0.5;

            var penUnstable = new Pen(new SolidColorBrush(Color.FromArgb(160, 244, 63, 94)), 1.5, DashStyle.Dash); // Rose
            var penStable = new Pen(new SolidColorBrush(Color.FromArgb(160, 6, 182, 212)), 1.5, DashStyle.Dash);   // Cyan

            // Unstable line (slope = 1/phi)
            double xSpan = plotSize * 0.45;
            context.DrawLine(penUnstable, new Point(cx - xSpan, cy + xSpan / phi), new Point(cx + xSpan, cy - xSpan / phi));
            DrawText(context, "v₁ (Stretching λ₁ ≈ 2.618)", cx + xSpan - 40, cy - xSpan / phi - 14, tokens.AccentRoseBrush, 10, true);

            // Stable line (slope = -phi)
            context.DrawLine(penStable, new Point(cx - xSpan / phi, cy - xSpan), new Point(cx + xSpan / phi, cy + xSpan));
            DrawText(context, "v₂ (Contracting λ₂ ≈ 0.382)", cx + xSpan / phi + 8, cy + xSpan - 10, tokens.AccentCyanBrush, 10, true);
        }

        // Recurrence progress bar under cat grid
        double barY = top + plotSize + 14;
        double barW = plotSize;
        double barH = 10;
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(100, 36, 31, 58)), new Rect(left, barY, barW, barH));
        double fillW = barW * ((double)_engine.CatStep / _engine.CatPeriod);
        context.FillRectangle(tokens.AccentEmeraldBrush, new Rect(left, barY, fillW, barH));
        context.DrawRectangle(new Pen(tokens.BorderBrush, 1.0), new Rect(left, barY, barW, barH));

        DrawText(context, $"Poincaré Recurrence Progress: {_engine.CatStep} / {_engine.CatPeriod} Steps ({(fillW / barW) * 100:F0}%)", left, barY + 14, tokens.TextSecondaryBrush, 11, true);

        // Theory card on right
        RenderArnoldTheoryCard(context, bounds, tokens);
    }

    private void RenderArnoldTheoryCard(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double cardW = 340;
        double cardX = bounds.Width - cardW - 20;
        double cardY = 70;
        double cardH = bounds.Height - 90;

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 24, 20, 39)), cardRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 168, 85, 247)), 1.5), cardRect);

        double ty = cardY + 16;
        DrawText(context, "ARNOLD'S CAT MAP (TORAL AUTOMORPHISM)", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 22;
        DrawText(context, "Г: [x', y']ᵀ = [2 1; 1 1] [x, y]ᵀ (mod 1)", cardX + 16, ty, tokens.AccentCyanBrush, 14, true);
        ty += 26;

        DrawText(context, "DYNAMICAL & ERGODIC PROPERTIES:", cardX + 16, ty, tokens.AccentPurpleBrush, 12, true);
        ty += 18;
        DrawText(context, "• Symplectic: det(A) = 2·1 - 1·1 = 1 (Area Preserved)", cardX + 16, ty, tokens.AccentEmeraldBrush, 11, true);
        ty += 16;
        DrawText(context, "• Trace tr(A) = 3  |  Hyperbolic matrix (|tr| > 2)", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, "• Eigenvalues: λ₁,₂ = (3 ± √5) / 2", cardX + 16, ty, tokens.TextPrimaryBrush, 11);
        ty += 16;
        DrawText(context, "  λ₁ ≈ 2.618 (Unstable) | λ₂ ≈ 0.382 (Stable)", cardX + 16, ty, tokens.AccentRoseBrush, 11, true);
        ty += 16;
        DrawText(context, "• Lyapunov Exponent λ = ln(λ₁) ≈ 0.9624 > 0", cardX + 16, ty, tokens.AccentAmberBrush, 11, true);
        ty += 16;
        DrawText(context, "• Eigenvector Slope = 1/φ ≈ 0.618 (Golden Ratio!)", cardX + 16, ty, tokens.AccentEmeraldBrush, 11, true);
        ty += 24;

        DrawText(context, "POINCARÉ RECURRENCE ON LATTICE:", cardX + 16, ty, tokens.AccentPurpleBrush, 12, true);
        ty += 18;
        DrawText(context, $"• Lattice Dimension N = {DiscreteDynamicsEngine.CatGridSize} × {DiscreteDynamicsEngine.CatGridSize}", cardX + 16, ty, tokens.TextMutedBrush, 11);
        ty += 16;
        DrawText(context, $"• Recurrence Period T(64) = {_engine.CatPeriod} steps", cardX + 16, ty, tokens.AccentCyanBrush, 11, true);
        ty += 16;
        DrawText(context, "• By Pigeonhole Principle, finite states must repeat", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Scrambled entropy reconstitutes to initial picture!", cardX + 16, ty, tokens.AccentEmeraldBrush, 10, true);
        ty += 24;

        DrawText(context, "INTERACTIVE CONTROLS:", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 18;
        DrawText(context, "• Left-Click Canvas: Advance 1 Step", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Step Button / Slider: Jump directly to any step", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Reset: Return to Step 0 pristine image", cardX + 16, ty, tokens.TextMutedBrush, 10);
    }

    #endregion

    #region Logistic Cobweb Rendering

    private void RenderLogisticCobwebMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double plotSize = Math.Min(bounds.Width - 380 - 60, bounds.Height - 140);
        double left = 40;
        double top = 80;
        var gridRect = new Rect(left, top, plotSize, plotSize);

        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 20, 16, 34)), gridRect);
        context.DrawRectangle(new Pen(tokens.AccentPurpleBrush, 1.5), gridRect);

        // Draw diagonal line y = x in dashed amber
        var penDiag = new Pen(new SolidColorBrush(Color.FromArgb(160, 245, 158, 11)), 1.5, DashStyle.Dash);
        context.DrawLine(penDiag, new Point(left, top + plotSize), new Point(left + plotSize, top));

        // Draw Logistic parabola y = r*x*(1-x) in neon cyan
        var penParabola = new Pen(new SolidColorBrush(Color.FromRgb(6, 182, 212)), 2.5);
        var penParabolaGlow = new Pen(new SolidColorBrush(Color.FromArgb(40, 6, 182, 212)), 6.0);

        var parabolaPoints = new List<Point>();
        int steps = 120;
        for (int i = 0; i <= steps; i++)
        {
            double x = (double)i / steps;
            double y = DiscreteDynamicsEngine.EvaluateLogistic(x, _engine.LogisticR);
            double sx = left + x * plotSize;
            double sy = top + (1.0 - y) * plotSize;
            parabolaPoints.Add(new Point(sx, sy));
        }

        DrawPolyline(context, penParabolaGlow, parabolaPoints);
        DrawPolyline(context, penParabola, parabolaPoints);

        // Draw Cobweb Orbit in neon rose
        var orbit = _engine.GenerateCobwebOrbit();
        if (orbit.Count > 1)
        {
            var orbitPoints = new List<Point>();
            foreach (var (ox, oy) in orbit)
            {
                double sx = left + ox * plotSize;
                double sy = top + (1.0 - oy) * plotSize;
                orbitPoints.Add(new Point(sx, sy));
            }

            var penCobweb = new Pen(new SolidColorBrush(Color.FromRgb(244, 63, 94)), 1.5);
            var penCobwebGlow = new Pen(new SolidColorBrush(Color.FromArgb(50, 244, 63, 94)), 4.0);
            DrawPolyline(context, penCobwebGlow, orbitPoints);
            DrawPolyline(context, penCobweb, orbitPoints);
        }

        // Draw Draggable Initial Point x0 on x-axis
        double x0Screen = left + _engine.LogisticX0 * plotSize;
        double y0Screen = top + plotSize;
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 244, 63, 94)), null, new Point(x0Screen, y0Screen), 10, 10);
        context.DrawEllipse(tokens.AccentRoseBrush, new Pen(new SolidColorBrush(Colors.White), 2.0), new Point(x0Screen, y0Screen), 6, 6);
        DrawText(context, $"x₀ = {_engine.LogisticX0:F3}", x0Screen - 16, y0Screen + 8, tokens.AccentRoseBrush, 11, true);

        // Theory card on right
        RenderLogisticTheoryCard(context, bounds, tokens);
    }

    private void RenderLogisticTheoryCard(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double cardW = 340;
        double cardX = bounds.Width - cardW - 20;
        double cardY = 70;
        double cardH = bounds.Height - 90;

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 24, 20, 39)), cardRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 168, 85, 247)), 1.5), cardRect);

        double ty = cardY + 16;
        DrawText(context, "LOGISTIC MAP & COBWEB DYNAMICS", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 22;
        DrawText(context, $"xₙ₊₁ = ({_engine.LogisticR:F3}) · xₙ · (1 - xₙ)", cardX + 16, ty, tokens.AccentCyanBrush, 15, true);
        ty += 28;

        double lambda = _engine.ComputeLyapunovExponent(_engine.LogisticR);
        string regime = lambda > 0.005 ? "DETERMINISTIC CHAOS" : (Math.Abs(lambda) < 0.005 ? "BIFURCATION BOUNDARY" : "PERIODIC ATTRACTOR");
        var regimeBrush = lambda > 0 ? tokens.AccentRoseBrush : tokens.AccentEmeraldBrush;

        DrawText(context, "LYAPUNOV EXPONENT λ(r):", cardX + 16, ty, tokens.AccentPurpleBrush, 12, true);
        ty += 20;
        DrawText(context, $"λ = {lambda:F4}  [{regime}]", cardX + 16, ty, regimeBrush, 13, true);
        ty += 18;
        DrawText(context, "• λ < 0: Stable periodic orbit (nearby orbits contract)", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• λ > 0: Sensitive dependence on initial conditions", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 24;

        DrawText(context, "FEIGENBAUM UNIVERSAL CONSTANTS:", cardX + 16, ty, tokens.TextSecondaryBrush, 12, true);
        ty += 18;
        DrawText(context, "• Period-Doubling Ratio δ ≈ 4.6692016", cardX + 16, ty, tokens.AccentAmberBrush, 11, true);
        ty += 16;
        DrawText(context, "• Scaling Factor α ≈ 2.5029078", cardX + 16, ty, tokens.AccentAmberBrush, 11, true);
        ty += 16;
        DrawText(context, "• Chaos Onset r_∞ ≈ 3.5699456", cardX + 16, ty, tokens.TextPrimaryBrush, 11);
        ty += 16;
        DrawText(context, "• Period 3 Window r ≈ 3.8284 (Li-Yorke Theorem)", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 24;

        DrawText(context, "INTERACTION CONTROLS:", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 18;
        DrawText(context, "• Drag Rose Handle on bottom axis to adjust x₀", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Parameter Slider r: 1.0 → 4.0", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "• Observe transition from fixed point to 2-cycle,", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 15;
        DrawText(context, "  4-cycle, and infinite chaos!", cardX + 16, ty, tokens.AccentEmeraldBrush, 10, true);
    }

    #endregion

    #region Bifurcation Tree Rendering

    private void RenderBifurcationMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double plotSize = Math.Min(bounds.Width - 380 - 60, bounds.Height - 140);
        double left = 40;
        double top = 80;
        var gridRect = new Rect(left, top, plotSize, plotSize);

        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 20, 16, 34)), gridRect);
        context.DrawRectangle(new Pen(tokens.AccentPurpleBrush, 1.5), gridRect);

        double rMin = 2.8, rMax = 4.0;
        var bifPoints = _engine.GenerateBifurcationDiagram(rMin, rMax, 180, 120, 40);

        var brushPt = new SolidColorBrush(Color.FromArgb(160, 6, 182, 212)); // Cyan dots
        foreach (var (r, x) in bifPoints)
        {
            double sx = left + ((r - rMin) / (rMax - rMin)) * plotSize;
            double sy = top + (1.0 - x) * plotSize;
            context.FillRectangle(brushPt, new Rect(sx, sy, 1.5, 1.5));
        }

        // Draw current r cursor line in rose
        if (_engine.LogisticR >= rMin && _engine.LogisticR <= rMax)
        {
            double curX = left + ((_engine.LogisticR - rMin) / (rMax - rMin)) * plotSize;
            var penCursor = new Pen(new SolidColorBrush(Color.FromArgb(200, 244, 63, 94)), 1.5, DashStyle.Dash);
            context.DrawLine(penCursor, new Point(curX, top), new Point(curX, top + plotSize));
            DrawText(context, $"r = {_engine.LogisticR:F2}", curX - 18, top - 16, tokens.AccentRoseBrush, 11, true);
        }

        // Theory card on right
        RenderLogisticTheoryCard(context, bounds, tokens);
    }

    #endregion

    #region Iterated Function Systems Rendering

    private void RenderIfsMode(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double plotSize = Math.Min(bounds.Width - 380 - 60, bounds.Height - 140);
        double left = 40;
        double top = 80;
        var gridRect = new Rect(left, top, plotSize, plotSize);

        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 18, 14, 30)), gridRect);
        context.DrawRectangle(new Pen(tokens.AccentPurpleBrush, 1.5), gridRect);

        var points = _engine.GenerateIfsPoints();

        // Palette for transforms
        var brushes = new[]
        {
            new SolidColorBrush(Color.FromArgb(220, 245, 158, 11)), // Amber (stem)
            new SolidColorBrush(Color.FromArgb(200, 16, 185, 129)), // Vibrant emerald
            new SolidColorBrush(Color.FromArgb(200, 6, 182, 212)),  // Neon cyan
            new SolidColorBrush(Color.FromArgb(200, 244, 63, 94))   // Rose
        };

        if (_engine.IfsPreset == "sierpinski")
        {
            foreach (var (x, y, idx) in points)
            {
                double sx = left + x * plotSize;
                double sy = top + (1.0 - y) * plotSize;
                var b = brushes[idx % brushes.Length];
                context.FillRectangle(b, new Rect(sx, sy, 1.5, 1.5));
            }
        }
        else // Barnsley Fern
        {
            // Coordinate mapping: x in [-2.5, 2.8], y in [0, 10.2]
            double xMin = -2.6, xMax = 2.8;
            double yMin = 0.0, yMax = 10.2;

            foreach (var (x, y, idx) in points)
            {
                double sx = left + ((x - xMin) / (xMax - xMin)) * plotSize;
                double sy = top + (1.0 - (y - yMin) / (yMax - yMin)) * plotSize;
                var b = brushes[idx % brushes.Length];
                context.FillRectangle(b, new Rect(sx, sy, 1.4, 1.4));
            }
        }

        // Theory card on right
        RenderIfsTheoryCard(context, bounds, tokens);
    }

    private void RenderIfsTheoryCard(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double cardW = 340;
        double cardX = bounds.Width - cardW - 20;
        double cardY = 70;
        double cardH = bounds.Height - 90;

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(235, 24, 20, 39)), cardRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 168, 85, 247)), 1.5), cardRect);

        double ty = cardY + 16;
        DrawText(context, "ITERATED FUNCTION SYSTEMS (IFS)", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 22;
        string name = _engine.IfsPreset == "sierpinski" ? "Sierpinski Gasket / Triangle" : "Michael Barnsley's Fractal Fern";
        DrawText(context, name, cardX + 16, ty, tokens.AccentEmeraldBrush, 14, true);
        ty += 26;

        DrawText(context, "HUTCHINSON OPERATOR & ATTRACTOR:", cardX + 16, ty, tokens.AccentPurpleBrush, 12, true);
        ty += 18;
        DrawText(context, "• Contractive Affine Maps: wᵢ(x) = Aᵢ x + bᵢ", cardX + 16, ty, tokens.TextPrimaryBrush, 11);
        ty += 16;
        DrawText(context, "• Hutchinson Fixed Point: W(K) = ⋃ wᵢ(K) = K", cardX + 16, ty, tokens.AccentCyanBrush, 11, true);
        ty += 16;
        DrawText(context, "• Generated via Chaos Game with probability pᵢ", cardX + 16, ty, tokens.TextMutedBrush, 10);
        ty += 24;

        DrawText(context, "HAUSDORFF FRACTAL DIMENSION:", cardX + 16, ty, tokens.TextSecondaryBrush, 12, true);
        ty += 18;
        if (_engine.IfsPreset == "sierpinski")
        {
            DrawText(context, "• D = ln(3) / ln(2) ≈ 1.58496", cardX + 16, ty, tokens.AccentAmberBrush, 13, true);
            ty += 16;
            DrawText(context, "• 3 self-similar copies with scale factor ½", cardX + 16, ty, tokens.TextMutedBrush, 10);
        }
        else
        {
            DrawText(context, "• Hausdorff Dimension D ≈ 1.82", cardX + 16, ty, tokens.AccentAmberBrush, 13, true);
            ty += 16;
            DrawText(context, "• 4 affine transforms modeling real botanical morphology", cardX + 16, ty, tokens.TextMutedBrush, 10);
        }
        ty += 24;

        DrawText(context, "COLOR CODING BY TRANSFORM:", cardX + 16, ty, tokens.TextSecondaryBrush, 11, true);
        ty += 16;
        DrawText(context, "■ Amber: Stem base (p = 0.01)", cardX + 16, ty, tokens.AccentAmberBrush, 10);
        ty += 15;
        DrawText(context, "■ Emerald: Leaflet hierarchy (p = 0.85)", cardX + 16, ty, tokens.AccentEmeraldBrush, 10);
        ty += 15;
        DrawText(context, "■ Cyan: Left pinna leaflet (p = 0.07)", cardX + 16, ty, tokens.AccentCyanBrush, 10);
        ty += 15;
        DrawText(context, "■ Rose: Right pinna leaflet (p = 0.07)", cardX + 16, ty, tokens.AccentRoseBrush, 10);
    }

    #endregion

    private void RenderTopPillHud(DrawingContext context, Rect bounds, MathThemeTokens tokens)
    {
        double hudW = bounds.Width - 60;
        double hudH = 34;
        double hudX = 30;
        double hudY = 16;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 24, 20, 39)), hudRect);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(90, 6, 182, 212)), 1.5), hudRect);

        string hudText;
        if (DisplayMode == "arnold")
        {
            hudText = $"Arnold's Cat Map: [x', y']ᵀ = [2 1; 1 1][x, y]ᵀ (mod 1)  |  det(A) = 1 (Symplectic)  |  Step {_engine.CatStep} / {_engine.CatPeriod}  |  Lyapunov λ = ln((3+√5)/2) ≈ 0.962";
        }
        else if (DisplayMode == "cobweb")
        {
            double lambda = _engine.ComputeLyapunovExponent(_engine.LogisticR);
            hudText = $"Logistic Map Cobweb: xₙ₊₁ = ({_engine.LogisticR:F2}) xₙ(1 - xₙ)  |  x₀ = {_engine.LogisticX0:F3}  |  Lyapunov Exponent λ = {lambda:F3}  |  {(lambda > 0 ? "Chaotic Regime" : "Stable Attractor")}";
        }
        else if (DisplayMode == "bifurcation")
        {
            hudText = $"Feigenbaum Bifurcation Tree: r ∈ [2.8, 4.0]  |  Period-Doubling Cascade  |  Universal Constants: δ ≈ 4.669, α ≈ 2.503";
        }
        else
        {
            hudText = $"Iterated Function Systems (IFS): {(_engine.IfsPreset == "sierpinski" ? "Sierpinski Gasket (D ≈ 1.585)" : "Barnsley Fern (D ≈ 1.82)")}  |  Hutchinson Fixed Point W(K) = K";
        }

        DrawText(context, hudText, hudX + 16, hudY + 8, tokens.AccentCyanBrush, 12, true);
    }

    private static void DrawPolyline(DrawingContext context, Pen pen, List<Point> points)
    {
        if (points.Count < 2) return;
        var geom = new StreamGeometry();
        using (var sgc = geom.Open())
        {
            sgc.BeginFigure(points[0], false);
            for (int i = 1; i < points.Count; i++)
            {
                sgc.LineTo(points[i]);
            }
            sgc.EndFigure(false);
        }
        context.DrawGeometry(null, pen, geom);
    }

    private void DrawText(DrawingContext context, string text, double x, double y, IBrush brush, double fontSize, bool bold = false)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            bold ? _fontBold : _fontRegular,
            fontSize,
            brush);
        context.DrawText(formatted, new Point(x, y));
    }
}
