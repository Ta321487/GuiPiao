using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GuiPiao.Model;

namespace GuiPiao.Converters
{
    /// <summary>
    /// 标签列表转Tooltip转换器 - 将所有标签名称拼接为字符串
    /// </summary>
    public class TagListToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<TicketTag> tags && tags.Count > 0)
            {
                var tagNames = tags.Select(t => t.Name).ToList();
                return string.Join("、", tagNames);
            }
            return "无标签";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
