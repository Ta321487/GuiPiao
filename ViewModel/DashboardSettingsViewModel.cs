using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

/// <summary>
///     仪表盘设置视图模型
/// </summary>
public partial class DashboardSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly DashboardSettingsService _settingsService;

    /// <summary>
    ///     最大允许的卡片数量
    /// </summary>
    public const int MaxCards = 20;

    #region 可用统计项

    [ObservableProperty] private ObservableCollection<AvailableStatisticItem> _availableStatistics = new();

    #endregion

    #region 已添加的卡片

    [ObservableProperty] private ObservableCollection<DashboardCard> _cards = new();

    #endregion

    private bool _isLoadingConfig;
    private DashboardConfig _originalConfig;

    public DashboardSettingsViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

        _settingsService = new DashboardSettingsService();
        InitializeAvailableStatistics();
        LoadConfig();
    }

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (_isLoadingConfig || _originalConfig == null)
                return false;

            var currentConfig = GetCurrentConfig();
            return !ConfigsEqual(_originalConfig, currentConfig);
        }
    }

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    public void ReloadSettings()
    {
        LoadConfig();
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    /// <summary>
    ///     仪表盘配置已保存事件
    /// </summary>
    public static event EventHandler? DashboardConfigSaved;

    /// <summary>
    ///     统计数据刷新事件
    /// </summary>
    public static event EventHandler? StatisticsRefreshRequested;

    /// <summary>
    ///     统计缓存清除事件
    /// </summary>
    public static event EventHandler? StatisticsCacheClearRequested;

    /// <summary>
    ///     初始化可用统计项
    /// </summary>
    private void InitializeAvailableStatistics()
    {
        AvailableStatistics.Clear();
        foreach (var kvp in StatisticTypeInfo.Info)
            AvailableStatistics.Add(new AvailableStatisticItem
            {
                Type = kvp.Key,
                Name = kvp.Value.Name,
                Description = kvp.Value.Description,
                Icon = kvp.Value.Icon
            });
    }

    /// <summary>
    ///     加载配置到视图模型
    /// </summary>
    private void LoadConfig()
    {
        _isLoadingConfig = true;
        try
        {
            var loadedConfig = _settingsService.Config;

            LayoutType = loadedConfig.LayoutType;
            CardSpacing = loadedConfig.CardSpacing;
            GlobalTimeRange = loadedConfig.GlobalTimeRange;
            GlobalCustomStartDate = loadedConfig.GlobalCustomStartDate;
            GlobalCustomEndDate = loadedConfig.GlobalCustomEndDate;
            GlobalChartType = loadedConfig.GlobalChartType;
            ExcludeRefundedTickets = loadedConfig.ExcludeRefundedTickets;
            ExcludeDuplicateTickets = loadedConfig.ExcludeDuplicateTickets;
            AutoRefresh = loadedConfig.AutoRefresh;
            EnableChartAnimation = loadedConfig.EnableChartAnimation;

            Cards.Clear();
            foreach (var card in loadedConfig.Cards.OrderBy(c => c.SortOrder))
            {
                if (card.CustomConfig != null)
                {
                    if (card.CustomConfig.UseCustomChartType)
                        card.ChartType = card.CustomConfig.ChartType;
                    else
                        card.ChartType = GlobalChartType;
                }

                Cards.Add(card);
            }

            _originalConfig = GetCurrentConfig();
        }
        finally
        {
            _isLoadingConfig = false;
        }
    }

    /// <summary>
    ///     从视图模型获取当前配置
    /// </summary>
    private DashboardConfig GetCurrentConfig()
    {
        var config = new DashboardConfig
        {
            LayoutType = LayoutType,
            CardSpacing = CardSpacing,
            GlobalTimeRange = GlobalTimeRange,
            GlobalCustomStartDate = GlobalCustomStartDate,
            GlobalCustomEndDate = GlobalCustomEndDate,
            GlobalChartType = GlobalChartType,
            ExcludeRefundedTickets = ExcludeRefundedTickets,
            ExcludeDuplicateTickets = ExcludeDuplicateTickets,
            AutoRefresh = AutoRefresh,
            EnableChartAnimation = EnableChartAnimation
        };

        foreach (var card in Cards) config.Cards.Add(CloneCard(card));

        return config;
    }

    private static DashboardCard CloneCard(DashboardCard source)
    {
        return new DashboardCard
        {
            Id = source.Id,
            Name = source.Name,
            StatisticType = source.StatisticType,
            ChartType = source.ChartType,
            TimeRange = source.TimeRange,
            SortOrder = source.SortOrder,
            UseGlobalConfig = source.UseGlobalConfig,
            CustomConfig = source.CustomConfig != null ? CloneCustomConfig(source.CustomConfig) : null,
            CustomStartDate = source.CustomStartDate,
            CustomEndDate = source.CustomEndDate,
            GridRow = source.GridRow,
            GridColumn = source.GridColumn,
            GridRowSpan = source.GridRowSpan,
            GridColumnSpan = source.GridColumnSpan
        };
    }

    private static StatisticCardConfig CloneCustomConfig(StatisticCardConfig source)
    {
        return new StatisticCardConfig
        {
            StatisticType = source.StatisticType,
            CardName = source.CardName,
            CardIcon = source.CardIcon,
            TimeRange = source.TimeRange,
            CustomStartDate = source.CustomStartDate,
            CustomEndDate = source.CustomEndDate,
            ClassificationBasis = source.ClassificationBasis,
            StatisticIndicator = source.StatisticIndicator,
            DisplayThreshold = source.DisplayThreshold,
            TimeGranularity = source.TimeGranularity,
            SortOrder = source.SortOrder,
            TopCount = source.TopCount,
            CustomTimePeriods = source.CustomTimePeriods?.ToList() ?? new List<CustomTimePeriod>(),
            UseCustomChartType = source.UseCustomChartType,
            ChartType = source.ChartType,
            ChartColor = source.ChartColor,
            ShowPercentage = source.ShowPercentage,
            ShowValue = source.ShowValue,
            ShowTooltip = source.ShowTooltip,
            ShowValueLabel = source.ShowValueLabel,
            ShowTrendLine = source.ShowTrendLine,
            UseCustomFilter = source.UseCustomFilter,
            ExcludeRefundedTickets = source.ExcludeRefundedTickets,
            ExcludeDuplicateTickets = source.ExcludeDuplicateTickets
        };
    }

    /// <summary>
    ///     比较两个配置是否相等
    /// </summary>
    private bool ConfigsEqual(DashboardConfig a, DashboardConfig b)
    {
        if (a.LayoutType != b.LayoutType) return false;
        if (a.CardSpacing != b.CardSpacing) return false;
        if (a.GlobalTimeRange != b.GlobalTimeRange) return false;
        if (a.GlobalCustomStartDate != b.GlobalCustomStartDate) return false;
        if (a.GlobalCustomEndDate != b.GlobalCustomEndDate) return false;
        if (a.GlobalChartType != b.GlobalChartType) return false;
        if (a.ExcludeRefundedTickets != b.ExcludeRefundedTickets) return false;
        if (a.ExcludeDuplicateTickets != b.ExcludeDuplicateTickets) return false;
        if (a.AutoRefresh != b.AutoRefresh) return false;
        if (a.EnableChartAnimation != b.EnableChartAnimation) return false;

        if (a.Cards.Count != b.Cards.Count) return false;

        // 按 SortOrder 排序后比较，避免顺序不同导致误判
        var sortedCardsA = a.Cards.OrderBy(c => c.SortOrder).ToList();
        var sortedCardsB = b.Cards.OrderBy(c => c.SortOrder).ToList();

        for (var i = 0; i < sortedCardsA.Count; i++)
        {
            var cardA = sortedCardsA[i];
            var cardB = sortedCardsB[i];
            if (cardA.Id != cardB.Id) return false;
            if (cardA.Name != cardB.Name) return false;
            if (cardA.StatisticType != cardB.StatisticType) return false;
            if (cardA.ChartType != cardB.ChartType) return false;
            if (cardA.TimeRange != cardB.TimeRange) return false;
            if (cardA.SortOrder != cardB.SortOrder) return false;
            if (cardA.UseGlobalConfig != cardB.UseGlobalConfig) return false;
            if (cardA.CustomStartDate != cardB.CustomStartDate) return false;
            if (cardA.CustomEndDate != cardB.CustomEndDate) return false;
            if (cardA.GridRow != cardB.GridRow) return false;
            if (cardA.GridColumn != cardB.GridColumn) return false;
            if (cardA.GridRowSpan != cardB.GridRowSpan) return false;
            if (cardA.GridColumnSpan != cardB.GridColumnSpan) return false;
            if (!CustomConfigsEqual(cardA.CustomConfig, cardB.CustomConfig)) return false;
        }

        return true;
    }

    private static bool CustomConfigsEqual(StatisticCardConfig? a, StatisticCardConfig? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.StatisticType != b.StatisticType) return false;
        if (a.CardName != b.CardName) return false;
        if (a.CardIcon != b.CardIcon) return false;
        if (a.TimeRange != b.TimeRange) return false;
        if (a.CustomStartDate != b.CustomStartDate) return false;
        if (a.CustomEndDate != b.CustomEndDate) return false;
        if (a.ClassificationBasis != b.ClassificationBasis) return false;
        if (a.StatisticIndicator != b.StatisticIndicator) return false;
        if (a.DisplayThreshold != b.DisplayThreshold) return false;
        if (a.TimeGranularity != b.TimeGranularity) return false;
        if (a.SortOrder != b.SortOrder) return false;
        if (a.TopCount != b.TopCount) return false;
        if (a.UseCustomChartType != b.UseCustomChartType) return false;
        if (a.ChartType != b.ChartType) return false;
        if (a.ChartColor != b.ChartColor) return false;
        if (a.ShowPercentage != b.ShowPercentage) return false;
        if (a.ShowValue != b.ShowValue) return false;
        if (a.ShowTooltip != b.ShowTooltip) return false;
        if (a.ShowValueLabel != b.ShowValueLabel) return false;
        if (a.ShowTrendLine != b.ShowTrendLine) return false;
        if (a.UseCustomFilter != b.UseCustomFilter) return false;
        if (a.ExcludeRefundedTickets != b.ExcludeRefundedTickets) return false;
        if (a.ExcludeDuplicateTickets != b.ExcludeDuplicateTickets) return false;
        return true;
    }

    /// <summary>
    ///     是否可以添加新卡片
    /// </summary>
    public bool CanAddCard => Cards.Count < MaxCards;

    /// <summary>
    ///     当前卡片数量提示文本
    /// </summary>
    public string CardCountText => $"{Cards.Count}/{MaxCards}";

    /// <summary>
    ///     添加统计项到仪表盘
    /// </summary>
    [RelayCommand]
    private void AddStatistic(StatisticType statisticType)
    {
        // 检查是否已达到最大卡片数限制
        if (Cards.Count >= MaxCards)
        {
            MessageBoxWindow.Show(
                Application.Current.MainWindow,
                $"仪表盘最多只能添加 {MaxCards} 个卡片，请先删除部分卡片后再添加。",
                "添加失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var info = StatisticTypeInfo.Info[statisticType];
        var card = new DashboardCard
        {
            StatisticType = statisticType,
            Name = info.Name,
            ChartType = GlobalChartType,
            TimeRange = GlobalTimeRange,
            SortOrder = Cards.Count,
            UseGlobalConfig = true
        };

        // 检查是否已存在相同配置的卡片
        if (IsDuplicateCard(card))
        {
            var result = MessageBoxWindow.Show(
                Application.Current.MainWindow,
                $"您已添加过相同配置的『{card.Name}』图表，确定要重复添加吗？",
                "重复配置确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        Cards.Add(card);
        OnPropertyChanged(nameof(CanAddCard));
        OnPropertyChanged(nameof(CardCountText));
    }

    /// <summary>
    ///     检查是否存在相同配置的卡片
    /// </summary>
    private bool IsDuplicateCard(DashboardCard newCard)
    {
        return Cards.Any(existingCard =>
            // 统计类型相同
            existingCard.StatisticType == newCard.StatisticType &&
            // 图表类型相同
            existingCard.ChartType == newCard.ChartType &&
            // 时间范围相同
            existingCard.TimeRange == newCard.TimeRange &&
            // 都使用全局配置或都不使用
            existingCard.UseGlobalConfig == newCard.UseGlobalConfig &&
            // 自定义配置相同（如果存在）
            AreCustomConfigsEqual(existingCard.CustomConfig, newCard.CustomConfig));
    }

    /// <summary>
    ///     比较两个自定义配置是否相同
    /// </summary>
    private bool AreCustomConfigsEqual(StatisticCardConfig? config1, StatisticCardConfig? config2)
    {
        // 如果都是 null，认为相同
        if (config1 == null && config2 == null)
            return true;

        // 如果一个是 null 一个不是，认为不同
        if (config1 == null || config2 == null)
            return false;

        // 比较关键配置项
        return config1.StatisticType == config2.StatisticType &&
               config1.ClassificationBasis == config2.ClassificationBasis &&
               config1.StatisticIndicator == config2.StatisticIndicator &&
               config1.ChartType == config2.ChartType &&
               config1.TimeRange == config2.TimeRange;
    }

    /// <summary>
    ///     删除卡片
    /// </summary>
    [RelayCommand]
    private void RemoveCard(DashboardCard card)
    {
        if (card != null && Cards.Contains(card))
        {
            Cards.Remove(card);
            // 重新排序
            for (var i = 0; i < Cards.Count; i++) Cards[i].SortOrder = i;
            OnPropertyChanged(nameof(CanAddCard));
            OnPropertyChanged(nameof(CardCountText));
        }
    }

    /// <summary>
    ///     配置卡片
    /// </summary>
    [RelayCommand]
    private void ConfigureCard(DashboardCard card)
    {
        if (card == null) return;

        // 如果卡片已有自定义配置，则使用；否则创建新的默认配置
        StatisticCardConfig config;
        if (card.CustomConfig != null)
        {
            config = card.CustomConfig;
            // 同步 UseCustomFilter 状态：如果卡片使用自定义配置，则设置 UseCustomFilter = true
            config.UseCustomFilter = !card.UseGlobalConfig;
        }
        else
        {
            config = card.StatisticType switch
            {
                StatisticType.TrainTypeRatio => new TrainTypeRatioConfig(),
                StatisticType.MonthlyTripStats => new MonthlyTripStatsConfig(),
                StatisticType.StationTopRanking => new StationTopRankingConfig(),
                StatisticType.SeatTypeRatio => new SeatTypeRatioConfig(),
                StatisticType.AnnualTripSummary => new AnnualTripSummaryConfig(),
                StatisticType.TripTimeDistribution => new TripTimeDistributionConfig(),
                StatisticType.PopularRouteStats => new PopularRouteStatsConfig(),
                StatisticType.TripCostAnalysis => new TripCostAnalysisConfig(),
                _ => new StatisticCardConfig { StatisticType = card.StatisticType, CardName = card.Name }
            };
            // 新创建的配置，根据卡片设置 UseCustomFilter
            config.UseCustomFilter = !card.UseGlobalConfig;
        }

        // 如果卡片使用全局配置，将时间范围设置为全局值
        // 注意：图表类型由 UseCustomChartType 独立控制，ShowValue、ShowPercentage、ShowTooltip 保持卡片原有值
        if (card.UseGlobalConfig)
        {
            // 时间范围跟随全局
            config.TimeRange = GlobalTimeRange switch
            {
                TimeRangeType.Last3Months => "近 3 个月",
                TimeRangeType.Last6Months => "近 6 个月",
                TimeRangeType.Last12Months => "近 12 个月",
                TimeRangeType.CalendarYear => "自然年",
                TimeRangeType.CustomRange => "自定义时间段",
                _ => "近 3 个月"
            };
            // 同步全局自定义时间段日期
            config.CustomStartDate = GlobalCustomStartDate;
            config.CustomEndDate = GlobalCustomEndDate;

            // 图表类型：如果未启用自定义图表类型，则跟随全局
            if (!config.UseCustomChartType) config.ChartType = GlobalChartType;
            // 如果启用了自定义图表类型，保持 config.ChartType 不变（使用已保存的自定义值）
        }

        // 获取当前全局配置
        var globalConfig = new DashboardConfig
        {
            GlobalTimeRange = GlobalTimeRange,
            GlobalChartType = GlobalChartType,
            ExcludeRefundedTickets = ExcludeRefundedTickets,
            ExcludeDuplicateTickets = ExcludeDuplicateTickets
        };

        var viewModel = new StatisticCardConfigViewModel(config, globalConfig);
        var window = new StatisticCardConfigWindow(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        var result = window.ShowDialog();
        if (result == true)
        {
            // 保存配置到卡片
            card.CustomConfig = viewModel.Config;
            // 根据是否使用自定义过滤规则决定是否使用全局配置（仅针对时间范围和数据过滤）
            card.UseGlobalConfig = !viewModel.Config.UseCustomFilter;

            // 处理时间范围（UseGlobalConfig 控制）
            if (card.UseGlobalConfig)
            {
                // 跟随全局时，同步全局时间范围
                card.TimeRange = GlobalTimeRange;
                card.CustomStartDate = GlobalCustomStartDate;
                card.CustomEndDate = GlobalCustomEndDate;
            }
            else
            {
                // 自定义时间范围
                card.TimeRange = viewModel.Config.TimeRange switch
                {
                    "近 3 个月" => TimeRangeType.Last3Months,
                    "近 6 个月" => TimeRangeType.Last6Months,
                    "近 12 个月" => TimeRangeType.Last12Months,
                    "自然年" => TimeRangeType.CalendarYear,
                    "自定义时间段" => TimeRangeType.CustomRange,
                    _ => GlobalTimeRange
                };
                card.CustomStartDate = viewModel.Config.CustomStartDate;
                card.CustomEndDate = viewModel.Config.CustomEndDate;
            }

            // 处理图表类型（UseCustomChartType 独立控制）
            if (viewModel.Config.UseCustomChartType)
                // 使用自定义图表类型
                card.ChartType = viewModel.Config.ChartType;
            else
                // 跟随全局图表类型
                card.ChartType = GlobalChartType;
        }
    }

    /// <summary>
    ///     立即刷新所有统计数据
    /// </summary>
    [RelayCommand]
    private void RefreshAllStatistics()
    {
        // 触发刷新事件，由 DashboardViewModel 统一处理刷新逻辑和进度显示
        StatisticsRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     清除统计缓存
    /// </summary>
    [RelayCommand]
    private void ClearStatisticsCache()
    {
        var result = MessageBoxWindow.Show(
            Application.Current.MainWindow,
            "确定要清除统计缓存吗？这将清空所有统计数据。",
            "确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            // 触发清除缓存事件，通知 MainViewModel 清除缓存
            StatisticsCacheClearRequested?.Invoke(this, EventArgs.Empty);

            MessageBoxWindow.Show(
                Application.Current.MainWindow,
                "统计缓存已清除，请重新加载数据。",
                "成功");
        }
    }

    /// <summary>
    ///     保存设置命令
    /// </summary>
    [RelayCommand]
    private async Task SaveSettings()
    {
        await SaveSettingsInternalAsync(true);
    }

    /// <summary>
    ///     保存设置内部实现
    /// </summary>
    private async Task SaveSettingsInternalAsync(bool showMessage)
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            var config = GetCurrentConfig();
            _settingsService.SaveConfig(config);

            foreach (var card in Cards)
                if (card.UseGlobalConfig)
                {
                    card.ChartType = GlobalChartType;
                    card.TimeRange = GlobalTimeRange;
                    card.CustomStartDate = GlobalCustomStartDate;
                    card.CustomEndDate = GlobalCustomEndDate;
                }

            _originalConfig = GetCurrentConfig();

            // 触发配置已保存事件
            Debug.WriteLine("[DashboardSettingsViewModel] 触发 DashboardConfigSaved 事件");
            DashboardConfigSaved?.Invoke(this, EventArgs.Empty);

            if (showMessage)
                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxWindow.Show(settingsWindow, "仪表盘配置已保存", "成功");
                    });
                });
        }
        catch (Exception ex)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxWindow.Show(settingsWindow, $"保存失败：{ex.Message}", "错误", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            });
        }
    }

    /// <summary>
    ///     恢复默认设置命令
    /// </summary>
    [RelayCommand]
    private async Task RestoreDefaults()
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        var result = MessageBoxWindow.Show(settingsWindow, "确定要恢复默认设置吗？", "确认", MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        var defaultConfig = _settingsService.GetDefaultConfig();

        LayoutType = defaultConfig.LayoutType;
        CardSpacing = defaultConfig.CardSpacing;
        GlobalTimeRange = defaultConfig.GlobalTimeRange;
        GlobalChartType = defaultConfig.GlobalChartType;
        ExcludeRefundedTickets = defaultConfig.ExcludeRefundedTickets;
        ExcludeDuplicateTickets = defaultConfig.ExcludeDuplicateTickets;
        AutoRefresh = defaultConfig.AutoRefresh;

        Cards.Clear();
        foreach (var card in defaultConfig.Cards) Cards.Add(card);

        await Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBoxWindow.Show(settingsWindow, "已恢复默认设置，请点击保存配置按钮保存更改。");
            });
        });
    }

    /// <summary>
    ///     全局时间范围变化时，更新所有使用全局配置的卡片
    /// </summary>
    partial void OnGlobalTimeRangeChanged(TimeRangeType value)
    {
        if (_isLoadingConfig) return;

        foreach (var card in Cards)
            if (card.UseGlobalConfig)
                card.TimeRange = value;
    }

    /// <summary>
    ///     全局图表类型变化时，更新所有使用全局配置的卡片
    /// </summary>
    partial void OnGlobalChartTypeChanged(ChartType value)
    {
        if (_isLoadingConfig) return;

        foreach (var card in Cards)
            if (card.UseGlobalConfig)
                card.ChartType = value;
    }

    #region 全局配置

    [ObservableProperty] private LayoutType _layoutType = LayoutType.ThreeColumn;

    private int _cardSpacing = 10;

    /// <summary>
    ///     卡片间距（限制范围 0-100）
    /// </summary>
    public int CardSpacing
    {
        get => _cardSpacing;
        set
        {
            // 限制范围 0-100
            var clampedValue = Math.Clamp(value, 0, 100);
            if (_cardSpacing != clampedValue)
            {
                _cardSpacing = clampedValue;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty] private TimeRangeType _globalTimeRange = TimeRangeType.Last12Months;

    [ObservableProperty] private DateTime? _globalCustomStartDate;

    [ObservableProperty] private DateTime? _globalCustomEndDate;

    [ObservableProperty] private ChartType _globalChartType = ChartType.BarChart;

    [ObservableProperty] private bool _excludeRefundedTickets = true;

    [ObservableProperty] private bool _excludeDuplicateTickets = true;

    [ObservableProperty] private AutoRefreshType _autoRefresh = AutoRefreshType.Off;

    [ObservableProperty] private bool _enableChartAnimation = true;

    #endregion

    #region 下拉选项

    public LayoutType[] LayoutTypes => (LayoutType[])Enum.GetValues(typeof(LayoutType));
    public TimeRangeType[] TimeRangeTypes => (TimeRangeType[])Enum.GetValues(typeof(TimeRangeType));
    public ChartType[] ChartTypes => (ChartType[])Enum.GetValues(typeof(ChartType));
    public AutoRefreshType[] AutoRefreshTypes => (AutoRefreshType[])Enum.GetValues(typeof(AutoRefreshType));

    #endregion
}