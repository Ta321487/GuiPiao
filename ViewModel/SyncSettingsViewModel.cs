using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.DataAccess;
using GuiPiao.Model.Sync;
using GuiPiao.Services;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

/// <summary>
///     同步设置：主路径为启动服务并生成配对码；端口等项折叠在高级设置。
/// </summary>
public partial class SyncSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly SyncPairingService _pairingService = new();
    private readonly SyncChangeRepository _changeRepository = new();
    private readonly SyncBaselinePublisher _baselinePublisher = new();
    private readonly SyncHttpServer _httpServer = SyncHttpServer.Instance;
    private readonly SyncSettingsService _settingsService = new();
    private readonly DispatcherTimer _countdownTimer;
    private DateTime? _expiresAtUtc;
    private string _plainCode = string.Empty;
    private bool _isRefreshingCode;

    [ObservableProperty] private string _displayCode = "— — — — — —";

    [ObservableProperty] private string _countdownText = "--:--";

    [ObservableProperty] private int _remainingSeconds;

    [ObservableProperty] private bool _hasActiveCode;

    [ObservableProperty] private string _statusMessage = "点击「开始配对」启动服务并生成配对码。";

    [ObservableProperty] private long _localSeq;

    [ObservableProperty] private string _devicesEmptyHint = "暂无已配对设备";

    [ObservableProperty] private int _listenPort = SyncHttpServer.DefaultPort;

    [ObservableProperty] private bool _allowLan = true;

    [ObservableProperty] private bool _isServerRunning;

    [ObservableProperty] private string _serverStatusText = "未运行";

    [ObservableProperty] private string _listenUrlText = string.Empty;

    [ObservableProperty] private string _phoneGuideText =
        "手机与 PC 需在同一局域网（或经隧道可达）。「开始配对」会启动服务、把现有行程写入同步日志并生成配对码；手机填服务地址与配对码后执行「立即对齐」。若仍无数据，可点「发布现有行程」。";

    [ObservableProperty] private BitmapImage? _connectionQrImage;

    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<SyncPairedDevice> ActiveDevices { get; } = new();

    public bool HasUnsavedChanges => false;

    public SyncSettingsViewModel()
    {
        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += OnCountdownTick;
        _httpServer.StateChanged += OnHttpServerStateChanged;
        SyncPairingService.PairingCodeConsumed += OnPairingCodeConsumed;
        SyncPairingService.DeviceRevoked += OnDeviceRevoked;
        LoadFromSettings();
        SyncFromHttpServer();
        _ = RefreshStatusAsync();
    }

    public void ReloadSettings()
    {
        _settingsService.Reload();
        LoadFromSettings();
        SyncFromHttpServer();
        _ = RefreshStatusAsync();
    }

    public Task SaveSettingsAsync(bool showMessage = true)
    {
        PersistSettings();
        return Task.CompletedTask;
    }

    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage) => await SaveSettingsAsync(showMessage);

    public void OnWindowClosing()
    {
        _countdownTimer.Stop();
    }

    private void LoadFromSettings()
    {
        ListenPort = _settingsService.Config.ListenPort;
        AllowLan = _settingsService.Config.AllowLan;
    }

    private void PersistSettings()
    {
        _settingsService.Config.ListenPort = ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort;
        _settingsService.Config.AllowLan = AllowLan;
        _settingsService.Save();
    }

    /// <summary>主路径：启动服务（优先局域网）并生成配对码。</summary>
    [RelayCommand]
    private async Task ConnectPhoneAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var port = ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort;
            ListenPort = port;
            AllowLan = true;
            PersistSettings();

            if (!_httpServer.IsRunning || !_httpServer.AllowLan)
            {
                if (_httpServer.IsRunning)
                    _httpServer.Stop();

                try
                {
                    _httpServer.Start(port, allowLan: true);
                }
                catch (Exception ex)
                {
                    SyncFromHttpServer();
                    StatusMessage = "启动失败：" + SimplifyError(ex.Message);
                    return;
                }
            }

            SyncFromHttpServer();
            RefreshConnectionQr();

            var baseline = await _baselinePublisher.PublishMissingAsync();
            await RotatePairingCodeAsync(showStatus: false);
            TryCopyCodeSilent();

            var baselineHint = baseline.PublishedTotal > 0
                ? $" 已把 {baseline.PublishedRides} 条现有行程写入同步日志（seq={baseline.MaxSeq}）。"
                : $" 同步水位 seq={baseline.MaxSeq}。";

            if (_httpServer.AllowLan)
            {
                StatusMessage =
                    $"配对码已生成。{baselineHint}请在手机端「同步」确认服务地址为 {ListenUrlText}，输入配对码完成绑定后执行「立即对齐」。";
            }
            else
            {
                StatusMessage =
                    $"服务已在本机启动（未绑定局域网）。{baselineHint}当前地址 {ListenUrlText}；手机同网连接请检查防火墙是否放行端口 {port}。";
            }

            await LoadSeqAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StartServer()
    {
        try
        {
            var port = ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort;
            ListenPort = port;
            PersistSettings();
            if (_httpServer.IsRunning)
                _httpServer.Stop();
            _httpServer.Start(port, AllowLan);
            SyncFromHttpServer();
            RefreshConnectionQr();
            StatusMessage = _httpServer.AllowLan
                ? "同步服务已启动（含局域网）。"
                : "同步服务已启动（仅本机）。";
            if (!string.IsNullOrWhiteSpace(_httpServer.LastWarning))
                StatusMessage = _httpServer.LastWarning;
            _ = LoadSeqAsync();
        }
        catch (Exception ex)
        {
            SyncFromHttpServer();
            StatusMessage = SimplifyError(ex.Message);
        }
    }

    [RelayCommand]
    private void StopServer()
    {
        _httpServer.Stop();
        SyncFromHttpServer();
        ConnectionQrImage = null;
        StatusMessage = "同步服务已停止。";
    }

    [RelayCommand(CanExecute = nameof(IsServerRunning))]
    private void CopyListenUrl()
    {
        if (string.IsNullOrWhiteSpace(ListenUrlText)) return;
        try
        {
            Clipboard.SetText(ListenUrlText);
            StatusMessage = "服务地址已复制。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败：{ex.Message}";
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
        TryCopyCodeSilent();
        StatusMessage = "配对码已复制。";
        await Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasActiveCode))]
    private async Task InvalidatePairingCodeAsync()
    {
        _countdownTimer.Stop();
        await _pairingService.InvalidateActivePairingCodesAsync();
        ClearActiveCodeUi();
        StatusMessage = "当前配对码已作废。";
    }

    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        await LoadDevicesAsync();
        await LoadSeqAsync();
    }

    /// <summary>把库中已有行程/标签补进 sync_change（历史数据无变更日志时手机拉不到）。</summary>
    [RelayCommand]
    private async Task PublishBaselineAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _baselinePublisher.PublishMissingAsync();
            await LoadSeqAsync();
            StatusMessage = result.SummaryText;
        }
        catch (Exception ex)
        {
            StatusMessage = "发布现有数据失败：" + SimplifyError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
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
            $"撤销设备「{device.DeviceName}」的配对？\n撤销后需重新配对。",
            "撤销配对",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await _pairingService.RevokeDeviceAsync(device.DeviceId, source: "pc");
        await LoadDevicesAsync();
        StatusMessage = $"已撤销：{device.DeviceName}";
    }

    private void TryCopyCodeSilent()
    {
        if (string.IsNullOrEmpty(_plainCode)) return;
        try
        {
            Clipboard.SetText(_plainCode);
        }
        catch
        {
            // ignore
        }
    }

    private void RefreshConnectionQr()
    {
        if (string.IsNullOrWhiteSpace(ListenUrlText))
        {
            ConnectionQrImage = null;
            return;
        }

        // 有活跃配对码时写入 GuiPiao|地址|码，手机拍一次即可配对；无码时仍只编码地址。
        var payload = ListenUrlText.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(_plainCode) && _plainCode.Length == 6 &&
            _plainCode.All(char.IsDigit))
            payload = $"GuiPiao|{payload}|{_plainCode}";

        ConnectionQrImage = TicketPreviewQrService.CreateQrBitmap(payload, 5);
    }

    private static string SimplifyError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "未知错误";
        if (raw.Contains("AddressAlreadyInUse", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("已被使用", StringComparison.Ordinal) ||
            raw.Contains("in use", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("占用", StringComparison.Ordinal))
            return "端口已被占用，请在高级设置中更换端口。";
        return raw;
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
            ServerStatusText = _httpServer.AllowLan ? "运行中 · 局域网" : "运行中 · 本机";
            ListenUrlText = _httpServer.ListenUrls.FirstOrDefault()
                            ?? SyncHttpServer.GetPreferredBaseUrl(_httpServer.Port, _httpServer.AllowLan);
            if (_httpServer.AllowLan)
            {
                var lan = _httpServer.ListenUrls.FirstOrDefault(u =>
                    !u.Contains("127.0.0.1", StringComparison.Ordinal));
                if (!string.IsNullOrWhiteSpace(lan))
                    ListenUrlText = lan;
            }

            RefreshConnectionQr();
        }
        else
        {
            ServerStatusText = string.IsNullOrWhiteSpace(_httpServer.LastError) ? "未运行" : "启动失败";
            ListenUrlText = SyncHttpServer.GetPreferredBaseUrl(
                ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort,
                AllowLan);
            ConnectionQrImage = null;
        }

        CopyListenUrlCommand.NotifyCanExecuteChanged();
    }

    private async void OnCountdownTick(object? sender, EventArgs e)
    {
        if (_isRefreshingCode || _expiresAtUtc == null) return;

        ApplyCountdownFromExpiry(_expiresAtUtc.Value);

        if (!string.IsNullOrEmpty(_plainCode) &&
            await _pairingService.IsPairingCodeConsumedAsync(_plainCode))
        {
            StatusMessage = "配对码已使用，正在刷新。";
            await RotatePairingCodeAsync(showStatus: true);
            return;
        }

        if (!SyncPairingService.IsDisplayExpired(_expiresAtUtc.Value))
            return;

        await RotatePairingCodeAsync(showStatus: false);
    }

    private void OnPairingCodeConsumed(object? sender, EventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            _ = RefreshAfterCodeConsumedAsync();
            return;
        }

        if (dispatcher.CheckAccess())
            _ = RefreshAfterCodeConsumedAsync();
        else
            dispatcher.InvokeAsync(RefreshAfterCodeConsumedAsync);
    }

    private void OnDeviceRevoked(object? sender, SyncDeviceRevokedEventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            _ = HandleDeviceRevokedAsync(e);
            return;
        }

        if (dispatcher.CheckAccess())
            _ = HandleDeviceRevokedAsync(e);
        else
            dispatcher.InvokeAsync(() => HandleDeviceRevokedAsync(e));
    }

    private async Task HandleDeviceRevokedAsync(SyncDeviceRevokedEventArgs e)
    {
        await LoadDevicesAsync();
        if (string.Equals(e.Source, "device", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = $"手机「{e.DeviceName}」已解除配对。";
            var owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext is SettingsViewModel);
            MessageBoxWindow.Show(
                owner,
                $"设备「{e.DeviceName}」已在手机端解除配对，凭证已失效。",
                "设备已解绑",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            StatusMessage = $"已撤销：{e.DeviceName}";
        }
    }

    private async Task RefreshAfterCodeConsumedAsync()
    {
        if (_isRefreshingCode) return;
        StatusMessage = "设备已配对，配对码已刷新。";
        await RotatePairingCodeAsync(showStatus: false);
        await LoadDevicesAsync();
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
                StatusMessage = $"配对码已刷新，{SyncPairingService.CodeTtlSeconds} 秒内有效。";
            await LoadDevicesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成配对码失败：{ex.Message}";
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
        RefreshConnectionQr();

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
        RefreshConnectionQr();
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
                ? "暂无已配对设备"
                : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载设备失败：{ex.Message}";
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
