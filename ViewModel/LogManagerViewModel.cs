using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using Microsoft.Win32;

namespace GuiPiao.ViewModel;

public partial class LogManagerViewModel : ObservableObject
{
    private readonly ConfirmationService _confirmationService;
    private readonly LogService _logService;

    [ObservableProperty] private DateTime? _endDate = DateTime.Today;

    [ObservableProperty] private string _keyword;

    [ObservableProperty] private ObservableCollection<LogItem> _logItems;

    [ObservableProperty] private ObservableCollection<LogItem> _selectedLogItems;

    [ObservableProperty] private LogLevel _selectedLogLevel = LogLevel.ALL;

    [ObservableProperty] private DateTime? _startDate = DateTime.Today;

    [ObservableProperty] private string _statusMessage = "查看中";

    [ObservableProperty] private int _totalLogCount;

    public LogManagerViewModel()
    {
        _logService = ServiceManager.Instance.LogService;
        _confirmationService = new ConfirmationService();
        LogItems = new ObservableCollection<LogItem>();
        SelectedLogItems = new ObservableCollection<LogItem>();
        _ = LoadLogsAsync();
        //设计器模式下不加载任何代码
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            return;

        // 订阅日志变更事件
        _logService.LogsChanged += async (sender, e) =>
        {
            // 在UI线程上刷新日志
            await Application.Current.Dispatcher.InvokeAsync(async () => { await LoadLogsAsync(); });
        };
    }

    [RelayCommand]
    private async Task Search()
    {
        await LoadLogsAsync();
    }

    [RelayCommand]
    private void ClearConditions()
    {
        SelectedLogLevel = LogLevel.ALL;
        StartDate = null;
        EndDate = null;
        Keyword = string.Empty;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        try
        {
            var logs = await _logService.GetLogsAsync(
                SelectedLogLevel == LogLevel.ALL ? null : SelectedLogLevel,
                StartDate,
                EndDate,
                Keyword);

            LogItems.Clear();
            foreach (var log in logs) LogItems.Add(log);

            TotalLogCount = LogItems.Count;
            StatusMessage = "查看中";
        }
        catch (Exception ex)
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            MessageBoxWindow.Show(owner, $"加载日志失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ExportSelectedLogs()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (SelectedLogItems.Count == 0)
        {
            MessageBoxWindow.Show(owner, "请先选择要导出的日志");
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV文件 (*.csv)|*.csv",
            FileName = $"日志导出_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() == true)
            try
            {
                var ids = SelectedLogItems.Select(l => l.Id);
                await _logService.ExportLogsToCsvAsync(saveDialog.FileName, ids);
                MessageBoxWindow.Show(owner, $"成功导出 {SelectedLogItems.Count} 条日志", "成功");
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(owner, $"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
    }

    [RelayCommand]
    private async Task ExportAllLogs()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        var saveDialog = new SaveFileDialog
        {
            Filter = "CSV文件 (*.csv)|*.csv",
            FileName = $"日志导出_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (saveDialog.ShowDialog() == true)
            try
            {
                await _logService.ExportLogsToCsvAsync(saveDialog.FileName);
                MessageBoxWindow.Show(owner, $"成功导出 {TotalLogCount} 条日志", "成功");
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(owner, $"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
    }

    [RelayCommand]
    private async Task DeleteOldLogs()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (!_confirmationService.ConfirmBatchDelete("确定要清空7天前的日志吗？"))
            return;

        try
        {
            var deleted = await _logService.DeleteLogsOlderThanAsync(7);
            MessageBoxWindow.Show(owner, $"成功删除 {deleted} 条日志", "成功");
            await LoadLogsAsync();
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteAllLogs()
    {
        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        if (!_confirmationService.ConfirmBatchDelete("确定要清空所有日志吗？此操作不可恢复！", true))
            return;

        try
        {
            var deleted = await _logService.DeleteAllLogsAsync();
            MessageBoxWindow.Show(owner, $"成功删除 {deleted} 条日志", "成功");
            await LoadLogsAsync();
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(owner, $"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void SelectLog(LogItem log)
    {
        if (log != null && !SelectedLogItems.Contains(log)) SelectedLogItems.Add(log);
    }

    public void DeselectLog(LogItem log)
    {
        if (log != null) SelectedLogItems.Remove(log);
    }
}