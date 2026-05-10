using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发属性 - 日志面板相关

    public ObservableCollection<LogItem> LogItems => LogPanel.LogItems;

    #endregion

    #region 转发命令 - 日志面板

    [RelayCommand]
    private async Task ExportLog()
    {
        await LogPanel.ExportLog();
    }

    [RelayCommand]
    private async Task ClearLog()
    {
        await LogPanel.ClearLog();
    }

    #endregion

    private void SubscribeToLogPanelChanges()
    {
        _logPanelPropertyChangedHandler = (s, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(LogPanel.LogItems))
            {
                OnPropertyChanged(nameof(LogItems));
                SetTemporaryStatus($"日志面板已更新，共 {LogPanel.LogItems.Count} 条日志");
            }
        };
        LogPanel.PropertyChanged += _logPanelPropertyChangedHandler;
    }
}
