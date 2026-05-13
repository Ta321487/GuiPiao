using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace GuiPiao;

public partial class App : Application
{
    private static Mutex? _mutex;
    private readonly GeneralSettingsService _generalSettingsService = null!;
    private readonly DatabaseLifecycleService _lifecycleService = null!;
    private readonly LogService _logService = null!;

    public App()
    {
        _logService = new LogService();
        _lifecycleService = new DatabaseLifecycleService();
        _generalSettingsService = new GeneralSettingsService();

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    ///     配置 LiveCharts 使用微软雅黑字体以支持中文显示，并设置 Tooltip 字体大小
    /// </summary>
    private void ConfigureLiveChartsFont()
    {
        try
        {
            var typeface = SKTypeface.FromFamilyName("Microsoft YaHei");
            var fontSize = GetApplicationFontSize();

            LiveCharts.Configure(config =>
            {
                config.HasGlobalSKTypeface(typeface);
                // 设置全局 Tooltip 字体大小
                config.TooltipTextSize = fontSize;
            });
        }
        catch (Exception ex)
        {
            _logService?.Error("App", $"配置 LiveCharts 字体失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取应用程序字体大小
    /// </summary>
    private double GetApplicationFontSize()
    {
        try
        {
            if (Current?.Resources?.Contains("BaseFontSize") == true) return (double)Current.Resources["BaseFontSize"];
        }
        catch
        {
        }

        return 14; // 默认字体大小
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        _logService.Info("App", "程序启动");

        _mutex = new Mutex(true, "GuiPiao_SingleInstance_Mutex", out var createdNew);
        if (!createdNew)
        {
            _logService.Info("App", "程序已在运行，退出当前实例");
            MessageBoxWindow.Show("程序已经在运行中");
            Current.Shutdown();
            return;
        }

        // 在加载主窗口前配置 LiveCharts 字体（StartupUri 会在 base.OnStartup 中创建 MainWindow）
        ConfigureLiveChartsFont();

        var config = _generalSettingsService.Config;

        base.OnStartup(e);

        WindowManager.RegisterFormWindowType<AddTrainTicketWindow>();
        WindowManager.RegisterFormWindowType<EditTrainTicketWindow>();

        ThemeManager.ApplyTheme(config);
        _logService.Info("App", "主题应用完成");

        // 应用DPI缩放设置
        var uiConfig = new UISettingsService().Config;
        ThemeManager.ApplyDpiScaling(uiConfig.DpiScaling);
        _logService.Info("App", "DPI缩放设置应用完成");

        try
        {
            Database.Initialize();
            _logService.Info("App", "数据库初始化完成");
        }
        catch (Exception ex)
        {
            _logService.Fatal("App", $"数据库初始化失败: {ex.Message}");
            MessageBoxWindow.Show($"数据库初始化失败，程序即将退出：\n{ex.Message}");
            Current.Shutdown();
            return;
        }

        // 执行启动时数据库生命周期操作
        try
        {
            await _lifecycleService.OnStartupAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("App", $"启动时生命周期操作失败: {ex.Message}");
            // 不阻塞启动，仅记录错误
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Debug.WriteLine("[App] OnExit 被调用");
        _logService.Info("App", "程序退出");


        try
        {
            await _lifecycleService.OnExitAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("App", $"退出时生命周期操作失败: {ex.Message}");
        }

        try
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        catch (Exception ex)
        {
            _logService.Error("App", $"释放互斥锁失败: {ex.Message}");
        }

        base.OnExit(e);
    }

    /// <summary>
    ///     保存上次关闭的页面状态
    /// </summary>
    private void SaveLastPageState()
    {
        try
        {
            // 检查各种窗口的打开状态（按优先级顺序）
            var isLogManagerWindowOpen = Current.Windows.OfType<LogManagerWindow>().Any();
            var isMapWindowOpen = Current.Windows.OfType<MapWindow>().Any();

            Debug.WriteLine(
                $"[App] SaveLastPageState: isLogManagerWindowOpen={isLogManagerWindowOpen}, isMapWindowOpen={isMapWindowOpen}");

            LastPageOption lastPage;
            if (isLogManagerWindowOpen)
                lastPage = LastPageOption.LogManager;
            else if (isMapWindowOpen)
                lastPage = LastPageOption.Map;
            else
                lastPage = LastPageOption.MainList;

            Debug.WriteLine($"[App] SaveLastPageState: 保存 lastPage={lastPage}");

            _generalSettingsService.SaveLastPage(lastPage);

            _logService.Info("App", $"保存上次页面状态: {lastPage}");
        }
        catch (Exception ex)
        {
            _logService.Error("App", $"保存上次页面状态失败: {ex.Message}");
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var ex = e.ExceptionObject as Exception;
            _logService.Fatal("App", $"未处理异常: {ex?.Message ?? e.ExceptionObject?.ToString() ?? "未知异常"}");
        }
        catch
        {
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            _logService.Fatal("App", $"调度器异常: {e.Exception?.Message ?? "未知异常"}");
        }
        catch
        {
        }

        e.Handled = true;
    }

    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            _logService.Error("App", $"任务异常: {e.Exception?.InnerException?.Message ?? e.Exception?.Message ?? "未知异常"}");
        }
        catch
        {
        }

        e.SetObserved();
    }
}