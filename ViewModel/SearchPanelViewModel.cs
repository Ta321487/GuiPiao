using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.DataAccess;
using GuiPiao.Messages;
using GuiPiao.Model;

namespace GuiPiao.ViewModel;

public partial class SearchPanelViewModel : ObservableObject
{
    private readonly TrainRideRepository _trainRideRepository;

    [ObservableProperty] private string _arriveStation = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _arriveStationSuggestions = new();

    [ObservableProperty] private string _dateRange = string.Empty;

    [ObservableProperty] private string _departStation = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _departStationSuggestions = new();

    [ObservableProperty] private bool _isArriveStationDropDownOpen;

    [ObservableProperty] private bool _isDepartStationDropDownOpen;

    [ObservableProperty] private bool _isSearchExpanded;

    // 车次号前缀（如 G/D/C/Z/T/K 等），默认"全部"
    [ObservableProperty] private string _selectedTrainNoPrefix = "全部";

    [ObservableProperty] private string _status = string.Empty;

    // 车次号数字部分
    [ObservableProperty] private string _trainNoNumber = string.Empty;

    public SearchPanelViewModel()
    {
        _trainRideRepository = new TrainRideRepository();
    }

    /// <summary>
    ///     车次号数字输入框是否可用（选择"全部"时禁用）
    /// </summary>
    public bool IsTrainNoNumberEnabled => SelectedTrainNoPrefix != "全部";

    // 车次号前缀选项
    public ObservableCollection<string> TrainNoPrefixes { get; } = new()
    {
        "全部", "G", "C", "D", "Z", "T", "K", "L", "S", "纯数字"
    };

    [RelayCommand]
    private async Task DepartStationTextChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            DepartStationSuggestions.Clear();
            IsDepartStationDropDownOpen = false;
            return;
        }

        var suggestions = await _trainRideRepository.SearchUserDepartStationsAsync(keyword);
        DepartStationSuggestions = new ObservableCollection<string>(suggestions);
        IsDepartStationDropDownOpen = suggestions.Count > 0;
    }

    [RelayCommand]
    private async Task ArriveStationTextChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            ArriveStationSuggestions.Clear();
            IsArriveStationDropDownOpen = false;
            return;
        }

        var suggestions = await _trainRideRepository.SearchUserArriveStationsAsync(keyword);
        ArriveStationSuggestions = new ObservableCollection<string>(suggestions);
        IsArriveStationDropDownOpen = suggestions.Count > 0;
    }

    [RelayCommand]
    private void SelectDepartStation(string station)
    {
        DepartStation = station;
        IsDepartStationDropDownOpen = false;
        DepartStationSuggestions.Clear();
    }

    [RelayCommand]
    private void SelectArriveStation(string station)
    {
        ArriveStation = station;
        IsArriveStationDropDownOpen = false;
        ArriveStationSuggestions.Clear();
    }

    /// <summary>
    ///     验证车次号数字部分只能输入数字
    /// </summary>
    [RelayCommand]
    private void TrainNoNumberPreviewTextInput(string text)
    {
        // 只允许输入数字
        if (!string.IsNullOrEmpty(text) && !Regex.IsMatch(text, "^[0-9]+$"))
        {
            // 如果不是数字，阻止输入
        }
    }

    [RelayCommand]
    public void SearchCommand()
    {
        IsDepartStationDropDownOpen = false;
        IsArriveStationDropDownOpen = false;

        // 构建完整车次号
        var trainNo = BuildTrainNo();

        var criteria = new AdvancedSearchCriteria
        {
            DepartStation = DepartStation,
            ArriveStation = ArriveStation,
            TrainNo = trainNo,
            TrainNoPrefix = SelectedTrainNoPrefix == "全部" ? string.Empty : SelectedTrainNoPrefix,
            TrainNoNumber = TrainNoNumber,
            DateRange = DateRange,
            Status = Status
        };

        WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage(criteria));
    }

    [RelayCommand]
    public void ClearConditionCommand()
    {
        DepartStation = string.Empty;
        ArriveStation = string.Empty;
        SelectedTrainNoPrefix = "全部";
        TrainNoNumber = string.Empty;
        DateRange = string.Empty;
        Status = string.Empty;
        DepartStationSuggestions.Clear();
        ArriveStationSuggestions.Clear();
        IsDepartStationDropDownOpen = false;
        IsArriveStationDropDownOpen = false;

        WeakReferenceMessenger.Default.Send(new AdvancedSearchMessage());
    }

    /// <summary>
    ///     根据前缀和数字构建完整车次号
    /// </summary>
    private string BuildTrainNo()
    {
        if (SelectedTrainNoPrefix == "全部" || string.IsNullOrWhiteSpace(SelectedTrainNoPrefix)) return TrainNoNumber;

        if (SelectedTrainNoPrefix == "纯数字") return TrainNoNumber;

        return SelectedTrainNoPrefix + TrainNoNumber;
    }
}