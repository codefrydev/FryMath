using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Fourier;
using FryMath.Core.LinearAlgebra;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class EpicycleCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<EpicycleCanvas, string>(nameof(PresetName), "heart");

    public static readonly StyledProperty<int> HarmonicCountProperty =
        AvaloniaProperty.Register<EpicycleCanvas, int>(nameof(HarmonicCount), 15);

    public static readonly StyledProperty<bool> IsPlayingProperty =
        AvaloniaProperty.Register<EpicycleCanvas, bool>(nameof(IsPlaying), true);

    public static readonly StyledProperty<bool> ShowSpectrumProperty =
        AvaloniaProperty.Register<EpicycleCanvas, bool>(nameof(ShowSpectrum), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public int HarmonicCount
    {
        get => GetValue(HarmonicCountProperty);
        set => SetValue(HarmonicCountProperty, value);
    }

    public bool IsPlaying
    {
        get => GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public bool ShowSpectrum
    {
        get => GetValue(ShowSpectrumProperty);
        set => SetValue(ShowSpectrumProperty, value);
    }

    private readonly DispatcherTimer _timer;
    private double _time;
    private readonly List<Vector2D> _tracedPath = new();
    private readonly List<Vector2D> _drawingStroke = new();
    private bool _isDrawing;
    private List<Epicycle> _epicycles = new();
    private string? _loadedPreset;
    private int _loadedHarmonics;

    static EpicycleCanvas()
    {
        AffectsRender<EpicycleCanvas>(PresetNameProperty, HarmonicCountProperty, IsPlayingProperty, ShowSpectrumProperty);
    }

    public EpicycleCanvas()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            if (IsPlaying && !_isDrawing)
            {
                _time += 0.0035;
                if (_time > 1.0)
                {
                    _time = 0.0;
                }
                InvalidateVisual();
            }
        };
        _timer.Start();

        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pt = e.GetPosition(this);
        double cx = Bounds.Width * 0.5;
        double cy = Bounds.Height * 0.5;
        _isDrawing = true;
        _drawingStroke.Clear();
        _drawingStroke.Add(new Vector2D(pt.X - cx, pt.Y - cy));
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDrawing)
        {
            var pt = e.GetPosition(this);
            double cx = Bounds.Width * 0.5;
            double cy = Bounds.Height * 0.5;
            _drawingStroke.Add(new Vector2D(pt.X - cx, pt.Y - cy));
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDrawing)
        {
            _isDrawing = false;
            if (_drawingStroke.Count >= 5)
            {
                var resampled = EpicycleDecomposer.ResampleAndCenter(_drawingStroke, 256);
                _epicycles = EpicycleDecomposer.Decompose(resampled, HarmonicCount);
                _tracedPath.Clear();

                int sampleCount = 280;
                for (int i = 0; i < sampleCount; i++)
                {
                    double t = (double)i / sampleCount;
                    double px = 0.0;
                    double py = 0.0;
                    foreach (var epi in _epicycles)
                    {
                        double angle = epi.Frequency * 2.0 * Math.PI * t + epi.InitialPhase;
                        px += epi.Radius * Math.Cos(angle);
                        py += epi.Radius * Math.Sin(angle);
                    }
                    _tracedPath.Add(new Vector2D(px, py));
                }

                PresetName = "custom";
                _loadedPreset = "custom";
                _loadedHarmonics = HarmonicCount;
                _time = 0.0;
            }
            _drawingStroke.Clear();
            InvalidateVisual();
        }
    }

    private void EnsureDecomposed()
    {
        if (_loadedPreset != PresetName || _loadedHarmonics != HarmonicCount)
        {
            var rawPoints = EpicycleDecomposer.GeneratePresetPath(PresetName, 256);
            _epicycles = EpicycleDecomposer.Decompose(rawPoints, HarmonicCount);
            _tracedPath.Clear();

            // Pre-trace full period so the closed curve (heart, square, etc.) is immediately visible
            int sampleCount = 280;
            for (int i = 0; i < sampleCount; i++)
            {
                double t = (double)i / sampleCount;
                double px = 0.0;
                double py = 0.0;
                foreach (var epi in _epicycles)
                {
                    double angle = epi.Frequency * 2.0 * Math.PI * t + epi.InitialPhase;
                    px += epi.Radius * Math.Cos(angle);
                    py += epi.Radius * Math.Sin(angle);
                }
                _tracedPath.Add(new Vector2D(px, py));
            }

            _loadedPreset = PresetName;
            _loadedHarmonics = HarmonicCount;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        EnsureDecomposed();

        var tokens = MathThemeService.Instance.Tokens;
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        double centerX = w * 0.5;
        double centerY = h * 0.5;

        // 1. Draw Traced Fourier Waveform Path
        if (_tracedPath.Count > 1)
        {
            var pathGeom = new StreamGeometry();
            using (var ctx = pathGeom.Open())
            {
                ctx.BeginFigure(new Point(centerX + _tracedPath[0].X, centerY + _tracedPath[0].Y), false);
                for (int i = 1; i < _tracedPath.Count; i++)
                {
                    ctx.LineTo(new Point(centerX + _tracedPath[i].X, centerY + _tracedPath[i].Y));
                }
            }
            context.DrawGeometry(null, new Pen(tokens.AccentCyanBrush, 2.2), pathGeom);
        }

        // 2. Draw Epicycle Rotating Circles & Arm Vectors
        double currX = centerX;
        double currY = centerY;

        var circlePen = new Pen(new SolidColorBrush(Color.FromArgb(40, 168, 85, 247)), 1.0);
        var armPen = new Pen(tokens.AccentPurpleBrush, 1.2);

        foreach (var epi in _epicycles)
        {
            if (epi.Radius < 0.5) continue;

            context.DrawEllipse(null, circlePen, new Point(currX, currY), epi.Radius, epi.Radius);

            double angle = epi.Frequency * 2.0 * Math.PI * _time + epi.InitialPhase;
            double nextX = currX + epi.Radius * Math.Cos(angle);
            double nextY = currY + epi.Radius * Math.Sin(angle);

            context.DrawLine(armPen, new Point(currX, currY), new Point(nextX, nextY));

            currX = nextX;
            currY = nextY;
        }

        // Add tip point to traced curve
        var tipRel = new Vector2D(currX - centerX, currY - centerY);
        _tracedPath.Add(tipRel);
        if (_tracedPath.Count > 500) _tracedPath.RemoveAt(0);

        // Tip glowing dot
        context.DrawEllipse(tokens.AccentEmeraldBrush, null, new Point(currX, currY), 4.5, 4.5);
        context.DrawEllipse(null, new Pen(Brushes.White, 1.5), new Point(currX, currY), 6.5, 6.5);

        // Live User Drawing Stroke
        if (_isDrawing && _drawingStroke.Count > 1)
        {
            var strokeGeom = new StreamGeometry();
            using (var ctx = strokeGeom.Open())
            {
                ctx.BeginFigure(new Point(centerX + _drawingStroke[0].X, centerY + _drawingStroke[0].Y), false);
                for (int i = 1; i < _drawingStroke.Count; i++)
                {
                    ctx.LineTo(new Point(centerX + _drawingStroke[i].X, centerY + _drawingStroke[i].Y));
                }
            }
            context.DrawGeometry(null, new Pen(tokens.AccentAmberBrush, 3.2), strokeGeom);
        }

        // 3. Header HUD
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        string hudMsg = _isDrawing
            ? "✏️ FREEHAND DRAWING MODE: Draw any loop or shape, release to decompose into epicycles!"
            : $"Fourier Epicycles: {PresetName.ToUpperInvariant()}  |  Harmonics: {HarmonicCount}\n" +
              $"f(t) = Σ cₖ · e^(i 2π k t)  |  Phase t = {_time:F3}  (Click & drag anytime to draw custom shape)";

        var headerText = new FormattedText(
            hudMsg,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            11,
            _isDrawing ? tokens.AccentAmberBrush : tokens.TextPrimaryBrush);

        context.DrawText(headerText, new Point(16.0, 16.0));

        // 4. Spectrum Analyzer (Bottom-Right corner)
        if (ShowSpectrum && _epicycles.Count > 0)
        {
            RenderSpectrumAnalyzer(context, w, h, tokens);
        }
    }

    private void RenderSpectrumAnalyzer(DrawingContext context, double w, double h, MathThemeTokens tokens)
    {
        double cardW = 280.0;
        double cardH = 110.0;
        double cardX = w - cardW - 16.0;
        double cardY = h - cardH - 16.0;

        var cardRect = new Rect(cardX, cardY, cardW, cardH);
        context.FillRectangle(tokens.SurfaceHighBrush, cardRect, 6f);
        context.DrawRectangle(null, new Pen(tokens.BorderBrush, 1.0), cardRect, 6f, 6f);

        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
        var title = new FormattedText("FOURIER SPECTRUM |cₖ|", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, tokens.AccentCyanBrush);
        context.DrawText(title, new Point(cardX + 10.0, cardY + 8.0));

        // Draw frequency spectrum bars
        int maxBars = Math.Min(_epicycles.Count, 16);
        double barAreaW = cardW - 24.0;
        double barW = barAreaW / Math.Max(maxBars, 1) - 3.0;
        double maxRadius = 1.0;
        foreach (var ep in _epicycles) maxRadius = Math.Max(maxRadius, ep.Radius);

        double barBaseY = cardY + cardH - 18.0;

        for (int k = 0; k < maxBars; k++)
        {
            var ep = _epicycles[k];
            double normHeight = Math.Clamp((ep.Radius / maxRadius) * 60.0, 2.0, 60.0);
            double bx = cardX + 12.0 + k * (barW + 3.0);
            double by = barBaseY - normHeight;

            var barRect = new Rect(bx, by, barW, normHeight);
            context.FillRectangle(tokens.AccentPurpleBrush, barRect, 2f);

            if (k % 4 == 0)
            {
                var kText = new FormattedText($"{k}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 8, tokens.TextMutedBrush);
                context.DrawText(kText, new Point(bx, barBaseY + 2.0));
            }
        }
    }
}
