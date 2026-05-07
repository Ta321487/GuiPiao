using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     布尔值转 GridLength 转换器 - 用于控制 DataGrid 列的显示/隐藏
/// </summary>
public class BoolToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
            // 显示列 - 返回 Auto
            return GridLength.Auto;
        // 隐藏列 - 返回 0
        return new GridLength(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength) return gridLength.Value > 0;
        return false;
    }
}