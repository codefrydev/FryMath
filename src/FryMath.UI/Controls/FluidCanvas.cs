using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FryMath.Core.Fluids;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class FluidCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<FluidCanvas, string>(nameof(PresetName), "karman");

    public static readonly StyledProperty<double> ViscosityProperty =
        AvaloniaProperty.Register<FluidCanvas, double>(nameof(Viscosity), 0.0006);

    public static readonly StyledProperty<bool> ShowVorticityProperty =
        AvaloniaProperty.Register<FluidCanvas, bool>(nameof(ShowVorticity), true);

    public static readonly StyledProperty<bool> ShowVelocityArrowsProperty =
        AvaloniaProperty.Register<FluidCanvas, bool>(nameof(ShowVelocityArrows), true);

    public static readonly StyledProperty<bool> ShowStreamlinesProperty =
        AvaloniaProperty.Register<FluidCanvas, bool>(nameof(ShowStreamlines), true);

    public static readonly StyledProperty<bool> ShowParticlesProperty =
        AvaloniaProperty.Register<FluidCanvas, bool>(nameof(ShowParticles), true);

    public static readonly StyledProperty<bool> IsSimulatingProperty =
        AvaloniaProperty.Register<FluidCanvas, bool>(nameof(IsSimulating), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double Viscosity
    {
        get => GetValue(ViscosityProperty);
        set => SetValue(ViscosityProperty, value);
    }

    public bool ShowVorticity
    {
        get => GetValue(ShowVorticityProperty);
        set => SetValue(ShowVorticityProperty, value);
    }

    public bool ShowVelocityArrows
    {
        get => GetValue(ShowVelocityArrowsProperty);
        set => SetValue(ShowVelocityArrowsProperty, value);
    }

    public bool ShowStreamlines
    {
        get => GetValue(ShowStreamlinesProperty);
        set => SetValue(ShowStreamlinesProperty, value);
    }

    public bool ShowParticles
    {
        get => GetValue(ShowParticlesProperty);
        set => SetValue(ShowParticlesProperty, value);
    }

    public bool IsSimulating
    {
        get => GetValue(IsSimulatingProperty);
        set => SetValue(IsSimulatingProperty, value);
    }

    private readonly NavierStokesEngine _engine = new(100, 65);
    private readonly DispatcherTimer _timer;
    private WriteableBitmap? _bitmap;
    private Point? _hoverPos;
    private Point? _lastMousePos;
    private bool _isLeftMouseDown;
    private bool _isRightMouseDown;

    static FluidCanvas()
    {
        AffectsRender<FluidCanvas>(
            PresetNameProperty,
            ViscosityProperty,
            ShowVorticityProperty,
            ShowVelocityArrowsProperty,
            ShowStreamlinesProperty,
            ShowParticlesProperty,
            IsSimulatingProperty);
    }

    public FluidCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            if (IsSimulating)
            {
                _engine.Step(0.35);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PresetNameProperty)
        {
            string name = change.GetNewValue<string>() ?? "karman";
            var presetType = name.ToLowerInvariant() switch
            {
                "dipole" or "vortexdipole" => FluidPresetType.VortexDipole,
                "kelvin" or "shear" or "kelvinhelmholtz" => FluidPresetType.KelvinHelmholtz,
                "taylor" or "taylorgreen" => FluidPresetType.TaylorGreen,
                "cavity" or "liddrivencavity" => FluidPresetType.LidDrivenCavity,
                _ => FluidPresetType.VonKarman
            };
            _engine.LoadPreset(presetType);
            SetCurrentValue(ViscosityProperty, _engine.Viscosity);
        }
        else if (change.Property == ViscosityProperty)
        {
            _engine.Viscosity = change.GetNewValue<double>();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        _lastMousePos = pt.Position;
        if (pt.Properties.IsLeftButtonPressed) _isLeftMouseDown = true;
        if (pt.Properties.IsRightButtonPressed) _isRightMouseDown = true;
        HandleMouseInteraction(pt.Position);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);
        _hoverPos = pt.Position;

        if (_isLeftMouseDown || _isRightMouseDown)
        {
            HandleMouseInteraction(pt.Position);
        }
        _lastMousePos = pt.Position;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isLeftMouseDown = false;
        _isRightMouseDown = false;
        _lastMousePos = null;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hoverPos = null;
        _isLeftMouseDown = false;
        _isRightMouseDown = false;
        _lastMousePos = null;
        InvalidateVisual();
    }

    private void HandleMouseInteraction(Point currentPos)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 10 || h < 10) return;

        double gridX = (currentPos.X / w) * _engine.Width;
        double gridY = (currentPos.Y / h) * _engine.Height;

        if (_isLeftMouseDown)
        {
            double strokeDx = _lastMousePos.HasValue ? currentPos.X - _lastMousePos.Value.X : 0;
            double strokeDy = _lastMousePos.HasValue ? currentPos.Y - _lastMousePos.Value.Y : 0;
            double curl = (strokeDx - strokeDy) * 0.15;
            if (Math.Abs(curl) < 0.2) curl = 1.8; // default swirl injection
            _engine.InjectVorticity(gridX, gridY, curl, 4.5);
        }
        else if (_isRightMouseDown)
        {
            _engine.AddObstacle(gridX, gridY, 3.5);
        }
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var tokens = new MathThemeTokens();

        // 1. Background Fill
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        // 2. Render Vorticity Bitmap
        if (ShowVorticity)
        {
            RenderVorticityBitmap(context, w, h);
        }

        // 3. Render Streamlines
        if (ShowStreamlines)
        {
            RenderStreamlines(context, w, h);
        }

        // 4. Render Velocity Vector Arrows
        if (ShowVelocityArrows)
        {
            RenderVelocityArrows(context, w, h);
        }

        // 5. Render Obstacles
        RenderObstacles(context, w, h);

        // 6. Render Passive Dye Tracer Particles
        if (ShowParticles)
        {
            RenderParticles(context, w, h);
        }

        // 7. Hover Probe HUD
        if (_hoverPos.HasValue)
        {
            double gx = (_hoverPos.Value.X / w) * _engine.Width;
            double gy = (_hoverPos.Value.Y / h) * _engine.Height;
            DrawHoverHUD(context, _hoverPos.Value, gx, gy, tokens);
        }

        // 8. Top-Right Status Badge
        DrawStatusBadge(context, w, tokens);
    }

    private void RenderVorticityBitmap(DrawingContext context, double screenW, double screenH)
    {
        int gw = _engine.Width;
        int gh = _engine.Height;

        if (_bitmap == null || _bitmap.PixelSize.Width != gw || _bitmap.PixelSize.Height != gh)
        {
            _bitmap = new WriteableBitmap(new PixelSize(gw, gh), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        using (var fb = _bitmap.Lock())
        {
            unsafe
            {
                uint* ptr = (uint*)fb.Address;
                for (int y = 0; y < gh; y++)
                {
                    int row = y * gw;
                    for (int x = 0; x < gw; x++)
                    {
                        int idx = row + x;

                        if (_engine.Obstacles[idx])
                        {
                            // Slate obstacle
                            ptr[idx] = 0xFF2A2038;
                            continue;
                        }

                        double omega = _engine.Omega[idx];
                        // Map vorticity: positive = cyan, negative = rose
                        byte r, g, b;
                        if (omega >= 0)
                        {
                            double t = Math.Min(1.0, omega * 1.8);
                            // Interpolate #0f0c1a to #06b6d4 (cyan)
                            r = (byte)(15 + t * (6 - 15));
                            g = (byte)(12 + t * (182 - 12));
                            b = (byte)(26 + t * (212 - 26));
                        }
                        else
                        {
                            double t = Math.Min(1.0, -omega * 1.8);
                            // Interpolate #0f0c1a to #f43f5e (rose)
                            r = (byte)(15 + t * (244 - 15));
                            g = (byte)(12 + t * (63 - 12));
                            b = (byte)(26 + t * (94 - 26));
                        }

                        // BGRA format
                        ptr[idx] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
                    }
                }
            }
        }

        context.DrawImage(_bitmap, new Rect(0, 0, gw, gh), new Rect(0, 0, screenW, screenH));
    }

    private void RenderObstacles(DrawingContext context, double screenW, double screenH)
    {
        double cellW = screenW / _engine.Width;
        double cellH = screenH / _engine.Height;
        var obstacleBrush = new SolidColorBrush(Color.FromRgb(40, 32, 60));
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 158, 11)), 1.2); // Amber border

        for (int y = 0; y < _engine.Height; y++)
        {
            for (int x = 0; x < _engine.Width; x++)
            {
                int idx = y * _engine.Width + x;
                if (_engine.Obstacles[idx])
                {
                    var rect = new Rect(x * cellW, y * cellH, cellW + 0.5, cellH + 0.5);
                    context.FillRectangle(obstacleBrush, rect);

                    // Check if edge cell to draw perimeter highlight
                    bool isEdge = (x == 0 || x == _engine.Width - 1 || y == 0 || y == _engine.Height - 1 ||
                                   !_engine.Obstacles[idx - 1] || !_engine.Obstacles[idx + 1] ||
                                   !_engine.Obstacles[idx - _engine.Width] || !_engine.Obstacles[idx + _engine.Width]);
                    if (isEdge)
                    {
                        context.DrawRectangle(borderPen, rect);
                    }
                }
            }
        }
    }

    private void RenderStreamlines(DrawingContext context, double screenW, double screenH)
    {
        // Draw contours of constant streamfunction Psi
        var streamPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 56, 189, 248)), 1.0,
            new DashStyle([3, 4], 0));

        double cellW = screenW / _engine.Width;
        double cellH = screenH / _engine.Height;

        for (int y = 2; y < _engine.Height - 2; y += 4)
        {
            for (int x = 2; x < _engine.Width - 2; x += 4)
            {
                int idx = y * _engine.Width + x;
                if (_engine.Obstacles[idx]) continue;

                double u = _engine.U[idx];
                double v = _engine.V[idx];
                double speed = Math.Sqrt(u * u + v * v);
                if (speed < 0.05) continue;

                double sx = (x + 0.5) * cellW;
                double sy = (y + 0.5) * cellH;

                double len = Math.Min(18.0, speed * 12.0);
                double dirX = (u / speed) * len;
                double dirY = (v / speed) * len;

                context.DrawLine(streamPen, new Point(sx - dirX * 0.5, sy - dirY * 0.5),
                    new Point(sx + dirX * 0.5, sy + dirY * 0.5));
            }
        }
    }

    private void RenderVelocityArrows(DrawingContext context, double screenW, double screenH)
    {
        double cellW = screenW / _engine.Width;
        double cellH = screenH / _engine.Height;

        int stepX = Math.Max(3, _engine.Width / 22);
        int stepY = Math.Max(3, _engine.Height / 16);

        for (int y = stepY / 2; y < _engine.Height; y += stepY)
        {
            for (int x = stepX / 2; x < _engine.Width; x += stepX)
            {
                int idx = y * _engine.Width + x;
                if (_engine.Obstacles[idx]) continue;

                double u = _engine.U[idx];
                double v = _engine.V[idx];
                double speed = Math.Sqrt(u * u + v * v);
                if (speed < 0.08) continue;

                double sx = (x + 0.5) * cellW;
                double sy = (y + 0.5) * cellH;

                double arrowLen = Math.Clamp(speed * 14.0, 4.0, 20.0);
                double nx = u / speed;
                double ny = v / speed;

                double tipX = sx + nx * arrowLen;
                double tipY = sy + ny * arrowLen;

                // Color based on speed
                byte alpha = (byte)Math.Clamp(speed * 180 + 50, 60, 220);
                var arrowBrush = new SolidColorBrush(Color.FromArgb(alpha, 6, 182, 212));
                var arrowPen = new Pen(arrowBrush, 1.2);

                context.DrawLine(arrowPen, new Point(sx, sy), new Point(tipX, tipY));

                // Arrowhead barb
                double barbSize = 3.5;
                double perpX = -ny * barbSize;
                double perpY = nx * barbSize;
                context.DrawLine(arrowPen, new Point(tipX, tipY), new Point(tipX - nx * barbSize + perpX, tipY - ny * barbSize + perpY));
                context.DrawLine(arrowPen, new Point(tipX, tipY), new Point(tipX - nx * barbSize - perpX, tipY - ny * barbSize - perpY));
            }
        }
    }

    private void RenderParticles(DrawingContext context, double screenW, double screenH)
    {
        double cellW = screenW / _engine.Width;
        double cellH = screenH / _engine.Height;

        var particleBrush = new SolidColorBrush(Color.FromArgb(200, 245, 158, 11)); // Golden amber dye
        var glowBrush = new SolidColorBrush(Color.FromArgb(60, 245, 158, 11));

        foreach (var p in _engine.Particles)
        {
            double sx = p.X * cellW;
            double sy = p.Y * cellH;

            context.DrawEllipse(glowBrush, null, new Point(sx, sy), 3.5, 3.5);
            context.DrawEllipse(particleBrush, null, new Point(sx, sy), 1.5, 1.5);
        }
    }

    private void DrawHoverHUD(DrawingContext context, Point hoverPt, double gx, double gy, MathThemeTokens tokens)
    {
        int x = (int)Math.Clamp(gx, 0, _engine.Width - 1);
        int y = (int)Math.Clamp(gy, 0, _engine.Height - 1);
        int idx = y * _engine.Width + x;

        double u = _engine.U[idx];
        double v = _engine.V[idx];
        double speed = Math.Sqrt(u * u + v * v);
        double omega = _engine.Omega[idx];
        double psi = _engine.Psi[idx];
        double re = _engine.ReynoldsNumber;

        string line1 = $"Grid Cell:    ({x}, {y})";
        string line2 = $"Velocity:     u = {u:+0.00;-0.00},  v = {v:+0.00;-0.00}  (||u|| = {speed:F2})";
        string line3 = $"Vorticity:    ω = ∇×u = {omega:+0.000;-0.000}";
        string line4 = $"Stream ψ:     ψ = {psi:+0.00;-0.00}   |   Re ≈ {re:F0}";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var monoTypeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal);

        var ft1 = new FormattedText(line1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11, tokens.TextPrimaryBrush);
        var ft2 = new FormattedText(line2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11, tokens.TextSecondaryBrush);
        var ft3 = new FormattedText(line3, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, monoTypeface, 11,
            new SolidColorBrush(omega >= 0 ? Color.FromRgb(6, 182, 212) : Color.FromRgb(244, 63, 94)));
        var ft4 = new FormattedText(line4, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 11, tokens.TextPrimaryBrush);

        double hudW = Math.Max(Math.Max(ft1.Width, ft2.Width), Math.Max(ft3.Width, ft4.Width)) + 24;
        double hudH = 88;

        double hudX = hoverPt.X + 16;
        double hudY = hoverPt.Y - hudH - 8;
        if (hudX + hudW > Bounds.Width) hudX = hoverPt.X - hudW - 16;
        if (hudY < 10) hudY = hoverPt.Y + 16;

        var hudRect = new Rect(hudX, hudY, hudW, hudH);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(230, 24, 20, 39)), hudRect, 8);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(140, 6, 182, 212)), 1.2), hudRect, 8);

        context.DrawText(ft1, new Point(hudX + 12, hudY + 8));
        context.DrawText(ft2, new Point(hudX + 12, hudY + 26));
        context.DrawText(ft3, new Point(hudX + 12, hudY + 44));
        context.DrawText(ft4, new Point(hudX + 12, hudY + 62));
    }

    private void DrawStatusBadge(DrawingContext context, double w, MathThemeTokens tokens)
    {
        double re = _engine.ReynoldsNumber;
        double nu = _engine.Viscosity;
        string badgeText = $"Re = {re:F0}   |   ν = {nu:F5}   |   ∂ω/∂t + (u·∇)ω = ν∇²ω   |   ∇²ψ = -ω";

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var ft = new FormattedText(badgeText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            typeface, 11, new SolidColorBrush(Color.FromRgb(6, 182, 212)));

        double bw = ft.Width + 20;
        double bh = 28;
        double bx = w - bw - 16;
        double by = 16;

        var bRect = new Rect(bx, by, bw, bh);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(210, 24, 20, 39)), bRect, 6);
        context.DrawRectangle(new Pen(new SolidColorBrush(Color.FromArgb(120, 6, 182, 212)), 1.2), bRect, 6);
        context.DrawText(ft, new Point(bx + 10, by + 6));
    }
}
