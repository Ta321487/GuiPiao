namespace GuiPiao.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("tripdetail", typeof(Views.TripDetailPage));
        Routing.RegisterRoute("tripform", typeof(Views.TripFormPage));
        Routing.RegisterRoute("ticketface", typeof(Views.TicketFacePage));
        Routing.RegisterRoute("syncscan", typeof(Views.QrScanPage));
        Routing.RegisterRoute("syncconflicts", typeof(Views.SyncConflictsPage));
        Routing.RegisterRoute("syncconnection", typeof(Views.SyncConnectionPage));
        Routing.RegisterRoute("syncstations", typeof(Views.SyncStationsPage));
        Routing.RegisterRoute("tags", typeof(Views.TagsPage));
    }
}
