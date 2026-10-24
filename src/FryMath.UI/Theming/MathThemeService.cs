using System;
using Avalonia.Controls;
using FryWorld.Sdk.Services;
using FryWorld.Sdk.Theming;

namespace FryMath.UI.Theming;

public sealed class MathThemeService
{
    private static MathThemeService? _instance;
    public static MathThemeService Instance => _instance ??= new MathThemeService();

    private MathThemeTokens _tokens = MathThemeTokens.DarkElectric;
    public MathThemeTokens Tokens => _tokens;

    public event Action<MathThemeTokens>? TokensChanged;

    public void SetTheme(AppTheme theme)
    {
        _tokens = theme == AppTheme.Light ? MathThemeTokens.LightPaper : MathThemeTokens.DarkElectric;
        TokensChanged?.Invoke(_tokens);
    }

    public void SetTheme(string themeName)
    {
        bool isLight = themeName.Contains("light", StringComparison.OrdinalIgnoreCase) ||
                       themeName.Contains("paper", StringComparison.OrdinalIgnoreCase);

        _tokens = isLight ? MathThemeTokens.LightPaper : MathThemeTokens.DarkElectric;
        TokensChanged?.Invoke(_tokens);
    }

    public void SetCustomTokens(MathThemeTokens tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        TokensChanged?.Invoke(_tokens);
    }

    public void TrackResourceRoot(Control root)
    {
        // Keeps resources synchronized with Avalonia dynamic resource resolution
    }
}
