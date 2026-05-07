using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters
{
    /// <summary>
    /// 日期格式化转换器 - 将日期字符串统一格式化为 yyyy/MM/dd 格式
    /// </summary>
    public class DateFormatConverter : IValueConverter
    {
        // 支持多种日期格式
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
                // 尝试使用特定格式解析
                if (DateTime.TryParseExact(dateString, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date.ToString("yyyy/MM/dd");
                }

                // 尝试通用解析
                if (DateTime.TryParse(dateString, out date))
                {
                    return date.ToString("yyyy/MM/dd");
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 不需要反向转换
            return value;
        }
    }
}
