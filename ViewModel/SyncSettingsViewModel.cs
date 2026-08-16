using System;
using System.Collections.Generic;
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
    private readonly SyncConflictResolveService _conflictService = new();
    private readonly SyncHttpServer _httpServer = SyncHttpServer.Instance;
    private readonly SyncSettingsService _settingsService = new();
    private readonly DispatcherTimer _countdownTimer;
    private DispatcherTimer? _statusResetTimer;
    private DateTime? _expiresAtUtc;
    private string _plainCode = string.Empty;
    private bool _isRefreshingCode;
    private bool _suppressUrlSelection;

    [ObservableProperty] private string _displayCode = "— — — — — —";

    [ObservableProperty] private string _countdownText = "--:--";

    [ObservableProperty] private int _remainingSeconds;

    [ObservableProperty] private bool _hasActiveCode;

    /// <summary>页内短时/操作反馈；瞬时提示会清空，不占设置窗底栏。</summary>
    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private long _localSeq;

    [ObservableProperty] private string _devicesEmptyHint = "暂无已配对设备";

    [ObservableProperty] private int _listenPort = SyncHttpServer.DefaultPort;

    [ObservableProperty] private bool _allowLan = true;

    [ObservableProperty] private bool _isServerRunning;

    [ObservableProperty] private string _serverStatusText = "未运行";

    [ObservableProperty] private string _listenUrlText = string.Empty;

    [ObservableProperty] private string _phoneGuideText =
        "手机与电脑需在同一局域网（或经隧道可达）。开始配对后，用手机「同步 → 扫码」扫描下方二维码。";

    [ObservableProperty] private BitmapImage? _connectionQrImage;

    [ObservableProperty] private bool _isBusy;

    /// <summary>一眼健康：服务 / 设备数 / seq / 冲突。</summary>
    [ObservableProperty] private string _healthSummaryText = "服务未启动";

    [ObservableProperty] private int _openConflictCount;

    [ObservableProperty] private int _pairedDeviceCount;

    /// <summary>当前写入二维码的服务地址（多网卡时可切换）。</summary>
    [ObservableProperty] private string _selectedListenUrl = string.Empty;

    [ObservableProperty] private bool _hasMultipleUrls;

    public ObservableCollection<SyncPairedDevice> ActiveDevices { get; } = new();

    public ObservableCollection<string> CandidateUrls { get; } = new();

    /// <summary>待处理同步冲突（与手机冲突箱同源）。</summary>
    public ObservableCollection<SyncConflictDto> OpenConflicts { get; } = new();

    [ObservableProperty] private string _conflictsEmptyHint = "暂无待处理冲突";

    [ObservableProperty] private bool _hasOpenConflicts;

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
        _statusResetTimer?.Stop();
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
                    SetStickyStatus("启动失败：" + SimplifyError(ex.Message));
                    return;
                }
            }

            SyncFromHttpServer();
            RefreshConnectionQr();

            var baseline = await _baselinePublisher.PublishMissingAsync();
            await RotatePairingCodeAsync(showStatus: false);

            var baselineHint = baseline.PublishedTotal > 0
                ? $"已写入 {baseline.PublishedRides} 条现有行程（序号 {baseline.MaxSeq}）。"
                : $"当前同步序号 {baseline.MaxSeq}。";

            if (_httpServer.AllowLan)
                SetStickyStatus($"可以开始扫码。{baselineHint}");
            else
                SetStickyStatus(
                    $"服务仅在本机监听。{baselineHint}若手机无法连接，请检查防火墙是否放行端口 {port}，或在高级设置中开启局域网。");

            await LoadSeqAsync();
            RefreshHealthSummary();
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
            SetTemporaryStatus(_httpServer.AllowLan
                ? "同步服务已启动（局域网）。"
                : "同步服务已启动（仅本机）。");
            if (!string.IsNullOrWhiteSpace(_httpServer.LastWarning))
                SetStickyStatus(_httpServer.LastWarning);
            _ = LoadSeqAsync();
        }
        catch (Exception ex)
        {
            SyncFromHttpServer();
            SetStickyStatus(SimplifyError(ex.Message));
        }
    }

    [RelayCommand]
    private void StopServer()
    {
        _httpServer.Stop();
        SyncFromHttpServer();
        ConnectionQrImage = null;
        ClearActiveCodeUi();
        SetStickyStatus("同步服务已暂停。已配对的设备不会解除，再次开始配对即可继续。");
        RefreshHealthSummary();
    }

    [RelayCommand(CanExecute = nameof(IsServerRunning))]
    private void CopyListenUrl()
    {
        var url = !string.IsNullOrWhiteSpace(SelectedListenUrl) ? SelectedListenUrl : ListenUrlText;
        if (string.IsNullOrWhiteSpace(url)) return;
        SetStatusAfterCopy(TrySetClipboard(url), "服务地址已复制。");
    }

    [RelayCommand]
    private async Task GeneratePairingCodeAsync()
    {
        await RotatePairingCodeAsync(showStatus: true);
    }

    [RelayCommand(CanExecute = nameof(HasActiveCode))]
    private void CopyPairingCode()
    {
        if (string.IsNullOrEmpty(_plainCode)) return;
        SetStatusAfterCopy(TrySetClipboard(_plainCode), "配对码已复制。");
    }

    [RelayCommand(CanExecute = nameof(HasActiveCode))]
    private async Task InvalidatePairingCodeAsync()
    {
        _countdownTimer.Stop();
        await _pairingService.InvalidateActivePairingCodesAsync();
        ClearActiveCodeUi();
        SetTemporaryStatus("配对码已作废，二维码已清除。需要时请点击「刷新二维码」。");
    }

    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        await LoadDevicesAsync();
        await LoadSeqAsync();
        await LoadConflictsAsync();
        RefreshHealthSummary();
    }

    [RelayCommand]
    private async Task RefreshConflictsAsync()
    {
        await LoadConflictsAsync();
        SetTemporaryStatus(HasOpenConflicts
            ? $"待处理冲突 {OpenConflictCount} 条。"
            : "暂无待处理冲突。");
    }

    [RelayCommand]
    private async Task ResolveConflictKeepLocalAsync(SyncConflictDto? conflict)
    {
        await ResolveConflictAsync(conflict, keep: "local", successHint: "已保留本机数据。");
    }

    [RelayCommand]
    private async Task ResolveConflictKeepRemoteAsync(SyncConflictDto? conflict)
    {
        await ResolveConflictAsync(conflict, keep: "remote", successHint: "已采用手机推送稿。");
    }

    private async Task ResolveConflictAsync(SyncConflictDto? conflict, string keep, string successHint)
    {
        if (conflict == null || conflict.Id <= 0) return;

        try
        {
            var result = await _conflictService.ResolveAsync(new SyncConflictResolveRequest
            {
                Id = conflict.Id,
                Keep = keep
            });
            if (!result.Ok)
            {
                SetStickyStatus("解决冲突失败：" + (result.Error ?? "未知错误"));
                return;
            }

            await LoadConflictsAsync();
            await LoadSeqAsync();
            SetTemporaryStatus(successHint);
            RefreshHealthSummary();
        }
        catch (Exception ex)
        {
            SetStickyStatus("解决冲突失败：" + SimplifyError(ex.Message));
        }
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
            SetTemporaryStatus(result.SummaryText);
        }
        catch (Exception ex)
        {
            SetStickyStatus("发布现有数据失败：" + SimplifyError(ex.Message));
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
            $"确定解除与「{device.DeviceName}」的配对吗？\n解除后需重新扫码才能同步。",
            "解除配对",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await _pairingService.RevokeDeviceAsync(device.DeviceId, source: "pc");
        await LoadDevicesAsync();
        SetTemporaryStatus($"已解除配对：{device.DeviceName}");
        RefreshHealthSummary();
    }

    /// <summary>
    ///     只放 Unicode 文本，且 copy=false，避免 SetDataObject(true)/Clear 在 UI 线程上拖住整页。
    /// </summary>
    private static bool TrySetClipboard(string text)
    {
        try
        {
            var data = new DataObject(DataFormats.UnicodeText, text);
            Clipboard.SetDataObject(data, false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>瞬时提示：约 2 秒后清空（页内反馈，不占设置窗底栏）。</summary>
    private void SetTemporaryStatus(string message, int delaySeconds = 2)
    {
        StatusMessage = message;

        if (_statusResetTimer == null)
        {
            _statusResetTimer = new DispatcherTimer();
            _statusResetTimer.Tick += (_, _) =>
            {
                StatusMessage = string.Empty;
                _statusResetTimer?.Stop();
            };
        }
        else
        {
            _statusResetTimer.Stop();
        }

        _statusResetTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, delaySeconds));
        _statusResetTimer.Start();
    }

    /// <summary>引导/错误：留在页内直到下一次状态写入。</summary>
    private void SetStickyStatus(string message)
    {
        _statusResetTimer?.Stop();
        StatusMessage = message;
    }

    /// <summary>状态文案延后到下一拍，避免与点击手势同一帧抢布局。</summary>
    private void SetStatusAfterCopy(bool ok, string successMessage)
    {
        var message = ok ? successMessage : "复制失败，请手动选中配对码后按 Ctrl+C。";
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.BeginInvoke(
            () =>
            {
                if (ok)
                    SetTemporaryStatus(message);
                else
                    SetStickyStatus(message);
            },
            DispatcherPriority.Background);
    }

    private void RefreshConnectionQr()
    {
        // 作废/无码时不展示二维码，避免扫到「只能填地址、无法配对」的残码。
        if (string.IsNullOrEmpty(_plainCode) || _plainCode.Length != 6 ||
            !_plainCode.All(char.IsDigit))
        {
            ConnectionQrImage = null;
            return;
        }

        var url = !string.IsNullOrWhiteSpace(SelectedListenUrl)
            ? SelectedListenUrl
            : ListenUrlText;
        if (string.IsNullOrWhiteSpace(url))
        {
            ConnectionQrImage = null;
            return;
        }

        var payload = $"GuiPiao|{url.Trim().TrimEnd('/')}|{_plainCode}";
        ConnectionQrImage = TicketPreviewQrService.CreateQrBitmap(payload, 8);
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
            ServerStatusText = _httpServer.AllowLan ? "运行中（局域网）" : "运行中（仅本机）";
            RebuildCandidateUrls(_httpServer.ListenUrls);
            if (!string.IsNullOrWhiteSpace(SelectedListenUrl))
                ListenUrlText = SelectedListenUrl;
            else
            {
                ListenUrlText = _httpServer.ListenUrls.FirstOrDefault()
                                ?? SyncHttpServer.GetPreferredBaseUrl(_httpServer.Port, _httpServer.AllowLan);
            }

            RefreshConnectionQr();
        }
        else
        {
            ServerStatusText = string.IsNullOrWhiteSpace(_httpServer.LastError) ? "未运行" : "启动失败";
            RebuildCandidateUrls(SyncHttpServer.BuildPrefixes(
                ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort,
                AllowLan));
            ListenUrlText = string.IsNullOrWhiteSpace(SelectedListenUrl)
                ? SyncHttpServer.GetPreferredBaseUrl(
                    ListenPort <= 0 ? SyncHttpServer.DefaultPort : ListenPort,
                    AllowLan)
                : SelectedListenUrl;
            ConnectionQrImage = null;
        }

        CopyListenUrlCommand.NotifyCanExecuteChanged();
        RefreshHealthSummary();
    }

    private void RebuildCandidateUrls(IReadOnlyList<string> urls)
    {
        _suppressUrlSelection = true;
        try
        {
            var previous = SelectedListenUrl;
            CandidateUrls.Clear();
            foreach (var u in urls.Where(x => !string.IsNullOrWhiteSpace(x)))
                CandidateUrls.Add(u.Trim().TrimEnd('/'));

            HasMultipleUrls = CandidateUrls.Count > 1;

            string? pick = null;
            if (!string.IsNullOrWhiteSpace(previous) &&
                CandidateUrls.Any(u => string.Equals(u, previous, StringComparison.OrdinalIgnoreCase)))
                pick = CandidateUrls.First(u => string.Equals(u, previous, StringComparison.OrdinalIgnoreCase));
            else if (AllowLan || _httpServer.AllowLan)
                pick = CandidateUrls.FirstOrDefault(u =>
                           !u.Contains("127.0.0.1", StringComparison.Ordinal))
                       ?? CandidateUrls.FirstOrDefault();
            else
                pick = CandidateUrls.FirstOrDefault();

            SelectedListenUrl = pick ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(SelectedListenUrl))
                ListenUrlText = SelectedListenUrl;
        }
        finally
        {
            _suppressUrlSelection = false;
        }
    }

    partial void OnSelectedListenUrlChanged(string value)
    {
        if (_suppressUrlSelection || string.IsNullOrWhiteSpace(value)) return;
        ListenUrlText = value.Trim().TrimEnd('/');
        RefreshConnectionQr();
        SetTemporaryStatus($"已切换到地址：{ListenUrlText}");
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        if (_isRefreshingCode || _expiresAtUtc == null) return;

        ApplyCountdownFromExpiry(_expiresAtUtc.Value);

        // 兑换成功走 PairingCodeConsumed 事件换码，不要每秒查库（会和点击复制抢 UI 线程）。
        if (!SyncPairingService.IsDisplayExpired(_expiresAtUtc.Value))
            return;

        _ = RotatePairingCodeAsync(showStatus: false);
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
            var owner = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext is SettingsViewModel);
            MessageBoxWindow.Show(
                owner,
                $"设备「{e.DeviceName}」已在手机端解除配对。",
                "设备已解绑",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            SetTemporaryStatus($"已解除配对：{e.DeviceName}");
        }

        RefreshHealthSummary();
    }

    private async Task RefreshAfterCodeConsumedAsync()
    {
        if (_isRefreshingCode) return;
        SetTemporaryStatus("设备已配对。可在手机上执行对齐；若需连接新手机，请再次开始配对。");
        await RotatePairingCodeAsync(showStatus: false);
        await LoadDevicesAsync();
        RefreshHealthSummary();
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
                SetTemporaryStatus($"配对码已刷新，{SyncPairingService.CodeTtlSeconds} 秒内有效。");
            await LoadDevicesAsync();
        }
        catch (Exception ex)
        {
            SetStickyStatus($"生成配对码失败：{ex.Message}");
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
        await LoadConflictsAsync();
        RefreshHealthSummary();
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
            PairedDeviceCount = active.Count;
            DevicesEmptyHint = active.Count == 0
                ? "暂无已配对设备"
                : string.Empty;
            RefreshHealthSummary();
        }
        catch (Exception ex)
        {
            SetStickyStatus($"加载设备失败：{ex.Message}");
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

        RefreshHealthSummary();
    }

    private async Task LoadConflictsAsync()
    {
        try
        {
            var list = await _conflictService.ListOpenAsync();
            var rows = list.Conflicts ?? new List<SyncConflictDto>();
            OpenConflicts.Clear();
            foreach (var c in rows)
                OpenConflicts.Add(c);
            OpenConflictCount = OpenConflicts.Count;
            HasOpenConflicts = OpenConflictCount > 0;
            ConflictsEmptyHint = HasOpenConflicts
                ? string.Empty
                : "暂无待处理冲突。手机推送与本机冲突时会出现在这里。";
        }
        catch (Exception ex)
        {
            OpenConflicts.Clear();
            OpenConflictCount = 0;
            HasOpenConflicts = false;
            ConflictsEmptyHint = "加载冲突失败：" + SimplifyError(ex.Message);
        }

        RefreshHealthSummary();
    }

    private void RefreshHealthSummary()
    {
        PairedDeviceCount = ActiveDevices.Count;
        if (!IsServerRunning)
        {
            HealthSummaryText = PairedDeviceCount == 0
                ? "服务未启动"
                : $"服务已暂停，仍保留 {PairedDeviceCount} 台已配对设备";
            return;
        }

        var conflict = OpenConflictCount > 0 ? $"，待处理冲突 {OpenConflictCount}" : "";
        HealthSummaryText =
            $"{ServerStatusText}，{PairedDeviceCount} 台设备，同步序号 {LocalSeq}{conflict}";
    }
}