using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     整数比较转换器，当值等于参数时返回Visible，否则返回Collapsed
/// </summary>
public class IntComparisonToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string paramString)
        {
            if (int.TryParse(paramString, out var paramValue))
            {
                return intValue == paramValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
///     整数比较转换器（反向），当值等于参数时返回Collapsed，否则返回Visible
/// </summary>
public class InverseIntComparisonToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue && parameter is string paramString)
        {
            if (int.TryParse(paramString, out var paramValue))
            {
                return intValue == paramValue ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
