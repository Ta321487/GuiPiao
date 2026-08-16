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

    [ObservableProperty] private string _baseUrl = "http://127.0.0.1:17880";
    [ObservableProperty] private string _deviceName = "GuiPiao Mobile";
    [ObservableProperty] private string _pairingCode = string.Empty;
    [ObservableProperty] private string _statusText = "未配对";
    [ObservableProperty] private string _detailText =
        "在 PC 设置 → 同步点击「开始配对」，确认服务地址后输入配对码。";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isPaired;
    [ObservableProperty] private long _lastPullSeq;
    [ObservableProperty] private long _remoteMaxSeq;
    [ObservableProperty] private int _pendingPushCount;
    [ObservableProperty] private int _bufferedPullCount;
    [ObservableProperty] private ObservableCollection<SyncConflictDto> _conflicts = new();
    [ObservableProperty] private bool _hasConflicts;
    [ObservableProperty] private string _stationsHint = string.Empty;

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
            ? $"设备 {cfg.DeviceId} · seq={LastPullSeq} · 待推 {PendingPushCount} · 待应用 {_pullBuffer.Count} · 可执行「立即对齐」"
            : "在 PC 设置 → 同步点击「开始配对」，确认服务地址后输入配对码。";
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
            PairingCode = string.Empty;
            StatusText = "配对成功";
            DetailText = $"已绑定 {pair.DeviceId} · 可执行「立即对齐」";
        }
        catch (Exception ex)
        {
            StatusText = "配对失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AlignAsync()
    {
        if (IsBusy) return;
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

            StatusText = "对齐完成";
            var errHint = apply.Errors.Count == 0
                ? ""
                : $" · 入库错误 {apply.Errors.Count}";
            var conflictHint = HasConflicts ? $" · 冲突 {Conflicts.Count}" : "";
            if (pulled == 0 && apply.Applied == 0)
            {
                DetailText =
                    $"推送接受 {push.Accepted} · 拉取 0 · 远端 seq={RemoteMaxSeq}{conflictHint}。若 PC 有行程却拉不到，请在 PC「设置 → 同步」点「发布现有行程」或再点一次「开始配对」，然后重新对齐。";
            }
            else
            {
                DetailText =
                    $"推送接受 {push.Accepted} / 跳过 {push.Skipped} · 拉取 {pulled} · 入库 {apply.Applied}{errHint}{conflictHint} · seq={LastPullSeq}";
            }
        }
        catch (Exception ex)
        {
            StatusText = "对齐失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
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
                ? "选择「保留电脑」或「采用手机」后会对齐水位。"
                : "冲突箱为空。";
        }
        catch (Exception ex)
        {
            StatusText = "拉取冲突失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshConflictsInternalAsync(SyncClientConfig cfg)
    {
        var list = await _client.ConflictsAsync(cfg);
        Conflicts = new ObservableCollection<SyncConflictDto>(list.Conflicts ?? new List<SyncConflictDto>());
        HasConflicts = Conflicts.Count > 0;
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
            StatusText = "解决冲突失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
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
            StatusText = "车站同步失败";
            DetailText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Unpair()
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
        StatusText = "已撤销配对";
        DetailText = "本机凭证已清除，可重新配对。";
    }

    [RelayCommand]
    private void SaveServer()
    {
        PersistPartial(_ => { });
        StatusText = "已保存";
        DetailText = $"服务地址：{BaseUrl}";
    }

    [RelayCommand]
    private async Task PasteServerUrlAsync()
    {
        try
        {
            if (!Clipboard.Default.HasText)
            {
                StatusText = "剪贴板为空";
                return;
            }

            var text = (await Clipboard.Default.GetTextAsync() ?? "").Trim();
            var url = ServerUrlQrHelper.ExtractHttpUrl(text);
            if (string.IsNullOrWhiteSpace(url))
            {
                StatusText = "未识别到服务地址";
                DetailText = "请复制 PC 二维码旁的 ListenUrl，或扫码选图。";
                return;
            }

            BaseUrl = url;
            PersistPartial(_ => { });
            StatusText = "已填入服务地址";
            DetailText = url;
        }
        catch (Exception ex)
        {
            StatusText = "读取失败";
            DetailText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ScanQrFromPhotoAsync()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null) return;

            await using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var decoded = ServerUrlQrHelper.TryDecodeQr(ms.ToArray());
            var url = ServerUrlQrHelper.ExtractHttpUrl(decoded);
            if (string.IsNullOrWhiteSpace(url))
            {
                StatusText = "未识别到二维码地址";
                DetailText = "请对准 PC「服务地址」二维码拍照，或改用剪贴板粘贴。";
                return;
            }

            BaseUrl = url;
            PersistPartial(_ => { });
            StatusText = "扫码成功";
            DetailText = url;
        }
        catch (Exception ex)
        {
            StatusText = "扫码失败";
            DetailText = ex.Message;
        }
    }
}
