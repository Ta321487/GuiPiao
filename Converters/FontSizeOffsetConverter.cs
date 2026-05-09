using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     字体大小偏移转换器 - 基于基础字体大小计算偏移后的值
/// </summary>
public class FontSizeOffsetConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double baseFontSize && parameter is string offsetStr && double.TryParse(offsetStr, out var offset))
            return baseFontSize + offset;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}