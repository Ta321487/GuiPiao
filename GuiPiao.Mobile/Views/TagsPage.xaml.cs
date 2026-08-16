using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class TagsPage : ContentPage
{
    public TagsPage(MeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MeViewModel vm)
            vm.Reload();
    }
}
