using System;
using System.Globalization;
using System.Windows.Data;
using GuiPiao.Model;

namespace GuiPiao.Converters;

/// <summary>
///     统计类型转标签文本转换器
/// </summary>
public class StatisticTypeToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not StatisticType type)
            return "分类依据：";

        return type switch
        {
            StatisticType.MonthlyTripStats => "时间粒度：",
            StatisticType.TrainTypeRatio => "分类依据：",
            StatisticType.SeatTypeRatio => "分类依据：",
            StatisticType.StationTopRanking => "统计依据：",
            StatisticType.PopularRouteStats => "统计依据：",
            StatisticType.AnnualTripSummary => "对比维度：",
            StatisticType.TripTimeDistribution => "时段划分：",
            StatisticType.TripCostAnalysis => "统计维度：",
            _ => "分类依据："
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}