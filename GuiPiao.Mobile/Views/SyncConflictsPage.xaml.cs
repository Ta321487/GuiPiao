using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class SyncConflictsPage : ContentPage
{
    public SyncConflictsPage(SyncViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SyncViewModel vm && vm.IsPaired)
            _ = vm.RefreshConflictsCommand.ExecuteAsync(null);
    }
}
