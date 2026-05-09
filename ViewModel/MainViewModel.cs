using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
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

        Layout = new LayoutViewModel();
        TripList = new TripListViewModel();
        Dashboard = new DashboardViewModel();
        LogPanel = new LogPanelViewModel();
        Menu = new MenuViewModel();
        QuickActions = new QuickActionsViewModel();
        SearchPanel = new SearchPanelViewModel();

        _generalSettingsService = new GeneralSettingsService();
        _logService = ServiceManager.Instance.LogService;

        SubscribeToChildViewModels();

        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            Debug.WriteLine("[MainViewModel] 设计模式，跳过初始化");
            return;
        }

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            if (message.SettingType == "General") _ = TripList.LoadTripItemsAsync();
        });

        WeakReferenceMessenger.Default.Register<StatusMessageMessage>(this, (recipient, message) =>
        {
            if (message.AutoReset)
                SetTemporaryStatus(message.Message, message.ResetDelaySeconds);
            else
                StatusMessage = message.Message;
        });

        WeakReferenceMessenger.Default.Register<BatchUpdateStatusMessage>(this,
            async (recipient, message) => { await TripList.BatchUpdateStatusAsync(); });

        if (_generalSettingsService.Config.AutoRefreshOnStartup) _ = TripList.LoadTripItemsAsync();

        Debug.WriteLine("[MainViewModel] 初始化完成");
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

    public UISettingsService UISettingsService => Layout.UISettingsService;

    #region IDisposable 实现

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        Layout?.Dispose();
        TripList?.Dispose();
        Dashboard?.Dispose();
        LogPanel?.Dispose();

        WeakReferenceMessenger.Default.UnregisterAll(this);

        _statusResetTimer?.Stop();
        _statusResetTimer = null;

        Debug.WriteLine("[MainViewModel] 资源已释放");
    }

    #endregion

    private void SetTemporaryStatus(string message, int delaySeconds = 2)
    {
        StatusMessage = message;

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
        SubscribeToLayoutChanges();
        SubscribeToTripListChanges();
        SubscribeToDashboardChanges();
        SubscribeToSearchPanelChanges();
        SubscribeToLogPanelChanges();
        SubscribeToQuickActionsChanges();
    }

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

    public void SaveLayoutSettings()
    {
        Layout.SaveLayoutSettings();
    }

    public async Task SaveDataSortInfoAsync(string sortColumn, ListSortDirection direction)
    {
        await TripList.SaveDataSortInfoAsync(sortColumn, direction);
    }

    public void HandleTripDoubleClick(TripItem trip)
    {
        TripList.HandleTripDoubleClick(trip);
    }

    public void ApplyScrollbarStyle()
    {
        Layout.ApplyScrollbarStyle();
    }

    public async Task RefreshLogItemsAsync()
    {
        await Task.CompletedTask;
    }

    public List<TripItem> GetSelectedTripItems()
    {
        return TripList.GetSelectedTripItems();
    }

    public void UpdateColumnSortDirection(string sortColumn, bool isDescending)
    {
    }
}
