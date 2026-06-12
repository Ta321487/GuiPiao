using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GuiPiao.Model;

namespace GuiPiao.Converters;

/// <summary>
///     票面参数工作台与行程预览：工作台模式始终显示完整票面（含「站」字、简字等布局块）；
///     行程预览时按可选 [2] 隐藏无内容的块（如未出现的票种简字）。
///     MultiBinding: [0]=SessionMode，[1]=LayoutIsolationTargetKind（占位，保持与 XAML 一致），可选 [2]=bool。
///     ConverterParameter=目标 TicketFaceLayoutElementKind 枚举名（非法参数时整项 Visible）。
/// </summary>
public sealed class TicketLayoutIsolationToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2) return Visibility.Visible;
        if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
            return Visibility.Visible;

        // 布局工作台：始终显示各布局块（含「站」字、简字等），否则滑块/拖拽调整了也看不到。
        if (values[0] is TicketPreviewSessionMode.LayoutWorkbench)
            return Visibility.Visible;

        var contentVisible = !(values.Length >= 3 && values[2] is bool b && !b);

        var paramStr = parameter as string ?? parameter?.ToString();
        if (string.IsNullOrWhiteSpace(paramStr) ||
            !Enum.TryParse<TicketFaceLayoutElementKind>(paramStr.Trim(), true, out _))
            return Visibility.Visible;

        return contentVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
