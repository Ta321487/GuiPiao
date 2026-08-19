using System;
using System.Collections.Generic;
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

public partial class ManageStationsViewModel : ObservableObject
{
    private readonly ConfirmationService _confirmationService;
    private readonly LogService _logService;
    private readonly StationRepository _stationRepository;
    private readonly TrainRideRepository _trainRideRepository;
    private FormSnapshot _baseline = FormSnapshot.Empty;
    private string _loadedStationCode = string.Empty;
    private bool _hasRideReferences;

    [ObservableProperty] private string _city = string.Empty;

    [ObservableProperty] private string _district = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    [NotifyPropertyChangedFor(nameof(StationCodeReadOnly))]
    private bool _isAdding = true;

    [ObservableProperty] private bool _isStationListEmpty = true;

    [ObservableProperty] private string _latitude = string.Empty;

    [ObservableProperty] private string _longitude = string.Empty;

    [ObservableProperty] private string _province = string.Empty;

    [ObservableProperty] private string _railwayBureau = string.Empty;

    [ObservableProperty] private string _searchKeyword = string.Empty;

    [ObservableProperty] private StationInfo? _selectedStation;

    [ObservableProperty] private string _stationCode = string.Empty;

    [ObservableProperty] private StationLevel _selectedStationLevel = StationLevel.Unspecified;

    [ObservableProperty] private string _stationName = string.Empty;

    [ObservableProperty] private string _stationPinyin = string.Empty;

    [ObservableProperty] private ObservableCollection<StationInfo> _stations = new();

    public ManageStationsViewModel()
    {
        _stationRepository = new StationRepository();
        _trainRideRepository = new TrainRideRepository();
        _confirmationService = new ConfirmationService();
        _logService = new LogService();
        Stations = new ObservableCollection<StationInfo>();
        LoadAllStationsAsync();
        CaptureBaseline();
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            return;
    }

    public IReadOnlyList<KeyValuePair<StationLevel, string>> StationLevelOptions { get; } =
        StationLevelNames.Names.ToList();

    public string FormTitle => IsAdding ? "新增车站" : "编辑车站";

    public string SaveButtonText => IsAdding ? "添加" : "保存";

    /// <summary>
    ///     已有行程引用且载入时已有电报码时锁定；空码可补填。
    /// </summary>
    public bool StationCodeReadOnly => !IsAdding && _hasRideReferences &&
                                       !string.IsNullOrEmpty(_loadedStationCode);

    public bool HasUnsavedChanges
    {
        get
        {
            var current = CaptureCurrent();
            if (current.IsAdding && current == FormSnapshot.Empty)
                return false;
            return _baseline != current;
        }
    }

    public IReadOnlyList<string> GetEmptyRequiredFields() =>
        StationFormRules.GetEmptyRequiredFields(StationName, StationPinyin);

    public bool HasRequiredFieldsEmpty() => GetEmptyRequiredFields().Count > 0;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(HasUnsavedChanges) or nameof(FormTitle) or nameof(SaveButtonText)
            or nameof(StationCodeReadOnly) or nameof(Stations) or nameof(IsStationListEmpty)
            or nameof(SearchKeyword) or nameof(SelectedStation) or nameof(StationLevelOptions))
            return;
        OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    private async void LoadAllStationsAsync()
    {
        var stations = await _stationRepository.GetAllStationsAsync();
        ReplaceStations(stations);
    }

    private void ReplaceStations(IEnumerable<StationInfo> stations)
    {
        var selectedCode = IsAdding ? null : SelectedStation?.StationCode;
        Stations.Clear();
        foreach (var station in stations) Stations.Add(station);
        IsStationListEmpty = Stations.Count == 0;
        if (!IsAdding && !string.IsNullOrEmpty(selectedCode))
            SelectedStation = Stations.FirstOrDefault(s =>
                string.Equals(s.StationCode, selectedCode, StringComparison.OrdinalIgnoreCase)) ?? SelectedStation;
    }

    private void FillFrom(StationInfo station)
    {
        StationName = StationFormRules.ToNameBody(station.StationName);
        Province = station.Province ?? string.Empty;
        City = station.City ?? string.Empty;
        District = station.District ?? string.Empty;
        StationCode = station.StationCode ?? string.Empty;
        StationPinyin = station.StationPinyin ?? string.Empty;
        SelectedStationLevel = StationLevelNames.FromStoredValue(station.StationLevel);
        RailwayBureau = station.RailwayBureau ?? string.Empty;
        Longitude = station.Longitude ?? string.Empty;
        Latitude = station.Latitude ?? string.Empty;
        IsAdding = false;
        _loadedStationCode = StationFormRules.NormalizeCode(station.StationCode);
        _hasRideReferences = false;
        OnPropertyChanged(nameof(StationCodeReadOnly));
        CaptureBaseline();
        _ = RefreshRideReferencesAsync(_loadedStationCode);
    }

    private async Task RefreshRideReferencesAsync(string stationCode)
    {
        if (string.IsNullOrEmpty(stationCode))
            return;

        var rideCount = await _trainRideRepository.CountActiveRidesUsingStationCodeAsync(stationCode);
        _hasRideReferences = rideCount > 0;
        OnPropertyChanged(nameof(StationCodeReadOnly));
    }

    public async Task<bool> TrySelectStationAsync(StationInfo? station)
    {
        if (station == null)
            return true;

        if (!IsAdding && SelectedStation != null &&
            string.Equals(SelectedStation.StationCode, station.StationCode, StringComparison.OrdinalIgnoreCase) &&
            !HasUnsavedChanges)
            return true;

        if (!await ConfirmLeaveAsync())
            return false;

        SelectedStation = station;
        FillFrom(station);
        return true;
    }

    [RelayCommand]
    private async Task SaveStationAsync()
    {
        await SaveStationCoreAsync(showSuccess: true);
    }

    private async Task<bool> SaveStationCoreAsync(bool showSuccess)
    {
        var empty = GetEmptyRequiredFields();
        if (empty.Count > 0)
        {
            MessageBoxWindow.Show($"请填写{string.Join("、", empty)}", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var storedName = StationFormRules.ToStoredName(StationName);
        var pinyin = StationFormRules.NormalizePinyin(StationPinyin);
        StationPinyin = pinyin;

        var duplicateName = await _stationRepository.GetStationByNameAsync(storedName,
            IsAdding ? null : _loadedStationCode);
        if (duplicateName != null)
        {
            MessageBoxWindow.Show("该站名已存在，请使用其他名称或在左侧列表中选择后编辑。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var existingCodes = Stations
            .Select(s => s.StationCode ?? string.Empty)
            .Where(c => !string.IsNullOrEmpty(c));
        if (!IsAdding && !string.IsNullOrEmpty(_loadedStationCode))
            existingCodes = existingCodes.Where(c =>
                !string.Equals(c, _loadedStationCode, StringComparison.OrdinalIgnoreCase));

        var codeInput = StationFormRules.NormalizeCode(StationCode);
        var code = !string.IsNullOrEmpty(codeInput)
            ? codeInput
            : !string.IsNullOrEmpty(_loadedStationCode)
                ? _loadedStationCode
                : StationFormRules.EnsureUniqueCode(
                    StationFormRules.GenerateLocalCodeFromPinyin(pinyin, StationName), existingCodes);
        StationCode = code;

        if (!string.IsNullOrEmpty(codeInput) && !StationFormRules.IsValidCodeFormat(code))
        {
            MessageBoxWindow.Show("电报码须为 2–5 位字母或数字。", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        if (IsAdding || !string.Equals(code, _loadedStationCode, StringComparison.OrdinalIgnoreCase))
        {
            var existingByCode = await _stationRepository.GetStationByCodeAsync(code);
            if (existingByCode != null)
            {
                MessageBoxWindow.Show("该电报码已被其他车站使用。", "提示", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
        }

        if (IsAdding)
        {
            var newStation = BuildStationFromForm(storedName, code, pinyin);
            try
            {
                await _stationRepository.AddStationAsync(newStation);
                _logService.Info("ManageStationsViewModel", $"添加车站: {storedName} ({code})");
                if (showSuccess)
                    MessageBoxWindow.Show("车站添加成功");
                LoadAllStationsAsync();
                ResetToAddMode();
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error("ManageStationsViewModel", $"添加车站失败: {ex.Message}");
                MessageBoxWindow.Show($"添加失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        var existingByLoadedCode = string.IsNullOrEmpty(_loadedStationCode)
            ? null
            : await _stationRepository.GetStationByCodeAsync(_loadedStationCode);
        if (existingByLoadedCode == null)
        {
            MessageBoxWindow.Show("未找到要更新的车站。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!string.Equals(code, _loadedStationCode, StringComparison.OrdinalIgnoreCase) && _hasRideReferences)
        {
            MessageBoxWindow.Show("该车站已被行程引用，无法修改电报码。", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        ApplyFormTo(existingByLoadedCode, storedName, code, pinyin);
        try
        {
            await _stationRepository.UpdateStationAsync(existingByLoadedCode, _loadedStationCode);
            _logService.Info("ManageStationsViewModel", $"更新车站: {storedName} ({code})");
            if (showSuccess)
                MessageBoxWindow.Show("车站信息更新成功");
            LoadAllStationsAsync();
            ResetToAddMode();
            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("ManageStationsViewModel", $"更新车站失败: {ex.Message}");
            MessageBoxWindow.Show($"更新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private StationInfo BuildStationFromForm(string storedName, string code, string pinyin)
    {
        return new StationInfo
        {
            StationName = storedName,
            Province = Province.Trim(),
            City = City.Trim(),
            District = District.Trim(),
            StationCode = code,
            StationPinyin = pinyin,
            StationLevel = (int)SelectedStationLevel,
            RailwayBureau = RailwayBureau.Trim(),
            Longitude = Longitude.Trim(),
            Latitude = Latitude.Trim()
        };
    }

    private void ApplyFormTo(StationInfo station, string storedName, string code, string pinyin)
    {
        station.StationName = storedName;
        station.StationCode = code;
        station.StationPinyin = pinyin;
        station.Province = Province.Trim();
        station.City = City.Trim();
        station.District = District.Trim();
        station.StationLevel = (int)SelectedStationLevel;
        station.RailwayBureau = RailwayBureau.Trim();
        station.Longitude = Longitude.Trim();
        station.Latitude = Latitude.Trim();
    }

    [RelayCommand]
    private async Task EditStation(StationInfo station)
    {
        await TrySelectStationAsync(station);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedStation))]
    private async Task DeleteStationAsync()
    {
        var station = SelectedStation;
        if (station == null || IsAdding)
            return;

        var rideCount = await _trainRideRepository.CountActiveRidesUsingStationCodeAsync(station.StationCode);
        if (rideCount > 0)
        {
            MessageBoxWindow.Show(
                $"有 {rideCount} 条行程引用电报码「{station.StationCode}」，无法删除。",
                "无法删除",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!_confirmationService.ConfirmDelete($"车站 {station.StationName}"))
            return;

        try
        {
            await _stationRepository.DeleteStationAsync(station.StationCode);
            Stations.Remove(station);
            IsStationListEmpty = Stations.Count == 0;
            ResetToAddMode();
            _logService.Info("ManageStationsViewModel", $"删除车站: {station.StationName}");
        }
        catch (Exception ex)
        {
            _logService.Error("ManageStationsViewModel", $"删除车站失败: {ex.Message}");
            MessageBoxWindow.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanDeleteSelectedStation() => !IsAdding && SelectedStation != null;

    partial void OnIsAddingChanged(bool value) => DeleteStationCommand.NotifyCanExecuteChanged();

    partial void OnSelectedStationChanged(StationInfo? value) => DeleteStationCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task SearchStationsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            LoadAllStationsAsync();
            return;
        }

        var stations = await _stationRepository.SearchStationsAsync(SearchKeyword.Trim());
        ReplaceStations(stations);
    }

    [RelayCommand]
    private async Task BeginAddAsync()
    {
        if (IsAdding && !HasUnsavedChanges)
            return;
        if (!await ConfirmLeaveAsync())
            return;
        ResetToAddMode();
    }

    public async Task<bool> ConfirmLeaveAsync()
    {
        if (!HasUnsavedChanges)
            return true;

        var result = MessageBoxWindow.Show(
            "您有未保存的车站信息。\n\n是否保存更改？",
            "未保存的更改",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        switch (result)
        {
            case MessageBoxResult.Cancel:
            case MessageBoxResult.None:
                return false;
            case MessageBoxResult.No:
                return true;
            case MessageBoxResult.Yes:
                return await SaveStationCoreAsync(showSuccess: false);
            default:
                return false;
        }
    }

    private void ResetToAddMode()
    {
        SelectedStation = null;
        IsAdding = true;
        _loadedStationCode = string.Empty;
        _hasRideReferences = false;
        OnPropertyChanged(nameof(StationCodeReadOnly));
        StationName = string.Empty;
        Province = string.Empty;
        City = string.Empty;
        District = string.Empty;
        StationCode = string.Empty;
        StationPinyin = string.Empty;
        SelectedStationLevel = StationLevel.Unspecified;
        RailwayBureau = string.Empty;
        Longitude = string.Empty;
        Latitude = string.Empty;
        CaptureBaseline();
    }

    private void CaptureBaseline() => _baseline = CaptureCurrent();

    private FormSnapshot CaptureCurrent() => new(
        IsAdding,
        StationName.Trim(),
        StationCode.Trim(),
        Province.Trim(),
        City.Trim(),
        District.Trim(),
        StationFormRules.NormalizePinyin(StationPinyin),
        SelectedStationLevel,
        RailwayBureau.Trim(),
        Longitude.Trim(),
        Latitude.Trim());

    private sealed record FormSnapshot(
        bool IsAdding,
        string StationName,
        string StationCode,
        string Province,
        string City,
        string District,
        string StationPinyin,
        StationLevel Level,
        string RailwayBureau,
        string Longitude,
        string Latitude)
    {
        public static FormSnapshot Empty { get; } = new(true, "", "", "", "", "", "", StationLevel.Unspecified, "", "",
            "");
    }
}
