using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class TripsPage : ContentPage
{
    public TripsPage(TripsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TripsViewModel vm)
            vm.OnAppearing();
    }

    private async void OnGoSyncClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//sync");
    }
}
