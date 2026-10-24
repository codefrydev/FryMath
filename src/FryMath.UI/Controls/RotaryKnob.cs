using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class RotaryKnob : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(Value), 1.0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(Minimum), -5.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RotaryKnob, double>(nameof(Maximum), 5.0);

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<RotaryKnob, string>(nameof(Label), "k");

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private bool _isDragging;
    private Point _lastPos;

    static RotaryKnob()
    {
        AffectsRender<RotaryKnob>(ValueProperty, MinimumProperty, MaximumProperty, LabelProperty);
    }

    public RotaryKnob()
    {
        Width = 52;
        Height = 64;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        _lastPos = e.GetPosition(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            var pos = e.GetPosition(this);
            double deltaY = _lastPos.Y - pos.Y;
            _lastPos = pos;

            double range = Maximum - Minimum;
            double step = range * 0.01;
            Value = Math.Clamp(Math.Round(Value + deltaY * step, 2), Minimum, Maximum);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var tokens = MathThemeService.Instance.Tokens;
        double w = Bounds.Width;
        double cx = w * 0.5;
        double cy = 24.0;
        double r = 18.0;

        // Outer track arc
        var trackPen = new Pen(new SolidColorBrush(tokens.BorderColor), 3.0);
        context.DrawEllipse(null, trackPen, new Point(cx, cy), r, r);

        // Active value arc angle (-135 deg to +135 deg)
        double norm = (Value - Minimum) / Math.Max(1e-5, Maximum - Minimum);
        norm = Math.Clamp(norm, 0.0, 1.0);
        double angleRad = (-135.0 + norm * 270.0) * (Math.PI / 180.0) - Math.PI * 0.5;

        // Pointer indicator line
        double indicatorX = cx + Math.Cos(angleRad) * (r - 3.0);
        double indicatorY = cy + Math.Sin(angleRad) * (r - 3.0);
        var pointerPen = new Pen(tokens.AccentPurpleBrush, 2.5);
        context.DrawLine(pointerPen, new Point(cx, cy), new Point(indicatorX, indicatorY));

        // Center cap
        context.DrawEllipse(tokens.SurfaceHighBrush, null, new Point(cx, cy), 7.0, 7.0);

        // Label and Value Text
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var labelText = new FormattedText(
            $"{Label}: {Value:F2}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            10,
            tokens.TextPrimaryBrush);

        context.DrawText(labelText, new Point(cx - labelText.Width * 0.5, 48.0));
    }
}
