using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    private void SubscribeToDashboardChanges()
    {
        if (_dashboardPropertyChangedHandler != null)
            return;

        _dashboardPropertyChangedHandler = (s, e) =>
        {
            if (s is not DashboardViewModel d)
                return;

            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(DashboardViewModel.DashboardCharts):
                case nameof(DashboardViewModel.HasDashboardCharts):
                    OnPropertyChanged(nameof(DashboardCharts));
                    OnPropertyChanged(nameof(HasDashboardCharts));
                    TripMenuCommandCommand.NotifyCanExecuteChanged();
                    SetTemporaryStatus(d.HasDashboardCharts
                        ? $"仪表盘已加载 {d.DashboardCharts.Count} 个图表"
                        : "仪表盘暂无图表");
                    break;
                case nameof(DashboardViewModel.DashboardColumns):
                    OnPropertyChanged(nameof(DashboardColumns));
                    break;
                case nameof(DashboardViewModel.IsFullscreenMode):
                    OnPropertyChanged(nameof(IsFullscreenMode));
                    break;
                case nameof(DashboardViewModel.FullscreenChart):
                    OnPropertyChanged(nameof(FullscreenChart));
                    break;
                case nameof(DashboardViewModel.FullscreenChartIndex):
                    OnPropertyChanged(nameof(FullscreenChartIndex));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(FullscreenIndicator));
                    break;
            }
        };
    }

    #region 转发属性 - 仪表盘相关

    public ObservableCollection<DashboardChartViewModel> DashboardCharts =>
        _dashboard?.DashboardCharts ?? _emptyDashboardCharts;

    public int DashboardColumns => _dashboard?.DashboardColumns ?? 2;

    public bool HasDashboardCharts => _dashboard?.HasDashboardCharts ?? false;

    public DashboardConfig DashboardConfig =>
        _dashboard?.DashboardConfig ?? (_dashboardSettingsForLazyConfig ??= new DashboardSettingsService()).Config;

    public bool IsFullscreenMode => _dashboard?.IsFullscreenMode ?? false;

    public DashboardChartViewModel? FullscreenChart => _dashboard?.FullscreenChart;

    public int FullscreenChartIndex => _dashboard?.FullscreenChartIndex ?? 0;

    public bool CanNavigatePrevious => _dashboard?.CanNavigatePrevious ?? false;

    public bool CanNavigateNext => _dashboard?.CanNavigateNext ?? false;

    public string FullscreenIndicator => _dashboard?.FullscreenIndicator ?? "0 / 0";

    #endregion

    #region 转发命令 - 仪表盘

    [RelayCommand]
    private void StatisticsConfigCommand()
    {
        Dashboard.StatisticsConfigCommand();
    }

    [RelayCommand]
    private async Task RefreshStatisticsCommand()
    {
        await Dashboard.RefreshStatisticsCommand();
    }

    [RelayCommand]
    private void EnterFullscreen(DashboardChartViewModel chart)
    {
        Dashboard.EnterFullscreenCommand.Execute(chart);
    }

    [RelayCommand]
    private void ExitFullscreen()
    {
        Dashboard.ExitFullscreenCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToPreviousChart()
    {
        Dashboard.NavigateToPreviousChartCommand.Execute(null);
    }

    [RelayCommand]
    private void NavigateToNextChart()
    {
        Dashboard.NavigateToNextChartCommand.Execute(null);
    }

    #endregion
}
