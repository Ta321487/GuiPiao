using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class SyncPage : ContentPage
{
    private IDispatcherTimer? _breatheTimer;
    private bool _breatheExpand = true;

    public SyncPage(SyncViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        // 虚线环用代码设置，避免部分 Android 对 XAML StrokeDashArray 解析异常
        UnpairedRing.StrokeDashArray = [6, 5];
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SyncViewModel vm)
        {
            try
            {
                vm.ReloadFromStore();
                if (vm.IsPaired)
                    _ = SafeRefreshConflictsAsync(vm);
            }
            catch (Exception ex)
            {
                CrashLog.Write("SyncPage.OnAppearing", ex);
            }
        }

        StartUnpairedBreathing();
    }

    protected override void OnDisappearing()
    {
        StopUnpairedBreathing();
        base.OnDisappearing();
    }

    private static async Task SafeRefreshConflictsAsync(SyncViewModel vm)
    {
        try
        {
            await vm.RefreshConflictsCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            CrashLog.Write("SyncPage.RefreshConflicts", ex);
        }
    }

    private void StartUnpairedBreathing()
    {
        StopUnpairedBreathing();
        if (BindingContext is not SyncViewModel { IsPaired: false })
            return;

        _breatheExpand = true;
        _breatheTimer = Dispatcher.CreateTimer();
        _breatheTimer.Interval = TimeSpan.FromMilliseconds(900);
        _breatheTimer.Tick += OnBreatheTick;
        _breatheTimer.Start();
    }

    private async void OnBreatheTick(object? sender, EventArgs e)
    {
        if (!UnpairedRing.IsVisible)
        {
            StopUnpairedBreathing();
            return;
        }

        try
        {
            var target = _breatheExpand ? 1.03 : 1.0;
            _breatheExpand = !_breatheExpand;
            await UnpairedRing.ScaleToAsync(target, 850, Easing.SinInOut);
        }
        catch (Exception ex)
        {
            CrashLog.Write("SyncPage.Breathe", ex);
            StopUnpairedBreathing();
        }
    }

    private void StopUnpairedBreathing()
    {
        if (_breatheTimer != null)
        {
            _breatheTimer.Stop();
            _breatheTimer.Tick -= OnBreatheTick;
            _breatheTimer = null;
        }

        if (UnpairedRing != null)
            UnpairedRing.Scale = 1;
    }
}
