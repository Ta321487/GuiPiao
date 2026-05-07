using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;
using Timer = System.Timers.Timer;

namespace GuiPiao.ViewModel;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IChartDataService _chartDataService;

    private readonly DashboardSettingsService _dashboardSettingsService;
    private readonly SemaphoreSlim _loadChartsLock = new(1, 1);

    [ObservableProperty] private ObservableCollection<DashboardChartViewModel> _dashboardCharts = new();

    [ObservableProperty] private int _dashboardColumns = 2;

    [ObservableProperty] private DashboardChartViewModel? _fullscreenChart;

    [ObservableProperty] private int _fullscreenChartIndex;

    private bool _isDisposed;

    [ObservableProperty] private bool _isFullscreenMode;

    private bool _isInitializingDashboard;
    private Timer? _weeklyRefreshTimer;

    public DashboardViewModel()
    {
        _dashboardSettingsService = new DashboardSettingsService();
        _chartDataService = new ChartDataService();

        // 订阅仪表盘配置保存事件
        DashboardSettingsViewModel.DashboardConfigSaved += OnDashboardConfigSaved;
        _dashboardSettingsService.ConfigSaved += OnDashboardConfigSavedFromService;

        // 订阅统计数据刷新事件
        DashboardSettingsViewModel.StatisticsRefreshRequested += OnStatisticsRefreshRequested;
        DashboardSettingsViewModel.StatisticsCacheClearRequested += OnStatisticsCacheClearRequested;

        // 初始化仪表盘
        _ = InitializeDashboardAsync();

        // 应用自动刷新策略
        ApplyAutoRefreshStrategy();

        // 如果配置为 OnStartup，在启动时自动刷新数据
        if (_dashboardSettingsService.Config.AutoRefresh == AutoRefreshType.OnStartup)
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                await RefreshDashboardDataAsync();
            });

        // 订阅打开统计配置消息
        WeakReferenceMessenger.Default.Register<OpenStatisticsConfigMessage>(this, (recipient, message) =>
        {
            Debug.WriteLine("[OpenStatisticsConfigMessage] Received");
            Application.Current.Dispatcher.Invoke(() => { StatisticsConfigCommand(); });
        });

        // 订阅刷新统计数据消息
        WeakReferenceMessenger.Default.Register<RefreshStatisticsMessage>(this, async (recipient, message) =>
        {
            Debug.WriteLine("[RefreshStatisticsMessage] Received");
            await RefreshStatisticsCommand();
        });
    }

    public bool HasDashboardCharts => DashboardCharts.Count > 0;

    public bool CanNavigatePrevious => IsFullscreenMode && FullscreenChartIndex > 0;

    public bool CanNavigateNext => IsFullscreenMode && FullscreenChartIndex < DashboardCharts.Count - 1;

    public string FullscreenIndicator => $"{FullscreenChartIndex + 1} / {DashboardCharts.Count}";

    public DashboardConfig DashboardConfig => _dashboardSettingsService.Config;

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        DashboardSettingsViewModel.DashboardConfigSaved -= OnDashboardConfigSaved;
        _dashboardSettingsService.ConfigSaved -= OnDashboardConfigSavedFromService;
        DashboardSettingsViewModel.StatisticsRefreshRequested -= OnStatisticsRefreshRequested;
        DashboardSettingsViewModel.StatisticsCacheClearRequested -= OnStatisticsCacheClearRequested;

        _weeklyRefreshTimer?.Stop();
        _weeklyRefreshTimer?.Dispose();
        _loadChartsLock?.Dispose();

        foreach (var chart in DashboardCharts) chart.Dispose();
        DashboardCharts.Clear();

        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private async Task InitializeDashboardAsync()
    {
        if (_isInitializingDashboard)
        {
            Debug.WriteLine("[InitializeDashboardAsync] 正在初始化中，跳过重复调用");
            return;
        }

        _isInitializingDashboard = true;
        try
        {
            var config = _dashboardSettingsService.Config;

            // 安全检查：如果卡片数量超过限制，只加载前20个
            const int maxCards = 20;
            if (config.Cards != null && config.Cards.Count > maxCards)
            {
                Debug.WriteLine($"[InitializeDashboardAsync] 警告：卡片数量({config.Cards.Count})超过限制({maxCards})，只加载前{maxCards}个");
                // 创建新的配置副本，只包含前20个卡片
                var limitedCards = config.Cards.Take(maxCards).ToList();
                config.Cards.Clear();
                foreach (var card in limitedCards) config.Cards.Add(card);
            }

            DashboardColumns = DashboardLayoutManager.GetRequiredColumns(config.LayoutType);

            if (config.Cards != null && config.Cards.Count > 0)
                DashboardLayoutManager.ApplyLayout(config.Cards, config.LayoutType);

            await LoadDashboardChartsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[InitializeDashboardAsync] 初始化仪表盘失败: {ex.Message}");
        }
        finally
        {
            _isInitializingDashboard = false;
        }
    }

    private async Task LoadDashboardChartsAsync()
    {
        // 使用锁防止并发调用导致重复添加图表
        await _loadChartsLock.WaitAsync();
        try
        {
            var config = _dashboardSettingsService.Config;

            // 如果处于全屏模式，检查当前全屏的卡片是否还在新的配置中
            if (IsFullscreenMode && FullscreenChart != null)
            {
                var currentCardId = FullscreenChart.Card.Id;
                var cardStillExists = config.Cards?.Any(c => c.Id == currentCardId) ?? false;

                if (!cardStillExists)
                {
                    Debug.WriteLine($"[LoadDashboardChartsAsync] 当前全屏的卡片 {currentCardId} 已被删除，退出全屏模式");
                    ExitFullscreen();
                }
            }

            // 使用 Dispatcher 确保在 UI 线程上更新集合
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var oldChart in DashboardCharts) oldChart.Dispose();
                DashboardCharts.Clear();
            });

            Debug.WriteLine($"[LoadDashboardChartsAsync] Cards count: {config.Cards?.Count ?? 0}");

            if (config.Cards == null || config.Cards.Count == 0)
            {
                Debug.WriteLine("[LoadDashboardChartsAsync] 没有配置卡片，不显示图表");
                OnPropertyChanged(nameof(HasDashboardCharts));
                return;
            }

            var chartViewModels = new List<DashboardChartViewModel>();
            foreach (var card in config.Cards.OrderBy(c => c.SortOrder))
            {
                Debug.WriteLine($"[LoadDashboardChartsAsync] 创建图表: {card.Name}");
                var chartVm = new DashboardChartViewModel(card, _chartDataService);
                chartViewModels.Add(chartVm);
            }

            // 使用 Dispatcher 确保在 UI 线程上更新集合
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var chartVm in chartViewModels) DashboardCharts.Add(chartVm);
                OnPropertyChanged(nameof(HasDashboardCharts));
            });

            Debug.WriteLine($"[LoadDashboardChartsAsync] 开始并行加载 {chartViewModels.Count} 个图表的数据");
            var loadDataTasks = chartViewModels.Select(async chartVm =>
            {
                try
                {
                    await chartVm.LoadDataAsync();
                    Debug.WriteLine("[LoadDashboardChartsAsync] 图表数据加载完成");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LoadDashboardChartsAsync] 图表数据加载失败: {ex.Message}");
                }
            }).ToArray();

            await Task.WhenAll(loadDataTasks);

            Debug.WriteLine($"[LoadDashboardChartsAsync] 总共加载了 {DashboardCharts.Count} 个图表");

            // 如果处于全屏模式，更新全屏图表引用到新的实例
            if (IsFullscreenMode && FullscreenChartIndex >= 0 && FullscreenChartIndex < DashboardCharts.Count)
            {
                FullscreenChart = DashboardCharts[FullscreenChartIndex];
                Debug.WriteLine($"[LoadDashboardChartsAsync] 全屏模式已更新图表引用: {FullscreenChart.Title}");
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(DashboardCharts));
                    Debug.WriteLine("[LoadDashboardChartsAsync] 已触发图表重绘");
                });
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoadDashboardChartsAsync] 加载图表失败: {ex.Message}");
            Debug.WriteLine($"[LoadDashboardChartsAsync] 异常详情: {ex}");
        }
        finally
        {
            _loadChartsLock.Release();
        }
    }

    [RelayCommand]
    public void StatisticsConfigCommand()
    {
        Debug.WriteLine("[DashboardViewModel] StatisticsConfigCommand 被调用");
        var settingsWindow = new SettingsWindow(SettingsPageType.Dashboard);
        Debug.WriteLine("[DashboardViewModel] 打开 SettingsWindow");
        var result = settingsWindow.ShowDialog();
        Debug.WriteLine($"[DashboardViewModel] SettingsWindow 关闭，DialogResult: {result}");

        if (result == true) _ = InitializeDashboardAsync();
    }

    [RelayCommand]
    public async Task RefreshStatisticsCommand()
    {
        MessageBoxWindow? progressWindow = null;

        try
        {
            // 显示进度对话框
            progressWindow = MessageBoxWindow.ShowProgress("正在刷新统计数据，请稍候...", "刷新统计");

            // 等待窗口渲染
            await Task.Delay(100);

            // 执行刷新
            await RefreshChartDataAndReloadAsync(true);

            // 关闭进度对话框
            progressWindow?.Close();
            progressWindow = null;

            Debug.WriteLine("[DashboardViewModel] 统计刷新完成");

            // 显示完成提示
            MessageBoxWindow.Show(
                Application.Current.MainWindow,
                "统计数据刷新完成！",
                "完成");
        }
        catch (Exception ex)
        {
            // 确保关闭进度对话框
            progressWindow?.Close();
            progressWindow = null;

            Debug.WriteLine($"[DashboardViewModel] 统计刷新失败: {ex.Message}");

            MessageBoxWindow.Show(
                Application.Current.MainWindow,
                $"刷新统计数据时发生错误:\n{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task RefreshDashboardDataAsync()
    {
        Debug.WriteLine("[DashboardViewModel] 自动刷新仪表盘数据");
        await RefreshChartDataAndReloadAsync(false);
        Debug.WriteLine("[DashboardViewModel] 仪表盘数据刷新完成");
    }

    private async Task RefreshChartDataAndReloadAsync(bool reloadConfig)
    {
        // 触发数据刷新（通知所有图表重新加载数据）
        _chartDataService.RefreshData();

        if (reloadConfig)
        {
            _dashboardSettingsService.RefreshConfig();
            await InitializeDashboardAsync();
        }
        else
        {
            await LoadDashboardChartsAsync();
        }
    }

    private void SetupWeeklyRefreshTimer()
    {
        var now = DateTime.Now;
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0 && now.Hour >= 0) daysUntilSunday = 7;
        var nextSunday = now.Date.AddDays(daysUntilSunday);
        var timeUntilNextSunday = nextSunday - now;

        Debug.WriteLine($"[DashboardViewModel] 设置每周自动刷新定时器，下次执行时间: {nextSunday}");

        _weeklyRefreshTimer?.Stop();
        _weeklyRefreshTimer?.Dispose();

        _weeklyRefreshTimer = new Timer(timeUntilNextSunday.TotalMilliseconds);
        _weeklyRefreshTimer.Elapsed += OnWeeklyRefreshTimerElapsed;
        _weeklyRefreshTimer.AutoReset = false;
        _weeklyRefreshTimer.Start();
    }

    private async void OnWeeklyRefreshTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            _weeklyRefreshTimer?.Stop();
            _weeklyRefreshTimer?.Dispose();

            Debug.WriteLine("[DashboardViewModel] 每周自动刷新触发");
            await RefreshDashboardDataAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardViewModel] 每周自动刷新失败: {ex.Message}");
        }
        finally
        {
            SetupWeeklyRefreshTimer();
        }
    }

    private void OnDashboardConfigSaved(object? sender, EventArgs e)
    {
        Debug.WriteLine("[DashboardViewModel] 收到 DashboardConfigSaved 事件");
        _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                Debug.WriteLine("[DashboardViewModel] 开始刷新仪表盘");
                _dashboardSettingsService.RefreshConfig();
                OnPropertyChanged(nameof(DashboardConfig));
                await InitializeDashboardAsync();

                // 重新应用自动刷新策略
                ApplyAutoRefreshStrategy();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DashboardViewModel] 刷新仪表盘失败: {ex.Message}");
            }
        });
    }

    /// <summary>
    ///     应用自动刷新策略
    /// </summary>
    private void ApplyAutoRefreshStrategy()
    {
        var autoRefreshType = _dashboardSettingsService.Config.AutoRefresh;
        Debug.WriteLine($"[DashboardViewModel] 应用自动刷新策略: {autoRefreshType}");

        // 停止现有的定时器
        _weeklyRefreshTimer?.Stop();
        _weeklyRefreshTimer?.Dispose();
        _weeklyRefreshTimer = null;

        switch (autoRefreshType)
        {
            case AutoRefreshType.Weekly:
                SetupWeeklyRefreshTimer();
                break;
            case AutoRefreshType.OnStartup:
                // OnStartup 只在程序启动时执行，不需要设置定时器
                Debug.WriteLine("[DashboardViewModel] 自动刷新策略为 OnStartup，下次启动时自动刷新");
                break;
            case AutoRefreshType.Off:
            default:
                Debug.WriteLine("[DashboardViewModel] 自动刷新已关闭");
                break;
        }
    }

    private void OnDashboardConfigSavedFromService(object? sender, EventArgs e)
    {
        Debug.WriteLine("[DashboardViewModel] 收到 ConfigSaved 事件");
        OnPropertyChanged(nameof(DashboardConfig));
    }

    private async void OnStatisticsRefreshRequested(object? sender, EventArgs e)
    {
        Debug.WriteLine("[DashboardViewModel] 收到 StatisticsRefreshRequested 事件");
        // 使用统一的刷新方法，带进度对话框
        await RefreshStatisticsCommand();
    }

    private void OnStatisticsCacheClearRequested(object? sender, EventArgs e)
    {
        Debug.WriteLine("[DashboardViewModel] 收到 StatisticsCacheClearRequested 事件");
        // 触发数据刷新事件，通知所有图表重新加载
        _chartDataService.ClearCache();
    }

    [RelayCommand]
    private void EnterFullscreen(DashboardChartViewModel chart)
    {
        var index = DashboardCharts.IndexOf(chart);
        if (index >= 0)
        {
            FullscreenChartIndex = index;
            FullscreenChart = chart;
            IsFullscreenMode = true;
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            OnPropertyChanged(nameof(FullscreenIndicator));
        }
    }

    [RelayCommand]
    private void ExitFullscreen()
    {
        IsFullscreenMode = false;
        FullscreenChart = null;
    }

    [RelayCommand]
    private void NavigateToPreviousChart()
    {
        if (FullscreenChartIndex > 0)
        {
            FullscreenChartIndex--;
            FullscreenChart = DashboardCharts[FullscreenChartIndex];
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            OnPropertyChanged(nameof(FullscreenIndicator));
        }
    }

    [RelayCommand]
    private void NavigateToNextChart()
    {
        if (FullscreenChartIndex < DashboardCharts.Count - 1)
        {
            FullscreenChartIndex++;
            FullscreenChart = DashboardCharts[FullscreenChartIndex];
            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanNavigateNext));
            OnPropertyChanged(nameof(FullscreenIndicator));
        }
    }
}