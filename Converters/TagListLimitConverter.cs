using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using GuiPiao.Model;

namespace GuiPiao.Converters
{
    /// <summary>
    /// 标签列表限制转换器 - 只显示前N个标签
    /// </summary>
    public class TagListLimitConverter : IValueConverter
    {
        /// <summary>
        /// 最大显示标签数量
        /// </summary>
        public int MaxDisplayCount { get; set; } = 3;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<TicketTag> tags && tags.Count > 0)
            {
                if (tags.Count <= MaxDisplayCount)
                {
                    return tags;
                }
                return tags.Take(MaxDisplayCount).ToList();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
