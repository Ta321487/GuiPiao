using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GuiPiao.Model;
using GuiPiao.View;
using LiveChartsCore;
using Microsoft.Win32;

namespace GuiPiao.Services;

/// <summary>
///     主题管理器 - 管理应用程序的外观主题
/// </summary>
public static class ThemeManager
{
    // 当前主题字典
    private static ResourceDictionary? _currentThemeDictionary;

    // 当前DPI缩放比例（用于与字体大小叠加）
    private static double _currentDpiScale = 1.0;

    // SolidColorBrush 缓存
    private static readonly Dictionary<string, SolidColorBrush> _brushCache = new();
    private static readonly Dictionary<Color, SolidColorBrush> _colorToBrushCache = new();

    /// <summary>
    ///     当前是否为深色主题
    /// </summary>
    public static bool IsDarkTheme { get; private set; }

    /// <summary>
    ///     主题已更改事件
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    ///     获取或创建 SolidColorBrush（带缓存）
    /// </summary>
    private static SolidColorBrush GetOrCreateBrush(Color color)
    {
        if (!_colorToBrushCache.TryGetValue(color, out var brush))
        {
            brush = new SolidColorBrush(color);
            brush.Freeze();
            _colorToBrushCache[color] = brush;
        }

        return brush;
    }

    /// <summary>
    ///     获取或创建 SolidColorBrush（从十六进制颜色字符串）
    /// </summary>
    private static SolidColorBrush GetOrCreateBrush(string colorHex)
    {
        if (!_brushCache.TryGetValue(colorHex, out var brush))
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            brush = GetOrCreateBrush(color);
            _brushCache[colorHex] = brush;
        }

        return brush;
    }

    /// <summary>
    ///     应用主题设置
    /// </summary>
    public static void ApplyTheme(GeneralConfig config)
    {
        // 确保DPI缩放已加载（从UI设置中读取）
        if (_currentDpiScale == 1.0)
        {
            var uiConfig = new UISettingsService().Config;
            _currentDpiScale = uiConfig.DpiScaling switch
            {
                "100" => 1.0,
                "125" => 1.25,
                "150" => 1.5,
                _ => 1.0
            };
        }

        // 应用主题模式（浅色/深色/跟随系统）
        ApplyThemeMode(config.ThemeMode);

        // 应用字体大小（考虑DPI缩放）
        ApplyFontSize(config.FontSize);

        // 应用行高
        ApplyRowHeight(config.RowHeight);

        // 应用强调色（必须在主题模式之后，确保覆盖主题文件中的默认值）
        ApplyAccentColor(config.AccentColor, config.CustomColor);

        // 触发主题已更改事件
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    ///     应用DPI缩放设置
    /// </summary>
    public static void ApplyDpiScaling(string dpiScaling)
    {
        _currentDpiScale = dpiScaling switch
        {
            "100" => 1.0,
            "125" => 1.25,
            "150" => 1.5,
            _ => 1.0 // System或其他值默认使用100%
        };

        // 应用DPI缩放到应用程序资源
        Application.Current.Resources["DpiScale"] = _currentDpiScale;
        Application.Current.Resources["DpiScaleTransform"] = new ScaleTransform(_currentDpiScale, _currentDpiScale);

        // 重新应用字体大小（考虑DPI缩放）
        var generalConfig = new GeneralSettingsService().Config;
        ApplyFontSize(generalConfig.FontSize);

        // 触发主题已更改事件，通知图表等组件刷新
        ThemeChanged?.Invoke(null, EventArgs.Empty);

        // 刷新所有窗口以应用新的DPI设置
        RefreshAllWindows();
    }

    /// <summary>
    ///     应用主题模式
    /// </summary>
    private static void ApplyThemeMode(ThemeMode themeMode)
    {
        IsDarkTheme = themeMode switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            ThemeMode.System => IsSystemDarkMode(),
            _ => false
        };

        var isDarkMode = IsDarkTheme;

        // 移除旧的主题字典
        if (_currentThemeDictionary != null)
            Application.Current.Resources.MergedDictionaries.Remove(_currentThemeDictionary);

        // 加载新的主题字典
        var themeUri = isDarkMode
            ? "pack://application:,,,/GuiPiao;component/Themes/DarkTheme.xaml"
            : "pack://application:,,,/GuiPiao;component/Themes/LightTheme.xaml";

        _currentThemeDictionary = new ResourceDictionary
        {
            Source = new Uri(themeUri)
        };

        // 插入到合并字典的开头（确保优先级）
        Application.Current.Resources.MergedDictionaries.Insert(0, _currentThemeDictionary);
    }

    /// <summary>
    ///     检测系统是否为深色模式
    /// </summary>
    public static bool IsSystemDarkMode()
    {
        try
        {
            // 通过注册表检测 Windows 系统主题
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue) return intValue == 0; // 0 = 深色模式, 1 = 浅色模式
            }
        }
        catch
        {
            // 如果检测失败，默认使用浅色模式
        }

        return false;
    }

    /// <summary>
    ///     应用强调色（从颜色字符串）
    /// </summary>
    public static void ApplyAccentColor(string colorHex)
    {
        ApplyAccentColorInternal(colorHex);
    }

    /// <summary>
    ///     应用强调色
    /// </summary>
    public static void ApplyAccentColor(AccentColor accentColor, string customColor)
    {
        var colorHex = accentColor switch
        {
            AccentColor.MicrosoftBlue => "#0078D4",
            AccentColor.FreshGreen => "#28A745",
            AccentColor.VitalityOrange => "#FD7E14",
            AccentColor.DarkPurple => "#6F42C1",
            AccentColor.MinimalGray => "#6C757D",
            AccentColor.Custom => customColor,
            _ => "#0078D4"
        };

        ApplyAccentColorInternal(colorHex);
    }

    /// <summary>
    ///     内部方法：应用强调色
    /// </summary>
    private static void ApplyAccentColorInternal(string colorHex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var brush = GetOrCreateBrush(color);

            // 获取当前主题字典并更新（确保覆盖主题文件中的默认值）
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            ResourceDictionary? themeDictionary = null;

            // 查找当前活动的主题字典（最后一个通常是当前主题）
            for (var i = mergedDictionaries.Count - 1; i >= 0; i--)
            {
                var dict = mergedDictionaries[i];
                if (dict.Source != null &&
                    (dict.Source.ToString().Contains("LightTheme.xaml") ||
                     dict.Source.ToString().Contains("DarkTheme.xaml")))
                {
                    themeDictionary = dict;
                    break;
                }
            }

            // 如果找到了主题字典，在其中更新资源（优先级最高）
            var targetDictionary = themeDictionary ?? Application.Current.Resources;

            targetDictionary["AccentColor"] = color;
            targetDictionary["AccentBrush"] = brush;
            targetDictionary["PrimaryBrush"] = brush;

            // 计算悬停和按下颜色
            var hoverColor = AdjustBrightness(color, 0.1);
            var pressedColor = AdjustBrightness(color, -0.1);
            targetDictionary["AccentHoverBrush"] = GetOrCreateBrush(hoverColor);
            targetDictionary["AccentPressedBrush"] = GetOrCreateBrush(pressedColor);

            // 计算对比文字颜色（根据背景色亮度决定使用黑色或白色文字）
            var textBrush = GetContrastTextBrush(color);
            targetDictionary["AccentTextBrush"] = textBrush;
        }
        catch
        {
            // 如果颜色解析失败，使用默认蓝色
            var defaultColor = (Color)ColorConverter.ConvertFromString("#0078D4");
            var defaultBrush = GetOrCreateBrush(defaultColor);
            Application.Current.Resources["AccentColor"] = defaultColor;
            Application.Current.Resources["AccentBrush"] = defaultBrush;
            Application.Current.Resources["PrimaryBrush"] = defaultBrush;
            Application.Current.Resources["AccentTextBrush"] = GetOrCreateBrush(Colors.White);
        }
    }

    /// <summary>
    ///     获取对比文字颜色（根据背景色亮度决定使用黑色或白色）
    /// </summary>
    private static SolidColorBrush GetContrastTextBrush(Color backgroundColor)
    {
        // 使用 YIQ 公式计算亮度
        var brightness = (backgroundColor.R * 299 + backgroundColor.G * 587 + backgroundColor.B * 114) / 1000.0;

        // 亮度 > 128 使用黑色文字，否则使用白色文字
        return brightness > 128 ? GetOrCreateBrush(Colors.Black) : GetOrCreateBrush(Colors.White);
    }

    /// <summary>
    ///     调整颜色亮度
    /// </summary>
    private static Color AdjustBrightness(Color color, double factor)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;

        r = Math.Min(1.0, Math.Max(0.0, r + factor));
        g = Math.Min(1.0, Math.Max(0.0, g + factor));
        b = Math.Min(1.0, Math.Max(0.0, b + factor));

        return Color.FromRgb(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255));
    }

    /// <summary>
    ///     应用字体大小（考虑DPI缩放）
    /// </summary>
    private static void ApplyFontSize(FontSizeOption fontSize)
    {
        // 基础字体大小
        double baseFontSize = fontSize switch
        {
            FontSizeOption.Small => 12,
            FontSizeOption.Medium => 14,
            FontSizeOption.Large => 16,
            FontSizeOption.ExtraLarge => 18,
            _ => 14
        };

        // 应用DPI缩放
        baseFontSize *= _currentDpiScale;

        Application.Current.Resources["BaseFontSize"] = baseFontSize;
        Application.Current.Resources["SmallFontSize"] = baseFontSize - 2;
        Application.Current.Resources["LargeFontSize"] = baseFontSize + 2;
        Application.Current.Resources["TitleFontSize"] = baseFontSize + 4;

        // 根据字体大小调整行高和控件尺寸
        var rowHeightMultiplier = fontSize switch
        {
            FontSizeOption.Small => 0.9,
            FontSizeOption.Medium => 1.0,
            FontSizeOption.Large => 1.15,
            FontSizeOption.ExtraLarge => 1.3,
            _ => 1.0
        };

        // 更新行高资源
        Application.Current.Resources["CompactRowHeight"] = 28 * rowHeightMultiplier;
        Application.Current.Resources["StandardRowHeight"] = 36 * rowHeightMultiplier;
        Application.Current.Resources["LooseRowHeight"] = 44 * rowHeightMultiplier;
        Application.Current.Resources["DataGridRowHeight"] = 36 * rowHeightMultiplier;

        // 更新 LiveCharts 的 Tooltip 字体大小
        UpdateLiveChartsTooltipFontSize(baseFontSize);
    }

    /// <summary>
    ///     更新 LiveCharts 的 Tooltip 字体大小
    /// </summary>
    private static void UpdateLiveChartsTooltipFontSize(double fontSize)
    {
        try
        {
            LiveCharts.Configure(config => { config.TooltipTextSize = fontSize; });
        }
        catch (Exception ex)
        {
            // 记录日志但不影响其他功能
            Debug.WriteLine($"[ThemeManager] 更新 LiveCharts Tooltip 字体大小失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     应用列表行高
    /// </summary>
    private static void ApplyRowHeight(RowHeightOption rowHeight)
    {
        double rowHeightValue = rowHeight switch
        {
            RowHeightOption.Compact => 28,
            RowHeightOption.Standard => 36,
            RowHeightOption.Loose => 44,
            _ => 36
        };

        Application.Current.Resources["DataGridRowHeight"] = rowHeightValue;
    }

    /// <summary>
    ///     获取当前强调色
    /// </summary>
    public static Color GetCurrentAccentColor()
    {
        if (Application.Current.Resources.Contains("AccentColor"))
            return (Color)Application.Current.Resources["AccentColor"];
        return (Color)ColorConverter.ConvertFromString("#0078D4");
    }

    /// <summary>
    ///     获取当前强调色画刷
    /// </summary>
    public static Brush GetCurrentAccentBrush()
    {
        if (Application.Current.Resources.Contains("AccentBrush"))
            return (Brush)Application.Current.Resources["AccentBrush"];
        return new SolidColorBrush(GetCurrentAccentColor());
    }

    /// <summary>
    ///     应用主题到指定窗口
    /// </summary>
    public static void ApplyThemeToWindow(Window window)
    {
        // 窗口会自动继承应用程序资源
        // 这个方法确保窗口在显示前资源已准备好
        if (_currentThemeDictionary != null)
            // 确保窗口资源包含当前主题
            if (!window.Resources.MergedDictionaries.Contains(_currentThemeDictionary))
                window.Resources.MergedDictionaries.Insert(0, _currentThemeDictionary);
    }

    /// <summary>
    ///     刷新所有打开窗口的主题
    /// </summary>
    public static void RefreshAllWindows()
    {
        foreach (Window window in Application.Current.Windows)
        {
            // 跳过主窗口，避免破坏菜单栏等资源
            if (window is MainWindow)
            {
                // 主窗口只需要强制刷新视觉
                window.InvalidateVisual();
                continue;
            }

            // 对于其他窗口（如设置窗口、对话框等），更新其资源字典
            // 移除窗口级别的旧主题资源
            var oldDictionaries = window.Resources.MergedDictionaries
                .Where(d => d.Source != null &&
                            (d.Source.OriginalString.Contains("DarkTheme.xaml") ||
                             d.Source.OriginalString.Contains("LightTheme.xaml")))
                .ToList();

            foreach (var oldDict in oldDictionaries) window.Resources.MergedDictionaries.Remove(oldDict);

            // 添加当前主题到窗口
            if (_currentThemeDictionary != null) window.Resources.MergedDictionaries.Insert(0, _currentThemeDictionary);

            // 强制更新窗口样式
            window.InvalidateVisual();
        }
    }
}