using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发命令 - 菜单

    [RelayCommand]
    public async Task StorageMenuCommand(string action)
    {
        await Menu.StorageMenuCommand(action);
    }

    [RelayCommand]
    public void TicketMenuCommand(string action)
    {
        _logService?.Info("MainViewModel", $"TicketMenuCommand 被调用，action={action}");
        if (action == "BatchUpdateStatus")
        {
            _logService?.Info("MainViewModel", "开始调用 BatchUpdateStatusAsync");
            _ = TripList.BatchUpdateStatusAsync();
        }
        else if (action == "BatchUpdateTag")
        {
            _logService?.Info("MainViewModel", "开始调用 BatchUpdateTagAsync");
            _ = TripList.BatchUpdateTagAsync();
        }
        else if (action == "BatchDelete")
        {
            _logService?.Info("MainViewModel", "开始调用 BatchDeleteAsync");
            _ = TripList.BatchDeleteAsync();
        }
        else
        {
            Menu.TicketMenuCommand(action);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteTripMenuCommand))]
    public async Task TripMenuCommand(string action)
    {
        await Menu.TripMenuCommandAsync(action);
    }

    private bool CanExecuteTripMenuCommand(string action)
    {
        return action switch
        {
            "RefreshStats" => HasDashboardCharts,
            "ExportChart" => HasDashboardCharts,
            _ => true
        };
    }

    [RelayCommand]
    public async Task ToolsMenuCommand(string action)
    {
        await Menu.ToolsMenuCommandAsync(action);
    }

    [RelayCommand]
    private void ConfigMenuCommand(string action)
    {
        Menu.ConfigMenuCommand(action);
    }

    [RelayCommand]
    public void HelpMenuCommand(string action)
    {
        Menu.HelpMenuCommand(action);
    }

    [RelayCommand]
    public void OpenLogManager()
    {
        Menu.OpenLogManager();
    }

    [RelayCommand]
    private void OpenLogSettings()
    {
        Menu.OpenLogSettings();
    }

    [RelayCommand]
    public void OpenSettings(string? pageName = null)
    {
        Menu.OpenSettings(pageName);
    }

    #endregion
}
