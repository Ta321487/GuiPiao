using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Models;
using GuiPiao.Services;
using GuiPiao.View;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

/// <summary>
///     OCR 识别 / 粘贴导入窗口（粘贴文本 + 选图 CnOCR，支持多选逐张识别）。
/// </summary>
public partial class OcrRecognizeTicketViewModel : ObservableObject
{
    private readonly TicketTextExtractor _extractor = new();
    private readonly OcrRecognitionService _ocrService = new();
    private readonly OcrEnvironmentService _envService = new();
    private bool? _ocrEnvironmentReady;
    private bool _suppressRawTextSync;

    [ObservableProperty] private string _rawText = string.Empty;

    [ObservableProperty] private string _statusMessage = "粘贴短信或订单文本，或切换至图片识别。";

    [ObservableProperty] private bool _isPasteMode = true;

    [ObservableProperty] private bool _isImageMode;

    [ObservableProperty] private string _selectedImagePath = string.Empty;

    [ObservableProperty] private BitmapImage? _previewImage;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _busyProgressText = string.Empty;

    /// <summary>与 OCR 设置页测试识别相同的阶段进度 0–100。</summary>
    [ObservableProperty] private int _busyProgressPercent;

    [ObservableProperty] private int _currentImageIndex;

    /// <summary>多图队列（路径 + 每张识别原文）。</summary>
    public ObservableCollection<OcrImageQueueItem> ImageQueue { get; } = new();

    public bool HasSelectedImage => ImageQueue.Count > 0;

    public bool HasMultipleImages => ImageQueue.Count > 1;

    public string ImagePositionText =>
        ImageQueue.Count == 0
            ? string.Empty
            : $"{CurrentImageIndex + 1} / {ImageQueue.Count}";

    /// <summary>选图模式下是否已有识别原文（用于展示可编辑文本区）。</summary>
    public bool HasOcrResultText => IsImageMode && !string.IsNullOrWhiteSpace(RawText);

    /// <summary>解析成功后的识别稿（单张/粘贴）；多图时为第一张有效稿。</summary>
    public TicketImportDraft? ResultDraft { get; private set; }

    /// <summary>多图时每张一张识别稿（按顺序）。</summary>
    public IReadOnlyList<TicketImportDraft> ResultDrafts { get; private set; } = Array.Empty<TicketImportDraft>();

    public bool DialogConfirmed { get; private set; }

    public Action? RequestClose { get; set; }

    partial void OnIsPasteModeChanged(bool value)
    {
        if (value && IsImageMode)
            IsImageMode = false;
        if (value)
            StatusMessage = "粘贴短信或订单文本后执行「识别并预填」。支持 Ctrl+V。";
        OnPropertyChanged(nameof(HasOcrResultText));
    }

    partial void OnIsImageModeChanged(bool value)
    {
        if (value && IsPasteMode)
            IsPasteMode = false;
        if (value)
            StatusMessage = HasSelectedImage
                ? $"已选择图片 {ImageQueue.Count} 张。"
                : "选择票面或订单截图（支持多选），将依次执行 OCR。";
        OnPropertyChanged(nameof(HasOcrResultText));
    }

    partial void OnCurrentImageIndexChanged(int value)
    {
        ShowCurrentQueueItem();
        OnPropertyChanged(nameof(ImagePositionText));
        PreviousImageCommand.NotifyCanExecuteChanged();
        NextImageCommand.NotifyCanExecuteChanged();
        RecognizeImageCommand.NotifyCanExecuteChanged();
    }

    partial void OnRawTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasOcrResultText));
        if (_suppressRawTextSync) return;
        if (IsImageMode && CurrentImageIndex >= 0 && CurrentImageIndex < ImageQueue.Count)
            ImageQueue[CurrentImageIndex].RawText = value ?? string.Empty;
    }

    [RelayCommand]
    private void PasteFromClipboard()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                RawText = Clipboard.GetText();
                StatusMessage = "已粘贴文本，可执行「识别并预填」。";
            }
            else
            {
                StatusMessage = "剪贴板无可用文本。";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"读取剪贴板失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearText()
    {
        RawText = string.Empty;
        if (IsImageMode)
        {
            ImageQueue.Clear();
            SelectedImagePath = string.Empty;
            PreviewImage = null;
            CurrentImageIndex = 0;
            NotifyQueueChanged();
            StatusMessage = "已清除图片队列。请重新选择图片。";
        }
        else
        {
            StatusMessage = "已清除文本。";
        }
    }

    [RelayCommand]
    private async Task SelectImageAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择图片（支持多选）",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp|所有文件|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
            return;

        IsImageMode = true;
        ImageQueue.Clear();
        foreach (var path in dialog.FileNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(path))
                ImageQueue.Add(new OcrImageQueueItem { ImagePath = path });
        }

        if (ImageQueue.Count == 0)
        {
            StatusMessage = "未选择有效图片文件。";
            NotifyQueueChanged();
            return;
        }

        CurrentImageIndex = 0;
        ShowCurrentQueueItem();
        NotifyQueueChanged();
        StatusMessage = ImageQueue.Count == 1
            ? $"已选择：{Path.GetFileName(ImageQueue[0].ImagePath)}。开始识别…"
            : $"已选择 {ImageQueue.Count} 张图片。开始依次识别…";

        await RunOcrOnQueueAsync(onlyCurrent: false);
    }

    [RelayCommand(CanExecute = nameof(CanReRecognize))]
    private async Task RecognizeImageAsync()
    {
        // 多图时整队重跑；单图等价于只识别当前张
        await RunOcrOnQueueAsync(onlyCurrent: false);
    }

    private bool CanReRecognize() => !IsBusy && HasSelectedImage;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousImage()
    {
        if (CurrentImageIndex > 0)
            CurrentImageIndex--;
    }

    private bool CanGoPrevious() => !IsBusy && CurrentImageIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextImage()
    {
        if (CurrentImageIndex < ImageQueue.Count - 1)
            CurrentImageIndex++;
    }

    private bool CanGoNext() => !IsBusy && CurrentImageIndex < ImageQueue.Count - 1;

    [RelayCommand(CanExecute = nameof(CanRecognizeAndPrefill))]
    private async Task RecognizeAndPrefillAsync()
    {
        if (IsImageMode)
        {
            if (ImageQueue.Count == 0)
            {
                MessageBoxWindow.Show(
                    Application.Current.MainWindow,
                    "尚未选择图片。请选择图片，或切换至「粘贴文本」。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            // 尚无任一原文时整队识别；否则用已有（含手改）原文抽字段
            if (ImageQueue.All(x => string.IsNullOrWhiteSpace(x.RawText)))
            {
                var ocrOk = await RunOcrOnQueueAsync(onlyCurrent: false);
                if (!ocrOk)
                    return;
            }

            var drafts = new List<TicketImportDraft>();
            for (var i = 0; i < ImageQueue.Count; i++)
            {
                var text = ImageQueue[i].RawText;
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                var draft = _extractor.Extract(text);
                if (!draft.HasAnyField)
                    continue;
                if (ImageQueue.Count > 1)
                    draft.SourceHint = $"{draft.SourceHint}·图{i + 1}";
                drafts.Add(draft);
            }

            if (drafts.Count == 0)
            {
                MessageBoxWindow.Show(
                    Application.Current.MainWindow,
                    "未能解析出行程字段。\n请切换图片核对识别原文后重试，或改为手工录入。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                StatusMessage = "未解析到有效字段。";
                return;
            }

            ResultDrafts = drafts;
            ResultDraft = drafts[0];
            DialogConfirmed = true;
            RequestClose?.Invoke();
            return;
        }

        if (string.IsNullOrWhiteSpace(RawText))
        {
            MessageBoxWindow.Show(
                Application.Current.MainWindow,
                "文本为空。请粘贴短信或订单内容。",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var pasteDraft = _extractor.Extract(RawText);
        if (!pasteDraft.HasAnyField)
        {
            MessageBoxWindow.Show(
                Application.Current.MainWindow,
                "未能解析出行程字段。\n请修订原文后重试，或改为手工录入。",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            StatusMessage = "未解析到有效字段。";
            return;
        }

        ResultDrafts = new[] { pasteDraft };
        ResultDraft = pasteDraft;
        DialogConfirmed = true;
        RequestClose?.Invoke();
    }

    private bool CanRecognizeAndPrefill() => !IsBusy;

    [RelayCommand]
    private void OpenOcrSettings()
    {
        var settings = new SettingsWindow(SettingsPageType.OCR)
        {
            Owner = Application.Current.MainWindow
        };
        settings.ShowDialog();
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsBusy) return;
        DialogConfirmed = false;
        ResultDraft = null;
        ResultDrafts = Array.Empty<TicketImportDraft>();
        RequestClose?.Invoke();
    }

    private void ShowCurrentQueueItem()
    {
        if (ImageQueue.Count == 0 || CurrentImageIndex < 0 || CurrentImageIndex >= ImageQueue.Count)
        {
            SelectedImagePath = string.Empty;
            PreviewImage = null;
            _suppressRawTextSync = true;
            RawText = string.Empty;
            _suppressRawTextSync = false;
            return;
        }

        var item = ImageQueue[CurrentImageIndex];
        SelectedImagePath = item.ImagePath;
        LoadPreviewImage(item.ImagePath);
        _suppressRawTextSync = true;
        RawText = item.RawText;
        _suppressRawTextSync = false;
    }

    private void NotifyQueueChanged()
    {
        OnPropertyChanged(nameof(HasSelectedImage));
        OnPropertyChanged(nameof(HasMultipleImages));
        OnPropertyChanged(nameof(ImagePositionText));
        RecognizeImageCommand.NotifyCanExecuteChanged();
        PreviousImageCommand.NotifyCanExecuteChanged();
        NextImageCommand.NotifyCanExecuteChanged();
    }

    private void LoadPreviewImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            PreviewImage = null;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            PreviewImage = bitmap;
        }
        catch
        {
            PreviewImage = null;
        }
    }

    private async Task<bool> EnsureOcrEnvironmentAsync()
    {
        if (_ocrEnvironmentReady == true)
            return true;

        BusyProgressText = "检测 OCR 环境…";
        StatusMessage = "检测 OCR 环境…";
        BusyProgressPercent = 10;
        SetBusy(true);

        try
        {
            var hasRec = _envService.CheckRecognitionModelInstalled();
            var hasDet = _envService.CheckDetectionModelInstalled();
            if (!hasRec || !hasDet)
            {
                _ocrEnvironmentReady = false;
            }
            else
            {
                var python = await _envService.CheckPythonInstalled();
                var cnocr = python.installed && python.isVersionValid &&
                            await _envService.CheckCnocrInstalled();
                _ocrEnvironmentReady = cnocr;
            }

            if (_ocrEnvironmentReady == true)
                return true;
        }
        finally
        {
            SetBusy(false);
            BusyProgressText = string.Empty;
            BusyProgressPercent = 0;
        }

        var result = MessageBoxWindow.Show(
            Application.Current.MainWindow,
            "OCR 环境未就绪（需 Python、CnOCR 及模型）。\n可打开 OCR 配置完成安装，或切换至粘贴文本。",
            "OCR 环境未就绪",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
            "打开 OCR 配置",
            "切换粘贴文本",
            cancelText: "关闭");

        switch (result)
        {
            case MessageBoxResult.Yes:
                OpenOcrSettings();
                _ocrEnvironmentReady = null;
                break;
            case MessageBoxResult.No:
                IsPasteMode = true;
                StatusMessage = "已切换至粘贴文本模式。";
                break;
        }

        return false;
    }

    /// <param name="onlyCurrent">true=仅当前图片；false=队列全部。</param>
    private async Task<bool> RunOcrOnQueueAsync(bool onlyCurrent)
    {
        if (ImageQueue.Count == 0)
        {
            StatusMessage = "未选择图片。";
            return false;
        }

        if (!await EnsureOcrEnvironmentAsync())
            return false;

        var indices = onlyCurrent
            ? new[] { CurrentImageIndex }
            : Enumerable.Range(0, ImageQueue.Count).ToArray();

        SetBusy(true);
        BusyProgressPercent = 0;
        var okCount = 0;
        var failCount = 0;

        try
        {
            for (var step = 0; step < indices.Length; step++)
            {
                var idx = indices[step];
                if (idx < 0 || idx >= ImageQueue.Count)
                    continue;

                var item = ImageQueue[idx];
                if (!File.Exists(item.ImagePath))
                {
                    failCount++;
                    continue;
                }

                CurrentImageIndex = idx;
                var label = indices.Length == 1
                    ? Path.GetFileName(item.ImagePath)
                    : $"{step + 1}/{indices.Length} {Path.GetFileName(item.ImagePath)}";

                StatusMessage = $"识别中：{label}";
                var basePercent = indices.Length == 0 ? 0 : (int)(step * 100.0 / indices.Length);

                var progress = new Progress<string>(msg =>
                {
                    BusyProgressText = msg;
                    StatusMessage = $"{label} · {msg}";
                    var stage = MapStagePercent(msg);
                    BusyProgressPercent = Math.Min(99,
                        basePercent + (int)(stage / 100.0 * (100.0 / indices.Length)));
                });

                try
                {
                    var results = await _ocrService.RecognizeAsync(item.ImagePath, progress);
                    if (results.Count == 0)
                    {
                        item.RawText = string.Empty;
                        failCount++;
                        continue;
                    }

                    item.RawText = JoinOcrTexts(results);
                    okCount++;
                    if (idx == CurrentImageIndex)
                    {
                        _suppressRawTextSync = true;
                        RawText = item.RawText;
                        _suppressRawTextSync = false;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    StatusMessage = $"{label} · 失败：{ex.Message}";
                    if (indices.Length == 1)
                    {
                        MessageBoxWindow.Show(
                            Application.Current.MainWindow,
                            $"OCR 识别失败：\n{ex.Message}",
                            "错误",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            BusyProgressPercent = 100;
            OnPropertyChanged(nameof(HasOcrResultText));

            if (okCount == 0)
            {
                StatusMessage = indices.Length > 1
                    ? "全部图片未识别到文字。请更换清晰图像，或切换至粘贴文本。"
                    : "未识别到文字。请更换清晰图像，或切换至粘贴文本。";
                return false;
            }

            StatusMessage = failCount == 0
                ? (okCount == 1
                    ? "识别完成。可编辑原文后执行「识别并预填」。"
                    : $"识别完成：{okCount} 张。可切换图片核对原文后执行「识别并预填」。")
                : $"识别结束：成功 {okCount}，失败 {failCount}。可切换图片后执行「识别并预填」。";
            return true;
        }
        finally
        {
            SetBusy(false);
            BusyProgressText = string.Empty;
            BusyProgressPercent = 0;
        }
    }

    private void SetBusy(bool busy)
    {
        IsBusy = busy;
        RecognizeImageCommand.NotifyCanExecuteChanged();
        RecognizeAndPrefillCommand.NotifyCanExecuteChanged();
        PreviousImageCommand.NotifyCanExecuteChanged();
        NextImageCommand.NotifyCanExecuteChanged();
    }

    private static int MapStagePercent(string msg)
    {
        if (msg.Contains("初始化", StringComparison.Ordinal)) return 20;
        if (msg.Contains("执行", StringComparison.Ordinal)) return 50;
        if (msg.Contains("解析", StringComparison.Ordinal)) return 80;
        if (msg.Contains("完成", StringComparison.Ordinal)) return 100;
        return 50;
    }

    /// <summary>按大致阅读顺序拼接 OCR 块。</summary>
    public static string JoinOcrTexts(IReadOnlyList<OcrResult> results)
    {
        static double Top(OcrResult r)
        {
            if (r.Position == null || r.Position.Count == 0) return 0;
            return r.Position.Min(p => p.Count > 1 ? p[1] : 0);
        }

        static double Left(OcrResult r)
        {
            if (r.Position == null || r.Position.Count == 0) return 0;
            return r.Position.Min(p => p.Count > 0 ? p[0] : 0);
        }

        var ordered = results
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .OrderBy(Top)
            .ThenBy(Left)
            .Select(r => r.Text.Trim());

        return string.Join(" ", ordered);
    }
}

/// <summary>多图 OCR 队列项。</summary>
public class OcrImageQueueItem
{
    public string ImagePath { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
}
