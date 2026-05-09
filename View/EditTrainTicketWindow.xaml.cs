using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Utils;
using GuiPiao.ViewModel;
using GuiPiao.ViewModel.TrainTicketForm;

namespace GuiPiao.View;

public partial class EditTrainTicketWindow : Window
{
    private readonly int? _ticketId;
    private bool _isUndoRedoMessageRegistered;

    // 静态字典存储已打开的编辑窗口（按车票ID）
    private static readonly Dictionary<int, EditTrainTicketWindow> _openEditWindows = new();
    private static readonly object _lock = new();

    /// <summary>
    ///     获取或创建编辑窗口（单例模式，同一车票只能打开一个编辑窗口）
    /// </summary>
    public static EditTrainTicketWindow GetInstance(int ticketId)
    {
        lock (_lock)
        {
            if (_openEditWindows.TryGetValue(ticketId, out var existingWindow) && existingWindow.IsVisible)
            {
                // 已存在且可见，激活并返回现有窗口
                existingWindow.Activate();
                existingWindow.WindowState = WindowState.Normal;
                return existingWindow;
            }

            // 创建新窗口
            var newWindow = new EditTrainTicketWindow(ticketId);
            _openEditWindows[ticketId] = newWindow;
            return newWindow;
        }
    }

    public EditTrainTicketWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
    }

    public EditTrainTicketWindow(int ticketId) : this()
    {
        _ticketId = ticketId;
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // 创建 ViewModel
        var viewModel = new EditTrainTicketViewModel();

        // 先设置 DataContext，让 UI 开始绑定
        DataContext = viewModel;
        FormView.DataContext = viewModel;

        // 如果指定了车票ID，异步加载数据
        if (_ticketId.HasValue) await viewModel.LoadTicketByIdAsync(_ticketId.Value);

        // 设置标题
        TitleTextBlock.Text = $"✏️ {viewModel.WindowTitle}";

        // 设置按钮绑定
        SaveButton.Content = $"💾 {viewModel.SaveButtonText}";
        SaveButton.Command = viewModel.SaveCommand;
        CancelButton.Command = viewModel.CancelCommand;

        // 绑定操作历史面板
        HistoryPanel.SetHistoryItems(viewModel.OperationHistory);

        // 订阅属性变更事件
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // 注册编辑操作相关的快捷键
        RegisterEditShortcuts(viewModel);

        // 订阅撤销重做设置变更消息
        if (!_isUndoRedoMessageRegistered)
        {
            WeakReferenceMessenger.Default.Register<UndoRedoSettingsChangedMessage>(this, (recipient, message) =>
            {
                if (DataContext is TrainTicketFormViewModelBase vm) vm.RefreshUndoRedoSettings();
            });
            _isUndoRedoMessageRegistered = true;
        }

        viewModel.RefreshUndoRedoSettings();

        Loaded -= OnWindowLoaded;
    }

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
        if (sender is TrainTicketFormViewModelBase vm &&
            e.PropertyName != nameof(vm.HasUnsavedChanges) &&
            e.PropertyName != nameof(vm.StationNames) &&
            e.PropertyName != nameof(vm.SelectedTagIds))
            vm.CheckForChanges();
    }

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not TrainTicketFormViewModelBase vm) return;

        if (vm.HasUnsavedChanges && vm.HasRequiredFieldsEmpty())
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
                    if (vm.SaveCommand is IAsyncRelayCommand asyncCommand)
                    {
                        await asyncCommand.ExecuteAsync(null);
                    }
                    else
                    {
                        vm.SaveCommand.Execute(null);
                    }

                    if (!vm.HasUnsavedChanges)
                    {
                        Closing -= OnWindowClosing;
                        Close();
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
                break;
            case MessageBoxResult.Cancel:
            default:
                e.Cancel = true;
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is TrainTicketFormViewModelBase vm) vm.PropertyChanged -= OnViewModelPropertyChanged;
        Closing -= OnWindowClosing;

        WeakReferenceMessenger.Default.Unregister<UndoRedoSettingsChangedMessage>(this);

        // 从字典中移除
        lock (_lock)
        {
            if (_ticketId.HasValue)
            {
                _openEditWindows.Remove(_ticketId.Value);
            }
        }
    }
}