using System;
using System.Globalization;
using System.Windows;
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
                // 获取应用程序资源中的样式
                if (currentPage == target)
                    return Application.Current.FindResource("NavItemSelectedStyle");

        return Application.Current.FindResource("NavItemStyle");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}