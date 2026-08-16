using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class SyncConnectionPage : ContentPage
{
    public SyncConnectionPage(SyncViewModel vm)
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
