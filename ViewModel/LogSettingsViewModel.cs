using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

public partial class LogSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly LogService _logService;

    [ObservableProperty] private bool _autoCleanup = true;

    [ObservableProperty] private string _currentLogFileSize;

    [ObservableProperty] private string _logFilePath;

    [ObservableProperty] private int _maxLogCount = 1000;

    private LogConfig _originalConfig;

    [ObservableProperty] private int _retentionDays = 7;

    [ObservableProperty] private LogLevel _selectedLogLevel;

    public LogSettingsViewModel()
    {
        _logService = ServiceManager.Instance.LogService;
        LoadConfig();
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            return;
    }

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (_originalConfig == null)
                return false;

            return SelectedLogLevel != _originalConfig.MinLogLevel ||
                   AutoCleanup != _originalConfig.AutoCleanup ||
                   RetentionDays != _originalConfig.RetentionDays ||
                   MaxLogCount != _originalConfig.MaxLogCount ||
                   LogFilePath != _originalConfig.LogFilePath;
        }
    }

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    public void ReloadSettings()
    {
        LoadConfig();
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    private void LoadConfig()
    {
        var config = _logService.Config;
        _originalConfig = new LogConfig
        {
            MinLogLevel = config.MinLogLevel,
            AutoCleanup = config.AutoCleanup,
            RetentionDays = config.RetentionDays,
            MaxLogCount = config.MaxLogCount,
            LogFilePath = config.LogFilePath
        };
        SelectedLogLevel = config.MinLogLevel;
        AutoCleanup = config.AutoCleanup;
        RetentionDays = config.RetentionDays;
        MaxLogCount = config.MaxLogCount;
        LogFilePath = config.LogFilePath;
        CurrentLogFileSize = _logService.FormatFileSize(config.CurrentLogFileSize);
    }

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
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        // 校验最大日志数量
        if (MaxLogCount < 100 || MaxLogCount > 10000)
        {
            MessageBoxWindow.Show(owner, "最大日志数量必须在100-10000之间", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var config = new LogConfig
            {
                MinLogLevel = SelectedLogLevel,
                AutoCleanup = AutoCleanup,
                RetentionDays = RetentionDays,
                MaxLogCount = MaxLogCount,
                LogFilePath = LogFilePath
            };

            _logService.SaveConfig(config);
            _originalConfig = new LogConfig
            {
                MinLogLevel = config.MinLogLevel,
                AutoCleanup = config.AutoCleanup,
                RetentionDays = config.RetentionDays,
                MaxLogCount = config.MaxLogCount,
                LogFilePath = config.LogFilePath
            };

            if (AutoCleanup) await _logService.AutoCleanupAsync();

            _logService.RefreshConfig();
            CurrentLogFileSize = _logService.FormatFileSize(_logService.Config.CurrentLogFileSize);

            // 发送设置变更消息
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage("Log"));

            if (showMessage) MessageBoxWindow.Show(owner, "日志设置已保存", SettingsDialogMessages.SuccessTitle);
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"{SettingsDialogMessages.SaveFailedPrefix}{ex.Message}",
                SettingsDialogMessages.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RestoreDefaults()
    {
        var owner = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        var result = MessageBoxWindow.Show(owner, SettingsDialogMessages.RestoreConfirmBody,
            SettingsDialogMessages.ConfirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        SelectedLogLevel = LogLevel.INFO;
        AutoCleanup = true;
        RetentionDays = 7;
        MaxLogCount = 1000;

        MessageBoxWindow.Show(owner, SettingsDialogMessages.RestoreNeedSaveHint);
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        try
        {
            var logDir = _logService.GetLogDirectory();
            if (Directory.Exists(logDir))
                Process.Start(new ProcessStartInfo
                {
                    FileName = logDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            else
                MessageBoxWindow.Show(owner, "日志文件夹不存在");
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"打开文件夹失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}