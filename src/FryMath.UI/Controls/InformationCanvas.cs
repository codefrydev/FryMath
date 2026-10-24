using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FryMath.Core.Information;
using FryMath.UI.Theming;

namespace FryMath.UI.Controls;

public class InformationCanvas : Control
{
    public static readonly StyledProperty<string> PresetNameProperty =
        AvaloniaProperty.Register<InformationCanvas, string>(nameof(PresetName), "huffman");

    public static readonly StyledProperty<string> ModeNameProperty =
        AvaloniaProperty.Register<InformationCanvas, string>(nameof(ModeName), "huffman");

    public static readonly StyledProperty<string> SourceTextProperty =
        AvaloniaProperty.Register<InformationCanvas, string>(nameof(SourceText), "INFORMATION THEORY AND SHANNON ENTROPY");

    public static readonly StyledProperty<string> ModulationNameProperty =
        AvaloniaProperty.Register<InformationCanvas, string>(nameof(ModulationName), "qam16");

    public static readonly StyledProperty<double> SnrDbProperty =
        AvaloniaProperty.Register<InformationCanvas, double>(nameof(SnrDb), 14.0);

    public static readonly StyledProperty<double> CrossoverProbabilityProperty =
        AvaloniaProperty.Register<InformationCanvas, double>(nameof(CrossoverProbability), 0.15);

    public static readonly StyledProperty<bool> ShowTreeBranchesProperty =
        AvaloniaProperty.Register<InformationCanvas, bool>(nameof(ShowTreeBranches), true);

    public static readonly StyledProperty<bool> ShowProbabilityBarsProperty =
        AvaloniaProperty.Register<InformationCanvas, bool>(nameof(ShowProbabilityBars), true);

    public static readonly StyledProperty<bool> ShowConstellationCloudProperty =
        AvaloniaProperty.Register<InformationCanvas, bool>(nameof(ShowConstellationCloud), true);

    public string PresetName
    {
        get => GetValue(PresetNameProperty);
        set => SetValue(PresetNameProperty, value);
    }

    public string ModeName
    {
        get => GetValue(ModeNameProperty);
        set => SetValue(ModeNameProperty, value);
    }

    public string SourceText
    {
        get => GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    public string ModulationName
    {
        get => GetValue(ModulationNameProperty);
        set => SetValue(ModulationNameProperty, value);
    }

    public double SnrDb
    {
        get => GetValue(SnrDbProperty);
        set => SetValue(SnrDbProperty, value);
    }

    public double CrossoverProbability
    {
        get => GetValue(CrossoverProbabilityProperty);
        set => SetValue(CrossoverProbabilityProperty, value);
    }

    public bool ShowTreeBranches
    {
        get => GetValue(ShowTreeBranchesProperty);
        set => SetValue(ShowTreeBranchesProperty, value);
    }

    public bool ShowProbabilityBars
    {
        get => GetValue(ShowProbabilityBarsProperty);
        set => SetValue(ShowProbabilityBarsProperty, value);
    }

    public bool ShowConstellationCloud
    {
        get => GetValue(ShowConstellationCloudProperty);
        set => SetValue(ShowConstellationCloudProperty, value);
    }

    private readonly InformationTheoryEngine _engine = new();

    // Hover Probe
    private Point? _hoverPos;

    static InformationCanvas()
    {
        AffectsRender<InformationCanvas>(
            PresetNameProperty,
            ModeNameProperty,
            SourceTextProperty,
            ModulationNameProperty,
            SnrDbProperty,
            CrossoverProbabilityProperty,
            ShowTreeBranchesProperty,
            ShowProbabilityBarsProperty,
            ShowConstellationCloudProperty);
    }

    public InformationCanvas()
    {
        ClipToBounds = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PresetNameProperty)
        {
            string name = PresetName?.ToLowerInvariant() ?? "huffman";
            switch (name)
            {
                case "huffman" or "entropy" or "shannon":
                    _engine.LoadPreset("Shannon Entropy & Huffman Tree");
                    break;
                case "qam16" or "qam":
                    _engine.LoadPreset("16-QAM Constellation & AWGN Channel");
                    break;
                case "qpsk":
                    _engine.LoadPreset("QPSK Modulation & Bit Error Rate");
                    break;
                case "bsc" or "channel":
                    _engine.LoadPreset("Binary Symmetric Channel & Capacity");
                    break;
                case "kldiv" or "relative":
                    _engine.LoadPreset("KL Divergence Asymmetry");
                    break;
            }
        }
        else if (change.Property == ModeNameProperty)
        {
            string m = ModeName?.ToLowerInvariant() ?? "huffman";
            _engine.Mode = m switch
            {
                "constellation" or "capacity" or "qam" => InformationMode.ConstellationAndCapacity,
                "bsc" or "binary" => InformationMode.BinarySymmetricChannel,
                "kldiv" or "relative" => InformationMode.KlDivergence,
                _ => InformationMode.HuffmanAndEntropy
            };
        }
        else if (change.Property == SourceTextProperty)
        {
            if (!string.IsNullOrEmpty(SourceText))
            {
                _engine.SetSourceText(SourceText);
            }
        }
        else if (change.Property == ModulationNameProperty)
        {
            string mod = ModulationName?.ToLowerInvariant() ?? "qam16";
            var scheme = mod switch
            {
                "bpsk" => ModulationScheme.Bpsk,
                "qpsk" => ModulationScheme.Qpsk,
                "8psk" or "psk8" => ModulationScheme.Psk8,
                _ => ModulationScheme.Qam16
            };
            _engine.SetModulation(scheme);
        }
        else if (change.Property == SnrDbProperty)
        {
            _engine.SetSnrDb(SnrDb);
        }
        else if (change.Property == CrossoverProbabilityProperty)
        {
            _engine.SetCrossoverProbability(CrossoverProbability);
        }
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

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double w = Bounds.Width;
        double h = Bounds.Height;
        if (w <= 10 || h <= 10) return;

        // Dark Studio Background
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(13, 17, 23)), new Rect(0, 0, w, h));

        switch (_engine.Mode)
        {
            case InformationMode.HuffmanAndEntropy:
                RenderHuffmanAndEntropy(context, w, h);
                break;
            case InformationMode.ConstellationAndCapacity:
                RenderConstellationAndCapacity(context, w, h);
                break;
            case InformationMode.BinarySymmetricChannel:
                RenderBinarySymmetricChannel(context, w, h);
                break;
            case InformationMode.KlDivergence:
                RenderKlDivergence(context, w, h);
                break;
        }

        // Draw HUD Probe if mouse hovering
        if (_hoverPos.HasValue)
        {
            DrawHoverProbe(context, _hoverPos.Value, w, h);
        }
    }

    #region Render Mode 1: Huffman & Entropy

    private void RenderHuffmanAndEntropy(DrawingContext context, double w, double h)
    {
        // 1. Header Badge
        string badgeText = $"Shannon Entropy H(X) = {_engine.ShannonEntropy:F3} bits/char | Max H = {_engine.MaxEntropy:F3} bits | Redundancy = {_engine.Redundancy * 100:F1}% | Avg Code L = {_engine.AverageCodeLength:F3} bits | Compression = {_engine.CompressionRatio * 100:F1}%";
        DrawHeaderBadge(context, badgeText, w);

        double treeAreaHeight = h * 0.55;
        double barAreaHeight = h - treeAreaHeight - 35;

        // 2. Render Huffman Binary Tree
        if (ShowTreeBranches && _engine.HuffmanRoot != null)
        {
            double treeCenterX = w * 0.5;
            double treeTopY = 65;
            double treeWidth = w * 0.85;
            double treeHeight = treeAreaHeight - 95;

            RenderHuffmanTreeNodes(context, _engine.HuffmanRoot, treeCenterX, treeTopY, treeWidth, treeHeight);
        }

        // 3. Render Alphabet Probability & Information Spectrum
        if (ShowProbabilityBars && _engine.Alphabet.Count > 0)
        {
            double barY = h - barAreaHeight - 12;
            RenderProbabilityBars(context, w, barY, barAreaHeight);
        }
    }

    private void RenderHuffmanTreeNodes(DrawingContext context, HuffmanNode node, double cx, double topY, double treeW, double treeH)
    {
        double nodeX = cx + node.X * (treeW * 0.5);
        double nodeY = topY + node.Y * treeH;

        // Draw branches to children
        if (node.Left != null)
        {
            double childX = cx + node.Left.X * (treeW * 0.5);
            double childY = topY + node.Left.Y * treeH;

            // Rose branch for bit '0'
            var branchPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 83, 112)), 2.0);
            context.DrawLine(branchPen, new Point(nodeX, nodeY), new Point(childX, childY));

            // Bit '0' badge at midpoint
            double midX = (nodeX + childX) * 0.5;
            double midY = (nodeY + childY) * 0.5;
            DrawBitBadge(context, "0", midX, midY, Color.FromRgb(255, 83, 112));

            RenderHuffmanTreeNodes(context, node.Left, cx, topY, treeW, treeH);
        }

        if (node.Right != null)
        {
            double childX = cx + node.Right.X * (treeW * 0.5);
            double childY = topY + node.Right.Y * treeH;

            // Cyan branch for bit '1'
            var branchPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 77, 208, 225)), 2.0);
            context.DrawLine(branchPen, new Point(nodeX, nodeY), new Point(childX, childY));

            // Bit '1' badge at midpoint
            double midX = (nodeX + childX) * 0.5;
            double midY = (nodeY + childY) * 0.5;
            DrawBitBadge(context, "1", midX, midY, Color.FromRgb(77, 208, 225));

            RenderHuffmanTreeNodes(context, node.Right, cx, topY, treeW, treeH);
        }

        // Draw Node itself
        if (node.IsLeaf && node.Character.HasValue)
        {
            // Leaf card
            double radius = 18;
            var fillBrush = new SolidColorBrush(Color.FromRgb(22, 27, 34));
            var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 64)), 2.0);

            context.DrawEllipse(fillBrush, borderPen, new Point(nodeX, nodeY), radius, radius);

            char ch = node.Character.Value;
            string displayChar = ch == ' ' ? "␣" : ch.ToString();

            var glyphText = new FormattedText(
                displayChar,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
                14,
                new SolidColorBrush(Color.FromRgb(255, 255, 255))
            );
            context.DrawText(glyphText, new Point(nodeX - glyphText.Width * 0.5, nodeY - glyphText.Height * 0.5));

            // Below leaf: Code string badge
            var codeText = new FormattedText(
                node.Code,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
                10,
                new SolidColorBrush(Color.FromRgb(255, 215, 64))
            );
            context.DrawText(codeText, new Point(nodeX - codeText.Width * 0.5, nodeY + radius + 4));
        }
        else
        {
            // Internal node
            double radius = 10;
            var fillBrush = new SolidColorBrush(Color.FromRgb(33, 38, 45));
            var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 139, 148, 158)), 1.5);

            context.DrawEllipse(fillBrush, borderPen, new Point(nodeX, nodeY), radius, radius);

            var countText = new FormattedText(
                node.Count.ToString(),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.SemiBold),
                9,
                new SolidColorBrush(Color.FromArgb(200, 201, 209, 217))
            );
            context.DrawText(countText, new Point(nodeX - countText.Width * 0.5, nodeY - countText.Height * 0.5));
        }
    }

    private void DrawBitBadge(DrawingContext context, string bit, double cx, double cy, Color color)
    {
        double size = 14;
        var rect = new Rect(cx - size * 0.5, cy - size * 0.5, size, size);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(230, 13, 17, 23)), new Pen(new SolidColorBrush(color), 1.0), rect, 3, 3);

        var txt = new FormattedText(
            bit,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
            9,
            new SolidColorBrush(color)
        );
        context.DrawText(txt, new Point(cx - txt.Width * 0.5, cy - txt.Height * 0.5));
    }

    private void RenderProbabilityBars(DrawingContext context, double w, double topY, double totalH)
    {
        var alphabet = _engine.Alphabet;
        int count = Math.Min(alphabet.Count, 26);
        if (count == 0) return;

        double cardMargin = 20;
        double cardW = w - cardMargin * 2;
        var cardRect = new Rect(cardMargin, topY, cardW, totalH);

        // Glassmorphism background for frequency deck
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 22, 27, 34)), new Pen(new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)), 1.0), cardRect, 8, 8);

        // Section Title
        var titleText = new FormattedText(
            "EMPIRICAL ALPHABET DISTRIBUTION & SURPRISE (INFORMATION BITS: -log₂ pᵢ)",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
            10,
            new SolidColorBrush(Color.FromArgb(200, 139, 148, 158))
        );
        context.DrawText(titleText, new Point(cardMargin + 12, topY + 8));

        double availableW = cardW - 24;
        double slotW = availableW / count;
        double barMaxW = Math.Max(12, Math.Min(32, slotW - 6));
        double barMaxH = totalH - 55;
        double baselineY = topY + totalH - 24;

        double maxProb = alphabet.Max(a => a.Probability);
        if (maxProb <= 0) maxProb = 1.0;

        for (int i = 0; i < count; i++)
        {
            var sym = alphabet[i];
            double slotCenterX = cardMargin + 12 + i * slotW + slotW * 0.5;

            double ratio = sym.Probability / maxProb;
            double bh = ratio * barMaxH;
            double bx = slotCenterX - barMaxW * 0.5;
            double by = baselineY - bh;

            // Bar Gradient
            var grad = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 1.0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.5, 0.0, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(160, 77, 208, 225), 0.0),
                    new GradientStop(Color.FromArgb(240, 255, 215, 64), 1.0)
                }
            };
            context.DrawRectangle(grad, null, new Rect(bx, by, barMaxW, bh), 4, 4);

            // Character Label below
            char ch = sym.Character;
            string displayChar = ch == ' ' ? "␣" : ch.ToString();
            var charText = new FormattedText(
                displayChar,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
                11,
                new SolidColorBrush(Color.FromRgb(255, 255, 255))
            );
            context.DrawText(charText, new Point(slotCenterX - charText.Width * 0.5, baselineY + 5));

            // Information bits above bar
            var infoText = new FormattedText(
                $"{sym.InformationBits:F1}b",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.Medium),
                9,
                new SolidColorBrush(Color.FromArgb(200, 201, 209, 217))
            );
            context.DrawText(infoText, new Point(slotCenterX - infoText.Width * 0.5, Math.Max(topY + 22, by - 14)));
        }
    }

    #endregion

    #region Render Mode 2: Constellation & Capacity

    private void RenderConstellationAndCapacity(DrawingContext context, double w, double h)
    {
        // 1. Header Badge
        string schemeStr = _engine.CurrentModulation switch
        {
            ModulationScheme.Bpsk => "BPSK (1 bit/sym)",
            ModulationScheme.Qpsk => "QPSK (2 bits/sym)",
            ModulationScheme.Psk8 => "8-PSK (3 bits/sym)",
            _ => "16-QAM (4 bits/sym)"
        };
        string badgeText = $"{schemeStr} Constellation | SNR = {_engine.SnrDb:F1} dB | Capacity C = {_engine.ChannelCapacity:F0} bps | Theor BER = {_engine.TheoreticalBer:E2} | Empir BER = {_engine.EmpiricalBer:F4}";
        DrawHeaderBadge(context, badgeText, w);

        double splitX = w * 0.58;
        double topY = 60;
        double usableH = h - topY - 20;

        // Left Panel: IQ Plane Constellation Diagram
        double iqCenterX = splitX * 0.5;
        double iqCenterY = topY + usableH * 0.5;
        double iqRadius = Math.Min(splitX * 0.42, usableH * 0.42);

        RenderIqPlane(context, iqCenterX, iqCenterY, iqRadius);

        // Right Panel: Capacity C(SNR) & Theory Card
        double rightW = w - splitX - 25;
        RenderCapacityAndTheory(context, splitX + 10, topY, rightW, usableH);
    }

    private void RenderIqPlane(DrawingContext context, double cx, double cy, double radius)
    {
        // Background Grid circle & Crosshairs
        var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 48, 54, 61)), 1.5);
        context.DrawEllipse(null, borderPen, new Point(cx, cy), radius, radius);
        context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(50, 48, 54, 61)), 1.0), new Point(cx, cy), radius * 0.5, radius * 0.5);

        // Axes (I = horizontal, Q = vertical)
        var axisPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 139, 148, 158)), 1.5);
        context.DrawLine(axisPen, new Point(cx - radius * 1.15, cy), new Point(cx + radius * 1.15, cy));
        context.DrawLine(axisPen, new Point(cx, cy - radius * 1.15), new Point(cx, cy + radius * 1.15));

        // Axis Labels
        var iLabel = new FormattedText("In-Phase (I)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 11, new SolidColorBrush(Color.FromRgb(77, 208, 225)));
        context.DrawText(iLabel, new Point(cx + radius * 0.65, cy + 8));

        var qLabel = new FormattedText("Quadrature (Q)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 11, new SolidColorBrush(Color.FromRgb(255, 83, 112)));
        context.DrawText(qLabel, new Point(cx - qLabel.Width * 0.5, cy - radius * 1.25));

        // Decision Boundaries (for 16-QAM or QPSK)
        if (_engine.CurrentModulation == ModulationScheme.Qam16)
        {
            double scale = 1.0 / Math.Sqrt(10.0);
            double[] boundLevels = [-2.0 * scale, 0.0, 2.0 * scale];
            var boundPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 179, 136, 255)), 1.0, DashStyle.Dash);

            foreach (double lvl in boundLevels)
            {
                double sx = cx + lvl * radius;
                double sy = cy - lvl * radius;
                context.DrawLine(boundPen, new Point(sx, cy - radius), new Point(sx, cy + radius));
                context.DrawLine(boundPen, new Point(cx - radius, sy), new Point(cx + radius, sy));
            }
        }

        // Draw Received Noisy Symbols Cloud
        if (ShowConstellationCloud)
        {
            foreach (var rx in _engine.ReceivedSymbols)
            {
                double px = cx + rx.I * radius;
                double py = cy - rx.Q * radius;

                Color dotColor = rx.IsError
                    ? Color.FromArgb(200, 255, 82, 82)    // Bright Rose error
                    : Color.FromArgb(160, 77, 208, 225);  // Soft Cyan success

                context.DrawEllipse(new SolidColorBrush(dotColor), null, new Point(px, py), 2.5, 2.5);
            }
        }

        // Draw Ideal Constellation Centroids
        foreach (var pt in _engine.Constellation)
        {
            double px = cx + pt.I * radius;
            double py = cy - pt.Q * radius;

            // Halo
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(40, 255, 215, 64)), null, new Point(px, py), 12, 12);
            // Centroid
            context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 64)), new Pen(new SolidColorBrush(Color.FromRgb(13, 17, 23)), 2.0), new Point(px, py), 5.5, 5.5);

            // Bit pattern badge
            var bitText = new FormattedText(
                pt.BitPattern,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
                10,
                new SolidColorBrush(Color.FromRgb(255, 255, 255))
            );
            context.DrawText(bitText, new Point(px - bitText.Width * 0.5, py - 18));
        }
    }

    private void RenderCapacityAndTheory(DrawingContext context, double x, double y, double w, double h)
    {
        var cardRect = new Rect(x, y, w, h);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 22, 27, 34)), new Pen(new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)), 1.0), cardRect, 8, 8);

        // Section Title
        var titleText = new FormattedText(
            "SHANNON-HARTLEY CHANNEL CAPACITY & AWGN",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
            11,
            new SolidColorBrush(Color.FromRgb(255, 215, 64))
        );
        context.DrawText(titleText, new Point(x + 14, y + 14));

        // Capacity Curve C(SNR) Plot
        double plotX = x + 16;
        double plotY = y + 42;
        double plotW = w - 32;
        double plotH = h * 0.45;

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 13, 17, 23)), new Pen(new SolidColorBrush(Color.FromArgb(60, 48, 54, 61)), 1.0), new Rect(plotX, plotY, plotW, plotH), 4, 4);

        // Plot Shannon Limit Curve C = B * log2(1 + SNR) from SNR = -5 dB to +30 dB
        double minSnr = -5.0;
        double maxSnr = 30.0;
        double maxCap = _engine.BandwidthHz * Math.Log2(1.0 + Math.Pow(10.0, maxSnr / 10.0));

        var curveGeometry = new StreamGeometry();
        using (var sgc = curveGeometry.Open())
        {
            int steps = 60;
            for (int i = 0; i <= steps; i++)
            {
                double sDb = minSnr + (maxSnr - minSnr) * (double)i / steps;
                double cap = _engine.BandwidthHz * Math.Log2(1.0 + Math.Pow(10.0, sDb / 10.0));

                double px = plotX + ((sDb - minSnr) / (maxSnr - minSnr)) * plotW;
                double py = plotY + plotH - (cap / maxCap) * plotH;

                if (i == 0) sgc.BeginFigure(new Point(px, py), false);
                else sgc.LineTo(new Point(px, py));
            }
        }
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(77, 208, 225)), 2.5), curveGeometry);

        // Operating Point Cursor
        double opX = plotX + ((_engine.SnrDb - minSnr) / (maxSnr - minSnr)) * plotW;
        double opY = plotY + plotH - (_engine.ChannelCapacity / maxCap) * plotH;

        var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 215, 64)), 1.0, DashStyle.Dash);
        context.DrawLine(dashPen, new Point(opX, plotY + plotH), new Point(opX, opY));
        context.DrawLine(dashPen, new Point(plotX, opY), new Point(opX, opY));

        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 64)), new Pen(new SolidColorBrush(Color.FromRgb(13, 17, 23)), 2.0), new Point(opX, opY), 6.0, 6.0);

        // Curve Label
        var curveLabel = new FormattedText(
            $"Operating Point: {_engine.SnrDb:F1} dB → {_engine.ChannelCapacity:F0} bps",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Inter", FontStyle.Normal, FontWeight.Bold),
            10,
            new SolidColorBrush(Color.FromRgb(255, 215, 64))
        );
        context.DrawText(curveLabel, new Point(plotX + 8, plotY + 8));

        // Theoretical Equations Card below
        double mathY = plotY + plotH + 18;
        string[] formulas = [
            "• Shannon-Hartley Capacity:  C = B · log₂(1 + S/N)  [bits/sec]",
            "• Additive White Gaussian Noise:  rₖ = sₖ + wₖ,  w ~ N(0, σ²)",
            "• Noise Variance:  σ² = 1 / (2 · 10^(SNR_dB/10))",
            "• Minimum Euclidean Distance:  ŝ = argmin_i ‖rₖ - s_i‖²",
            $"• Bandwidth: B = {_engine.BandwidthHz:F0} Hz,  Spectral Efficiency: {_engine.ChannelCapacity / _engine.BandwidthHz:F2} b/s/Hz"
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

    #endregion

    #region Render Mode 3: Binary Symmetric Channel (BSC)

    private void RenderBinarySymmetricChannel(DrawingContext context, double w, double h)
    {
        string badgeText = $"Binary Symmetric Channel (BSC) | Crossover Probability p = {_engine.CrossoverProbability:F3} | Binary Entropy H_b(p) = {_engine.BinaryEntropy:F3} bits | Capacity C = {_engine.BscCapacity:F3} bits/use";
        DrawHeaderBadge(context, badgeText, w);

        double splitX = w * 0.5;
        double topY = 70;
        double usableH = h - topY - 25;

        // Left: BSC Transition Graph
        RenderBscTransitionGraph(context, 25, topY, splitX - 35, usableH);

        // Right: Binary Entropy Function Curve H_b(p)
        RenderBinaryEntropyPlot(context, splitX + 10, topY, splitX - 35, usableH);
    }

    private void RenderBscTransitionGraph(DrawingContext context, double x, double y, double w, double h)
    {
        var cardRect = new Rect(x, y, w, h);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 22, 27, 34)), new Pen(new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)), 1.0), cardRect, 8, 8);

        var title = new FormattedText("CHANNEL TRANSITION GRAPH", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 11, new SolidColorBrush(Color.FromRgb(255, 215, 64)));
        context.DrawText(title, new Point(x + 14, y + 14));

        double inX = x + w * 0.22;
        double outX = x + w * 0.78;
        double topNodeY = y + h * 0.35;
        double botNodeY = y + h * 0.65;
        double nodeRadius = 24;

        double p = _engine.CrossoverProbability;
        double q = 1.0 - p;

        // Transitions
        // 0 -> 0 (direct)
        var directPen = new Pen(new SolidColorBrush(Color.FromRgb(105, 240, 174)), 2.5);
        context.DrawLine(directPen, new Point(inX + nodeRadius, topNodeY), new Point(outX - nodeRadius, topNodeY));
        DrawTransitionBadge(context, $"1 - p = {q:F2}", (inX + outX) * 0.5, topNodeY - 14, Color.FromRgb(105, 240, 174));

        // 1 -> 1 (direct)
        context.DrawLine(directPen, new Point(inX + nodeRadius, botNodeY), new Point(outX - nodeRadius, botNodeY));
        DrawTransitionBadge(context, $"1 - p = {q:F2}", (inX + outX) * 0.5, botNodeY + 14, Color.FromRgb(105, 240, 174));

        // 0 -> 1 (cross)
        var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 83, 112)), 2.0, DashStyle.Dash);
        context.DrawLine(crossPen, new Point(inX + nodeRadius, topNodeY), new Point(outX - nodeRadius, botNodeY));
        DrawTransitionBadge(context, $"p = {p:F2}", (inX + outX) * 0.5 - 45, (topNodeY + botNodeY) * 0.5 - 18, Color.FromRgb(255, 83, 112));

        // 1 -> 0 (cross)
        context.DrawLine(crossPen, new Point(inX + nodeRadius, botNodeY), new Point(outX - nodeRadius, topNodeY));
        DrawTransitionBadge(context, $"p = {p:F2}", (inX + outX) * 0.5 + 45, (topNodeY + botNodeY) * 0.5 + 18, Color.FromRgb(255, 83, 112));

        // Draw Nodes: Input X = 0, 1
        DrawChannelNode(context, "X = 0", inX, topNodeY, nodeRadius, Color.FromRgb(77, 208, 225));
        DrawChannelNode(context, "X = 1", inX, botNodeY, nodeRadius, Color.FromRgb(77, 208, 225));

        // Draw Nodes: Output Y = 0, 1
        DrawChannelNode(context, "Y = 0", outX, topNodeY, nodeRadius, Color.FromRgb(255, 215, 64));
        DrawChannelNode(context, "Y = 1", outX, botNodeY, nodeRadius, Color.FromRgb(255, 215, 64));
    }

    private void DrawChannelNode(DrawingContext context, string label, double cx, double cy, double radius, Color color)
    {
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(13, 17, 23)), new Pen(new SolidColorBrush(color), 2.5), new Point(cx, cy), radius, radius);
        var txt = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 10, new SolidColorBrush(Color.FromRgb(255, 255, 255)));
        context.DrawText(txt, new Point(cx - txt.Width * 0.5, cy - txt.Height * 0.5));
    }

    private void DrawTransitionBadge(DrawingContext context, string text, double cx, double cy, Color color)
    {
        var txt = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 10, new SolidColorBrush(color));
        var rect = new Rect(cx - txt.Width * 0.5 - 6, cy - txt.Height * 0.5 - 3, txt.Width + 12, txt.Height + 6);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(230, 13, 17, 23)), new Pen(new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)), 1.0), rect, 4, 4);
        context.DrawText(txt, new Point(cx - txt.Width * 0.5, cy - txt.Height * 0.5));
    }

    private void RenderBinaryEntropyPlot(DrawingContext context, double x, double y, double w, double h)
    {
        var cardRect = new Rect(x, y, w, h);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 22, 27, 34)), new Pen(new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)), 1.0), cardRect, 8, 8);

        var title = new FormattedText("BINARY ENTROPY FUNCTION H_b(p) & CAPACITY C", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 11, new SolidColorBrush(Color.FromRgb(255, 215, 64)));
        context.DrawText(title, new Point(x + 14, y + 14));

        double plotX = x + 20;
        double plotY = y + 45;
        double plotW = w - 40;
        double plotH = h * 0.55;

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 13, 17, 23)), new Pen(new SolidColorBrush(Color.FromArgb(60, 48, 54, 61)), 1.0), new Rect(plotX, plotY, plotW, plotH), 4, 4);

        // Draw H_b(p) curve from p = 0 to 1
        var hbGeometry = new StreamGeometry();
        using (var sgc = hbGeometry.Open())
        {
            int steps = 100;
            for (int i = 0; i <= steps; i++)
            {
                double p = (double)i / steps;
                double hb = (p <= 0 || p >= 1) ? 0 : -(p * Math.Log2(p) + (1 - p) * Math.Log2(1 - p));

                double px = plotX + p * plotW;
                double py = plotY + plotH - hb * plotH;

                if (i == 0) sgc.BeginFigure(new Point(px, py), false);
                else sgc.LineTo(new Point(px, py));
            }
        }
        context.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(255, 83, 112)), 2.5), hbGeometry);

        // Operating Point Marker
        double curP = _engine.CrossoverProbability;
        double curHb = _engine.BinaryEntropy;
        double opX = plotX + curP * plotW;
        double opY = plotY + plotH - curHb * plotH;

        var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 215, 64)), 1.0, DashStyle.Dash);
        context.DrawLine(dashPen, new Point(opX, plotY + plotH), new Point(opX, opY));
        context.DrawLine(dashPen, new Point(plotX, opY), new Point(opX, opY));
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 64)), new Pen(new SolidColorBrush(Color.FromRgb(13, 17, 23)), 2.0), new Point(opX, opY), 6.0, 6.0);

        // Formula Card below
        double mathY = plotY + plotH + 20;
        string[] lines = [
            "• Binary Entropy:  H_b(p) = -p·log₂(p) - (1-p)·log₂(1-p)",
            "• Channel Capacity:  C = 1 - H_b(p)  [bits per channel use]",
            "• Zero Error (p = 0):  H_b = 0,  C = 1.0 bit/use (lossless)",
            "• Pure Noise (p = 0.5):  H_b = 1,  C = 0.0 bits/use (zero capacity!)"
        ];

        for (int i = 0; i < lines.Length; i++)
        {
            var lText = new FormattedText(lines[i], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 10, new SolidColorBrush(i == 0 ? Color.FromRgb(255, 83, 112) : Color.FromArgb(200, 201, 209, 217)));
            context.DrawText(lText, new Point(x + 20, mathY + i * 20));
        }
    }

    #endregion

    #region Render Mode 4: KL Divergence

    private void RenderKlDivergence(DrawingContext context, double w, double h)
    {
        string badgeText = $"Kullback-Leibler (KL) Divergence | D_KL(P || Q) = {_engine.KlDivergencePQ:F3} bits | D_KL(Q || P) = {_engine.KlDivergenceQP:F3} bits | Cross Entropy H(P, Q) = {_engine.CrossEntropyPQ:F3} bits";
        DrawHeaderBadge(context, badgeText, w);

        double topY = 70;
        double usableH = h - topY - 25;
        double cardMargin = 25;
        double cardW = w - cardMargin * 2;

        var cardRect = new Rect(cardMargin, topY, cardW, usableH);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(220, 22, 27, 34)), new Pen(new SolidColorBrush(Color.FromArgb(80, 48, 54, 61)), 1.0), cardRect, 8, 8);

        var title = new FormattedText("RELATIVE ENTROPY & DISTRIBUTION COMPARATOR (GIBBS' INEQUALITY: D_KL ≥ 0)", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 11, new SolidColorBrush(Color.FromRgb(255, 215, 64)));
        context.DrawText(title, new Point(cardMargin + 16, topY + 14));

        // Side-by-side grouped bar charts for P vs Q
        int numCategories = _engine.DistP.Length;
        double chartX = cardMargin + 25;
        double chartY = topY + 50;
        double chartW = cardW - 50;
        double chartH = usableH * 0.52;

        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 13, 17, 23)), new Pen(new SolidColorBrush(Color.FromArgb(60, 48, 54, 61)), 1.0), new Rect(chartX, chartY, chartW, chartH), 4, 4);

        double slotW = chartW / numCategories;
        double barW = Math.Min(36, slotW * 0.35);
        double baselineY = chartY + chartH - 24;

        for (int i = 0; i < numCategories; i++)
        {
            double slotCenterX = chartX + i * slotW + slotW * 0.5;

            double p = _engine.DistP[i];
            double q = _engine.DistQ[i];

            double ph = p * (chartH - 45);
            double qh = q * (chartH - 45);

            // Bar P (True, Cyan)
            double px = slotCenterX - barW - 2;
            double py = baselineY - ph;
            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(77, 208, 225)), null, new Rect(px, py, barW, ph), 3, 3);

            // Bar Q (Model, Amber)
            double qx = slotCenterX + 2;
            double qy = baselineY - qh;
            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 215, 64)), null, new Rect(qx, qy, barW, qh), 3, 3);

            // Category Label
            var catText = new FormattedText($"Category x_{i + 1}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 10, new SolidColorBrush(Color.FromRgb(255, 255, 255)));
            context.DrawText(catText, new Point(slotCenterX - catText.Width * 0.5, baselineY + 5));

            // Numbers above bars
            var pValText = new FormattedText($"{p:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.SemiBold), 9, new SolidColorBrush(Color.FromRgb(77, 208, 225)));
            context.DrawText(pValText, new Point(px + barW * 0.5 - pValText.Width * 0.5, py - 14));

            var qValText = new FormattedText($"{q:F2}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.SemiBold), 9, new SolidColorBrush(Color.FromRgb(255, 215, 64)));
            context.DrawText(qValText, new Point(qx + barW * 0.5 - qValText.Width * 0.5, qy - 14));
        }

        // Theoretical Explanation Card below
        double mathY = chartY + chartH + 20;
        string[] lines = [
            $"• Kullback-Leibler Divergence:  D_KL(P ‖ Q) = Σ p_i · log₂(p_i / q_i) = {_engine.KlDivergencePQ:F4} bits",
            $"• Reverse Divergence:  D_KL(Q ‖ P) = Σ q_i · log₂(q_i / p_i) = {_engine.KlDivergenceQP:F4} bits  (Asymmetric!)",
            $"• Cross-Entropy:  H(P, Q) = H(P) + D_KL(P ‖ Q) = {_engine.CrossEntropyPQ:F4} bits",
            "• Gibbs' Inequality:  D_KL(P ‖ Q) ≥ 0, with equality if and only if P ≡ Q almost everywhere."
        ];

        for (int i = 0; i < lines.Length; i++)
        {
            var lText = new FormattedText(lines[i], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 10, new SolidColorBrush(i == 0 ? Color.FromRgb(255, 215, 64) : Color.FromArgb(200, 201, 209, 217)));
            context.DrawText(lText, new Point(cardMargin + 25, mathY + i * 20));
        }
    }

    #endregion

    #region Helper Drawing Methods

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

        string line1 = $"Mode: {_engine.Mode}";
        string line2 = _engine.Mode switch
        {
            InformationMode.HuffmanAndEntropy => $"Alphabet Size: {_engine.Alphabet.Count} unique",
            InformationMode.ConstellationAndCapacity => $"Points: {_engine.Constellation.Count} states",
            InformationMode.BinarySymmetricChannel => $"Crossover: {_engine.CrossoverProbability:F3}",
            _ => $"Categories: {_engine.DistP.Length}"
        };
        string line3 = $"Screen Pos: ({mouse.X:F0}, {mouse.Y:F0})";

        var t1 = new FormattedText(line1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 9, new SolidColorBrush(Color.FromArgb(200, 201, 209, 217)));
        var t2 = new FormattedText(line2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 9, new SolidColorBrush(Color.FromRgb(77, 208, 225)));
        var t3 = new FormattedText(line3, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter", FontStyle.Normal, FontWeight.Normal), 9, new SolidColorBrush(Color.FromArgb(160, 139, 148, 158)));

        context.DrawText(t1, new Point(px + 10, py + 24));
        context.DrawText(t2, new Point(px + 10, py + 38));
        context.DrawText(t3, new Point(px + 10, py + 52));
    }

    #endregion
}
