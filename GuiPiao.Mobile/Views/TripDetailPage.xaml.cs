using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class TripDetailPage : ContentPage
{
    public TripDetailPage(TripDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TripDetailViewModel vm)
            vm.OnAppearing();
    }
}
