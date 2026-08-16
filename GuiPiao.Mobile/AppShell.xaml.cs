namespace GuiPiao.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("tripdetail", typeof(Views.TripDetailPage));
        Routing.RegisterRoute("tripform", typeof(Views.TripFormPage));
        Routing.RegisterRoute("ticketface", typeof(Views.TicketFacePage));
    }
}
