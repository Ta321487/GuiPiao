using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class CapturePage : ContentPage
{
    public CapturePage(CaptureViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
