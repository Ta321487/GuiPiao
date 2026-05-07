using System;
using System.Threading.Tasks;
using GuiPiao.Model;

namespace GuiPiao.Services;

/// <summary>
///     图表数据服务接口 - 定义各类统计数据的获取方法
/// </summary>
public interface IChartDataService
{
    /// <summary>
    ///     数据刷新事件
    /// </summary>
    event EventHandler? DataRefreshed;

    /// <summary>
    ///     刷新数据
    /// </summary>
    void RefreshData();

    /// <summary>
    ///     清除缓存
    /// </summary>
    void ClearCache();

    /// <summary>
    ///     月度出行统计
    /// </summary>
    Task<ChartData> GetMonthlyTripStatsAsync(StatisticCardConfig config);

    /// <summary>
    ///     车次类型占比
    /// </summary>
    Task<ChartData> GetTrainTypeRatioAsync(StatisticCardConfig config);

    /// <summary>
    ///     站点TOP排行
    /// </summary>
    Task<ChartData> GetStationTopRankingAsync(StatisticCardConfig config);

    /// <summary>
    ///     席别占比统计
    /// </summary>
    Task<ChartData> GetSeatTypeRatioAsync(StatisticCardConfig config);

    /// <summary>
    ///     年度出行总结
    /// </summary>
    Task<ChartData> GetAnnualTripSummaryAsync(StatisticCardConfig config);

    /// <summary>
    ///     出行时段分布
    /// </summary>
    Task<ChartData> GetTripTimeDistributionAsync(StatisticCardConfig config);

    /// <summary>
    ///     热门线路统计
    /// </summary>
    Task<ChartData> GetPopularRouteStatsAsync(StatisticCardConfig config);

    /// <summary>
    ///     出行花费分析
    /// </summary>
    Task<ChartData> GetTripCostAnalysisAsync(StatisticCardConfig config);
}