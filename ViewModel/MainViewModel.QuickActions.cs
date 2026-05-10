using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

public partial class MainViewModel
{
    #region 转发命令 - 快捷功能区

    [RelayCommand]
    public void NewTicketRecord()
    {
        QuickActions.NewTicketRecordCommand();
    }

    [RelayCommand]
    public void OcrRecognizeTicket()
    {
        QuickActions.OcrRecognizeTicketCommand();
    }

    [RelayCommand]
    public void OpenTicketMap()
    {
        QuickActions.OpenTicketMapCommand();
    }

    [RelayCommand]
    public void TicketPreview()
    {
        QuickActions.TicketPreviewCommand();
    }

    [RelayCommand]
    public void BackupRestoreDatabase()
    {
        QuickActions.BackupRestoreDatabaseCommand();
    }

    [RelayCommand]
    public void SystemConfig()
    {
        QuickActions.SystemConfigCommand();
    }

    #endregion

    #region 转发命令 - 票务菜单（编辑/删除选中项）

    [RelayCommand]
    private async Task EditSelectedTicket()
    {
        if (TripList.SelectedTripItem != null)
            await TripList.EditTripCommand(TripList.SelectedTripItem);
        else
            MessageBoxWindow.Show(Application.Current.MainWindow, "请先选择一条票务记录");
    }

    [RelayCommand]
    private async Task DeleteSelectedTicket()
    {
        if (TripList.SelectedTripItem != null)
            await TripList.DeleteTripCommand(TripList.SelectedTripItem);
        else
            MessageBoxWindow.Show(Application.Current.MainWindow, "请先选择一条票务记录");
    }

    #endregion

    private void SubscribeToQuickActionsChanges()
    {
        _quickActionsPropertyChangedHandler = (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(QuickActions.NewTicketRecordCommand):
                    SetTemporaryStatus("正在打开新增票务记录...");
                    break;
            }
        };
        QuickActions.PropertyChanged += _quickActionsPropertyChangedHandler;
    }
}
