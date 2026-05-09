using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发属性 - 仪表盘相关

    public ObservableCollection<DashboardChartViewModel> DashboardCharts => Dashboard.DashboardCharts;
    public int DashboardColumns => Dashboard.DashboardColumns;
    public bool HasDashboardCharts => Dashboard.HasDashboardCharts;
    public DashboardConfig DashboardConfig => Dashboard.DashboardConfig;
    public bool IsFullscreenMode => Dashboard.IsFullscreenMode;
    public DashboardChartViewModel? FullscreenChart => Dashboard.FullscreenChart;
    public int FullscreenChartIndex => Dashboard.FullscreenChartIndex;
    public bool CanNavigatePrevious => Dashboard.CanNavigatePrevious;
    public bool CanNavigateNext => Dashboard.CanNavigateNext;
    public string FullscreenIndicator => Dashboard.FullscreenIndicator;

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

    private void SubscribeToDashboardChanges()
    {
        Dashboard.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(Dashboard.DashboardCharts):
                    OnPropertyChanged(nameof(DashboardCharts));
                    OnPropertyChanged(nameof(HasDashboardCharts));
                    SetTemporaryStatus(Dashboard.HasDashboardCharts
                        ? $"仪表盘已加载 {Dashboard.DashboardCharts.Count} 个图表"
                        : "仪表盘暂无图表");
                    break;
                case nameof(Dashboard.DashboardColumns):
                    OnPropertyChanged(nameof(DashboardColumns));
                    break;
                case nameof(Dashboard.IsFullscreenMode):
                    OnPropertyChanged(nameof(IsFullscreenMode));
                    break;
                case nameof(Dashboard.FullscreenChart):
                    OnPropertyChanged(nameof(FullscreenChart));
                    break;
                case nameof(Dashboard.FullscreenChartIndex):
                    OnPropertyChanged(nameof(FullscreenChartIndex));
                    OnPropertyChanged(nameof(CanNavigatePrevious));
                    OnPropertyChanged(nameof(CanNavigateNext));
                    OnPropertyChanged(nameof(FullscreenIndicator));
                    break;
            }
        };
    }
}
