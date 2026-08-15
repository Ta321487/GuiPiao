using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.DataAccess;
using GuiPiao.Model.Sync;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

/// <summary>
///     同步设置：配对码倒计时与 <see cref="SyncPairingService"/> 展示 TTL 同源；
///     HTTP 服务启停走 <see cref="SyncHttpServer"/>。
/// </summary>
public partial class SyncSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly SyncPairingService _pairingService = new();
    private readonly SyncChangeRepository _changeRepository = new();
    private readonly SyncHttpServer _httpServer = SyncHttpServer.Instance;
    private readonly DispatcherTimer _countdownTimer;
    private DateTime? _expiresAtUtc;
    private string _plainCode = string.Empty;
    private bool _isRefreshingCode;

    [ObservableProperty] private string _displayCode = "— — — — — —";

    [ObservableProperty] private string _countdownText = "--:--";

    [ObservableProperty] private int _remainingSeconds;

    [ObservableProperty] private bool _hasActiveCode;

    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private long _localSeq;

    [ObservableProperty] private string _devicesEmptyHint = "尚无设备 — 生成配对码后在手机端输入";

    [ObservableProperty] private int _listenPort = SyncHttpServer.DefaultPort;

    [ObservableProperty] private bool _isServerRunning;

    [ObservableProperty] private string _serverStatusText = "已停止";

    [ObservableProperty] private string _listenUrlText = string.Empty;

    public ObservableCollection<SyncPairedDevice> ActiveDevices { get; } = new();

    /// <summary>无 JSON 偏好；配对与服务状态在运行时/SQLite。</summary>
    public bool HasUnsavedChanges => false;

    public SyncSettingsViewModel()
    {
        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += OnCountdownTick;
        _httpServer.StateChanged += OnHttpServerStateChanged;
        SyncFromHttpServer();
        _ = RefreshStatusAsync();
    }

    public void ReloadSettings()
    {
        SyncFromHttpServer();
        _ = RefreshStatusAsync();
    }

    public Task SaveSettingsAsync(bool showMessage = true) => Task.CompletedTask;

    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage) => await SaveSettingsAsync(showMessage);

    /// <summary>设置窗关闭时暂停倒计时（VM 可能被缓存复用，勿拆掉 Timer）。</summary>
    public void OnWindowClosing()
    {
        _countdownTimer.Stop();
    }

    [RelayCommand]
    private void StartServer()
    {
        try
        {
            var port = ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort;
            ListenPort = port;
            _httpServer.Start(port);
            SyncFromHttpServer();
            StatusMessage = "同步服务已启动";
            _ = LoadSeqAsync();
        }
        catch (Exception ex)
        {
            SyncFromHttpServer();
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void StopServer()
    {
        _httpServer.Stop();
        SyncFromHttpServer();
        StatusMessage = "同步服务已停止";
    }

    [RelayCommand(CanExecute = nameof(IsServerRunning))]
    private void CopyListenUrl()
    {
        if (string.IsNullOrWhiteSpace(ListenUrlText)) return;
        try
        {
            Clipboard.SetText(ListenUrlText);
            StatusMessage = "同步地址已复制";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GeneratePairingCodeAsync()
    {
        await RotatePairingCodeAsync(showStatus: true);
    }

    [RelayCommand(CanExecute = nameof(HasActiveCode))]
    private async Task CopyPairingCodeAsync()
    {
        if (string.IsNullOrEmpty(_plainCode)) return;
        try
        {
            Clipboard.SetText(_plainCode);
            StatusMessage = "配对码已复制";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败: {ex.Message}";
        }

        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasActiveCode))]
    private async Task InvalidatePairingCodeAsync()
    {
        _countdownTimer.Stop();
        await _pairingService.InvalidateActivePairingCodesAsync();
        ClearActiveCodeUi();
        StatusMessage = "已作废当前配对码";
    }

    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        await LoadDevicesAsync();
        await LoadSeqAsync();
    }

    [RelayCommand]
    private async Task RevokeDeviceAsync(SyncPairedDevice? device)
    {
        if (device == null || string.IsNullOrWhiteSpace(device.DeviceId)) return;

        var owner = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        var confirm = MessageBoxWindow.Show(
            owner,
            $"确定撤销设备「{device.DeviceName}」的配对？\n撤销后该设备需重新输入配对码。",
            "撤销配对",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await _pairingService.RevokeDeviceAsync(device.DeviceId);
        await LoadDevicesAsync();
        StatusMessage = $"已撤销：{device.DeviceName}";
    }

    private void OnHttpServerStateChanged(object? sender, EventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
            SyncFromHttpServer();
        else
            dispatcher.Invoke(SyncFromHttpServer);
    }

    private void SyncFromHttpServer()
    {
        IsServerRunning = _httpServer.IsRunning;
        if (_httpServer.IsRunning)
        {
            ListenPort = _httpServer.Port;
            ServerStatusText = "运行中";
            ListenUrlText = SyncHttpServer.GetPreferredBaseUrl(_httpServer.Port);
        }
        else
        {
            ServerStatusText = string.IsNullOrWhiteSpace(_httpServer.LastError) ? "已停止" : "启动失败";
            ListenUrlText = SyncHttpServer.GetPreferredBaseUrl(ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort);
        }

        CopyListenUrlCommand.NotifyCanExecuteChanged();
    }

    private async void OnCountdownTick(object? sender, EventArgs e)
    {
        if (_isRefreshingCode || _expiresAtUtc == null) return;

        ApplyCountdownFromExpiry(_expiresAtUtc.Value);

        if (!SyncPairingService.IsDisplayExpired(_expiresAtUtc.Value))
            return;

        await RotatePairingCodeAsync(showStatus: false);
    }

    private async Task RotatePairingCodeAsync(bool showStatus)
    {
        if (_isRefreshingCode) return;
        _isRefreshingCode = true;
        try
        {
            var result = await _pairingService.CreatePairingCodeAsync();
            ApplyCodeResult(result);
            if (showStatus)
                StatusMessage = $"配对码已生成，{SyncPairingService.CodeTtlSeconds} 秒后自动刷新";
            await LoadDevicesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成配对码失败: {ex.Message}";
            ClearActiveCodeUi();
        }
        finally
        {
            _isRefreshingCode = false;
        }
    }

    private void ApplyCodeResult(SyncPairingCodeResult result)
    {
        _plainCode = result.Code;
        _expiresAtUtc = result.ExpiresAtUtc;
        DisplayCode = SyncPairingService.FormatCodeForDisplay(result.Code);
        HasActiveCode = true;
        ApplyCountdownFromExpiry(result.ExpiresAtUtc);
        CopyPairingCodeCommand.NotifyCanExecuteChanged();
        InvalidatePairingCodeCommand.NotifyCanExecuteChanged();

        if (!_countdownTimer.IsEnabled)
            _countdownTimer.Start();
    }

    private void ApplyCountdownFromExpiry(DateTime expiresAtUtc)
    {
        RemainingSeconds = SyncPairingService.GetRemainingDisplaySeconds(expiresAtUtc);
        var m = RemainingSeconds / 60;
        var s = RemainingSeconds % 60;
        CountdownText = $"{m:00}:{s:00}";
    }

    private void ClearActiveCodeUi()
    {
        _plainCode = string.Empty;
        _expiresAtUtc = null;
        DisplayCode = "— — — — — —";
        CountdownText = "--:--";
        RemainingSeconds = 0;
        HasActiveCode = false;
        _countdownTimer.Stop();
        CopyPairingCodeCommand.NotifyCanExecuteChanged();
        InvalidatePairingCodeCommand.NotifyCanExecuteChanged();
    }

    private async Task RefreshStatusAsync()
    {
        await LoadDevicesAsync();
        await LoadSeqAsync();
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            var all = await _pairingService.ListDevicesAsync();
            var active = all.Where(d => !d.Revoked).ToList();
            ActiveDevices.Clear();
            foreach (var d in active)
                ActiveDevices.Add(d);
            DevicesEmptyHint = active.Count == 0
                ? "尚无设备 — 生成配对码后在手机端输入"
                : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载设备失败: {ex.Message}";
        }
    }

    private async Task LoadSeqAsync()
    {
        try
        {
            LocalSeq = await _changeRepository.GetMaxSeqAsync();
        }
        catch
        {
            LocalSeq = 0;
        }
    }
}
