using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     将整数转换为 Thickness（四边相同的边距）
/// </summary>
public class IntToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
            // 将整数转换为四边相同的 Thickness
            return new Thickness(intValue);

        if (value is double doubleValue) return new Thickness(doubleValue);

        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Thickness thickness)
            // 返回左边的值作为代表
            return (int)thickness.Left;

        return 0;
    }
}