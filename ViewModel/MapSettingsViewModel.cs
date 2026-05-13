using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

/// <summary>
///     地图设置视图模型
/// </summary>
public partial class MapSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly MapSettingsService _settingsService;

    #region 开发者选项

    [ObservableProperty] private bool _enableDevTools;

    #endregion

    private bool _isLoadingConfig;
    private MapSettingsConfig _originalConfig;

    public MapSettingsViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

        _settingsService = new MapSettingsService();
        LoadSettings();
    }

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (_isLoadingConfig || _originalConfig == null)
                return false;

            var currentConfig = GetCurrentConfig();
            return !ConfigsEqual(_originalConfig, currentConfig);
        }
    }

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    public void ReloadSettings()
    {
        LoadSettings();
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    /// <summary>
    ///     地图设置已保存事件
    /// </summary>
    public static event EventHandler? MapSettingsSaved;

    /// <summary>
    ///     从十六进制字符串创建画刷
    /// </summary>
    private Brush CreateBrushFromHex(string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    /// <summary>
    ///     加载设置
    /// </summary>
    private void LoadSettings()
    {
        _isLoadingConfig = true;
        try
        {
            _originalConfig = _settingsService.Config;

            MapTileSource = _originalConfig.MapTileSource;
            DefaultMapDisplay = _originalConfig.DefaultMapDisplay;
            CompletedTripColor = _originalConfig.CompletedTripColor;
            PendingTripColor = _originalConfig.PendingTripColor;
            RescheduledTripColor = _originalConfig.RescheduledTripColor;
            RefundedTripColor = _originalConfig.RefundedTripColor;
            CompletedLineWidth = _originalConfig.CompletedLineWidth;
            PendingLineWidth = _originalConfig.PendingLineWidth;
            RescheduledLineWidth = _originalConfig.RescheduledLineWidth;
            RefundedLineWidth = _originalConfig.RefundedLineWidth;
            SelectedTripColor = _originalConfig.SelectedTripColor;
            SelectedLineWidth = _originalConfig.SelectedLineWidth;
            StationMarkerColor = _originalConfig.StationMarkerColor;
            MarkerSize = _originalConfig.MarkerSize;
            ShowStationLabels = _originalConfig.ShowStationLabels;
            ShowDateLabels = _originalConfig.ShowDateLabels;
            HighlightSelectedTrip = _originalConfig.HighlightSelectedTrip;
            ShowHoverCard = _originalConfig.ShowHoverCard;
            DirectionFilter = _originalConfig.DirectionFilter;

            EnableMouseWheelZoom = _originalConfig.EnableMouseWheelZoom;
            EnableLeftDragPan = _originalConfig.EnableLeftDragPan;
            EnableRightClickReset = _originalConfig.EnableRightClickReset;
            ZoomSensitivity = _originalConfig.ZoomSensitivity;
            EnablePanInertia = _originalConfig.EnablePanInertia;
            DoubleClickTripAction = _originalConfig.DoubleClickTripAction;
            DoubleClickStationAction = _originalConfig.DoubleClickStationAction;
            DoubleClickBlankAction = _originalConfig.DoubleClickBlankAction;

            EnableHardwareAcceleration = _originalConfig.EnableHardwareAcceleration;
            PreloadMapResources = _originalConfig.PreloadMapResources;
            AutoCleanCache = _originalConfig.AutoCleanCache;

            // 开发者选项
            EnableDevTools = _originalConfig.EnableDevTools;

            // 刷新缓存大小
            RefreshCacheSize();

            _originalConfig = GetCurrentConfig();
        }
        finally
        {
            _isLoadingConfig = false;
        }
    }

    /// <summary>
    ///     从视图模型获取当前配置
    /// </summary>
    private MapSettingsConfig GetCurrentConfig()
    {
        return new MapSettingsConfig
        {
            MapTileSource = MapTileSource,
            DefaultMapDisplay = DefaultMapDisplay,
            CompletedTripColor = CompletedTripColor,
            PendingTripColor = PendingTripColor,
            RescheduledTripColor = RescheduledTripColor,
            RefundedTripColor = RefundedTripColor,
            CompletedLineWidth = CompletedLineWidth,
            PendingLineWidth = PendingLineWidth,
            RescheduledLineWidth = RescheduledLineWidth,
            RefundedLineWidth = RefundedLineWidth,
            SelectedTripColor = SelectedTripColor,
            SelectedLineWidth = SelectedLineWidth,
            StationMarkerColor = StationMarkerColor,
            MarkerSize = MarkerSize,
            ShowStationLabels = ShowStationLabels,
            ShowDateLabels = ShowDateLabels,
            HighlightSelectedTrip = HighlightSelectedTrip,
            ShowHoverCard = ShowHoverCard,
            DirectionFilter = DirectionFilter,
            EnableMouseWheelZoom = EnableMouseWheelZoom,
            EnableLeftDragPan = EnableLeftDragPan,
            EnableRightClickReset = EnableRightClickReset,
            ZoomSensitivity = ZoomSensitivity,
            EnablePanInertia = EnablePanInertia,
            DoubleClickTripAction = DoubleClickTripAction,
            DoubleClickStationAction = DoubleClickStationAction,
            DoubleClickBlankAction = DoubleClickBlankAction,
            EnableHardwareAcceleration = EnableHardwareAcceleration,
            PreloadMapResources = PreloadMapResources,
            AutoCleanCache = AutoCleanCache,

            // 开发者选项
            EnableDevTools = EnableDevTools
        };
    }

    /// <summary>
    ///     比较两个配置是否相等
    /// </summary>
    private bool ConfigsEqual(MapSettingsConfig a, MapSettingsConfig b)
    {
        return a.MapTileSource == b.MapTileSource &&
               a.DefaultMapDisplay == b.DefaultMapDisplay &&
               a.CompletedTripColor == b.CompletedTripColor &&
               a.PendingTripColor == b.PendingTripColor &&
               a.RescheduledTripColor == b.RescheduledTripColor &&
               a.RefundedTripColor == b.RefundedTripColor &&
               a.CompletedLineWidth == b.CompletedLineWidth &&
               a.PendingLineWidth == b.PendingLineWidth &&
               a.RescheduledLineWidth == b.RescheduledLineWidth &&
               a.RefundedLineWidth == b.RefundedLineWidth &&
               a.SelectedTripColor == b.SelectedTripColor &&
               a.SelectedLineWidth == b.SelectedLineWidth &&
               a.StationMarkerColor == b.StationMarkerColor &&
               a.MarkerSize == b.MarkerSize &&
               a.ShowStationLabels == b.ShowStationLabels &&
               a.ShowDateLabels == b.ShowDateLabels &&
               a.HighlightSelectedTrip == b.HighlightSelectedTrip &&
               a.ShowHoverCard == b.ShowHoverCard &&
               a.DirectionFilter == b.DirectionFilter &&
               a.EnableMouseWheelZoom == b.EnableMouseWheelZoom &&
               a.EnableLeftDragPan == b.EnableLeftDragPan &&
               a.EnableRightClickReset == b.EnableRightClickReset &&
               a.ZoomSensitivity == b.ZoomSensitivity &&
               a.EnablePanInertia == b.EnablePanInertia &&
               a.DoubleClickTripAction == b.DoubleClickTripAction &&
               a.DoubleClickStationAction == b.DoubleClickStationAction &&
               a.DoubleClickBlankAction == b.DoubleClickBlankAction &&
               a.EnableHardwareAcceleration == b.EnableHardwareAcceleration &&
               a.PreloadMapResources == b.PreloadMapResources &&
               a.AutoCleanCache == b.AutoCleanCache &&
               a.EnableDevTools == b.EnableDevTools;
    }

    /// <summary>
    ///     保存设置命令
    /// </summary>
    [RelayCommand]
    private async Task SaveSettings()
    {
        await SaveSettingsInternalAsync(true);
    }

    /// <summary>
    ///     保存设置内部实现
    /// </summary>
    private async Task SaveSettingsInternalAsync(bool showMessage)
    {
        // 获取当前设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            var config = GetCurrentConfig();
            var previousConfig = _originalConfig;
            _settingsService.SaveConfig(config);
            _originalConfig = GetCurrentConfig();

            // 触发地图设置已保存事件
            MapSettingsSaved?.Invoke(this, EventArgs.Empty);

            if (showMessage)
                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var restartHints = new List<string>();
                        if (config.EnableHardwareAcceleration != previousConfig.EnableHardwareAcceleration)
                            restartHints.Add("• 硬件加速");
                        if (config.EnableDevTools != previousConfig.EnableDevTools)
                            restartHints.Add("• 开发者工具");
                        if (config.PreloadMapResources != previousConfig.PreloadMapResources)
                            restartHints.Add("• 预加载资源");
                        if (config.AutoCleanCache != previousConfig.AutoCleanCache)
                            restartHints.Add("• 自动清理缓存");
                        if (config.DefaultMapDisplay != previousConfig.DefaultMapDisplay)
                            restartHints.Add("• 默认显示选项");

                        var message = "地图设置已保存";
                        if (restartHints.Count > 0)
                            message += $"\n\n以下设置需重新打开地图窗口后生效：\n{string.Join("\n", restartHints)}";

                        MessageBoxWindow.Show(settingsWindow, message, SettingsDialogMessages.SuccessTitle);
                    });
                });
        }
        catch (Exception ex)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxWindow.Show(settingsWindow,
                        $"{SettingsDialogMessages.SaveFailedPrefix}{ex.Message}", SettingsDialogMessages.ErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            });
        }
    }

    /// <summary>
    ///     恢复默认设置命令
    /// </summary>
    [RelayCommand]
    private async Task RestoreDefaults()
    {
        // 获取当前设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        var result = await Task.Run(() =>
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                return MessageBoxWindow.Show(settingsWindow, SettingsDialogMessages.RestoreConfirmBody,
                    SettingsDialogMessages.ConfirmTitle, MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });
        });

        if (result != MessageBoxResult.Yes)
            return;

        var defaultConfig = _settingsService.GetDefaultConfig();

        MapTileSource = defaultConfig.MapTileSource;
        DefaultMapDisplay = defaultConfig.DefaultMapDisplay;
        CompletedTripColor = defaultConfig.CompletedTripColor;
        PendingTripColor = defaultConfig.PendingTripColor;
        RescheduledTripColor = defaultConfig.RescheduledTripColor;
        RefundedTripColor = defaultConfig.RefundedTripColor;
        CompletedLineWidth = defaultConfig.CompletedLineWidth;
        PendingLineWidth = defaultConfig.PendingLineWidth;
        RescheduledLineWidth = defaultConfig.RescheduledLineWidth;
        RefundedLineWidth = defaultConfig.RefundedLineWidth;
        SelectedTripColor = defaultConfig.SelectedTripColor;
        SelectedLineWidth = defaultConfig.SelectedLineWidth;
        StationMarkerColor = defaultConfig.StationMarkerColor;
        MarkerSize = defaultConfig.MarkerSize;
        ShowStationLabels = defaultConfig.ShowStationLabels;
        ShowDateLabels = defaultConfig.ShowDateLabels;
        HighlightSelectedTrip = defaultConfig.HighlightSelectedTrip;
        ShowHoverCard = defaultConfig.ShowHoverCard;

        EnableMouseWheelZoom = defaultConfig.EnableMouseWheelZoom;
        EnableLeftDragPan = defaultConfig.EnableLeftDragPan;
        EnableRightClickReset = defaultConfig.EnableRightClickReset;
        ZoomSensitivity = defaultConfig.ZoomSensitivity;
        EnablePanInertia = defaultConfig.EnablePanInertia;
        DoubleClickTripAction = defaultConfig.DoubleClickTripAction;
        DoubleClickStationAction = defaultConfig.DoubleClickStationAction;
        DoubleClickBlankAction = defaultConfig.DoubleClickBlankAction;

        EnableHardwareAcceleration = defaultConfig.EnableHardwareAcceleration;
        PreloadMapResources = defaultConfig.PreloadMapResources;
        AutoCleanCache = defaultConfig.AutoCleanCache;

        // 开发者选项
        EnableDevTools = defaultConfig.EnableDevTools;

        OnPropertyChanged(nameof(CompletedTripColorBrush));
        OnPropertyChanged(nameof(PendingTripColorBrush));
        OnPropertyChanged(nameof(RescheduledTripColorBrush));
        OnPropertyChanged(nameof(RefundedTripColorBrush));
        OnPropertyChanged(nameof(SelectedTripColorBrush));
        OnPropertyChanged(nameof(StationMarkerColorBrush));

        await Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBoxWindow.Show(settingsWindow, SettingsDialogMessages.RestoreNeedSaveHint);
            });
        });
    }

    /// <summary>
    ///     清理WebView2缓存命令
    /// </summary>
    [RelayCommand]
    private async Task ClearWebViewCache()
    {
        // 获取当前设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        // 检查地图窗口是否打开
        var mapWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w is MapWindow);

        if (mapWindow != null)
        {
            var closeResult = MessageBoxWindow.Show(settingsWindow,
                "地图窗口当前正在运行，需要先关闭地图窗口才能彻底清理缓存。\n\n是否立即关闭地图窗口并清理缓存？",
                "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (closeResult != MessageBoxResult.Yes)
                return;

            // 关闭地图窗口
            mapWindow.Close();

            // 等待WebView2进程完全退出（给予足够时间）
            await Task.Delay(3000);
        }
        else
        {
            var result = MessageBoxWindow.Show(settingsWindow,
                "确定要清理WebView2缓存吗？\n清理完成后可以重新打开地图窗口。",
                "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        try
        {
            // 获取WebView2缓存目录
            var cachePath = Path.Combine(Path.GetTempPath(), "GuiPiaoWebView2");
            if (Directory.Exists(cachePath))
            {
                // 计算清理前的缓存大小
                var sizeBefore = CalculateDirectorySize(cachePath);

                // 尝试删除缓存目录（逐个文件删除，处理占用情况）
                var deletedCount = 0;
                var failedCount = 0;
                long freedSpace = 0;

                // 先尝试删除文件
                foreach (var file in Directory.GetFiles(cachePath, "*", SearchOption.AllDirectories))
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var fileSize = fileInfo.Length;
                        fileInfo.Delete();
                        freedSpace += fileSize;
                        deletedCount++;
                    }
                    catch
                    {
                        failedCount++;
                    }

                // 再尝试删除空目录
                foreach (var dir in Directory.GetDirectories(cachePath, "*", SearchOption.AllDirectories)
                             .OrderByDescending(d => d.Length))
                    try
                    {
                        if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                            Directory.Delete(dir);
                    }
                    catch
                    {
                        // 忽略目录删除失败
                    }

                // 更新缓存大小显示
                RefreshCacheSize();

                var message = $"WebView2缓存清理完成\n" +
                              $"释放空间：{FormatBytes(freedSpace)}\n" +
                              $"删除文件：{deletedCount} 个";
                if (failedCount > 0)
                    message += $"\n跳过占用：{failedCount} 个（正在使用中）\n\n提示：关闭地图窗口后等待几秒再清理，可以清理更多文件。";
                else
                    message += "\n\n可以重新打开地图窗口继续使用。";

                MessageBoxWindow.Show(settingsWindow, message, SettingsDialogMessages.SuccessTitle);
            }
            else
            {
                WebViewCacheSize = 0;
                MessageBoxWindow.Show(settingsWindow, "WebView2缓存目录不存在，无需清理");
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"清理缓存失败：{ex.Message}\n\n请确保地图窗口已完全关闭后再试。", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     计算目录大小
    /// </summary>
    private long CalculateDirectorySize(string path)
    {
        long size = 0;
        try
        {
            var dir = new DirectoryInfo(path);
            foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories)) size += file.Length;
        }
        catch
        {
            // 忽略访问错误
        }

        return size;
    }

    /// <summary>
    ///     格式化字节大小
    /// </summary>
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        var order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    ///     刷新WebView2缓存大小
    /// </summary>
    [RelayCommand]
    private void RefreshCacheSize()
    {
        try
        {
            var cachePath = Path.Combine(Path.GetTempPath(), "GuiPiaoWebView2");
            if (Directory.Exists(cachePath))
            {
                var size = CalculateDirectorySize(cachePath);
                WebViewCacheSize = (int)(size / 1024 / 1024); // 转换为MB
            }
            else
            {
                WebViewCacheSize = 0;
            }
        }
        catch
        {
            WebViewCacheSize = 0;
        }
    }

    /// <summary>
    ///     打开颜色选择器命令（地图颜色不影响全局主题，无预览功能）
    /// </summary>
    [RelayCommand]
    private void OpenColorPicker(string colorType)
    {
        try
        {
            var currentColor = colorType switch
            {
                "Completed" => CompletedTripColor,
                "Pending" => PendingTripColor,
                "Refunded" => RefundedTripColor,
                "Selected" => SelectedTripColor,
                "Station" => StationMarkerColor,
                _ => "#0078D4"
            };

            var color = Colors.Blue;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(currentColor);
            }
            catch
            {
            }

            // 获取当前设置窗口作为父窗口
            var settingsWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext is SettingsViewModel);

            // 传入 enableThemePreview = false，不影响全局主题
            // 不传入预览回调和取消回调，完全禁用预览功能
            // showPreviewButton = false，隐藏预览按钮
            var dialog = new ColorPickerDialog(color, false, null, null, false);
            // 不设置 Owner，避免最小化时影响主窗口

            if (dialog.ShowDialog() == true && dialog.SelectedColor.HasValue)
            {
                var selectedColor = dialog.SelectedColor.Value;
                var hexColor = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";

                switch (colorType)
                {
                    case "Completed":
                        CompletedTripColor = hexColor;
                        OnPropertyChanged(nameof(CompletedTripColorBrush));
                        break;
                    case "Pending":
                        PendingTripColor = hexColor;
                        OnPropertyChanged(nameof(PendingTripColorBrush));
                        break;
                    case "Rescheduled":
                        RescheduledTripColor = hexColor;
                        OnPropertyChanged(nameof(RescheduledTripColorBrush));
                        break;
                    case "Refunded":
                        RefundedTripColor = hexColor;
                        OnPropertyChanged(nameof(RefundedTripColorBrush));
                        break;
                    case "Selected":
                        SelectedTripColor = hexColor;
                        OnPropertyChanged(nameof(SelectedTripColorBrush));
                        break;
                    case "Station":
                        StationMarkerColor = hexColor;
                        OnPropertyChanged(nameof(StationMarkerColorBrush));
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            // 获取当前设置窗口作为父窗口
            var settingsWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext is SettingsViewModel);

            MessageBoxWindow.Show(settingsWindow, $"打开颜色选择器失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #region 行程显示与样式自定义

    [ObservableProperty] private string _mapTileSource = "osm";

    [ObservableProperty] private string _defaultMapDisplay = "AllSavedTrips";

    [ObservableProperty] private string _completedTripColor = "#2E7D32";

    [ObservableProperty] private string _pendingTripColor = "#1976D2";

    [ObservableProperty] private string _rescheduledTripColor = "#FF9800";

    [ObservableProperty] private string _refundedTripColor = "#9E9E9E";

    [ObservableProperty] private int _completedLineWidth = 3;

    [ObservableProperty] private int _pendingLineWidth = 4;

    [ObservableProperty] private int _rescheduledLineWidth = 2;

    [ObservableProperty] private int _refundedLineWidth = 2;

    [ObservableProperty] private string _selectedTripColor = "#FF5722";

    [ObservableProperty] private int _selectedLineWidth = 6;

    [ObservableProperty] private string _stationMarkerColor = "#D32F2F";

    [ObservableProperty] private int _markerSize = 12;

    [ObservableProperty] private bool _showStationLabels = true;

    [ObservableProperty] private bool _showDateLabels = true;

    [ObservableProperty] private bool _highlightSelectedTrip = true;

    [ObservableProperty] private bool _showHoverCard = true;

    /// <summary>
    ///     方向过滤：All(全部), Up(上行/偶数车次), Down(下行/奇数车次)
    /// </summary>
    [ObservableProperty] private string _directionFilter = "All";

    #endregion

    #region 地图交互行为设置

    [ObservableProperty] private bool _enableMouseWheelZoom = true;

    [ObservableProperty] private bool _enableLeftDragPan = true;

    [ObservableProperty] private bool _enableRightClickReset = true;

    [ObservableProperty] private int _zoomSensitivity = 120;

    [ObservableProperty] private bool _enablePanInertia = true;

    [ObservableProperty] private string _doubleClickTripAction = "OpenTicketEdit";

    [ObservableProperty] private string _doubleClickStationAction = "ShowStationTickets";

    [ObservableProperty] private string _doubleClickBlankAction = "ZoomInMap";

    #endregion

    #region WebView2性能与存储管理

    [ObservableProperty] private bool _enableHardwareAcceleration = true;

    [ObservableProperty] private bool _preloadMapResources = true;

    [ObservableProperty] private int _webViewCacheSize = 128;

    [ObservableProperty] private bool _autoCleanCache = true;

    #endregion

    #region 颜色画刷属性

    public Brush CompletedTripColorBrush => CreateBrushFromHex(CompletedTripColor);
    public Brush PendingTripColorBrush => CreateBrushFromHex(PendingTripColor);
    public Brush RescheduledTripColorBrush => CreateBrushFromHex(RescheduledTripColor);
    public Brush RefundedTripColorBrush => CreateBrushFromHex(RefundedTripColor);
    public Brush SelectedTripColorBrush => CreateBrushFromHex(SelectedTripColor);
    public Brush StationMarkerColorBrush => CreateBrushFromHex(StationMarkerColor);

    #endregion

    #region 属性变更通知

    /// <summary>
    ///     已完成行程颜色变更时通知画刷更新
    /// </summary>
    partial void OnCompletedTripColorChanged(string value)
    {
        OnPropertyChanged(nameof(CompletedTripColorBrush));
    }

    /// <summary>
    ///     未出行行程颜色变更时通知画刷更新
    /// </summary>
    partial void OnPendingTripColorChanged(string value)
    {
        OnPropertyChanged(nameof(PendingTripColorBrush));
    }

    /// <summary>
    ///     已改签行程颜色变更时通知画刷更新
    /// </summary>
    partial void OnRescheduledTripColorChanged(string value)
    {
        OnPropertyChanged(nameof(RescheduledTripColorBrush));
    }

    /// <summary>
    ///     已退票行程颜色变更时通知画刷更新
    /// </summary>
    partial void OnRefundedTripColorChanged(string value)
    {
        OnPropertyChanged(nameof(RefundedTripColorBrush));
    }

    /// <summary>
    ///     选中行程颜色变更时通知画刷更新
    /// </summary>
    partial void OnSelectedTripColorChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedTripColorBrush));
    }

    /// <summary>
    ///     站点标记颜色变更时通知画刷更新
    /// </summary>
    partial void OnStationMarkerColorChanged(string value)
    {
        OnPropertyChanged(nameof(StationMarkerColorBrush));
    }

    #endregion
}