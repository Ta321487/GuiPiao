using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using GuiPiao.Services;
using GuiPiao.ViewModel;
using Microsoft.Web.WebView2.Core;

namespace GuiPiao.View;

/// <summary>
///     Web消息结构
/// </summary>
public class WebMessage
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")] public JsonElement? Data { get; set; }
}

/// <summary>
///     MapWindow.xaml 的交互逻辑
/// </summary>
public partial class MapWindow : Window
{
    private bool _isDisposed;
    private MapWindowViewModel? _viewModel;

    public MapWindow()
    {
        InitializeComponent();

        // 应用主题
        ThemeManager.ApplyThemeToWindow(this);

        // 设置DataContext
        _viewModel = new MapWindowViewModel();
        DataContext = _viewModel;

        // 初始化WebView2（延迟执行，避免阻塞窗口显示）
        Loaded += MapWindow_Loaded;
    }

    /// <summary>
    ///     窗口关闭时执行完整清理流程
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        if (!_isDisposed)
        {
            PerformCleanup();
            _isDisposed = true;
        }

        base.OnClosed(e);
    }

    /// <summary>
    ///     执行完整的资源清理流程
    /// </summary>
    private void PerformCleanup()
    {
        try
        {
            // 1. 断开 View 事件订阅
            Loaded -= MapWindow_Loaded;

            // 2. 取消 WebView2 事件订阅
            if (MapWebView?.CoreWebView2 != null)
            {
                MapWebView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
                MapWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            }

            // 3. 调用 ViewModel.Cleanup() 断开静态事件
            _viewModel?.Cleanup();

            // 4. 彻底销毁 WebView2（必须显式 Dispose 才能杀掉 msedgewebview2.exe 进程）
            if (MapWebView != null)
            {
                try
                {
                    // 先导航到空白页
                    if (MapWebView.CoreWebView2 != null) MapWebView.CoreWebView2.Navigate("about:blank");
                }
                catch
                {
                }

                // 显式 Dispose 以释放非托管资源
                MapWebView.Dispose();
            }

            // 5. 将 DataContext 置为 null
            DataContext = null;
            _viewModel = null;

            // 6. 强制执行垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Debug.WriteLine("[MapWindow] 资源清理完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapWindow] 清理过程中发生错误: {ex.Message}");
        }
    }

    private void MapWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 窗口加载完成后再初始化WebView2
        InitializeWebView2();
    }

    private async void InitializeWebView2()
    {
        try
        {
            // 加载地图设置
            var settingsService = new MapSettingsService();
            var config = settingsService.Config;

            // 配置WebView2环境选项
            var options = new CoreWebView2EnvironmentOptions();

            // 根据设置启用/禁用硬件加速
            if (!config.EnableHardwareAcceleration)
                // 禁用硬件加速（通过命令行参数）
                options.AdditionalBrowserArguments = "--disable-gpu";

            // 确保WebView2运行时已安装
            var webView2Environment = await CoreWebView2Environment.CreateAsync(
                null,
                Path.Combine(Path.GetTempPath(), "GuiPiaoWebView2"),
                options);

            await MapWebView.EnsureCoreWebView2Async(webView2Environment);

            // 加载地图设置
            var mapConfig = settingsService.Config;

            // 设置WebView2配置
            MapWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            MapWebView.CoreWebView2.Settings.AreDevToolsEnabled = mapConfig.EnableDevTools;

            // 禁止链接点击（通过拦截导航事件）
            MapWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

            // 添加消息接收处理器
            MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            // 加载地图HTML
            LoadMapHtml();

            // 如果启用了自动清理缓存，检查并清理过期缓存
            if (config.AutoCleanCache) await CleanOldCacheAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化地图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     清理30天未访问的缓存
    /// </summary>
    private async Task CleanOldCacheAsync()
    {
        try
        {
            var cachePath = Path.Combine(Path.GetTempPath(), "GuiPiaoWebView2");
            if (!Directory.Exists(cachePath))
                return;

            var cutoffDate = DateTime.Now.AddDays(-30);
            var dirsToClean = new DirectoryInfo(cachePath)
                .GetDirectories()
                .Where(d => d.LastAccessTime < cutoffDate)
                .ToList();

            foreach (var dir in dirsToClean)
                try
                {
                    dir.Delete(true);
                }
                catch
                {
                    // 忽略删除失败的目录
                }

            // 清理过期文件
            var filesToClean = new DirectoryInfo(cachePath)
                .GetFiles("*", SearchOption.AllDirectories)
                .Where(f => f.LastAccessTime < cutoffDate)
                .ToList();

            foreach (var file in filesToClean)
                try
                {
                    file.Delete();
                }
                catch
                {
                    // 忽略删除失败的文件
                }
        }
        catch
        {
            // 忽略清理错误
        }
    }

    private void LoadMapHtml()
    {
        try
        {
            // 获取地图HTML文件路径
            var htmlPath = GetMapHtmlPath();
            if (File.Exists(htmlPath))
                // 使用Navigate而不是设置Source，避免与EnsureCoreWebView2Async冲突
                MapWebView.CoreWebView2?.Navigate(htmlPath);
            else
                MessageBox.Show("地图文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载地图失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetMapHtmlPath()
    {
        // 首先检查输出目录
        var exePath = Assembly.GetExecutingAssembly().Location;
        var exeDir = Path.GetDirectoryName(exePath)!;
        var htmlPath = Path.Combine(exeDir, "Resources", "Map", "map.html");

        if (File.Exists(htmlPath)) return htmlPath;

        // 如果在开发环境，检查项目目录
        var projectDir = Path.GetFullPath(Path.Combine(exeDir, "..", "..", ".."));
        htmlPath = Path.Combine(projectDir, "Resources", "Map", "map.html");

        if (File.Exists(htmlPath)) return htmlPath;

        // 返回默认路径（即使不存在）
        return Path.Combine(exeDir, "Resources", "Map", "map.html");
    }

    /// <summary>
    ///     导航开始事件 - 禁止点击链接跳转到外部页面
    /// </summary>
    private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // 获取当前加载的地图HTML文件路径
        var mapHtmlPath = GetMapHtmlPath();
        var mapHtmlUri = new Uri(mapHtmlPath).ToString().ToLowerInvariant();

        // 获取导航目标URI
        var uri = e.Uri.ToLowerInvariant();

        // 允许本地地图HTML文件加载
        if (uri == mapHtmlUri || (uri.StartsWith("file://") && uri.EndsWith("map.html"))) return; // 允许导航

        // 禁止所有其他导航（包括点击链接）
        e.Cancel = true;
        Debug.WriteLine($"已阻止导航到：{e.Uri}");
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.TryGetWebMessageAsString();
            if (!string.IsNullOrEmpty(message)) HandleWebMessage(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"处理Web消息失败：{ex.Message}");
        }
    }

    private void HandleWebMessage(string message)
    {
        try
        {
            var msgData = JsonSerializer.Deserialize<WebMessage>(message);
            if (msgData == null) return;

            switch (msgData.Type)
            {
                case "stationClick":
                    // 车站点击事件
                    var stationName = msgData.Data?.GetProperty("stationName").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.OnStationClick(stationName); });
                    break;

                case "tripClick":
                    // 行程点击事件
                    var tripId = msgData.Data?.GetProperty("tripId").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.OnTripClick(tripId); });
                    break;

                case "mapReady":
                    // 地图加载完成
                    Dispatcher.Invoke(() =>
                    {
                        // 如果_viewModel为null，尝试从DataContext获取
                        if (_viewModel == null) _viewModel = DataContext as MapWindowViewModel;

                        if (_viewModel != null)
                        {
                            // 确保WebView已设置
                            _viewModel.SetWebView(MapWebView);
                            _viewModel.OnMapReady();
                        }
                    });
                    break;

                case "error":
                    // 地图错误
                    var errorMsg = msgData.Data?.GetProperty("message").GetString() ?? "未知错误";
                    Dispatcher.Invoke(() =>
                    {
                        if (_viewModel != null) _viewModel.StatusMessage = $"地图错误：{errorMsg}";
                    });
                    break;

                case "clearSelection":
                    // 取消选中（点击地图空白处）
                    Dispatcher.Invoke(() => { _viewModel?.ClearSelection(); });
                    break;

                case "openTicketEdit":
                    // 双击行程 - 打开车票编辑
                    var editTripId = msgData.Data?.GetProperty("tripId").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.OpenTicketEdit(editTripId); });
                    break;

                case "showTicketDetails":
                    // 双击行程 - 显示车票详情
                    var detailsTripId = msgData.Data?.GetProperty("tripId").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.ShowTicketDetails(detailsTripId); });
                    break;

                case "showStationTickets":
                    // 双击车站 - 显示车站车票列表
                    var dblClickStationName = msgData.Data?.GetProperty("stationName").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.ShowStationTickets(dblClickStationName); });
                    break;

                case "previewTicket":
                    // 双击行程 - 车票票面预览
                    var previewTripId = msgData.Data?.GetProperty("tripId").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.PreviewTicket(previewTripId); });
                    break;

                case "showStationStats":
                    // 双击车站 - 统计该站出行数据
                    var statsStationName = msgData.Data?.GetProperty("stationName").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.ShowStationStats(statsStationName); });
                    break;

                case "createTicketFromStation":
                    // 双击车站 - 新建以该站为出发站的车票
                    var createFromStationName = msgData.Data?.GetProperty("stationName").GetString();
                    Dispatcher.Invoke(() => { _viewModel?.CreateTicketFromStation(createFromStationName); });
                    break;

                case "createNewTicket":
                    // 双击空白处 - 新建车票录入
                    Dispatcher.Invoke(() => { _viewModel?.CreateNewTicket(); });
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"处理消息失败：{ex.Message}");
        }
    }

    private void MapWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _viewModel = DataContext as MapWindowViewModel;
            if (_viewModel != null)
            {
                _viewModel.SetWebView(MapWebView);
                _viewModel.StatusMessage = "地图页面加载完成，等待JavaScript初始化...";
            }
        }
        else
        {
            if (_viewModel != null) _viewModel.StatusMessage = $"地图加载失败：{e.WebErrorStatus}";
        }
    }

    /// <summary>
    ///     向地图发送数据
    /// </summary>
    public void SendDataToMap(string data)
    {
        if (MapWebView.CoreWebView2 != null)
        {
            var script = $"window.receiveData({data});";
            MapWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }
}