using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GuiPiao.Model;

namespace GuiPiao.Converters;

/// <summary>
///     页面类型到样式的转换器
/// </summary>
public class PageToStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SettingsPageType currentPage && parameter is string targetPage)
            if (Enum.TryParse<SettingsPageType>(targetPage, out var target))
                if (currentPage == target)
                    return Application.Current?.TryFindResource("NavItemSelectedStyle") as Style
                           ?? new Style(typeof(ListBoxItem));

        return Application.Current?.TryFindResource("NavItemStyle") as Style
               ?? new Style(typeof(ListBoxItem));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}