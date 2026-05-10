using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发属性 - 高级检索区相关

    public bool IsSearchExpanded
    {
        get => SearchPanel.IsSearchExpanded;
        set => SearchPanel.IsSearchExpanded = value;
    }

    public string DepartStation
    {
        get => SearchPanel.DepartStation;
        set => SearchPanel.DepartStation = value;
    }

    public string ArriveStation
    {
        get => SearchPanel.ArriveStation;
        set => SearchPanel.ArriveStation = value;
    }

    public string DateRange
    {
        get => SearchPanel.DateRange;
        set => SearchPanel.DateRange = value;
    }

    public string Status
    {
        get => SearchPanel.Status;
        set => SearchPanel.Status = value;
    }

    public ObservableCollection<string> TrainNoPrefixes => SearchPanel.TrainNoPrefixes;

    public string SelectedTrainNoPrefix
    {
        get => SearchPanel.SelectedTrainNoPrefix;
        set => SearchPanel.SelectedTrainNoPrefix = value;
    }

    public string TrainNoNumber
    {
        get => SearchPanel.TrainNoNumber;
        set => SearchPanel.TrainNoNumber = value;
    }

    public bool IsTrainNoNumberEnabled => SearchPanel.IsTrainNoNumberEnabled;

    public ObservableCollection<string> DepartStationSuggestions => SearchPanel.DepartStationSuggestions;

    public bool IsDepartStationDropDownOpen
    {
        get => SearchPanel.IsDepartStationDropDownOpen;
        set => SearchPanel.IsDepartStationDropDownOpen = value;
    }

    public IRelayCommand<string> DepartStationTextChangedCommand => SearchPanel.DepartStationTextChangedCommand;
    public IRelayCommand<string> SelectDepartStationCommand => SearchPanel.SelectDepartStationCommand;

    public ObservableCollection<string> ArriveStationSuggestions => SearchPanel.ArriveStationSuggestions;

    public bool IsArriveStationDropDownOpen
    {
        get => SearchPanel.IsArriveStationDropDownOpen;
        set => SearchPanel.IsArriveStationDropDownOpen = value;
    }

    public IRelayCommand<string> ArriveStationTextChangedCommand => SearchPanel.ArriveStationTextChangedCommand;
    public IRelayCommand<string> SelectArriveStationCommand => SearchPanel.SelectArriveStationCommand;

    #endregion

    #region 转发命令 - 高级检索区

    [RelayCommand]
    private void SearchCommand()
    {
        SearchPanel.SearchCommand();
    }

    [RelayCommand]
    private void ClearConditionCommand()
    {
        SearchPanel.ClearConditionCommand();
    }

    #endregion

    private void SubscribeToSearchPanelChanges()
    {
        _searchPanelPropertyChangedHandler = (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(SearchPanel.IsSearchExpanded):
                    OnPropertyChanged(nameof(IsSearchExpanded));
                    SetTemporaryStatus(SearchPanel.IsSearchExpanded ? "高级检索区已展开" : "高级检索区已收起");
                    break;
                case nameof(SearchPanel.DepartStation):
                case nameof(SearchPanel.ArriveStation):
                case nameof(SearchPanel.SelectedTrainNoPrefix):
                    OnPropertyChanged(e.PropertyName);
                    OnPropertyChanged(nameof(IsTrainNoNumberEnabled));
                    break;
                case nameof(SearchPanel.TrainNoNumber):
                case nameof(SearchPanel.DateRange):
                case nameof(SearchPanel.Status):
                    OnPropertyChanged(e.PropertyName);
                    break;
                case nameof(SearchPanel.DepartStationSuggestions):
                    OnPropertyChanged(nameof(DepartStationSuggestions));
                    break;
                case nameof(SearchPanel.ArriveStationSuggestions):
                    OnPropertyChanged(nameof(ArriveStationSuggestions));
                    break;
                case nameof(SearchPanel.IsDepartStationDropDownOpen):
                    OnPropertyChanged(nameof(IsDepartStationDropDownOpen));
                    break;
                case nameof(SearchPanel.IsArriveStationDropDownOpen):
                    OnPropertyChanged(nameof(IsArriveStationDropDownOpen));
                    break;
            }
        };
        SearchPanel.PropertyChanged += _searchPanelPropertyChangedHandler;
    }
}
