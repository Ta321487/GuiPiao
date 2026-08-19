using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using GuiPiao.Model;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

public partial class ManageStationsView : UserControl
{
    private bool _syncingSelection;

    public ManageStationsView()
    {
        InitializeComponent();
        FormAreaRoot.PreviewMouseLeftButtonDown += FormArea_OnPreviewMouseLeftButtonDown;
    }

    private async void StationList_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox list)
            return;
        if (FindAncestorListBoxItem(e.OriginalSource as DependencyObject) != null)
            return;
        if (IsListScrollChrome(e.OriginalSource as DependencyObject))
            return;

        e.Handled = true;
        await ClearSelectionAsync(list);
    }

    private async void FormArea_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveFormControl(e.OriginalSource as DependencyObject))
            return;

        e.Handled = true;
        await ClearSelectionAsync(StationList);
    }

    private async Task ClearSelectionAsync(ListBox list)
    {
        if (DataContext is not ManageStationsViewModel vm)
            return;
        if (vm.IsAdding && vm.SelectedStation == null)
            return;

        if (vm.BeginAddCommand.CanExecute(null))
            await vm.BeginAddCommand.ExecuteAsync(null);

        _syncingSelection = true;
        try
        {
            list.SelectedItem = vm.SelectedStation;
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private static bool IsInteractiveFormControl(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is TextBox or ComboBox or Button or ToggleButton or Thumb or ScrollBar)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private static bool IsListScrollChrome(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is ScrollBar)
                return true;
            if (source is ListBox or ListBoxItem)
                return false;
            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private static ListBoxItem? FindAncestorListBoxItem(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is ListBoxItem item)
                return item;
            if (source is ListBox)
                return null;
            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private async void StationList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection)
            return;
        if (DataContext is not ManageStationsViewModel vm)
            return;
        if (sender is not ListBox list)
            return;

        var item = list.SelectedItem as StationInfo;
        if (item == null)
            return;

        var accepted = await vm.TrySelectStationAsync(item);
        if (accepted)
            return;

        _syncingSelection = true;
        try
        {
            list.SelectedItem = vm.SelectedStation;
        }
        finally
        {
            _syncingSelection = false;
        }
    }
}
