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
    ///     撤销重做状态恢复回调
    /// </summary>
    private void OnUndoRedoStateRestored(FormState state, bool isUndo)
    {
        _isUndoingOrRedoing = true;
        try
        {
            state.ApplyTo(_formData);
            SyncFromFormData();

            if (isUndo)
                MarkOperationAsUndone(state.PropertyName);
            else
                UnmarkOperationAsUndone(state.PropertyName);

            CheckForChanges();
        }
        finally
        {
            _isUndoingOrRedoing = false;
        }
    }

    /// <summary>
    ///     撤销重做状态保存回调
    /// </summary>
    private void OnUndoRedoStateSaved(FormState state)
    {
        // 状态已保存，可以在这里添加额外逻辑
    }

    /// <summary>
    ///     更新撤销重做命令状态
    /// </summary>
    private void UpdateUndoRedoCommands()
    {
        // 实时读取配置，确保设置变更后立即生效
        var enableUndo = _generalSettingsService.Config.EnableUndo;
        var canUndo = _undoRedoManager.CanUndo && enableUndo;
        var canRedo = _undoRedoManager.CanRedo && enableUndo;

        _logService?.Info("TrainTicketFormViewModelBase",
            $"UpdateUndoRedoCommands: EnableUndo={enableUndo}, Manager.CanUndo={_undoRedoManager.CanUndo}, CanUndo={canUndo}");

        CanUndo = canUndo;
        CanRedo = canRedo;

        // 通知命令的 CanExecute 状态已变更
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    ///     刷新撤销重做设置（在设置变更后调用）
    /// </summary>
    public void RefreshUndoRedoSettings()
    {
        _logService?.Info("TrainTicketFormViewModelBase", "RefreshUndoRedoSettings 被调用");

        // 重新加载配置
        _generalSettingsService.RefreshConfig();

        _logService?.Info("TrainTicketFormViewModelBase",
            $"刷新后 EnableUndo={_generalSettingsService.Config.EnableUndo}, MaxUndoSteps={_generalSettingsService.Config.MaxUndoSteps}");

        // 更新最大撤销步数
        _undoRedoManager.Initialize(_generalSettingsService.Config.MaxUndoSteps);

        // 更新命令状态
        UpdateUndoRedoCommands();
    }


    /// <summary>
    ///     添加操作历史记录
    /// </summary>
    private void AddOperationHistory(string propertyName)
    {
        var description = GetPropertyDescription(propertyName);
        var newValue = GetPropertyValue(propertyName);

        var item = new OperationHistoryItem
        {
            Index = OperationHistory.Count + 1,
            PropertyName = propertyName,
            Description = description,
            NewValue = newValue,
            Timestamp = DateTime.Now,
            IsUndone = false
        };

        OperationHistory.Insert(0, item);

        var maxSteps = _generalSettingsService.Config.MaxUndoSteps;
        while (OperationHistory.Count > maxSteps && maxSteps > 0) OperationHistory.RemoveAt(OperationHistory.Count - 1);
    }

    /// <summary>
    ///     获取属性描述
    /// </summary>
    private string GetPropertyDescription(string propertyName)
    {
        return propertyName switch
        {
            nameof(TrainNoNumber) => "修改车次号",
            nameof(SelectedTrainNoPrefix) => "修改车次前缀",
            nameof(DepartStationInput) => "修改出发车站",
            nameof(ArriveStationInput) => "修改到达车站",
            nameof(DepartDateTime) => "修改出发日期",
            nameof(DepartTimeValue) => "修改出发时间",
            nameof(ArriveTimeValue) => "修改到达时间",
            nameof(ArriveDayOffset) => "修改到达跨天",
            nameof(CoachNoInput) => "修改车厢号",
            nameof(IsJiaChe) => "修改加车",
            nameof(SeatNoNumber) => "修改座位号",
            nameof(SelectedSeatLetter) => "修改座位字母",
            nameof(IsNoSeat) => "修改无座状态",
            nameof(MoneyText) => "修改金额",
            nameof(SeatType) => "修改席别",
            nameof(AdditionalInfo) => "修改附加信息",
            nameof(TicketPurpose) => "修改车票用途",
            nameof(TicketModificationType) => "修改改签类型",
            nameof(Hint) => "修改提示信息",
            nameof(SelectedStatus) => "修改状态",
            nameof(IsStudentTicket) => "修改学生票",
            nameof(IsDiscountTicket) => "修改优惠票",
            nameof(IsOnlineTicket) => "修改网络售票",
            nameof(IsChildTicket) => "修改儿童票",
            nameof(IsAlipay) => "修改支付宝",
            nameof(IsWeChat) => "修改微信",
            nameof(IsABC) => "修改农业银行",
            nameof(IsCCB) => "修改建设银行",
            nameof(IsICBC) => "修改工商银行",
            nameof(IsBCOM) => "修改交通银行",
            nameof(IsCMB) => "修改招商银行",
            nameof(IsPSBC) => "修改邮储银行",
            nameof(IsBOC) => "修改中国银行",
            nameof(TicketNumber) => "修改取票号",
            nameof(CheckInLocation) => "修改检票位置",
            nameof(SelectedTagIds) => "修改标签",
            _ => $"修改 {propertyName}"
        };
    }

    /// <summary>
    ///     获取属性当前值
    /// </summary>
    private string GetPropertyValue(string propertyName)
    {
        return propertyName switch
        {
            nameof(TrainNoNumber) => TrainNoNumber ?? string.Empty,
            nameof(SelectedTrainNoPrefix) => SelectedTrainNoPrefix ?? string.Empty,
            nameof(DepartStationInput) => DepartStationInput ?? string.Empty,
            nameof(ArriveStationInput) => ArriveStationInput ?? string.Empty,
            nameof(DepartDateTime) => DepartDateTime.HasValue
                ? RideDateTime.FormatDate(DepartDateTime.Value)
                : string.Empty,
            nameof(DepartTimeValue) => DepartTimeValue.HasValue
                ? RideDateTime.FormatTime(DepartTimeValue.Value)
                : string.Empty,
            nameof(ArriveTimeValue) => ArriveTimeValue.HasValue
                ? RideDateTime.FormatTime(ArriveTimeValue.Value)
                : string.Empty,
            nameof(ArriveDayOffset) => ArriveTimeFormat.ToLabel(ArriveDayOffset),
            nameof(CoachNoInput) => CoachNoInput ?? string.Empty,
            nameof(IsJiaChe) => IsJiaChe ? "是" : "否",
            nameof(SeatNoNumber) => SeatNoNumber ?? string.Empty,
            nameof(SelectedSeatLetter) => SelectedSeatLetter ?? string.Empty,
            nameof(IsNoSeat) => IsNoSeat ? "是" : "否",
            nameof(MoneyText) => MoneyText ?? string.Empty,
            nameof(SeatType) => SeatType ?? string.Empty,
            nameof(AdditionalInfo) => AdditionalInfo ?? string.Empty,
            nameof(TicketPurpose) => TicketPurpose ?? string.Empty,
            nameof(TicketModificationType) => TicketModificationType ?? string.Empty,
            nameof(Hint) => Hint ?? string.Empty,
            nameof(SelectedStatus) => SelectedStatus ?? string.Empty,
            nameof(IsStudentTicket) => IsStudentTicket ? "是" : "否",
            nameof(IsDiscountTicket) => IsDiscountTicket ? "是" : "否",
            nameof(IsOnlineTicket) => IsOnlineTicket ? "是" : "否",
            nameof(IsChildTicket) => IsChildTicket ? "是" : "否",
            nameof(IsAlipay) => IsAlipay ? "是" : "否",
            nameof(IsWeChat) => IsWeChat ? "是" : "否",
            nameof(IsABC) => IsABC ? "是" : "否",
            nameof(IsCCB) => IsCCB ? "是" : "否",
            nameof(IsICBC) => IsICBC ? "是" : "否",
            nameof(IsBCOM) => IsBCOM ? "是" : "否",
            nameof(IsCMB) => IsCMB ? "是" : "否",
            nameof(IsPSBC) => IsPSBC ? "是" : "否",
            nameof(IsBOC) => IsBOC ? "是" : "否",
            nameof(TicketNumber) => TicketNumber ?? string.Empty,
            nameof(CheckInLocation) => CheckInLocation ?? string.Empty,
            nameof(SelectedTagIds) => GetSelectedTagsDisplayValue(),
            _ => string.Empty
        };
    }

    /// <summary>
    ///     获取已选标签的显示值
    /// </summary>
    private string GetSelectedTagsDisplayValue()
    {
        if (SelectedTagIds == null || SelectedTagIds.Count == 0)
            return "无标签";

        var tagNames = new List<string>();
        foreach (var tagId in SelectedTagIds)
        {
            var tag = AvailableTags?.FirstOrDefault(t => t.Id == tagId);
            if (tag != null) tagNames.Add(tag.Name);
        }

        return tagNames.Count > 0 ? string.Join(", ", tagNames) : "无标签";
    }

    /// <summary>
    ///     标记操作为已撤销
    /// </summary>
    private void MarkOperationAsUndone(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // 查找最新的未撤销记录（列表最前面的是最新的）
        var item = OperationHistory.FirstOrDefault(h => h.PropertyName == propertyName && !h.IsUndone);
        if (item != null)
        {
            item.IsUndone = true;
            _logService?.Info("TrainTicketFormViewModelBase", $"标记操作为已撤销: {propertyName}");
        }
    }

    /// <summary>
    ///     取消标记操作为已撤销
    /// </summary>
    private void UnmarkOperationAsUndone(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // 查找最新的已撤销记录（列表最前面的是最新的）
        var item = OperationHistory.FirstOrDefault(h => h.PropertyName == propertyName && h.IsUndone);
        if (item != null)
        {
            item.IsUndone = false;
            _logService?.Info("TrainTicketFormViewModelBase", $"取消标记操作为已撤销: {propertyName}");
        }
    }

    /// <summary>
    ///     切换操作历史面板展开/折叠
    /// </summary>
    [RelayCommand]
    public void ToggleOperationHistory()
    {
        IsOperationHistoryExpanded = !IsOperationHistoryExpanded;
    }

    /// <summary>
    ///     切换标签选择状态
    /// </summary>
    [RelayCommand]
    public void ToggleTagSelection(int tagId)
    {
        _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 开始: tagId={tagId}");
        _logService?.Info("TrainTicketFormViewModelBase",
            $"[ToggleTagSelection] 操作前 SelectedTagIds.Count={SelectedTagIds.Count}");

        // 记录操作前状态（UndoRedoManager只保存变更前的状态）
        // 注意：SelectedTagIds 不在 _formFieldNames 中，不会触发自动撤销重做
        _undoRedoManager.BeginPropertyChange(nameof(SelectedTagIds));
        AddOperationHistory(nameof(SelectedTagIds));

        if (SelectedTagIds.Contains(tagId))
        {
            _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 移除标签 {tagId}");
            SelectedTagIds.Remove(tagId);
        }
        else
        {
            _logService?.Info("TrainTicketFormViewModelBase", $"[ToggleTagSelection] 添加标签 {tagId}");
            SelectedTagIds.Add(tagId);
        }

        _logService?.Info("TrainTicketFormViewModelBase",
            $"[ToggleTagSelection] 操作后 SelectedTagIds.Count={SelectedTagIds.Count}");

        // 触发属性变更通知，让 PropertyChanged 事件处理器同步到 FormData
        OnPropertyChanged(nameof(SelectedTagIds));

        _logService?.Info("TrainTicketFormViewModelBase", "[ToggleTagSelection] 完成: OnPropertyChanged 已触发");
    }

    /// <summary>
    ///     撤销命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void Undo()
    {
        _undoRedoManager.Undo();
        UpdateUndoRedoCommands();
    }

    /// <summary>
    ///     重做命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void Redo()
    {
        _undoRedoManager.Redo();
        UpdateUndoRedoCommands();
    }
}
