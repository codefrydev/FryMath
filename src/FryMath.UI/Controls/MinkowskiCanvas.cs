using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Relativity;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class MinkowskiCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<MinkowskiCanvas, string>(nameof(PresetName), "boost");

    public static readonly StyledProperty<double> BetaProperty =
        AvaloniaProperty.Register<MinkowskiCanvas, double>(nameof(Beta), 0.6);

    public static readonly StyledProperty<bool> ShowHyperbolasProperty =
        AvaloniaProperty.Register<MinkowskiCanvas, bool>(nameof(ShowHyperbolas), true);

    public static readonly StyledProperty<bool> ShowLightConeProperty =
        AvaloniaProperty.Register<MinkowskiCanvas, bool>(nameof(ShowLightCone), true);

    public static readonly StyledProperty<bool> ShowSimultaneityGridProperty =
        AvaloniaProperty.Register<MinkowskiCanvas, bool>(nameof(ShowSimultaneityGrid), true);

    public static readonly StyledProperty<bool> ShowWorldlinesProperty =
        AvaloniaProperty.Register<MinkowskiCanvas, bool>(nameof(ShowWorldlines), true);

    public static readonly StyledProperty<bool> IsSimulatingProperty =
        AvaloniaProperty.Register<MinkowskiCanvas, bool>(nameof(IsSimulating), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double Beta
    {
        get => GetValue(BetaProperty);
        set => SetValue(BetaProperty, value);
    }

    public bool ShowHyperbolas
    {
        get => GetValue(ShowHyperbolasProperty);
        set => SetValue(ShowHyperbolasProperty, value);
    }

    public bool ShowLightCone
    {
        get => GetValue(ShowLightConeProperty);
        set => SetValue(ShowLightConeProperty, value);
    }

    public bool ShowSimultaneityGrid
    {
        get => GetValue(ShowSimultaneityGridProperty);
        set => SetValue(ShowSimultaneityGridProperty, value);
    }

    public bool ShowWorldlines
    {
        get => GetValue(ShowWorldlinesProperty);
        set => SetValue(ShowWorldlinesProperty, value);
    }

    public bool IsSimulating
    {
        get => GetValue(IsSimulatingProperty);
        set => SetValue(IsSimulatingProperty, value);
    }

    private readonly MinkowskiEngine _engine = new();
    private readonly DispatcherTimer _timer;
    private Point? _hoverPos;

    static MinkowskiCanvas()
    {
        AffectsRender<MinkowskiCanvas>(
            PresetNameProperty,
            BetaProperty,
            ShowHyperbolasProperty,
            ShowLightConeProperty,
            ShowSimultaneityGridProperty,
            ShowWorldlinesProperty,
            IsSimulatingProperty);
    }

    public MinkowskiCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            if (IsSimulating)
            {
                _engine.Step(0.03);
                InvalidateVisual();
            }
        };
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _hoverPos = e.GetPosition(this);
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hoverPos = null;
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PresetNameProperty)
        {
            string name = change.GetNewValue<string>() ?? "boost";
            var presetType = name.ToLowerInvariant() switch
            {
                "twin" or "twinparadox" => RelativityPresetType.TwinParadox,
                "muon" or "muondecay" => RelativityPresetType.MuonDecay,
                "bell" or "bellspaceships" => RelativityPresetType.BellSpaceships,
                "doppler" or "dopplershift" => RelativityPresetType.DopplerShift,
                _ => RelativityPresetType.StandardBoost
            };
            _engine.LoadPreset(presetType);
            SetCurrentValue(BetaProperty, _engine.Beta);
        }
        else if (change.Property == BetaProperty)
        {
            _engine.Beta = change.GetNewValue<double>();
        }
    }

    public override void Render(DrawingContext context)
    {
        _engine.Beta = Beta;

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var tokens = new MathThemeTokens();

        // 1. Background Fill
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        // Coordinate scaling: origin centered at (w/2, h/2 + offset)
        double scale = Math.Min(w, h) / 18.0;
        double originX = w / 2.0;
        double originY = h * 0.55; // slightly lower than center to give more future view

        double ToScreenX(double x) => originX + x * scale;
        double ToScreenY(double ct) => originY - ct * scale;
        double ToMathX(double sx) => (sx - originX) / scale;
        double ToMathCt(double sy) => (originY - sy) / scale;

        // 2. Light Cone Region (Translucent fill for Future and Past)
        if (ShowLightCone)
        {
            DrawLightCone(context, originX, originY, scale, w, h);
        }

        // 3. Invariant Hyperbolas
        if (ShowHyperbolas)
        {
            DrawInvariantHyperbolas(context, ToScreenX, ToScreenY);
        }

        // 4. Rest Frame Grid & Axes (x, ct)
        DrawRestGridAndAxes(context, ToScreenX, ToScreenY, w, h, tokens);

        // 5. Moving Frame Grid & Axes (x', ct')
        DrawBoostedFrame(context, ToScreenX, ToScreenY, tokens);

        // 6. Worldlines & Events
        if (ShowWorldlines)
        {
            DrawWorldlines(context, ToScreenX, ToScreenY);
            DrawKeyEvents(context, ToScreenX, ToScreenY, tokens);
        }

        // 7. Live Simulation Markers
        DrawSimulationParticles(context, ToScreenX, ToScreenY);

        // 8. Hover HUD Probe
        if (_hoverPos.HasValue)
        {
            double mx = ToMathX(_hoverPos.Value.X);
            double mct = ToMathCt(_hoverPos.Value.Y);
            DrawHoverHUD(context, _hoverPos.Value, mx, mct, tokens);
        }

        // 9. Status Badge (Top Right)
        DrawStatusBadge(context, w, tokens);
    }

    private void DrawLightCone(DrawingContext context, double ox, double oy, double scale, double w, double h)
    {
        // Future light cone triangle: apex at origin, expanding upward
        double topY = 0;
        double dyTop = oy - topY;
        var futurePath = new StreamGeometry();
        using (var ctx = futurePath.Open())
        {
            ctx.BeginFigure(new Point(ox, oy), true);
            ctx.LineTo(new Point(ox - dyTop, topY));
            ctx.LineTo(new Point(ox + dyTop, topY));
        }
        var futureBrush = new SolidColorBrush(Color.FromArgb(20, 245, 158, 11)); // amber glow
        context.DrawGeometry(futureBrush, null, futurePath);

        // Past light cone triangle
        double botY = h;
        double dyBot = botY - oy;
        var pastPath = new StreamGeometry();
        using (var ctx = pastPath.Open())
        {
            ctx.BeginFigure(new Point(ox, oy), true);
            ctx.LineTo(new Point(ox - dyBot, botY));
            ctx.LineTo(new Point(ox + dyBot, botY));
        }
        var pastBrush = new SolidColorBrush(Color.FromArgb(12, 56, 189, 248)); // blue tint
        context.DrawGeometry(pastBrush, null, pastPath);

        // Light cone border rays x = +/- ct (slope 1 and -1)
        var lightRayPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 251, 191, 36)), 1.8,
            new DashStyle([4, 4], 0));
        double maxCoord = 12.0;
        context.DrawLine(lightRayPen, new Point(ox - maxCoord * scale, oy - maxCoord * scale),
            new Point(ox + maxCoord * scale, oy + maxCoord * scale));
        context.DrawLine(lightRayPen, new Point(ox - maxCoord * scale, oy + maxCoord * scale),
            new Point(ox + maxCoord * scale, oy - maxCoord * scale));
    }

    private void DrawInvariantHyperbolas(DrawingContext context, Func<double, double> toX, Func<double, double> toY)
    {
        var timelikePen = new Pen(new SolidColorBrush(Color.FromArgb(90, 168, 85, 247)), 1.2,
            new DashStyle([3, 3], 0));
        var spacelikePen = new Pen(new SolidColorBrush(Color.FromArgb(70, 16, 185, 129)), 1.0,
            new DashStyle([2, 4], 0));

        // Timelike calibration hyperbolas: ct^2 - x^2 = tau^2 for tau = 2, 4, 6
        foreach (double tau in new[] { 2.0, 4.0, 6.0 })
        {
            var pts = _engine.GetHyperbolaPoints(tau * tau, 7.0);
            if (pts.Count > 1)
            {
                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new Point(toX(pts[0].X), toY(pts[0].Ct)), false);
                    for (int i = 1; i < pts.Count; i++)
                        ctx.LineTo(new Point(toX(pts[i].X), toY(pts[i].Ct)));
                }
                context.DrawGeometry(null, timelikePen, geom);
            }
        }

        // Spacelike calibration hyperbolas: x^2 - ct^2 = sigma^2 for sigma = 2, 4, 6 (both +x and -x)
        foreach (double sigma in new[] { 2.0, 4.0, 6.0 })
        {
            var ptsRight = _engine.GetHyperbolaPoints(-sigma * sigma, 7.0);
            if (ptsRight.Count > 1)
            {
                var geomR = new StreamGeometry();
                using (var ctx = geomR.Open())
                {
                    ctx.BeginFigure(new Point(toX(ptsRight[0].X), toY(ptsRight[0].Ct)), false);
                    for (int i = 1; i < ptsRight.Count; i++)
                        ctx.LineTo(new Point(toX(ptsRight[i].X), toY(ptsRight[i].Ct)));
                }
                context.DrawGeometry(null, spacelikePen, geomR);

                var geomL = new StreamGeometry();
                using (var ctx = geomL.Open())
                {
                    ctx.BeginFigure(new Point(toX(-ptsRight[0].X), toY(ptsRight[0].Ct)), false);
                    for (int i = 1; i < ptsRight.Count; i++)
                        ctx.LineTo(new Point(toX(-ptsRight[i].X), toY(ptsRight[i].Ct)));
                }
                context.DrawGeometry(null, spacelikePen, geomL);
            }
        }
    }

    private void DrawRestGridAndAxes(DrawingContext context, Func<double, double> toX, Func<double, double> toY,
        double w, double h, MathThemeTokens tokens)
    {
        var minorPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), 0.8);
        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.5);

        // Grid lines
        for (double c = -10; c <= 10; c += 2.0)
        {
            if (Math.Abs(c) < 1e-6) continue;
            // Vertical x = c
            context.DrawLine(minorPen, new Point(toX(c), 0), new Point(toX(c), h));
            // Horizontal ct = c
            context.DrawLine(minorPen, new Point(0, toY(c)), new Point(w, toY(c)));
        }

        // Rest axes: ct axis (vertical) and x axis (horizontal)
        context.DrawLine(axisPen, new Point(toX(0), 0), new Point(toX(0), h));
        context.DrawLine(axisPen, new Point(0, toY(0)), new Point(w, toY(0)));

        // Axis labels
        var textBrush = tokens.TextMutedBrush;
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

        var ctText = new FormattedText("ct (time)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            typeface, 12, textBrush);
        context.DrawText(ctText, new Point(toX(0) + 8, 12));

        var xText = new FormattedText("x (space)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            typeface, 12, textBrush);
        context.DrawText(xText, new Point(w - 70, toY(0) + 8));
    }

    private void DrawBoostedFrame(DrawingContext context, Func<double, double> toX, Func<double, double> toY,
        MathThemeTokens tokens)
    {
        double b = _engine.Beta;
        if (Math.Abs(b) < 1e-4) return;

        var boostedAxisPen = new Pen(new SolidColorBrush(Color.FromRgb(6, 182, 212)), 2.0); // Cyan #06B6D4
        var simultaneityPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 6, 182, 212)), 1.2,
            new DashStyle([4, 4], 0));

        // ct' axis: x = beta * ct
        double ctSpan = 8.0;
        context.DrawLine(boostedAxisPen,
            new Point(toX(-b * ctSpan), toY(-ctSpan)),
            new Point(toX(b * ctSpan), toY(ctSpan)));

        // x' axis: ct = beta * x
        double xSpan = 8.0;
        context.DrawLine(boostedAxisPen,
            new Point(toX(-xSpan), toY(-b * xSpan)),
            new Point(toX(xSpan), toY(b * xSpan)));

        // Simultaneity lines ct' = const: ct = beta * x + ct' / gamma
        if (ShowSimultaneityGrid)
        {
            double gamma = _engine.Gamma;
            foreach (double ctPrime in _engine.SimultaneityValues)
            {
                if (Math.Abs(ctPrime) < 1e-6) continue;
                double ctIntercept = ctPrime / gamma;
                double x1 = -8.0;
                double ct1 = b * x1 + ctIntercept;
                double x2 = 8.0;
                double ct2 = b * x2 + ctIntercept;
                context.DrawLine(simultaneityPen, new Point(toX(x1), toY(ct1)), new Point(toX(x2), toY(ct2)));
            }
        }

        // Moving axis labels
        var cyanBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));
        var boldTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);

        var ctPrimeText = new FormattedText("ct'", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            boldTypeface, 13, cyanBrush);
        context.DrawText(ctPrimeText, new Point(toX(b * ctSpan) + 8, toY(ctSpan) - 4));

        var xPrimeText = new FormattedText("x'", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            boldTypeface, 13, cyanBrush);
        context.DrawText(xPrimeText, new Point(toX(xSpan) + 4, toY(b * xSpan) + 8));
    }

    private void DrawWorldlines(DrawingContext context, Func<double, double> toX, Func<double, double> toY)
    {
        foreach (var wl in _engine.Worldlines)
        {
            if (wl.Points.Count < 2) continue;

            Color wlColor = Color.Parse(wl.ColorHex);
            var pen = new Pen(new SolidColorBrush(wlColor), wl.IsDashed ? 1.4 : 2.2,
                wl.IsDashed ? new DashStyle([4, 4], 0) : null);

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(new Point(toX(wl.Points[0].X), toY(wl.Points[0].Ct)), false);
                for (int i = 1; i < wl.Points.Count; i++)
                    ctx.LineTo(new Point(toX(wl.Points[i].X), toY(wl.Points[i].Ct)));
            }
            context.DrawGeometry(null, pen, geom);
        }
    }

    private void DrawKeyEvents(DrawingContext context, Func<double, double> toX, Func<double, double> toY,
        MathThemeTokens tokens)
    {
        var nodeBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
        var nodeBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1.5);
        var labelBrush = tokens.TextPrimaryBrush;
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

        foreach (var ev in _engine.KeyEvents)
        {
            double sx = toX(ev.X);
            double sy = toY(ev.Ct);

            // Glowing ring
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 245, 158, 11)), null,
                new Point(sx, sy), 9, 9);
            // Center bead
            context.DrawEllipse(nodeBrush, nodeBorderPen, new Point(sx, sy), 4.5, 4.5);

            if (!string.IsNullOrEmpty(ev.Label))
            {
                var ft = new FormattedText(ev.Label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    typeface, 10, labelBrush);
                // Background bubble
                var textRect = new Rect(sx + 10, sy - 8, ft.Width + 8, ft.Height + 4);
                context.FillRectangle(new SolidColorBrush(Color.FromArgb(200, 24, 20, 39)), textRect, 4);
                context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(100, 245, 158, 11)), 1.0), textRect, 4);
                context.DrawText(ft, new Point(sx + 14, sy - 6));
            }
        }
    }

    private void DrawSimulationParticles(DrawingContext context, Func<double, double> toX, Func<double, double> toY)
    {
        double simT = _engine.SimTime;
        var beadBrush = new SolidColorBrush(Color.FromRgb(244, 63, 94)); // Rose
        var beadPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1.5);

        foreach (var wl in _engine.Worldlines)
        {
            if (wl.IsDashed || wl.Points.Count == 0) continue;

            // Find point on worldline closest to simT
            SpacetimeEvent? currentPoint = null;
            for (int i = 0; i < wl.Points.Count - 1; i++)
            {
                if (wl.Points[i].Ct <= simT && wl.Points[i + 1].Ct >= simT)
                {
                    double fraction = (simT - wl.Points[i].Ct) / Math.Max(1e-6, wl.Points[i + 1].Ct - wl.Points[i].Ct);
                    double curX = wl.Points[i].X + fraction * (wl.Points[i + 1].X - wl.Points[i].X);
                    currentPoint = new SpacetimeEvent(curX, simT);
                    break;
                }
            }

            if (currentPoint != null)
            {
                double px = toX(currentPoint.X);
                double py = toY(currentPoint.Ct);

                context.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 244, 63, 94)), null,
                    new Point(px, py), 8, 8);
                context.DrawEllipse(beadBrush, beadPen, new Point(px, py), 4, 4);
            }
        }
    }

    private void DrawHoverHUD(DrawingContext context, Point hoverPt, double mx, double mct, MathThemeTokens tokens)
    {
        double b = _engine.Beta;
        var (xPrime, ctPrime) = MinkowskiEngine.LorentzTransform(mx, mct, b);
        double s2 = mct * mct - mx * mx;

        string causalityText;
        Color causalityColor;
        if (Math.Abs(s2) < 0.1)
        {
            causalityText = "LIGHTLIKE (Null, s² = 0)";
            causalityColor = Color.FromRgb(251, 191, 36); // Amber
        }
        else if (s2 > 0)
        {
            double properTime = Math.Sqrt(s2);
            causalityText = $"TIMELIKE (Δτ = {properTime:F2})";
            causalityColor = Color.FromRgb(168, 85, 247); // Purple
        }
        else
        {
            double properDist = Math.Sqrt(-s2);
            causalityText = $"SPACELIKE (Δσ = {properDist:F2})";
            causalityColor = Color.FromRgb(16, 185, 129); // Emerald
        }

        string line1 = $"S Frame:   x = {mx:F2},  ct = {mct:F2}";
        string line2 = $"S' Frame:  x' = {xPrime:F2},  ct' = {ctPrime:F2}";
        string line3 = $"Interval:  s² = c²t² - x² = {s2:F2}";
        string line4 = $"Nature:    {causalityText}";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var monoTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

        var ft1 = new FormattedText(line1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11, tokens.TextPrimaryBrush);
        var ft2 = new FormattedText(line2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11, tokens.TextSecondaryBrush);
        var ft3 = new FormattedText(line3, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11, tokens.TextPrimaryBrush);
        var ft4 = new FormattedText(line4, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(causalityColor));

        double hudW = Math.Max(Math.Max(ft1.Width, ft2.Width), Math.Max(ft3.Width, ft4.Width)) + 24;
        double hudH = 88;

        double hudX = hoverPt.X + 16;
        double hudY = hoverPt.Y - hudH - 8;
        if (hudX + hudW > Bounds.Width) hudX = hoverPt.X - hudW - 16;
        if (hudY < 10) hudY = hoverPt.Y + 16;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 24, 20, 39)), hudRect, 8);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(140, 168, 85, 247)), 1.2), hudRect, 8);

        context.DrawText(ft1, new Point(hudX + 12, hudY + 8));
        context.DrawText(ft2, new Point(hudX + 12, hudY + 26));
        context.DrawText(ft3, new Point(hudX + 12, hudY + 44));
        context.DrawText(ft4, new Point(hudX + 12, hudY + 62));
    }

    private void DrawStatusBadge(DrawingContext context, double w, MathThemeTokens tokens)
    {
        double gamma = _engine.Gamma;
        double beta = _engine.Beta;
        string badgeText = $"β = {beta:F2} c   |   γ = {gamma:F3}   |   Δt' = γΔt   |   L' = L/γ";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var ft = new FormattedText(badgeText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            typeface, 11, new SolidColorBrush(Color.FromRgb(6, 182, 212)));

        double bw = ft.Width + 20;
        double bh = 28;
        double bx = w - bw - 16;
        double by = 56;

        var bRect = new Rect(bx, by, bw, bh);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(210, 24, 20, 39)), bRect, 6);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(120, 6, 182, 212)), 1.2), bRect, 6);
        context.DrawText(ft, new Point(bx + 10, by + 6));
    }
}
