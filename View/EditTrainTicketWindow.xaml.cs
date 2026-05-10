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

    private static readonly Dictionary<int, EditTrainTicketWindow> _openEditWindows = new();
    private static readonly object _lock = new();

    public static EditTrainTicketWindow GetInstance(int ticketId)
    {
        lock (_lock)
        {
            if (_openEditWindows.TryGetValue(ticketId, out var existingWindow) && existingWindow.IsVisible)
            {
                existingWindow.Activate();
                existingWindow.WindowState = WindowState.Normal;
                return existingWindow;
            }

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
        var viewModel = new EditTrainTicketViewModel();
        DataContext = viewModel;
        FormView.DataContext = viewModel;

        if (_ticketId.HasValue) await viewModel.LoadTicketByIdAsync(_ticketId.Value);

        TitleTextBlock.Text = $"✏️ {viewModel.WindowTitle}";

        SaveButton.Content = $"💾 {viewModel.SaveButtonText}";
        SaveButton.Command = viewModel.SaveCommand;
        CancelButton.Command = viewModel.CancelCommand;

        HistoryPanel.SetHistoryItems(viewModel.OperationHistory);

        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        RegisterEditShortcuts(viewModel);

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

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is TrainTicketFormViewModelBase vm) vm.PropertyChanged -= OnViewModelPropertyChanged;
        Closing -= OnWindowClosing;

        WeakReferenceMessenger.Default.UnregisterAll(this);

        lock (_lock)
        {
            if (_ticketId.HasValue)
            {
                _openEditWindows.Remove(_ticketId.Value);
            }
        }
    }
}
