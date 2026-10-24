using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using FryMath.Core.Probability;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class GaltonBoardCanvas : Control
{
    public static readonly StyledProperty<bool> IsContinuousDroppingProperty =
        AvaloniaProperty.Register<GaltonBoardCanvas, bool>(nameof(IsContinuousDropping), true);

    public static readonly StyledProperty<double> ProbabilityPProperty =
        AvaloniaProperty.Register<GaltonBoardCanvas, double>(nameof(ProbabilityP), 0.5);

    public bool IsContinuousDropping
    {
        get => GetValue(IsContinuousDroppingProperty);
        set => SetValue(IsContinuousDroppingProperty, value);
    }

    public double ProbabilityP
    {
        get => GetValue(ProbabilityPProperty);
        set => SetValue(ProbabilityPProperty, value);
    }

    private readonly GaltonBoardSimulation _simulation = new(rows: 12);
    private readonly DispatcherTimer _timer;
    private int _dropCounter = 0;

    static GaltonBoardCanvas()
    {
        AffectsRender<GaltonBoardCanvas>(IsContinuousDroppingProperty, ProbabilityPProperty);
    }

    public GaltonBoardCanvas()
    {
        ClipToBounds = true;

        // Pre-settle balls on startup so the Central Limit Theorem Gaussian curve is immediately apparent
        Prewarm(80);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (s, e) =>
        {
            _simulation.SuccessProbabilityP = ProbabilityP;

            if (IsContinuousDropping)
            {
                _dropCounter++;
                if (_dropCounter % 3 == 0)
                {
                    _simulation.SpawnBall();
                }
            }

            _simulation.Step(0.016);
            InvalidateVisual();
        };
        _timer.Start();

        MathThemeService.Instance.TokensChanged += _ => InvalidateVisual();
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _simulation.SpawnBall();
        }
        for (int i = 0; i < 400; i++)
        {
            _simulation.Step(0.02);
        }
    }

    public void DropBurst(int count = 30)
    {
        for (int i = 0; i < count; i++)
        {
            _simulation.SpawnBall();
        }
    }

    public void ResetSimulation()
    {
        _simulation.Reset();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w < 40 || h < 40) return;

        var tokens = MathThemeService.Instance.Tokens;
        context.FillRectangle(tokens.BgBrush, new Rect(0, 0, w, h));

        double centerX = w * 0.5;
        double pegSpacingX = 26.0;

        // 1. Draw Pegs Grid
        var pegBrush = tokens.TextMutedBrush;
        foreach (var peg in _simulation.Pegs)
        {
            context.DrawEllipse(pegBrush, null, new Point(centerX + peg.X, peg.Y), 2.5, 2.5);
        }

        // 2. Draw Falling Bouncing Balls
        var ballFill = tokens.AccentCyanBrush;
        var ballStroke = new Pen(Brushes.White, 1.0);

        foreach (var b in _simulation.Balls)
        {
            if (!b.IsSettled)
            {
                context.DrawEllipse(ballFill, ballStroke, new Point(centerX + b.Position.X, b.Position.Y), 3.5, 3.5);
            }
        }

        // 3. Draw Bins & Settled Balls Histogram
        int binCount = _simulation.BinCount;
        double binAreaW = binCount * pegSpacingX;
        double binStartX = centerX - binAreaW * 0.5;
        double binBottomY = h - 50.0;
        double binHeight = 160.0;

        var binPen = new Pen(tokens.BorderBrush, 1.0);
        context.DrawLine(binPen, new Point(binStartX - 20, binBottomY), new Point(binStartX + binAreaW + 20, binBottomY));

        int maxBinCount = 1;
        for (int i = 0; i < binCount; i++)
        {
            maxBinCount = Math.Max(maxBinCount, _simulation.BinCounts[i]);
        }

        var colFill = new SolidColorBrush(Color.FromArgb(140, 124, 77, 255));
        var colBorder = new Pen(tokens.AccentPurpleBrush, 1.2);

        for (int i = 0; i < binCount; i++)
        {
            double bx = binStartX + i * pegSpacingX;
            context.DrawLine(binPen, new Point(bx, binBottomY - binHeight), new Point(bx, binBottomY));

            int count = _simulation.BinCounts[i];
            if (count > 0)
            {
                double barH = Math.Min((double)count / Math.Max(maxBinCount, 8) * (binHeight - 20.0), binHeight);
                var barRect = new Rect(bx + 2.0, binBottomY - barH, pegSpacingX - 4.0, barH);
                context.FillRectangle(colFill, barRect, 3f);
                context.DrawRectangle(null, colBorder, barRect, 3f, 3f);
            }
        }
        context.DrawLine(binPen, new Point(binStartX + binAreaW, binBottomY - binHeight), new Point(binStartX + binAreaW, binBottomY));

        // 4. Theoretical Gaussian Bell Curve Overlay
        if (_simulation.TotalSettledCount > 10)
        {
            var curveGeom = new StreamGeometry();
            using (var ctx = curveGeom.Open())
            {
                double mean = _simulation.Rows * ProbabilityP;
                double variance = _simulation.Rows * ProbabilityP * (1.0 - ProbabilityP);
                double sigma = Math.Sqrt(Math.Max(variance, 0.1));

                bool started = false;
                for (double binX = 0; binX <= binCount - 1; binX += 0.2)
                {
                    double prob = (1.0 / (sigma * Math.Sqrt(2.0 * Math.PI))) *
                                  Math.Exp(-0.5 * Math.Pow((binX - mean) / sigma, 2));

                    double expectedCount = prob * _simulation.TotalSettledCount;
                    double curveY = binBottomY - (expectedCount / Math.Max(maxBinCount, 8) * (binHeight - 20.0));
                    double screenX = binStartX + binX * pegSpacingX + pegSpacingX * 0.5;

                    if (!started)
                    {
                        ctx.BeginFigure(new Point(screenX, curveY), false);
                        started = true;
                    }
                    else
                    {
                        ctx.LineTo(new Point(screenX, curveY));
                    }
                }
            }

            var curvePen = new Pen(tokens.AccentAmberBrush, 2.2);
            context.DrawGeometry(null, curvePen, curveGeom);
        }

        // 5. Statistics Header HUD
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var statsText = new FormattedText(
            $"Galton Board Simulation (Plinko)  |  Settled Balls: {_simulation.TotalSettledCount}  |  p = {ProbabilityP:F2}\n" +
            "Demonstrating the Central Limit Theorem: Independent random bounces naturally converge to Gaussian Normal distribution.",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            11,
            tokens.TextPrimaryBrush);

        context.DrawText(statsText, new Point(16.0, 16.0));
    }
}
