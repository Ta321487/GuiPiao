using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     卡片内容密度到可见性的转换器
///     参数格式: "Compact|Standard|Detailed|Full" 表示在哪些密度下显示
/// </summary>
public class CardDensityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string density || parameter is not string allowedDensities)
            return Visibility.Collapsed;

        // 检查当前密度是否在允许的列表中
        var densities = allowedDensities.Split('|');
        return Array.Exists(densities, d => d.Equals(density, StringComparison.OrdinalIgnoreCase))
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}