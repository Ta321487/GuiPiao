using GuiPiao.Mobile.Services;
using GuiPiao.Mobile.ViewModels;

namespace GuiPiao.Mobile;

public partial class App : Application
{
    private readonly ThemeService _theme;
    private readonly MobileSettingsStore _settings;

    public App(ThemeService theme, MobileSettingsStore settings)
    {
        InitializeComponent();
        _theme = theme;
        _settings = settings;
        _theme.Apply(_settings.LoadAppearance());

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            CrashLog.Write("UnhandledException", e.ExceptionObject as Exception
                ?? new Exception(e.ExceptionObject?.ToString() ?? "unknown"));
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLog.Write("UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new AppShell());
}
