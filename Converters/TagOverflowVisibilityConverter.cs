using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GuiPiao.Model;

namespace GuiPiao.Converters;

/// <summary>
///     标签溢出可见性转换器 - 当标签数量超过限制时显示溢出指示器
/// </summary>
public class TagOverflowVisibilityConverter : IValueConverter
{
    /// <summary>
    ///     最大显示标签数量
    /// </summary>
    public int MaxDisplayCount { get; set; } = 3;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is List<TicketTag> tags && tags.Count > MaxDisplayCount) return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}