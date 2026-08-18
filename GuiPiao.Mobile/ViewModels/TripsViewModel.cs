using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Messaging;
using GuiPiao.Mobile.Model;
using GuiPiao.Mobile.Services;

namespace GuiPiao.Mobile.ViewModels;

public partial class TripsViewModel : ObservableObject, IRecipient<TripsDataChangedMessage>
{
    /// <summary>与 PC DefaultPageSize 对齐。</summary>
    public const int DefaultPageSize = 20;

    private readonly RideRepository _rides;
    private readonly TagRepository _tags;
    private readonly SyncPullBuffer _pullBuffer;
    private readonly MobileSyncIngressService _ingress;
    private CancellationTokenSource? _searchDebounce;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private ObservableCollection<MobileRide> _items = new();
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private bool _canGoPrevious;
    [ObservableProperty] private bool _canGoNext;
    [ObservableProperty] private bool _showPager;
    [ObservableProperty] private string _pageText = "第 1 / 1 页";
    /// <summary>-1=全部；0未出行 1已完成 2已改签 3已退票。</summary>
    [ObservableProperty] private int _statusFilter = -1;

    public TripsViewModel(
        RideRepository rides,
        TagRepository tags,
        SyncPullBuffer pullBuffer,
        MobileSyncIngressService ingress)
    {
        _rides = rides;
        _tags = tags;
        _pullBuffer = pullBuffer;
        _ingress = ingress;
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(TripsDataChangedMessage message) =>
        MainThread.BeginInvokeOnMainThread(() => Reload(resetPage: true));

    public void OnAppearing()
    {
        ApplyPendingBuffer();
        Reload(resetPage: false);
    }

    private void ApplyPendingBuffer()
    {
        var pending = _pullBuffer.Load();
        if (pending.Count == 0) return;
        var result = _ingress.Apply(pending);
        if (result.Errors.Count == 0)
            _pullBuffer.Clear();
    }

    [RelayCommand]
    private void Reload() => Reload(resetPage: true);

    private void Reload(bool resetPage)
    {
        try
        {
            if (resetPage)
                CurrentPage = 1;

            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
            int? status = StatusFilter < 0 ? null : StatusFilter;
            TotalCount = _rides.CountActive(search, status);
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)DefaultPageSize));
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
            if (CurrentPage < 1)
                CurrentPage = 1;

            var list = _rides.ListActivePage(search, CurrentPage, DefaultPageSize, status);
            var tagMap = _tags.GetTagNamesForRides(list.Select(r => r.SyncId));
            foreach (var ride in list)
            {
                if (tagMap.TryGetValue(ride.SyncId, out var names) && names.Count > 0)
                    ride.TagsText = string.Join(" · ", names);
                else
                    ride.TagsText = string.Empty;
            }

            Items = new ObservableCollection<MobileRide>(list);
            IsEmpty = TotalCount == 0;
            CanGoPrevious = CurrentPage > 1;
            CanGoNext = CurrentPage < TotalPages;
            ShowPager = TotalCount > 0;
            PageText = $"第 {CurrentPage} / {TotalPages} 页";
            StatusText = IsEmpty
                ? "暂无行程。请到「同步」配对并对齐，从 PC 拉取数据。"
                : $"共 {TotalCount} 条 · 每页 {DefaultPageSize} 条";
        }
        catch (Exception ex)
        {
            StatusText = "刷新失败：" + ex.Message;
        }
    }

    [RelayCommand]
    private void Search() => Reload(resetPage: true);

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        _ = DebouncedReloadAsync(token);
    }

    private async Task DebouncedReloadAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(280, token);
            if (token.IsCancellationRequested) return;
            MainThread.BeginInvokeOnMainThread(() => Reload(resetPage: true));
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    partial void OnStatusFilterChanged(int value) => Reload(resetPage: true);

    [RelayCommand]
    private void SetStatusFilter(string? raw)
    {
        if (!int.TryParse(raw, out var n)) return;
        StatusFilter = n;
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (!CanGoPrevious) return;
        CurrentPage--;
        Reload(resetPage: false);
    }

    [RelayCommand]
    private void NextPage()
    {
        if (!CanGoNext) return;
        CurrentPage++;
        Reload(resetPage: false);
    }

    [RelayCommand]
    private async Task OpenDetailAsync(MobileRide? ride)
    {
        if (ride == null || string.IsNullOrWhiteSpace(ride.SyncId)) return;
        await Shell.Current.GoToAsync($"tripdetail?syncId={Uri.EscapeDataString(ride.SyncId)}");
    }

    [RelayCommand]
    private async Task OpenTagsAsync() =>
        await Shell.Current.GoToAsync("tags");

    [RelayCommand]
    private async Task AddAsync()
    {
        await Shell.Current.GoToAsync("tripform");
    }
}
