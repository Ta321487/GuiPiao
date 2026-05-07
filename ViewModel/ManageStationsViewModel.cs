using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

public partial class ManageStationsViewModel : ObservableObject
{
    private readonly ConfirmationService _confirmationService;
    private readonly LogService _logService;
    private readonly StationRepository _stationRepository;

    [ObservableProperty] private string _city;

    [ObservableProperty] private string _district;

    [ObservableProperty] private string _latitude;

    [ObservableProperty] private string _longitude;

    [ObservableProperty] private string _province;

    [ObservableProperty] private string _railwayBureau;

    // 搜索关键词
    [ObservableProperty] private string _searchKeyword;

    [ObservableProperty] private string _stationCode;

    [ObservableProperty] private int _stationLevel;

    // 车站信息属性
    [ObservableProperty] private string _stationName;

    [ObservableProperty] private string _stationPinyin;

    // 车站列表
    [ObservableProperty] private ObservableCollection<StationInfo> _stations;

    public ManageStationsViewModel()
    {
        _stationRepository = new StationRepository();
        _confirmationService = new ConfirmationService();
        _logService = new LogService();
        Stations = new ObservableCollection<StationInfo>();
        LoadAllStationsAsync();
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            return;
    }

    private async void LoadAllStationsAsync()
    {
        var stations = await _stationRepository.GetAllStationsAsync();
        Stations.Clear();
        foreach (var station in stations) Stations.Add(station);
    }

    [RelayCommand]
    private async Task SaveStationAsync()
    {
        // 验证必填字段
        if (string.IsNullOrEmpty(StationName) || string.IsNullOrEmpty(StationCode))
        {
            MessageBoxWindow.Show("请填写车站名称和车站代码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 检查车站代码是否已存在
        var existingStation = await _stationRepository.GetStationByCodeAsync(StationCode);
        if (existingStation != null)
        {
            // 更新现有车站
            existingStation.StationName = StationName;
            existingStation.Province = Province;
            existingStation.City = City;
            existingStation.District = District;
            existingStation.StationPinyin = StationPinyin;
            existingStation.StationLevel = StationLevel;
            existingStation.RailwayBureau = RailwayBureau;
            existingStation.Longitude = Longitude;
            existingStation.Latitude = Latitude;

            try
            {
                await _stationRepository.UpdateStationAsync(existingStation);
                _logService.Info("ManageStationsViewModel", $"更新车站: {StationName}");
                MessageBoxWindow.Show("车站信息更新成功");
                LoadAllStationsAsync();
                ClearFields();
            }
            catch (Exception ex)
            {
                _logService.Error("ManageStationsViewModel", $"更新车站失败: {ex.Message}");
                MessageBoxWindow.Show($"更新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            // 创建新车站
            var newStation = new StationInfo
            {
                StationName = StationName,
                Province = Province,
                City = City,
                District = District,
                StationCode = StationCode,
                StationPinyin = StationPinyin,
                StationLevel = StationLevel,
                RailwayBureau = RailwayBureau,
                Longitude = Longitude,
                Latitude = Latitude
            };

            try
            {
                await _stationRepository.AddStationAsync(newStation);
                _logService.Info("ManageStationsViewModel", $"添加车站: {StationName}");
                MessageBoxWindow.Show("车站添加成功");
                Stations.Add(newStation);
                ClearFields();
            }
            catch (Exception ex)
            {
                _logService.Error("ManageStationsViewModel", $"添加车站失败: {ex.Message}");
                MessageBoxWindow.Show($"添加失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void EditStation(StationInfo station)
    {
        if (station == null)
            return;

        StationName = station.StationName;
        Province = station.Province;
        City = station.City;
        District = station.District;
        StationCode = station.StationCode;
        StationPinyin = station.StationPinyin;
        StationLevel = station.StationLevel;
        RailwayBureau = station.RailwayBureau;
        Longitude = station.Longitude;
        Latitude = station.Latitude;
    }

    [RelayCommand]
    private async Task DeleteStationAsync(StationInfo station)
    {
        if (station == null)
            return;

        if (!_confirmationService.ConfirmDelete($"车站 {station.StationName}"))
            return;

        try
        {
            await _stationRepository.DeleteStationAsync(station.StationCode);
            Stations.Remove(station);
            _logService.Info("ManageStationsViewModel", $"删除车站: {station.StationName}");
            MessageBoxWindow.Show("车站删除成功");
        }
        catch (Exception ex)
        {
            _logService.Error("ManageStationsViewModel", $"删除车站失败: {ex.Message}");
            MessageBoxWindow.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SearchStationsAsync()
    {
        if (string.IsNullOrEmpty(SearchKeyword))
        {
            LoadAllStationsAsync();
            return;
        }

        var stations = await _stationRepository.SearchStationsAsync(SearchKeyword);
        Stations.Clear();
        foreach (var station in stations) Stations.Add(station);
    }

    [RelayCommand]
    private void ClearFields()
    {
        StationName = string.Empty;
        Province = string.Empty;
        City = string.Empty;
        District = string.Empty;
        StationCode = string.Empty;
        StationPinyin = string.Empty;
        StationLevel = 0;
        RailwayBureau = string.Empty;
        Longitude = string.Empty;
        Latitude = string.Empty;
    }
}