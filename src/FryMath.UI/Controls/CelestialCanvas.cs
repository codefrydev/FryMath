using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Celestial;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class CelestialCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<CelestialCanvas, string>(nameof(PresetName), "figure8");

    public static readonly StyledProperty<double> TimeStepProperty =
        AvaloniaProperty.Register<CelestialCanvas, double>(nameof(TimeStep), 0.005);

    public static readonly StyledProperty<bool> ShowTrailsProperty =
        AvaloniaProperty.Register<CelestialCanvas, bool>(nameof(ShowTrails), true);

    public static readonly StyledProperty<bool> ShowVelocityVectorsProperty =
        AvaloniaProperty.Register<CelestialCanvas, bool>(nameof(ShowVelocityVectors), true);

    public static readonly StyledProperty<bool> ShowCenterOfMassProperty =
        AvaloniaProperty.Register<CelestialCanvas, bool>(nameof(ShowCenterOfMass), true);

    public static readonly StyledProperty<bool> ShowPotentialGridProperty =
        AvaloniaProperty.Register<CelestialCanvas, bool>(nameof(ShowPotentialGrid), true);

    public static readonly StyledProperty<bool> IsSimulatingProperty =
        AvaloniaProperty.Register<CelestialCanvas, bool>(nameof(IsSimulating), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double TimeStep
    {
        get => GetValue(TimeStepProperty);
        set => SetValue(TimeStepProperty, value);
    }

    public bool ShowTrails
    {
        get => GetValue(ShowTrailsProperty);
        set => SetValue(ShowTrailsProperty, value);
    }

    public bool ShowVelocityVectors
    {
        get => GetValue(ShowVelocityVectorsProperty);
        set => SetValue(ShowVelocityVectorsProperty, value);
    }

    public bool ShowCenterOfMass
    {
        get => GetValue(ShowCenterOfMassProperty);
        set => SetValue(ShowCenterOfMassProperty, value);
    }

    public bool ShowPotentialGrid
    {
        get => GetValue(ShowPotentialGridProperty);
        set => SetValue(ShowPotentialGridProperty, value);
    }

    public bool IsSimulating
    {
        get => GetValue(IsSimulatingProperty);
        set => SetValue(IsSimulatingProperty, value);
    }

    private readonly CelestialNBodyEngine _engine = new();
    private readonly DispatcherTimer _timer;

    // 3D Camera Controls
    private double _camYaw = 0.45;
    private double _camPitch = 0.55;
    private double _camDistance = 4.2;
    private double _panX = 0;
    private double _panY = 0;

    private Point? _lastMousePos;
    private bool _isLeftMouseDown;
    private bool _isRightMouseDown;
    private int _hoveredBodyId = -1;

    static CelestialCanvas()
    {
        AffectsRender<CelestialCanvas>(
            PresetNameProperty,
            TimeStepProperty,
            ShowTrailsProperty,
            ShowVelocityVectorsProperty,
            ShowCenterOfMassProperty,
            ShowPotentialGridProperty,
            IsSimulatingProperty);
    }

    public CelestialCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            if (IsSimulating)
            {
                // Run 2 sub-steps per frame for numerical stability
                _engine.Step(TimeStep * 0.5);
                _engine.Step(TimeStep * 0.5);
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
            string name = change.GetNewValue<string>() ?? "figure8";
            var preset = name.ToLowerInvariant() switch
            {
                "lagrange" or "trojan" or "trojans" => CelestialPreset.LagrangeTrojans,
                "pythagorean" or "burrau" or "chaos" => CelestialPreset.PythagoreanChaos,
                "sunearthmoon" or "moon" or "solar" => CelestialPreset.SunEarthMoon,
                "galaxy" or "merger" or "collision" => CelestialPreset.GalaxyCollision,
                _ => CelestialPreset.FigureEight
            };

            _engine.LoadPreset(preset);

            // Preset camera adjustments
            switch (preset)
            {
                case CelestialPreset.FigureEight:
                    _camYaw = 0.15;
                    _camPitch = 0.25;
                    _camDistance = 3.6;
                    break;
                case CelestialPreset.LagrangeTrojans:
                    _camYaw = 0.0;
                    _camPitch = 0.85;
                    _camDistance = 5.2;
                    break;
                case CelestialPreset.PythagoreanChaos:
                    _camYaw = 0.0;
                    _camPitch = 0.0;
                    _camDistance = 8.5;
                    break;
                case CelestialPreset.SunEarthMoon:
                    _camYaw = 0.2;
                    _camPitch = 0.65;
                    _camDistance = 6.5;
                    break;
                case CelestialPreset.GalaxyCollision:
                    _camYaw = 0.75;
                    _camPitch = 0.65;
                    _camDistance = 7.5;
                    break;
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetCurrentPoint(this);
        _lastMousePos = pt.Position;
        if (pt.Properties.IsLeftButtonPressed) _isLeftMouseDown = true;
        if (pt.Properties.IsRightButtonPressed) _isRightMouseDown = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pt = e.GetCurrentPoint(this);
        var pos = pt.Position;

        if (_lastMousePos.HasValue)
        {
            var delta = pos - _lastMousePos.Value;
            if (_isLeftMouseDown)
            {
                _camYaw += delta.X * 0.01;
                _camPitch = Math.Clamp(_camPitch + delta.Y * 0.01, -1.5, 1.5);
                InvalidateVisual();
            }
            else if (_isRightMouseDown)
            {
                _panX += delta.X * 0.008;
                _panY += delta.Y * 0.008;
                InvalidateVisual();
            }
        }

        _lastMousePos = pos;

        // Hover probe for nearest celestial body
        double cx = Bounds.Width * 0.5 + _panX * 100;
        double cy = Bounds.Height * 0.5 + _panY * 100;
        double scale = Math.Min(Bounds.Width, Bounds.Height) / _camDistance;

        int closestId = -1;
        double minDist = 24.0; // 24 px pickup radius

        foreach (var b in _engine.Bodies)
        {
            var p2D = Project3DTo2D(b.X, b.Y, b.Z, cx, cy, scale);
            if (p2D.HasValue)
            {
                double d = Math.Sqrt(Math.Pow(pos.X - p2D.Value.X, 2) + Math.Pow(pos.Y - p2D.Value.Y, 2));
                if (d < minDist)
                {
                    minDist = d;
                    closestId = b.Id;
                }
            }
        }

        if (_hoveredBodyId != closestId)
        {
            _hoveredBodyId = closestId;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isLeftMouseDown = false;
        _isRightMouseDown = false;
        _lastMousePos = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _camDistance = Math.Clamp(_camDistance - e.Delta.Y * 0.4, 1.2, 25.0);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width <= 10 || height <= 10) return;

        // Background
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(10, 10, 20)), new Rect(0, 0, width, height));

        double cx = width * 0.5 + _panX * 100;
        double cy = height * 0.5 + _panY * 100;
        double scale = Math.Min(width, height) / _camDistance;

        // 1. Gravitational Potential Grid (Plane Z = 0)
        if (ShowPotentialGrid)
        {
            DrawPotentialGrid(context, cx, cy, scale);
        }

        // 2. Orbital Trails
        if (ShowTrails)
        {
            DrawTrails(context, cx, cy, scale);
        }

        // 3. Center of Mass Indicator
        if (ShowCenterOfMass)
        {
            var cm = _engine.CenterOfMass;
            var cm2D = Project3DTo2D(cm.X, cm.Y, cm.Z, cx, cy, scale);
            if (cm2D.HasValue)
            {
                var yellowPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 251, 191, 36)), 1.2);
                context.DrawLine(yellowPen, new Point(cm2D.Value.X - 8, cm2D.Value.Y), new Point(cm2D.Value.X + 8, cm2D.Value.Y));
                context.DrawLine(yellowPen, new Point(cm2D.Value.X, cm2D.Value.Y - 8), new Point(cm2D.Value.X, cm2D.Value.Y + 8));
                context.DrawEllipse(null, yellowPen, cm2D.Value, 5, 5);
            }
        }

        // 4. Celestial Bodies & Velocity Vectors
        DrawBodies(context, cx, cy, scale);

        // 5. Diagnostics HUD Badge & Hover Probe
        DrawDiagnosticsHUD(context, width, height);
    }

    private void DrawPotentialGrid(DrawingContext context, double cx, double cy, double scale)
    {
        double extent = _camDistance * 0.55;
        int steps = 18;
        double d = (2 * extent) / steps;

        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(28, 99, 102, 241)), 1.0); // Faint indigo

        for (int i = 0; i <= steps; i++)
        {
            double coord = -extent + i * d;

            // X-parallel line
            Point? prevPt = null;
            for (int j = 0; j <= steps; j++)
            {
                double other = -extent + j * d;
                double phi = _engine.EvaluatePotentialAt(coord, other, 0);
                double zOffset = Math.Max(phi * 0.15, -0.8); // Warp down into well

                var p = Project3DTo2D(coord, other, zOffset, cx, cy, scale);
                if (p.HasValue && prevPt.HasValue)
                {
                    context.DrawLine(gridPen, prevPt.Value, p.Value);
                }
                prevPt = p;
            }

            // Y-parallel line
            prevPt = null;
            for (int j = 0; j <= steps; j++)
            {
                double other = -extent + j * d;
                double phi = _engine.EvaluatePotentialAt(other, coord, 0);
                double zOffset = Math.Max(phi * 0.15, -0.8);

                var p = Project3DTo2D(other, coord, zOffset, cx, cy, scale);
                if (p.HasValue && prevPt.HasValue)
                {
                    context.DrawLine(gridPen, prevPt.Value, p.Value);
                }
                prevPt = p;
            }
        }
    }

    private void DrawTrails(DrawingContext context, double cx, double cy, double scale)
    {
        foreach (var b in _engine.Bodies)
        {
            if (b.Trail.Count < 2) continue;

            Color baseColor = Color.Parse(b.ColorHex);

            for (int i = 1; i < b.Trail.Count; i++)
            {
                double alpha = (double)i / b.Trail.Count; // Fade towards tail
                byte a = (byte)(alpha * 190);
                var trailPen = new Pen(new SolidColorBrush(Color.FromArgb(a, baseColor.R, baseColor.G, baseColor.B)), 1.4);

                var pt1 = Project3DTo2D(b.Trail[i - 1].X, b.Trail[i - 1].Y, b.Trail[i - 1].Z, cx, cy, scale);
                var pt2 = Project3DTo2D(b.Trail[i].X, b.Trail[i].Y, b.Trail[i].Z, cx, cy, scale);

                if (pt1.HasValue && pt2.HasValue)
                {
                    context.DrawLine(trailPen, pt1.Value, pt2.Value);
                }
            }
        }
    }

    private void DrawBodies(DrawingContext context, double cx, double cy, double scale)
    {
        // Sort bodies by camera depth (painters algorithm)
        var sortedBodies = new (CelestialBody Body, double Depth, Point ScreenPos)[_engine.Bodies.Count];
        int count = 0;

        foreach (var b in _engine.Bodies)
        {
            var p = Project3DTo2DWithDepth(b.X, b.Y, b.Z, cx, cy, scale);
            if (p.HasValue)
            {
                sortedBodies[count++] = (b, p.Value.Depth, p.Value.Screen);
            }
        }

        Array.Sort(sortedBodies, 0, count, Comparer<(CelestialBody Body, double Depth, Point ScreenPos)>.Create((a, b) => b.Depth.CompareTo(a.Depth)));

        for (int i = 0; i < count; i++)
        {
            var item = sortedBodies[i];
            var b = item.Body;
            var pos = item.ScreenPos;
            Color baseColor = Color.Parse(b.ColorHex);

            double radiusPx = Math.Max(b.Radius * scale * 0.65, 3.0);
            if (b.Id == _hoveredBodyId) radiusPx *= 1.35;

            // Halo glow
            var haloBrush = new SolidColorBrush(Color.FromArgb(45, baseColor.R, baseColor.G, baseColor.B));
            context.DrawEllipse(haloBrush, null, pos, radiusPx * 2.2, radiusPx * 2.2);

            // Core sphere
            var coreBrush = new SolidColorBrush(baseColor);
            var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 1.2);
            context.DrawEllipse(coreBrush, borderPen, pos, radiusPx, radiusPx);

            // Velocity vector arrow
            if (ShowVelocityVectors && (b.Mass > 1e-5 || b.Id == _hoveredBodyId))
            {
                double vScale = 0.45;
                var vHead = Project3DTo2D(b.X + b.Vx * vScale, b.Y + b.Vy * vScale, b.Z + b.Vz * vScale, cx, cy, scale);
                if (vHead.HasValue)
                {
                    var vPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 244, 63, 94)), 1.5);
                    context.DrawLine(vPen, pos, vHead.Value);

                    // Tiny arrowhead
                    double dx = vHead.Value.X - pos.X;
                    double dy = vHead.Value.Y - pos.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 8)
                    {
                        double ux = dx / len;
                        double uy = dy / len;
                        Point a1 = new(vHead.Value.X - ux * 5 - uy * 3, vHead.Value.Y - uy * 5 + ux * 3);
                        Point a2 = new(vHead.Value.X - ux * 5 + uy * 3, vHead.Value.Y - uy * 5 - ux * 3);
                        context.DrawLine(vPen, vHead.Value, a1);
                        context.DrawLine(vPen, vHead.Value, a2);
                    }
                }
            }

            // Body name label
            if (b.Mass > 1e-4 || b.Id == _hoveredBodyId)
            {
                var nameFormatted = new FormattedText(
                    b.Name,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Inter,Segoe UI,sans-serif", FontStyle.Normal, FontWeight.SemiBold),
                    10.5,
                    new SolidColorBrush(Color.FromRgb(226, 232, 240))
                );
                context.DrawText(nameFormatted, new Point(pos.X + radiusPx + 6, pos.Y - 7));
            }
        }
    }

    private void DrawDiagnosticsHUD(DrawingContext context, double width, double height)
    {
        // Top Badging Banner
        string presetName = _engine.CurrentPreset switch
        {
            CelestialPreset.FigureEight => "Figure-8 Choreography",
            CelestialPreset.LagrangeTrojans => "Lagrange L4/L5 Trojans",
            CelestialPreset.PythagoreanChaos => "Pythagorean Chaos",
            CelestialPreset.SunEarthMoon => "Sun-Earth-Moon",
            CelestialPreset.GalaxyCollision => "Galactic Collision",
            _ => "N-Body Dynamics"
        };

        string hudText = $"{presetName} | N = {_engine.Bodies.Count} | E = {_engine.TotalEnergy:F3} | ΔE/E₀ = {_engine.EnergyDrift:E2} | |L| = {Math.Sqrt(Math.Pow(_engine.TotalAngularMomentum.X, 2) + Math.Pow(_engine.TotalAngularMomentum.Y, 2) + Math.Pow(_engine.TotalAngularMomentum.Z, 2)):F3}";

        var hudFormatted = new FormattedText(
            hudText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas,Menlo,monospace", FontStyle.Normal, FontWeight.Bold),
            11.5,
            new SolidColorBrush(Color.FromRgb(6, 182, 212)) // Cyan
        );

        double badgeW = hudFormatted.Width + 24;
        double badgeH = 26;
        double badgeX = Math.Max(12, (width - badgeW) * 0.5);
        double badgeY = 12;

        var badgeBg = new SolidColorBrush(Color.FromArgb(210, 15, 23, 42));
        var badgeBorder = new Pen(new SolidColorBrush(Color.FromArgb(140, 6, 182, 212)), 1);
        context.DrawRectangle(badgeBg, badgeBorder, new Rect(badgeX, badgeY, badgeW, badgeH), 6, 6);
        context.DrawText(hudFormatted, new Point(badgeX + 12, badgeY + 5));

        // Hover Probe Details HUD (Bottom Left)
        if (_hoveredBodyId >= 0)
        {
            var b = _engine.Bodies.Find(body => body.Id == _hoveredBodyId);
            if (b != null)
            {
                double speed = Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy + b.Vz * b.Vz);
                string probeText = $"SELECTED BODY: {b.Name}\n" +
                                   $"• Mass: {b.Mass:F4} M☉\n" +
                                   $"• Position: ({b.X:F3}, {b.Y:F3}, {b.Z:F3})\n" +
                                   $"• Velocity: ({b.Vx:F3}, {b.Vy:F3}, {b.Vz:F3}) [|v| = {speed:F3}]\n" +
                                   $"• Accel: ({b.Ax:F3}, {b.Ay:F3}, {b.Az:F3})";

                var probeFormatted = new FormattedText(
                    probeText,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Consolas,Menlo,monospace", FontStyle.Normal, FontWeight.SemiBold),
                    10.5,
                    new SolidColorBrush(Color.FromRgb(241, 245, 249))
                );

                double pW = probeFormatted.Width + 20;
                double pH = probeFormatted.Height + 16;
                double pX = 16;
                double pY = height - pH - 16;

                context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 15, 23, 42)), new Pen(new SolidColorBrush(Color.FromRgb(244, 63, 94)), 1), new Rect(pX, pY, pW, pH), 6, 6);
                context.DrawText(probeFormatted, new Point(pX + 10, pY + 8));
            }
        }
    }

    private Point? Project3DTo2D(double x, double y, double z, double cx, double cy, double scale)
    {
        var res = Project3DTo2DWithDepth(x, y, z, cx, cy, scale);
        return res?.Screen;
    }

    private (Point Screen, double Depth)? Project3DTo2DWithDepth(double x, double y, double z, double cx, double cy, double scale)
    {
        // Camera rotation: Yaw around Z, Pitch around X
        double cosY = Math.Cos(_camYaw);
        double sinY = Math.Sin(_camYaw);

        double x1 = x * cosY - y * sinY;
        double y1 = x * sinY + y * cosY;
        double z1 = z;

        double cosP = Math.Cos(_camPitch);
        double sinP = Math.Sin(_camPitch);

        double y2 = y1 * cosP - z1 * sinP;
        double z2 = y1 * sinP + z1 * cosP;
        double x2 = x1;

        // Perspective projection
        double d = _camDistance + y2;
        if (d <= 0.2) return null;

        double fov = 1.0 / d;
        double sx = cx + x2 * scale * fov;
        double sy = cy - z2 * scale * fov;

        return (new Point(sx, sy), y2);
    }
}
