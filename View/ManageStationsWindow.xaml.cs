using System.ComponentModel;
using System.Windows;

namespace GuiPiao.View;

public partial class ManageStationsWindow : Window
{
    private bool _forceClose;

    public ManageStationsWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_forceClose)
            return;

        if (Content is not ManageStationsView view || view.DataContext is not ViewModel.ManageStationsViewModel vm)
            return;

        if (!vm.HasUnsavedChanges)
            return;

        e.Cancel = true;
        if (!await vm.ConfirmLeaveAsync())
            return;

        _forceClose = true;
        Close();
    }
}
