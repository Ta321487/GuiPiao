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

        // 联调期：未处理异常记日志，避免完全无迹可循
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[Unhandled] " + e.ExceptionObject);
            }
            catch
            {
                // ignore
            }
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[UnobservedTask] " + e.Exception);
                e.SetObserved();
            }
            catch
            {
                // ignore
            }
        };
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new AppShell());
}
