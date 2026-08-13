using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Utils;
using GuiPiao.View;
using GuiPiao.ViewModel.TrainTicketForm;

namespace GuiPiao.ViewModel;

/// <summary>
///     添加火车票 ViewModel
/// </summary>
public partial class AddTrainTicketViewModel : TrainTicketFormViewModelBase
{
    /// <summary>多图导入时的内存槽。</summary>
    private sealed class ImportBatchItem
    {
        public required TicketImportDraft Draft { get; init; }
        public TrainTicketFormData? StashedForm { get; set; }
        public TrainTicketFormData? StashedOriginal { get; set; }
        public string ImportBannerText { get; set; } = string.Empty;
        public string ImportRawText { get; set; } = string.Empty;
        public bool IsImportRawTextExpanded { get; set; }
        public bool IsSaved { get; set; }
        public int? SavedTicketId { get; set; }
        public bool HasPendingEdits { get; set; }
    }

    private readonly List<ImportBatchItem> _batchItems = new();

    public AddTrainTicketViewModel()
    {
        WindowTitle = "添加火车票";
        SaveButtonText = "保存";
        IsEditMode = false;
        IsStatusVisible = true; // 新增窗口显示状态下拉框
    }

    /// <summary>
    ///     是否已成功保存（用于改签逻辑判断）
    /// </summary>
    public bool IsSaved { get; private set; }

    /// <summary>
    ///     是否跳过加载默认值（用于改签模式）
    /// </summary>
    public bool SkipLoadDefaults { get; set; } = false;

    [ObservableProperty] private bool _showImportBanner;

    [ObservableProperty] private string _importBannerText = string.Empty;

    [ObservableProperty] private string _importRawText = string.Empty;

    [ObservableProperty] private bool _isImportRawTextExpanded;

    [ObservableProperty] private int _currentBatchIndex;

    [ObservableProperty] private bool _isImportBatchMode;

    public string BatchPositionText =>
        !IsImportBatchMode || _batchItems.Count == 0
            ? string.Empty
            : $"{CurrentBatchIndex + 1} / {_batchItems.Count}";

    public bool CanGoPreviousBatch => IsImportBatchMode && CurrentBatchIndex > 0;

    public bool CanGoNextBatch => IsImportBatchMode && CurrentBatchIndex < _batchItems.Count - 1;

    /// <summary>队列中未入库，或已入库但仍有未保存编辑的条数。</summary>
    public int CountUnsavedBatchItems()
    {
        if (!IsImportBatchMode)
            return HasUnsavedChanges || !IsSaved ? 1 : 0;

        StashCurrentBatchItem();
        var count = 0;
        for (var i = 0; i < _batchItems.Count; i++)
        {
            var item = _batchItems[i];
            if (!item.IsSaved)
            {
                count++;
                continue;
            }

            var dirty = i == CurrentBatchIndex ? HasUnsavedChanges : item.HasPendingEdits;
            if (dirty)
                count++;
        }

        return count;
    }

    [RelayCommand]
    private void ToggleImportRawText()
    {
        IsImportRawTextExpanded = !IsImportRawTextExpanded;
    }

    [RelayCommand(CanExecute = nameof(CanGoPreviousBatch))]
    private async Task PreviousBatchItemAsync()
    {
        if (!CanGoPreviousBatch) return;
        await SwitchToBatchIndexAsync(CurrentBatchIndex - 1);
    }

    [RelayCommand(CanExecute = nameof(CanGoNextBatch))]
    private async Task NextBatchItemAsync()
    {
        if (!CanGoNextBatch) return;
        await SwitchToBatchIndexAsync(CurrentBatchIndex + 1);
    }

    /// <summary>
    ///     初始化加载默认值（在 SkipLoadDefaults 设置后调用）
    /// </summary>
    public void InitializeDefaults()
    {
        // 加载默认值（除非在改签模式下）
        if (!SkipLoadDefaults) LoadDefaultValues();
    }

    /// <summary>
    ///     应用 OCR/粘贴导入识别稿（预填，不落库）
    /// </summary>
    public async Task ApplyImportDraftAsync(TicketImportDraft draft)
    {
        if (draft == null) return;

        await FillFromImportDraftAsync(draft);
        ApplyImportBanner(draft);
        ResetUndoBaseline();
        BackupOriginalValues();
    }

    private void ApplyImportBanner(TicketImportDraft draft, int? batchIndex = null)
    {
        var review = draft.FieldsNeedingReview.Count > 0
            ? "待核对：" + string.Join("、", draft.FieldsNeedingReview.Take(6))
            : "待确认后保存";

        ImportBannerText = batchIndex is >= 0 && IsImportBatchMode
            ? $"导入来源：{draft.SourceHint} · {batchIndex.Value + 1}/{_batchItems.Count} · {review}"
            : $"导入来源：{draft.SourceHint} · {review}";
        ImportRawText = draft.RawText;
        ShowImportBanner = true;
        IsImportRawTextExpanded = false;
    }

    /// <summary>
    ///     多图导入：初始化批次并加载第一张。
    /// </summary>
    public async Task InitializeImportBatchAsync(IReadOnlyList<TicketImportDraft> drafts)
    {
        _batchItems.Clear();
        if (drafts == null || drafts.Count == 0)
            return;

        foreach (var draft in drafts)
            _batchItems.Add(new ImportBatchItem { Draft = draft });

        IsImportBatchMode = _batchItems.Count > 1;
        CurrentBatchIndex = 0;
        NotifyBatchNavChanged();
        await LoadBatchItemAsync(0, stashCurrent: false);
    }

    /// <summary>
    ///     OCR「直接保存」：校验通过则入库，不弹编辑窗；成功时发 TicketSavedMessage。
    /// </summary>
    public async Task<(bool Ok, string? Error)> TrySaveImportDirectlyAsync()
    {
        if (!TryValidateSilent(out var error))
            return (false, error);

        try
        {
            var newId = await InsertNewTrainRideAsync();
            IsSaved = true;
            HasUnsavedChanges = false;
            LogSaveOperation("OCR直接保存");

            WeakReferenceMessenger.Default.Send(new TicketSavedMessage
            {
                TicketId = newId,
                IsEditMode = false,
                TrainNo = TrainNo
            });

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    ///     应用改签数据
    /// </summary>
    public async Task ApplyRescheduleDataAsync(string departStation, string arriveStation, bool isChangeDestination)
    {
        _isApplyingRescheduleData = true;
        try
        {
            // 设置只读状态
            IsDepartStationReadOnly = true;
            IsArriveStationReadOnly = !isChangeDestination;

            // 先同步到 FormData，确保数据不丢失
            _formData.DepartStationInput = departStation;
            _formData.ArriveStationInput = arriveStation;

            // 设置车站值（这会触发属性变更通知）
            DepartStationInput = departStation;
            ArriveStationInput = arriveStation;

            // 查询并设置车站代码（外键约束需要）
            await QueryDepartStationInfoAsync();
            await QueryArriveStationInfoAsync();

            // 改签状态默认为已完成，并隐藏状态下拉框
            SelectedStatus = "已完成";
            _formData.SelectedStatus = "已完成";
            IsStatusVisible = false;

            // 清空标签选择（新车票不继承原票标签）
            SelectedTagIds.Clear();
            _formData.SelectedTagIds.Clear();

            // 触发属性变更通知，确保UI更新
            OnPropertyChanged(nameof(DepartStationInput));
            OnPropertyChanged(nameof(ArriveStationInput));
            OnPropertyChanged(nameof(DepartStationCode));
            OnPropertyChanged(nameof(ArriveStationCode));
            OnPropertyChanged(nameof(IsDepartStationReadOnly));
            OnPropertyChanged(nameof(IsArriveStationReadOnly));
            OnPropertyChanged(nameof(SelectedStatus));
            OnPropertyChanged(nameof(IsStatusVisible));
            OnPropertyChanged(nameof(SelectedTagIds));
        }
        finally
        {
            _isApplyingRescheduleData = false;
        }
    }

    /// <summary>
    ///     验证表单数据（改签模式下需要验证改签类型）
    /// </summary>
    protected override bool ValidateForm()
    {
        // 先执行基类验证
        if (!base.ValidateForm())
            return false;

        // 改签模式下，验证改签类型是否填写
        if (IsRescheduleMode && string.IsNullOrWhiteSpace(TicketModificationType))
        {
            MessageBoxWindow.Show("请选择改签类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     执行保存：新增或（批次内已入库后）更新；批次模式保存后不关窗。
    /// </summary>
    protected override async Task ExecuteSaveAsync()
    {
        if (!ValidateForm())
            return;

        var isUpdate = EditTicketId is > 0;
        try
        {
            if (isUpdate)
                await UpdateExistingTrainRideAsync();
            else
                await InsertNewTrainRideAsync();

            IsSaved = true;
            HasUnsavedChanges = false;
            LogSaveOperation(isUpdate ? "更新" : "添加");

            if (IsImportBatchMode)
            {
                MarkCurrentBatchSaved(EditTicketId!.Value);
                StashCurrentBatchItem();
                ShowSaveSuccess(isUpdate ? "行程已更新" : "行程已保存", closeWindow: false);
                return;
            }

            ShowSaveSuccessAndClose(isUpdate ? "火车票更新成功" : "火车票添加成功");
        }
        catch (Exception ex)
        {
            ShowSaveError(isUpdate ? "更新" : "添加", ex.Message);
        }
    }

    private async Task SwitchToBatchIndexAsync(int targetIndex)
    {
        if (!IsImportBatchMode || targetIndex < 0 || targetIndex >= _batchItems.Count)
            return;
        if (targetIndex == CurrentBatchIndex)
            return;

        await LoadBatchItemAsync(targetIndex, stashCurrent: true);
    }

    private async Task LoadBatchItemAsync(int index, bool stashCurrent)
    {
        if (index < 0 || index >= _batchItems.Count)
            return;

        if (stashCurrent)
            StashCurrentBatchItem();

        CurrentBatchIndex = index;
        var item = _batchItems[index];

        if (item.StashedForm != null)
        {
            ApplyFormDataClone(item.StashedForm);
            ImportBannerText = item.ImportBannerText;
            ImportRawText = item.ImportRawText;
            IsImportRawTextExpanded = item.IsImportRawTextExpanded;
            ShowImportBanner = true;
            EditTicketId = item.SavedTicketId;
            IsSaved = item.IsSaved;
            IsEditMode = item.SavedTicketId is > 0;
            SetOriginalFormData(item.StashedOriginal ?? item.StashedForm);
            ResetUndoBaseline();
        }
        else
        {
            EditTicketId = item.SavedTicketId;
            IsSaved = item.IsSaved;
            IsEditMode = item.SavedTicketId is > 0;
            await FillFromImportDraftAsync(item.Draft);
            ApplyImportBanner(item.Draft, index);
            ResetUndoBaseline();
            BackupOriginalValues();
            StashCurrentBatchItem();
        }

        SaveButtonText = item.SavedTicketId is > 0 ? "更新" : "保存";
        WindowTitle = IsImportBatchMode
            ? $"添加火车票（{index + 1}/{_batchItems.Count}）"
            : "添加火车票";
        OnPropertyChanged(nameof(WindowTitle));
        NotifyBatchNavChanged();
    }

    private void StashCurrentBatchItem()
    {
        if (!IsImportBatchMode || CurrentBatchIndex < 0 || CurrentBatchIndex >= _batchItems.Count)
            return;

        var item = _batchItems[CurrentBatchIndex];
        item.StashedForm = _formData.Clone();
        item.StashedOriginal = CloneOriginalFormData() ?? _formData.Clone();
        item.ImportBannerText = ImportBannerText;
        item.ImportRawText = ImportRawText;
        item.IsImportRawTextExpanded = IsImportRawTextExpanded;
        item.SavedTicketId = EditTicketId;
        item.IsSaved = EditTicketId is > 0 && IsSaved;
        item.HasPendingEdits = HasUnsavedChanges;
    }

    private void MarkCurrentBatchSaved(int ticketId)
    {
        if (!IsImportBatchMode || CurrentBatchIndex < 0 || CurrentBatchIndex >= _batchItems.Count)
            return;

        var item = _batchItems[CurrentBatchIndex];
        item.IsSaved = true;
        item.SavedTicketId = ticketId;
        item.HasPendingEdits = false;
        item.StashedForm = _formData.Clone();
        item.StashedOriginal = _formData.Clone();
        SaveButtonText = "更新";
        IsEditMode = true;
    }

    private void ResetUndoBaseline()
    {
        OperationHistory.Clear();
        _undoRedoManager.SetInitialState(FormState.FromFormData(_formData.Clone(), string.Empty));
    }

    private void NotifyBatchNavChanged()
    {
        OnPropertyChanged(nameof(BatchPositionText));
        OnPropertyChanged(nameof(CanGoPreviousBatch));
        OnPropertyChanged(nameof(CanGoNextBatch));
        PreviousBatchItemCommand.NotifyCanExecuteChanged();
        NextBatchItemCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentBatchIndexChanged(int value)
    {
        NotifyBatchNavChanged();
    }
}
