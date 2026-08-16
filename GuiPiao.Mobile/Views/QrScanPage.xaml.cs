using BarcodeScanning;
using GuiPiao.Mobile.Services;
using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

/// <summary>全屏实时扫码（ML Kit / 原生 API），对齐国内「对准即连」体验。</summary>
public partial class QrScanPage : ContentPage
{
    private readonly SyncViewModel _vm;
    private bool _handled;
    private bool _busy;

    public QrScanPage(SyncViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        StatusLabel.Text = "正在准备相机…";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _handled = false;
        try
        {
            await Methods.AskForRequiredPermissionAsync();
            var camStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (camStatus != PermissionStatus.Granted)
            {
                StatusLabel.Text = "需要相机权限。也可使用下方相册/粘贴。";
                CameraView.CameraEnabled = false;
                return;
            }

            CameraView.CameraEnabled = true;
            CameraView.PauseScanning = false;
            StatusLabel.Text = "请对准二维码…";
        }
        catch (Exception ex)
        {
            CrashLog.Write("QrScanPage.OnAppearing", ex);
            StatusLabel.Text = "相机启动失败，请用相册或粘贴。";
            CameraView.CameraEnabled = false;
        }
    }

    protected override void OnDisappearing()
    {
        try { CameraView.CameraEnabled = false; } catch { /* ignore */ }
        base.OnDisappearing();
    }

    private void OnDetectionFinished(object? sender, OnDetectionFinishedEventArg e)
    {
        if (_handled || _busy) return;
        if (e.BarcodeResults == null || e.BarcodeResults.Count == 0) return;

        var raw = e.BarcodeResults
            .Select(r => r.DisplayValue ?? r.RawValue)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        if (string.IsNullOrWhiteSpace(raw)) return;

        MainThread.BeginInvokeOnMainThread(() => _ = ConnectFromRawAsync(raw, "实时扫码"));
    }

    private async void OnAlbumClicked(object? sender, EventArgs e)
    {
        if (_busy || _handled) return;
        try
        {
            CameraView.CameraEnabled = false;
            StatusLabel.Text = "打开相册…";
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null)
            {
                StatusLabel.Text = "已取消。";
                CameraView.CameraEnabled = true;
                CameraView.PauseScanning = false;
                return;
            }

            await using var stream = await photo.OpenReadAsync();
            var results = await Methods.ScanFromImageAsync(stream);
            var decoded = results?
                .Select(r => r.DisplayValue ?? r.RawValue)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (string.IsNullOrWhiteSpace(decoded))
            {
                // 回退 ZXing（部分机型相册图）
                await using var stream2 = await photo.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream2.CopyToAsync(ms);
                decoded = ServerUrlQrHelper.TryDecodeQr(ms.ToArray());
            }

            if (string.IsNullOrWhiteSpace(decoded))
            {
                StatusLabel.Text = "相册未识别到二维码。";
                CameraView.CameraEnabled = true;
                CameraView.PauseScanning = false;
                return;
            }

            await ConnectFromRawAsync(decoded, "相册");
        }
        catch (Exception ex)
        {
            CrashLog.Write("QrScanPage.Album", ex);
            StatusLabel.Text = "相册失败：" + ex.Message;
            CameraView.CameraEnabled = true;
            CameraView.PauseScanning = false;
        }
    }

    private async void OnPasteClicked(object? sender, EventArgs e)
    {
        if (_busy || _handled) return;
        try
        {
            if (!Clipboard.Default.HasText)
            {
                StatusLabel.Text = "剪贴板为空。";
                return;
            }

            var text = (await Clipboard.Default.GetTextAsync() ?? "").Trim();
            await ConnectFromRawAsync(text, "粘贴");
        }
        catch (Exception ex)
        {
            CrashLog.Write("QrScanPage.Paste", ex);
            StatusLabel.Text = "粘贴失败：" + ex.Message;
        }
    }

    private async Task ConnectFromRawAsync(string raw, string source)
    {
        if (_handled || _busy) return;
        _busy = true;
        _handled = true;
        try
        {
            CameraView.PauseScanning = true;
            CameraView.CameraEnabled = false;
            CrashLog.Write("QrScanPage.Raw", new Exception($"{source}: {raw}"));

            var (url, code) = ServerUrlQrHelper.ParseSyncQr(raw);
            if (string.IsNullOrWhiteSpace(url))
            {
                StatusLabel.Text = "不是服务地址：\n" + Truncate(raw, 100);
                _handled = false;
                CameraView.CameraEnabled = true;
                CameraView.PauseScanning = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                StatusLabel.Text = $"仅识别到地址。\n请确认 PC 已「开始配对」且为新二维码。";
                await _vm.ApplyScannedBaseUrlAsync(url, null);
                await Task.Delay(600);
                await Shell.Current.GoToAsync("..");
                return;
            }

            StatusLabel.Text = "识别成功，正在连接…";
            try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(40)); } catch { /* ignore */ }
            await _vm.ApplyScannedBaseUrlAsync(url, code);

            StatusLabel.Text = _vm.IsPaired ? "连接成功" : ("连接失败：" + _vm.DetailText);
            await Task.Delay(400);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            CrashLog.Write("QrScanPage.Connect", ex);
            StatusLabel.Text = "连接失败：" + ex.Message;
            _handled = false;
            CameraView.CameraEnabled = true;
            CameraView.PauseScanning = false;
        }
        finally
        {
            _busy = false;
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
