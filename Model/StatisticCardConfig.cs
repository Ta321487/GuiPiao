using System;
using System.Collections.Generic;

namespace GuiPiao.Model
{
    /// <summary>
    /// 统计卡片配置基类（用于JSON序列化存储）
    /// </summary>
    public class StatisticCardConfig
    {
        public StatisticType StatisticType { get; set; }

        public string CardName { get; set; } = string.Empty;

        public string CardIcon { get; set; } = string.Empty;

        // 统计维度配置
        public string TimeRange { get; set; } = "跟随全局";

        // 自定义时间范围（当TimeRange为"自定义时间段"时使用）
        public DateTime? CustomStartDate { get; set; }

        public DateTime? CustomEndDate { get; set; }

        public string ClassificationBasis { get; set; } = string.Empty;

        public string StatisticIndicator { get; set; } = string.Empty;

        public string DisplayThreshold { get; set; } = "1%";

        public string TimeGranularity { get; set; } = string.Empty;

        public string SortOrder { get; set; } = string.Empty;

        public int TopCount { get; set; } = 5;

        // 自定义时段配置（当ClassificationBasis为"自定义时段"时使用）
        public List<CustomTimePeriod> CustomTimePeriods { get; set; } = new List<CustomTimePeriod>();

        // 显示样式配置
        public bool UseCustomChartType { get; set; }

        public ChartType ChartType { get; set; } = ChartType.PieChart;

        public string ChartColor { get; set; } = "#2E7D32";

        public bool ShowPercentage { get; set; }

        public bool ShowValue { get; set; }

        public bool ShowTooltip { get; set; } = true;

        public bool ShowValueLabel { get; set; }

        public bool ShowTrendLine { get; set; }

        // 数据过滤配置
        public bool UseCustomFilter { get; set; }

        public bool ExcludeRefundedTickets { get; set; }

        public bool ExcludeDuplicateTickets { get; set; }
    }

    /// <summary>
    /// 车次类型占比配置
    /// </summary>
    public class TrainTypeRatioConfig : StatisticCardConfig
    {
        public TrainTypeRatioConfig()
        {
            StatisticType = StatisticType.TrainTypeRatio;
            CardName = "车次类型占比";
            CardIcon = "🚄";
            ClassificationBasis = "按车次类型";
            StatisticIndicator = "车票数量占比";
            DisplayThreshold = "1%";
            ChartType = ChartType.PieChart;
            ChartColor = "#2E7D32";
            ShowTooltip = true;
        }
    }

    /// <summary>
    /// 月度出行统计配置
    /// </summary>
    public class MonthlyTripStatsConfig : StatisticCardConfig
    {
        public MonthlyTripStatsConfig()
        {
            StatisticType = StatisticType.MonthlyTripStats;
            CardName = "月度出行统计";
            CardIcon = "📅";
            TimeRange = "近 12 个月";
            TimeGranularity = "自然月";
            StatisticIndicator = "出行次数";
            SortOrder = "按时间升序";
            ChartType = ChartType.BarChart;
            ChartColor = "#1976D2";
            ShowValueLabel = true;
            ShowTrendLine = true;
            ShowTooltip = true;
        }
    }

    /// <summary>
    /// 站点TOP排行配置
    /// </summary>
    public class StationTopRankingConfig : StatisticCardConfig
    {
        public StationTopRankingConfig()
        {
            StatisticType = StatisticType.StationTopRanking;
            CardName = "站点TOP排行";
            CardIcon = "🚉";
            ClassificationBasis = "按出发站";
            TopCount = 5;
            StatisticIndicator = "出行次数";
            SortOrder = "按数值降序";
            ChartType = ChartType.HorizontalBarChart;
            ChartColor = "#388E3C";
            ShowTooltip = true;
        }
    }

    /// <summary>
    /// 席别占比统计配置
    /// </summary>
    public class SeatTypeRatioConfig : StatisticCardConfig
    {
        public SeatTypeRatioConfig()
        {
            StatisticType = StatisticType.SeatTypeRatio;
            CardName = "席别占比统计";
            CardIcon = "💺";
            ClassificationBasis = "按席别";
            StatisticIndicator = "车票数量占比";
            DisplayThreshold = "1%";
            ChartType = ChartType.PieChart;
            ChartColor = "#9C27B0";
            ShowTooltip = true;
        }
    }

    /// <summary>
    /// 年度出行总结配置
    /// </summary>
    public class AnnualTripSummaryConfig : StatisticCardConfig
    {
        public AnnualTripSummaryConfig()
        {
            StatisticType = StatisticType.AnnualTripSummary;
            CardName = "年度出行总结";
            CardIcon = "📆";
            StatisticIndicator = "总出行次数";
            ClassificationBasis = "无对比";
            ChartType = ChartType.LineChart;
            ChartColor = "#F57C00";
            ShowTooltip = true;
        }
    }

    /// <summary>
    /// 出行时段分布配置
    /// </summary>
    public class TripTimeDistributionConfig : StatisticCardConfig
    {
        public TripTimeDistributionConfig()
        {
            StatisticType = StatisticType.TripTimeDistribution;
            CardName = "出行时段分布";
            CardIcon = "⏰";
            ClassificationBasis = "4段（凌晨/早/中/晚）";
            StatisticIndicator = "出行次数";
            ChartType = ChartType.PieChart;
            ChartColor = "#00ACC1";
            ShowTooltip = true;
        }
    }

    /// <summary>
    /// 热门线路统计配置
    /// </summary>
    public class PopularRouteStatsConfig : StatisticCardConfig
    {
        public PopularRouteStatsConfig()
        {
            StatisticType = StatisticType.PopularRouteStats;
            CardName = "热门线路统计";
            CardIcon = "📍";
            ClassificationBasis = "按出发-到达站";
            TopCount = 5;
            StatisticIndicator = "出行次数";
            SortOrder = "按数值降序";
            ChartType = ChartType.HorizontalBarChart;
            ChartColor = "#E53935";
            ShowTooltip = true;
        }
    }

    /// <summary>
    /// 出行花费分析配置
    /// </summary>
    public class TripCostAnalysisConfig : StatisticCardConfig
    {
        public TripCostAnalysisConfig()
        {
            StatisticType = StatisticType.TripCostAnalysis;
            CardName = "出行花费分析";
            CardIcon = "💰";
            ClassificationBasis = "按月份";
            StatisticIndicator = "总花费";
            ChartType = ChartType.BarChart;
            ChartColor = "#689F38";
            ShowTooltip = true;
        }
    }
}
