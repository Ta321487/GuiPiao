using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuiPiao.Model;

namespace GuiPiao.Services;

/// <summary>
///     图表数据查询构建器 - 提供通用的数据查询和配置处理逻辑
/// </summary>
public static class ChartDataQueryBuilder
{
    /// <summary>
    ///     根据配置过滤数据
    /// </summary>
    public static IEnumerable<T> FilterByConfig<T>(
        this IEnumerable<T> source,
        StatisticCardConfig config,
        Func<T, string> dateSelector,
        Func<T, string> statusSelector) where T : class
    {
        var query = source.AsEnumerable();

        if (config == null)
            return query;

        // 排除已改签和已退票
        if (config.ExcludeRefundedTickets)
            query = query.Where(t => statusSelector(t) != "已改签" && statusSelector(t) != "已退票");

        // 时间范围过滤
        query = config.TimeRange switch
        {
            "近 3 个月" => query.Where(t => IsInLastMonths(dateSelector(t), 3)),
            "近 6 个月" => query.Where(t => IsInLastMonths(dateSelector(t), 6)),
            "近 12 个月" => query.Where(t => IsInLastMonths(dateSelector(t), 12)),
            "自然年" => query.Where(t => IsInCurrentYear(dateSelector(t))),
            "自定义时间段" => query.Where(t => IsInDateRange(dateSelector(t), config.CustomStartDate, config.CustomEndDate)),
            _ => query
        };

        return query;
    }

    /// <summary>
    ///     根据时间粒度分组，返回包含年份信息的分组
    /// </summary>
    public static IEnumerable<IGrouping<TimeGroupKey, T>> GroupByTimeGranularity<T>(
        this IEnumerable<T> source,
        string timeGranularity,
        Func<T, DateTime> dateSelector)
    {
        return timeGranularity switch
        {
            "自然周" or "周" => source.GroupBy(t => new TimeGroupKey(dateSelector(t).Year, GetWeekOfYear(dateSelector(t)))),
            "季度" => source.GroupBy(t => new TimeGroupKey(dateSelector(t).Year, (dateSelector(t).Month - 1) / 3 + 1)),
            "半年" => source.GroupBy(t => new TimeGroupKey(dateSelector(t).Year, (dateSelector(t).Month - 1) / 6 + 1)),
            "自然月" or _ => source.GroupBy(t => new TimeGroupKey(dateSelector(t).Year, dateSelector(t).Month)) // 默认自然月
        };
    }

    /// <summary>
    ///     根据统计指标计算值
    /// </summary>
    public static double CalculateValue<T>(
        this IEnumerable<T> group,
        string statisticIndicator,
        Func<T, double> countSelector,
        Func<T, double> priceSelector,
        Func<T, double> distanceSelector,
        Func<T, string>? tripKeySelector = null) where T : class
    {
        // 移除"占比"后缀，统一处理
        var indicator = statisticIndicator?.Replace("占比", "") ?? "";

        return indicator switch
        {
            "出行花费" or "总花费" => group.Sum(priceSelector),
            "出行里程" or "总里程" => group.Sum(distanceSelector),
            "车票数量" => group.Sum(countSelector),
            "出行次数" => tripKeySelector != null
                ? group.Select(tripKeySelector).Distinct().Count() // 按行程去重
                : group.Sum(countSelector),
            "平均单次花费" => group.Any() ? group.Average(priceSelector) : 0,
            _ => group.Sum(countSelector) // 默认出行次数
        };
    }

    /// <summary>
    ///     根据排序方式排序
    /// </summary>
    public static IEnumerable<T> ApplySortOrder<T, TKey>(
        this IEnumerable<T> source,
        string sortOrder,
        Func<T, TKey> periodSelector,
        Func<T, double> valueSelector) where TKey : IComparable<TKey>
    {
        return sortOrder switch
        {
            "按时间降序" => source.OrderByDescending(periodSelector),
            "按数值降序" => source.OrderByDescending(valueSelector),
            "按数值升序" => source.OrderBy(valueSelector),
            _ => source.OrderBy(periodSelector) // 默认按时间升序
        };
    }

    /// <summary>
    ///     生成时间粒度标签（包含年份信息）
    /// </summary>
    public static string[] GenerateTimeLabels(IEnumerable<TimeGroupKey> keys, string timeGranularity)
    {
        var keyList = keys.ToList();

        // 检查是否跨年
        var years = keyList.Select(k => k.Year).Distinct().ToList();
        var isCrossYear = years.Count > 1;

        return timeGranularity switch
        {
            "自然周" or "周" => keyList.Select(k => isCrossYear ? $"{k.Year}年第{k.Period}周" : $"第{k.Period}周").ToArray(),
            "季度" => keyList.Select(k => isCrossYear ? $"{k.Year}年Q{k.Period}" : $"Q{k.Period}").ToArray(),
            "半年" => keyList.Select(k => isCrossYear ? $"{k.Year}年H{k.Period}" : $"H{k.Period}").ToArray(),
            "自然月" or _ => keyList.Select(k => isCrossYear ? $"{k.Year}年{k.Period}月" : $"{k.Period}月").ToArray()
        };
    }

    /// <summary>
    ///     生成时间粒度标签（旧版本，用于兼容）
    /// </summary>
    [Obsolete("请使用包含 TimeGroupKey 的重载版本")]
    public static string[] GenerateTimeLabels(IEnumerable<int> periods, string timeGranularity)
    {
        return timeGranularity switch
        {
            "自然周" or "周" => periods.Select(x => $"第{x}周").ToArray(),
            "季度" => periods.Select(x => $"Q{x}").ToArray(),
            "半年" => periods.Select(x => $"H{x}").ToArray(),
            "自然月" or _ => periods.Select(x => $"{x}月").ToArray()
        };
    }

    /// <summary>
    ///     时间分组键（包含年份和周期信息）
    /// </summary>
    public readonly record struct TimeGroupKey(int Year, int Period) : IComparable<TimeGroupKey>
    {
        public int CompareTo(TimeGroupKey other)
        {
            var yearComparison = Year.CompareTo(other.Year);
            if (yearComparison != 0) return yearComparison;
            return Period.CompareTo(other.Period);
        }
    }

    #region 辅助方法

    // 基准日期，用于测试（可以通过配置修改）
    public static DateTime BaseDate { get; set; } = DateTime.Now;

    private static bool IsInLastMonths(string dateStr, int months)
    {
        if (!DateTime.TryParse(dateStr, out var date)) return false;
        var startDate = BaseDate.AddMonths(-months).Date;
        var endDate = BaseDate.Date;
        return date.Date >= startDate && date.Date <= endDate;
    }

    private static bool IsInCurrentYear(string dateStr)
    {
        if (!DateTime.TryParse(dateStr, out var date)) return false;
        return date.Year == BaseDate.Year;
    }

    private static bool IsInDateRange(string dateStr, DateTime? start, DateTime? end)
    {
        if (!DateTime.TryParse(dateStr, out var date)) return false;
        if (start.HasValue && date < start.Value) return false;
        if (end.HasValue && date > end.Value) return false;
        return true;
    }

    private static int GetWeekOfYear(DateTime date)
    {
        var calendar = CultureInfo.CurrentCulture.Calendar;
        return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }

    #endregion
}