using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Clifford;
using FryMath.Core.LinearAlgebra;
using FryMath.UI.Theming;
using Vector3D = FryMath.Core.LinearAlgebra.Vector3D;

namespace FryMath.UI.Controls;

public class CliffordCanvas : Control
{
    public static readonly StyledProperty<string> CliffordModeProperty =
        AvaloniaProperty.Register<CliffordCanvas, string>(nameof(CliffordMode), "wedge");

    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<CliffordCanvas, string>(nameof(PresetName), "wedge");

    public static readonly StyledProperty<double> VectorAXProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(VectorAX), 1.8);

    public static readonly StyledProperty<double> VectorAYProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(VectorAY), 0.6);

    public static readonly StyledProperty<double> VectorAZProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(VectorAZ), 0.0);

    public static readonly StyledProperty<double> VectorBXProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(VectorBX), 0.4);

    public static readonly StyledProperty<double> VectorBYProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(VectorBY), 1.6);

    public static readonly StyledProperty<double> VectorBZProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(VectorBZ), 0.8);

    public static readonly StyledProperty<double> RotorAngleProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(RotorAngle), 1.047);

    public static readonly StyledProperty<double> ReflectionAngleProperty =
        AvaloniaProperty.Register<CliffordCanvas, double>(nameof(ReflectionAngle), 0.524);

    public static readonly StyledProperty<bool> ShowBivectorDiskProperty =
        AvaloniaProperty.Register<CliffordCanvas, bool>(nameof(ShowBivectorDisk), true);

    public static readonly StyledProperty<bool> ShowDualNormalProperty =
        AvaloniaProperty.Register<CliffordCanvas, bool>(nameof(ShowDualNormal), true);

    public static readonly StyledProperty<bool> ShowMultivectorGradesProperty =
        AvaloniaProperty.Register<CliffordCanvas, bool>(nameof(ShowMultivectorGrades), true);

    public static readonly StyledProperty<bool> ShowRotorArcProperty =
        AvaloniaProperty.Register<CliffordCanvas, bool>(nameof(ShowRotorArc), true);

    public string CliffordMode
    {
        get => GetValue(CliffordModeProperty);
        set => SetValue(CliffordModeProperty, value);
    }

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public double VectorAX
    {
        get => GetValue(VectorAXProperty);
        set => SetValue(VectorAXProperty, value);
    }

    public double VectorAY
    {
        get => GetValue(VectorAYProperty);
        set => SetValue(VectorAYProperty, value);
    }

    public double VectorAZ
    {
        get => GetValue(VectorAZProperty);
        set => SetValue(VectorAZProperty, value);
    }

    public double VectorBX
    {
        get => GetValue(VectorBXProperty);
        set => SetValue(VectorBXProperty, value);
    }

    public double VectorBY
    {
        get => GetValue(VectorBYProperty);
        set => SetValue(VectorBYProperty, value);
    }

    public double VectorBZ
    {
        get => GetValue(VectorBZProperty);
        set => SetValue(VectorBZProperty, value);
    }

    public double RotorAngle
    {
        get => GetValue(RotorAngleProperty);
        set => SetValue(RotorAngleProperty, value);
    }

    public double ReflectionAngle
    {
        get => GetValue(ReflectionAngleProperty);
        set => SetValue(ReflectionAngleProperty, value);
    }

    public bool ShowBivectorDisk
    {
        get => GetValue(ShowBivectorDiskProperty);
        set => SetValue(ShowBivectorDiskProperty, value);
    }

    public bool ShowDualNormal
    {
        get => GetValue(ShowDualNormalProperty);
        set => SetValue(ShowDualNormalProperty, value);
    }

    public bool ShowMultivectorGrades
    {
        get => GetValue(ShowMultivectorGradesProperty);
        set => SetValue(ShowMultivectorGradesProperty, value);
    }

    public bool ShowRotorArc
    {
        get => GetValue(ShowRotorArcProperty);
        set => SetValue(ShowRotorArcProperty, value);
    }

    private readonly MathThemeTokens _tokens = MathThemeTokens.DarkElectric;

    private double _yaw = 0.65;
    private double _pitch = 0.40;
    private double _zoom = 1.0;
    private bool _isOrbiting;
    private Point _lastMouse;

    static CliffordCanvas()
    {
        AffectsRender<CliffordCanvas>(
            CliffordModeProperty, PresetNameProperty,
            VectorAXProperty, VectorAYProperty, VectorAZProperty,
            VectorBXProperty, VectorBYProperty, VectorBZProperty,
            RotorAngleProperty, ReflectionAngleProperty,
            ShowBivectorDiskProperty, ShowDualNormalProperty,
            ShowMultivectorGradesProperty, ShowRotorArcProperty
        );
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        var prop = e.GetCurrentPoint(this).Properties;

        if (prop.IsLeftButtonPressed && pt.X < Bounds.Width * 0.62)
        {
            _isOrbiting = true;
            _lastMouse = pt;
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isOrbiting)
        {
            var pt = e.GetPosition(this);
            double dx = pt.X - _lastMouse.X;
            double dy = pt.Y - _lastMouse.Y;
            _yaw += dx * 0.008;
            _pitch = Math.Clamp(_pitch + dy * 0.008, -1.4, 1.4);
            _lastMouse = pt;
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isOrbiting = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _zoom = Math.Clamp(_zoom + e.Delta.Y * 0.08, 0.4, 2.5);
        InvalidateVisual();
        e.Handled = true;
    }

    private (Point Pt, double Depth) Project3D(Vector3D v, Rect bounds)
    {
        double cosY = Math.Cos(_yaw), sinY = Math.Sin(_yaw);
        double cosP = Math.Cos(_pitch), sinP = Math.Sin(_pitch);

        // Yaw rotation around Y axis
        double x1 = v.X * cosY - v.Z * sinY;
        double z1 = v.X * sinY + v.Z * cosY;

        // Pitch rotation around X axis
        double y2 = v.Y * cosP - z1 * sinP;
        double z2 = v.Y * sinP + z1 * cosP;

        double depth = z2 + 8.0;
        if (depth < 0.2) depth = 0.2;

        double scale = (8.0 / depth) * _zoom * (Math.Min(bounds.Width, bounds.Height) / 5.2);
        double px = bounds.X + bounds.Width * 0.5 + x1 * scale;
        double py = bounds.Y + bounds.Height * 0.55 - y2 * scale;

        return (new Point(px, py), depth);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width < 200 || bounds.Height < 150) return;

        // Background
        context.FillRectangle(_tokens.BgBrush, bounds);

        double splitX = bounds.Width * 0.60;
        Rect leftRect = new(0, 0, splitX, bounds.Height);
        Rect rightTopRect = new(splitX, 0, bounds.Width - splitX, bounds.Height * 0.50);
        Rect rightBottomRect = new(splitX, bounds.Height * 0.50, bounds.Width - splitX, bounds.Height * 0.50);

        // Draw Left 3D Panel
        Render3DCliffordSpace(context, leftRect);

        // Draw Right Top Panel: Multivector Blade Decomposition & Product
        RenderMultivectorInspector(context, rightTopRect);

        // Draw Right Bottom Panel: Rotor Kinematics & Sandwich Product / Spacetime
        RenderRotorKinematics(context, rightBottomRect);
    }

    private void Render3DCliffordSpace(DrawingContext context, Rect rect)
    {
        using (context.PushClip(rect))
        {
            // Panel background & subtle border
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), rect);

            // Ground grid in e1-e2 plane
            DrawGroundGrid(context, rect);

            // Basis coordinate axes e1, e2, e3
            DrawCoordinateAxes(context, rect);

            Vector3D a = new(VectorAX, VectorAY, VectorAZ);
            Vector3D b = new(VectorBX, VectorBY, VectorBZ);

            string mode = CliffordMode.ToLowerInvariant();

            if (mode == "wedge" || mode == "multivector")
            {
                DrawWedgeAndBivector(context, rect, a, b);
            }
            else if (mode == "rotor")
            {
                DrawRotorRotation(context, rect, a, b);
            }
            else if (mode == "reflection")
            {
                DrawDoubleReflection(context, rect, a);
            }
            else if (mode == "maxwell")
            {
                DrawMaxwellBivector(context, rect, a, b);
            }
            else if (mode == "spacetime")
            {
                DrawSpacetimeBoost(context, rect);
            }

            // Panel Title badge
            DrawBadge(context, new Point(rect.X + 16, rect.Y + 16),
                "CLIFFORD 3D GEOMETRIC SPACE G₃", _tokens.AccentCyanBrush);

            // Camera stats HUD
            string hudText = $"Yaw: {_yaw * 180 / Math.PI:F0}° | Pitch: {_pitch * 180 / Math.PI:F0}° | Zoom: {_zoom:F2}x (Drag to orbit, scroll to zoom)";
            FormattedText ftHud = new(
                hudText,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif"),
                11,
                _tokens.TextMutedBrush
            );
            context.DrawText(ftHud, new Point(rect.X + 16, rect.Bottom - 26));
        }
    }

    private void DrawGroundGrid(DrawingContext context, Rect rect)
    {
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)), 1);
        int gridSteps = 4;
        double stepSize = 0.8;

        for (int i = -gridSteps; i <= gridSteps; i++)
        {
            double val = i * stepSize;
            var (p1, _) = Project3D(new Vector3D(val, 0, -gridSteps * stepSize), rect);
            var (p2, _) = Project3D(new Vector3D(val, 0, gridSteps * stepSize), rect);
            context.DrawLine(gridPen, p1, p2);

            var (q1, _) = Project3D(new Vector3D(-gridSteps * stepSize, 0, val), rect);
            var (q2, _) = Project3D(new Vector3D(gridSteps * stepSize, 0, val), rect);
            context.DrawLine(gridPen, q1, q2);
        }
    }

    private void DrawCoordinateAxes(DrawingContext context, Rect rect)
    {
        double axisLen = 2.4;
        var (origin, _) = Project3D(Vector3D.Zero, rect);

        // e1 (X axis - Red / Rose)
        var (e1Pt, _) = Project3D(new Vector3D(axisLen, 0, 0), rect);
        context.DrawLine(new Pen(_tokens.AccentRoseBrush, 2), origin, e1Pt);
        DrawAxisLabel(context, e1Pt, "e₁", _tokens.AccentRoseBrush);

        // e2 (Y axis - Green / Emerald)
        var (e2Pt, _) = Project3D(new Vector3D(0, axisLen, 0), rect);
        context.DrawLine(new Pen(_tokens.AccentEmeraldBrush, 2), origin, e2Pt);
        DrawAxisLabel(context, e2Pt, "e₂", _tokens.AccentEmeraldBrush);

        // e3 (Z axis - Blue / Cyan)
        var (e3Pt, _) = Project3D(new Vector3D(0, 0, axisLen), rect);
        context.DrawLine(new Pen(_tokens.AccentCyanBrush, 2), origin, e3Pt);
        DrawAxisLabel(context, e3Pt, "e₃", _tokens.AccentCyanBrush);
    }

    private void DrawWedgeAndBivector(DrawingContext context, Rect rect, Vector3D a, Vector3D b)
    {
        var (origin, _) = Project3D(Vector3D.Zero, rect);
        var (aPt, _) = Project3D(a, rect);
        var (bPt, _) = Project3D(b, rect);
        var (abPt, _) = Project3D(a + b, rect);

        // Draw Oriented Bivector Parallelogram
        if (ShowBivectorDisk)
        {
            var streamGeom = new StreamGeometry();
            using (var ctx = streamGeom.Open())
            {
                ctx.BeginFigure(origin, isFilled: true);
                ctx.LineTo(aPt);
                ctx.LineTo(abPt);
                ctx.LineTo(bPt);
                ctx.EndFigure(isClosed: true);
            }

            var bivectorFill = new SolidColorBrush(Color.FromArgb(70, 0, 220, 255));
            var bivectorPen = new Pen(_tokens.AccentCyanBrush, 1.5, DashStyle.Dash);
            context.DrawGeometry(bivectorFill, bivectorPen, streamGeom);

            // Oriented circulation arrow in the center of the parallelogram
            Point center = new(
                (origin.X + aPt.X + abPt.X + bPt.X) * 0.25,
                (origin.Y + aPt.Y + abPt.Y + bPt.Y) * 0.25
            );
            context.DrawEllipse(null, new Pen(_tokens.AccentAmberBrush, 1.5), center, 14, 14);

            // Bivector symbol badge in center
            FormattedText ftWedge = new(
                "a ∧ b",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.SemiBold),
                12,
                _tokens.AccentAmberBrush
            );
            context.DrawText(ftWedge, new Point(center.X - 14, center.Y - 7));
        }

        // Draw Vector a (Cyan)
        DrawArrow3D(context, origin, aPt, _tokens.AccentCyanBrush, 3.0, "a");

        // Draw Vector b (Amber)
        DrawArrow3D(context, origin, bPt, _tokens.AccentAmberBrush, 3.0, "b");

        // Draw Dual Normal Vector n = -I(a ^ b) = a x b
        if (ShowDualNormal)
        {
            Vector3D cross = a.Cross(b);
            var (normalPt, _) = Project3D(cross, rect);
            DrawArrow3D(context, origin, normalPt, _tokens.AccentEmeraldBrush, 2.5, "n = -I(a∧b)");
        }
    }

    private void DrawRotorRotation(DrawingContext context, Rect rect, Vector3D a, Vector3D b)
    {
        var (origin, _) = Project3D(Vector3D.Zero, rect);

        // Bivector plane defined by a and b
        Bivector3D plane = CliffordEngine.Wedge(a, b).Normalize();
        Rotor3D rotor = Rotor3D.FromAngleAndPlane(RotorAngle, plane);

        // Vector to rotate: v
        Vector3D v = a;
        Vector3D vRotated = rotor.Rotate(v);

        var (vPt, _) = Project3D(v, rect);
        var (vRotPt, _) = Project3D(vRotated, rect);

        // Draw original vector v (Cyan)
        DrawArrow3D(context, origin, vPt, _tokens.AccentCyanBrush, 2.5, "v");

        // Draw rotated vector v' (Gold / Amber)
        DrawArrow3D(context, origin, vRotPt, _tokens.AccentAmberBrush, 3.2, "v' = R v R†");

        // Draw arc trajectory
        if (ShowRotorArc)
        {
            int steps = 30;
            Point prevPt = vPt;
            var arcPen = new Pen(_tokens.AccentRoseBrush, 2, DashStyle.Dash);

            for (int i = 1; i <= steps; i++)
            {
                double angleStep = RotorAngle * (i / (double)steps);
                Rotor3D stepRotor = Rotor3D.FromAngleAndPlane(angleStep, plane);
                Vector3D stepVec = stepRotor.Rotate(v);
                var (stepPt, _) = Project3D(stepVec, rect);
                context.DrawLine(arcPen, prevPt, stepPt);
                prevPt = stepPt;
            }
        }

        // Draw the plane normal
        Vector3D normal = plane.Dual() * 1.5;
        var (nPt, _) = Project3D(normal, rect);
        DrawArrow3D(context, origin, nPt, _tokens.AccentEmeraldBrush, 2.0, "Rotation Axis");
    }

    private void DrawDoubleReflection(DrawingContext context, Rect rect, Vector3D v)
    {
        var (origin, _) = Project3D(Vector3D.Zero, rect);

        // Normal 1 along e1
        Vector3D n1 = new(1, 0, 0);
        // Normal 2 rotated by reflection angle
        Vector3D n2 = new(Math.Cos(ReflectionAngle), Math.Sin(ReflectionAngle), 0);

        var (vReflectedTwice, rotor) = CliffordEngine.DoubleReflect(v, n1, n2);
        Vector3D vReflectedOnce = CliffordEngine.Reflect(v, n1);

        var (vPt, _) = Project3D(v, rect);
        var (v1Pt, _) = Project3D(vReflectedOnce, rect);
        var (v2Pt, _) = Project3D(vReflectedTwice, rect);

        var (n1Pt, _) = Project3D(n1 * 1.6, rect);
        var (n2Pt, _) = Project3D(n2 * 1.6, rect);

        // Draw Mirror Normals
        DrawArrow3D(context, origin, n1Pt, _tokens.AccentRoseBrush, 2.0, "n₁");
        DrawArrow3D(context, origin, n2Pt, _tokens.AccentEmeraldBrush, 2.0, "n₂");

        // Original vector v
        DrawArrow3D(context, origin, vPt, _tokens.AccentCyanBrush, 2.8, "v");

        // Step 1: v' = -n1 v n1 (dim / dashed)
        context.DrawLine(new Pen(_tokens.TextMutedBrush, 1.5, DashStyle.Dash), origin, v1Pt);
        DrawAxisLabel(context, v1Pt, "v₁ = -n₁vn₁", _tokens.TextMutedBrush);

        // Step 2: v'' = (n2 n1) v (n1 n2) = R v R†
        DrawArrow3D(context, origin, v2Pt, _tokens.AccentAmberBrush, 3.2, "v₂ = R v R†");
    }

    private void DrawMaxwellBivector(DrawingContext context, Rect rect, Vector3D E, Vector3D B)
    {
        var (origin, _) = Project3D(Vector3D.Zero, rect);

        var (ePt, _) = Project3D(E, rect);
        var (bPt, _) = Project3D(B, rect);

        // Electric field vector (Cyan)
        DrawArrow3D(context, origin, ePt, _tokens.AccentCyanBrush, 3.0, "E (Grade 1 Vector)");

        // Magnetic field vector (Amber)
        DrawArrow3D(context, origin, bPt, _tokens.AccentAmberBrush, 3.0, "B (Dual of Bivector)");

        // Poynting vector S = E x B = -I(E ^ B)
        Vector3D S = E.Cross(B) * 0.5;
        var (sPt, _) = Project3D(S, rect);
        DrawArrow3D(context, origin, sPt, _tokens.AccentEmeraldBrush, 3.0, "S = E × B = -I(E∧B)");
    }

    private void DrawSpacetimeBoost(DrawingContext context, Rect rect)
    {
        var (origin, _) = Project3D(Vector3D.Zero, rect);

        // Spacetime lightcone rays
        double c = 2.0;
        var (cPlus, _) = Project3D(new Vector3D(c, c, 0), rect);
        var (cMinus, _) = Project3D(new Vector3D(-c, c, 0), rect);

        context.DrawLine(new Pen(_tokens.AccentAmberBrush, 2, DashStyle.Dash), origin, cPlus);
        context.DrawLine(new Pen(_tokens.AccentAmberBrush, 2, DashStyle.Dash), origin, cMinus);

        // Worldline boost
        double beta = Math.Tanh(RotorAngle * 0.8);
        var (boosted, _) = Project3D(new Vector3D(beta * 1.8, 1.8, 0), rect);
        DrawArrow3D(context, origin, boosted, _tokens.AccentCyanBrush, 3.0, "Boosted Worldline");
    }

    private void RenderMultivectorInspector(DrawingContext context, Rect rect)
    {
        using (context.PushClip(rect))
        {
            // Panel background & card styling
            context.FillRectangle(_tokens.SurfaceBrush, rect);
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), rect);

            // Title
            DrawBadge(context, new Point(rect.X + 16, rect.Y + 14),
                "MULTIVECTOR BLADE DECOMPOSITION & GEOMETRIC PRODUCT", _tokens.AccentCyanBrush);

            Vector3D a = new(VectorAX, VectorAY, VectorAZ);
            Vector3D b = new(VectorBX, VectorBY, VectorBZ);

            Multivector3 ab = CliffordEngine.GeometricProduct(a, b);
            double dot = a.Dot(b);
            Bivector3D wedge = CliffordEngine.Wedge(a, b);

            double y = rect.Y + 44;

            // Row 1: Grade 0 (Scalar)
            DrawGradeRow(context, rect.X + 16, y, "Grade 0 (Scalar / Inner Product):",
                $"⟨ab⟩₀ = a · b = {dot:F3}",
                "Measures symmetric collinearity: |a||b|cos(θ)",
                _tokens.AccentEmeraldBrush);

            y += 42;
            // Row 2: Grade 1 (Vector)
            DrawGradeRow(context, rect.X + 16, y, "Grade 1 (Vector):",
                "⟨ab⟩₁ = 0.00 e₁ + 0.00 e₂ + 0.00 e₃",
                "Vector component vanishes for pure vector-vector geometric product",
                _tokens.TextMutedBrush);

            y += 42;
            // Row 3: Grade 2 (Bivector / Outer Product)
            DrawGradeRow(context, rect.X + 16, y, "Grade 2 (Bivector / Outer Wedge):",
                $"⟨ab⟩₂ = a ∧ b = {wedge.E12:F2} e₁₂ + {wedge.E23:F2} e₂₃ + {wedge.E31:F2} e₃₁  (|a∧b| = {wedge.Magnitude:F3})",
                "Oriented planar area element. Area = |a||b|sin(θ)",
                _tokens.AccentAmberBrush);

            y += 42;
            // Row 4: Grade 3 (Trivector / Pseudoscalar)
            DrawGradeRow(context, rect.X + 16, y, "Grade 3 (Pseudoscalar Volume):",
                "⟨ab⟩₃ = 0.00 I (I = e₁₂₃)",
                "Oriented 3D volume element; vanishes for 2-vector product",
                _tokens.TextMutedBrush);

            y += 44;
            // Full equation summary
            string fullEquation = $"Full Geometric Product: ab = {dot:F2} + ({wedge.E12:F2}e₁₂ + {wedge.E23:F2}e₂₃ + {wedge.E31:F2}e₃₁)";
            FormattedText ftFull = new(
                fullEquation,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
                13,
                _tokens.AccentRoseBrush
            );
            context.DrawText(ftFull, new Point(rect.X + 16, y));
        }
    }

    private void RenderRotorKinematics(DrawingContext context, Rect rect)
    {
        using (context.PushClip(rect))
        {
            // Panel background & card styling
            context.FillRectangle(_tokens.SurfaceBrush, rect);
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), rect);

            // Title
            DrawBadge(context, new Point(rect.X + 16, rect.Y + 14),
                "ROTOR KINEMATICS & SANDWICH PRODUCT v' = R v R†", _tokens.AccentAmberBrush);

            Vector3D a = new(VectorAX, VectorAY, VectorAZ);
            Vector3D b = new(VectorBX, VectorBY, VectorBZ);
            Bivector3D plane = CliffordEngine.Wedge(a, b).Normalize();
            Rotor3D rotor = Rotor3D.FromAngleAndPlane(RotorAngle, plane);

            double y = rect.Y + 44;

            // Rotor formula
            string rotorForm = $"Active Rotor: R = cos(θ/2) - B̂ sin(θ/2) = {rotor.Scalar:F3} - ({rotor.Bivector.E12:F2}e₁₂ + {rotor.Bivector.E23:F2}e₂₃ + {rotor.Bivector.E31:F2}e₃₁)";
            FormattedText ftRotor = new(
                rotorForm,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.SemiBold),
                12,
                _tokens.AccentAmberBrush
            );
            context.DrawText(ftRotor, new Point(rect.X + 16, y));

            y += 28;
            // Angle stats
            string angleStats = $"Rotation Angle θ: {RotorAngle * 180 / Math.PI:F1}° ({RotorAngle:F3} rad) | Rotor Norm: ||R|| = {rotor.Norm:F4} (Unitary, SO(3))";
            FormattedText ftAngle = new(
                angleStats,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif"),
                11,
                _tokens.TextSecondaryBrush
            );
            context.DrawText(ftAngle, new Point(rect.X + 16, y));

            y += 32;
            // Rigorous Quaternion isomorphism box
            Rect isoBox = new(rect.X + 14, y, rect.Width - 28, 80);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), isoBox);
            context.DrawRectangle(null, new Pen(_tokens.BorderBrush, 1), isoBox);

            string isoTitle = "ISOMORPHISM WITH QUATERNIONS (Even Subalgebra G₃⁺ ≅ ℍ):";
            FormattedText ftIsoTitle = new(
                isoTitle,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
                11,
                _tokens.AccentCyanBrush
            );
            context.DrawText(ftIsoTitle, new Point(isoBox.X + 10, isoBox.Y + 8));

            string isoDesc = "Quaternions q = w + x i + y j + z k are directly identified as Clifford Rotors:\n" +
                            "  i = -e₂₃,   j = -e₃₁,   k = -e₁₂\n" +
                            "Notice i² = j² = k² = i j k = -1 arises naturally from bivector multiplication without abstract postulate!";
            FormattedText ftIsoDesc = new(
                isoDesc,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif"),
                10,
                _tokens.TextSecondaryBrush
            );
            context.DrawText(ftIsoDesc, new Point(isoBox.X + 10, isoBox.Y + 26));
        }
    }

    private void DrawGradeRow(DrawingContext context, double x, double y, string title, string formula, string detail, IBrush accent)
    {
        FormattedText ftTitle = new(
            title,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
            11,
            accent
        );
        context.DrawText(ftTitle, new Point(x, y));

        FormattedText ftFormula = new(
            formula,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas, Courier New, monospace", FontStyle.Normal, FontWeight.SemiBold),
            11,
            _tokens.TextPrimaryBrush
        );
        context.DrawText(ftFormula, new Point(x + 220, y));

        FormattedText ftDetail = new(
            detail,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif"),
            10,
            _tokens.TextMutedBrush
        );
        context.DrawText(ftDetail, new Point(x, y + 16));
    }

    private void DrawArrow3D(DrawingContext context, Point from, Point to, IBrush brush, double thickness, string label)
    {
        var pen = new Pen(brush, thickness);
        context.DrawLine(pen, from, to);

        // Arrowhead
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len > 8)
        {
            double ux = dx / len;
            double uy = dy / len;
            Point p1 = new(to.X - 12 * ux + 6 * uy, to.Y - 12 * uy - 6 * ux);
            Point p2 = new(to.X - 12 * ux - 6 * uy, to.Y - 12 * uy + 6 * ux);

            var streamGeom = new StreamGeometry();
            using (var ctx = streamGeom.Open())
            {
                ctx.BeginFigure(to, isFilled: true);
                ctx.LineTo(p1);
                ctx.LineTo(p2);
                ctx.EndFigure(isClosed: true);
            }
            context.DrawGeometry(brush, null, streamGeom);
        }

        // Label
        if (!string.IsNullOrEmpty(label))
        {
            FormattedText ft = new(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
                11,
                brush
            );
            context.DrawText(ft, new Point(to.X + 8, to.Y - 8));
        }
    }

    private void DrawAxisLabel(DrawingContext context, Point pt, string label, IBrush brush)
    {
        FormattedText ft = new(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
            12,
            brush
        );
        context.DrawText(ft, new Point(pt.X + 6, pt.Y - 6));
    }

    private void DrawBadge(DrawingContext context, Point origin, string text, IBrush accent)
    {
        FormattedText ft = new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter, Segoe UI, sans-serif", FontStyle.Normal, FontWeight.Bold),
            11,
            accent
        );

        Rect badgeRect = new(origin.X, origin.Y, ft.Width + 16, ft.Height + 8);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(45, 0, 0, 0)), badgeRect);
        context.DrawRectangle(null, new Pen(accent, 1), badgeRect);
        context.DrawText(ft, new Point(origin.X + 8, origin.Y + 4));
    }
}
