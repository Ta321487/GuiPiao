using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Messaging;
using GuiPiao.Mobile.Model;
using GuiPiao.Mobile.Services;

namespace GuiPiao.Mobile.ViewModels;

public partial class TripDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly RideRepository _rides;
    private readonly RideWriteService _write;
    private readonly MobileSettingsStore _settings;

    [ObservableProperty] private string _syncId = string.Empty;
    [ObservableProperty] private MobileRide? _ride;
    [ObservableProperty] private string _title = "行程详情";
    [ObservableProperty] private bool _canRefundOrReschedule;
    [ObservableProperty] private bool _hasRide;
    [ObservableProperty] private bool _showMore;
    [ObservableProperty] private string _moreChevron = "›";

    public TripDetailViewModel(RideRepository rides, RideWriteService write, MobileSettingsStore settings)
    {
        _rides = rides;
        _write = write;
        _settings = settings;
        MoreChevron = Icons.AppIcons.ChevronRight;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("syncId", out var v) && v != null)
            SyncId = Uri.UnescapeDataString(v.ToString() ?? "");
        Reload();
    }

    public void OnAppearing() => Reload();

    private void Reload()
    {
        if (string.IsNullOrWhiteSpace(SyncId))
        {
            ClearMissing("行程详情");
            return;
        }

        try
        {
            Ride = _rides.GetActiveBySyncId(SyncId);
        }
        catch (Exception)
        {
            ClearMissing("加载失败");
            return;
        }

        if (Ride == null)
        {
            ClearMissing("行程不存在");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try { await Shell.Current.GoToAsync(".."); }
                catch { /* ignore */ }
            });
            return;
        }

        HasRide = true;
        Title = string.IsNullOrWhiteSpace(Ride.TrainNo) ? "行程详情" : Ride.TrainNo;
        CanRefundOrReschedule = Ride.Status == 0;
    }

    private void ClearMissing(string title)
    {
        Ride = null;
        HasRide = false;
        Title = title;
        CanRefundOrReschedule = false;
    }

    [RelayCommand]
    private void ToggleMore()
    {
        ShowMore = !ShowMore;
        MoreChevron = ShowMore ? Icons.AppIcons.Up : Icons.AppIcons.ChevronRight;
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncId)) return;
        await Shell.Current.GoToAsync($"tripform?syncId={Uri.EscapeDataString(SyncId)}");
    }

    [RelayCommand]
    private async Task PreviewFaceAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncId)) return;
        await Shell.Current.GoToAsync($"ticketface?syncId={Uri.EscapeDataString(SyncId)}");
    }

    [RelayCommand]
    private async Task RefundAsync()
    {
        if (Ride == null || Ride.Status != 0) return;
        var ok = await Shell.Current.DisplayAlertAsync(
            "退票",
            "确认将此行程标记为「已退票」？对齐后会推送到 PC。",
            "退票",
            "取消");
        if (!ok) return;
        _write.UpdateStatus(SyncId, 3);
        NotifyChanged();
        Reload();
    }

    [RelayCommand]
    private async Task RescheduleAsync()
    {
        if (Ride == null || Ride.Status != 0) return;
        var ok = await Shell.Current.DisplayAlertAsync(
            "改签",
            "原行程将标为「已改签」，并打开新行程表单填写改签后车票。",
            "继续",
            "取消");
        if (!ok) return;
        _write.UpdateStatus(SyncId, 2);
        NotifyChanged();
        await Shell.Current.GoToAsync(
            $"tripform?mode=reschedule&fromSyncId={Uri.EscapeDataString(SyncId)}");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (string.IsNullOrWhiteSpace(SyncId)) return;
        if (_settings.LoadAppearance().ConfirmDelete)
        {
            var ok = await Shell.Current.DisplayAlertAsync(
                "删除",
                "确认删除此行程？（软删，对齐后同步到 PC）",
                "删除",
                "取消");
            if (!ok) return;
        }

        _write.SoftDelete(SyncId);
        NotifyChanged();
        await Shell.Current.GoToAsync("..");
    }

    private static void NotifyChanged() =>
        WeakReferenceMessenger.Default.Send(new TripsDataChangedMessage());
}
