using GuiPiao.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters
{
    /// <summary>
    /// 枚举值转描述字符串转换器
    /// </summary>
    public class EnumToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            return value switch
            {
                LayoutType layoutType => GetLayoutTypeName(layoutType),
                TimeRangeType timeRangeType => GetTimeRangeTypeName(timeRangeType),
                ChartType chartType => GetChartTypeName(chartType),
                AutoRefreshType autoRefreshType => GetAutoRefreshTypeName(autoRefreshType),
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string GetLayoutTypeName(LayoutType type)
        {
            return LayoutTypeNames.Names.TryGetValue(type, out var name) ? name : type.ToString();
        }

        private static string GetTimeRangeTypeName(TimeRangeType type)
        {
            return TimeRangeTypeNames.Names.TryGetValue(type, out var name) ? name : type.ToString();
        }

        private static string GetChartTypeName(ChartType type)
        {
            return ChartTypeNames.Names.TryGetValue(type, out var name) ? name : type.ToString();
        }

        private static string GetAutoRefreshTypeName(AutoRefreshType type)
        {
            return AutoRefreshTypeNames.Names.TryGetValue(type, out var name) ? name : type.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
