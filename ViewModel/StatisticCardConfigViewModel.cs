using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.View;
using System.Linq;
using System.Windows;

namespace GuiPiao.ViewModel
{
    /// <summary>
    /// 统计卡片配置视图模型
    /// </summary>
    public partial class StatisticCardConfigViewModel : ObservableObject
    {
        private StatisticCardConfig _config;
        private readonly DashboardConfig _globalConfig;

        [ObservableProperty]
        private string _windowTitle = string.Empty;

        /// <summary>
        /// 配置对象（用于绑定）
        /// </summary>
        public StatisticCardConfig Config
        {
            get => _config;
            set
            {
                if (_config != value)
                {
                    _config = value;
                    OnPropertyChanged(nameof(Config));
                    // 同时通知所有依赖 Config 的属性
                    OnPropertyChanged(nameof(UseCustomFilter));
                    OnPropertyChanged(nameof(TimeRangeDisplay));
                    OnPropertyChanged(nameof(ChartTypeDisplay));
                    OnPropertyChanged(nameof(CurrentClassificationBasisOptions));
                    OnPropertyChanged(nameof(CurrentStatisticIndicatorOptions));
                    OnPropertyChanged(nameof(CurrentSortOrderOptions));
                    OnPropertyChanged(nameof(CurrentChartTypeOptions));
                }
            }
        }

        /// <summary>
        /// 全局过滤配置 - 排除已退票/改签车票
        /// </summary>
        public bool GlobalExcludeRefundedTickets => _globalConfig?.ExcludeRefundedTickets ?? false;

        /// <summary>
        /// 全局过滤配置 - 排除重复录入车票
        /// </summary>
        public bool GlobalExcludeDuplicateTickets => _globalConfig?.ExcludeDuplicateTickets ?? false;

        /// <summary>
        /// 全局时间范围
        /// </summary>
        public string GlobalTimeRange => _globalConfig?.GlobalTimeRange.ToString() switch
        {
            "Last3Months" => "近 3 个月",
            "Last6Months" => "近 6 个月",
            "Last12Months" => "近 12 个月",
            "CalendarYear" => "自然年",
            "CustomRange" => "自定义时间段",
            _ => "近 12 个月"
        };

        /// <summary>
        /// 全局图表类型
        /// </summary>
        public ChartType GlobalChartType => _globalConfig?.GlobalChartType ?? ChartType.Auto;

        /// <summary>
        /// 实际显示的图表类型是否为饼图（用于控制"显示占比百分比"选项的可见性）
        /// </summary>
        public bool IsActualPieChart => ActualChartType == ChartType.PieChart;

        // ========== 月度出行统计 下拉选项 ==========
        // 时间粒度：自然月、自然周、季度、半年
        public string[] TimeGranularityOptions => new[] { "自然月", "自然周", "季度", "半年" };

        // 统计指标：出行次数、出行里程、出行花费、车票数量、平均单次花费
        public string[] MonthlyStatisticIndicatorOptions => new[] { "出行次数", "出行里程", "出行花费", "车票数量", "平均单次花费" };

        // 排序方式：按时间升序、按数值降序、按数值升序
        public string[] MonthlySortOrderOptions => new[] { "按时间升序", "按数值降序", "按数值升序" };

        // 图表类型：柱状图、折线图、文本列表、面积图
        public ChartType[] MonthlyChartTypeOptions => new[] { ChartType.BarChart, ChartType.LineChart, ChartType.TextList };

        // ========== 车次/席别占比 下拉选项 ==========
        // 分类依据：按车次类型、按席别、按出发时段、按出行日期（工作日/周末）
        public string[] RatioClassificationBasisOptions => new[] { "按车次类型", "按席别", "按出发时段", "按出行日期（工作日/周末）" };

        // 统计指标：车票数量占比、出行里程占比、出行花费占比、出行次数占比
        public string[] RatioStatisticIndicatorOptions => new[] { "车票数量占比", "出行里程占比", "出行花费占比", "出行次数占比" };

        // 显示阈值：1%、2%、5%、10%
        public string[] DisplayThresholdOptions => new[] { "1%", "2%", "5%", "10%" };

        // 图表类型：饼图、环形图、文本列表、柱状图
        public ChartType[] RatioChartTypeOptions => new[] { ChartType.PieChart, ChartType.BarChart, ChartType.TextList };

        // ========== 站点/线路 TOP 下拉选项 ==========
        // 统计依据：按出发站、按到达站
        public string[] TopStationBasisOptions => new[] { "按出发站", "按到达站" };

        // 线路统计依据：按出发-到达站
        public string[] PopularRouteBasisOptions => new[] { "按出发-到达站" };

        // 显示数量：TOP3、TOP5、TOP10
        public int[] TopCountOptions => new[] { 3, 5, 10 };

        // 统计指标：出行次数、出行里程、平均花费
        public string[] TopStatisticIndicatorOptions => new[] { "出行次数", "出行里程", "平均花费" };

        // 排序方式：按数值降序、按站点名称升序（拼音）
        public string[] TopSortOrderOptions => new[] { "按数值降序", "按站点名称升序（拼音）" };

        // 图表类型：条形图、饼图、文本列表、雷达图
        public ChartType[] TopChartTypeOptions => new[] { ChartType.HorizontalBarChart, ChartType.PieChart, ChartType.TextList };

        // ========== 年度出行总结 下拉选项 ==========
        // 统计指标：总出行次数、总里程、总花费、平均每月出行次数
        public string[] AnnualStatisticIndicatorOptions => new[] { "总出行次数", "总里程", "总花费", "平均每月出行次数" };

        // 对比维度：无对比、与上一年对比、与近三年均值对比
        public string[] AnnualCompareDimensionOptions => new[] { "无对比", "与上一年对比", "与近三年均值对比" };

        // 图表类型：折线图、柱状图、文本列表
        public ChartType[] AnnualChartTypeOptions => new[] { ChartType.LineChart, ChartType.BarChart, ChartType.TextList };

        // ========== 出行时段分布 下拉选项 ==========
        // 时段划分：4段（凌晨/早/中/晚）、6段（细粒度）、自定义时段
        public string[] TimePeriodDivisionOptions => new[] { "4段（凌晨/早/中/晚）", "6段（细粒度）", "自定义时段" };

        // 统计指标：出行次数、车票数量、出行花费
        public string[] TimeDistributionStatisticIndicatorOptions => new[] { "出行次数", "车票数量", "出行花费" };

        // 图表类型：饼图、条形图、文本列表
        public ChartType[] TimeDistributionChartTypeOptions => new[] { ChartType.PieChart, ChartType.HorizontalBarChart, ChartType.TextList };

        // ========== 出行花费分析 下拉选项 ==========
        // 统计维度：按月份、按车次类型、按席别、按出行时段
        public string[] CostAnalysisDimensionOptions => new[] { "按月份", "按车次类型", "按席别", "按出行时段" };

        // 统计指标：总花费、平均单次花费、各维度占比
        public string[] CostAnalysisIndicatorOptions => new[] { "总花费", "平均单次花费", "各维度占比" };

        // 图表类型：柱状图、折线图、饼图、文本列表
        public ChartType[] CostAnalysisChartTypeOptions => new[] { ChartType.BarChart, ChartType.LineChart, ChartType.PieChart, ChartType.TextList };

        // ========== 通用选项 ==========
        private static readonly string[] _timeRangeOptions = new[] { "近 3 个月", "近 6 个月", "近 12 个月", "自然年", "自定义时间段" };
        private static readonly string[] _timeRangeOptionsWithGlobal = new[] { "跟随全局", "近 3 个月", "近 6 个月", "近 12 个月", "自然年", "自定义时间段" };

        // 时间范围选项（单独配置时使用，不包含"跟随全局"）
        public string[] TimeRangeOptions => _timeRangeOptions;

        // 时间范围选项（全局配置时使用，包含"跟随全局"）
        public string[] TimeRangeOptionsWithGlobal => _timeRangeOptionsWithGlobal;

        // 图表颜色
        public string[] ChartColorOptions => new[] { "#2E7D32", "#1976D2", "#388E3C", "#9C27B0", "#F57C00", "#D32F2F", "#00796B", "#5E35B1", "#00ACC1", "#E53935", "#689F38" };

        // 当前统计类型对应的选项（用于XAML绑定）
        public string[] CurrentClassificationBasisOptions => GetClassificationBasisOptions(Config.StatisticType);
        public string[] CurrentStatisticIndicatorOptions => GetStatisticIndicatorOptions(Config.StatisticType);
        public string[] CurrentSortOrderOptions => GetSortOrderOptions(Config.StatisticType);
        public ChartType[] CurrentChartTypeOptions => GetChartTypeOptions(Config.StatisticType);

        // 时间范围选项（不包含"跟随全局"）
        public string[] TimeRangeOptionsWithFollowGlobal => _timeRangeOptions;

        /// <summary>
        /// 是否单独配置图表类型（而非跟随全局）
        /// </summary>
        public bool UseCustomChartType
        {
            get => Config.UseCustomChartType;
            set
            {
                if (Config.UseCustomChartType != value)
                {
                    Config.UseCustomChartType = value;
                    
                    // 如果开启自定义，且当前值为 Auto，设置为第一个可用选项
                    if (value && Config.ChartType == ChartType.Auto)
                    {
                        var availableOptions = GetChartTypeOptions(Config.StatisticType);
                        if (availableOptions.Length > 0)
                        {
                            Config.ChartType = availableOptions[0];
                        }
                    }
                    // 如果关闭自定义，重置为跟随全局
                    else if (!value)
                    {
                        Config.ChartType = ChartType.Auto;
                    }
                    
                    OnPropertyChanged(nameof(UseCustomChartType));
                    OnPropertyChanged(nameof(ChartTypeOptionsForDisplay));
                    OnPropertyChanged(nameof(ChartTypeDisplay));
                    OnPropertyChanged(nameof(IsChartTypeEditable));
                    OnPropertyChanged(nameof(IsActualPieChart));
                }
            }
        }

        /// <summary>
        /// 图表类型选项（未勾选单独配置时跟随全局，勾选时显示所有可用选项）
        /// </summary>
        public object[] ChartTypeOptionsForDisplay
        {
            get
            {
                // 如果未勾选单独配置，只显示当前实际使用的类型
                if (!Config.UseCustomChartType)
                {
                    return new object[] { ActualChartType };
                }
                
                // 勾选了单独配置，显示所有可用选项
                return GetChartTypeOptions(Config.StatisticType).Cast<object>().ToArray();
            }
        }

        /// <summary>
        /// 实际使用的图表类型（考虑全局设置和自定义设置）
        /// </summary>
        private ChartType ActualChartType
        {
            get
            {
                // 如果勾选了单独配置，使用自定义类型
                if (Config.UseCustomChartType && Config.ChartType != ChartType.Auto)
                {
                    return Config.ChartType;
                }
                
                // 否则跟随全局
                if (GlobalChartType != ChartType.Auto)
                {
                    return GlobalChartType;
                }
                
                // 全局为 Auto，使用统计类型的默认类型
                return Config.StatisticType switch
                {
                    StatisticType.TrainTypeRatio => ChartType.PieChart,
                    StatisticType.SeatTypeRatio => ChartType.PieChart,
                    StatisticType.TripTimeDistribution => ChartType.PieChart,
                    StatisticType.StationTopRanking => ChartType.HorizontalBarChart,
                    StatisticType.PopularRouteStats => ChartType.HorizontalBarChart,
                    StatisticType.AnnualTripSummary => ChartType.LineChart,
                    StatisticType.MonthlyTripStats => ChartType.BarChart,
                    StatisticType.TripCostAnalysis => ChartType.BarChart,
                    _ => ChartType.BarChart
                };
            }
        }

        /// <summary>
        /// 图表类型是否可编辑（勾选了单独配置时可编辑）
        /// </summary>
        public bool IsChartTypeEditable => Config.UseCustomChartType;

        /// <summary>
        /// 时间范围显示值（用于绑定到ComboBox）- 受 UseCustomFilter 控制
        /// </summary>
        public string TimeRangeDisplay
        {
            get
            {
                if (Config.UseCustomFilter)
                {
                    // 如果在自定义模式下值无效或为"跟随全局"，返回第一个实际选项
                    if (string.IsNullOrEmpty(Config.TimeRange) || Config.TimeRange == "跟随全局")
                    {
                        return _timeRangeOptions[0];
                    }
                    return Config.TimeRange;
                }
                return "跟随全局";
            }
            set
            {
                // 仅在启用自定义过滤且选择了一个非"跟随全局"的有效值时才更新
                if (Config.UseCustomFilter && !string.IsNullOrEmpty(value) && value != "跟随全局")
                {
                    if (Config.TimeRange != value)
                    {
                        Config.TimeRange = value;
                        OnPropertyChanged(nameof(TimeRangeDisplay));
                    }
                }
                else if (Config.UseCustomFilter && value == "跟随全局")
                {
                    // 如果在自定义模式下用户选了"跟随全局"，强制刷回当前值
                    OnPropertyChanged(nameof(TimeRangeDisplay));
                }
            }
        }

        /// <summary>
        /// 图表类型显示值（用于绑定到ComboBox）
        /// 未勾选单独配置时：显示实际使用的类型（跟随全局）
        /// 勾选单独配置时：显示自定义选择的类型
        /// </summary>
        public object ChartTypeDisplay
        {
            get
            {
                // 如果勾选了自定义图表类型，显示 Config.ChartType
                if (Config.UseCustomChartType && Config.ChartType != ChartType.Auto)
                {
                    return Config.ChartType;
                }
                // 否则返回实际使用的类型（跟随全局）
                return ActualChartType;
            }
            set
            {
                // 只有勾选了单独配置时才允许修改
                if (!Config.UseCustomChartType)
                    return;
                    
                if (value is ChartType chartType && Config.ChartType != chartType)
                {
                    Config.ChartType = chartType;
                    OnPropertyChanged(nameof(ChartTypeDisplay));
                    OnPropertyChanged(nameof(IsActualPieChart));
                }
            }
        }

        /// <summary>
        /// 是否使用自定义时间范围（而非全局设置）
        /// </summary>
        public bool UseCustomFilter
        {
            get => Config.UseCustomFilter;
            set
            {
                if (Config.UseCustomFilter != value)
                {
                    // 1. 如果开启自定义，同步全局时间配置作为初始值
                    if (value)
                    {
                        Config.TimeRange = GlobalTimeRange;

                        // 同步全局自定义日期，并进行基本的合法性校准
                        var start = _globalConfig.GlobalCustomStartDate;
                        var end = _globalConfig.GlobalCustomEndDate;

                        if (start != null && end != null && start > end)
                        {
                            // 如果全局配置中存在非法的颠倒日期，进行自动校准
                            Config.CustomStartDate = end;
                            Config.CustomEndDate = start;
                        }
                        else
                        {
                            Config.CustomStartDate = start;
                            Config.CustomEndDate = end;
                        }
                    }
                    else
                    {
                        // 关闭自定义时，重置为跟随全局状态
                        Config.TimeRange = "跟随全局";
                    }

                    // 2. 更新 Config 标志
                    Config.UseCustomFilter = value;

                    // 3. 通知相关属性变化
                    OnPropertyChanged(nameof(UseCustomFilter));
                    OnPropertyChanged(nameof(TimeRangeDisplay));
                }
            }
        }

        public StatisticCardConfigViewModel(StatisticCardConfig config, DashboardConfig? globalConfig = null)
        {
            _config = config;
            _globalConfig = globalConfig ?? new DashboardConfig();
            WindowTitle = $"{config.CardIcon} {config.CardName} - 统计配置";

            // 如果启用自定义配置但时间范围无效，设置为默认值
            if (config.UseCustomFilter && (string.IsNullOrEmpty(config.TimeRange) || config.TimeRange == "跟随全局"))
            {
                config.TimeRange = TimeRangeOptions[0];
            }
        }

        /// <summary>
        /// 根据统计类型获取分类依据选项
        /// </summary>
        private string[] GetClassificationBasisOptions(StatisticType type)
        {
            return type switch
            {
                StatisticType.TrainTypeRatio or StatisticType.SeatTypeRatio => RatioClassificationBasisOptions,
                StatisticType.StationTopRanking => TopStationBasisOptions,
                StatisticType.PopularRouteStats => PopularRouteBasisOptions,
                StatisticType.TripCostAnalysis => CostAnalysisDimensionOptions,
                _ => RatioClassificationBasisOptions
            };
        }

        /// <summary>
        /// 根据统计类型获取统计指标选项
        /// </summary>
        private string[] GetStatisticIndicatorOptions(StatisticType type)
        {
            return type switch
            {
                StatisticType.MonthlyTripStats => MonthlyStatisticIndicatorOptions,
                StatisticType.TrainTypeRatio or StatisticType.SeatTypeRatio => RatioStatisticIndicatorOptions,
                StatisticType.StationTopRanking or StatisticType.PopularRouteStats => TopStatisticIndicatorOptions,
                StatisticType.AnnualTripSummary => AnnualStatisticIndicatorOptions,
                StatisticType.TripTimeDistribution => TimeDistributionStatisticIndicatorOptions,
                StatisticType.TripCostAnalysis => CostAnalysisIndicatorOptions,
                _ => MonthlyStatisticIndicatorOptions
            };
        }

        /// <summary>
        /// 根据统计类型获取排序方式选项
        /// </summary>
        private string[] GetSortOrderOptions(StatisticType type)
        {
            return type switch
            {
                StatisticType.MonthlyTripStats => MonthlySortOrderOptions,
                StatisticType.StationTopRanking or StatisticType.PopularRouteStats => TopSortOrderOptions,
                _ => MonthlySortOrderOptions
            };
        }

        /// <summary>
        /// 根据统计类型获取图表类型选项
        /// </summary>
        private ChartType[] GetChartTypeOptions(StatisticType type)
        {
            return type switch
            {
                StatisticType.MonthlyTripStats => MonthlyChartTypeOptions,
                StatisticType.TrainTypeRatio or StatisticType.SeatTypeRatio => RatioChartTypeOptions,
                StatisticType.StationTopRanking or StatisticType.PopularRouteStats => TopChartTypeOptions,
                StatisticType.AnnualTripSummary => AnnualChartTypeOptions,
                StatisticType.TripTimeDistribution => TimeDistributionChartTypeOptions,
                StatisticType.TripCostAnalysis => CostAnalysisChartTypeOptions,
                _ => MonthlyChartTypeOptions
            };
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        [RelayCommand]
        private void SaveConfig()
        {
            // 配置已自动更新到Config对象
            // 关闭窗口并返回成功
            CloseWindow(true);
        }

        /// <summary>
        /// 恢复默认
        /// </summary>
        [RelayCommand]
        private void RestoreDefaults()
        {
            var result = MessageBoxWindow.Show(Application.Current.MainWindow, "确定要恢复默认配置吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            // 根据统计类型创建新的默认配置
            Config = Config.StatisticType switch
            {
                StatisticType.TrainTypeRatio => new TrainTypeRatioConfig(),
                StatisticType.MonthlyTripStats => new MonthlyTripStatsConfig(),
                StatisticType.StationTopRanking => new StationTopRankingConfig(),
                StatisticType.SeatTypeRatio => new SeatTypeRatioConfig(),
                StatisticType.AnnualTripSummary => new AnnualTripSummaryConfig(),
                StatisticType.TripTimeDistribution => new TripTimeDistributionConfig(),
                StatisticType.PopularRouteStats => new PopularRouteStatsConfig(),
                StatisticType.TripCostAnalysis => new TripCostAnalysisConfig(),
                _ => new StatisticCardConfig()
            };

            WindowTitle = $"{Config.CardIcon} {Config.CardName} - 统计配置";
        }

        /// <summary>
        /// 取消
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            CloseWindow(false);
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        private void CloseWindow(bool dialogResult)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = dialogResult;
                    window.Close();
                    break;
                }
            }
        }
    }
}
