using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GuiPiao.Converters;

/// <summary>
///     Hex颜色字符串转Brush转换器
/// </summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hexColor)
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush) return brush.Color.ToString();
        return "#808080";
    }
}