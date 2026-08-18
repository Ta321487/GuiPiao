using GuiPiao.Mobile.Model;
using GuiPiao.Mobile.Resources.Themes;

namespace GuiPiao.Mobile.Services;

/// <summary>
///     对齐 PC ThemeManager：先套结构色字典，再覆盖强调色 token。
///     MAUI 禁止代码设置 ResourceDictionary.Source，故用带 x:Class 的主题字典实例。
/// </summary>
public sealed class ThemeService
{
    private ResourceDictionary? _currentTheme;

    public bool IsDarkTheme { get; private set; }

    public event EventHandler? ThemeChanged;

    public void Apply(AppearanceConfig config)
    {
        ApplyThemeMode(config.ThemeMode);
        ApplyAccentColor(config.AccentColor, config.CustomColor);
        ApplyFontSize(config.FontSize);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>对齐 PC 字号档：小12 / 中14 / 大16 / 特大18（Base），并按比例推演相关 token。</summary>
    public void ApplyFontSize(FontSizeOption fontSize)
    {
        var baseSize = fontSize switch
        {
            FontSizeOption.Small => 12.0,
            FontSizeOption.Large => 16.0,
            FontSizeOption.ExtraLarge => 18.0,
            _ => 14.0
        };
        SetDouble("BaseFontSize", baseSize);
        SetDouble("SmallFontSize", Math.Max(10, baseSize - 2));
        SetDouble("LargeFontSize", baseSize + 2);
        SetDouble("TitleFontSize", baseSize + 4);
    }

    public void ApplyThemeMode(ThemeMode themeMode)
    {
        var nextDark = themeMode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            ThemeMode.System => Application.Current?.RequestedTheme == AppTheme.Dark,
            _ => false
        };

        var app = Application.Current;
        if (app == null) return;

        // 仅在深浅切换时替换主题字典；切强调色不要 Clear MergedDictionaries（会卡且 Style 丢绑定）
        if (_currentTheme == null || IsDarkTheme != nextDark)
        {
            IsDarkTheme = nextDark;
            var merged = app.Resources.MergedDictionaries;
            if (_currentTheme != null)
                merged.Remove(_currentTheme);

            _currentTheme = IsDarkTheme ? new DarkTheme() : new LightTheme();
            // 只替换主题字典，不清空 Tokens/Controls（避免卡顿与 Style DynamicResource 失效）
            merged.Add(_currentTheme);
        }

        app.UserAppTheme = IsDarkTheme ? AppTheme.Dark : AppTheme.Light;
    }

    public void ApplyAccentColor(AccentColor accentColor, string customColor) =>
        ApplyAccentHex(ResolveAccentHex(accentColor, customColor));

    public void ApplyAccentHex(string colorHex)
    {
        if (!TryParseColor(colorHex, out var color))
            color = Color.FromArgb("#0078D4");

        var hover = AdjustBrightness(color, IsDarkTheme ? 0.12 : 0.1);
        var pressed = AdjustBrightness(color, IsDarkTheme ? -0.05 : -0.12);
        var foreground = GetAccentForegroundColor(color, IsDarkTheme);
        var text = GetContrastTextColor(color);

        SetColor("AccentColor", color);
        SetColor("AccentForeground", foreground);
        SetColor("AccentHover", hover);
        SetColor("AccentPressed", pressed);
        SetColor("AccentText", text);

        SetBrush("AccentBrush", color);
        SetBrush("PrimaryBrush", color);
        SetBrush("AccentForegroundBrush", foreground);
        SetBrush("AccentHoverBrush", hover);
        SetBrush("AccentPressedBrush", pressed);
        SetBrush("AccentTextBrush", text);
    }

    public static string ResolveAccentHex(AccentColor accentColor, string customColor) =>
        accentColor switch
        {
            AccentColor.MicrosoftBlue => "#0078D4",
            AccentColor.FreshGreen => "#28A745",
            AccentColor.VitalityOrange => "#FD7E14",
            AccentColor.DarkPurple => "#6F42C1",
            AccentColor.MinimalGray => "#6C757D",
            AccentColor.Custom => string.IsNullOrWhiteSpace(customColor) ? "#0078D4" : customColor.Trim(),
            _ => "#0078D4"
        };

    private void SetColor(string key, Color color)
    {
        // 同时写主题字典与 Application.Resources，避免新开页（如表单）Style 仍解析到默认蓝
        if (_currentTheme != null)
            _currentTheme[key] = color;

        var app = Application.Current;
        if (app != null)
            app.Resources[key] = color;
    }

    private static void SetDouble(string key, double value)
    {
        var app = Application.Current;
        if (app == null) return;
        app.Resources[key] = value;
    }

    private void SetBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        if (_currentTheme != null)
            _currentTheme[key] = brush;

        var app = Application.Current;
        if (app != null)
            app.Resources[key] = brush;
    }

    private static bool TryParseColor(string hex, out Color color)
    {
        try
        {
            color = Color.FromArgb(hex);
            return true;
        }
        catch
        {
            color = Colors.Transparent;
            return false;
        }
    }

    private static Color GetAccentForegroundColor(Color accent, bool isDarkTheme)
    {
        var background = isDarkTheme
            ? Color.FromRgb(0x1E / 255.0, 0x1E / 255.0, 0x1E / 255.0)
            : Colors.White;

        var candidate = accent;
        var step = isDarkTheme ? 0.08 : -0.08;
        for (var i = 0; i < 24; i++)
        {
            if (GetContrastRatio(candidate, background) >= 4.5)
                return candidate;
            candidate = AdjustBrightness(candidate, step);
        }

        return isDarkTheme ? Color.FromArgb("#60CDFF") : Color.FromArgb("#005A9E");
    }

    private static Color GetContrastTextColor(Color backgroundColor)
    {
        var brightness = (backgroundColor.Red * 255 * 299
                          + backgroundColor.Green * 255 * 587
                          + backgroundColor.Blue * 255 * 114) / 1000.0;
        return brightness > 128 ? Colors.Black : Colors.White;
    }

    private static double GetContrastRatio(Color a, Color b)
    {
        var l1 = GetRelativeLuminance(a);
        var l2 = GetRelativeLuminance(b);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double GetRelativeLuminance(Color color)
    {
        static double Channel(double s) =>
            s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);

        return 0.2126 * Channel(color.Red) + 0.7152 * Channel(color.Green) + 0.0722 * Channel(color.Blue);
    }

    private static Color AdjustBrightness(Color color, double factor)
    {
        var r = Math.Clamp(color.Red + factor, 0, 1);
        var g = Math.Clamp(color.Green + factor, 0, 1);
        var b = Math.Clamp(color.Blue + factor, 0, 1);
        return Color.FromRgb(r, g, b);
    }
}
