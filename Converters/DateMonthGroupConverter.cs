using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters;

/// <summary>
///     日期按月分组的转换器
/// </summary>
public class DateMonthGroupConverter : IValueConverter
{
    // 支持多种输入日期格式
    private static readonly string[] DateFormats = new[]
    {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "MM/dd/yyyy",
        "yyyy-M-d",
        "yyyy/M/d",
        "d/M/yyyy",
        "d-M-yyyy"
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string dateString && !string.IsNullOrEmpty(dateString))
        {
            // 先尝试使用特定格式解析（优先dd/MM/yyyy格式）
            if (DateTime.TryParseExact(dateString, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var date)) return date.ToString("yyyy年MM月");

            // 尝试通用解析
            if (DateTime.TryParse(dateString, out date)) return date.ToString("yyyy年MM月");
        }

        return value?.ToString() ?? "未知日期";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}