using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

public partial class QueryTrainTicketViewModel : ObservableObject
{
    private readonly ConfirmationService _confirmationService;
    private readonly LogService _logService;
    private readonly TrainRideRepository _trainRideRepository;

    // 分页相关
    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private int _pageSize = 50;

    [ObservableProperty] private string _queryDate;

    // 查询条件
    [ObservableProperty] private string _queryKeyword;

    [ObservableProperty] private string _queryStation;

    [ObservableProperty] private string _queryTrainNo;

    [ObservableProperty] private int _totalCount;

    // 查询结果
    [ObservableProperty] private ObservableCollection<TrainRideInfo> _trainRides;

    public QueryTrainTicketViewModel()
    {
        _trainRideRepository = new TrainRideRepository();
        _confirmationService = new ConfirmationService();
        _logService = new LogService();
        TrainRides = new ObservableCollection<TrainRideInfo>();

        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            return;

        // 使用分页加载，避免一次性加载全部数据
        _ = LoadTrainRidesAsync();
    }

    /// <summary>
    ///     分页加载行程数据
    /// </summary>
    private async Task LoadTrainRidesAsync(int pageIndex = 1)
    {
        CurrentPage = pageIndex;
        var rides = await _trainRideRepository.GetTrainRidesByPageAsync(CurrentPage, PageSize);
        var rideList = rides.ToList();

        TrainRides.Clear();
        foreach (var ride in rideList) TrainRides.Add(ride);

        // 获取总数（仅在第一页时）
        if (CurrentPage == 1) TotalCount = await _trainRideRepository.GetTotalTrainRidesCountAsync();
    }

    [RelayCommand]
    private async Task QueryByKeywordAsync()
    {
        if (string.IsNullOrEmpty(QueryKeyword))
        {
            await LoadTrainRidesAsync();
            return;
        }

        var rides = await _trainRideRepository.SearchTrainRidesAsync(QueryKeyword);
        TrainRides.Clear();
        foreach (var ride in rides) TrainRides.Add(ride);
        TotalCount = TrainRides.Count;
    }

    [RelayCommand]
    private async Task QueryByDateAsync()
    {
        if (string.IsNullOrEmpty(QueryDate))
        {
            await LoadTrainRidesAsync();
            return;
        }

        var rides = await _trainRideRepository.GetTrainRidesByDateAsync(QueryDate);
        TrainRides.Clear();
        foreach (var ride in rides) TrainRides.Add(ride);
        TotalCount = TrainRides.Count;
    }

    [RelayCommand]
    private async Task QueryByStationAsync()
    {
        if (string.IsNullOrEmpty(QueryStation))
        {
            await LoadTrainRidesAsync();
            return;
        }

        var rides = await _trainRideRepository.GetTrainRidesByStationAsync(QueryStation);
        TrainRides.Clear();
        foreach (var ride in rides) TrainRides.Add(ride);
        TotalCount = TrainRides.Count;
    }

    [RelayCommand]
    private async Task QueryByTrainNoAsync()
    {
        if (string.IsNullOrEmpty(QueryTrainNo))
        {
            await LoadTrainRidesAsync();
            return;
        }

        var rides = await _trainRideRepository.GetTrainRidesByTrainNoAsync(QueryTrainNo);
        TrainRides.Clear();
        foreach (var ride in rides) TrainRides.Add(ride);
        TotalCount = TrainRides.Count;
    }

    [RelayCommand]
    private async Task DeleteTrainRideAsync(TrainRideInfo ride)
    {
        if (ride == null)
            return;

        if (!_confirmationService.ConfirmDelete("这张火车票"))
            return;

        try
        {
            await _trainRideRepository.DeleteTrainRideAsync(ride.Id);
            TrainRides.Remove(ride);
            _logService.Info("QueryTrainTicketViewModel",
                $"删除火车票: {ride.TrainNo} {ride.DepartStation}->{ride.ArriveStation}");
            MessageBoxWindow.Show(Application.Current.MainWindow, "删除成功");
        }
        catch (Exception ex)
        {
            _logService.Error("QueryTrainTicketViewModel", $"删除火车票失败: {ex.Message}");
            MessageBoxWindow.Show(Application.Current.MainWindow, $"删除失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ResetQuery()
    {
        QueryKeyword = string.Empty;
        QueryDate = string.Empty;
        QueryStation = string.Empty;
        QueryTrainNo = string.Empty;
        await LoadTrainRidesAsync();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1) await LoadTrainRidesAsync(CurrentPage - 1);
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage * PageSize < TotalCount) await LoadTrainRidesAsync(CurrentPage + 1);
    }
}