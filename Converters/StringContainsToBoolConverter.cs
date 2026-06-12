using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     判断字符串是否包含指定子串，用于票种/渠道多选结果的只读勾选展示。
/// </summary>
public class StringContainsToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string token || string.IsNullOrEmpty(token)) return false;
        var s = value?.ToString() ?? string.Empty;
        return s.Contains(token, StringComparison.Ordinal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
