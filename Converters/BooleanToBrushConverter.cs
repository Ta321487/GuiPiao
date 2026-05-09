using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GuiPiao.Converters;

/// <summary>
///     布尔值到画刷的转换器 - 用于操作历史面板的撤销状态显示
/// </summary>
public class BooleanToBrushConverter : IValueConverter
{
    // 缓存画刷实例
    private static readonly SolidColorBrush GrayBrush = new(Colors.Gray);
    private static readonly SolidColorBrush LightGrayBrush = new(Colors.LightGray);
    private static readonly SolidColorBrush WhiteBrush = new(Colors.White);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isUndone = value is bool b && b;
        var param = parameter?.ToString() ?? string.Empty;

        if (isUndone)
            // 已撤销状态 - 返回灰色
            return GrayBrush;

        // 正常状态 - 根据参数返回不同的画刷
        if (param == "UndoneTimestamp")
        {
            // 时间戳使用次要文本颜色
            var brush = Application.Current?.Resources["TextSecondaryBrush"] as Brush;
            return brush ?? LightGrayBrush;
        }
        else
        {
            // 普通文本使用主要文本颜色
            var brush = Application.Current?.Resources["TextPrimaryBrush"] as Brush;
            return brush ?? WhiteBrush;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}