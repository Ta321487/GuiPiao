using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     将 yyyy-MM-dd 等日期字符串格式化为「2025年6月30日」用于票面展示。
/// </summary>
public class RailwayDateToChineseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return value ?? string.Empty;

        if (DateTime.TryParse(s, culture, DateTimeStyles.None, out var d) ||
            DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return $"{d.Year}年{d.Month}月{d.Day}日";

        return s;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
