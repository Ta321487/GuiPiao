using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using TripItem = GuiPiao.Model.TripItem;

namespace GuiPiao.ViewModel;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly GeneralSettingsService _generalSettingsService;
    private readonly LogService _logService;
    private bool _isDisposed;

    [ObservableProperty] private string _statusMessage = "就绪";

    private DispatcherTimer? _statusResetTimer;

    public MainViewModel()
    {
        Debug.WriteLine("[MainViewModel] 开始初始化");

        // 初始化子 ViewModel
        Layout = new LayoutViewModel();
        TripList = new TripListViewModel();
        Dashboard = new DashboardViewModel();
        LogPanel = new LogPanelViewModel();
        Menu = new MenuViewModel();
        QuickActions = new QuickActionsViewModel();
        SearchPanel = new SearchPanelViewModel();

        _generalSettingsService = new GeneralSettingsService();
        _logService = ServiceManager.Instance.LogService;

        // 订阅子 ViewModel 的属性变更事件，转发到自身
        SubscribeToChildViewModels();

        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            Debug.WriteLine("[MainViewModel] 设计模式，跳过初始化");
            return;
        }

        // 订阅设置变更消息
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            if (message.SettingType == "General") _ = TripList.LoadTripItemsAsync();
        });

        // 订阅状态栏消息
        WeakReferenceMessenger.Default.Register<StatusMessageMessage>(this, (recipient, message) =>
        {
            if (message.AutoReset)
                SetTemporaryStatus(message.Message, message.ResetDelaySeconds);
            else
                StatusMessage = message.Message;
        });

        // 订阅批量修改状态消息
        WeakReferenceMessenger.Default.Register<BatchUpdateStatusMessage>(this,
            async (recipient, message) => { await TripList.BatchUpdateStatusAsync(); });

        // 注意：ApplyStartupPageSetting 在 MainWindow Loaded 事件中调用
        // 避免在窗口未显示时打开子窗口导致 Owner 属性设置失败

        // 根据配置决定是否启动时自动刷新行程数据
        if (_generalSettingsService.Config.AutoRefreshOnStartup) _ = TripList.LoadTripItemsAsync();

        Debug.WriteLine("[MainViewModel] 初始化完成");
    }

    #region 转发属性 - 日志面板相关

    public ObservableCollection<LogItem> LogItems => LogPanel.LogItems;

    #endregion

    public UISettingsService UISettingsService => Layout.UISettingsService;

    #region IDisposable 实现

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // 释放子 ViewModel
        Layout?.Dispose();
        TripList?.Dispose();
        Dashboard?.Dispose();
        LogPanel?.Dispose();

        // 取消 Messenger 订阅
        WeakReferenceMessenger.Default.UnregisterAll(this);

        // 停止状态栏定时器
        _statusResetTimer?.Stop();
        _statusResetTimer = null;

        Debug.WriteLine("[MainViewModel] 资源已释放");
    }

    #endregion

    /// <summary>
    ///     设置临时状态消息，2秒后自动恢复为"就绪"
    /// </summary>
    private void SetTemporaryStatus(string message, int delaySeconds = 2)
    {
        StatusMessage = message;

        // 复用定时器实例，避免频繁创建
        if (_statusResetTimer == null)
        {
            _statusResetTimer = new DispatcherTimer();
            _statusResetTimer.Tick += (s, e) =>
            {
                StatusMessage = "就绪";
                _statusResetTimer?.Stop();
            };
        }
        else
        {
            _statusResetTimer.Stop();
        }

        _statusResetTimer.Interval = TimeSpan.FromSeconds(delaySeconds);
        _statusResetTimer.Start();
    }

    private void SubscribeToChildViewModels()
    {
        // 订阅 Layout 的属性变更
        Layout.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            // 同时触发转发属性的变更通知
            switch (e.PropertyName)
            {
                case nameof(Layout.LeftPanelWidth):
                    OnPropertyChanged(nameof(LeftPanelWidth));
                    OnPropertyChanged(nameof(LeftColumnWidth));
                    break;
                case nameof(Layout.LeftPanelLocked):
                    OnPropertyChanged(nameof(LeftPanelLocked));
                    OnPropertyChanged(nameof(LeftSplitterVisible));
                    OnPropertyChanged(nameof(LeftColumnWidth));
                    OnPropertyChanged(nameof(LeftSplitterWidth));
                    break;
                case nameof(Layout.RightPanelWidth):
                    OnPropertyChanged(nameof(RightPanelWidth));
                    OnPropertyChanged(nameof(RightColumnWidth));
                    break;
                case nameof(Layout.RightPanelLocked):
                    OnPropertyChanged(nameof(RightPanelLocked));
                    OnPropertyChanged(nameof(RightSplitterVisible));
                    OnPropertyChanged(nameof(RightColumnWidth));
                    OnPropertyChanged(nameof(RightSplitterWidth));
                    break;
                case nameof(Layout.BottomPanelHeight):
                    OnPropertyChanged(nameof(BottomPanelHeight));
                    OnPropertyChanged(nameof(BottomRowHeight));
                    break;
                case nameof(Layout.BottomPanelLocked):
                    OnPropertyChanged(nameof(BottomPanelLocked));
                    OnPropertyChanged(nameof(BottomSplitterVisible));
                    OnPropertyChanged(nameof(BottomRowHeight));
                    OnPropertyChanged(nameof(BottomSplitterHeight));
                    break;
                case nameof(Layout.ScrollbarStyle):
                    OnPropertyChanged(nameof(ScrollbarStyle));
                    break;
                case nameof(Layout.ShowActionButtonsOnHover):
                    OnPropertyChanged(nameof(ShowActionButtonsOnHover));
                    break;
                case nameof(Layout.ShowTimestamp):
                    OnPropertyChanged(nameof(ShowTimestamp));
                    break;
                case nameof(Layout.ShowModuleSource):
                    OnPropertyChanged(nameof(ShowModuleSource));
                    break;
                case nameof(Layout.LogRowHeight):
                    OnPropertyChanged(nameof(LogRowHeight));
                    OnPropertyChanged(nameof(LogRowHeightValue));
                    break;
            }
        };

        // 订阅 TripList 的属性变更
        TripList.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            // 同时触发转发属性的变更通知
            switch (e.PropertyName)
            {
                case nameof(TripList.TripItems):
                    OnPropertyChanged(nameof(TripItems));
                    SetTemporaryStatus($"行程列表已更新，共 {TripList.TotalItems} 条记录");
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
                    SetTemporaryStatus(TripList.IsTripListExpanded ? "行程列表已展开" : "行程列表已收起");
                    break;
                case nameof(TripList.IsOperationButtonsVisible):
                    OnPropertyChanged(nameof(IsOperationButtonsVisible));
                    break;
            }
        };

        // 订阅 Dashboard 的属性变更
        Dashboard.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            // 同时触发转发属性的变更通知
            switch (e.PropertyName)
            {
                case nameof(Dashboard.DashboardCharts):
                    OnPropertyChanged(nameof(DashboardCharts));
                    OnPropertyChanged(nameof(HasDashboardCharts));
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

        // 订阅 LogPanel 的属性变更
        LogPanel.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            // 同时触发转发属性的变更通知
            if (e.PropertyName == nameof(LogPanel.LogItems)) OnPropertyChanged(nameof(LogItems));
        };

        // 订阅 SearchPanel 的属性变更
        SearchPanel.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            // 同时触发转发属性的变更通知
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

        // 订阅 QuickActions 的命令执行（通过消息或属性变更）
        QuickActions.PropertyChanged += (s, e) =>
        {
            // 快捷功能区操作状态更新
            switch (e.PropertyName)
            {
                case nameof(QuickActions.NewTicketRecordCommand):
                    SetTemporaryStatus("正在打开新增票务记录...");
                    break;
            }
        };

        // 订阅 Dashboard 的属性变更
        Dashboard.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(Dashboard.DashboardCharts):
                    SetTemporaryStatus(Dashboard.HasDashboardCharts
                        ? $"仪表盘已加载 {Dashboard.DashboardCharts.Count} 个图表"
                        : "仪表盘暂无图表");
                    break;
            }
        };

        // 订阅 LogPanel 的属性变更
        LogPanel.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(LogPanel.LogItems):
                    SetTemporaryStatus($"日志面板已更新，共 {LogPanel.LogItems.Count} 条日志");
                    break;
            }
        };
    }

    /// <summary>
    ///     应用启动页面设置
    /// </summary>
    public void ApplyStartupPageSetting()
    {
        var startupPage = _generalSettingsService.Config.StartupPage;
        Debug.WriteLine($"[MainViewModel] ApplyStartupPageSetting: startupPage={startupPage}");
        switch (startupPage)
        {
            case StartupPageOption.MainList:
                Debug.WriteLine("[MainViewModel] 启动页面: MainList");
                break;
            case StartupPageOption.Map:
                Debug.WriteLine("[MainViewModel] 启动页面: Map");
                QuickActions.OpenTicketMapCommand();
                break;
            case StartupPageOption.LastPage:
                Debug.WriteLine("[MainViewModel] 启动页面: LastPage");
                RestoreLastPage();
                break;
        }
    }

    /// <summary>
    ///     恢复上次关闭时的页面
    /// </summary>
    private void RestoreLastPage()
    {
        var lastPage = _generalSettingsService.GetLastPage();
        Debug.WriteLine($"[MainViewModel] RestoreLastPage: lastPage={lastPage}");
        switch (lastPage)
        {
            case LastPageOption.Map:
                Debug.WriteLine("[MainViewModel] 恢复页面: Map");
                QuickActions.OpenTicketMapCommand();
                break;
            case LastPageOption.LogManager:
                Debug.WriteLine("[MainViewModel] 恢复页面: LogManager");
                Menu.OpenLogManager();
                break;
            case LastPageOption.MainList:
            default:
                Debug.WriteLine("[MainViewModel] 恢复页面: MainList");
                break;
        }
    }

    /// <summary>
    ///     保存布局设置到配置文件
    /// </summary>
    public void SaveLayoutSettings()
    {
        Layout.SaveLayoutSettings();
    }

    /// <summary>
    ///     保存数据排序信息到配置文件并重新加载数据（数据库排序）
    /// </summary>
    public async Task SaveDataSortInfoAsync(string sortColumn, ListSortDirection direction)
    {
        await TripList.SaveDataSortInfoAsync(sortColumn, direction);
    }

    /// <summary>
    ///     处理行程列表双击事件
    /// </summary>
    public void HandleTripDoubleClick(TripItem trip)
    {
        TripList.HandleTripDoubleClick(trip);
    }

    /// <summary>
    ///     应用滚动条样式到DataGrid
    /// </summary>
    public void ApplyScrollbarStyle()
    {
        Layout.ApplyScrollbarStyle();
    }

    /// <summary>
    ///     刷新日志列表
    /// </summary>
    public async Task RefreshLogItemsAsync()
    {
        // LogPanelViewModel 自己处理刷新
        await Task.CompletedTask;
    }

    /// <summary>
    ///     获取选中的行程项（通过主窗口的DataGrid）
    /// </summary>
    public List<TripItem> GetSelectedTripItems()
    {
        return TripList.GetSelectedTripItems();
    }

    /// <summary>
    ///     更新主窗口列头排序指示器
    /// </summary>
    public void UpdateColumnSortDirection(string sortColumn, bool isDescending)
    {
        // 这个方法由 MainWindow 调用，不需要在 ViewModel 中实现
    }

    #region 子 ViewModel

    public LayoutViewModel Layout { get; }
    public TripListViewModel TripList { get; }
    public DashboardViewModel Dashboard { get; }
    public LogPanelViewModel LogPanel { get; }
    public MenuViewModel Menu { get; }
    public QuickActionsViewModel QuickActions { get; }
    public SearchPanelViewModel SearchPanel { get; }

    #endregion

    #region 转发属性 - 布局相关

    public int LeftPanelWidth => Layout.LeftPanelWidth;
    public bool LeftPanelLocked => Layout.LeftPanelLocked;
    public int RightPanelWidth => Layout.RightPanelWidth;
    public bool RightPanelLocked => Layout.RightPanelLocked;
    public int BottomPanelHeight => Layout.BottomPanelHeight;
    public bool BottomPanelLocked => Layout.BottomPanelLocked;
    public GridLength LeftColumnWidth => Layout.LeftColumnWidth;
    public GridLength RightColumnWidth => Layout.RightColumnWidth;
    public GridLength BottomRowHeight => Layout.BottomRowHeight;
    public bool LeftSplitterVisible => Layout.LeftSplitterVisible;
    public bool RightSplitterVisible => Layout.RightSplitterVisible;
    public bool BottomSplitterVisible => Layout.BottomSplitterVisible;
    public GridLength LeftSplitterWidth => Layout.LeftSplitterWidth;
    public GridLength RightSplitterWidth => Layout.RightSplitterWidth;
    public GridLength BottomSplitterHeight => Layout.BottomSplitterHeight;
    public string ScrollbarStyle => Layout.ScrollbarStyle;
    public bool ShowActionButtonsOnHover => Layout.ShowActionButtonsOnHover;
    public bool ShowTimestamp => Layout.ShowTimestamp;
    public bool ShowModuleSource => Layout.ShowModuleSource;
    public string LogRowHeight => Layout.LogRowHeight;
    public double LogRowHeightValue => Layout.LogRowHeightValue;

    #endregion

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

    // 车次相关属性
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

    // 出发站联想相关属性和命令
    public ObservableCollection<string> DepartStationSuggestions => SearchPanel.DepartStationSuggestions;

    public bool IsDepartStationDropDownOpen
    {
        get => SearchPanel.IsDepartStationDropDownOpen;
        set => SearchPanel.IsDepartStationDropDownOpen = value;
    }

    public IRelayCommand<string> DepartStationTextChangedCommand => SearchPanel.DepartStationTextChangedCommand;
    public IRelayCommand<string> SelectDepartStationCommand => SearchPanel.SelectDepartStationCommand;

    // 到达站联想相关属性和命令
    public ObservableCollection<string> ArriveStationSuggestions => SearchPanel.ArriveStationSuggestions;

    public bool IsArriveStationDropDownOpen
    {
        get => SearchPanel.IsArriveStationDropDownOpen;
        set => SearchPanel.IsArriveStationDropDownOpen = value;
    }

    public IRelayCommand<string> ArriveStationTextChangedCommand => SearchPanel.ArriveStationTextChangedCommand;
    public IRelayCommand<string> SelectArriveStationCommand => SearchPanel.SelectArriveStationCommand;

    #endregion

    #region 转发命令

    // 菜单命令
    [RelayCommand]
    public async Task StorageMenuCommand(string action)
    {
        await Menu.StorageMenuCommand(action);
    }

    [RelayCommand]
    public void TicketMenuCommand(string action)
    {
        _logService?.Info("MainViewModel", $"TicketMenuCommand 被调用，action={action}");
        if (action == "BatchUpdateStatus")
        {
            _logService?.Info("MainViewModel", "开始调用 BatchUpdateStatusAsync");
            _ = TripList.BatchUpdateStatusAsync();
        }
        else if (action == "BatchUpdateTag")
        {
            _logService?.Info("MainViewModel", "开始调用 BatchUpdateTagAsync");
            _ = TripList.BatchUpdateTagAsync();
        }
        else if (action == "BatchDelete")
        {
            _logService?.Info("MainViewModel", "开始调用 BatchDeleteAsync");
            _ = TripList.BatchDeleteAsync();
        }
        else
        {
            Menu.TicketMenuCommand(action);
        }
    }

    [RelayCommand]
    public async Task TripMenuCommand(string action)
    {
        await Menu.TripMenuCommandAsync(action);
    }

    [RelayCommand]
    public async Task ToolsMenuCommand(string action)
    {
        await Menu.ToolsMenuCommandAsync(action);
    }

    [RelayCommand]
    private void ConfigMenuCommand(string action)
    {
        Menu.ConfigMenuCommand(action);
    }

    [RelayCommand]
    public void HelpMenuCommand(string action)
    {
        Menu.HelpMenuCommand(action);
    }

    [RelayCommand]
    public void OpenLogManager()
    {
        Menu.OpenLogManager();
    }

    [RelayCommand]
    private void OpenLogSettings()
    {
        Menu.OpenLogSettings();
    }

    [RelayCommand]
    public void OpenSettings(string? pageName = null)
    {
        Menu.OpenSettings(pageName);
    }

    // 快捷功能区命令
    [RelayCommand]
    public void NewTicketRecord()
    {
        QuickActions.NewTicketRecordCommand();
    }

    [RelayCommand]
    public void OcrRecognizeTicket()
    {
        QuickActions.OcrRecognizeTicketCommand();
    }

    [RelayCommand]
    public void OpenTicketMap()
    {
        QuickActions.OpenTicketMapCommand();
    }

    [RelayCommand]
    public void TicketPreview()
    {
        QuickActions.TicketPreviewCommand();
    }

    [RelayCommand]
    public void BackupRestoreDatabase()
    {
        QuickActions.BackupRestoreDatabaseCommand();
    }

    [RelayCommand]
    public void SystemConfig()
    {
        QuickActions.SystemConfigCommand();
    }

    // 票务菜单命令 - 编辑和删除选中项
    [RelayCommand]
    private async Task EditSelectedTicket()
    {
        if (TripList.SelectedTripItem != null)
            await TripList.EditTripCommand(TripList.SelectedTripItem);
        else
            MessageBoxWindow.Show(Application.Current.MainWindow, "请先选择一条票务记录");
    }

    [RelayCommand]
    private async Task DeleteSelectedTicket()
    {
        if (TripList.SelectedTripItem != null)
            await TripList.DeleteTripCommand(TripList.SelectedTripItem);
        else
            MessageBoxWindow.Show(Application.Current.MainWindow, "请先选择一条票务记录");
    }

    // 高级检索区命令
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

    // 行程列表命令
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

    // 日志面板命令
    [RelayCommand]
    private async Task ExportLog()
    {
        await LogPanel.ExportLog();
    }

    [RelayCommand]
    private async Task ClearLog()
    {
        await LogPanel.ClearLog();
    }

    // 仪表盘命令
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