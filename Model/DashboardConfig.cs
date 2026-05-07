using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GuiPiao.Model;

/// <summary>
///     仪表盘配置
/// </summary>
public partial class DashboardConfig : ObservableObject
{
    // 刷新策略
    [ObservableProperty] private AutoRefreshType _autoRefresh = AutoRefreshType.Off;

    // 已添加的卡片列表
    [ObservableProperty] private ObservableCollection<DashboardCard> _cards = new();

    [ObservableProperty] private int _cardSpacing = 10;

    // 自定义布局网格大小（默认6x6）
    [ObservableProperty] private int _customLayoutGridSize = 6;

    // 图表显示设置
    [ObservableProperty] private bool _enableChartAnimation = true;

    [ObservableProperty] private bool _excludeDuplicateTickets = true;

    [ObservableProperty] private bool _excludeRefundedTickets = true;

    [ObservableProperty] private ChartType _globalChartType = ChartType.Auto;

    [ObservableProperty] private DateTime? _globalCustomEndDate;

    // 全局自定义时间范围
    [ObservableProperty] private DateTime? _globalCustomStartDate;

    [ObservableProperty] private TimeRangeType _globalTimeRange = TimeRangeType.Last12Months;

    // 全局默认配置
    [ObservableProperty] private LayoutType _layoutType = LayoutType.ThreeColumn;
}

/// <summary>
///     仪表盘卡片
/// </summary>
public partial class DashboardCard : ObservableObject
{
    [ObservableProperty] private ChartType _chartType = ChartType.Auto;

    /// <summary>
    ///     卡片自定义配置（JSON序列化存储）
    /// </summary>
    [ObservableProperty] private StatisticCardConfig? _customConfig;

    /// <summary>
    ///     自定义时间段结束日期（当 TimeRange 为 CustomRange 时使用）
    /// </summary>
    [ObservableProperty] private DateTime? _customEndDate;

    /// <summary>
    ///     自定义时间段起始日期（当 TimeRange 为 CustomRange 时使用）
    /// </summary>
    [ObservableProperty] private DateTime? _customStartDate;

    /// <summary>
    ///     在网格布局中的列位置
    /// </summary>
    private int _gridColumn;

    /// <summary>
    ///     在网格布局中跨列数
    /// </summary>
    private int _gridColumnSpan = 1;

    /// <summary>
    ///     在网格布局中的行位置
    /// </summary>
    private int _gridRow;

    /// <summary>
    ///     在网格布局中跨行数
    /// </summary>
    private int _gridRowSpan = 1;

    [ObservableProperty] private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] private int _sortOrder;

    [ObservableProperty] private StatisticType _statisticType;

    [ObservableProperty] private TimeRangeType _timeRange = TimeRangeType.Last12Months;

    [ObservableProperty] private bool _useGlobalConfig = true;

    public int GridRow
    {
        get => _gridRow;
        set
        {
            var clampedValue = Math.Max(0, value);
            if (_gridRow != clampedValue)
            {
                _gridRow = clampedValue;
                OnPropertyChanged();
            }
        }
    }

    public int GridColumn
    {
        get => _gridColumn;
        set
        {
            var clampedValue = Math.Max(0, value);
            if (_gridColumn != clampedValue)
            {
                _gridColumn = clampedValue;
                OnPropertyChanged();
            }
        }
    }

    public int GridRowSpan
    {
        get => _gridRowSpan;
        set
        {
            var clampedValue = Math.Max(1, value);
            if (_gridRowSpan != clampedValue)
            {
                _gridRowSpan = clampedValue;
                OnPropertyChanged();
            }
        }
    }

    public int GridColumnSpan
    {
        get => _gridColumnSpan;
        set
        {
            var clampedValue = Math.Max(1, value);
            if (_gridColumnSpan != clampedValue)
            {
                _gridColumnSpan = clampedValue;
                OnPropertyChanged();
            }
        }
    }
}

/// <summary>
///     布局类型
/// </summary>
public enum LayoutType
{
    ThreeColumn,
    TwoColumn,
    LeftOneRightTwo,
    TopOneBottomTwo
}

/// <summary>
///     时间范围类型
/// </summary>
public enum TimeRangeType
{
    Last3Months,
    Last6Months,
    Last12Months,
    CalendarYear,
    CustomRange
}

/// <summary>
///     图表类型
/// </summary>
public enum ChartType
{
    Auto,
    TextList,
    BarChart,
    PieChart,
    HorizontalBarChart,
    LineChart
}

/// <summary>
///     自动刷新类型
/// </summary>
public enum AutoRefreshType
{
    Off,
    OnStartup,
    Weekly
}

/// <summary>
///     统计类型
/// </summary>
public enum StatisticType
{
    MonthlyTripStats,
    TrainTypeRatio,
    StationTopRanking,
    SeatTypeRatio,
    AnnualTripSummary,
    TripTimeDistribution,
    PopularRouteStats,
    TripCostAnalysis
}

/// <summary>
///     可用统计项
/// </summary>
public class AvailableStatisticItem
{
    public StatisticType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

/// <summary>
///     布局类型显示名称
/// </summary>
public static class LayoutTypeNames
{
    public static readonly Dictionary<LayoutType, string> Names = new()
    {
        { LayoutType.ThreeColumn, "三列等宽" },
        { LayoutType.TwoColumn, "两列等宽" },
        { LayoutType.LeftOneRightTwo, "左一右二" },
        { LayoutType.TopOneBottomTwo, "上一下二" }
    };
}

/// <summary>
///     时间范围类型显示名称
/// </summary>
public static class TimeRangeTypeNames
{
    public static readonly Dictionary<TimeRangeType, string> Names = new()
    {
        { TimeRangeType.Last3Months, "近 3 个月" },
        { TimeRangeType.Last6Months, "近 6 个月" },
        { TimeRangeType.Last12Months, "近 12 个月" },
        { TimeRangeType.CalendarYear, "自然年" },
        { TimeRangeType.CustomRange, "自定义时间段" }
    };
}

/// <summary>
///     图表类型显示名称
/// </summary>
public static class ChartTypeNames
{
    public static readonly Dictionary<ChartType, string> Names = new()
    {
        { ChartType.Auto, "自动适配" },
        { ChartType.TextList, "文本列表" },
        { ChartType.BarChart, "柱状图" },
        { ChartType.PieChart, "饼图" },
        { ChartType.HorizontalBarChart, "条形图" },
        { ChartType.LineChart, "折线图" }
    };
}

/// <summary>
///     自动刷新类型显示名称
/// </summary>
public static class AutoRefreshTypeNames
{
    public static readonly Dictionary<AutoRefreshType, string> Names = new()
    {
        { AutoRefreshType.Off, "关闭" },
        { AutoRefreshType.OnStartup, "每次启动程序" },
        { AutoRefreshType.Weekly, "每周（周日凌晨）" }
    };
}

/// <summary>
///     统计类型显示信息
/// </summary>
public static class StatisticTypeInfo
{
    public static readonly Dictionary<StatisticType, (string Name, string Description, string Icon)> Info = new()
    {
        { StatisticType.MonthlyTripStats, ("月度出行统计", "按月份统计出行次数/里程/花费", "📅") },
        { StatisticType.TrainTypeRatio, ("车次类型占比", "按G/D/Z/T/K统计车票占比", "🚄") },
        { StatisticType.StationTopRanking, ("站点TOP排行", "按出发/到达站统计出行次数TOP5", "🚉") },
        { StatisticType.SeatTypeRatio, ("席别占比统计", "按一等/二等/无座统计车票占比", "💺") },
        { StatisticType.AnnualTripSummary, ("年度出行总结", "按年份统计出行总里程/总花费", "📆") },
        { StatisticType.TripTimeDistribution, ("出行时段分布", "按早/中/晚/凌晨统计出行次数", "⏰") },
        { StatisticType.PopularRouteStats, ("热门线路统计", "按出发+到达站统计热门线路TOP5", "📍") },
        { StatisticType.TripCostAnalysis, ("出行花费分析", "按维度统计出行花费分布", "💰") }
    };
}