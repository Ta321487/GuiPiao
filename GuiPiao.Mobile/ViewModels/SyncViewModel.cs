using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Messaging;
using GuiPiao.Mobile.Model;
using GuiPiao.Mobile.Services;
using GuiPiao.Model.Sync;

namespace GuiPiao.Mobile.ViewModels;

public partial class SyncViewModel : ObservableObject
{
    private readonly MobileSettingsStore _settings;
    private readonly SyncApiClient _client;
    private readonly SyncPullBuffer _pullBuffer;
    private readonly SyncPushQueue _pushQueue;
    private readonly MobileSyncIngressService _ingress;
    private readonly StationCacheRepository _stations;
    private string _lastAutoPairAttempt = string.Empty;
    private bool _suppressAutoPair;
    private bool _hasAlignedThisSession;
    private IDispatcherTimer? _pairWatch;
    private bool _pairProbeBusy;
    private bool _kickDialogShowing;

    [ObservableProperty] private string _baseUrl = "http://127.0.0.1:17880";
    [ObservableProperty] private string _deviceName = "GuiPiao Mobile";
    [ObservableProperty] private string _pairingCode = string.Empty;
    [ObservableProperty] private string _statusText = "未配对";
    [ObservableProperty] private string _detailText = "在 PC 设置 → 同步点「开始配对」，扫码后输入配对码。";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isPaired;
    [ObservableProperty] private long _lastPullSeq;
    [ObservableProperty] private long _remoteMaxSeq;
    [ObservableProperty] private int _pendingPushCount;
    [ObservableProperty] private int _bufferedPullCount;
    [ObservableProperty] private ObservableCollection<SyncConflictDto> _conflicts = new();
    [ObservableProperty] private bool _hasConflicts;
    [ObservableProperty] private string _stationsHint = string.Empty;
    [ObservableProperty] private string _shortHost = "未设置地址";
    [ObservableProperty] private string _spherePrimary = string.Empty;
    [ObservableProperty] private string _sphereSecondary = string.Empty;
    [ObservableProperty] private string _centerHint = "输入配对码后自动连接";
    [ObservableProperty] private string _footerPrimary = string.Empty;
    [ObservableProperty] private string _conflictBadgeText = "0";
    [ObservableProperty] private bool _showAlignHint;

    public SyncViewModel(
        MobileSettingsStore settings,
        SyncApiClient client,
        SyncPullBuffer pullBuffer,
        SyncPushQueue pushQueue,
        MobileSyncIngressService ingress,
        StationCacheRepository stations)
    {
        _settings = settings;
        _client = client;
        _pullBuffer = pullBuffer;
        _pushQueue = pushQueue;
        _ingress = ingress;
        _stations = stations;
        ReloadFromStore();
    }

    /// <summary>已配对时后台探测会话；PC 撤销后约 2 秒内弹窗踢出。</summary>
    private void StartPairWatch()
    {
        if (_pairWatch != null) return;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null) return;

        _pairWatch = dispatcher.CreateTimer();
        _pairWatch.Interval = TimeSpan.FromSeconds(2);
        _pairWatch.Tick += OnPairWatchTick;
        _pairWatch.Start();
        _ = ProbePairingAsync();
    }

    private void StopPairWatch()
    {
        if (_pairWatch == null) return;
        _pairWatch.Stop();
        _pairWatch.Tick -= OnPairWatchTick;
        _pairWatch = null;
    }

    private void OnPairWatchTick(object? sender, EventArgs e) => _ = ProbePairingAsync();

    private async Task ProbePairingAsync()
    {
        if (_pairProbeBusy || !IsPaired || IsBusy || _kickDialogShowing) return;
        _pairProbeBusy = true;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _client.SessionAsync(CurrentConfig(), cts.Token);
        }
        catch (SyncUnauthorizedException ex)
        {
            await TryHandleAuthFailureAsync(ex);
        }
        catch (OperationCanceledException)
        {
            // 超时/网络抖动，不踢出
        }
        catch
        {
            // 服务暂不可达，不踢出
        }
        finally
        {
            _pairProbeBusy = false;
        }
    }

    public void ReloadFromStore()
    {
        var cfg = _settings.LoadSync();
        BaseUrl = cfg.BaseUrl;
        DeviceName = string.IsNullOrWhiteSpace(cfg.DeviceName) ? "GuiPiao Mobile" : cfg.DeviceName;
        LastPullSeq = cfg.LastPullSeq;
        IsPaired = !string.IsNullOrWhiteSpace(cfg.DeviceId) && !string.IsNullOrWhiteSpace(cfg.DeviceToken);
        BufferedPullCount = _pullBuffer.Count;
        PendingPushCount = _pushQueue.Count;
        StationsHint = $"本地车站缓存 {_stations.Count()} 条";
        StatusText = IsPaired ? "已配对" : "未配对";
        DetailText = IsPaired
            ? $"设备 {cfg.DeviceId} · seq={LastPullSeq} · 待推 {PendingPushCount}"
            : "在 PC 设置 → 同步点「开始配对」，扫码后输入配对码。";
        RefreshChrome();
    }

    private void RefreshChrome()
    {
        ShortHost = FormatShortHost(BaseUrl);
        ConflictBadgeText = Conflicts.Count.ToString();
        HasConflicts = Conflicts.Count > 0;
        ShowAlignHint = IsPaired && !_hasAlignedThisSession;

        if (IsBusy && !IsPaired)
        {
            SpherePrimary = "···";
            SphereSecondary = string.Empty;
            CenterHint = "正在连接 PC…";
            FooterPrimary = ShortHost;
            return;
        }

        if (IsBusy && IsPaired)
        {
            SpherePrimary = "对齐中";
            SphereSecondary = PendingPushCount > 0 ? $"↑ {PendingPushCount}" : string.Empty;
            CenterHint = DetailText;
            FooterPrimary = $"已连接 · {ShortHost}";
            return;
        }

        if (IsPaired)
        {
            SpherePrimary = "对齐";
            SphereSecondary = PendingPushCount > 0 ? $"↑ {PendingPushCount}" : string.Empty;
            CenterHint = ShowAlignHint ? "点按球体以对齐" : DetailText;
            FooterPrimary = $"已连接 · {ShortHost}";
        }
        else
        {
            SpherePrimary = string.Empty;
            SphereSecondary = string.Empty;
            // 扫码后保留反馈；空闲时提示输入配对码
            if (!string.IsNullOrWhiteSpace(DetailText) &&
                (DetailText.Contains("已填", StringComparison.Ordinal) ||
                 DetailText.Contains("可达", StringComparison.Ordinal) ||
                 DetailText.Contains("失败", StringComparison.Ordinal) ||
                 DetailText.Contains("不可达", StringComparison.Ordinal) ||
                 DetailText.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            {
                CenterHint = DetailText.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? "地址已填入，请输入 6 位配对码"
                    : DetailText;
            }
            else
                CenterHint = "输入配对码后自动连接";
            FooterPrimary = ShortHost;
        }
    }

    private static string FormatShortHost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "未设置地址";
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return url.Trim();
        return string.IsNullOrEmpty(uri.Port.ToString()) || uri.IsDefaultPort
            ? uri.Host
            : $"{uri.Host}:{uri.Port}";
    }

    private SyncClientConfig CurrentConfig() => new()
    {
        BaseUrl = BaseUrl.Trim(),
        DeviceName = DeviceName.Trim(),
        DeviceId = _settings.LoadSync().DeviceId,
        DeviceToken = _settings.LoadSync().DeviceToken,
        LastPullSeq = LastPullSeq
    };

    private void PersistPartial(Action<SyncClientConfig> mutate)
    {
        var cfg = _settings.LoadSync();
        cfg.BaseUrl = BaseUrl.Trim();
        cfg.DeviceName = DeviceName.Trim();
        cfg.LastPullSeq = LastPullSeq;
        mutate(cfg);
        _settings.SaveSync(cfg);
        ReloadFromStore();
    }

    public void ApplyScannedBaseUrl(string url)
    {
        BaseUrl = url.Trim();
        PersistPartial(_ => { });
        StatusText = IsPaired ? "已连接" : "未配对";
        DetailText = BaseUrl;
        CenterHint = IsPaired
            ? DetailText
            : "地址已填入，请输入 6 位配对码";
        RefreshChrome();
    }

    /// <summary>扫码成功：写入地址；若二维码含配对码则自动配对。</summary>
    public async Task ApplyScannedBaseUrlAsync(string url, string? pairCode = null)
    {
        ApplyScannedBaseUrl(url);

        if (!string.IsNullOrWhiteSpace(pairCode) && pairCode.Trim().Length == 6)
        {
            var code = pairCode.Trim();
            StatusText = "正在配对…";
            DetailText = $"地址 {BaseUrl} · 码 {code}";
            CenterHint = "扫码配对中…";
            RefreshChrome();

            _suppressAutoPair = true;
            PairingCode = code;
            _suppressAutoPair = false;
            _lastAutoPairAttempt = code;
            await PairCommand.ExecuteAsync(null);

            if (IsPaired)
            {
                StatusText = "配对成功";
                DetailText = $"已绑定 · {BaseUrl}";
                CenterHint = "点按球体以对齐";
            }
            else
            {
                CenterHint = string.IsNullOrWhiteSpace(DetailText)
                    ? "扫码配对失败，请重试或手输配对码"
                    : DetailText;
            }

            RefreshChrome();
            return;
        }

        try
        {
            var health = await _client.HealthAsync(BaseUrl);
            if (health.Ok)
            {
                StatusText = IsPaired ? "已连接" : "地址可用";
                DetailText = $"已填入 {BaseUrl} · 服务可达";
                CenterHint = IsPaired
                    ? DetailText
                    : "地址可用，请输入 6 位配对码（或重新开始配对后扫新码）";
            }
            else
            {
                StatusText = "地址已填";
                DetailText = $"已填入 {BaseUrl} · 服务异常，请确认 PC 已开始配对";
                CenterHint = DetailText;
            }
        }
        catch (Exception ex)
        {
            StatusText = "地址已填";
            DetailText = $"已填入 {BaseUrl} · 暂不可达：{ex.Message}";
            CenterHint = "地址已填入，请确认与 PC 同一 Wi‑Fi 后输入配对码";
        }

        RefreshChrome();
    }

    partial void OnPairingCodeChanged(string value)
    {
        if (_suppressAutoPair) return;

        var digits = new string((value ?? string.Empty).Where(char.IsDigit).Take(6).ToArray());
        if (digits != value)
        {
            _suppressAutoPair = true;
            PairingCode = digits;
            _suppressAutoPair = false;
            return;
        }

        if (digits.Length < 6)
        {
            _lastAutoPairAttempt = string.Empty;
            return;
        }

        if (IsPaired || IsBusy || digits == _lastAutoPairAttempt)
            return;

        _lastAutoPairAttempt = digits;
        _ = PairAsync();
    }

    partial void OnIsBusyChanged(bool value) => RefreshChrome();
    partial void OnIsPairedChanged(bool value)
    {
        if (value) StartPairWatch();
        else StopPairWatch();
        RefreshChrome();
    }
    partial void OnPendingPushCountChanged(int value) => RefreshChrome();
    partial void OnDetailTextChanged(string value) => RefreshChrome();
    partial void OnBaseUrlChanged(string value) => ShortHost = FormatShortHost(value);

    [RelayCommand]
    private async Task OpenScanAsync() =>
        await Shell.Current.GoToAsync("syncscan");

    [RelayCommand]
    private async Task OpenConflictsAsync() =>
        await Shell.Current.GoToAsync("syncconflicts");

    [RelayCommand]
    private async Task OpenConnectionAsync() =>
        await Shell.Current.GoToAsync("syncconnection");

    [RelayCommand]
    private async Task OpenStationsAsync() =>
        await Shell.Current.GoToAsync("syncstations");

    [RelayCommand]
    private async Task CheckHealthAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            PersistPartial(_ => { });
            var health = await _client.HealthAsync(BaseUrl);
            RemoteMaxSeq = health.MaxSeq;
            StatusText = health.Ok ? "服务可达" : "服务异常";
            DetailText = $"api_version={health.ApiVersion} · max_seq={health.MaxSeq}";
        }
        catch (Exception ex)
        {
            StatusText = "无法连接";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PairAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(PairingCode) || PairingCode.Trim().Length != 6)
        {
            StatusText = "配对失败";
            DetailText = "请输入 6 位配对码。";
            RefreshChrome();
            return;
        }

        IsBusy = true;
        try
        {
            PersistPartial(_ => { });
            var pair = await _client.PairAsync(BaseUrl, PairingCode, DeviceName);
            PersistPartial(cfg =>
            {
                cfg.DeviceId = pair.DeviceId;
                cfg.DeviceToken = pair.DeviceToken;
                if (!string.IsNullOrWhiteSpace(pair.DeviceName))
                    cfg.DeviceName = pair.DeviceName;
            });
            _suppressAutoPair = true;
            PairingCode = string.Empty;
            _suppressAutoPair = false;
            _lastAutoPairAttempt = string.Empty;
            StatusText = "配对成功";
            DetailText = $"已绑定 {pair.DeviceId}";
        }
        catch (Exception ex)
        {
            StatusText = "配对失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshChrome();
        }
    }

    [RelayCommand]
    private async Task AlignAsync()
    {
        if (IsBusy || !IsPaired) return;
        IsBusy = true;
        try
        {
            var cfg = CurrentConfig();
            if (string.IsNullOrWhiteSpace(cfg.DeviceId) || string.IsNullOrWhiteSpace(cfg.DeviceToken))
            {
                StatusText = "未配对";
                DetailText = "请先完成配对。";
                return;
            }

            var pending = _pushQueue.Load();
            PendingPushCount = pending.Count;
            var push = await _client.PushAsync(cfg, pending);
            RemoteMaxSeq = push.MaxSeq;
            if (push.Errors.Count == 0 && pending.Count > 0)
                _pushQueue.Clear();
            PendingPushCount = _pushQueue.Count;

            var pulled = 0;
            var after = LastPullSeq;
            SyncPullResponse? lastPull = null;
            do
            {
                lastPull = await _client.PullAsync(cfg, after);
                if (lastPull.Changes.Count > 0)
                {
                    _pullBuffer.Append(lastPull.Changes);
                    pulled += lastPull.Changes.Count;
                    after = lastPull.Changes.Max(c => c.Seq);
                }
            } while (lastPull.HasMore);

            if (lastPull != null)
            {
                after = Math.Max(after, lastPull.MaxSeq);
                RemoteMaxSeq = lastPull.MaxSeq;
            }

            var buffered = _pullBuffer.Load();
            var apply = _ingress.Apply(buffered);
            if (apply.Errors.Count == 0)
                _pullBuffer.Clear();

            LastPullSeq = after;
            PersistPartial(c => c.LastPullSeq = after);
            BufferedPullCount = _pullBuffer.Count;

            await RefreshConflictsInternalAsync(cfg);

            WeakReferenceMessenger.Default.Send(new TripsDataChangedMessage());
            _hasAlignedThisSession = true;

            StatusText = "对齐完成";
            var errHint = apply.Errors.Count == 0
                ? ""
                : $" · 入库错误 {apply.Errors.Count}";
            var conflictHint = HasConflicts ? $" · 冲突 {Conflicts.Count}" : "";
            if (pulled == 0 && apply.Applied == 0)
            {
                DetailText =
                    $"推送 {push.Accepted} · 拉取 0 · seq={RemoteMaxSeq}{conflictHint}";
            }
            else
            {
                DetailText =
                    $"推送 {push.Accepted} · 拉取 {pulled} · 入库 {apply.Applied}{errHint}{conflictHint}";
            }
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthFailureAsync(ex))
                return;
            StatusText = "对齐失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshChrome();
        }
    }

    [RelayCommand]
    private async Task RefreshConflictsAsync()
    {
        if (IsBusy || !IsPaired) return;
        IsBusy = true;
        try
        {
            await RefreshConflictsInternalAsync(CurrentConfig());
            StatusText = HasConflicts ? $"待处理冲突 {Conflicts.Count}" : "无待处理冲突";
            DetailText = HasConflicts
                ? "选择「保留电脑」或「采用手机」。"
                : "冲突箱为空。";
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthFailureAsync(ex))
                return;
            StatusText = "拉取冲突失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshChrome();
        }
    }

    private async Task RefreshConflictsInternalAsync(SyncClientConfig cfg)
    {
        var list = await _client.ConflictsAsync(cfg);
        Conflicts = new ObservableCollection<SyncConflictDto>(list.Conflicts ?? new List<SyncConflictDto>());
        HasConflicts = Conflicts.Count > 0;
        ConflictBadgeText = Conflicts.Count.ToString();
    }

    [RelayCommand]
    private async Task ResolveKeepLocalAsync(SyncConflictDto? item)
    {
        if (item == null) return;
        await ResolveAsync(item.Id, "local");
    }

    [RelayCommand]
    private async Task ResolveKeepRemoteAsync(SyncConflictDto? item)
    {
        if (item == null) return;
        await ResolveAsync(item.Id, "remote");
    }

    private async Task ResolveAsync(long id, string keep)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _client.ResolveConflictAsync(CurrentConfig(), id, keep);
            if (!result.Ok)
            {
                StatusText = "解决冲突失败";
                DetailText = result.Error ?? "unknown";
                return;
            }

            await RefreshConflictsInternalAsync(CurrentConfig());
            StatusText = "冲突已处理";
            DetailText = keep == "local" ? "已保留电脑版。" : "已采用手机稿并写入 PC。";
            if (keep == "remote")
                WeakReferenceMessenger.Default.Send(new TripsDataChangedMessage());
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthFailureAsync(ex))
                return;
            StatusText = "解决冲突失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshChrome();
        }
    }

    [RelayCommand]
    private async Task SyncStationsAsync()
    {
        if (IsBusy || !IsPaired) return;
        IsBusy = true;
        try
        {
            var resp = await _client.StationsAsync(CurrentConfig());
            var rows = (resp.Stations ?? new List<SyncStationDto>())
                .Select(s => (s.StationName, s.StationCode, s.StationPinyin));
            _stations.UpsertMany(rows);
            StationsHint = $"本地车站缓存 {_stations.Count()} 条（刚同步 {resp.Stations?.Count ?? 0}）";
            StatusText = "车站库已同步";
            DetailText = StationsHint;
        }
        catch (Exception ex)
        {
            if (await TryHandleAuthFailureAsync(ex))
                return;
            StatusText = "车站同步失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshChrome();
        }
    }

    [RelayCommand]
    private async Task UnpairAsync()
    {
        if (!IsPaired) return;

        var page = Shell.Current?.CurrentPage;
        var confirm = page != null &&
                      await page.DisplayAlert(
                          "解除配对",
                          "解除后本机与电脑将断开同步，需重新扫码配对。确定解除？",
                          "解除",
                          "取消");
        if (!confirm) return;

        IsBusy = true;
        try
        {
            var cfg = CurrentConfig();
            try
            {
                await _client.UnpairAsync(cfg);
            }
            catch (SyncUnauthorizedException)
            {
                // 对端已撤销，本机仍清凭证
            }
            catch (Exception ex)
            {
                // 网络失败仍清本机，避免卡在「假已配对」
                CrashLog.Write("Unpair.NotifyPc", ex);
            }

            ClearLocalPairing(
                "已解除配对",
                "本机凭证已清除。电脑端设备列表将同步移除。");
            if (page != null)
                await page.DisplayAlert("已解除配对", "如需同步，请在电脑重新「开始配对」后扫码。", "好");
        }
        finally
        {
            IsBusy = false;
            RefreshChrome();
        }
    }

    /// <summary>凭证被 PC 撤销或失效时：清本地并提示。其它模块（如采集 OCR）共用此入口。</summary>
    public async Task<bool> TryHandleAuthFailureAsync(Exception ex)
    {
        if (ex is not SyncUnauthorizedException &&
            !string.Equals(ex.Message, "revoked", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ex.Message, "unauthorized", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ex.Message, "http_401", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!IsPaired) return true;

        var revoked = ex is SyncUnauthorizedException ue && ue.IsRevoked
                      || string.Equals(ex.Message, "revoked", StringComparison.OrdinalIgnoreCase);
        ClearLocalPairing(
            "配对已失效",
            revoked
                ? "电脑端已解除配对，本机凭证已清除。"
                : "同步凭证无效，本机已退出配对。");

        if (_kickDialogShowing) return true;
        _kickDialogShowing = true;
        try
        {
            var page = Shell.Current?.CurrentPage;
            if (page != null)
            {
                await page.DisplayAlert(
                    "配对已解除",
                    revoked
                        ? "电脑端已撤销本机配对。如需继续同步，请重新扫码配对。"
                        : "同步凭证已失效。请重新扫码配对。",
                    "好");
            }
        }
        finally
        {
            _kickDialogShowing = false;
        }

        return true;
    }

    private void ClearLocalPairing(string status, string detail)
    {
        PersistPartial(cfg =>
        {
            cfg.DeviceId = null;
            cfg.DeviceToken = null;
            cfg.LastPullSeq = 0;
        });
        LastPullSeq = 0;
        Conflicts.Clear();
        HasConflicts = false;
        ConflictBadgeText = "0";
        _hasAlignedThisSession = false;
        StatusText = status;
        DetailText = detail;
        CenterHint = "输入配对码后自动连接";
        RefreshChrome();
    }

    [RelayCommand]
    private void SaveServer()
    {
        PersistPartial(_ => { });
        StatusText = "已保存";
        DetailText = $"服务地址：{BaseUrl}";
        RefreshChrome();
    }
}

