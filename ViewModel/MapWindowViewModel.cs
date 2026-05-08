using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.View;
using Microsoft.Web.WebView2.Wpf;

namespace GuiPiao.ViewModel;

/// <summary>
///     地图窗口ViewModel
/// </summary>
public partial class MapWindowViewModel : ObservableObject
{
    private static readonly LogService _logService = new();
    private readonly MapDataService _mapDataService = new();
    private List<TicketData> _allTickets = new();

    private bool _isDataLoaded;
    private string _currentDirectionFilter = "All"; // 当前方向过滤值
    private CancellationTokenSource? _statusResetCts; // 用于取消状态重置延迟

    [ObservableProperty] private bool _isMapReady;

    /// <summary>
    ///     数据是否已加载完成
    /// </summary>
    public bool IsDataLoaded => _isDataLoaded;

    private List<string> _stationsWithoutCoordinates = new();

    [ObservableProperty] private string _statusMessage = "正在加载地图...";

    private WebView2? _webView;

    public MapWindowViewModel()
    {
        // 订阅地图设置已保存事件
        MapSettingsViewModel.MapSettingsSaved += OnMapSettingsSaved;

        // 订阅主题更改事件
        ThemeManager.ThemeChanged += OnThemeChanged;

        // 异步加载车票数据
        _ = InitializeDataAsync();
    }

    /// <summary>
    ///     初始化数据
    /// </summary>
    private async Task InitializeDataAsync()
    {
        await LoadTicketDataAsync();
        _isDataLoaded = true;

        // 如果地图已经就绪，发送数据
        if (IsMapReady && _webView?.CoreWebView2 != null)
            Application.Current.Dispatcher.Invoke(() => { ApplyDefaultMapDisplay(); });
    }

    /// <summary>
    ///     地图设置已保存事件处理
    /// </summary>
    private async void OnMapSettingsSaved(object? sender, EventArgs e)
    {
        if (!IsMapReady || _webView?.CoreWebView2 == null) return;
        
        var settingsService = new MapSettingsService();
        settingsService.RefreshConfig();
        var newDirectionFilter = settingsService.Config.DirectionFilter;
        
        if (_currentDirectionFilter != newDirectionFilter)
        {
            _currentDirectionFilter = newDirectionFilter;
            StatusMessage = "方向过滤已更改，正在重新加载数据...";
            
            // 应用新设置后重新加载数据
            ApplyAllMapSettings();
            if (_isDataLoaded && _allTickets.Count > 0)
            {
                SendTicketsToMap(_allTickets);
            }
            StatusMessage = "数据已重新加载";
        }
        else
        {
            ApplyAllMapSettings();
        }
    }

    /// <summary>
    ///     主题更改事件处理
    /// </summary>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        // 在UI线程上应用新的主题设置
        Application.Current.Dispatcher.Invoke(() => { ApplyThemeToMap(); });
    }

    /// <summary>
    ///     清理资源
    /// </summary>
    public void Cleanup()
    {
        try
        {
            // 取消订阅事件，避免内存泄漏
            MapSettingsViewModel.MapSettingsSaved -= OnMapSettingsSaved;
            ThemeManager.ThemeChanged -= OnThemeChanged;

            // 取消状态重置延迟任务
            _statusResetCts?.Cancel();
            _statusResetCts?.Dispose();
            _statusResetCts = null;

            _allTickets?.Clear();
            _webView = null;
            Debug.WriteLine("[MapWindowViewModel] 资源已释放");
            _logService.Info("[MapWindowViewModel] ", "资源已释放");
        }
        catch (Exception ex)
        {
            _logService.Error("[MapWindowViewModel] ", $"释放出错：{ex.Message}");
        }
    }

    /// <summary>
    ///     设置WebView引用
    /// </summary>
    public void SetWebView(WebView2 webView)
    {
        _webView = webView;
    }

    /// <summary>
    ///     地图加载完成回调
    /// </summary>
    public void OnMapReady()
    {
        IsMapReady = true;
        StatusMessage = "地图就绪，正在加载数据...";

        // 初始化当前方向过滤值
        var settingsService = new MapSettingsService();
        _currentDirectionFilter = settingsService.Config.DirectionFilter;

        // 应用主题设置
        ApplyThemeToMap();

        // 应用所有地图设置（包括底图源和样式）
        ApplyAllMapSettings();

        // 如果数据已经加载完成，发送数据到地图
        if (_isDataLoaded)
            ApplyDefaultMapDisplay();
        else
            StatusMessage = "地图就绪，等待数据加载...";
    }

    /// <summary>
    ///     应用默认地图显示设置
    /// </summary>
    private void ApplyDefaultMapDisplay()
    {
        try
        {
            var settingsService = new MapSettingsService();
            var defaultDisplay = settingsService.Config.DefaultMapDisplay;

            switch (defaultDisplay)
            {
                case "BlankMap":
                    // 不自动加载，显示空白地图
                    StatusMessage = "空白地图 - 请使用工具栏筛选行程";
                    break;

                case "AllSavedTrips":
                    // 显示所有已保存行程
                    SendAllTicketsToMap();
                    StatusMessage = $"显示所有行程 ({_allTickets.Count}条)";
                    break;

                case "ThisYearOnly":
                    // 仅当年内行程
                    var currentYear = DateTime.Now.Year;
                    var thisYearTickets = _allTickets.Where(t =>
                    {
                        if (DateTime.TryParse(t.DepartDate, out var date))
                            return date.Year == currentYear;
                        return false;
                    }).ToList();
                    SendTicketsToMap(thisYearTickets);
                    StatusMessage = $"显示当年行程 ({thisYearTickets.Count}条)";
                    break;

                case "CompletedOnly":
                    // 仅已完成行程
                    var completedTickets = _allTickets.Where(t => t.Status == "已完成").ToList();
                    SendTicketsToMap(completedTickets);
                    StatusMessage = $"显示已完成行程 ({completedTickets.Count}条)";
                    break;

                case "PendingOnly":
                    // 仅未出行待乘行程
                    var pendingTickets = _allTickets.Where(t => t.Status == "未出行").ToList();
                    SendTicketsToMap(pendingTickets);
                    StatusMessage = $"显示未出行行程 ({pendingTickets.Count}条)";
                    break;

                default:
                    // 默认显示所有
                    SendAllTicketsToMap();
                    StatusMessage = $"显示所有行程 ({_allTickets.Count}条)";
                    break;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"应用默认显示设置失败：{ex.Message}";
            // 出错时默认显示所有
            SendAllTicketsToMap();
        }
    }

    /// <summary>
    ///     应用主题设置到地图
    /// </summary>
    private async void ApplyThemeToMap()
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            // 获取当前主题设置
            var generalConfig = new GeneralSettingsService().Config;
            var isDarkMode = generalConfig.ThemeMode == ThemeMode.Dark ||
                             (generalConfig.ThemeMode == ThemeMode.System && ThemeManager.IsSystemDarkMode());

            // 获取字体大小
            double fontSize = generalConfig.FontSize switch
            {
                FontSizeOption.Small => 12,
                FontSizeOption.Medium => 14,
                FontSizeOption.Large => 16,
                FontSizeOption.ExtraLarge => 18,
                _ => 14
            };

            // 应用DPI缩放
            var uiConfig = new UISettingsService().Config;
            var dpiScale = uiConfig.DpiScaling switch
            {
                "100" => 1.0,
                "125" => 1.25,
                "150" => 1.5,
                _ => 1.0
            };
            fontSize *= dpiScale;

            // 发送主题设置到地图
            var themeSettings = new
            {
                isDarkMode, fontSize
            };

            var script = $"setTheme({JsonSerializer.Serialize(themeSettings)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            StatusMessage = $"应用主题失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     应用地图底图源设置
    /// </summary>
    private async void ApplyMapTileSource()
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            // 从配置中读取地图源（重新加载配置以确保获取最新设置）
            var settingsService = new MapSettingsService();
            settingsService.RefreshConfig();
            var mapSource = settingsService.Config.MapTileSource;

            StatusMessage = $"正在应用地图源：{mapSource}";

            var script = $"setMapSource('{mapSource}');";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            StatusMessage = $"地图源已应用：{mapSource}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"应用地图源失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     应用地图样式设置（颜色、线宽、标记大小等）
    /// </summary>
    private async void ApplyMapStyles()
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            // 从配置中读取地图样式设置
            var settingsService = new MapSettingsService();
            settingsService.RefreshConfig();
            var config = settingsService.Config;

            StatusMessage = "正在应用地图样式设置...";

            var styleSettings = new
            {
                colors = new
                {
                    completed = config.CompletedTripColor,
                    pending = config.PendingTripColor,
                    rescheduled = config.RescheduledTripColor,
                    refunded = config.RefundedTripColor,
                    selected = config.SelectedTripColor,
                    station = config.StationMarkerColor
                },
                lineWidth = new
                {
                    completed = config.CompletedLineWidth,
                    pending = config.PendingLineWidth,
                    rescheduled = config.RescheduledLineWidth,
                    refunded = config.RefundedLineWidth,
                    selected = config.SelectedLineWidth
                },
                markerSize = config.MarkerSize,
                showStationLabels = config.ShowStationLabels,
                showDateLabels = config.ShowDateLabels,
                highlightSelectedTrip = config.HighlightSelectedTrip,
                showHoverCard = config.ShowHoverCard,
                directionFilter = config.DirectionFilter
            };

            var script = $"setMapStyles({JsonSerializer.Serialize(styleSettings)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            StatusMessage = "地图样式设置已应用";
        }
        catch (Exception ex)
        {
            StatusMessage = $"应用地图样式失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     应用地图交互设置
    /// </summary>
    private async void ApplyMapInteractions()
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            // 从配置中读取地图交互设置
            var settingsService = new MapSettingsService();
            settingsService.RefreshConfig();
            var config = settingsService.Config;

            StatusMessage = "正在应用地图交互设置...";

            var interactionSettings = new
            {
                enableMouseWheelZoom = config.EnableMouseWheelZoom,
                enableLeftDragPan = config.EnableLeftDragPan,
                enableRightClickReset = config.EnableRightClickReset,
                zoomSensitivity = config.ZoomSensitivity,
                enablePanInertia = config.EnablePanInertia,
                doubleClickTripAction = config.DoubleClickTripAction,
                doubleClickStationAction = config.DoubleClickStationAction,
                doubleClickBlankAction = config.DoubleClickBlankAction
            };

            var script = $"setMapInteractions({JsonSerializer.Serialize(interactionSettings)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            StatusMessage = "地图交互设置已应用";
        }
        catch (Exception ex)
        {
            StatusMessage = $"应用地图交互设置失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     应用所有地图设置（包括底图源、样式和交互）
    /// </summary>
    private void ApplyAllMapSettings()
    {
        ApplyMapTileSource();
        ApplyMapStyles();
        ApplyMapInteractions();
    }

    /// <summary>
    ///     车站点击事件
    /// </summary>
    public void OnStationClick(string? stationName)
    {
        if (string.IsNullOrEmpty(stationName)) return;

        StatusMessage = $"选中车站：{stationName}";

        // 筛选该车站的车票
        var stationTickets = _allTickets.Where(t =>
            t.DepartStation == stationName || t.ArriveStation == stationName).ToList();

        // 高亮显示相关行程
        HighlightTrips(stationTickets.Select(t => t.Id).ToList());
    }

    /// <summary>
    ///     行程点击事件
    /// </summary>
    public void OnTripClick(string? tripId)
    {
        if (string.IsNullOrEmpty(tripId)) return;

        SelectTripById(tripId);
    }

    /// <summary>
    ///     根据行程ID选中行程（公共方法，供外部调用）
    /// </summary>
    /// <param name="tripId">行程ID</param>
    public void SelectTripById(string? tripId)
    {
        if (string.IsNullOrEmpty(tripId)) return;

        var ticket = _allTickets.FirstOrDefault(t => t.Id == tripId);
        if (ticket != null)
        {
            // 取消之前的延迟重置任务
            _statusResetCts?.Cancel();
            _statusResetCts = new CancellationTokenSource();
            var token = _statusResetCts.Token;
            
            StatusMessage = $"选中行程：{ticket.DepartStation} → {ticket.ArriveStation} ({ticket.TrainNo})";
            // 调用前端的 selectTrip 函数，高亮+信息卡片，不调整视野（避免单击时缩放）
            SelectTripOnMap(tripId, false);
            
            // 延迟1秒后变为就绪
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000, token);
                    if (!token.IsCancellationRequested)
                    {
                        StatusMessage = "就绪";
                    }
                }
                catch (TaskCanceledException)
                {
                    // 任务被取消，忽略异常
                }
            }, token);
        }
    }

    /// <summary>
    ///     在地图上选中行程（高亮+信息卡片），可选是否调整视野
    /// </summary>
    private void SelectTripOnMap(string tripId, bool fitView = true)
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            var data = new
            {
                type = "selectTrip",
                tripId,
                fitView
            };

            var json = JsonSerializer.Serialize(data);
            var script = $"window.receiveData({json});";
            _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            StatusMessage = $"选中行程失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     清除选中状态
    /// </summary>
    public void ClearSelection()
    {
        // 重置所有线路样式
        ResetTripStyles();
        StatusMessage = "就绪";
    }

    /// <summary>
    ///     打开车票编辑
    /// </summary>
    public void OpenTicketEdit(string? tripId)
    {
        if (string.IsNullOrEmpty(tripId)) return;

        var ticket = _allTickets.FirstOrDefault(t => t.Id == tripId);
        if (ticket == null) return;

        try
        {
            StatusMessage = $"打开车票编辑：{ticket.TrainNo} ({ticket.DepartStation} → {ticket.ArriveStation})";
            
            // 使用 DatabaseId 直接打开编辑窗口
            if (ticket.DatabaseId <= 0)
            {
                MessageBoxWindow.Show("未找到对应的车票记录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 打开编辑窗口（单例模式，同一车票只能打开一个）
            var editWindow = EditTrainTicketWindow.GetInstance(ticket.DatabaseId);
            editWindow.Owner = GetMapWindow();
            editWindow.ShowDialog();
            
            StatusMessage = "车票编辑窗口已关闭";
            
            // 刷新地图数据
            _ = RefreshDataAsync();
        }
        catch (Exception ex)
        {
            _logService.Error("MapWindowViewModel", $"打开编辑窗口失败: {ex.Message}");
            MessageBoxWindow.Show($"打开编辑窗口失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    ///     异步刷新数据（用于编辑后刷新）
    /// </summary>
    private async Task RefreshDataAsync()
    {
        await LoadTicketDataAsync();
        SendAllTicketsToMap();
    }

    /// <summary>
    ///     显示车票详情
    /// </summary>
    public void ShowTicketDetails(string? tripId)
    {
        if (string.IsNullOrEmpty(tripId)) return;

        var ticket = _allTickets.FirstOrDefault(t => t.Id == tripId);
        if (ticket != null)
        {
            StatusMessage = $"显示车票详情：{ticket.TrainNo} ({ticket.DepartStation} → {ticket.ArriveStation})";
            // TODO: 打开车票详情窗口
            // 暂时使用消息框显示详情
            MessageBoxWindow.Show(
                GetMapWindow(),
                $"车票详情：\n\n" +
                $"车次：{ticket.TrainNo}\n" +
                $"出发站：{ticket.DepartStation}\n" +
                $"到达站：{ticket.ArriveStation}\n" +
                $"出发日期：{ticket.DepartDate}\n" +
                $"出发时间：{ticket.DepartTime}\n" +
                $"座位类型：{ticket.SeatType}\n" +
                $"票价：¥{ticket.Price:F2}\n" +
                $"状态：{ticket.Status}",
                "车票详情");
        }
    }

    /// <summary>
    ///     显示车站车票列表
    /// </summary>
    public void ShowStationTickets(string? stationName)
    {
        if (string.IsNullOrEmpty(stationName)) return;

        StatusMessage = $"显示车站车票列表：{stationName}";
        // 筛选该车站的车票
        var stationTickets = _allTickets.Where(t =>
            t.DepartStation == stationName || t.ArriveStation == stationName).ToList();

        // TODO: 打开车站车票列表窗口 - 可以复用现有的 QueryTrainTicketView 或其他查询窗口
        // 暂时使用消息框显示统计信息
        var departCount = stationTickets.Count(t => t.DepartStation == stationName);
        var arriveCount = stationTickets.Count(t => t.ArriveStation == stationName);

        MessageBoxWindow.Show(
            GetMapWindow(),
            $"车站：{stationName}\n\n" +
            $"出发行程：{departCount} 条\n" +
            $"到达行程：{arriveCount} 条\n" +
            $"总计：{stationTickets.Count} 条",
            "车站车票统计");
    }

    /// <summary>
    ///     车票票面预览
    /// </summary>
    public void PreviewTicket(string? tripId)
    {
        if (string.IsNullOrEmpty(tripId)) return;

        var ticket = _allTickets.FirstOrDefault(t => t.Id == tripId);
        if (ticket != null)
        {
            StatusMessage = $"车票票面预览：{ticket.TrainNo} ({ticket.DepartStation} → {ticket.ArriveStation})";
            // TODO: 打开车票票面预览窗口 - 可以复用现有的 TicketPreviewWindow
            MessageBoxWindow.Show(
                GetMapWindow(),
                $"车票预览：\n\n" +
                $"车次：{ticket.TrainNo}\n" +
                $"{ticket.DepartStation} → {ticket.ArriveStation}\n" +
                $"{ticket.DepartDate} {ticket.DepartTime}\n" +
                $"{ticket.SeatType} ¥{ticket.Price:F2}",
                "车票预览");
        }
    }

    /// <summary>
    ///     获取地图窗口实例
    /// </summary>
    private Window? GetMapWindow()
    {
        return Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this);
    }

    /// <summary>
    ///     显示车站统计
    /// </summary>
    public void ShowStationStats(string? stationName)
    {
        if (string.IsNullOrEmpty(stationName)) return;

        StatusMessage = $"显示车站统计：{stationName}";
        var stationTickets = _allTickets.Where(t =>
            t.DepartStation == stationName || t.ArriveStation == stationName).ToList();

        var departCount = stationTickets.Count(t => t.DepartStation == stationName);
        var arriveCount = stationTickets.Count(t => t.ArriveStation == stationName);
        var totalSpent = stationTickets.Where(t => t.DepartStation == stationName).Sum(t => t.Price);

        // TODO: 打开车站统计窗口
        MessageBoxWindow.Show(
            GetMapWindow(),
            $"车站统计：{stationName}\n\n" +
            $"出发次数：{departCount} 次\n" +
            $"到达次数：{arriveCount} 次\n" +
            $"经过总计：{stationTickets.Count} 次\n" +
            $"出发总花费：¥{totalSpent:F2}",
            "车站统计");
    }

    /// <summary>
    ///     从车站创建车票
    /// </summary>
    public void CreateTicketFromStation(string? stationName)
    {
        if (string.IsNullOrEmpty(stationName)) return;

        StatusMessage = $"新建以 {stationName} 为出发站的车票";
        // TODO: 打开添加车票窗口，并预填出发站
        MessageBoxWindow.Show(
            GetMapWindow(),
            $"新建车票\n\n出发站：{stationName}\n\n（将打开车票录入窗口）",
            "新建车票");
    }

    /// <summary>
    ///     创建新车票
    /// </summary>
    public void CreateNewTicket()
    {
        StatusMessage = "新建车票录入";
        // TODO: 打开添加车票窗口
        MessageBoxWindow.Show(
            GetMapWindow(),
            "将打开车票录入窗口",
            "新建车票");
    }

    /// <summary>
    ///     重置所有行程样式
    /// </summary>
    private async void ResetTripStyles()
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            var script = @"
                    tripLayers.forEach(item => {
                        const color = getTripColor(item.ticket.status);
                        const weight = getTripWeight(item.ticket.status);
                        item.layer.setStyle({
                            color: color,
                            weight: weight,
                            opacity: 0.8
                        });
                    });
                ";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            StatusMessage = $"重置样式失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     刷新数据命令
    /// </summary>
    [RelayCommand]
    private async Task RefreshData()
    {
        await LoadTicketDataAsync();
        SendAllTicketsToMap();
        StatusMessage = "数据已刷新";
    }

    /// <summary>
    ///     显示全部命令
    /// </summary>
    [RelayCommand]
    private void ShowAll()
    {
        SendAllTicketsToMap();
        StatusMessage = "显示全部行程";
    }

    /// <summary>
    ///     筛选已完成行程
    /// </summary>
    [RelayCommand]
    private void FilterCompleted()
    {
        var completedTickets = _allTickets.Where(t => t.Status == "已完成").ToList();
        SendTicketsToMap(completedTickets);
        StatusMessage = $"显示已完成行程 ({completedTickets.Count}条)";
    }

    /// <summary>
    ///     筛选未出行行程
    /// </summary>
    [RelayCommand]
    private void FilterPending()
    {
        var pendingTickets = _allTickets.Where(t => t.Status == "未出行").ToList();
        SendTicketsToMap(pendingTickets);
        StatusMessage = $"显示未出行行程 ({pendingTickets.Count}条)";
    }

    /// <summary>
    ///     筛选已改签行程
    /// </summary>
    [RelayCommand]
    private void FilterRescheduled()
    {
        var rescheduledTickets = _allTickets.Where(t => t.Status == "已改签").ToList();
        SendTicketsToMap(rescheduledTickets);
        StatusMessage = $"显示已改签行程 ({rescheduledTickets.Count}条)";
    }

    /// <summary>
    ///     筛选已退票行程
    /// </summary>
    [RelayCommand]
    private void FilterRefunded()
    {
        var refundedTickets = _allTickets.Where(t => t.Status == "已退票").ToList();
        SendTicketsToMap(refundedTickets);
        StatusMessage = $"显示已退票行程 ({refundedTickets.Count}条)";
    }

    /// <summary>
    ///     打开设置命令
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        // 获取当前地图窗口
        var mapWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this);

        // 检查是否已有设置窗口打开
        var existingSettingsWindow = Application.Current.Windows
            .OfType<SettingsWindow>()
            .FirstOrDefault();
        
        if (existingSettingsWindow != null)
        {
            // 如果已有设置窗口，将其激活并前置
            existingSettingsWindow.Activate();
            existingSettingsWindow.Focus();
            return;
        }

        var settingsWindow = new SettingsWindow(SettingsPageType.Map);
        
        // 设置 Owner 为地图窗口，保持窗口关联
        // 但使用 ShowDialog 模态显示，符合对话框行为
        if (mapWindow != null)
        {
            settingsWindow.Owner = mapWindow;
        }

        // 使用 ShowDialog 模态方式显示，统一行为
        settingsWindow.ShowDialog();

        // 窗口关闭后应用设置
        ApplyAllMapSettings();
    }

    /// <summary>
    ///     异步加载车票数据（从数据库）
    /// </summary>
    private async Task LoadTicketDataAsync()
    {
        try
        {
            StatusMessage = "正在从数据库加载数据...";

            // 从数据库加载地图数据
            var mapDataResult = await _mapDataService.LoadMapDataAsync();

            // 转换数据模型
            _allTickets = mapDataResult.ValidTickets.Select(t => new TicketData
            {
                Id = t.Id,
                DatabaseId = int.TryParse(t.Id, out var dbId) ? dbId : 0,
                TrainNo = t.TrainNo,
                DepartStation = t.DepartStation,
                ArriveStation = t.ArriveStation,
                DepartDate = t.DepartDate,
                DepartTime = t.DepartTime,
                ArriveTime = t.ArriveTime,
                Status = t.Status,
                SeatType = t.SeatType,
                Price = (decimal)t.Price,
                DepartLat = t.DepartLat,
                DepartLng = t.DepartLng,
                ArriveLat = t.ArriveLat,
                ArriveLng = t.ArriveLng
            }).ToList();

            // 保存缺少经纬度的车站列表
            _stationsWithoutCoordinates = mapDataResult.StationsWithoutCoordinates;

            // 如果有缺少经纬度的车站，显示提示
            if (_stationsWithoutCoordinates.Count > 0)
            {
                var stationList = string.Join("、", _stationsWithoutCoordinates.Take(5));
                var moreCount = _stationsWithoutCoordinates.Count - 5;
                var moreText = moreCount > 0 ? $" 等共 {_stationsWithoutCoordinates.Count} 个车站" : "";
                StatusMessage = $"已加载 {_allTickets.Count} 条行程数据。以下车站缺少位置信息无法显示：{stationList}{moreText}";
            }
            else
            {
                StatusMessage = $"已加载 {_allTickets.Count} 条行程数据";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"从数据库加载数据失败：{ex.Message}，使用默认数据";
            Debug.WriteLine($"[MapWindowViewModel] 加载数据失败: {ex}");
            _allTickets = GetDefaultTickets();
        }
    }

    /// <summary>
    ///     加载车票数据（同步版本，用于兼容旧代码）
    /// </summary>
    private void LoadTicketData()
    {
        // 同步等待异步方法完成
        try
        {
            var task = LoadTicketDataAsync();
            task.Wait();
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载数据失败：{ex.Message}";
            _allTickets = GetDefaultTickets();
        }
    }

    /// <summary>
    ///     发送所有车票到地图
    /// </summary>
    private void SendAllTicketsToMap()
    {
        SendTicketsToMap(_allTickets);
    }

    /// <summary>
    ///     发送车票数据到地图
    /// </summary>
    private void SendTicketsToMap(List<TicketData> tickets)
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            var data = new
            {
                type = "loadTickets",
                tickets = tickets.Select(t => new
                {
                    id = t.Id,
                    trainNo = t.TrainNo,
                    departStation = t.DepartStation,
                    arriveStation = t.ArriveStation,
                    departDate = t.DepartDate,
                    departTime = t.DepartTime,
                    arriveTime = t.ArriveTime,
                    status = t.Status,
                    seatType = t.SeatType,
                    price = t.Price,
                    departLat = t.DepartLat,
                    departLng = t.DepartLng,
                    arriveLat = t.ArriveLat,
                    arriveLng = t.ArriveLng
                }).ToList(),
                missingStations = _stationsWithoutCoordinates
            };

            var json = JsonSerializer.Serialize(data);
            var script = $"window.receiveData({json});";
            _webView.CoreWebView2.ExecuteScriptAsync(script);

            StatusMessage = $"已加载 {tickets.Count} 条行程数据";
        }
        catch (Exception ex)
        {
            StatusMessage = $"发送数据失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     高亮显示指定行程
    /// </summary>
    private void HighlightTrips(List<string> tripIds, bool fitView = true)
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            var data = new
            {
                type = "highlightTrips", 
                tripIds,
                fitView
            };

            var json = JsonSerializer.Serialize(data);
            var script = $"window.receiveData({json});";
            _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            StatusMessage = $"高亮失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     在地图上显示行程信息卡片
    /// </summary>
    private void ShowTripInfoCard(string tripId)
    {
        if (_webView?.CoreWebView2 == null || !IsMapReady) return;

        try
        {
            var data = new
            {
                type = "showTripInfoCard",
                tripId
            };

            var json = JsonSerializer.Serialize(data);
            var script = $"window.receiveData({json});";
            _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            StatusMessage = $"显示信息卡片失败：{ex.Message}";
        }
    }

    /// <summary>
    ///     获取mock数据路径
    /// </summary>
    private string GetMockDataPath()
    {
        var exePath = Assembly.GetExecutingAssembly().Location;
        var exeDir = Path.GetDirectoryName(exePath)!;
        var dataPath = Path.Combine(exeDir, "Resources", "Map", "mock_tickets.json");

        if (File.Exists(dataPath)) return dataPath;

        // 开发环境路径
        var projectDir = Path.GetFullPath(Path.Combine(exeDir, "..", "..", ".."));
        return Path.Combine(projectDir, "Resources", "Map", "mock_tickets.json");
    }

    /// <summary>
    ///     获取默认车票数据
    /// </summary>
    private List<TicketData> GetDefaultTickets()
    {
        return new List<TicketData>
        {
            new()
            {
                Id = "1",
                TrainNo = "G1234",
                DepartStation = "北京南",
                ArriveStation = "上海虹桥",
                DepartDate = "2024-01-15",
                DepartTime = "08:00",
                ArriveTime = "12:28",
                Status = "已完成",
                SeatType = "二等座",
                Price = 553,
                DepartLat = 39.8652,
                DepartLng = 116.3785,
                ArriveLat = 31.1979,
                ArriveLng = 121.3356
            },
            new()
            {
                Id = "2",
                TrainNo = "G5678",
                DepartStation = "上海虹桥",
                ArriveStation = "杭州东",
                DepartDate = "2024-02-20",
                DepartTime = "14:30",
                ArriveTime = "15:30",
                Status = "已完成",
                SeatType = "一等座",
                Price = 117,
                DepartLat = 31.1979,
                DepartLng = 121.3356,
                ArriveLat = 30.2936,
                ArriveLng = 120.2092
            },
            new()
            {
                Id = "3",
                TrainNo = "G9012",
                DepartStation = "杭州东",
                ArriveStation = "南京南",
                DepartDate = "2024-12-25",
                DepartTime = "09:00",
                ArriveTime = "10:30",
                Status = "未出行",
                SeatType = "二等座",
                Price = 149,
                DepartLat = 30.2936,
                DepartLng = 120.2092,
                ArriveLat = 31.9728,
                ArriveLng = 118.8047
            }
        };
    }
}

/// <summary>
///     车票数据模型
/// </summary>
public class TicketData
{
    public string Id { get; set; } = string.Empty;
    public int DatabaseId { get; set; }
    public string TrainNo { get; set; } = string.Empty;
    public string DepartStation { get; set; } = string.Empty;
    public string ArriveStation { get; set; } = string.Empty;
    public string DepartDate { get; set; } = string.Empty;
    public string DepartTime { get; set; } = string.Empty;
    public string ArriveTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SeatType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double DepartLat { get; set; }
    public double DepartLng { get; set; }
    public double ArriveLat { get; set; }
    public double ArriveLng { get; set; }
}