using System;
using Avalonia.Media;

namespace FryMath.UI.Theming;

public sealed record MathThemeTokens
{
    public bool IsDark { get; init; } = true;

    // Studio Backgrounds (Refined Deep Slate Workspace)
    public Color BgColor { get; init; } = Color.FromRgb(13, 14, 18); // #0D0E12
    public Color SurfaceColor { get; init; } = Color.FromRgb(19, 21, 28); // #13151C
    public Color SurfaceHighColor { get; init; } = Color.FromRgb(26, 29, 39); // #1A1D27
    public Color SurfaceHoverColor { get; init; } = Color.FromRgb(37, 41, 55); // #252937
    public Color BorderColor { get; init; } = Color.FromRgb(40, 44, 59); // #282C3B

    // Text (Clean Monotone Light Grey / Soft White)
    public Color TextPrimaryColor { get; init; } = Color.FromRgb(248, 250, 252);
    public Color TextSecondaryColor { get; init; } = Color.FromRgb(203, 213, 225); // #CBD5E1
    public Color TextMutedColor { get; init; } = Color.FromRgb(148, 163, 184); // #94A3B8

    // Accents for Mathematical Curves & Graphs (D3 / Observable Palette)
    public Color AccentPurple { get; init; } = Color.FromRgb(167, 139, 250); // Soft lavender #A78BFA
    public Color AccentCyan { get; init; } = Color.FromRgb(56, 189, 248);   // Sky blue #38BDF8
    public Color AccentEmerald { get; init; } = Color.FromRgb(52, 211, 153); // Sage mint #34D399
    public Color AccentAmber { get; init; } = Color.FromRgb(251, 191, 36);  // Warm gold #FBBF24
    public Color AccentRose { get; init; } = Color.FromRgb(251, 113, 133);  // Soft rose #FB7185

    // Canvas Grid & Coordinate Axes (Neutral Monotone)
    public Color GridLineMajorColor { get; init; } = Color.FromArgb(32, 255, 255, 255);
    public Color GridLineMinorColor { get; init; } = Color.FromArgb(14, 255, 255, 255);
    public Color GridAxisColor { get; init; } = Color.FromArgb(110, 209, 213, 219);

    // Functions & Calculus Curves
    public Color CurveColor { get; init; } = Color.FromRgb(56, 189, 248); // Sky blue
    public Color TangentLineColor { get; init; } = Color.FromRgb(251, 191, 36); // Warm gold
    public Color SecantLineColor { get; init; } = Color.FromArgb(160, 167, 139, 250);
    public Color OsculatingCircleColor { get; init; } = Color.FromArgb(180, 251, 113, 133);
    public Color RiemannFillColor { get; init; } = Color.FromArgb(65, 56, 189, 248);
    public Color RiemannBorderColor { get; init; } = Color.FromArgb(200, 56, 189, 248);

    // Linear Algebra Basis Vectors
    public Color BasisVectorIColor { get; init; } = Color.FromRgb(56, 189, 248); // i-hat (sky blue)
    public Color BasisVectorJColor { get; init; } = Color.FromRgb(251, 113, 133); // j-hat (soft rose)
    public Color DetPositiveColor { get; init; } = Color.FromArgb(75, 56, 189, 248);
    public Color DetNegativeColor { get; init; } = Color.FromArgb(75, 251, 191, 36);

    // Brushes for direct Avalonia canvas rendering
    public IBrush BgBrush => new SolidColorBrush(BgColor);
    public IBrush SurfaceBrush => new SolidColorBrush(SurfaceColor);
    public IBrush SurfaceHighBrush => new SolidColorBrush(SurfaceHighColor);
    public IBrush BorderBrush => new SolidColorBrush(BorderColor);
    public IBrush TextPrimaryBrush => new SolidColorBrush(TextPrimaryColor);
    public IBrush TextSecondaryBrush => new SolidColorBrush(TextSecondaryColor);
    public IBrush TextMutedBrush => new SolidColorBrush(TextMutedColor);
    public IBrush AccentPurpleBrush => new SolidColorBrush(AccentPurple);
    public IBrush AccentCyanBrush => new SolidColorBrush(AccentCyan);
    public IBrush AccentEmeraldBrush => new SolidColorBrush(AccentEmerald);
    public IBrush AccentAmberBrush => new SolidColorBrush(AccentAmber);
    public IBrush AccentRoseBrush => new SolidColorBrush(AccentRose);

    public IPen GridMinorPen => new Pen(new SolidColorBrush(GridLineMinorColor), 1.0);
    public IPen GridMajorPen => new Pen(new SolidColorBrush(GridLineMajorColor), 1.0);
    public IPen GridAxisPen => new Pen(new SolidColorBrush(GridAxisColor), 1.5);
    public IPen CurvePen => new Pen(new SolidColorBrush(CurveColor), 2.5);
    public IPen TangentPen => new Pen(new SolidColorBrush(TangentLineColor), 1.8);
    public IPen OsculatingPen => new Pen(new SolidColorBrush(OsculatingCircleColor), 1.5);

    public static MathThemeTokens DarkElectric => new();

    public static MathThemeTokens LightPaper => new()
    {
        IsDark = false,
        BgColor = Color.FromRgb(250, 250, 252),
        SurfaceColor = Color.FromRgb(240, 240, 246),
        SurfaceHighColor = Color.FromRgb(228, 228, 238),
        SurfaceHoverColor = Color.FromRgb(220, 220, 232),
        BorderColor = Color.FromRgb(205, 205, 220),
        TextPrimaryColor = Color.FromRgb(20, 15, 38),
        TextSecondaryColor = Color.FromRgb(76, 50, 130),
        TextMutedColor = Color.FromRgb(120, 115, 140),
        GridLineMajorColor = Color.FromArgb(50, 120, 80, 200),
        GridLineMinorColor = Color.FromArgb(25, 120, 115, 140),
        GridAxisColor = Color.FromArgb(190, 40, 30, 70),
        CurveColor = Color.FromRgb(14, 116, 144),
        TangentLineColor = Color.FromRgb(217, 119, 6),
        RiemannFillColor = Color.FromArgb(70, 124, 58, 237),
        RiemannBorderColor = Color.FromArgb(190, 124, 58, 237),
        DetPositiveColor = Color.FromArgb(70, 14, 116, 144),
        DetNegativeColor = Color.FromArgb(70, 217, 119, 6)
    };
}
