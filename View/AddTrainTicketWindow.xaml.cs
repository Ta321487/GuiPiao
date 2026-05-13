using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Utils;
using GuiPiao.ViewModel;
using GuiPiao.ViewModel.TrainTicketForm;

namespace GuiPiao.View;

public partial class AddTrainTicketWindow : Window
{
    private bool _isRescheduleChangeDestination;
    private bool _isRescheduleMode;
    private bool _isUndoRedoMessageRegistered;
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
        TitleTextBlock.Text = $"🎫 {viewModel.WindowTitle}";

        // 设置按钮绑定
        SaveButton.Content = $"💾 {viewModel.SaveButtonText}";
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
    }

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // 获取 ViewModel（直接从 DataContext 获取，确保不为 null）
        if (DataContext is not TrainTicketFormViewModelBase vm) return;

        // 检查是否有必填项未填写
        if (vm.HasRequiredFieldsEmpty())
        {
            var emptyFields = vm.GetEmptyRequiredFields();
            var fieldsText = string.Join("、", emptyFields);

            var result = MessageBoxWindow.Show(
                this,
                $"以下必填项尚未填写：\n{fieldsText}\n\n是否仍要关闭窗口？",
                "必填项未填写",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) e.Cancel = true;
            return;
        }

        // 检查是否有未保存的更改
        if (!vm.HasUnsavedChanges) return;

        // 显示确认对话框
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
                    // 保存失败，显示错误信息
                    MessageBoxWindow.Show(
                        null, // 不传递 owner，避免窗口状态问题
                        $"保存失败：{ex.Message}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                break;
            case MessageBoxResult.No:
                // 放弃更改，直接关闭
                break;
            case MessageBoxResult.Cancel:
            default:
                // 取消关闭
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
        if (DataContext is TrainTicketFormViewModelBase vm) vm.PropertyChanged -= OnViewModelPropertyChanged;
        Closing -= OnWindowClosing;

        // 注销消息订阅
        WeakReferenceMessenger.Default.Unregister<UndoRedoSettingsChangedMessage>(this);
    }
}