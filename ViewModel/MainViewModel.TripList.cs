using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发属性 - 行程列表相关

    public ObservableCollection<TripItem> TripItems => TripList.TripItems;

    public TripItem? SelectedTripItem
    {
        get => TripList.SelectedTripItem;
        set => TripList.SelectedTripItem = value;
    }

    public ICollectionView? TripItemsView => TripList.TripItemsView;
    public int CurrentPage => TripList.CurrentPage;
    public int TotalPages => TripList.TotalPages;
    public int TotalItems => TripList.TotalItems;
    public ObservableCollection<int> PaginationButtons => TripList.PaginationButtons;
    public bool IsTripListExpanded => TripList.IsTripListExpanded;
    public bool IsDataGridVisible => TripList.IsDataGridVisible;
    public string CollapseButtonContent => TripList.CollapseButtonContent;
    public bool IsOperationButtonsVisible => TripList.IsOperationButtonsVisible;

    #endregion

    #region 转发命令 - 行程列表

    [RelayCommand]
    private void ViewTripCommand(TripItem trip)
    {
        TripList.ViewTripCommand(trip);
    }

    [RelayCommand]
    private void EditTripCommand(TripItem trip)
    {
        TripList.EditTripCommand(trip);
    }

    [RelayCommand]
    private async Task DeleteTripCommand(TripItem trip)
    {
        await TripList.DeleteTripCommand(trip);
    }

    [RelayCommand]
    public async Task PreviousPageCommand()
    {
        await TripList.PreviousPageCommand();
    }

    [RelayCommand]
    public async Task NextPageCommand()
    {
        await TripList.NextPageCommand();
    }

    [RelayCommand]
    public async Task GoToPageCommand(int page)
    {
        await TripList.GoToPageCommand(page);
    }

    [RelayCommand]
    public void ToggleTripListCommand()
    {
        TripList.ToggleTripListCommand();
    }

    [RelayCommand]
    private async Task ExportTripList()
    {
        await TripList.OnExportTripListAsync();
    }

    #endregion

    private void SubscribeToTripListChanges()
    {
        _tripListPropertyChangedHandler = (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(TripList.TripItems):
                    OnPropertyChanged(nameof(TripItems));
                    SetTemporaryStatus($"行程已更新，共 {TripList.TotalItems} 条记录");
                    break;
                case nameof(TripList.SelectedTripItem):
                    OnPropertyChanged(nameof(SelectedTripItem));
                    if (TripList.SelectedTripItem != null)
                        SetTemporaryStatus(
                            $"已选择：{TripList.SelectedTripItem.TrainNo} {TripList.SelectedTripItem.DepartStation}→{TripList.SelectedTripItem.ArriveStation}");
                    break;
                case nameof(TripList.TripItemsView):
                    OnPropertyChanged(nameof(TripItemsView));
                    break;
                case nameof(TripList.CurrentPage):
                    OnPropertyChanged(nameof(CurrentPage));
                    SetTemporaryStatus($"已切换到第 {TripList.CurrentPage} 页");
                    break;
                case nameof(TripList.TotalPages):
                    OnPropertyChanged(nameof(TotalPages));
                    break;
                case nameof(TripList.TotalItems):
                    OnPropertyChanged(nameof(TotalItems));
                    break;
                case nameof(TripList.PaginationButtons):
                    OnPropertyChanged(nameof(PaginationButtons));
                    break;
                case nameof(TripList.IsTripListExpanded):
                    Debug.WriteLine(
                        $"[MainViewModel] TripList.IsTripListExpanded changed to: {TripList.IsTripListExpanded}");
                    OnPropertyChanged(nameof(IsTripListExpanded));
                    OnPropertyChanged(nameof(IsDataGridVisible));
                    OnPropertyChanged(nameof(CollapseButtonContent));
                    SetTemporaryStatus(TripList.IsTripListExpanded ? "行程已展开" : "行程已收起");
                    break;
                case nameof(TripList.IsOperationButtonsVisible):
                    OnPropertyChanged(nameof(IsOperationButtonsVisible));
                    break;
            }
        };
        TripList.PropertyChanged += _tripListPropertyChangedHandler;
    }
}
