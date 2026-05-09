using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     检查集合是否包含指定值的转换器
/// </summary>
public class CollectionContainsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable collection && parameter != null)
            foreach (var item in collection)
                if (item?.ToString() == parameter.ToString())
                    return true;

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}