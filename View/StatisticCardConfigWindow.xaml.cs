using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using GuiPiao.Model;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
///     StatisticCardConfigWindow.xaml 的交互逻辑
/// </summary>
public partial class StatisticCardConfigWindow : Window
{
    private bool _isInitializing = true;

    public StatisticCardConfigWindow(StatisticCardConfigViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StatisticCardConfigViewModel viewModel)
        {
            UpdateComboBoxItemsSource(viewModel);
            InitializeCustomTimePeriodEditor(viewModel);
        }

        // 窗口加载完成后，标记初始化结束
        _isInitializing = false;
    }

    /// <summary>
    ///     初始化自定义时段编辑器
    /// </summary>
    private void InitializeCustomTimePeriodEditor(StatisticCardConfigViewModel viewModel)
    {
        if (viewModel.Config.StatisticType == StatisticType.TripTimeDistribution)
        {
            Debug.WriteLine(
                $"[InitializeCustomTimePeriodEditor] CustomTimePeriods.Count={viewModel.Config.CustomTimePeriods?.Count ?? 0}");

            // 加载已保存的自定义时段配置
            if (viewModel.Config.CustomTimePeriods?.Count > 0)
            {
                Debug.WriteLine("[InitializeCustomTimePeriodEditor] 加载已保存的时段");
                CustomTimePeriodEditor.SetPeriods(viewModel.Config.CustomTimePeriods);
            }
            else
            {
                Debug.WriteLine("[InitializeCustomTimePeriodEditor] 使用默认时段");
            }

            // 订阅时段变更事件
            CustomTimePeriodEditor.PeriodsChanged += (s, periods) =>
            {
                viewModel.Config.CustomTimePeriods = new List<CustomTimePeriod>(periods);
            };
        }
    }

    /// <summary>
    ///     根据统计类型更新ComboBox的ItemsSource
    /// </summary>
    private void UpdateComboBoxItemsSource(StatisticCardConfigViewModel viewModel)
    {
        var statisticType = viewModel.Config.StatisticType;

        // 更新统计指标下拉框
        StatisticIndicatorComboBox.ItemsSource = statisticType switch
        {
            StatisticType.MonthlyTripStats => viewModel.MonthlyStatisticIndicatorOptions,
            StatisticType.TrainTypeRatio => viewModel.RatioStatisticIndicatorOptions,
            StatisticType.SeatTypeRatio => viewModel.RatioStatisticIndicatorOptions,
            StatisticType.StationTopRanking => viewModel.TopStatisticIndicatorOptions,
            StatisticType.PopularRouteStats => viewModel.TopStatisticIndicatorOptions,
            StatisticType.AnnualTripSummary => viewModel.AnnualStatisticIndicatorOptions,
            StatisticType.TripTimeDistribution => viewModel.TimeDistributionStatisticIndicatorOptions,
            StatisticType.TripCostAnalysis => viewModel.CostAnalysisIndicatorOptions,
            _ => viewModel.MonthlyStatisticIndicatorOptions
        };

        // 更新排序方式下拉框
        SortOrderComboBox.ItemsSource = statisticType switch
        {
            StatisticType.MonthlyTripStats => viewModel.MonthlySortOrderOptions,
            StatisticType.StationTopRanking => viewModel.TopSortOrderOptions,
            StatisticType.PopularRouteStats => viewModel.TopSortOrderOptions,
            _ => viewModel.MonthlySortOrderOptions
        };

        // 图表类型下拉框的ItemsSource由XAML绑定处理，这里不再设置
        // 因为XAML中使用了Style触发器来动态切换ItemsSource
    }

    /// <summary>
    ///     时间范围下拉框关闭时处理自定义时间段
    /// </summary>
    private void TimeRangeComboBox_DropDownClosed(object sender, EventArgs e)
    {
        // 初始化期间不处理，避免窗口加载时触发
        if (_isInitializing) return;

        if (DataContext is not StatisticCardConfigViewModel viewModel) return;

        // 获取选中的项
        var selectedItem = TimeRangeComboBox.SelectedItem;
        if (selectedItem == null) return;

        var selectedText = selectedItem.ToString();
        if (selectedText != "自定义时间段") return;

        // 使用 Dispatcher 延迟打开对话框，确保下拉菜单先关闭
        Dispatcher.BeginInvoke(new Action(() => { OpenCustomTimeRangeDialog(); }), DispatcherPriority.Background);
    }

    /// <summary>
    ///     打开自定义时间范围对话框
    /// </summary>
    private void OpenCustomTimeRangeDialog()
    {
        if (DataContext is not StatisticCardConfigViewModel viewModel) return;

        // 打开自定义时间范围对话框，传递当前时间粒度以显示相应提示
        var window = new CustomTimeRangeWindow(
            viewModel.Config.CustomStartDate,
            viewModel.Config.CustomEndDate,
            viewModel.Config.TimeGranularity);
        // 不设置 Owner，避免最小化时影响主窗口

        var result = window.ShowDialog();
        if (result == true)
        {
            // 保存自定义时间范围
            viewModel.Config.CustomStartDate = window.SelectedStartDate;
            viewModel.Config.CustomEndDate = window.SelectedEndDate;
        }
        else
        {
            // 用户取消，恢复之前的选择
            // 如果之前不是自定义时间段，恢复为之前的选择
            if (viewModel.Config.TimeRange != "自定义时间段") TimeRangeComboBox.SelectedItem = viewModel.Config.TimeRange;
        }
    }
}