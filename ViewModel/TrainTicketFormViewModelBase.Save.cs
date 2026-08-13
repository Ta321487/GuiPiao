using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.DataAccess;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;

namespace GuiPiao.ViewModel.TrainTicketForm;

public abstract partial class TrainTicketFormViewModelBase
{
    /// <summary>
    ///     保存命令
    /// </summary>
    [RelayCommand]
    protected async Task SaveAsync()
    {
        _isSaving = true;
        try
        {
            await ExecuteSaveAsync();
            // 只有在保存成功后才重置未保存更改标志
            BackupOriginalValues();
        }
        finally
        {
            _isSaving = false;
        }
    }

    /// <summary>
    ///     执行保存操作（子类实现具体逻辑）
    /// </summary>
    protected abstract Task ExecuteSaveAsync();

    /// <summary>
    ///     保存标签关联（公共方法）
    /// </summary>
    protected async Task SaveTagsAsync(int ticketId)
    {
        // 无论是否有选中标签，都要更新数据库（空列表表示删除所有标签）
        var tagIdsToSave = SelectedTagIds ?? new ObservableCollection<int>();
        await _ticketTagRepository.SetTagsToRideAsync(ticketId, tagIdsToSave);
    }

    /// <summary>
    ///     新增入库并写标签，返回新 Id；同时写入 <see cref="EditTicketId"/>。
    /// </summary>
    protected async Task<int> InsertNewTrainRideAsync()
    {
        var trainRide = CreateTrainRideInfo();
        var newId = await _trainRideRepository.AddTrainRideAsync(trainRide);
        await SaveTagsAsync(newId);
        EditTicketId = newId;
        return newId;
    }

    /// <summary>
    ///     按 <see cref="EditTicketId"/> 更新入库并写标签。
    /// </summary>
    protected async Task UpdateExistingTrainRideAsync()
    {
        if (!EditTicketId.HasValue)
            throw new InvalidOperationException("车票ID无效");

        var trainRide = CreateTrainRideInfo();
        var rowsAffected = await _trainRideRepository.UpdateTrainRideAsync(trainRide);
        if (rowsAffected == 0)
            throw new InvalidOperationException("未找到对应的车票记录，可能已被删除");

        await SaveTagsAsync(EditTicketId.Value);
    }

    /// <summary>
    ///     显示保存成功消息；可选关闭窗口。
    /// </summary>
    protected void ShowSaveSuccess(string message, bool closeWindow = true)
    {
        WeakReferenceMessenger.Default.Send(new TicketSavedMessage
        {
            TicketId = EditTicketId,
            IsEditMode = IsEditMode,
            TrainNo = TrainNo
        });

        MessageBoxWindow.Show(message);
        if (closeWindow)
            CloseWindow();
    }

    /// <summary>
    ///     显示保存成功消息并关闭窗口
    /// </summary>
    protected void ShowSaveSuccessAndClose(string message)
    {
        ShowSaveSuccess(message, closeWindow: true);
    }

    /// <summary>
    ///     显示保存失败消息
    /// </summary>
    protected void ShowSaveError(string operation, string errorMessage)
    {
        _logService?.Error(GetType().Name, $"{operation}火车票失败: {errorMessage}");
        MessageBoxWindow.Show($"{operation}失败：{errorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    ///     记录保存日志
    /// </summary>
    protected void LogSaveOperation(string operation)
    {
        _logService?.Info(GetType().Name, $"{operation}火车票: {TrainNo} {DepartStation}->{ArriveStation}");
    }

    /// <summary>
    ///     取消命令
    /// </summary>
    [RelayCommand]
    protected void Cancel()
    {
        CloseWindow();
    }

    /// <summary>
    ///     关闭窗口
    /// </summary>
    protected void CloseWindow()
    {
        var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
        window?.Close();
    }
}
