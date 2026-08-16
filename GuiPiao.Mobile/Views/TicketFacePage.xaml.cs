using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile.Views;

public partial class TicketFacePage : ContentPage
{
    public TicketFacePage(TicketFaceViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
