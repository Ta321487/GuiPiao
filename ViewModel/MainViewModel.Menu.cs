using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发命令 - 菜单

    [RelayCommand]
    public async Task StorageMenu(string action)
    {
        await Menu.StorageMenuCommand(action);
    }

    [RelayCommand]
    public void TicketMenu(string action)
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
        else if (action == "BatchUpdateSeat")
        {
            _logService?.Info("MainViewModel", "开始调用 BatchUpdateSeatAsync");
            _ = TripList.BatchUpdateSeatAsync();
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

    [RelayCommand(CanExecute = nameof(CanExecuteTripMenu))]
    public async Task TripMenu(string action)
    {
        await Menu.TripMenuCommandAsync(action);
    }

    private bool CanExecuteTripMenu(string action)
    {
        return action switch
        {
            "RefreshStats" => HasDashboardCharts,
            "ExportChart" => HasDashboardCharts,
            _ => true
        };
    }

    [RelayCommand]
    public async Task ToolsMenu(string action)
    {
        await Menu.ToolsMenuCommandAsync(action);
    }

    [RelayCommand]
    private void ConfigMenu(string action)
    {
        Menu.ConfigMenuCommand(action);
    }

    [RelayCommand]
    public void HelpMenu(string action)
    {
        Menu.HelpMenuCommand(action);
    }

    [RelayCommand]
    public void OpenManageStations()
    {
        Menu.OpenManageStations();
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