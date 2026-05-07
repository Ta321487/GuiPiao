using System;
using System.Windows;
using GuiPiao.DataAccess;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
///     CustomTimeRangeWindow.xaml 的交互逻辑
/// </summary>
public partial class CustomTimeRangeWindow : Window
{
    private readonly DateTime _defaultEndDate;

    // 默认时间范围：近12个月（与系统全局默认一致）
    private readonly DateTime _defaultStartDate;
    private readonly TrainRideRepository _trainRideRepository;
    private CustomTimeRangeViewModel _viewModel = null!;

    public CustomTimeRangeWindow(DateTime? startDate = null, DateTime? endDate = null, string timeGranularity = "")
    {
        InitializeComponent();
        _trainRideRepository = new TrainRideRepository();

        // 设置默认时间范围为近12个月
        _defaultEndDate = DateTime.Now.Date;
        _defaultStartDate = _defaultEndDate.AddMonths(-12);

        // 异步加载日期
        LoadDatesAsync(startDate, endDate, timeGranularity);
    }

    /// <summary>
    ///     获取选择的开始日期
    /// </summary>
    public DateTime? SelectedStartDate => _viewModel?.StartDate;

    /// <summary>
    ///     获取选择的结束日期
    /// </summary>
    public DateTime? SelectedEndDate => _viewModel?.EndDate;

    /// <summary>
    ///     异步加载日期
    /// </summary>
    private async void LoadDatesAsync(DateTime? startDate, DateTime? endDate, string timeGranularity)
    {
        try
        {
            // 从数据库获取日期范围
            var (earliest, latest) = await _trainRideRepository.GetTicketDateRangeAsync();

            // 如果传入的参数为null，则使用默认时间范围（近12个月）
            // 如果数据库中最早日期比近12个月还早，则使用近12个月
            var actualStartDate = startDate ?? _defaultStartDate;
            var actualEndDate = endDate ?? _defaultEndDate;

            _viewModel = new CustomTimeRangeViewModel
            {
                StartDate = actualStartDate,
                EndDate = actualEndDate,
                TimeGranularity = timeGranularity,
                DefaultStartDate = _defaultStartDate,
                DefaultEndDate = _defaultEndDate
            };

            DataContext = _viewModel;
        }
        catch
        {
            // 如果数据库查询失败，使用默认时间范围
            _viewModel = new CustomTimeRangeViewModel
            {
                StartDate = startDate ?? _defaultStartDate,
                EndDate = endDate ?? _defaultEndDate,
                TimeGranularity = timeGranularity,
                DefaultStartDate = _defaultStartDate,
                DefaultEndDate = _defaultEndDate
            };

            DataContext = _viewModel;
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        // 最终二次检查，确保万无一失
        if (!_viewModel.IsValid)
        {
            MessageBoxWindow.Show(this, _viewModel.ErrorMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        // 重置为默认时间范围（近12个月）
        _viewModel.StartDate = _defaultStartDate;
        _viewModel.EndDate = _defaultEndDate;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}