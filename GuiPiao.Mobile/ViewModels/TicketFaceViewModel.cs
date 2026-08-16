using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Model;

namespace GuiPiao.Mobile.ViewModels;

public partial class TicketFaceViewModel : ObservableObject, IQueryAttributable
{
    private readonly RideRepository _rides;

    [ObservableProperty] private MobileRide? _ride;
    [ObservableProperty] private bool _hasRide;
    [ObservableProperty] private string _title = "票面预览";

    public TicketFaceViewModel(RideRepository rides) => _rides = rides;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("syncId", out var v) || v == null)
        {
            Ride = null;
            HasRide = false;
            return;
        }

        var syncId = Uri.UnescapeDataString(v.ToString() ?? "");
        try
        {
            Ride = _rides.GetActiveBySyncId(syncId);
        }
        catch
        {
            Ride = null;
        }

        HasRide = Ride != null;
        Title = Ride == null ? "票面" : "报销凭证";
    }

    [RelayCommand]
    private async Task CloseAsync() => await Shell.Current.GoToAsync("..");
}
