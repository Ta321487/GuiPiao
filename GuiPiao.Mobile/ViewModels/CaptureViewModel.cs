using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Mobile.Services;
using GuiPiao.Services;

namespace GuiPiao.Mobile.ViewModels;

public partial class CaptureViewModel : ObservableObject
{
    private readonly TicketTextExtractor _extractor = new();
    private readonly CapturePrefillStore _prefill;
    private readonly MobileSettingsStore _settings;
    private readonly SyncApiClient _client;
    private readonly SyncViewModel _sync;

    [ObservableProperty] private string _pasteText = string.Empty;
    [ObservableProperty] private string _statusText =
        "优先拍照；相册与粘贴为辅。图片 OCR 需已配对 PC。";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasPasteText;

    partial void OnPasteTextChanged(string value) =>
        HasPasteText = !string.IsNullOrWhiteSpace(value);

    public CaptureViewModel(
        CapturePrefillStore prefill,
        MobileSettingsStore settings,
        SyncApiClient client,
        SyncViewModel sync)
    {
        _prefill = prefill;
        _settings = settings;
        _client = client;
        _sync = sync;
    }

    [RelayCommand]
    private async Task ParsePasteAsync()
    {
        if (string.IsNullOrWhiteSpace(PasteText))
        {
            StatusText = "请先粘贴文本。";
            return;
        }

        await PrefillFromTextAsync(PasteText, "粘贴文本");
    }

    [RelayCommand]
    private async Task PasteFromClipboardAsync()
    {
        try
        {
            if (Clipboard.Default.HasText)
            {
                PasteText = await Clipboard.Default.GetTextAsync() ?? string.Empty;
                StatusText = string.IsNullOrWhiteSpace(PasteText)
                    ? "剪贴板为空。"
                    : "已粘贴，可改原文后点「用此文本预填表单」。";
            }
            else
                StatusText = "剪贴板没有文本。";
        }
        catch (Exception ex)
        {
            StatusText = "读取剪贴板失败：" + ex.Message;
        }
    }

    [RelayCommand]
    private Task TakePhotoAsync() => AcquireAndOcrAsync(capture: true);

    [RelayCommand]
    private Task PickPhotoAsync() => AcquireAndOcrAsync(capture: false);

    private async Task AcquireAndOcrAsync(bool capture)
    {
        try
        {
            FileResult? photo;
            if (capture)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    StatusText = "当前设备不支持拍照。";
                    return;
                }

                photo = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
                photo = await MediaPicker.Default.PickPhotoAsync();
            }

            if (photo == null)
            {
                StatusText = capture ? "已取消拍照。" : "已取消选择。";
                return;
            }

            await OcrPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            StatusText = (capture ? "拍照失败：" : "选图失败：") + ex.Message;
        }
    }

    private async Task OcrPhotoAsync(FileResult photo)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var cfg = _settings.LoadSync();
            if (string.IsNullOrWhiteSpace(cfg.DeviceId) || string.IsNullOrWhiteSpace(cfg.DeviceToken))
            {
                StatusText = "请先在「同步」配对 PC，图片 OCR 走本机 CnOCR。";
                await Shell.Current.DisplayAlertAsync("采集", "图片 OCR 需要已配对的 PC 服务。", "好的");
                return;
            }

            StatusText = "正在压缩并 OCR…";
            await using var stream = await photo.OpenReadAsync();
            var (bytes, fileName) = await OcrImagePreparer.PrepareAsync(stream, photo.FileName);
            var ocr = await _client.OcrAsync(cfg, bytes, fileName);
            PasteText = ocr.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(PasteText))
            {
                StatusText = "OCR 未识别到文字，可改用粘贴文本。";
                return;
            }

            await PrefillFromTextAsync(PasteText, ocr.SourceHint ?? "图片OCR");
        }
        catch (Exception ex)
        {
            if (await _sync.TryHandleAuthFailureAsync(ex))
            {
                StatusText = "配对已失效，请重新扫码配对。";
                return;
            }

            StatusText = "OCR 失败：" + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrefillFromTextAsync(string text, string sourceHint)
    {
        var draft = _extractor.Extract(text);
        if (!draft.HasAnyField)
        {
            StatusText = "未能抽出字段，请检查文本或到表单手工填写。";
            return;
        }

        draft.SourceHint = string.IsNullOrWhiteSpace(draft.SourceHint) ? sourceHint : draft.SourceHint;
        _prefill.Set(draft);
        var review = draft.FieldsNeedingReview.Count > 0
            ? "待核对：" + string.Join("、", draft.FieldsNeedingReview.Take(6))
            : "请确认后保存";
        StatusText = $"已预填（{draft.SourceHint}）· {review}";
        await Shell.Current.GoToAsync("tripform?prefill=1");
    }

    [RelayCommand]
    private async Task OpenBlankFormAsync() =>
        await Shell.Current.GoToAsync("tripform");
}
