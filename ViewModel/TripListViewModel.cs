using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Converters;
using GuiPiao.DataAccess;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;
using Microsoft.Win32;
using TripItem = GuiPiao.Model.TripItem;

namespace GuiPiao.ViewModel;

public partial class TripListViewModel : ObservableObject, IDisposable
{
    private readonly ConfirmationService _confirmationService;
    private readonly ExportService _exportService;
    private readonly ExportSettingsService _exportSettingsService;
    private readonly GeneralSettingsService _generalSettingsService;
    private readonly LogService _logService;
    private readonly TicketTagRepository _ticketTagRepository;

    private readonly TrainRideRepository _trainRideRepository;
    private readonly UISettingsService _uiSettingsService;

    private int _currentPage = 1;

    // 高级检索相关字段
    private AdvancedSearchCriteria? _currentSearchCriteria;

    private string _currentSortColumn = "id";
    private bool _currentSortDesc = true;
    private bool _isAdvancedSearchMode;
    private bool _isDisposed;

    [ObservableProperty] private bool _isOperationButtonsVisible = true;

    private bool _isTripListExpanded = true;

    private ObservableCollection<int> _paginationButtons = new();

    [ObservableProperty] private TripItem? _selectedTripItem;

    [ObservableProperty] private int _totalItems;

    private int _totalPages = 1;

    [ObservableProperty] private ObservableCollection<TripItem> _tripItems = new();

    private ICollectionView? _tripItemsView;

    public TripListViewModel()
    {
        _trainRideRepository = new TrainRideRepository();
        _ticketTagRepository = new TicketTagRepository();
        _generalSettingsService = new GeneralSettingsService();
        _logService = ServiceManager.Instance.LogService;
        _exportService = new ExportService();
        _exportSettingsService = new ExportSettingsService();
        _confirmationService = new ConfirmationService();
        _uiSettingsService = new UISettingsService();

        // 根据配置设置初始展开状态
        _isTripListExpanded = _uiSettingsService.Config.IsTripListExpandedByDefault;

        PaginationButtons = new ObservableCollection<int>();
        UpdatePaginationButtons();

        // 初始化排序状态
        InitializeSortState();

        // 订阅设置变更消息
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, async (recipient, message) =>
        {
            if (message.SettingType == "General") await RefreshTripItemsAsync();
        });

        // 订阅分组设置变更消息
        WeakReferenceMessenger.Default.Register<GroupSettingChangedMessage>(this, async (recipient, message) =>
        {
            Debug.WriteLine($"[GroupSettingChangedMessage] Received, GroupOption: {message.GroupOption}");
            _uiSettingsService.Config.DefaultGroup = message.GroupOption;
            Debug.WriteLine(
                $"[GroupSettingChangedMessage] Updated local config DefaultGroup to: {_uiSettingsService.Config.DefaultGroup}");
            await LoadTripItemsAsync();
            Debug.WriteLine("[GroupSettingChangedMessage] LoadTripItemsAsync completed");
        });

        // 订阅列配置变更消息
        WeakReferenceMessenger.Default.Register<DataGridColumnsChangedMessage>(this, (recipient, message) =>
        {
            _uiSettingsService.Config.DataGridColumns = message.ColumnConfigs;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (Application.Current.MainWindow is MainWindow mainWindow) mainWindow.RefreshDataGridColumns();
            });
        });

        // 订阅高级检索消息
        WeakReferenceMessenger.Default.Register<AdvancedSearchMessage>(this, async (recipient, message) =>
        {
            Debug.WriteLine($"[AdvancedSearchMessage] Received, IsClear: {message.IsClear}");
            if (message.IsClear)
                await ClearAdvancedSearchAsync();
            else
                await PerformAdvancedSearchAsync(message.Criteria);
        });

        // 订阅车票保存成功消息，刷新行程列表
        WeakReferenceMessenger.Default.Register<TicketSavedMessage>(this, async (recipient, message) =>
        {
            Debug.WriteLine(
                $"[TicketSavedMessage] Received, TicketId: {message.TicketId}, IsEditMode: {message.IsEditMode}, TrainNo: {message.TrainNo}");
            await LoadTripItemsAsync();
        });

        // 订阅刷新行程列表消息
        WeakReferenceMessenger.Default.Register<RefreshTripListMessage>(this, async (recipient, message) =>
        {
            Debug.WriteLine("[RefreshTripListMessage] Received");
            await LoadTripItemsAsync();
        });

        // 加载初始数据
        _ = LoadTripItemsAsync();
    }

    public ICollectionView? TripItemsView
    {
        get => _tripItemsView;
        private set
        {
            if (_tripItemsView != value)
            {
                _tripItemsView = value;
                OnPropertyChanged();
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                OnPropertyChanged();
                UpdatePaginationButtons();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (_totalPages != value)
            {
                _totalPages = value;
                OnPropertyChanged();
                UpdatePaginationButtons();
            }
        }
    }

    public ObservableCollection<int> PaginationButtons
    {
        get => _paginationButtons;
        set
        {
            _paginationButtons = value;
            OnPropertyChanged();
        }
    }

    public bool IsTripListExpanded
    {
        get => _isTripListExpanded;
        set
        {
            if (_isTripListExpanded != value)
            {
                _isTripListExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CollapseButtonContent));
                OnPropertyChanged(nameof(IsDataGridVisible));
            }
        }
    }

    // DataGrid是否可见（折叠时隐藏，展开时显示）
    public bool IsDataGridVisible => IsTripListExpanded;

    public string CollapseButtonContent => IsTripListExpanded ? "📥 折叠" : "📤 展开";

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        WeakReferenceMessenger.Default.Unregister<SettingsChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<GroupSettingChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<DataGridColumnsChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AdvancedSearchMessage>(this);
        WeakReferenceMessenger.Default.Unregister<TicketSavedMessage>(this);
    }

    private void InitializeSortState()
    {
        try
        {
            var uiConfig = _uiSettingsService.Config;
            var generalConfig = _generalSettingsService.Config;

            if (uiConfig.RememberDataSort &&
                !string.IsNullOrEmpty(uiConfig.LastSortColumn) &&
                !string.IsNullOrEmpty(uiConfig.LastSortDirection))
            {
                _currentSortColumn = uiConfig.LastSortColumn;
                _currentSortDesc = uiConfig.LastSortDirection != "Ascending";
                Debug.WriteLine($"[InitializeSortState] 使用上次排序: {_currentSortColumn} {uiConfig.LastSortDirection}");
                return;
            }

            switch (generalConfig.DefaultSort)
            {
                case SortOption.DateDesc:
                    _currentSortColumn = "date";
                    _currentSortDesc = true;
                    break;
                case SortOption.DateAsc:
                    _currentSortColumn = "date";
                    _currentSortDesc = false;
                    break;
                case SortOption.TrainNo:
                    _currentSortColumn = "train_no";
                    _currentSortDesc = false;
                    break;
                case SortOption.Departure:
                    _currentSortColumn = "depart_station";
                    _currentSortDesc = false;
                    break;
                default:
                    _currentSortColumn = "id";
                    _currentSortDesc = true;
                    break;
            }

            Debug.WriteLine(
                $"[InitializeSortState] 使用默认排序: {_currentSortColumn} {(_currentSortDesc ? "DESC" : "ASC")}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[InitializeSortState] 初始化排序状态失败: {ex.Message}");
            _currentSortColumn = "id";
            _currentSortDesc = true;
        }
    }

    private async Task RefreshTripItemsAsync()
    {
        _generalSettingsService.RefreshConfig();
        CurrentPage = 1;
        await LoadTripItemsAsync();
    }

    /// <summary>
    ///     执行高级检索
    /// </summary>
    public async Task PerformAdvancedSearchAsync(AdvancedSearchCriteria criteria)
    {
        Debug.WriteLine("[PerformAdvancedSearchAsync] Start");
        _currentSearchCriteria = criteria;
        _isAdvancedSearchMode = true;
        CurrentPage = 1; // 重置到第一页

        await LoadTripItemsAsync();
        Debug.WriteLine("[PerformAdvancedSearchAsync] Completed");
    }

    /// <summary>
    ///     清空高级检索条件，恢复默认列表
    /// </summary>
    public async Task ClearAdvancedSearchAsync()
    {
        Debug.WriteLine("[ClearAdvancedSearchAsync] Start");
        _currentSearchCriteria = null;
        _isAdvancedSearchMode = false;
        CurrentPage = 1; // 重置到第一页

        await LoadTripItemsAsync();
        Debug.WriteLine("[ClearAdvancedSearchAsync] Completed");
    }

    public async Task LoadTripItemsAsync()
    {
        Debug.WriteLine("[LoadTripItemsAsync] Start");
        try
        {
            var config = _generalSettingsService.Config;
            var pageSize = config.PageSize;
            Debug.WriteLine(
                $"[LoadTripItemsAsync] PageSize: {pageSize}, LoadRange: {config.LoadRange}, IsAdvancedSearch: {_isAdvancedSearchMode}");
            Debug.WriteLine($"[LoadTripItemsAsync] DatabasePath: {ConfigManager.Instance.DatabaseConnectionString}");

            var sortColumn = _currentSortColumn;
            var sortDesc = _currentSortDesc;
            Debug.WriteLine($"[LoadTripItemsAsync] SortColumn: {sortColumn}, SortDesc: {sortDesc}");

            IEnumerable<TrainRideInfo> trainRides;

            // 高级检索模式
            if (_isAdvancedSearchMode && _currentSearchCriteria != null)
            {
                Debug.WriteLine("[LoadTripItemsAsync] Using advanced search mode");
                var result = await _trainRideRepository.SearchTrainRidesAdvancedAsync(
                    _currentSearchCriteria,
                    CurrentPage,
                    pageSize,
                    sortColumn,
                    sortDesc);

                trainRides = result.Items;
                TotalItems = result.TotalCount;
            }
            else
            {
                // 普通模式（原有逻辑）
                var totalCount = await _trainRideRepository.GetTotalTrainRidesCountAsync();
                Debug.WriteLine($"[LoadTripItemsAsync] TotalCount: {totalCount}");
                var maxPage = Math.Max(1, (totalCount + pageSize - 1) / pageSize);
                if (CurrentPage > maxPage) CurrentPage = 1;

                DateTime? startDate = null;
                DateTime? endDate = null;
                var now = DateTime.Now;
                switch (config.LoadRange)
                {
                    case LoadRangeOption.ThisYear:
                        startDate = new DateTime(now.Year, 1, 1);
                        endDate = new DateTime(now.Year, 12, 31);
                        break;
                    case LoadRangeOption.Last3Months:
                        startDate = now.AddMonths(-3);
                        endDate = new DateTime(now.Year + 1, 12, 31);
                        break;
                    case LoadRangeOption.Last6Months:
                        startDate = now.AddMonths(-6);
                        endDate = new DateTime(now.Year + 1, 12, 31);
                        break;
                }

                Debug.WriteLine($"[LoadTripItemsAsync] startDate: {startDate}, endDate: {endDate}");

                if (startDate.HasValue && endDate.HasValue)
                {
                    trainRides = await _trainRideRepository.GetTrainRidesWithTagsByPageAsync(CurrentPage, pageSize,
                        sortColumn, sortDesc, startDate, endDate);
                    TotalItems = await _trainRideRepository.GetTotalTrainRidesCountAsync(startDate, endDate);
                }
                else
                {
                    trainRides =
                        await _trainRideRepository.GetTrainRidesWithTagsByPageAsync(CurrentPage, pageSize, sortColumn,
                            sortDesc);
                    TotalItems = await _trainRideRepository.GetTotalTrainRidesCountAsync();
                }
            }

            var tripItems = new ObservableCollection<TripItem>();
            var id = 1;
            var rideCount = 0;
            foreach (var ride in trainRides)
            {
                rideCount++;
                tripItems.Add(new TripItem
                {
                    Id = id++,
                    DatabaseId = ride.Id, // 保存数据库真实ID
                    TrainNo = ride.TrainNo,
                    DepartStation = ride.DepartStation,
                    ArriveStation = ride.ArriveStation,
                    DepartDate = ride.DepartDate,
                    DepartTime = ride.DepartTime,
                    SeatType = ride.SeatType,
                    Money = ride.Money.ToString(),
                    Status = ride.Status,
                    Tags = ride.Tags ?? new List<TicketTag>(),
                    CoachNo = ride.CoachNo,
                    SeatNo = ride.SeatNo,
                    TicketNumber = ride.TicketNumber,
                    DepartStationPinyin = ride.DepartStationPinyin,
                    ArriveStationPinyin = ride.ArriveStationPinyin,
                    CheckInLocation = ride.CheckInLocation,
                    Hint = ride.Hint,
                    AdditionalInfo = ride.AdditionalInfo,
                    TicketPurpose = ride.TicketPurpose,
                    TicketModificationType = ride.TicketModificationType,
                    TicketType = ConvertTicketTypeFlags(ride.TicketTypeFlags),
                    PaymentChannel = ConvertPaymentChannelFlags(ride.PaymentChannelFlags)
                });
            }

            Debug.WriteLine($"[LoadTripItemsAsync] Loaded {rideCount} rides from repository");
            TripItems = tripItems;
            Debug.WriteLine($"[LoadTripItemsAsync] Set TripItems, count: {TripItems.Count}");

            Debug.WriteLine("[LoadTripItemsAsync] Calling RecreateCollectionView");
            RecreateCollectionView();
            Debug.WriteLine("[LoadTripItemsAsync] RecreateCollectionView completed");

            var newTotalPages = (TotalItems + pageSize - 1) / pageSize;
            if (newTotalPages < 1) newTotalPages = 1;

            if (CurrentPage > newTotalPages) CurrentPage = newTotalPages;

            TotalPages = newTotalPages;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoadTripItemsAsync] Error: {ex.Message}");
            TripItems = new ObservableCollection<TripItem>
            {
                new()
                {
                    Id = 1, TrainNo = "G123", DepartStation = "北京南", ArriveStation = "上海虹桥", DepartDate = "2026-02-20",
                    DepartTime = "08:00", SeatType = "二等座", Money = "553.00", Status = 0
                },
                new()
                {
                    Id = 2, TrainNo = "D456", DepartStation = "上海虹桥", ArriveStation = "广州南", DepartDate = "2026-02-18",
                    DepartTime = "14:30", SeatType = "一等座", Money = "876.50", Status = 1
                },
                new()
                {
                    Id = 3, TrainNo = "Z789", DepartStation = "广州南", ArriveStation = "深圳北", DepartDate = "2026-02-25",
                    DepartTime = "09:15", SeatType = "硬座", Money = "74.50", Status = 0
                },
                new()
                {
                    Id = 4, TrainNo = "G789", DepartStation = "深圳北", ArriveStation = "杭州东", DepartDate = "2026-02-10",
                    DepartTime = "10:20", SeatType = "二等座", Money = "689.00", Status = 1
                }
            };
            TotalItems = TripItems.Count;
            TotalPages = 1;
            CurrentPage = 1;

            ApplyGroupingAndSorting();
        }
    }

    private void RecreateCollectionView()
    {
        Debug.WriteLine($"[RecreateCollectionView] Start, TripItems count: {TripItems?.Count ?? 0}");

        if (TripItems == null)
        {
            Debug.WriteLine("[RecreateCollectionView] TripItems is null, returning");
            return;
        }

        var uiConfig = _uiSettingsService.Config;
        if (uiConfig == null)
        {
            Debug.WriteLine("[RecreateCollectionView] uiConfig is null, returning");
            return;
        }

        if (TripItemsView != null && TripItemsView.SourceCollection == TripItems)
        {
            Debug.WriteLine("[RecreateCollectionView] Reusing existing CollectionView");
            UpdateCollectionViewGrouping(uiConfig.DefaultGroup);
        }
        else
        {
            TripItemsView = CollectionViewSource.GetDefaultView(TripItems);
            if (TripItemsView == null)
            {
                Debug.WriteLine("[RecreateCollectionView] TripItemsView is null after GetDefaultView, returning");
                return;
            }

            Debug.WriteLine("[RecreateCollectionView] Created new TripItemsView");
            ApplyGroupingToCollectionView(uiConfig.DefaultGroup);
        }

        Debug.WriteLine($"[RecreateCollectionView] GroupDescriptions count: {TripItemsView.GroupDescriptions.Count}");
        Debug.WriteLine("[RecreateCollectionView] End");
    }

    private void UpdateCollectionViewGrouping(GroupOption groupOption)
    {
        if (TripItemsView == null) return;
        TripItemsView.GroupDescriptions.Clear();
        ApplyGroupingToCollectionView(groupOption);
    }

    private void ApplyGroupingToCollectionView(GroupOption groupOption)
    {
        if (TripItemsView == null) return;

        Debug.WriteLine($"[ApplyGroupingToCollectionView] DefaultGroup from config: {groupOption}");

        switch (groupOption)
        {
            case GroupOption.DateDay:
                Debug.WriteLine("[ApplyGroupingToCollectionView] Adding DateDay group");
                TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("DepartDate",
                    new DateDayGroupConverter()));
                break;
            case GroupOption.DateMonth:
                Debug.WriteLine("[ApplyGroupingToCollectionView] Adding DateMonth group");
                TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("DepartDate",
                    new DateMonthGroupConverter()));
                break;
            case GroupOption.TrainNo:
                Debug.WriteLine("[ApplyGroupingToCollectionView] Adding TrainNo group");
                TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("TrainNo"));
                break;
            case GroupOption.Departure:
                Debug.WriteLine("[ApplyGroupingToCollectionView] Adding Departure group");
                TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("DepartStation"));
                break;
            case GroupOption.Arrival:
                Debug.WriteLine("[ApplyGroupingToCollectionView] Adding Arrival group");
                TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("ArriveStation"));
                break;
            case GroupOption.None:
            default:
                Debug.WriteLine("[ApplyGroupingToCollectionView] No group (None)");
                break;
        }
    }

    private void ApplyGroupingAndSorting()
    {
        if (TripItems == null || TripItemsView == null) return;

        var uiConfig = _uiSettingsService.Config;
        if (uiConfig == null) return;

        var needsUpdate = false;

        switch (uiConfig.DefaultGroup)
        {
            case GroupOption.DateDay:
                if (!HasGroupDescription("DepartDate"))
                {
                    TripItemsView.GroupDescriptions.Clear();
                    TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("DepartDate",
                        new DateDayGroupConverter()));
                    needsUpdate = true;
                }

                break;
            case GroupOption.DateMonth:
                if (!HasGroupDescription("DepartDate", true))
                {
                    TripItemsView.GroupDescriptions.Clear();
                    TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("DepartDate",
                        new DateMonthGroupConverter()));
                    needsUpdate = true;
                }

                break;
            case GroupOption.TrainNo:
                if (!HasGroupDescription("TrainNo"))
                {
                    TripItemsView.GroupDescriptions.Clear();
                    TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("TrainNo"));
                    needsUpdate = true;
                }

                break;
            case GroupOption.Departure:
                if (!HasGroupDescription("DepartStation"))
                {
                    TripItemsView.GroupDescriptions.Clear();
                    TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("DepartStation"));
                    needsUpdate = true;
                }

                break;
            case GroupOption.Arrival:
                if (!HasGroupDescription("ArriveStation"))
                {
                    TripItemsView.GroupDescriptions.Clear();
                    TripItemsView.GroupDescriptions.Add(new PropertyGroupDescription("ArriveStation"));
                    needsUpdate = true;
                }

                break;
            case GroupOption.None:
            default:
                if (TripItemsView.GroupDescriptions.Count > 0)
                {
                    TripItemsView.GroupDescriptions.Clear();
                    needsUpdate = true;
                }

                break;
        }

        if (needsUpdate) TripItemsView.Refresh();
    }

    private bool HasGroupDescription(string propertyName, bool hasConverter = false)
    {
        if (TripItemsView == null || TripItemsView.GroupDescriptions.Count == 0)
            return false;

        foreach (var gd in TripItemsView.GroupDescriptions)
            if (gd is PropertyGroupDescription pgd)
                if (pgd.PropertyName == propertyName)
                {
                    if (hasConverter) return pgd.Converter is DateMonthGroupConverter;
                    return true;
                }

        return false;
    }

    private void UpdatePaginationButtons()
    {
        PaginationButtons.Clear();

        var startPage = Math.Max(1, CurrentPage - 2);
        var endPage = Math.Min(TotalPages, startPage + 4);

        if (endPage - startPage < 4 && startPage > 1) startPage = Math.Max(1, endPage - 4);

        for (var i = startPage; i <= endPage; i++) PaginationButtons.Add(i);
    }

    [RelayCommand]
    public async Task PreviousPageCommand()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadTripItemsAsync();
        }
    }

    [RelayCommand]
    public async Task NextPageCommand()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadTripItemsAsync();
        }
    }

    [RelayCommand]
    public async Task GoToPageCommand(int page)
    {
        if (page >= 1 && page <= TotalPages && page != CurrentPage)
        {
            CurrentPage = page;
            await LoadTripItemsAsync();
        }
    }

    [RelayCommand]
    public void ToggleTripListCommand()
    {
        IsTripListExpanded = !IsTripListExpanded;
    }

    [RelayCommand]
    public void ViewTripCommand(TripItem trip)
    {
        MessageBoxWindow.Show("查看功能暂未实现");
    }

    [RelayCommand]
    public async Task EditTripCommand(TripItem trip)
    {
        if (trip == null) return;

        try
        {
            // 查找车票ID
            var rideId = await _trainRideRepository.FindTrainRideIdAsync(
                trip.TrainNo,
                trip.DepartStation,
                trip.ArriveStation,
                trip.DepartDate,
                trip.DepartTime
            );

            if (!rideId.HasValue)
            {
                MessageBoxWindow.Show("未找到对应的车票记录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 使用单例模式打开编辑窗口
            var editWindow = EditTrainTicketWindow.GetInstance(rideId.Value);
            editWindow.Owner = Application.Current.MainWindow;
            if (!editWindow.IsVisible)
            {
                editWindow.ShowDialog();
            }

            // 刷新列表
            await LoadTripItemsAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("TripListViewModel", $"打开编辑窗口失败: {ex.Message}");
            MessageBoxWindow.Show($"打开编辑窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task DeleteTripCommand(TripItem trip)
    {
        if (trip != null)
        {
            var itemName = $"车次 {trip.TrainNo} ({trip.DepartStation} → {trip.ArriveStation}) 的行程";
            if (!_confirmationService.ConfirmDelete(itemName)) return;

            try
            {
                var rideId = await _trainRideRepository.FindTrainRideIdAsync(
                    trip.TrainNo,
                    trip.DepartStation,
                    trip.ArriveStation,
                    trip.DepartDate,
                    trip.DepartTime
                );

                if (rideId.HasValue)
                {
                    await _trainRideRepository.DeleteTrainRideAsync(rideId.Value);
                    _logService.Info("TripListViewModel",
                        $"删除行程: {trip.TrainNo} {trip.DepartStation}->{trip.ArriveStation}");
                }
            }
            catch (Exception ex)
            {
                _logService.Error("TripListViewModel", $"删除行程失败: {ex.Message}");
                MessageBoxWindow.Show(
                    Application.Current.MainWindow,
                    $"删除失败: {ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            TripItems.Remove(trip);
            TotalItems--;
            await LoadTripItemsAsync();
        }
    }

    [RelayCommand]
    public async Task RefundTripCommand(TripItem trip)
    {
        Debug.WriteLine($"[RefundTripCommand] 被调用，trip={trip?.TrainNo}, Status={trip?.StatusDisplay}");
        _logService?.Info("TripListViewModel", $"[RefundTripCommand] 被调用，trip={trip?.TrainNo}");

        if (trip == null)
        {
            Debug.WriteLine("[RefundTripCommand] trip 为 null，直接返回");
            return;
        }

        // 检查状态（只能退未出行的票）
        if (trip.StatusDisplay != "未出行")
        {
            MessageBoxWindow.Show("只能退未出行的车票", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 确认对话框
        var result = MessageBoxWindow.Show(
            $"确定要退掉车次 {trip.TrainNo} 吗？\n\n" +
            $"出发：{trip.DepartStation} {trip.DepartDate} {trip.DepartTime}\n\n" +
            "退票后该车票状态将变为\"已退票\"",
            "确认退票",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            var rideId = await _trainRideRepository.FindTrainRideIdAsync(
                trip.TrainNo,
                trip.DepartStation,
                trip.ArriveStation,
                trip.DepartDate,
                trip.DepartTime
            );

            if (rideId.HasValue)
            {
                await _trainRideRepository.UpdateStatusAsync(rideId.Value, 3); // 3=已退票
                _logService.Info("TripListViewModel", $"退票成功：{trip.TrainNo}");
                await LoadTripItemsAsync();
                MessageBoxWindow.Show("退票成功！", "成功");
            }
        }
        catch (Exception ex)
        {
            _logService.Error("TripListViewModel", $"退票失败：{ex.Message}");
            MessageBoxWindow.Show($"退票失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task RescheduleTripCommand(TripItem trip)
    {
        Debug.WriteLine($"[RescheduleTripCommand] 被调用，trip={trip?.TrainNo}, Status={trip?.StatusDisplay}");
        _logService?.Info("TripListViewModel", $"[RescheduleTripCommand] 被调用，trip={trip?.TrainNo}");

        if (trip == null)
        {
            Debug.WriteLine("[RescheduleTripCommand] trip 为 null，直接返回");
            return;
        }

        // 检查状态（只能改未出行的票）
        if (trip.StatusDisplay != "未出行")
        {
            MessageBoxWindow.Show("只能改签未出行的车票", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 获取原票ID
        var rideId = await _trainRideRepository.FindTrainRideIdAsync(
            trip.TrainNo,
            trip.DepartStation,
            trip.ArriveStation,
            trip.DepartDate,
            trip.DepartTime
        );

        if (!rideId.HasValue)
        {
            MessageBoxWindow.Show("无法找到原车票记录", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 选择改签类型
        var rescheduleTypeResult = MessageBoxWindow.Show(
            $"请选择改签类型：\n\n" +
            $"车次：{trip.TrainNo}\n" +
            $"出发：{trip.DepartStation} {trip.DepartDate} {trip.DepartTime}\n" +
            $"到达：{trip.ArriveStation}\n\n" +
            "点击\"是\"：变更到站（可修改到达站）\n" +
            "点击\"否\"：普通改签（保持到达站不变）",
            "选择改签类型",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            "变更到站",
            "普通改签");

        if (rescheduleTypeResult == MessageBoxResult.Cancel)
            return;

        var isChangeDestination = rescheduleTypeResult == MessageBoxResult.Yes;

        // 确认改签
        var confirmResult = MessageBoxWindow.Show(
            $"确定要改签车次 {trip.TrainNo} 吗？\n\n" +
            $"改签类型：{(isChangeDestination ? "变更到站" : "普通改签")}\n\n" +
            "改签后：\n" +
            "1. 原票状态将变为\"已改签\"\n" +
            "2. 自动打开新增车票窗口录入新车票",
            "确认改签",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.Yes)
            return;

        try
        {
            // 更新原票状态为"已改签"
            await _trainRideRepository.UpdateStatusAsync(rideId.Value, 2); // 2=已改签
            _logService.Info("TripListViewModel", $"改签原票：{trip.TrainNo}，类型：{(isChangeDestination ? "变更到站" : "普通改签")}");

            // 打开新增车票窗口（改签模式）- 去掉"站"字后缀
            var departStation = trip.DepartStation?.TrimEnd('站') ?? string.Empty;
            var arriveStation = trip.ArriveStation?.TrimEnd('站') ?? string.Empty;
            var addWindow =
                AddTrainTicketWindow.CreateRescheduleWindow(departStation, arriveStation, isChangeDestination);
            addWindow.Owner = Application.Current.MainWindow;

            // 设置改签相关的额外信息
            if (addWindow.DataContext is AddTrainTicketViewModel viewModel)
            {
                viewModel.OriginalTicketId = rideId.Value;
                viewModel.OriginalTicketStatus = trip.StatusDisplay;
                viewModel.IsRescheduleTypeChangeDestination = isChangeDestination;
            }

            addWindow.ShowDialog();

            // 如果用户关闭窗口且没有保存新车票，还原旧票状态
            if (addWindow.DataContext is AddTrainTicketViewModel vm && !vm.IsSaved)
            {
                await _trainRideRepository.UpdateStatusAsync(rideId.Value, 0); // 0=未出行
                _logService.Info("TripListViewModel", $"改签取消，还原原票状态：{trip.TrainNo}");
                MessageBoxWindow.Show("改签已取消，原车票状态已还原");
            }

            // 刷新列表
            await LoadTripItemsAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("TripListViewModel", $"改签失败：{ex.Message}");
            MessageBoxWindow.Show($"改签失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void HandleTripDoubleClick(TripItem trip)
    {
        if (trip == null) return;

        var config = _generalSettingsService.Config;
        var action = config.DoubleClickAction;

        switch (action)
        {
            case DoubleClickActionOption.Edit:
                EditTripCommand(trip);
                break;
            case DoubleClickActionOption.Preview:
                ShowTicketPreview(trip);
                break;
            case DoubleClickActionOption.Map:
                ShowTripOnMap(trip);
                break;
            default:
                EditTripCommand(trip);
                break;
        }
    }

    private void ShowTicketPreview(TripItem trip)
    {
        var previewWindow = new TicketPreviewWindow(trip);
        previewWindow.ShowDialog();
    }

    private void ShowTripOnMap(TripItem trip)
    {
        try
        {
            // 使用单例模式打开地图窗口，并传递选中的行程ID
            var mapWindow = MapWindow.GetInstance(trip.DatabaseId.ToString());
            
            if (!mapWindow.IsVisible)
            {
                mapWindow.Show();
            }
        }
        catch (Exception ex)
        {
            _logService?.Error("TripListViewModel", $"打开地图窗口失败: {ex.Message}");
            MessageBoxWindow.Show($"打开地图窗口失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     批量修改状态命令
    /// </summary>
    [RelayCommand]
    public async Task BatchUpdateStatusAsync()
    {
        _logService?.Info("TripListViewModel", "BatchUpdateStatusAsync 开始执行");
        try
        {
            // 1. 获取选中的车票
            var selectedItems = GetSelectedTripItems();
            _logService?.Info("TripListViewModel", $"获取到 {selectedItems.Count} 个选中项");

            if (selectedItems.Count == 0)
            {
                _logService?.Info("TripListViewModel", "没有选中任何车票，显示提示");
                MessageBoxWindow.Show("请先选择要修改状态的车票");
                return;
            }

            // 2. 打开批量修改状态对话框
            _logService?.Info("TripListViewModel", "正在创建 BatchUpdateStatusWindow");
            var dialog = new BatchUpdateStatusWindow(selectedItems);
            dialog.Owner = Application.Current.MainWindow;
            _logService?.Info("TripListViewModel", "对话框已创建，准备显示");

            if (dialog.ShowDialog() != true || !dialog.IsConfirmed)
                return;

            // 3. 检查是否有可编辑的车票
            if (dialog.EditableTickets.Count == 0)
            {
                MessageBoxWindow.Show("选中的车票均为已改签或已退票状态，无法批量修改。\n" +
                                      "如需改签请使用改签功能，如需退票请使用退票功能。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var targetStatus = dialog.SelectedStatus;
            var statusText = targetStatus == 0 ? "未出行" : "已完成";
            var editableCount = dialog.EditableTickets.Count;
            var skippedCount = dialog.SkippedTickets.Count;

            // 4. 最终确认对话框
            var confirmMessage = $"确定要将 {editableCount} 张车票的状态修改为\"{statusText}\"吗？";
            if (skippedCount > 0) confirmMessage += $"\n\n（有 {skippedCount} 张已改签/已退票的车票将被跳过）";

            var result = MessageBoxWindow.Show(confirmMessage, "确认批量修改",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // 5. 执行批量更新（使用数据库真实ID）
            var ids = dialog.EditableTickets.Select(t => t.DatabaseId).ToList();
            var affectedCount = await _trainRideRepository.BatchUpdateStatusAsync(ids, targetStatus);

            _logService.Info("TripListViewModel",
                $"批量修改状态成功：{affectedCount} 张车票，目标状态：{statusText}");

            // 6. 刷新列表
            await LoadTripItemsAsync();

            // 7. 显示结果
            var successMessage = $"成功修改 {affectedCount} 张车票的状态为\"{statusText}\"";
            if (skippedCount > 0) successMessage += $"\n跳过 {skippedCount} 张已改签/已退票的车票";

            MessageBoxWindow.Show(successMessage, "成功");
        }
        catch (Exception ex)
        {
            _logService.Error("TripListViewModel", $"批量修改状态失败：{ex.Message}");
            MessageBoxWindow.Show($"批量修改状态失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     批量更改标签命令
    /// </summary>
    [RelayCommand]
    public async Task BatchUpdateTagAsync()
    {
        _logService?.Info("TripListViewModel", "BatchUpdateTagAsync 开始执行");
        try
        {
            // 1. 获取选中的车票
            var selectedItems = GetSelectedTripItems();
            _logService?.Info("TripListViewModel", $"获取到 {selectedItems.Count} 个选中项");

            if (selectedItems.Count == 0)
            {
                _logService?.Info("TripListViewModel", "没有选中任何车票，显示提示");
                MessageBoxWindow.Show("请先选择要更改标签的车票");
                return;
            }

            // 2. 获取所有可用标签
            var allTags = (await _ticketTagRepository.GetAllTagsAsync()).ToList();
            if (allTags.Count == 0)
            {
                MessageBoxWindow.Show("系统中没有可用的标签，请先创建标签");
                return;
            }

            // 3. 打开批量更改标签对话框
            _logService?.Info("TripListViewModel", "正在创建 BatchUpdateTagWindow");
            var dialog = new BatchUpdateTagWindow(selectedItems, allTags);
            dialog.Owner = Application.Current.MainWindow;
            _logService?.Info("TripListViewModel", "对话框已创建，准备显示");

            if (dialog.ShowDialog() != true || !dialog.IsConfirmed)
                return;

            // 4. 检查是否选择了标签
            if (!dialog.SelectedTagId.HasValue)
            {
                MessageBoxWindow.Show("请选择一个标签", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedTagId = dialog.SelectedTagId.Value;
            var selectedTagName = allTags.FirstOrDefault(t => t.Id == selectedTagId)?.Name ?? "未知标签";
            var isAppendMode = dialog.IsAppendMode;
            var modeText = isAppendMode ? "追加" : "替换";

            // 5. 最终确认对话框
            var confirmMessage = $"确定要{modeText} {selectedItems.Count} 张车票的标签为\"{selectedTagName}\"吗？";
            if (isAppendMode)
                confirmMessage += "\n\n（将保留现有标签并添加新标签）";
            else
                confirmMessage += "\n\n（将清除现有标签并设置为新标签）";

            var result = MessageBoxWindow.Show(confirmMessage, "确认批量更改标签",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // 6. 执行批量更新（为每个车票设置标签，使用数据库真实ID）
            var successCount = 0;
            foreach (var ticket in selectedItems)
                try
                {
                    // 使用数据库真实ID
                    var rideId = ticket.DatabaseId;

                    if (rideId > 0)
                    {
                        if (isAppendMode)
                            // 追加模式：直接添加标签
                            await _ticketTagRepository.AddTagToRideAsync(rideId, selectedTagId);
                        else
                            // 替换模式：先清空再添加
                            await _ticketTagRepository.SetTagsToRideAsync(rideId, new List<int> { selectedTagId });
                        successCount++;
                    }
                    else
                    {
                        _logService?.Error("TripListViewModel", $"未找到车票 {ticket.TrainNo} 对应的数据库ID");
                    }
                }
                catch (Exception ex)
                {
                    _logService?.Error("TripListViewModel", $"为车票 {ticket.TrainNo} 设置标签失败：{ex.Message}");
                }

            _logService?.Info("TripListViewModel",
                $"批量{modeText}标签成功：{successCount}/{selectedItems.Count} 张车票，标签：{selectedTagName}");

            // 7. 刷新列表
            await LoadTripItemsAsync();

            // 8. 显示结果
            MessageBoxWindow.Show($"成功{modeText} {successCount} 张车票的标签为\"{selectedTagName}\"",
                "成功");
        }
        catch (Exception ex)
        {
            _logService?.Error("TripListViewModel", $"批量更改标签失败：{ex.Message}");
            MessageBoxWindow.Show($"批量更改标签失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     批量删除命令
    /// </summary>
    [RelayCommand]
    public async Task BatchDeleteAsync()
    {
        _logService?.Info("TripListViewModel", "BatchDeleteAsync 开始执行");
        try
        {
            // 1. 获取选中的车票
            var selectedItems = GetSelectedTripItems();
            _logService?.Info("TripListViewModel", $"获取到 {selectedItems.Count} 个选中项");

            if (selectedItems.Count == 0)
            {
                _logService?.Info("TripListViewModel", "没有选中任何车票，显示提示");
                MessageBoxWindow.Show("请先选择要删除的车票");
                return;
            }

            // 2. 确认对话框
            var confirmMessage = $"确定要删除选中的 {selectedItems.Count} 张车票吗？\n\n此操作不可恢复！";
            var result = MessageBoxWindow.Show(confirmMessage, "确认批量删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            // 3. 执行批量删除
            var successCount = 0;
            var failCount = 0;
            foreach (var ticket in selectedItems)
                try
                {
                    // 使用数据库真实ID
                    var rideId = ticket.DatabaseId;

                    if (rideId > 0)
                    {
                        await _trainRideRepository.DeleteTrainRideAsync(rideId);
                        _logService?.Info("TripListViewModel",
                            $"删除行程: {ticket.TrainNo} {ticket.DepartStation}->{ticket.ArriveStation}");
                        successCount++;
                    }
                    else
                    {
                        _logService?.Error("TripListViewModel", $"未找到车票 {ticket.TrainNo} 对应的数据库ID");
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logService?.Error("TripListViewModel", $"删除车票 {ticket.TrainNo} 失败：{ex.Message}");
                    failCount++;
                }

            // 4. 刷新列表
            await LoadTripItemsAsync();

            // 5. 显示结果
            var message = $"批量删除完成！\n成功：{successCount} 张\n失败：{failCount} 张";
            MessageBoxWindow.Show(message, "删除完成", MessageBoxButton.OK,
                failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

            _logService?.Info("TripListViewModel", $"批量删除完成：成功 {successCount} 张，失败 {failCount} 张");
        }
        catch (Exception ex)
        {
            _logService?.Error("TripListViewModel", $"批量删除失败：{ex.Message}");
            MessageBoxWindow.Show($"批量删除失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task SaveDataSortInfoAsync(string sortColumn, ListSortDirection direction)
    {
        try
        {
            _currentSortColumn = sortColumn;
            _currentSortDesc = direction == ListSortDirection.Descending;

            var config = _uiSettingsService.Config;
            if (config.RememberDataSort)
            {
                config.LastSortColumn = sortColumn;
                config.LastSortDirection = direction == ListSortDirection.Ascending ? "Ascending" : "Descending";
                _uiSettingsService.SaveConfig(config);
            }

            await LoadTripItemsAsync();

            if (Application.Current.MainWindow is MainWindow mainWindow)
                mainWindow.UpdateColumnSortDirection(sortColumn, _currentSortDesc);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TripListViewModel] 保存数据排序信息失败: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task OnExportTripListAsync()
    {
        try
        {
            _exportSettingsService.RefreshConfig();
            var exportConfig = _exportSettingsService.Config;

            List<TripItem> dataToExport;
            var selectedItems = GetSelectedTripItems();
            var isExportSelected = selectedItems.Count > 0;

            if (isExportSelected)
                dataToExport = selectedItems;
            else
                dataToExport = await LoadAllTripItemsForExportAsync();

            if (dataToExport.Count == 0)
            {
                MessageBoxWindow.Show("没有可导出的数据");
                return;
            }

            if (exportConfig.DefaultFormat == ExportFormatOption.Image)
            {
                var dialogResult = MessageBoxWindow.Show(
                    "图片导出功能需要在车票预览中使用。\n\n是否跳转到车票预览功能？",
                    "图片导出",
                    MessageBoxButton.YesNo);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    _logService.Info("TripListViewModel", "用户选择跳转到车票预览功能进行图片导出");
                    MessageBoxWindow.Show(
                        "车票预览图片导出功能即将推出！\n\n您可以通过以下方式预览车票：\n1. 在行程列表中双击某条记录\n2. 或选中记录后按 Ctrl+P",
                        "功能预告");
                }

                return;
            }

            // 确定车次号：如果所有记录都是同一车次，使用该车次；否则显示"多车次"
            var trainNo = GetExportTrainNo(dataToExport);
            var fileName =
                _exportService.GenerateFileName(exportConfig.FileNameTemplate, exportConfig.DefaultFormat, trainNo);
            string? exportFilePath = null;

            // 如果设置了默认保存路径，直接使用；否则弹出对话框让用户选择
            if (!string.IsNullOrEmpty(exportConfig.DefaultSavePath))
            {
                // 使用默认路径
                exportFilePath = Path.Combine(exportConfig.DefaultSavePath, fileName);
            }
            else
            {
                // 未设置默认路径，弹出保存对话框
                var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    InitialDirectory = defaultPath,
                    Filter = exportConfig.DefaultFormat switch
                    {
                        ExportFormatOption.Excel => "Excel文件 (*.xlsx)|*.xlsx",
                        ExportFormatOption.Csv => "CSV文件 (*.csv)|*.csv",
                        ExportFormatOption.Pdf => "PDF文件 (*.pdf)|*.pdf",
                        _ => "所有文件 (*.*)|*.*"
                    },
                    DefaultExt = exportConfig.DefaultFormat switch
                    {
                        ExportFormatOption.Excel => "xlsx",
                        ExportFormatOption.Csv => "csv",
                        ExportFormatOption.Pdf => "pdf",
                        _ => "*"
                    }
                };

                if (saveFileDialog.ShowDialog() != true) return; // 用户取消了对话框
                exportFilePath = saveFileDialog.FileName;
            }

            if (!string.IsNullOrEmpty(exportFilePath))
            {
                var trainRides = dataToExport.Select(t => new TrainRideInfo
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber ?? "",
                    TrainNo = t.TrainNo ?? "",
                    DepartStation = t.DepartStation ?? "",
                    ArriveStation = t.ArriveStation ?? "",
                    DepartStationPinyin = t.DepartStationPinyin ?? "",
                    ArriveStationPinyin = t.ArriveStationPinyin ?? "",
                    DepartDate = t.DepartDate ?? "",
                    DepartTime = t.DepartTime ?? "",
                    CoachNo = t.CoachNo ?? "",
                    SeatNo = t.SeatNo ?? "",
                    SeatType = t.SeatType ?? "",
                    Money = decimal.TryParse(t.Money, out var money) ? money : 0,
                    CheckInLocation = t.CheckInLocation ?? "",
                    AdditionalInfo = t.AdditionalInfo ?? "",
                    TicketPurpose = t.TicketPurpose ?? "",
                    TicketModificationType = t.TicketModificationType ?? "",
                    Hint = t.Hint ?? "",
                    Status = t.Status,
                    Tags = t.Tags ?? new List<TicketTag>()
                }).ToList();

                _logService.Info("TripListViewModel",
                    $"准备导出 {trainRides.Count} 条记录到PDF，每页 {exportConfig.PdfRowsPerPage} 行");

                // 显示进度提示
                var progressWindow = MessageBoxWindow.ShowProgress($"正在导出 {trainRides.Count} 条记录...", "正在导出");

                ExportResult result;
                try
                {
                    // 如果启用了分组导出，使用分组导出方法
                    if (exportConfig.EnableGroupExport)
                    {
                        var groupOption = _uiSettingsService.Config.DefaultGroup;
                        result = await _exportService.ExportGroupedAsync(exportFilePath, exportConfig.DefaultFormat,
                            trainRides, groupOption, exportConfig);
                    }
                    else
                    {
                        result = await _exportService.ExportAsync(exportFilePath, exportConfig.DefaultFormat,
                            trainRides, exportConfig);
                    }
                }
                finally
                {
                    progressWindow.Close();
                }

                if (result.Success)
                {
                    if (exportConfig.ShowSuccessMessage)
                    {
                        var exportTypeText = isExportSelected ? "选中数据" : "全部数据";
                        MessageBoxWindow.Show(
                            $"导出成功！\n导出类型：{exportTypeText}\n文件路径：{result.FilePath}\n导出记录数：{result.RecordCount}条",
                            "导出完成");
                    }

                    if (exportConfig.OpenAfterExport && File.Exists(result.FilePath))
                        Process.Start(new ProcessStartInfo(result.FilePath) { UseShellExecute = true });

                    var logText = isExportSelected ? "选中数据" : "全部数据";
                    _logService.Info("TripListViewModel",
                        $"行程列表导出成功：{result.FilePath}，类型：{logText}，记录数：{result.RecordCount}");
                }
                else
                {
                    MessageBoxWindow.Show($"导出失败：{result.Message}", "导出错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    _logService.Error("TripListViewModel", $"行程列表导出失败：{result.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show($"导出过程中发生错误：{ex.Message}", "导出错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _logService.Error("TripListViewModel", $"行程列表导出异常：{ex.Message}");
        }
    }

    public List<TripItem> GetSelectedTripItems()
    {
        var selectedItems = new List<TripItem>();
        if (Application.Current.MainWindow is MainWindow mainWindow) selectedItems = mainWindow.GetSelectedTripItems();
        return selectedItems;
    }

    private async Task<List<TripItem>> LoadAllTripItemsForExportAsync()
    {
        try
        {
            _logService.Info("TripListViewModel", "开始从数据库加载全部行程数据用于导出");
            var allTrainRides = await _trainRideRepository.GetAllTrainRidesAsync();

            var tripItems = new List<TripItem>();
            var id = 1;
            foreach (var ride in allTrainRides)
                tripItems.Add(new TripItem
                {
                    Id = id++,
                    DatabaseId = ride.Id, // 保存数据库真实ID
                    TrainNo = ride.TrainNo,
                    DepartStation = ride.DepartStation,
                    ArriveStation = ride.ArriveStation,
                    DepartDate = ride.DepartDate,
                    DepartTime = ride.DepartTime,
                    SeatType = ride.SeatType,
                    Money = ride.Money.ToString(),
                    Status = ride.Status,
                    Tags = new List<TicketTag>(),
                    CoachNo = ride.CoachNo,
                    SeatNo = ride.SeatNo,
                    TicketNumber = ride.TicketNumber,
                    DepartStationPinyin = ride.DepartStationPinyin,
                    ArriveStationPinyin = ride.ArriveStationPinyin,
                    CheckInLocation = ride.CheckInLocation,
                    Hint = ride.Hint,
                    AdditionalInfo = ride.AdditionalInfo,
                    TicketPurpose = ride.TicketPurpose,
                    TicketModificationType = ride.TicketModificationType,
                    TicketType = ConvertTicketTypeFlags(ride.TicketTypeFlags),
                    PaymentChannel = ConvertPaymentChannelFlags(ride.PaymentChannelFlags)
                });

            _logService.Info("TripListViewModel", $"成功加载 {tripItems.Count} 条行程数据用于导出");
            return tripItems;
        }
        catch (Exception ex)
        {
            _logService.Error("TripListViewModel", $"加载全部行程数据失败：{ex.Message}");
            return TripItems.ToList();
        }
    }

    /// <summary>
    ///     获取导出用的车次号
    ///     如果所有记录都是同一车次，返回该车次；否则返回"多车次"
    /// </summary>
    private string GetExportTrainNo(List<TripItem> items)
    {
        if (items.Count == 0)
            return "多车次";

        var firstTrainNo = items[0].TrainNo;
        var allSameTrainNo = items.All(item => item.TrainNo == firstTrainNo);

        return allSameTrainNo ? firstTrainNo ?? "多车次" : "多车次";
    }

    /// <summary>
    ///     将票种类型标志转换为可读字符串
    /// </summary>
    private string ConvertTicketTypeFlags(int flags)
    {
        if (flags == 0) return string.Empty;

        var types = new List<string>();
        if ((flags & 1) != 0) types.Add("学生票");
        if ((flags & 2) != 0) types.Add("优惠票");
        if ((flags & 4) != 0) types.Add("网络售票");
        if ((flags & 8) != 0) types.Add("儿童票");

        return string.Join(", ", types);
    }

    /// <summary>
    ///     将支付渠道标志转换为可读字符串
    /// </summary>
    private string ConvertPaymentChannelFlags(int flags)
    {
        if (flags == 0) return string.Empty;

        var channels = new List<string>();
        if ((flags & 1) != 0) channels.Add("支付宝");
        if ((flags & 2) != 0) channels.Add("微信");
        if ((flags & 4) != 0) channels.Add("农业银行");
        if ((flags & 8) != 0) channels.Add("建设银行");
        if ((flags & 16) != 0) channels.Add("工商银行");
        if ((flags & 32) != 0) channels.Add("交通银行");
        if ((flags & 64) != 0) channels.Add("招商银行");
        if ((flags & 128) != 0) channels.Add("邮储银行");
        if ((flags & 256) != 0) channels.Add("中国银行");

        return string.Join(", ", channels);
    }
}