using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class SyncPage : ContentPage
{
    public SyncPage(SyncViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SyncViewModel vm)
            vm.ReloadFromStore();
    }
}
