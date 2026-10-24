using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Lagrangian;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class LagrangianCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<LagrangianCanvas, string>(nameof(PresetName), "double");

    public static readonly StyledProperty<double> L1Property =
        AvaloniaProperty.Register<LagrangianCanvas, double>(nameof(L1), 1.0);

    public static readonly StyledProperty<double> L2Property =
        AvaloniaProperty.Register<LagrangianCanvas, double>(nameof(L2), 0.9);

    public static readonly StyledProperty<double> M1Property =
        AvaloniaProperty.Register<LagrangianCanvas, double>(nameof(M1), 1.0);

    public static readonly StyledProperty<double> M2Property =
        AvaloniaProperty.Register<LagrangianCanvas, double>(nameof(M2), 0.8);

    public static readonly StyledProperty<double> GravityProperty =
        AvaloniaProperty.Register<LagrangianCanvas, double>(nameof(Gravity), 9.81);

    public static readonly StyledProperty<double> DampingProperty =
        AvaloniaProperty.Register<LagrangianCanvas, double>(nameof(Damping), 0.0);

    public static readonly StyledProperty<bool> ShowShadowProperty =
        AvaloniaProperty.Register<LagrangianCanvas, bool>(nameof(ShowShadow), true);

    public static readonly StyledProperty<bool> ShowTrailsProperty =
        AvaloniaProperty.Register<LagrangianCanvas, bool>(nameof(ShowTrails), true);

    public static readonly StyledProperty<bool> ShowPhasePortraitProperty =
        AvaloniaProperty.Register<LagrangianCanvas, bool>(nameof(ShowPhasePortrait), true);

    public static readonly StyledProperty<bool> IsSimulatingProperty =
        AvaloniaProperty.Register<LagrangianCanvas, bool>(nameof(IsSimulating), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double L1
    {
        get => GetValue(L1Property);
        set => SetValue(L1Property, value);
    }

    public double L2
    {
        get => GetValue(L2Property);
        set => SetValue(L2Property, value);
    }

    public double M1
    {
        get => GetValue(M1Property);
        set => SetValue(M1Property, value);
    }

    public double M2
    {
        get => GetValue(M2Property);
        set => SetValue(M2Property, value);
    }

    public double Gravity
    {
        get => GetValue(GravityProperty);
        set => SetValue(GravityProperty, value);
    }

    public double Damping
    {
        get => GetValue(DampingProperty);
        set => SetValue(DampingProperty, value);
    }

    public bool ShowShadow
    {
        get => GetValue(ShowShadowProperty);
        set => SetValue(ShowShadowProperty, value);
    }

    public bool ShowTrails
    {
        get => GetValue(ShowTrailsProperty);
        set => SetValue(ShowTrailsProperty, value);
    }

    public bool ShowPhasePortrait
    {
        get => GetValue(ShowPhasePortraitProperty);
        set => SetValue(ShowPhasePortraitProperty, value);
    }

    public bool IsSimulating
    {
        get => GetValue(IsSimulatingProperty);
        set => SetValue(IsSimulatingProperty, value);
    }

    private readonly LagrangianMechanicsEngine _engine = new();

    // Interaction & Drag
    private int _dragBobIndex = -1; // 1 for Bob1, 2 for Bob2
    private Point? _hoverPos;

    static LagrangianCanvas()
    {
        AffectsRender<LagrangianCanvas>(
            PresetNameProperty,
            L1Property,
            L2Property,
            M1Property,
            M2Property,
            GravityProperty,
            DampingProperty,
            ShowShadowProperty,
            ShowTrailsProperty,
            ShowPhasePortraitProperty,
            IsSimulatingProperty);
    }

    public LagrangianCanvas()
    {
        ClipToBounds = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PresetNameProperty)
        {
            string name = PresetName?.ToLowerInvariant() ?? "double";
            switch (name)
            {
                case "double" or "chaos":
                    _engine.LoadPreset("Double Pendulum Chaos");
                    break;
                case "poincare" or "kam" or "torus":
                    _engine.LoadPreset("Poincaré Section & KAM Torus");
                    break;
                case "kapitza" or "inverted":
                    _engine.LoadPreset("Kapitza's Inverted Pendulum");
                    break;
                case "spring" or "fermi":
                    _engine.LoadPreset("Spring Pendulum (Fermi Resonance)");
                    break;
                case "foucault":
                    _engine.LoadPreset("Foucault Pendulum Precession");
                    break;
            }
        }
        else if (change.Property == L1Property) _engine.L1 = L1;
        else if (change.Property == L2Property) _engine.L2 = L2;
        else if (change.Property == M1Property) _engine.M1 = M1;
        else if (change.Property == M2Property) _engine.M2 = M2;
        else if (change.Property == GravityProperty) _engine.Gravity = Gravity;
        else if (change.Property == DampingProperty) _engine.Damping = Damping;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var p = e.GetPosition(this);

        double w = Bounds.Width;
        double h = Bounds.Height;
        double pivotX = w * 0.38;
        double pivotY = h * 0.38;
        double scale = Math.Min(w, h) * 0.22;

        (double x1, double y1, double x2, double y2) = _engine.GetDoublePendulumCoordinates(_engine.State);
        Point b1 = new(pivotX + x1 * scale, pivotY - y1 * scale);
        Point b2 = new(pivotX + x2 * scale, pivotY - y2 * scale);

        double dist1 = Math.Sqrt((p.X - b1.X) * (p.X - b1.X) + (p.Y - b1.Y) * (p.Y - b1.Y));
        double dist2 = Math.Sqrt((p.X - b2.X) * (p.X - b2.X) + (p.Y - b2.Y) * (p.Y - b2.Y));

        if (dist2 < 24) _dragBobIndex = 2;
        else if (dist1 < 24) _dragBobIndex = 1;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _hoverPos = e.GetPosition(this);

        if (_dragBobIndex > 0)
        {
            double w = Bounds.Width;
            double h = Bounds.Height;
            double pivotX = w * 0.38;
            double pivotY = h * 0.38;
            double scale = Math.Min(w, h) * 0.22;

            Point p = _hoverPos.Value;
            double dx = (p.X - pivotX) / scale;
            double dy = -(p.Y - pivotY) / scale;

            if (_dragBobIndex == 1)
            {
                double angle1 = Math.Atan2(dx, -dy);
                _engine.ResetToState(angle1, _engine.State.Theta2, 0, 0);
            }
            else if (_dragBobIndex == 2)
            {
                double x1 = _engine.L1 * Math.Sin(_engine.State.Theta1);
                double y1 = -_engine.L1 * Math.Cos(_engine.State.Theta1);
                double angle2 = Math.Atan2(dx - x1, -(dy - y1));
                _engine.ResetToState(_engine.State.Theta1, angle2, 0, 0);
            }
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragBobIndex = -1;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        // Dark Studio Background
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(13, 17, 23)), new Rect(0, 0, w, h));

        // Advance simulation step if active
        if (IsSimulating && _dragBobIndex == -1)
        {
            _engine.Step(0.015);
        }

        // Header Metrics Badge
        string badgeText = $"Lagrangian L = T - V = {_engine.LagrangianL:F2} J | Energy E = {_engine.TotalEnergy:F2} J | Kinetic T = {_engine.KineticEnergy:F2} J | Potential V = {_engine.PotentialEnergy:F2} J | Lyapunov Sep = {_engine.LyapunovSeparation:F4} m | Drift = {_engine.EnergyDrift * 100:F2}%";
        DrawHeaderBadge(context, badgeText, w);

        double splitX = w * 0.64;
        double pivotX = splitX * 0.5;
        double pivotY = h * 0.42;
        double scale = Math.Min(splitX, h) * 0.24;

        // Render Physical Pendulum Space
        RenderPendulumSpace(context, pivotX, pivotY, scale, splitX, h);

        // Render Right Phase Portrait / Poincaré Section & Energy Gauges
        if (ShowPhasePortrait)
        {
            double rightW = w - splitX - 25;
            RenderPhasePortraitAndGauges(context, splitX + 12, 60, rightW, h - 85);
        }

        // Draw Hover Probe HUD
        if (_hoverPos.HasValue)
        {
            DrawHoverProbe(context, _hoverPos.Value, w, h);
        }
    }

    private void RenderPendulumSpace(DrawingContext context, double px, double py, double scale, double spaceW, double spaceH)
    {
        // Background Grid Rings centered at pivot
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 48, 54, 61)), 1.0);
        context.DrawEllipse(null, gridPen, new Point(px, py), (_engine.L1) * scale, (_engine.L1) * scale);
        context.DrawEllipse(null, gridPen, new Point(px, py), (_engine.L1 + _engine.L2) * scale, (_engine.L1 + _engine.L2) * scale);

        // Axis Crosshairs
        var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 139, 148, 158)), 1.0, DashStyle.Dash);
        context.DrawLine(crossPen, new Point(px - spaceW * 0.45, py), new Point(px + spaceW * 0.45, py));
        context.DrawLine(crossPen, new Point(px, py - spaceH * 0.42), new Point(px, py + spaceH * 0.42));

        // 1. Draw Phosphor Trails for Bob 2
        if (ShowTrails && _engine.Trail.Count > 1)
        {
            int trailCount = _engine.Trail.Count;
            for (int i = 0; i < trailCount - 1; i++)
            {
                var pt1 = _engine.Trail[i];
                var pt2 = _engine.Trail[i + 1];

                double alpha = (double)(i + 1) / trailCount;
                byte aByte = (byte)(alpha * 200);

                // Cyan to Emerald to Gold gradient trail
                Color trailColor = Color.FromArgb(aByte, 77, 208, 225);
                var trailPen = new Pen(new SolidColorBrush(trailColor), 1.8);

                Point p1 = new(px + pt1.X * scale, py - pt1.Y * scale);
                Point p2 = new(px + pt2.X * scale, py - pt2.Y * scale);
                context.DrawLine(trailPen, p1, p2);
            }
        }

        // 2. Draw Shadow Trajectory (Butterfly Effect / Lyapunov Divergence)
        if (ShowShadow && _engine.ShadowTrail.Count > 1)
        {
            int sCount = _engine.ShadowTrail.Count;
            for (int i = 0; i < sCount - 1; i++)
            {
                var pt1 = _engine.ShadowTrail[i];
                var pt2 = _engine.ShadowTrail[i + 1];

                double alpha = (double)(i + 1) / sCount;
                byte aByte = (byte)(alpha * 160);

                Color shadowColor = Color.FromArgb(aByte, 255, 83, 112); // Translucent Rose
                var sPen = new Pen(new SolidColorBrush(shadowColor), 1.4);

                Point p1 = new(px + pt1.X * scale, py - pt1.Y * scale);
                Point p2 = new(px + pt2.X * scale, py - pt2.Y * scale);
                context.DrawLine(sPen, p1, p2);
            }

            // Draw Shadow Pendulum Rods & Bob
            (double sx1, double sy1, double sx2, double sy2) = _engine.GetDoublePendulumCoordinates(_engine.ShadowState);
            Point sp0 = new(px, py);
            Point sp1 = new(px + sx1 * scale, py - sy1 * scale);
            Point sp2 = new(px + sx2 * scale, py - sy2 * scale);

            var shadowRodPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 83, 112)), 1.5, DashStyle.Dash);
            context.DrawLine(shadowRodPen, sp0, sp1);
            context.DrawLine(shadowRodPen, sp1, sp2);

            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(180, 255, 83, 112)), null, sp2, 6, 6);

            // Lyapunov divergence connector line
            (double mx1, double my1, double mx2, double my2) = _engine.GetDoublePendulumCoordinates(_engine.State);
            Point mp2 = new(px + mx2 * scale, py - my2 * scale);

            var divPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 64)), 1.2, DashStyle.Dash);
            context.DrawLine(divPen, mp2, sp2);
        }

        // 3. Draw Primary Double Pendulum Rods & Bobs
        (double x1, double y1, double x2, double y2) = _engine.GetDoublePendulumCoordinates(_engine.State);
        Point p0 = new(px, py);
        Point p1Bob = new(px + x1 * scale, py - y1 * scale);
        Point p2Bob = new(px + x2 * scale, py - y2 * scale);

        // Rod 1 (Pivot to Bob 1)
        var rod1Pen = new Pen(new SolidColorBrush(Color.FromRgb(139, 148, 158)), 3.0);
        context.DrawLine(rod1Pen, p0, p1Bob);

        // Rod 2 (Bob 1 to Bob 2)
        var rod2Pen = new Pen(new SolidColorBrush(Color.FromRgb(201, 209, 217)), 3.0);
        context.DrawLine(rod2Pen, p1Bob, p2Bob);

        // Fixed Suspension Mount at Pivot
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 255, 255)), new Pen(new SolidColorBrush(Color.FromRgb(13, 17, 23)), 2.0), p0, 6, 6);

        // Bob 1 (Cyan mass sphere)
        double rBob1 = Math.Max(8, Math.Min(18, 11 * Math.Sqrt(_engine.M1)));
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 77, 208, 225)), null, p1Bob, rBob1 + 6, rBob1 + 6);
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(77, 208, 225)), new Pen(new SolidColorBrush(Color.FromRgb(13, 17, 23)), 2.0), p1Bob, rBob1, rBob1);

        // Bob 2 (Golden mass sphere)
        double rBob2 = Math.Max(8, Math.Min(18, 11 * Math.Sqrt(_engine.M2)));
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(50, 255, 215, 64)), null, p2Bob, rBob2 + 7, rBob2 + 7);
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 64)), new Pen(new SolidColorBrush(Color.FromRgb(13, 17, 23)), 2.0), p2Bob, rBob2, rBob2);

        // Bob Labels
        var b1Text = new FormattedText("m₁", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 10, new SolidColorBrush(Color.FromRgb(13, 17, 23)));
        context.DrawText(b1Text, new Point(p1Bob.X - b1Text.Width * 0.5, p1Bob.Y - b1Text.Height * 0.5));

        var b2Text = new FormattedText("m₂", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 10, new SolidColorBrush(Color.FromRgb(13, 17, 23)));
        context.DrawText(b2Text, new Point(p2Bob.X - b2Text.Width * 0.5, p2Bob.Y - b2Text.Height * 0.5));
    }

    private void RenderPhasePortraitAndGauges(DrawingContext context, double x, double y, double w, double h)
    {
        var cardRect = new Rect(x, y, w, h);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 22, 27, 34)), new Pen(new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)), 1.0), cardRect, 8, 8);

        // Section Title
        var titleText = new FormattedText(
            "POINCARÉ SECTION & HAMILTONIAN ENERGY",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
            11,
            new SolidColorBrush(Color.FromRgb(255, 215, 64))
        );
        context.DrawText(titleText, new Point(x + 14, y + 14));

        // Poincaré Section Plot (θ₁ vs ω₁) at θ₂ = 0
        double plotX = x + 16;
        double plotY = y + 42;
        double plotW = w - 32;
        double plotH = h * 0.45;

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 13, 17, 23)), new Pen(new SolidColorBrush(Color.FromArgb(60, 48, 54, 61)), 1.0), new Rect(plotX, plotY, plotW, plotH), 4, 4);

        // Plot Axes
        double midPlotX = plotX + plotW * 0.5;
        double midPlotY = plotY + plotH * 0.5;
        var pAxisPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 139, 148, 158)), 1.0, DashStyle.Dash);
        context.DrawLine(pAxisPen, new Point(plotX, midPlotY), new Point(plotX + plotW, midPlotY));
        context.DrawLine(pAxisPen, new Point(midPlotX, plotY), new Point(midPlotX, plotY + plotH));

        // Axis Labels
        var th1Label = new FormattedText("θ₁ (rad)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 9, new SolidColorBrush(Color.FromArgb(180, 139, 148, 158)));
        context.DrawText(th1Label, new Point(plotX + plotW - th1Label.Width - 6, midPlotY + 4));

        var om1Label = new FormattedText("ω₁ (rad/s)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 9, new SolidColorBrush(Color.FromArgb(180, 139, 148, 158)));
        context.DrawText(om1Label, new Point(midPlotX + 6, plotY + 4));

        // Plot Poincaré Points (Stroboscopic KAM tori or chaotic sea)
        double maxOm = 12.0;
        foreach (var pt in _engine.PoincarePoints)
        {
            double px = midPlotX + (pt.Theta / Math.PI) * (plotW * 0.5);
            double py = midPlotY - (pt.Omega / maxOm) * (plotH * 0.5);

            if (px >= plotX && px <= plotX + plotW && py >= plotY && py <= plotY + plotH)
            {
                context.DrawEllipse(new SolidColorBrush(Color.FromRgb(105, 240, 174)), null, new Point(px, py), 2.0, 2.0);
            }
        }

        // Live Current Point in Phase Space
        double curPx = midPlotX + ((_engine.State.Theta1 % Math.PI) / Math.PI) * (plotW * 0.5);
        double curPy = midPlotY - (_engine.State.Omega1 / maxOm) * (plotH * 0.5);
        if (curPx >= plotX && curPx <= plotX + plotW && curPy >= plotY && curPy <= plotY + plotH)
        {
            context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 64)), new Pen(new SolidColorBrush(Color.FromRgb(13, 17, 23)), 1.5), new Point(curPx, curPy), 4.5, 4.5);
        }

        // Real-Time Energy Stack Bar
        double barY = plotY + plotH + 20;
        double barW = plotW;
        double barH = 18;

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 13, 17, 23)), new Pen(new SolidColorBrush(Color.FromArgb(60, 48, 54, 61)), 1.0), new Rect(plotX, barY, barW, barH), 4, 4);

        double totalMaxEnergy = Math.Max(20.0, Math.Abs(_engine.InitialEnergy) * 1.5);
        double kWidth = Math.Clamp((_engine.KineticEnergy / totalMaxEnergy) * barW, 0, barW);

        // Kinetic Energy (Cyan)
        context.DrawRectangle(new SolidColorBrush(Color.FromRgb(77, 208, 225)), null, new Rect(plotX, barY, kWidth, barH), 4, 4);

        var energyLabel = new FormattedText(
            $"Kinetic T: {_engine.KineticEnergy:F2} J  |  Potential V: {_engine.PotentialEnergy:F2} J",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.SemiBold),
            9,
            new SolidColorBrush(Color.FromRgb(255, 255, 255))
        );
        context.DrawText(energyLabel, new Point(plotX + 8, barY + (barH - energyLabel.Height) * 0.5));

        // Classical Equations Card
        double mathY = barY + barH + 16;
        string[] formulas = [
            "• Lagrangian:  ℒ = T - V = ½(m₁+m₂)L₁²θ̇₁² + ½m₂L₂²θ̇₂² + ...",
            "• Euler-Lagrange:  d/dt(∂ℒ/∂q̇ᵢ) - ∂ℒ/∂qᵢ = 0",
            "• Hamiltonian Chaos: Positive Lyapunov Exponent λ > 0",
            $"• Divergence: ‖Δq(t)‖ ≈ ‖Δq(0)‖·e^(λt) = {_engine.LyapunovSeparation:F4} m",
            "• KAM Theorem: Invariant tori withstand small nonlinear perturbations"
        ];

        for (int i = 0; i < formulas.Length; i++)
        {
            var fText = new FormattedText(
                formulas[i],
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.Normal),
                10,
                new SolidColorBrush(i == 0 ? Color.FromRgb(77, 208, 225) : Color.FromArgb(200, 201, 209, 217))
            );
            context.DrawText(fText, new Point(x + 16, mathY + i * 20));
        }
    }

    private void DrawHeaderBadge(DrawingContext context, string text, double width)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.SemiBold),
            11,
            new SolidColorBrush(Color.FromRgb(77, 208, 225))
        );

        double badgeW = formatted.Width + 28;
        double badgeH = 28;
        double badgeX = Math.Max(12, (width - badgeW) * 0.5);
        double badgeY = 14;

        var rect = new Rect(badgeX, badgeY, badgeW, badgeH);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(230, 22, 27, 34)), new Pen(new SolidColorBrush(Color.FromArgb(120, 77, 208, 225)), 1.5), rect, 14, 14);
        context.DrawText(formatted, new Point(badgeX + 14, badgeY + (badgeH - formatted.Height) * 0.5));
    }

    private void DrawHoverProbe(DrawingContext context, Point mouse, double w, double h)
    {
        double probeW = 210;
        double probeH = 75;
        double px = mouse.X + 15;
        double py = mouse.Y + 15;

        if (px + probeW > w - 10) px = mouse.X - probeW - 15;
        if (py + probeH > h - 10) py = mouse.Y - probeH - 15;

        var rect = new Rect(px, py, probeW, probeH);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(240, 13, 17, 23)), new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 215, 64)), 1.5), rect, 6, 6);

        var title = new FormattedText("PROBE HUD", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 10, new SolidColorBrush(Color.FromRgb(255, 215, 64)));
        context.DrawText(title, new Point(px + 10, py + 8));

        string line1 = $"Angles: θ₁={_engine.State.Theta1 * 180 / Math.PI:F1}°, θ₂={_engine.State.Theta2 * 180 / Math.PI:F1}°";
        string line2 = $"Velocities: ω₁={_engine.State.Omega1:F2}, ω₂={_engine.State.Omega2:F2} rad/s";
        string line3 = $"Screen Pos: ({mouse.X:F0}, {mouse.Y:F0})";

        var t1 = new FormattedText(line1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 9, new SolidColorBrush(Color.FromArgb(200, 201, 209, 217)));
        var t2 = new FormattedText(line2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 9, new SolidColorBrush(Color.FromRgb(77, 208, 225)));
        var t3 = new FormattedText(line3, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 9, new SolidColorBrush(Color.FromArgb(160, 139, 148, 158)));

        context.DrawText(t1, new Point(px + 10, py + 24));
        context.DrawText(t2, new Point(px + 10, py + 38));
        context.DrawText(t3, new Point(px + 10, py + 52));
    }
}
