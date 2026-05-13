using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
    private DashboardViewModel? _dashboard;
    private PropertyChangedEventHandler? _dashboardPropertyChangedHandler;
    private bool _isDisposed;
    private bool _isInitialized;

    private LayoutViewModel? _layout;

    private PropertyChangedEventHandler? _layoutPropertyChangedHandler;
    private LogPanelViewModel? _logPanel;
    private PropertyChangedEventHandler? _logPanelPropertyChangedHandler;
    private MenuViewModel? _menu;
    private QuickActionsViewModel? _quickActions;
    private PropertyChangedEventHandler? _quickActionsPropertyChangedHandler;
    private SearchPanelViewModel? _searchPanel;
    private PropertyChangedEventHandler? _searchPanelPropertyChangedHandler;

    [ObservableProperty] private string _statusMessage = "就绪";

    private DispatcherTimer? _statusResetTimer;
    private TripListViewModel? _tripList;
    private PropertyChangedEventHandler? _tripListPropertyChangedHandler;

    public MainViewModel()
    {
        Debug.WriteLine("[MainViewModel] 开始初始化");

        _generalSettingsService = new GeneralSettingsService();
        _logService = ServiceManager.Instance.LogService;

        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            Debug.WriteLine("[MainViewModel] 设计模式，跳过初始化");
            return;
        }

        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            if (message.SettingType == "General") _ = TripList.LoadTripItemsAsync();
        });

        WeakReferenceMessenger.Default.Register<ShortcutsChangedMessage>(this, (recipient, message) =>
        {
            RefreshShortcuts();
        });

        LoadShortcutKeys();

        WeakReferenceMessenger.Default.Register<StatusMessageMessage>(this, (recipient, message) =>
        {
            if (message.AutoReset)
                SetTemporaryStatus(message.Message, message.ResetDelaySeconds);
            else
                StatusMessage = message.Message;
        });

        WeakReferenceMessenger.Default.Register<BatchUpdateStatusMessage>(this,
            async (recipient, message) => { await TripList.BatchUpdateStatusAsync(); });

        Debug.WriteLine("[MainViewModel] 初始化完成");
    }

    public LayoutViewModel Layout
    {
        get
        {
            EnsureInitialized();
            return _layout!;
        }
    }

    public TripListViewModel TripList
    {
        get
        {
            EnsureInitialized();
            return _tripList!;
        }
    }

    public DashboardViewModel Dashboard
    {
        get
        {
            if (_dashboard == null)
            {
                Debug.WriteLine("[MainViewModel] 延迟初始化DashboardViewModel");
                _dashboard = new DashboardViewModel();
                if (_menu != null) _menu.Dashboard = _dashboard;
            }

            return _dashboard;
        }
    }

    public LogPanelViewModel LogPanel
    {
        get
        {
            EnsureInitialized();
            return _logPanel!;
        }
    }

    public MenuViewModel Menu
    {
        get
        {
            EnsureInitialized();
            return _menu!;
        }
    }

    public QuickActionsViewModel QuickActions
    {
        get
        {
            EnsureInitialized();
            return _quickActions!;
        }
    }

    public SearchPanelViewModel SearchPanel
    {
        get
        {
            EnsureInitialized();
            return _searchPanel!;
        }
    }

    public UISettingsService UISettingsService => Layout.UISettingsService;

    #region IDisposable 实现

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_layout != null && _layoutPropertyChangedHandler != null)
            _layout.PropertyChanged -= _layoutPropertyChangedHandler;
        if (_tripList != null && _tripListPropertyChangedHandler != null)
            _tripList.PropertyChanged -= _tripListPropertyChangedHandler;
        if (_dashboard != null && _dashboardPropertyChangedHandler != null)
            _dashboard.PropertyChanged -= _dashboardPropertyChangedHandler;
        if (_logPanel != null && _logPanelPropertyChangedHandler != null)
            _logPanel.PropertyChanged -= _logPanelPropertyChangedHandler;
        if (_quickActions != null && _quickActionsPropertyChangedHandler != null)
            _quickActions.PropertyChanged -= _quickActionsPropertyChangedHandler;
        if (_searchPanel != null && _searchPanelPropertyChangedHandler != null)
            _searchPanel.PropertyChanged -= _searchPanelPropertyChangedHandler;

        _layout?.Dispose();
        _tripList?.Dispose();
        _dashboard?.Dispose();
        _logPanel?.Dispose();

        WeakReferenceMessenger.Default.UnregisterAll(this);

        _statusResetTimer?.Stop();
        _statusResetTimer = null;

        Debug.WriteLine("[MainViewModel] 资源已释放");
    }

    #endregion

    private void EnsureInitialized()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        Debug.WriteLine("[MainViewModel] 延迟初始化子ViewModel");

        _layout = new LayoutViewModel();
        _tripList = new TripListViewModel();
        _logPanel = new LogPanelViewModel();
        _menu = new MenuViewModel();
        _quickActions = new QuickActionsViewModel();
        _searchPanel = new SearchPanelViewModel();

        if (_dashboard != null) _menu.Dashboard = _dashboard;

        SubscribeToChildViewModels();

        if (_generalSettingsService.Config.AutoRefreshOnStartup) _ = _tripList.LoadTripItemsAsync();
    }

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

    #region 动态快捷键显示

    private readonly ShortcutSettingsService _shortcutSettingsService = new();

    private Dictionary<string, string> _shortcutKeys = new();

    public Dictionary<string, string> ShortcutKeys
    {
        get => _shortcutKeys;
        set => SetProperty(ref _shortcutKeys, value);
    }

    private void LoadShortcutKeys()
    {
        var config = _shortcutSettingsService.Config;
        ShortcutKeys = config.Shortcuts.ToDictionary(s => s.ActionId, s => s.CurrentKey);
    }

    public void RefreshShortcuts()
    {
        _shortcutSettingsService.RefreshConfig();
        LoadShortcutKeys();
    }

    #endregion
}