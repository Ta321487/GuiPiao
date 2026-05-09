using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GuiPiao.Converters;

/// <summary>
///     布尔值转画刷转换器
///     参数格式：success|error 或 true|success|false|error
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;

        // 解析参数
        var param = parameter?.ToString() ?? "success|error";
        var parts = param.Split('|');

        if (parts.Length >= 2)
        {
            var trueBrushName = parts[0];
            var falseBrushName = parts.Length >= 3 ? parts[2] : parts[1];

            var brushName = boolValue ? trueBrushName : falseBrushName;

            return brushName.ToLower() switch
            {
                "success" => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // #28A745
                "error" => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // #DC3545
                "warning" => new SolidColorBrush(Color.FromRgb(253, 126, 20)), // #FD7E14
                "info" => new SolidColorBrush(Color.FromRgb(0, 123, 255)), // #007BFF
                "primary" => Application.Current?.TryFindResource("AccentBrush") as SolidColorBrush
                    ?? new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        return new SolidColorBrush(boolValue ? Colors.Green : Colors.Red);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}