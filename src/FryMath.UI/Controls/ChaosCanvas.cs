using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Dynamics;
using FryMath.Core.LinearAlgebra;
using FryMath.UI.Theming;
using Vector3D = FryMath.Core.LinearAlgebra.Vector3D;

namespace FryMath.UI.Controls;

public class ChaosCanvas : Control
{
    public static readonly StyledProperty<bool> ShowDualTrajectoriesProperty =
        AvaloniaProperty.Register<ChaosCanvas, bool>(nameof(ShowDualTrajectories), true);

    public static readonly StyledProperty<double> SigmaProperty =
        AvaloniaProperty.Register<ChaosCanvas, double>(nameof(Sigma), 10.0);

    public static readonly StyledProperty<double> RhoProperty =
        AvaloniaProperty.Register<ChaosCanvas, double>(nameof(Rho), 28.0);

    public static readonly StyledProperty<double> BetaProperty =
        AvaloniaProperty.Register<ChaosCanvas, double>(nameof(Beta), 8.0 / 3.0);

    public bool ShowDualTrajectories
    {
        get => GetValue(ShowDualTrajectoriesProperty);
        set => SetValue(ShowDualTrajectoriesProperty, value);
    }

    public double Sigma
    {
        get => GetValue(SigmaProperty);
        set => SetValue(SigmaProperty, value);
    }

    public double Rho
    {
        get => GetValue(RhoProperty);
        set => SetValue(RhoProperty, value);
    }

    public double Beta
    {
        get => GetValue(BetaProperty);
        set => SetValue(BetaProperty, value);
    }

    private readonly LorenzAttractorSimulation _simulation1 = new();
    private readonly LorenzAttractorSimulation _simulation2 = new();
    private readonly DispatcherTimer _timer;

    private double _yaw = 0.85;
    private double _pitch = 0.35;
    private bool _isOrbiting;
    private Point _orbitStart;
    private double _startYaw;
    private double _startPitch;

    static ChaosCanvas()
    {
        AffectsRender<ChaosCanvas>(ShowDualTrajectoriesProperty, SigmaProperty, RhoProperty, BetaProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SigmaProperty || change.Property == RhoProperty || change.Property == BetaProperty)
        {
            _simulation1.Sigma = Sigma;
            _simulation1.Rho = Rho;
            _simulation1.Beta = Beta;

            _simulation2.Sigma = Sigma;
            _simulation2.Rho = Rho;
            _simulation2.Beta = Beta;
        }
    }

    public ChaosCanvas()
    {
        ClipToBounds = true;

        _simulation1.MaxHistory = 2500;
        _simulation2.MaxHistory = 2500;

        // Initialize second trajectory with slight perturbation (10^-5) for Butterfly Effect
        _simulation2.Reset(0.10001, 0.0, 0.0);

        // Prewarm both simulations so both butterfly lobes are richly populated
        for (int i = 0; i < 5000; i++)
        {
            _simulation1.Step(0.008);
            _simulation2.Step(0.008);
        }

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            for (int i = 0; i < 4; i++)
            {
                _simulation1.Step(0.006);
                if (ShowDualTrajectories)
                {
                    _simulation2.Step(0.006);
                }
            }
            InvalidateVisual();
        };
        _timer.Start();

        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isOrbiting = true;
        _orbitStart = e.GetPosition(this);
        _startYaw = _yaw;
        _startPitch = _pitch;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isOrbiting)
        {
            var pos = e.GetPosition(this);
            double dx = (pos.X - _orbitStart.X) * 0.008;
            double dy = (pos.Y - _orbitStart.Y) * 0.008;
            _yaw = _startYaw + dx;
            _pitch = Math.Clamp(_startPitch + dy, -Math.PI * 0.45, Math.PI * 0.45);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isOrbiting = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var tokens = MathThemeService.Instance.Tokens;
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        double ox = w * 0.5;
        double oy = h * 0.52;
        double scale = Math.Min(w, h) * 0.013;

        // 1. Primary Trajectory (Cyan)
        RenderTrajectory(context, _simulation1, ox, oy, scale, tokens.AccentCyanBrush, tokens.AccentCyanBrush);

        // 2. Butterfly Perturbed Trajectory (Rose)
        if (ShowDualTrajectories)
        {
            RenderTrajectory(context, _simulation2, ox, oy, scale, tokens.AccentRoseBrush, tokens.AccentRoseBrush);
        }

        // 3. HUD Readout
        var p1 = _simulation1.CurrentPosition;
        var p2 = _simulation2.CurrentPosition;
        double divergence = (p1 - p2).Length;

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var hudText = new FormattedText(
            $"Lorenz Strange Attractor (Butterfly Effect Sensitivity)\n" +
            $"Primary (Cyan): ({p1.X:F2}, {p1.Y:F2}, {p1.Z:F2})  |  Perturbed ε=10⁻⁵ (Rose): ({p2.X:F2}, {p2.Y:F2}, {p2.Z:F2})\n" +
            $"Current Trajectory Separation Δ = {divergence:F2}  |  σ = {Sigma:F1}, ρ = {Rho:F1}, β = {Beta:F2}  |  Drag mouse to orbit 3D camera",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            11,
            tokens.TextPrimaryBrush);

        context.DrawText(hudText, new Point(16.0, 16.0));
    }

    private void RenderTrajectory(DrawingContext context, LorenzAttractorSimulation sim, double ox, double oy, double scale, IBrush pathBrush, IBrush headBrush)
    {
        var history = sim.TrajectoryHistory;
        if (history.Count > 1)
        {
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                var pt0 = LorenzAttractorSimulation.Project(history[0], _yaw, _pitch, scale, ox, oy);
                ctx.BeginFigure(new Point(pt0.X, pt0.Y), false);

                for (int i = 1; i < history.Count; i++)
                {
                    var proj = LorenzAttractorSimulation.Project(history[i], _yaw, _pitch, scale, ox, oy);
                    ctx.LineTo(new Point(proj.X, proj.Y));
                }
            }

            var pen = new Pen(pathBrush, 1.4);
            context.DrawGeometry(null, pen, geom);
        }

        var head = LorenzAttractorSimulation.Project(sim.CurrentPosition, _yaw, _pitch, scale, ox, oy);
        var headScreen = new Point(head.X, head.Y);
        context.DrawEllipse(headBrush, null, headScreen, 4.5, 4.5);
        context.DrawEllipse(null, new Pen(Brushes.White, 1.5), headScreen, 7.0, 7.0);
    }
}
