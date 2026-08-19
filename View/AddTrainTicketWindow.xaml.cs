using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Controls;
using GuiPiao.Icons;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Utils;
using GuiPiao.ViewModel;
using GuiPiao.ViewModel.TrainTicketForm;

namespace GuiPiao.View;

public partial class AddTrainTicketWindow : Window
{
    private bool _isRescheduleChangeDestination;
    private bool _isRescheduleMode;
    private bool _isUndoRedoMessageRegistered;
    private TicketImportDraft? _importDraft;
    private IReadOnlyList<TicketImportDraft>? _importDrafts;
    private string? _rescheduleArriveStation;
    private string? _rescheduleDepartStation;

    public AddTrainTicketWindow()
    {
        InitializeComponent();

        // 在窗口加载后设置 DataContext，避免 XAML 实例化时的阻塞
        Loaded += OnWindowLoaded;
        // 订阅关闭事件（包括点击右上角X按钮）
        Closing += OnWindowClosing;
    }

    /// <summary>
    ///     创建改签窗口
    /// </summary>
    public static AddTrainTicketWindow CreateRescheduleWindow(string departStation, string arriveStation,
        bool isChangeDestination)
    {
        var window = new AddTrainTicketWindow
        {
            _isRescheduleMode = true,
            _rescheduleDepartStation = departStation,
            _rescheduleArriveStation = arriveStation,
            _isRescheduleChangeDestination = isChangeDestination
        };
        return window;
    }

    /// <summary>
    ///     创建 OCR/粘贴导入预填窗口
    /// </summary>
    public static AddTrainTicketWindow CreateFromImportDraft(TicketImportDraft draft)
    {
        return new AddTrainTicketWindow
        {
            _importDraft = draft
        };
    }

    /// <summary>
    ///     创建多图 OCR 导入窗口（同一窗左右切换）
    /// </summary>
    public static AddTrainTicketWindow CreateFromImportDrafts(IReadOnlyList<TicketImportDraft> drafts)
    {
        if (drafts == null || drafts.Count == 0)
            throw new ArgumentException("识别稿列表不能为空", nameof(drafts));

        if (drafts.Count == 1)
            return CreateFromImportDraft(drafts[0]);

        return new AddTrainTicketWindow
        {
            _importDrafts = drafts.ToList()
        };
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 创建 ViewModel
        var viewModel = new AddTrainTicketViewModel();

        // 如果是改签模式，先设置标志
        if (_isRescheduleMode) viewModel.SkipLoadDefaults = true;

        // 初始化默认值（根据 SkipLoadDefaults 决定是否加载）
        viewModel.InitializeDefaults();

        // 设置 DataContext
        DataContext = viewModel;
        FormView.DataContext = viewModel;

        // 设置标题
        TitleTextBlock.Text = viewModel.WindowTitle;

        // 设置按钮绑定
        SaveButton.Content = IconLabel.Create(AppIcons.Save, viewModel.SaveButtonText);
        SaveButton.Command = viewModel.SaveCommand;
        CancelButton.Command = viewModel.CancelCommand;

        // 绑定操作历史面板
        HistoryPanel.SetHistoryItems(viewModel.OperationHistory);

        // 如果是改签模式，应用改签数据
        if (_isRescheduleMode)
        {
            viewModel.IsRescheduleMode = true;
            await viewModel.ApplyRescheduleDataAsync(_rescheduleDepartStation ?? string.Empty,
                _rescheduleArriveStation ?? string.Empty, _isRescheduleChangeDestination);
        }
        else if (_importDrafts != null && _importDrafts.Count > 0)
        {
            await viewModel.InitializeImportBatchAsync(_importDrafts);
            TitleTextBlock.Text = viewModel.WindowTitle;
            SaveButton.Content = IconLabel.Create(AppIcons.Save, viewModel.SaveButtonText);
        }
        else if (_importDraft != null)
        {
            await viewModel.ApplyImportDraftAsync(_importDraft);
        }

        // 订阅属性变更事件以检测更改（在 ViewModel 初始化完成后订阅）
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // 注册编辑操作相关的快捷键（撤销/重做）
        RegisterEditShortcuts(viewModel);

        // 订阅撤销重做设置变更消息
        if (!_isUndoRedoMessageRegistered)
        {
            WeakReferenceMessenger.Default.Register<UndoRedoSettingsChangedMessage>(this, (recipient, message) =>
            {
                Debug.WriteLine("[AddTrainTicketWindow] 收到 UndoRedoSettingsChangedMessage");
                if (DataContext is TrainTicketFormViewModelBase vm) vm.RefreshUndoRedoSettings();
            });
            _isUndoRedoMessageRegistered = true;
            Debug.WriteLine("[AddTrainTicketWindow] 已订阅 UndoRedoSettingsChangedMessage");
        }

        // 立即刷新撤销重做设置（确保打开窗口时应用最新设置）
        viewModel.RefreshUndoRedoSettings();

        // 移除事件处理
        Loaded -= OnWindowLoaded;
    }

    /// <summary>
    ///     注册编辑操作相关的快捷键
    /// </summary>
    private void RegisterEditShortcuts(TrainTicketFormViewModelBase viewModel)
    {
        ShortcutBehavior.RegisterEditShortcuts(this, actionId =>
        {
            return actionId switch
            {
                "Undo" => viewModel.UndoCommand,
                "Redo" => viewModel.RedoCommand,
                _ => null
            };
        });
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 当任何表单属性变更时，检查是否有未保存的更改
        // 排除 HasUnsavedChanges 自身、StationNames 和 SelectedTagIds 的变更
        // SelectedTagIds 的变更由 ToggleTagSelection 方法处理，不需要再次触发 CheckForChanges
        if (sender is TrainTicketFormViewModelBase vm &&
            e.PropertyName != nameof(vm.HasUnsavedChanges) &&
            e.PropertyName != nameof(vm.StationNames) &&
            e.PropertyName != nameof(vm.SelectedTagIds))
            vm.CheckForChanges();

        if (sender is AddTrainTicketViewModel addVm)
        {
            if (e.PropertyName is nameof(AddTrainTicketViewModel.WindowTitle) or nameof(AddTrainTicketViewModel.BatchPositionText))
                TitleTextBlock.Text = addVm.WindowTitle;

            if (e.PropertyName == nameof(AddTrainTicketViewModel.SaveButtonText))
                SaveButton.Content = IconLabel.Create(AppIcons.Save, addVm.SaveButtonText);
        }
    }

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not TrainTicketFormViewModelBase vm) return;

        var addVm = DataContext as AddTrainTicketViewModel;
        var isBatch = addVm?.IsImportBatchMode == true;
        var unsavedBatchCount = isBatch ? addVm!.CountUnsavedBatchItems() : 0;

        // 必填项：单张/多图共用同一确认框，多图附加队列说明
        if (vm.HasRequiredFieldsEmpty())
        {
            var fieldsText = string.Join("、", vm.GetEmptyRequiredFields());
            var message = isBatch
                ? $"当前行程（{addVm!.BatchPositionText}）以下必填项尚未填写：\n{fieldsText}\n\n" +
                  $"队列中尚有 {unsavedBatchCount} 条未保存。关闭后未保存内容将丢失。\n\n" +
                  "是否仍要关闭窗口？"
                : $"以下必填项尚未填写：\n{fieldsText}\n\n是否仍要关闭窗口？";

            var result = MessageBoxWindow.Show(
                this,
                message,
                "必填项未填写",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
                e.Cancel = true;
            return;
        }

        // 多图：有未保存则 Yes/No 丢弃，不走「是否保存更改」
        if (isBatch)
        {
            if (unsavedBatchCount == 0)
                return;

            var batchResult = MessageBoxWindow.Show(
                this,
                $"队列中尚有 {unsavedBatchCount} 条未保存。关闭后未保存内容将丢失。\n\n是否仍要关闭窗口？",
                "未保存的行程",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (batchResult == MessageBoxResult.No)
                e.Cancel = true;
            return;
        }

        if (!vm.HasUnsavedChanges) return;

        var result2 = MessageBoxWindow.Show(
            this,
            "您有未保存的车票信息。\n\n是否保存更改？",
            "未保存的更改",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        switch (result2)
        {
            case MessageBoxResult.Yes:
                e.Cancel = true;

                try
                {
                    if (vm.SaveCommand.CanExecute(null))
                    {
                        if (vm.SaveCommand is IAsyncRelayCommand asyncCommand)
                            await asyncCommand.ExecuteAsync(null);
                        else
                            vm.SaveCommand.Execute(null);

                        if (!vm.HasUnsavedChanges)
                        {
                            Closing -= OnWindowClosing;
                            Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxWindow.Show(
                        null,
                        $"保存失败：{ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                break;
            case MessageBoxResult.No:
                break;
            case MessageBoxResult.Cancel:
            default:
                e.Cancel = true;
                break;
        }
    }

    /// <summary>
    ///     窗口关闭后清理资源
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // 清理事件订阅
        if (DataContext is TrainTicketFormViewModelBase vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.Cleanup();
        }
        Closing -= OnWindowClosing;

        // 注销消息订阅
        WeakReferenceMessenger.Default.Unregister<UndoRedoSettingsChangedMessage>(this);
    }
}
