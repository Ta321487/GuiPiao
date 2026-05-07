using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

public partial class LogPanelViewModel : ObservableObject, IDisposable
{
    private readonly LogService _logService;
    private readonly TimeSpan _refreshThrottleInterval = TimeSpan.FromMilliseconds(500);
    private bool _isDisposed;
    private DateTime _lastRefreshTime = DateTime.MinValue;

    [ObservableProperty] private ObservableCollection<LogItem> _logItems = new();

    private CancellationTokenSource? _refreshCts;

    public LogPanelViewModel()
    {
        _logService = ServiceManager.Instance.LogService;

        WeakReferenceMessenger.Default.Register<LogColorsChangedMessage>(this, async (recipient, message) =>
        {
            Debug.WriteLine("[LogPanelViewModel] 收到 LogColorsChangedMessage");
            await LoadLogItemsAsync();
        });

        _logService.LogsChanged += OnLogsChanged;

        _ = LoadLogItemsAsync();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _refreshCts?.Cancel();
        _refreshCts?.Dispose();

        _logService.LogsChanged -= OnLogsChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void OnLogsChanged(object? sender, EventArgs e)
    {
        if (_isDisposed)
            return;

        var now = DateTime.Now;
        if (now - _lastRefreshTime < _refreshThrottleInterval) return;

        _lastRefreshTime = now;

        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var cts = _refreshCts;

        // 使用 Dispatcher 确保在 UI 线程上执行
        _ = Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(200, cts.Token);
                if (!cts.Token.IsCancellationRequested) await LoadLogItemsAsync();
            }
            catch (TaskCanceledException)
            {
            }
        });
    }

    private async Task LoadLogItemsAsync()
    {
        if (_isDisposed)
            return;

        try
        {
            var logs = await _logService.GetLogsAsync(limit: 50);
            var newLogItems = logs.Take(50).ToList();

            if (_isDisposed)
                return;

            LogItems.Clear();
            foreach (var log in newLogItems) LogItems.Add(log);
        }
        catch
        {
            if (!_isDisposed)
            {
                LogItems.Clear();
                LogItems.Add(new LogItem
                    { Time = DateTime.Now.ToString("HH:mm"), Content = "程序启动成功", Level = LogLevel.INFO });
                LogItems.Add(new LogItem
                    { Time = DateTime.Now.ToString("HH:mm"), Content = "数据库加载完成", Level = LogLevel.INFO });
            }
        }
    }

    [RelayCommand]
    public async Task ExportLog()
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV文件 (*.csv)|*.csv",
            FileName = $"日志导出_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() == true)
            try
            {
                await _logService.ExportLogsToCsvAsync(saveDialog.FileName);
                MessageBoxWindow.Show(Application.Current.MainWindow, "日志导出成功", "成功");
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(Application.Current.MainWindow, $"导出失败：{ex.Message}", "错误", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
    }

    [RelayCommand]
    public async Task ClearLog()
    {
        var confirmationService = new ConfirmationService();
        if (!confirmationService.ConfirmBatchDelete("确定要清空日志吗？", true))
            return;

        await _logService.DeleteAllLogsAsync();
        await LoadLogItemsAsync();
    }
}