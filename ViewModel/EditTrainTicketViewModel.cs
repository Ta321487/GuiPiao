using System;
using System.Threading.Tasks;
using System.Windows;
using GuiPiao.View;
using GuiPiao.ViewModel.TrainTicketForm;

namespace GuiPiao.ViewModel;

/// <summary>
///     编辑火车票 ViewModel
/// </summary>
public class EditTrainTicketViewModel : TrainTicketFormViewModelBase
{
    public EditTrainTicketViewModel()
    {
        WindowTitle = "编辑火车票";
        SaveButtonText = "更新";
        IsEditMode = true;
    }

    /// <summary>
    ///     根据ID加载车票信息
    /// </summary>
    public async Task LoadTicketByIdAsync(int ticketId)
    {
        try
        {
            var trainRide = await _trainRideRepository.GetTrainRideByIdAsync(ticketId);
            if (trainRide != null)
            {
                LoadFromTrainRide(trainRide);
            }
            else
            {
                MessageBoxWindow.Show("未找到指定的车票记录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                CloseWindow();
            }
        }
        catch (Exception ex)
        {
            _logService.Error("EditTrainTicketViewModel", $"加载车票失败: {ex.Message}");
            MessageBoxWindow.Show($"加载车票失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            CloseWindow();
        }
    }

    /// <summary>
    ///     执行保存操作 - 更新现有车票
    /// </summary>
    protected override async Task ExecuteSaveAsync()
    {
        if (!ValidateForm())
            return;

        if (!EditTicketId.HasValue)
        {
            MessageBoxWindow.Show("车票ID无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _logService.Error("EditTrainTicketViewModel", "保存失败：EditTicketId 为 null");
            return;
        }

        _logService.Info("EditTrainTicketViewModel",
            $"保存前: EditTicketId={EditTicketId.Value}, DepartTimeValue={DepartTimeValue}, DepartTime='{DepartTime}'");

        var trainRide = CreateTrainRideInfo();
        _logService.Info("EditTrainTicketViewModel",
            $"保存数据: ID={trainRide.Id}, TrainNo={trainRide.TrainNo}, DepartStation={trainRide.DepartStation}, ArriveStation={trainRide.ArriveStation}");

        try
        {
            // 更新车票信息
            var rowsAffected = await _trainRideRepository.UpdateTrainRideAsync(trainRide);
            _logService.Info("EditTrainTicketViewModel", $"更新数据库完成，影响行数: {rowsAffected}");

            if (rowsAffected == 0)
            {
                _logService.Error("EditTrainTicketViewModel", "更新失败：没有行被更新，可能是ID不存在");
                throw new InvalidOperationException("未找到对应的车票记录，可能已被删除");
            }

            // 更新标签关联
            await SaveTagsAsync(EditTicketId.Value);

            // 重要：在显示成功消息和关闭窗口之前，先重置未保存更改标志
            // 这样关闭窗口时不会再次弹出确认对话框
            HasUnsavedChanges = false;

            LogSaveOperation("更新");
            ShowSaveSuccessAndClose("火车票更新成功");
        }
        catch (Exception ex)
        {
            _logService.Error("EditTrainTicketViewModel", $"保存异常: {ex.Message}\n{ex.StackTrace}");
            // 重新抛出异常，让上层处理错误提示
            throw;
        }
    }
}