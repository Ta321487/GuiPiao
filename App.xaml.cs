using System;
using System.Diagnostics;
using System.IO;
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

        var config = _generalSettingsService.Config;
        if (config.SingleInstance)
        {
            _mutex = new Mutex(true, "GuiPiao_SingleInstance_Mutex", out var createdNew);
            if (!createdNew)
            {
                _logService.Info("App", "程序已在运行，退出当前实例");
                MessageBoxWindow.Show("程序已经在运行中");
                Current.Shutdown();
                return;
            }
        }

        // 先应用主题/DPI，再创建任何窗口，避免无主题白屏
        ThemeManager.ApplyTheme(config);
        _logService.Info("App", "主题应用完成");

        var uiConfig = new UISettingsService().Config;
        ThemeManager.ApplyDpiScaling(uiConfig.DpiScaling);
        _logService.Info("App", "DPI缩放设置应用完成");

        ConfigureLiveChartsFont();
        base.OnStartup(e);
        // 启动过程以启动页为过渡；主窗口就绪后再切过去
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        WindowManager.RegisterFormWindowType<AddTrainTicketWindow>();
        WindowManager.RegisterFormWindowType<EditTrainTicketWindow>();

        SplashWindow? splash = null;
        try
        {
            splash = new SplashWindow();
            ThemeManager.ApplyThemeToWindow(splash);
            splash.Show();
            splash.SetStatus("正在初始化数据库…");
        }
        catch (Exception ex)
        {
            _logService.Error("App", $"启动页显示失败: {ex.Message}");
        }

        try
        {
            Database.Initialize();
            _logService.Info("App", "数据库初始化完成");
        }
        catch (Exception ex)
        {
            splash?.Close();
            _logService.Fatal("App", $"数据库初始化失败: {ex.Message}");
            if (!TryRecoverDatabase(ex))
            {
                MessageBoxWindow.Show(
                    $"数据库无法打开，程序即将退出：\n{ex.Message}\n\n可稍后从「%AppData%\\GuiPiao\\Backups」手动恢复备份。",
                    "数据库错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }
        }

        try
        {
            splash?.SetStatus("正在加载主界面…");
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            ThemeManager.ApplyThemeToWindow(mainWindow);

            // 主窗先不可见，等首帧渲染完再与启动页切换，杜绝白闪
            mainWindow.Opacity = 0;
            mainWindow.ShowInTaskbar = false;
            EventHandler? rendered = null;
            rendered = (_, _) =>
            {
                mainWindow.ContentRendered -= rendered;
                mainWindow.ShowInTaskbar = true;
                mainWindow.Opacity = 1;
                try
                {
                    splash?.Close();
                }
                catch
                {
                    // ignore
                }

                mainWindow.Activate();
            };
            mainWindow.ContentRendered += rendered;
            mainWindow.Show();
            _logService.Info("App",
                $"主窗口已显示 Content={(mainWindow.Content == null ? "null" : mainWindow.Content.GetType().Name)}");
        }
        catch (Exception ex)
        {
            try
            {
                splash?.Close();
            }
            catch
            {
            }

            _logService.Fatal("App", $"主窗口创建失败: {ex}");
            MessageBox.Show($"主窗口创建失败：\n{ex}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
            return;
        }

        try
        {
            await _lifecycleService.OnStartupAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("App", $"启动时生命周期操作失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     数据库损坏时提供恢复选项，避免直接无法启动。
    /// </summary>
    private bool TryRecoverDatabase(Exception originalError)
    {
        var dbPath = DatabaseRecovery.GetDatabaseFilePath(ConfigManager.Instance.DatabaseConnectionString);
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GuiPiao", "Backups");

        var choice = MessageBoxWindow.Show(
            $"数据库初始化失败：\n{originalError.Message}\n\n当前文件：\n{dbPath}\n\n【是】从最近自动备份恢复并继续\n【否】隔离损坏文件并新建空库\n【取消】退出程序",
            "数据库损坏",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
            yesText: "恢复备份",
            noText: "新建空库",
            cancelText: "退出");

        try
        {
            if (choice == MessageBoxResult.Yes)
            {
                if (!DatabaseRecovery.TryRestoreFromBackup(dbPath, backupDir, out var used) || used == null)
                {
                    MessageBoxWindow.Show(
                        "未找到可用的自动备份。\n请检查：\n" + backupDir,
                        "恢复失败",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                ConfigManager.Instance.ReloadConfig();
                Database.Initialize();
                _logService.Info("App", $"已从备份恢复数据库: {used}");
                MessageBoxWindow.Show($"已从备份恢复并启动。\n来源：\n{used}", "恢复成功");
                return true;
            }

            if (choice == MessageBoxResult.No)
            {
                DatabaseRecovery.CreateEmptyDatabaseFile(dbPath);
                ConfigManager.Instance.ReloadConfig();
                Database.Initialize();
                _logService.Info("App", "已新建空数据库");
                MessageBoxWindow.Show(
                    "已新建空数据库。原损坏文件已改名为 *.broken_*，可稍后手动处理。",
                    "已新建");
                return true;
            }
        }
        catch (Exception recoverEx)
        {
            _logService.Fatal("App", $"数据库恢复失败: {recoverEx.Message}");
            MessageBoxWindow.Show($"恢复过程失败：\n{recoverEx.Message}", "恢复失败",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return false;
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
            var ex = e.Exception;
            var detail = ex == null
                ? "未知异常"
                : $"{ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}";
            if (ex?.InnerException != null)
                detail +=
                    $"\n--- Inner ---\n{ex.InnerException.GetType().FullName}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";

            _logService.Fatal("App", $"调度器异常: {detail}");
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