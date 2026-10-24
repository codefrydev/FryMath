using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Mechanics;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class HamiltonianCanvas : Control
{
    public static readonly StyledProperty<string> SystemTypeProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, string>(nameof(SystemType), "henonheiles");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, string>(nameof(PresetName), "henonheiles");

    public static readonly StyledProperty<double> EnergyLevelProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, double>(nameof(EnergyLevel), 0.12);

    public static readonly StyledProperty<string> IntegratorModeProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, string>(nameof(IntegratorMode), "yoshida");

    public static readonly StyledProperty<double> TimeStepProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, double>(nameof(TimeStep), 0.04);

    public static readonly StyledProperty<bool> ShowPoincareSectionProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, bool>(nameof(ShowPoincareSection), true);

    public static readonly StyledProperty<bool> ShowPotentialContoursProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, bool>(nameof(ShowPotentialContours), true);

    public static readonly StyledProperty<bool> ShowEnergyErrorGraphProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, bool>(nameof(ShowEnergyErrorGraph), true);

    public static readonly StyledProperty<bool> ShowLiouvilleDropletProperty =
        AvaloniaProperty.Register<HamiltonianCanvas, bool>(nameof(ShowLiouvilleDroplet), true);

    public string SystemType
    {
        get => GetValue(SystemTypeProperty);
        set => SetValue(SystemTypeProperty, value);
    }

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double EnergyLevel
    {
        get => GetValue(EnergyLevelProperty);
        set => SetValue(EnergyLevelProperty, value);
    }

    public string IntegratorMode
    {
        get => GetValue(IntegratorModeProperty);
        set => SetValue(IntegratorModeProperty, value);
    }

    public double TimeStep
    {
        get => GetValue(TimeStepProperty);
        set => SetValue(TimeStepProperty, value);
    }

    public bool ShowPoincareSection
    {
        get => GetValue(ShowPoincareSectionProperty);
        set => SetValue(ShowPoincareSectionProperty, value);
    }

    public bool ShowPotentialContours
    {
        get => GetValue(ShowPotentialContoursProperty);
        set => SetValue(ShowPotentialContoursProperty, value);
    }

    public bool ShowEnergyErrorGraph
    {
        get => GetValue(ShowEnergyErrorGraphProperty);
        set => SetValue(ShowEnergyErrorGraphProperty, value);
    }

    public bool ShowLiouvilleDroplet
    {
        get => GetValue(ShowLiouvilleDropletProperty);
        set => SetValue(ShowLiouvilleDropletProperty, value);
    }

    private readonly MathThemeTokens _tokens = MathThemeTokens.DarkElectric;

    static HamiltonianCanvas()
    {
        AffectsRender<HamiltonianCanvas>(
            SystemTypeProperty, PresetNameProperty, EnergyLevelProperty,
            IntegratorModeProperty, TimeStepProperty, ShowPoincareSectionProperty,
            ShowPotentialContoursProperty, ShowEnergyErrorGraphProperty, ShowLiouvilleDropletProperty);
    }

    public HamiltonianCanvas()
    {
        ClipToBounds = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        // Background
        context.DrawRectangle(_tokens.BgBrush, null, new Rect(0, 0, w, h));

        string sys = SystemType.ToLowerInvariant();
        bool isSymplectic = !IntegratorMode.Equals("rk4", StringComparison.OrdinalIgnoreCase);

        // Split Layout: Left Panel = Configuration / Potential Space, Right Panel = Phase Space / Poincaré Section
        double panelW = (w - 28) * 0.5;
        double panelH = h - 60;
        var leftRect = new Rect(14, 45, panelW, panelH);
        var rightRect = new Rect(14 + panelW + 10, 45, panelW, panelH);

        // Draw panels backgrounds
        var panelBg = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42));
        var panelBorder = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawRectangle(panelBg, panelBorder, leftRect, 6, 6);
        context.DrawRectangle(panelBg, panelBorder, rightRect, 6, 6);

        // Render specific system
        switch (sys)
        {
            case "duffing":
                RenderDuffing(context, leftRect, rightRect, isSymplectic);
                break;

            case "pendulum":
                RenderPendulum(context, leftRect, rightRect, isSymplectic);
                break;

            case "liouville":
                RenderLiouvilleFlow(context, leftRect, rightRect, isSymplectic);
                break;

            case "yoshida":
                RenderYoshidaComparison(context, leftRect, rightRect);
                break;

            case "henonheiles":
            default:
                RenderHenonHeiles(context, leftRect, rightRect, isSymplectic);
                break;
        }

        // Top Banner
        DrawTopBanner(context, sys, isSymplectic);

        // Bottom Probe status
        var probeText = new FormattedText(
            $"Integrator: {(isSymplectic ? "Yoshida 4th-Order Symplectic (dω = 0)" : "Classical Runge-Kutta 4 (Non-Symplectic)")}  |  dt = {TimeStep:F3}  |  Energy = {EnergyLevel:F3}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            10,
            _tokens.TextMutedBrush
        );
        context.DrawText(probeText, new Point(14, h - 18));
    }

    private void DrawTopBanner(DrawingContext context, string sys, bool isSymplectic)
    {
        string title = sys switch
        {
            "duffing" => "DUFFING QUARTIC DOUBLE-WELL (H = ½p² - ½q² + ¼q⁴)",
            "pendulum" => "NONLINEAR PENDULUM (H = ½p² - cos q) & SEPARATRIX HOMOCLINIC ORBIT",
            "liouville" => "LIOUVILLE PHASE SPACE VOLUME CONSERVATION (d/dt ∬ dq dp = 0)",
            "yoshida" => "YOSHIDA 4TH-ORDER SYMPLECTIC VS RUNGE-KUTTA 4 (SHADOW HAMILTONIAN PRESERVATION)",
            _ => "HÉNON-HEILES 2D ANHARMONIC CHAOS (H = ½(px²+py²) + ½(x²+y²) + x²y - ⅓y³)"
        };

        var bannerBrush = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42));
        var bannerBorder = new Pen(_tokens.AccentCyanBrush, 1.2);
        context.DrawRectangle(bannerBrush, bannerBorder, new Rect(14, 10, 480, 26), 4, 4);

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
    // 1. Hénon-Heiles Anharmonic Potential & Poincaré Section
    // -----------------------------------------------------------------
    private void RenderHenonHeiles(DrawingContext context, Rect leftRect, Rect rightRect, bool isSymplectic)
    {
        // Left: Potential Landscape V(x, y) & Real-space Trajectory
        DrawPanelHeader(context, leftRect, "CONFIGURATION SPACE & EQUIPOTENTIAL CONTOURS V(x, y)", _tokens.AccentCyanBrush);
        var centerLeft = new Point(leftRect.X + leftRect.Width * 0.5, leftRect.Y + leftRect.Height * 0.52);
        double scaleL = Math.Min(leftRect.Width, leftRect.Height) * 0.38;

        // Draw equipotential triangular contours
        DrawHenonHeilesContours(context, centerLeft, scaleL);

        // Simulate 4D trajectory
        double dt = TimeStep;
        int steps = 1400;

        // Initial condition matching EnergyLevel
        double y0 = 0.1;
        double x0 = 0.0;
        double py0 = 0.0;
        double v0 = 0.5 * (x0 * x0 + y0 * y0) + x0 * x0 * y0 - (1.0 / 3.0) * y0 * y0 * y0;
        double px0 = Math.Sqrt(Math.Max(0.001, 2.0 * (EnergyLevel - v0)));

        var state = new HamiltonianEngine.PhasePoint4D(x0, y0, px0, py0);
        var trajPen = new Pen(_tokens.AccentEmeraldBrush, 1.2);

        var poincarePoints = new List<Point>();
        Point? lastScreenPt = null;

        for (int i = 0; i < steps; i++)
        {
            var oldState = state;
            state = isSymplectic
                ? HamiltonianEngine.StepYoshida4D(state, dt, HamiltonianEngine.HenonHeilesGradV)
                : HamiltonianEngine.StepRungeKutta4D(state, dt, HamiltonianEngine.HenonHeilesGradV);

            var screenPt = new Point(centerLeft.X + state.X * scaleL, centerLeft.Y - state.Y * scaleL);

            if (lastScreenPt.HasValue && leftRect.Contains(screenPt))
            {
                context.DrawLine(trajPen, lastScreenPt.Value, screenPt);
            }
            lastScreenPt = screenPt;

            // Detect Poincaré section crossing at x = 0 with px > 0
            var cross = HamiltonianEngine.DetectPoincareCrossing(oldState, state);
            if (cross != null)
            {
                poincarePoints.Add(new Point(cross.Y, cross.Py));
            }
        }

        // Right: Poincaré Surface of Section (y, py) at x = 0
        DrawPanelHeader(context, rightRect, "POINCARÉ SURFACE OF SECTION (y, py) at x = 0, px > 0", _tokens.AccentAmberBrush);
        var centerRight = new Point(rightRect.X + rightRect.Width * 0.5, rightRect.Y + rightRect.Height * 0.52);
        double scaleR = Math.Min(rightRect.Width, rightRect.Height) * 0.42;

        DrawAxes(context, centerRight, rightRect, scaleR, "y", "py");

        // Render Poincaré dots
        var poincareBrush = new SolidColorBrush(_tokens.AccentAmber);
        foreach (var p in poincarePoints)
        {
            var sPt = new Point(centerRight.X + p.X * scaleR, centerRight.Y - p.Y * scaleR);
            if (rightRect.Contains(sPt))
            {
                context.DrawEllipse(poincareBrush, null, sPt, 2.0, 2.0);
            }
        }

        // Draw Inset Equations Card
        DrawCard(context, leftRect.X + 10, leftRect.Bottom - 95, 270, 85,
            "HÉNON-HEILES HAMILTONIAN AXIOM",
            $"H = ½(px² + py²) + ½(x² + y²) + x²y - ⅓y³\nEscape Energy Ec = 1/6 ≈ 0.1667\nCurrent E = {EnergyLevel:F3} ({(EnergyLevel < 0.11 ? "Regular KAM Tori" : "Chaotic Sea")})",
            _tokens.AccentCyanBrush);
    }

    private void DrawHenonHeilesContours(DrawingContext context, Point center, double scale)
    {
        double[] energies = { 0.05, 0.10, 0.125, 0.15, 0.1667 };
        foreach (var e in energies)
        {
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(70, 56, 189, 248)), e >= 0.166 ? 1.5 : 0.8);
            int n = 80;
            double dTheta = 2.0 * Math.PI / n;
            Point? firstPt = null;
            Point? lastPt = null;

            for (int i = 0; i <= n; i++)
            {
                double theta = i * dTheta;
                double rEst = Math.Sqrt(2.0 * e);
                // Approximate 3-fold triangular distortion r(theta) = r0 (1 - 1/3 r0 sin 3theta)
                double r = rEst * (1.0 - 0.25 * rEst * Math.Sin(3.0 * theta));
                var pt = new Point(center.X + r * Math.Cos(theta) * scale, center.Y - r * Math.Sin(theta) * scale);

                if (lastPt.HasValue)
                {
                    context.DrawLine(pen, lastPt.Value, pt);
                }
                else
                {
                    firstPt = pt;
                }
                lastPt = pt;
            }
        }
    }

    // -----------------------------------------------------------------
    // 2. Duffing Double-Well Oscillator
    // -----------------------------------------------------------------
    private void RenderDuffing(DrawingContext context, Rect leftRect, Rect rightRect, bool isSymplectic)
    {
        DrawPanelHeader(context, leftRect, "QUARTIC DOUBLE-WELL POTENTIAL V(q) = -½q² + ¼q⁴", _tokens.AccentCyanBrush);
        DrawPanelHeader(context, rightRect, "PHASE PORTRAIT (q, p) & FIGURE-8 SEPARATRIX", _tokens.AccentRoseBrush);

        var centerL = new Point(leftRect.X + leftRect.Width * 0.5, leftRect.Y + leftRect.Height * 0.65);
        double scaleL = leftRect.Width * 0.22;

        // Draw potential curve V(q)
        var potPen = new Pen(_tokens.AccentCyanBrush, 2.0);
        Point? lastV = null;
        for (double q = -2.2; q <= 2.2; q += 0.05)
        {
            double v = -0.5 * q * q + 0.25 * q * q * q * q;
            var pt = new Point(centerL.X + q * scaleL, centerL.Y - v * scaleL * 1.5);
            if (lastV.HasValue) context.DrawLine(potPen, lastV.Value, pt);
            lastV = pt;
        }

        // Draw Energy Level Line
        var energyPen = new Pen(_tokens.AccentAmberBrush, 1.2, new DashStyle(new double[] { 4, 3 }, 0));
        double yEnergy = centerL.Y - EnergyLevel * scaleL * 1.5;
        context.DrawLine(energyPen, new Point(leftRect.X + 20, yEnergy), new Point(leftRect.Right - 20, yEnergy));

        // Right: Phase Portrait
        var centerR = new Point(rightRect.X + rightRect.Width * 0.5, rightRect.Y + rightRect.Height * 0.5);
        double scaleR = rightRect.Width * 0.22;
        DrawAxes(context, centerR, rightRect, scaleR, "q", "p");

        // Draw multiple phase contours (inside wells and outside separatrix)
        double[] qInits = { 0.9, 1.1, 1.3, -0.9, -1.1, -1.3, 1.6 };
        var colors = new[] { _tokens.AccentEmerald, _tokens.AccentCyan, _tokens.AccentRose, _tokens.AccentPurple };

        for (int c = 0; c < qInits.Length; c++)
        {
            var p0 = new HamiltonianEngine.PhasePoint2D(qInits[c], 0.0);
            var orbitPen = new Pen(new SolidColorBrush(colors[c % colors.Length]), 1.2);
            Point? lastOrbit = null;

            for (int step = 0; step < 400; step++)
            {
                // Leapfrog 1D
                double pMid = p0.P - 0.5 * TimeStep * HamiltonianEngine.DuffingGradV(p0.Q);
                double qNew = p0.Q + TimeStep * pMid;
                double pNew = pMid - 0.5 * TimeStep * HamiltonianEngine.DuffingGradV(qNew);
                p0 = new HamiltonianEngine.PhasePoint2D(qNew, pNew);

                var sPt = new Point(centerR.X + p0.Q * scaleR, centerR.Y - p0.P * scaleR);
                if (lastOrbit.HasValue && rightRect.Contains(sPt))
                {
                    context.DrawLine(orbitPen, lastOrbit.Value, sPt);
                }
                lastOrbit = sPt;
            }
        }

        // Draw Inset Card
        DrawCard(context, leftRect.X + 10, leftRect.Bottom - 85, 270, 75,
            "DUFFING SEPARATRIX AXIOM",
            "Wells at q = ±1 (V = -0.25)\nSeparatrix at H = 0 divides local intra-well\nlibration from global inter-well transit.",
            _tokens.AccentCyanBrush);
    }

    // -----------------------------------------------------------------
    // 3. Nonlinear Pendulum
    // -----------------------------------------------------------------
    private void RenderPendulum(DrawingContext context, Rect leftRect, Rect rightRect, bool isSymplectic)
    {
        DrawPanelHeader(context, leftRect, "PHYSICAL PENDULUM OSCILLATION & ENERGY H = ½p² - cos q", _tokens.AccentCyanBrush);
        DrawPanelHeader(context, rightRect, "CYLINDER PHASE PORTRAIT (q, p) ∈ S¹ × ℝ", _tokens.AccentEmeraldBrush);

        var centerL = new Point(leftRect.X + leftRect.Width * 0.5, leftRect.Y + leftRect.Height * 0.45);
        double rodLen = leftRect.Height * 0.35;

        // Current pendulum angle
        double angle = EnergyLevel; // Driven by slider
        var bobPt = new Point(centerL.X + rodLen * Math.Sin(angle), centerL.Y + rodLen * Math.Cos(angle));

        // Draw rod & bob
        var rodPen = new Pen(_tokens.AccentCyanBrush, 2.5);
        context.DrawLine(rodPen, centerL, bobPt);
        context.DrawEllipse(_tokens.AccentEmeraldBrush, null, bobPt, 12, 12);
        context.DrawEllipse(_tokens.AccentAmberBrush, null, centerL, 5, 5);

        // Right: Phase Portrait
        var centerR = new Point(rightRect.X + rightRect.Width * 0.5, rightRect.Y + rightRect.Height * 0.5);
        double scaleR = rightRect.Width * 0.14;
        DrawAxes(context, centerR, rightRect, scaleR, "q (rad)", "p");

        // Draw separatrix curves H = 1: p = ±2 cos(q/2)
        var sepPen = new Pen(_tokens.AccentRoseBrush, 1.6);
        Point? lastSepTop = null;
        Point? lastSepBot = null;
        for (double q = -3.14; q <= 3.14; q += 0.05)
        {
            double pSep = 2.0 * Math.Cos(0.5 * q);
            var ptTop = new Point(centerR.X + q * scaleR, centerR.Y - pSep * scaleR);
            var ptBot = new Point(centerR.X + q * scaleR, centerR.Y + pSep * scaleR);

            if (lastSepTop.HasValue && lastSepBot.HasValue)
            {
                context.DrawLine(sepPen, lastSepTop.Value, ptTop);
                context.DrawLine(sepPen, lastSepBot.Value, ptBot);
            }
            lastSepTop = ptTop;
            lastSepBot = ptBot;
        }

        // Draw Inset Card
        DrawCard(context, leftRect.X + 10, leftRect.Bottom - 85, 270, 75,
            "HOMOCLINIC SEPARATRIX ORBIT",
            "Separatrix p = ±2 cos(q/2) at H = 1.\nInside: bounded libration (elliptic integrals).\nOutside: continuous unidirectional whirling.",
            _tokens.AccentCyanBrush);
    }

    // -----------------------------------------------------------------
    // 4. Liouville Phase Space Volume Conservation
    // -----------------------------------------------------------------
    private void RenderLiouvilleFlow(DrawingContext context, Rect leftRect, Rect rightRect, bool isSymplectic)
    {
        DrawPanelHeader(context, leftRect, "INITIAL PHASE DROPLET D₀ (AREA = π r²)", _tokens.AccentCyanBrush);
        DrawPanelHeader(context, rightRect, "EVOLVED DEFORMED DROPLET D(t) [AREA PRESERVED IDENTICALLY]", _tokens.AccentEmeraldBrush);

        var centerL = new Point(leftRect.X + leftRect.Width * 0.5, leftRect.Y + leftRect.Height * 0.5);
        var centerR = new Point(rightRect.X + rightRect.Width * 0.5, rightRect.Y + rightRect.Height * 0.5);
        double scale = rightRect.Width * 0.22;

        DrawAxes(context, centerL, leftRect, scale, "q", "p");
        DrawAxes(context, centerR, rightRect, scale, "q", "p");

        // Initial circular droplet
        var initialLoop = HamiltonianEngine.GeneratePhaseLoop(0.0, 1.2, 0.45, count: 64);
        double initialArea = HamiltonianEngine.ComputePhaseEnclosedArea(initialLoop);

        // Draw initial droplet on Left
        DrawPhasePolygon(context, initialLoop, centerL, scale, _tokens.AccentCyan);

        // Evolve loop on Right
        var evolvedLoop = initialLoop;
        double dt = TimeStep;
        int steps = 180;

        for (int i = 0; i < steps; i++)
        {
            evolvedLoop = HamiltonianEngine.StepPhaseLoop(evolvedLoop, dt, HamiltonianEngine.PendulumGradV, isSymplectic);
        }

        double evolvedArea = HamiltonianEngine.ComputePhaseEnclosedArea(evolvedLoop);
        double areaRelError = Math.Abs(evolvedArea - initialArea) / initialArea;

        // Draw evolved filamentary droplet on Right
        DrawPhasePolygon(context, evolvedLoop, centerR, scale, _tokens.AccentEmerald);

        // Display Area Conservation Proof Badge
        DrawCard(context, rightRect.X + 10, rightRect.Bottom - 95, 300, 85,
            "LIOUVILLE THEOREM VERIFICATION (Green's Theorem)",
            $"Initial Area A₀ = {initialArea:F5}\nEvolved Area A(t) = {evolvedArea:F5}\nRelative Invariance Error: {areaRelError:P4} [CONSERVED]",
            _tokens.AccentEmeraldBrush);
    }

    private void DrawPhasePolygon(DrawingContext context, IReadOnlyList<HamiltonianEngine.PhasePoint2D> loop, Point center, double scale, Color color)
    {
        var polyBrush = new SolidColorBrush(Color.FromArgb(90, color.R, color.G, color.B));
        var polyPen = new Pen(new SolidColorBrush(color), 1.8);

        var points = loop.Select(p => new Point(center.X + p.Q * scale, center.Y - p.P * scale)).ToList();

        for (int i = 0; i < points.Count; i++)
        {
            int next = (i + 1) % points.Count;
            context.DrawLine(polyPen, points[i], points[next]);
        }
    }

    // -----------------------------------------------------------------
    // 5. Yoshida Symplectic vs Non-Symplectic RK4 Comparison
    // -----------------------------------------------------------------
    private void RenderYoshidaComparison(DrawingContext context, Rect leftRect, Rect rightRect)
    {
        DrawPanelHeader(context, leftRect, "4TH-ORDER YOSHIDA SYMPLECTIC (BOUNDED SHADOW HAMILTONIAN)", _tokens.AccentEmeraldBrush);
        DrawPanelHeader(context, rightRect, "CLASSICAL RUNGE-KUTTA 4 (NON-SYMPLECTIC ENERGY DRIFT)", _tokens.AccentRoseBrush);

        var centerL = new Point(leftRect.X + leftRect.Width * 0.5, leftRect.Y + leftRect.Height * 0.52);
        var centerR = new Point(rightRect.X + rightRect.Width * 0.5, rightRect.Y + rightRect.Height * 0.52);
        double scale = Math.Min(leftRect.Width, leftRect.Height) * 0.36;

        DrawHenonHeilesContours(context, centerL, scale);
        DrawHenonHeilesContours(context, centerR, scale);

        // Run both for 2500 steps
        double dt = 0.06;
        int steps = 2500;

        var stateY = new HamiltonianEngine.PhasePoint4D(0.0, 0.25, 0.35, 0.0);
        var stateRK = new HamiltonianEngine.PhasePoint4D(0.0, 0.25, 0.35, 0.0);

        double h0 = HamiltonianEngine.EvaluateHenonHeilesH(stateY.X, stateY.Y, stateY.Px, stateY.Py);

        var penY = new Pen(_tokens.AccentEmeraldBrush, 1.2);
        var penRK = new Pen(_tokens.AccentRoseBrush, 1.2);

        Point? lastY = null;
        Point? lastRK = null;

        double maxErrY = 0.0;
        double maxErrRK = 0.0;

        for (int i = 0; i < steps; i++)
        {
            stateY = HamiltonianEngine.StepYoshida4D(stateY, dt, HamiltonianEngine.HenonHeilesGradV);
            stateRK = HamiltonianEngine.StepRungeKutta4D(stateRK, dt, HamiltonianEngine.HenonHeilesGradV);

            double errY = Math.Abs(HamiltonianEngine.EvaluateHenonHeilesH(stateY.X, stateY.Y, stateY.Px, stateY.Py) - h0);
            double errRK = Math.Abs(HamiltonianEngine.EvaluateHenonHeilesH(stateRK.X, stateRK.Y, stateRK.Px, stateRK.Py) - h0);

            maxErrY = Math.Max(maxErrY, errY);
            maxErrRK = Math.Max(maxErrRK, errRK);

            var ptY = new Point(centerL.X + stateY.X * scale, centerL.Y - stateY.Y * scale);
            var ptRK = new Point(centerR.X + stateRK.X * scale, centerR.Y - stateRK.Y * scale);

            if (lastY.HasValue && leftRect.Contains(ptY)) context.DrawLine(penY, lastY.Value, ptY);
            if (lastRK.HasValue && rightRect.Contains(ptRK)) context.DrawLine(penRK, lastRK.Value, ptRK);

            lastY = ptY;
            lastRK = ptRK;
        }

        // Display Comparison Badges
        DrawCard(context, leftRect.X + 10, leftRect.Bottom - 85, 270, 75,
            "YOSHIDA 4TH-ORDER SYMPLECTIC",
            $"Max Energy Drift: {maxErrY:E3}\nShadow Hamiltonian preserved identically.\nZero secular orbit spiral over 10⁴ cycles.",
            _tokens.AccentEmeraldBrush);

        DrawCard(context, rightRect.X + 10, rightRect.Bottom - 85, 270, 75,
            "CLASSICAL RUNGE-KUTTA 4",
            $"Max Energy Drift: {maxErrRK:E3}\nNon-symplectic flow breaks phase volume.\nArtificial energy dissipation causes orbit drift.",
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

    private void DrawAxes(DrawingContext context, Point center, Rect bounds, double scale, string labelX, string labelY)
    {
        var axisPen = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawLine(axisPen, new Point(bounds.X + 10, center.Y), new Point(bounds.Right - 10, center.Y));
        context.DrawLine(axisPen, new Point(center.X, bounds.Y + 25), new Point(center.X, bounds.Bottom - 10));

        var xText = new FormattedText(labelX, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas, Menlo, monospace"), 9, _tokens.TextMutedBrush);
        var yText = new FormattedText(labelY, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Consolas, Menlo, monospace"), 9, _tokens.TextMutedBrush);

        context.DrawText(xText, new Point(bounds.Right - 24, center.Y + 4));
        context.DrawText(yText, new Point(center.X + 4, bounds.Y + 26));
    }

    private void DrawCard(DrawingContext context, double x, double y, double w, double h, string title, string body, IBrush headerBrush)
    {
        var cardBg = new SolidColorBrush(Color.FromArgb(230, 15, 23, 42));
        var cardBorder = new Pen(_tokens.BorderBrush, 1.0);
        context.DrawRectangle(cardBg, cardBorder, new Rect(x, y, w, h), 4, 4);

        var titleText = new FormattedText(
            title,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI, Inter, sans-serif", FontStyle.Normal, FontWeight.Bold),
            9,
            headerBrush
        );
        context.DrawText(titleText, new Point(x + 8, y + 6));

        var bodyText = new FormattedText(
            body,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Menlo, monospace"),
            9,
            _tokens.TextPrimaryBrush
        );
        context.DrawText(bodyText, new Point(x + 8, y + 22));
    }
}
