using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class TripFormPage : ContentPage
{
    public TripFormPage(TripFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TripFormViewModel vm)
            vm.OnAppearing();
    }
}
