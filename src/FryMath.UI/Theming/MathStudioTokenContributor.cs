using System;
using System.Collections.Generic;
using FryWorld.Sdk.Theming;
using FryWorld.Sdk.Theming.Palette;

namespace FryMath.UI.Theming;

public sealed class MathStudioTokenContributor : IThemeTokenContributor
{
    public string Id => "mathstudio";

    public void ContributeTokens(GeneratedPalette palette, IDictionary<string, string> tokens)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(tokens);

        bool dark = palette.Spec.IsDark;

        tokens["MathBg"] = dark ? "#0F0C1A" : "#FAFAFC";
        tokens["MathSurface"] = dark ? "#181427" : "#F0F0F6";
        tokens["MathSurfaceHigh"] = dark ? "#241F3A" : "#E4E4EE";
        tokens["MathSurfaceHover"] = dark ? "#30294D" : "#DCDCE8";
        tokens["MathBorder"] = dark ? "#3A3258" : "#CDCDE0";

        tokens["MathTextPrimary"] = dark ? "#F3F4F6" : "#140F26";
        tokens["MathTextSecondary"] = dark ? "#C4B5FD" : "#4C3282";
        tokens["MathTextMuted"] = dark ? "#9389BA" : "#78738C";

        tokens["MathAccentPurple"] = "#A855F7";
        tokens["MathAccentCyan"] = "#06B6D4";
        tokens["MathAccentEmerald"] = "#10B981";
        tokens["MathAccentAmber"] = "#F59E0B";
        tokens["MathAccentRose"] = "#F43F5E";
    }

    public void ContributeTokens(ThemeDefinition theme, IDictionary<string, string> tokens)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(tokens);

        bool dark = theme.IsDark;

        tokens["MathBg"] = dark ? "#0F0C1A" : "#FAFAFC";
        tokens["MathSurface"] = dark ? "#181427" : "#F0F0F6";
        tokens["MathSurfaceHigh"] = dark ? "#241F3A" : "#E4E4EE";
        tokens["MathSurfaceHover"] = dark ? "#30294D" : "#DCDCE8";
        tokens["MathBorder"] = dark ? "#3A3258" : "#CDCDE0";

        tokens["MathTextPrimary"] = dark ? "#F3F4F6" : "#140F26";
        tokens["MathTextSecondary"] = dark ? "#C4B5FD" : "#4C3282";
        tokens["MathTextMuted"] = dark ? "#9389BA" : "#78738C";

        tokens["MathAccentPurple"] = "#A855F7";
        tokens["MathAccentCyan"] = "#06B6D4";
        tokens["MathAccentEmerald"] = "#10B981";
        tokens["MathAccentAmber"] = "#F59E0B";
        tokens["MathAccentRose"] = "#F43F5E";
    }
}
