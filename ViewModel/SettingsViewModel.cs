using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Messages;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

/// <summary>
///     设置中心视图模型
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    /// <summary>
    ///     页面标题映射
    /// </summary>
    private readonly Dictionary<SettingsPageType, string> _pageTitles = new()
    {
        { SettingsPageType.General, "常规" },
        { SettingsPageType.Log, "日志" },
        { SettingsPageType.Database, "数据库" },
        { SettingsPageType.Map, "地图" },
        { SettingsPageType.UI, "界面" },
        { SettingsPageType.Dashboard, "仪表盘" },
        { SettingsPageType.OCR, "OCR" },
        { SettingsPageType.Export, "导出" },
        { SettingsPageType.Shortcut, "快捷键" },
        { SettingsPageType.Sync, "同步" }
    };

    private readonly Dictionary<SettingsPageType, UserControl> _viewCache = new();

    [ObservableProperty] private SettingsPageType _currentPage = SettingsPageType.General;

    [ObservableProperty] private string _currentPageTitle = "常规";

    [ObservableProperty] private UserControl? _currentView;

    private DashboardSettingsViewModel? _dashboardSettingsViewModel;
    private DatabaseSettingsViewModel? _databaseSettingsViewModel;
    private ExportSettingsViewModel? _exportSettingsViewModel;
    private GeneralSettingsViewModel? _generalSettingsViewModel;

    // 使用延迟初始化，避免构造函数阻塞 UI
    private LogSettingsViewModel? _logSettingsViewModel;
    private MapSettingsViewModel? _mapSettingsViewModel;
    private OcrSettingsViewModel? _ocrSettingsViewModel;
    private ShortcutSettingsViewModel? _shortcutSettingsViewModel;
    private SyncSettingsViewModel? _syncSettingsViewModel;
    private UISettingsViewModel? _uiSettingsViewModel;

    // 延迟初始化属性
    private LogSettingsViewModel LogSettingsViewModel => _logSettingsViewModel ??= new LogSettingsViewModel();

    private GeneralSettingsViewModel GeneralSettingsViewModel =>
        _generalSettingsViewModel ??= new GeneralSettingsViewModel();

    private DatabaseSettingsViewModel DatabaseSettingsViewModel =>
        _databaseSettingsViewModel ??= new DatabaseSettingsViewModel();

    private UISettingsViewModel UISettingsViewModel => _uiSettingsViewModel ??= new UISettingsViewModel();
    private MapSettingsViewModel MapSettingsViewModel => _mapSettingsViewModel ??= new MapSettingsViewModel();

    private DashboardSettingsViewModel DashboardSettingsViewModel =>
        _dashboardSettingsViewModel ??= new DashboardSettingsViewModel();

    private OcrSettingsViewModel OcrSettingsViewModel => _ocrSettingsViewModel ??= new OcrSettingsViewModel();

    private ExportSettingsViewModel ExportSettingsViewModel =>
        _exportSettingsViewModel ??= new ExportSettingsViewModel();

    private ShortcutSettingsViewModel ShortcutSettingsViewModel =>
        _shortcutSettingsViewModel ??= new ShortcutSettingsViewModel();

    private SyncSettingsViewModel SyncSettingsViewModel =>
        _syncSettingsViewModel ??= new SyncSettingsViewModel();

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges =>
        (_generalSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_logSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_databaseSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_uiSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_mapSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_dashboardSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_ocrSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_exportSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_shortcutSettingsViewModel?.HasUnsavedChanges ?? false) ||
        (_syncSettingsViewModel?.HasUnsavedChanges ?? false);

    /// <summary>
    ///     初始化默认页面，应在窗口加载完成后调用
    /// </summary>
    public void InitializeDefaultPage()
    {
        if (CurrentView == null) NavigateToPage(SettingsPageType.General);
    }

    /// <summary>
    ///     导航到指定页面
    /// </summary>
    public void NavigateToPage(SettingsPageType page)
    {
        Debug.WriteLine($"[SettingsViewModel] NavigateToPage: {page}");
        CurrentPage = page;
        CurrentPageTitle = _pageTitles[page];

        if (!_viewCache.TryGetValue(page, out var view))
        {
            Debug.WriteLine($"[SettingsViewModel] 创建新视图: {page}");
            view = CreateViewForPage(page);
            if (view != null) _viewCache[page] = view;
        }
        else
        {
            Debug.WriteLine($"[SettingsViewModel] 使用缓存视图: {page}");
        }

        CurrentView = view;
        Debug.WriteLine($"[SettingsViewModel] CurrentView 已设置: {view?.GetType().Name}");
    }

    /// <summary>
    ///     导航命令
    /// </summary>
    [RelayCommand]
    private void Navigate(string pageName)
    {
        if (Enum.TryParse<SettingsPageType>(pageName, out var page)) NavigateToPage(page);
    }

    /// <summary>
    ///     保存所有设置命令
    /// </summary>
    [RelayCommand]
    private async Task SaveAll()
    {
        // 获取当前设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            // 各页保存本身很快，不再弹出进度窗（避免先闪空白框再弹成功）
            if (GeneralSettingsViewModel is ISettingsViewModel generalSettings)
                await generalSettings.SaveSettingsAsync(false);

            if (LogSettingsViewModel is ISettingsViewModel logSettings)
                await logSettings.SaveSettingsAsync(false);

            if (DatabaseSettingsViewModel is ISettingsViewModel databaseSettings)
                await databaseSettings.SaveSettingsAsync(false);

            if (UISettingsViewModel is ISettingsViewModel uiSettings)
                await uiSettings.SaveSettingsAsync(false);

            if (MapSettingsViewModel is ISettingsViewModel mapSettings)
                await mapSettings.SaveSettingsAsync(false);

            if (DashboardSettingsViewModel is ISettingsViewModel dashboardSettings)
                await dashboardSettings.SaveSettingsAsync(false);

            if (OcrSettingsViewModel is ISettingsViewModel ocrSettings)
                await ocrSettings.SaveSettingsAsync(false);

            if (ExportSettingsViewModel is ISettingsViewModel exportSettings)
                await exportSettings.SaveSettingsAsync(false);

            if (ShortcutSettingsViewModel is ISettingsViewModel shortcutSettings)
                await shortcutSettings.SaveSettingsAsync(false);

            if (SyncSettingsViewModel is ISettingsViewModel syncSettings)
                await syncSettings.SaveSettingsAsync(false);

            MessageBoxWindow.Show(settingsWindow, "所有设置已保存", SettingsDialogMessages.SuccessTitle);
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"{SettingsDialogMessages.SaveFailedPrefix}{ex.Message}",
                SettingsDialogMessages.ErrorTitle, MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     放弃所有更改
    /// </summary>
    public void DiscardAllChanges()
    {
        _generalSettingsViewModel?.ReloadSettings();
        _logSettingsViewModel?.ReloadSettings();
        _databaseSettingsViewModel?.ReloadSettings();
        _uiSettingsViewModel?.ReloadSettings();
        _mapSettingsViewModel?.ReloadSettings();
        _dashboardSettingsViewModel?.ReloadSettings();
        _ocrSettingsViewModel?.ReloadSettings();
        _exportSettingsViewModel?.ReloadSettings();
        _shortcutSettingsViewModel?.ReloadSettings();
        _syncSettingsViewModel?.ReloadSettings();
    }

    /// <summary>
    ///     窗口关闭时调用
    /// </summary>
    public void OnWindowClosing()
    {
        // 通知OCR设置页面暂停下载
        _ocrSettingsViewModel?.OnWindowClosing();
        _syncSettingsViewModel?.OnWindowClosing();
    }

    /// <summary>
    ///     窗口关闭后释放子页 VM、视图缓存与单例事件订阅。
    /// </summary>
    public void DetachAllResources()
    {
        _ocrSettingsViewModel?.Detach();
        _syncSettingsViewModel?.Detach();

        _viewCache.Clear();
        CurrentView = null;

        _generalSettingsViewModel = null;
        _logSettingsViewModel = null;
        _databaseSettingsViewModel = null;
        _uiSettingsViewModel = null;
        _mapSettingsViewModel = null;
        _dashboardSettingsViewModel = null;
        _ocrSettingsViewModel = null;
        _exportSettingsViewModel = null;
        _shortcutSettingsViewModel = null;
        _syncSettingsViewModel = null;
    }

    /// <summary>
    ///     导出设置命令
    /// </summary>
    [RelayCommand]
    private async Task ExportSettings()
    {
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "导出设置",
                Filter = SettingsImportExportService.Instance.GetImportFileFilter(),
                FileName = SettingsImportExportService.Instance.GetDefaultExportFileName(),
                DefaultExt = "json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var progressWindow = MessageBoxWindow.ShowProgress("正在导出设置...", "导出设置");

                try
                {
                    await SettingsImportExportService.Instance.ExportSettingsAsync(saveFileDialog.FileName);
                    progressWindow?.Close();

                    var result = MessageBoxWindow.Show(
                        settingsWindow,
                        $"设置已成功导出到:\n{saveFileDialog.FileName}\n\n是否打开文件所在目录?",
                        "导出成功",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        var directory = Path.GetDirectoryName(saveFileDialog.FileName);
                        if (!string.IsNullOrEmpty(directory))
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = directory,
                                UseShellExecute = true
                            });
                    }
                }
                catch (Exception ex)
                {
                    progressWindow?.Close();
                    MessageBoxWindow.Show(
                        settingsWindow,
                        $"导出设置失败:\n{ex.Message}",
                        "导出失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(
                settingsWindow,
                $"导出设置时发生错误:\n{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     导入设置命令
    /// </summary>
    [RelayCommand]
    private async Task ImportSettings()
    {
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            // 检查是否有未保存的更改
            if (HasUnsavedChanges)
            {
                var saveResult = MessageBoxWindow.Show(
                    settingsWindow,
                    "当前有未保存的设置更改。\n\n导入新设置前是否先保存当前更改?",
                    "未保存的更改",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (saveResult)
                {
                    case MessageBoxResult.Yes:
                        await SaveAll();
                        break;
                    case MessageBoxResult.No:
                        // 放弃当前更改，继续导入
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }

            var openFileDialog = new OpenFileDialog
            {
                Title = "导入设置",
                Filter = SettingsImportExportService.Instance.GetImportFileFilter(),
                DefaultExt = "json",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var progressWindow = MessageBoxWindow.ShowProgress("正在验证导入文件...", "导入设置");

                try
                {
                    // 验证导入文件
                    var validationResult =
                        await SettingsImportExportService.Instance.ImportSettingsAsync(openFileDialog.FileName);
                    progressWindow?.Close();

                    if (!validationResult.IsValid)
                    {
                        // 验证失败，显示错误信息
                        var errorMessage = "导入文件验证失败:\n\n" +
                                           string.Join("\n", validationResult.Errors.Select((e, i) => $"{i + 1}. {e}"));

                        MessageBoxWindow.Show(
                            settingsWindow,
                            errorMessage,
                            "导入失败",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    // 验证通过，询问是否应用
                    var confirmResult = MessageBoxWindow.Show(
                        settingsWindow,
                        "导入文件验证通过。\n\n这将覆盖所有当前设置，是否继续?",
                        "确认导入",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        progressWindow = MessageBoxWindow.ShowProgress("正在应用设置...", "导入设置");

                        try
                        {
                            // 应用导入的设置
                            SettingsImportExportService.Instance.ApplyImportedSettings(validationResult.ValidatedData!);

                            // 重新加载所有设置页面
                            DiscardAllChanges();

                            // 同步主窗口界面与卡片布局
                            ApplyImportedUiSettingsToMainWindow();

                            // 刷新当前页面
                            var currentPage = CurrentPage;
                            NavigateToPage(currentPage);

                            progressWindow?.Close();

                            MessageBoxWindow.Show(
                                settingsWindow,
                                "设置已成功导入并应用。\n\n所有设置页面已重新加载。",
                                "导入成功");
                        }
                        catch (Exception ex)
                        {
                            progressWindow?.Close();
                            MessageBoxWindow.Show(
                                settingsWindow,
                                $"应用设置失败:\n{ex.Message}",
                                "导入失败",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    progressWindow?.Close();
                    MessageBoxWindow.Show(
                        settingsWindow,
                        $"导入设置失败:\n{ex.Message}",
                        "导入失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(
                settingsWindow,
                $"导入设置时发生错误:\n{ex.Message}",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     设置导入后，将 uisettings.json 中的界面/卡片配置广播到主窗口。
    /// </summary>
    private static void ApplyImportedUiSettingsToMainWindow()
    {
        ConfigManager.Instance.RefreshUISettingsConfig();
        var config = ConfigManager.Instance.UISettingsService.Config;
        var tripListView = Application.Current.MainWindow?.DataContext is MainViewModel mainViewModel
            ? mainViewModel.TripList.CurrentViewType
            : config.DefaultTripListView;

        UISettingsViewModel.BroadcastAllFromConfig(config, tripListView);
        ThemeManager.ApplyDpiScaling(config.DpiScaling);
    }

    /// <summary>
    ///     根据页面类型创建对应的视图
    /// </summary>
    private UserControl? CreateViewForPage(SettingsPageType page)
    {
        return page switch
        {
            SettingsPageType.General => new GeneralSettingsView { DataContext = GeneralSettingsViewModel },
            SettingsPageType.Log => new LogSettingsView { DataContext = LogSettingsViewModel },
            SettingsPageType.Database => new DatabaseSettingsView { DataContext = DatabaseSettingsViewModel },
            SettingsPageType.UI => new UISettingsView { DataContext = UISettingsViewModel },
            SettingsPageType.Map => new MapSettingsView { DataContext = MapSettingsViewModel },
            SettingsPageType.Dashboard => new DashboardSettingsView { DataContext = DashboardSettingsViewModel },
            SettingsPageType.OCR => new OcrSettingsView { DataContext = OcrSettingsViewModel },
            SettingsPageType.Export => new ExportSettingsView { DataContext = ExportSettingsViewModel },
            SettingsPageType.Shortcut => new ShortcutSettingsView { DataContext = ShortcutSettingsViewModel },
            SettingsPageType.Sync => new SyncSettingsView { DataContext = SyncSettingsViewModel },
            _ => CreatePlaceholderView(page)
        };
    }

    /// <summary>
    ///     创建占位视图（用于未实现的页面）
    /// </summary>
    private UserControl CreatePlaceholderView(SettingsPageType page)
    {
        return new UserControl
        {
            Content = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"{_pageTitles[page]}设置",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 20),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "该功能正在开发中...",
                        FontSize = 14,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            }
        };
    }
}