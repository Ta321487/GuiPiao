using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     布尔值到透明度的转换器
/// </summary>
public class BooleanToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            // 如果为 true（已撤销），返回较低的不透明度
            return boolValue ? 0.5 : 1.0;
        return 1.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}