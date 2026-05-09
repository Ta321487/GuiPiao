using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     字符串非空转可见性转换器
/// </summary>
public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str) return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}