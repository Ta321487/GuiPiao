using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GuiPiao.Converters;

public sealed class LayoutFontFamilyMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2) return DependencyProperty.UnsetValue;
        foreach (var v in values)
        {
            if (v is not string s || string.IsNullOrWhiteSpace(s)) continue;
            try
            {
                return new FontFamily(s.Trim());
            }
            catch
            {
                // 尝试下一级回退
            }
        }

        return SystemFonts.MessageFontFamily;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
