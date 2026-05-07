using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using GuiPiao.Model;

namespace GuiPiao.Converters;

/// <summary>
///     标签溢出指示器转换器 - 显示"+N"格式的溢出数量
/// </summary>
public class TagOverflowIndicatorConverter : IValueConverter
{
    /// <summary>
    ///     最大显示标签数量
    /// </summary>
    public int MaxDisplayCount { get; set; } = 3;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is List<TicketTag> tags && tags.Count > MaxDisplayCount)
        {
            var overflowCount = tags.Count - MaxDisplayCount;
            return $"+{overflowCount}";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}