using GuiPiao.Mobile.Data;
using GuiPiao.Mobile.Services;
using GuiPiao.Mobile.ViewModels;
using GuiPiao.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace GuiPiao.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<MobileSettingsStore>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<SyncApiClient>();
        builder.Services.AddSingleton<SyncPullBuffer>();
        builder.Services.AddSingleton<SyncPushQueue>();
        builder.Services.AddSingleton<CapturePrefillStore>();
        builder.Services.AddSingleton<MobileDatabase>();
        builder.Services.AddSingleton<RideRepository>();
        builder.Services.AddSingleton<TagRepository>();
        builder.Services.AddSingleton<StationCacheRepository>();
        builder.Services.AddSingleton<MobileSyncIngressService>();
        builder.Services.AddSingleton<RideWriteService>();
        builder.Services.AddSingleton<TagWriteService>();
        builder.Services.AddTransient<TripsViewModel>();
        builder.Services.AddTransient<TripDetailViewModel>();
        builder.Services.AddTransient<TripFormViewModel>();
        builder.Services.AddTransient<TicketFaceViewModel>();
        builder.Services.AddTransient<CaptureViewModel>();
        builder.Services.AddTransient<SyncViewModel>();
        builder.Services.AddTransient<MeViewModel>();
        builder.Services.AddTransient<TripsPage>();
        builder.Services.AddTransient<TripDetailPage>();
        builder.Services.AddTransient<TripFormPage>();
        builder.Services.AddTransient<TicketFacePage>();
        builder.Services.AddTransient<CapturePage>();
        builder.Services.AddTransient<SyncPage>();
        builder.Services.AddTransient<MePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        app.Services.GetRequiredService<MobileDatabase>().EnsureCreated();
        return app;
    }
}
