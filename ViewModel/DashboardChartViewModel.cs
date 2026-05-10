using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using GuiPiao.Model;
using GuiPiao.Services;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GuiPiao.ViewModel;

/// <summary>
///     仪表盘图表视图模型 - 单个卡片的图表数据绑定
/// </summary>
public partial class DashboardChartViewModel : ObservableObject, IDisposable
{
    private readonly DashboardSettingsService _dashboardSettingsService;
    private readonly IChartDataService _dataService;
    private readonly LogService _logService = null!;
    private readonly List<IDisposable> _skiaResources = new();

    // 主题变化节流器，避免频繁刷新图表
    private readonly SemaphoreSlim _themeChangeThrottle = new(1, 1);

    [ObservableProperty] private DashboardCard _card;

    [ObservableProperty] private ChartData? _chartData;

    private bool _isDisposed;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsCartesianChart))]
    private bool _isPieChart;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsCartesianChart))]
    private bool _isTextList;

    [ObservableProperty] private ISeries[]? _series;

    [ObservableProperty] private bool _showDataLabels = true;

    [ObservableProperty] private bool _showPercentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TooltipPosition))]
    [NotifyPropertyChangedFor(nameof(PieTooltipPosition))]
    private bool _showTooltip = true;

    [ObservableProperty] private string? _title;

    /// <summary>
    ///     Tooltip 背景画笔，用于设置背景颜色
    /// </summary>
    [ObservableProperty] private SolidColorPaint? _tooltipBackgroundPaint;

    /// <summary>
    ///     Tooltip 文本画笔，用于设置字体颜色
    /// </summary>
    [ObservableProperty] private SolidColorPaint? _tooltipTextPaint;

    /// <summary>
    ///     Tooltip 字体大小
    /// </summary>
    [ObservableProperty] private float _tooltipTextSize;

    [ObservableProperty] private Axis[]? _xAxes;

    [ObservableProperty] private Axis[]? _yAxes;

    public DashboardChartViewModel(DashboardCard card, IChartDataService dataService)
    {
        _card = card;
        _dataService = dataService;
        _dashboardSettingsService = new DashboardSettingsService();
        _title = card.Name;
        _logService = ServiceManager.Instance.LogService;

        // 初始化 Tooltip 文本画笔和字体大小
        UpdateTooltipTextPaint();
        TooltipTextSize = GetApplicationFontSize();

        // 监听主题变化事件
        ThemeManager.ThemeChanged += OnThemeChanged;

        // 监听数据刷新事件
        _dataService.DataRefreshed += OnDataRefreshed;
    }

    public bool IsCartesianChart => !IsPieChart && !IsTextList;

    public TooltipPosition TooltipPosition => ShowTooltip ? TooltipPosition.Top : TooltipPosition.Hidden;

    public TooltipPosition PieTooltipPosition => ShowTooltip ? TooltipPosition.Center : TooltipPosition.Hidden;

    public bool HasData => ChartData != null && ChartData.Values.Length > 0;

    /// <summary>
    ///     图表动画速度（供 XAML 绑定，控制 Chart 级别的动画）
    /// </summary>
    public TimeSpan AnimationSpeed
    {
        get
        {
            _dashboardSettingsService.RefreshConfig();
            return _dashboardSettingsService.Config.EnableChartAnimation
                ? TimeSpan.FromMilliseconds(800)
                : TimeSpan.Zero;
        }
    }

    /// <summary>
    ///     图表动画缓动函数（设为 null 可完全禁用动画）
    /// </summary>
    public Func<float, float>? AnimationEasingFunction
    {
        get
        {
            _dashboardSettingsService.RefreshConfig();
            // 禁用动画时返回 null，启用动画时返回默认缓动函数
            return _dashboardSettingsService.Config.EnableChartAnimation
                ? EasingFunctions.EaseOut
                : null;
        }
    }

    /// <summary>
    ///     释放资源，取消事件订阅
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        ThemeManager.ThemeChanged -= OnThemeChanged;
        _dataService.DataRefreshed -= OnDataRefreshed;

        // 清除图表数据引用，帮助 GC 回收
        Series = null;
        XAxes = null;
        YAxes = null;
        ChartData = null;

        DisposeSkiaResources();
    }

    /// <summary>
    ///     数据刷新时重新加载图表数据
    /// </summary>
    private async void OnDataRefreshed(object? sender, EventArgs e)
    {
        Debug.WriteLine($"[DashboardChartViewModel] 收到数据刷新事件，重新加载: {Title}");
        await LoadDataAsync();
    }

    /// <summary>
    ///     主题变化时刷新图表
    /// </summary>
    private async void OnThemeChanged(object? sender, EventArgs e)
    {
        // 使用节流避免频繁刷新
        await _themeChangeThrottle.WaitAsync();
        try
        {
            // 更新 Tooltip 字体大小（这会触发属性变更通知）
            TooltipTextSize = GetApplicationFontSize();

            // 更新 Tooltip 文本画笔
            UpdateTooltipTextPaint();

            // 重新生成图表系列以应用新的主题颜色
            if (ChartData != null)
            {
                var config = BuildConfigFromCard(Card);
                
                // 1. 先清空图表，让 LiveCharts 完成清理
                Series = null;
                XAxes = null;
                YAxes = null;
                OnPropertyChanged(nameof(Series));
                OnPropertyChanged(nameof(XAxes));
                OnPropertyChanged(nameof(YAxes));
                
                // 2. 释放旧资源
                DisposeSkiaResources();

                // 3. 延迟以确保 LiveCharts 完成清理
                await Task.Delay(100);

                // 4. 生成新资源
                GenerateSeries(config.ChartType);
                GenerateAxes(config.ChartType);

                // 5. 触发属性变更通知，刷新 UI
                OnPropertyChanged(nameof(Series));
                OnPropertyChanged(nameof(XAxes));
                OnPropertyChanged(nameof(YAxes));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardChartViewModel] 主题变化刷新图表失败: {ex.Message}");
            _logService?.Error("DashboardChartViewModel", $"主题变化刷新图表失败: {ex.Message}");
        }
        finally
        {
            _themeChangeThrottle.Release();
        }
    }

    /// <summary>
    ///     更新 Tooltip 画笔（文本颜色和背景颜色）
    /// </summary>
    private void UpdateTooltipTextPaint()
    {
        var textColor = GetThemeTextColor();
        var backgroundColor = GetTooltipBackgroundColor();
        TooltipTextPaint = new SolidColorPaint(textColor);
        TooltipBackgroundPaint = new SolidColorPaint(backgroundColor);
    }

    /// <summary>
    ///     获取 Tooltip 背景颜色（根据主题）
    /// </summary>
    private SKColor GetTooltipBackgroundColor()
    {
        var isDarkTheme = IsDarkTheme();
        // 深色主题使用深灰色背景，浅色主题使用浅灰色背景
        return isDarkTheme ? SKColor.Parse("#424242") : SKColor.Parse("#F5F5F5");
    }

    /// <summary>
    ///     加载图表数据
    /// </summary>
    public async Task LoadDataAsync()
    {
        try
        {
            Debug.WriteLine($"[DashboardChartViewModel] 开始加载数据: {Card.Name}, 类型: {Card.StatisticType}");

            // 构建配置：以 CustomConfig 为基础（统计维度+显示样式），时间范围和数据过滤根据 UseGlobalConfig 决定
            var config = BuildConfigFromCard(Card);

            // 读取显示配置
            ShowDataLabels = config.ShowValue;
            ShowPercentage = config.ShowPercentage;
            ShowTooltip = config.ShowTooltip;

            Debug.WriteLine(
                $"[DashboardChartViewModel] 配置: ChartType={config.ChartType}, TimeRange={config.TimeRange}, ClassificationBasis={config.ClassificationBasis}, StatisticIndicator={config.StatisticIndicator}, ShowValue={ShowDataLabels}, ShowPercentage={ShowPercentage}, ShowTooltip={ShowTooltip}");
            _logService.Info("DashboardChartViewModel",
                $"配置: 图表类型 ={config.ChartType}, 时间范围 ={config.TimeRange}, 分类 ={config.ClassificationBasis}, 统计指标 ={config.StatisticIndicator}, 显示数值标签 ={ShowDataLabels}, 显示百分比 ={ShowPercentage}, 显示提示 ={ShowTooltip}");

            // 根据统计类型获取数据
            ChartData = Card.StatisticType switch
            {
                StatisticType.MonthlyTripStats => await _dataService.GetMonthlyTripStatsAsync(config),
                StatisticType.TrainTypeRatio => await _dataService.GetTrainTypeRatioAsync(config),
                StatisticType.StationTopRanking => await _dataService.GetStationTopRankingAsync(config),
                StatisticType.SeatTypeRatio => await _dataService.GetSeatTypeRatioAsync(config),
                StatisticType.AnnualTripSummary => await _dataService.GetAnnualTripSummaryAsync(config),
                StatisticType.TripTimeDistribution => await _dataService.GetTripTimeDistributionAsync(config),
                StatisticType.PopularRouteStats => await _dataService.GetPopularRouteStatsAsync(config),
                StatisticType.TripCostAnalysis => await _dataService.GetTripCostAnalysisAsync(config),
                _ => null
            };

            Debug.WriteLine(
                $"[DashboardChartViewModel] ChartData: {(ChartData != null ? $"Labels={ChartData.Labels?.Length}, Values={ChartData.Values?.Length}" : "null")}");

            // 根据图表类型生成 Series
            if (ChartData != null)
            {
                // 1. 先清空图表，让 LiveCharts 完成清理
                Series = null;
                XAxes = null;
                YAxes = null;
                OnPropertyChanged(nameof(Series));
                OnPropertyChanged(nameof(XAxes));
                OnPropertyChanged(nameof(YAxes));
                
                // 2. 释放旧资源
                DisposeSkiaResources();
                
                // 3. 延迟以确保 LiveCharts 完成清理
                await Task.Delay(50);
                
                // 4. 生成新资源
                GenerateSeries(config.ChartType);
                GenerateAxes(config.ChartType);
                
                Debug.WriteLine($"[DashboardChartViewModel] Series 生成完成: {Series?.Length ?? 0}");
                OnPropertyChanged(nameof(HasData));
            }
            else
            {
                Debug.WriteLine("[DashboardChartViewModel] ChartData 为 null，无法生成 Series");
                DisposeSkiaResources();
                Series = Array.Empty<ISeries>();
                XAxes = null;
                YAxes = null;
                OnPropertyChanged(nameof(HasData));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DashboardChartViewModel] 加载图表数据失败: {ex.Message}");
            _logService?.Error("DashboardChartViewModel", $"加载图表数据失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     根据图表类型生成 Series
    /// </summary>
    private void GenerateSeries(ChartType chartType)
    {
        if (ChartData == null) return;

        IsPieChart = chartType == ChartType.PieChart;
        IsTextList = chartType == ChartType.TextList;

        Series = chartType switch
        {
            ChartType.PieChart => GeneratePieSeries(),
            ChartType.BarChart => GenerateBarSeries(),
            ChartType.HorizontalBarChart => GenerateHorizontalBarSeries(),
            ChartType.LineChart => GenerateLineSeries(),
            ChartType.TextList => null,
            _ => GenerateBarSeries()
        };
    }

    private ISeries[] GeneratePieSeries()
    {
        if (ChartData == null || ChartData.Values.Length == 0 || ChartData.Labels.Length == 0)
            return Array.Empty<ISeries>();

        var colors = new[]
        {
            SKColor.Parse("#2E7D32"),
            SKColor.Parse("#1976D2"),
            SKColor.Parse("#9C27B0"),
            SKColor.Parse("#F57C00"),
            SKColor.Parse("#E53935"),
            SKColor.Parse("#00ACC1"),
            SKColor.Parse("#689F38"),
            SKColor.Parse("#5E35B1")
        };

        var length = Math.Min(ChartData.Values.Length, ChartData.Labels.Length);
        var pieSeriesList = new List<ISeries>(length);
        var total = ChartData.Values.Sum();
        var dataLabelColor = GetThemeTextColor();
        var baseFontSize = GetApplicationFontSize();

        // 根据数据量调整数据标签字体大小，避免重叠
        var dataLabelFontSize = CalculateDataLabelFontSize(baseFontSize, length);

        // 共享的 DataLabelsPaint，减少对象创建
        var sharedDataLabelsPaint = ShowDataLabels || ShowPercentage ? new SolidColorPaint(dataLabelColor) : null;
        if (sharedDataLabelsPaint != null)
            RegisterSkiaResource(sharedDataLabelsPaint);

        for (var i = 0; i < length; i++)
        {
            var color = colors[i % colors.Length];
            var value = ChartData.Values[i];
            var label = ChartData.Labels[i];

            if (value <= 0) continue;

            var dataLabel = "";
            if (ShowPercentage && ShowDataLabels)
            {
                var percentage = total > 0 ? (value / total * 100).ToString("F1") : "0";
                dataLabel = $"{FormatValue(value)} ({percentage}%)";
            }
            else if (ShowPercentage)
            {
                var percentage = total > 0 ? (value / total * 100).ToString("F1") : "0";
                dataLabel = $"{percentage}%";
            }
            else if (ShowDataLabels)
            {
                dataLabel = FormatValue(value);
            }

            var fillPaint = new SolidColorPaint(color);
            RegisterSkiaResource(fillPaint);

            pieSeriesList.Add(new PieSeries<double>
            {
                Values = new List<double> { value },
                Name = label,
                Fill = fillPaint,
                DataLabelsPaint = sharedDataLabelsPaint,
                DataLabelsPosition = PolarLabelsPosition.Middle,
                DataLabelsSize = dataLabelFontSize,
                DataLabelsFormatter = point => dataLabel,
                InnerRadius = 0,
                IsHoverable = ShowTooltip
            });
        }

        return pieSeriesList.ToArray();
    }

    private ISeries[] GenerateBarSeries()
    {
        if (ChartData == null || ChartData.Values.Length == 0)
            return Array.Empty<ISeries>();

        var config = BuildConfigFromCard(Card);
        var color = SKColor.Parse(config.ChartColor ?? "#2E7D32");
        var dataLabelColor = GetThemeTextColor();
        var baseFontSize = GetApplicationFontSize();

        // 根据数据量调整数据标签字体大小，避免重叠
        var dataLabelFontSize = CalculateDataLabelFontSize(baseFontSize, ChartData.Values.Length);

        var fillPaint = new SolidColorPaint(color);
        RegisterSkiaResource(fillPaint);

        SolidColorPaint? dataLabelsPaint = null;
        if (ShowDataLabels)
        {
            dataLabelsPaint = new SolidColorPaint(dataLabelColor);
            RegisterSkiaResource(dataLabelsPaint);
        }

        return new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = ChartData.Values,
                Name = ChartData.SeriesName,
                Fill = fillPaint,
                DataLabelsPaint = dataLabelsPaint,
                DataLabelsPosition = DataLabelsPosition.Middle,
                DataLabelsSize = dataLabelFontSize,
                DataLabelsFormatter = point => FormatValue(point.Coordinate.PrimaryValue),
                MaxBarWidth = 40,
                IsHoverable = ShowTooltip
            }
        };
    }

    private ISeries[] GenerateHorizontalBarSeries()
    {
        if (ChartData == null || ChartData.Values.Length == 0 || ChartData.Labels.Length == 0)
            return Array.Empty<ISeries>();

        var config = BuildConfigFromCard(Card);
        var color = SKColor.Parse(config.ChartColor ?? "#2E7D32");
        var reversedValues = ChartData.Values.Reverse().ToArray();
        var dataLabelColor = GetThemeTextColor();
        var baseFontSize = GetApplicationFontSize();

        // 根据数据量调整数据标签字体大小，避免重叠
        var dataLabelFontSize = CalculateDataLabelFontSize(baseFontSize, ChartData.Values.Length);

        // 为 Tooltip 准备标签数组（需要与 reversedValues 对应）
        var reversedLabels = ChartData.Labels.Reverse().ToArray();

        var fillPaint = new SolidColorPaint(color);
        RegisterSkiaResource(fillPaint);

        SolidColorPaint? dataLabelsPaint = null;
        if (ShowDataLabels)
        {
            dataLabelsPaint = new SolidColorPaint(dataLabelColor);
            RegisterSkiaResource(dataLabelsPaint);
        }

        return new ISeries[]
        {
            new RowSeries<double>
            {
                Values = reversedValues,
                Name = null,
                Fill = fillPaint,
                DataLabelsPaint = dataLabelsPaint,
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsSize = dataLabelFontSize,
                DataLabelsFormatter = point => FormatValue(point.Coordinate.PrimaryValue),
                MaxBarWidth = 25,
                IsHoverable = ShowTooltip,
                YToolTipLabelFormatter = point =>
                {
                    var index = point.Index;
                    if (index >= 0 && index < reversedLabels.Length)
                    {
                        var label = reversedLabels[index];
                        var value = FormatValue(point.Coordinate.PrimaryValue);
                        // 如果用户开启了"显示数值"，Tooltip只显示线路名称；否则显示线路名称和数值
                        return ShowDataLabels ? label : $"{label}: {value}";
                    }

                    return FormatValue(point.Coordinate.PrimaryValue);
                },
                // 隐藏 Tooltip 头部的 X 轴坐标显示
                XToolTipLabelFormatter = point => ""
            }
        };
    }

    private ISeries[] GenerateLineSeries()
    {
        if (ChartData == null || ChartData.Values.Length == 0)
            return Array.Empty<ISeries>();

        var config = BuildConfigFromCard(Card);
        var color = SKColor.Parse(config.ChartColor ?? "#2E7D32");
        var dataLabelColor = GetThemeTextColor();
        var baseFontSize = GetApplicationFontSize();

        // 根据数据量调整数据标签字体大小，避免重叠
        var dataLabelFontSize = CalculateDataLabelFontSize(baseFontSize, ChartData.Values.Length);

        var strokePaint = new SolidColorPaint(color);
        RegisterSkiaResource(strokePaint);

        var geometryFillPaint = new SolidColorPaint(dataLabelColor);
        RegisterSkiaResource(geometryFillPaint);

        SolidColorPaint? dataLabelsPaint = null;
        if (ShowDataLabels)
        {
            dataLabelsPaint = new SolidColorPaint(dataLabelColor);
            RegisterSkiaResource(dataLabelsPaint);
        }

        return new ISeries[]
        {
            new LineSeries<double>
            {
                Values = ChartData.Values,
                Name = ChartData.SeriesName,
                Stroke = strokePaint,
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = strokePaint,
                GeometryFill = geometryFillPaint,
                DataLabelsPaint = dataLabelsPaint,
                DataLabelsSize = dataLabelFontSize,
                DataLabelsFormatter = point => FormatValue(point.Coordinate.PrimaryValue),
                LineSmoothness = 0.5,
                IsHoverable = ShowTooltip
            }
        };
    }

    /// <summary>
    ///     释放 SkiaSharp 资源
    /// </summary>
    private void DisposeSkiaResources()
    {
        foreach (var resource in _skiaResources)
            try
            {
                resource.Dispose();
            }
            catch
            {
            }

        _skiaResources.Clear();
    }

    /// <summary>
    ///     注册 SkiaSharp 资源以便后续释放
    /// </summary>
    private void RegisterSkiaResource(IDisposable resource)
    {
        if (!_isDisposed)
            _skiaResources.Add(resource);
        else
            resource.Dispose();
    }

    /// <summary>
    ///     生成坐标轴
    /// </summary>
    private void GenerateAxes(ChartType chartType)
    {
        if (ChartData == null || ChartData.Labels.Length == 0) return;

        var textColor = GetThemeTextColor();
        var baseFontSize = GetApplicationFontSize();

        // 水平条形图的坐标轴是互换的：Y轴显示分类，X轴显示数值
        if (chartType == ChartType.HorizontalBarChart)
            GenerateHorizontalBarAxes(textColor, baseFontSize);
        else
            GenerateVerticalBarAxes(textColor, baseFontSize);
    }

    /// <summary>
    ///     生成垂直柱状图/折线图的坐标轴（X轴显示分类标签）
    /// </summary>
    private void GenerateVerticalBarAxes(SKColor textColor, float baseFontSize)
    {
        var xAxisFontSize = CalculateAxisFontSize(baseFontSize, ChartData.Labels);
        var needRotation = ShouldRotateLabels(ChartData.Labels);

        // 使用45度旋转，配合Start对齐让标签根部对准刻度线
        float rotationAngle = needRotation ? 45 : 0;

        // 45度旋转需要的底部空间
        float bottomPadding = baseFontSize > 14 ? 35 : 22;

        // 45度旋转时，最后一个标签会向右延伸，需要右侧边距防止截断
        float rightPadding = needRotation ? baseFontSize > 14 ? 30 : 20 : 0;

        var xAxis = new Axis
        {
            Labels = ChartData.Labels,
            LabelsPaint = new SolidColorPaint(textColor),
            TextSize = xAxisFontSize,
            LabelsRotation = rotationAngle,
            Padding = new Padding(0, 0, rightPadding, bottomPadding),
            MinStep = 1,
            // Start对齐：让标签的左上角（根部）对准刻度线
            LabelsAlignment = Align.Start,
            UnitWidth = 1
        };
        RegisterSkiaResource(xAxis.LabelsPaint);
        XAxes = new[] { xAxis };

        // Y轴显示数值，不旋转
        // 计算最大值以设置合适的刻度
        var maxValue = ChartData.Values.Length > 0 ? ChartData.Values.Max() : 0;
        var maxLimit = Math.Max(maxValue * 1.1, 1); // 留10%边距，至少为1

        var yAxis = new Axis
        {
            Name = ChartData.SeriesName,
            NamePaint = new SolidColorPaint(textColor),
            LabelsPaint = new SolidColorPaint(textColor),
            TextSize = baseFontSize,
            NameTextSize = baseFontSize, // 设置轴名称字体大小
            // 设置名称内边距，避免与标签重叠
            NamePadding = new Padding(0, 0, 10, 0),
            MinLimit = 0,
            MaxLimit = maxLimit,
            MinStep = 1, // 最小步长为1，确保显示整数刻度
            LabelsAlignment = Align.Start,
            Padding = new Padding(5, 0, 0, 0),
            // 强制使用整数标签
            Labeler = value => value.ToString("F0")
        };
        RegisterSkiaResource(yAxis.NamePaint);
        RegisterSkiaResource(yAxis.LabelsPaint);
        YAxes = new[] { yAxis };
    }

    /// <summary>
    ///     生成水平条形图的坐标轴（Y轴显示分类标签）
    /// </summary>
    private void GenerateHorizontalBarAxes(SKColor textColor, float baseFontSize)
    {
        // 水平条形图：Y轴显示分类标签（反转顺序），X轴显示数值
        var reversedLabels = ChartData.Labels.Reverse().ToArray();

        // Y轴标签不需要旋转，但可能需要调整字体大小
        var yAxisFontSize = CalculateAxisFontSize(baseFontSize, reversedLabels);

        // Y轴显示分类标签，不旋转
        var yAxis = new Axis
        {
            Labels = reversedLabels,
            LabelsPaint = new SolidColorPaint(textColor),
            TextSize = yAxisFontSize,
            LabelsRotation = 0, // 水平条形图的Y轴标签不旋转
            Padding = new Padding(0, 0, 10, 0),
            MinStep = 1,
            LabelsAlignment = Align.End, // 右对齐，靠近条形
            UnitWidth = 1
        };
        RegisterSkiaResource(yAxis.LabelsPaint);
        YAxes = new[] { yAxis };

        // X轴显示数值，不旋转
        // 计算最大值以设置合适的刻度
        var maxValue = ChartData.Values.Length > 0 ? ChartData.Values.Max() : 0;
        var maxLimit = Math.Max(maxValue * 1.1, 1); // 留10%边距，至少为1

        var xAxis = new Axis
        {
            Name = ChartData.SeriesName,
            NamePaint = new SolidColorPaint(textColor),
            LabelsPaint = new SolidColorPaint(textColor),
            TextSize = baseFontSize,
            NameTextSize = baseFontSize, // 设置轴名称字体大小
            NamePadding = new Padding(0, 0, 0, 10),
            MinLimit = 0,
            MaxLimit = maxLimit,
            MinStep = 1, // 最小步长为1，确保显示整数刻度
            LabelsAlignment = Align.Start,
            Padding = new Padding(0, 0, 0, 5),
            // 强制使用整数标签
            Labeler = value => value.ToString("F0")
        };
        RegisterSkiaResource(xAxis.NamePaint);
        RegisterSkiaResource(xAxis.LabelsPaint);
        XAxes = new[] { xAxis };
    }

    /// <summary>
    ///     判断是否需要旋转X轴标签
    ///     当标签数量多或标签文字较长时返回true
    /// </summary>
    private bool ShouldRotateLabels(string[] labels)
    {
        var baseFontSize = GetApplicationFontSize();

        // 大字号时（超过14pt），即使标签较少也旋转，避免拥挤
        if (baseFontSize > 14) return true;

        // 标签数量超过6个时旋转
        if (labels.Length > 6) return true;

        // 检查是否有较长的标签（跨年标签如"2024年1月"）
        foreach (var label in labels)
            if (label.Length > 4)
                return true; // 超过4个字符（如"1月"是2个字符，"2024年1月"是7个字符）

        return false;
    }

    /// <summary>
    ///     根据数据量和标签长度计算坐标轴标签字体大小
    ///     数据量越大或标签越长，字体越小，以避免标签拥挤
    /// </summary>
    private float CalculateAxisFontSize(float baseFontSize, string[] labels)
    {
        var labelCount = labels.Length;

        // 检查是否有长标签（跨年标签）
        var hasLongLabels = labels.Any(l => l.Length > 4);

        // 基础字体大小调整
        var fontSize = labelCount switch
        {
            <= 6 => baseFontSize, // 6个及以下：使用基础字体大小
            <= 10 => baseFontSize * 0.9f, // 7-10个：缩小到90%
            <= 15 => baseFontSize * 0.8f, // 11-15个：缩小到80%
            <= 20 => baseFontSize * 0.7f, // 16-20个：缩小到70%
            <= 30 => baseFontSize * 0.6f, // 21-30个：缩小到60%
            _ => baseFontSize * 0.5f // 30个以上：缩小到50%
        };

        // 如果有长标签，进一步缩小字体
        if (hasLongLabels && fontSize > baseFontSize * 0.7f) fontSize *= 0.85f; // 长标签时额外缩小15%

        return fontSize;
    }

    /// <summary>
    ///     根据数据量计算数据标签字体大小
    ///     数据量越大，字体越小，以避免标签重叠
    /// </summary>
    private float CalculateDataLabelFontSize(float baseFontSize, int dataCount)
    {
        // 数据标签字体大小调整策略
        return dataCount switch
        {
            <= 5 => baseFontSize, // 5个及以下：使用基础字体大小
            <= 8 => baseFontSize * 0.9f, // 6-8个：缩小到90%
            <= 12 => baseFontSize * 0.8f, // 9-12个：缩小到80%
            <= 20 => baseFontSize * 0.7f, // 13-20个：缩小到70%
            <= 30 => baseFontSize * 0.6f, // 21-30个：缩小到60%
            _ => baseFontSize * 0.5f // 30个以上：缩小到50%
        };
    }

    /// <summary>
    ///     格式化数值显示
    /// </summary>
    private string FormatValue(double value)
    {
        // 根据统计指标类型决定格式化方式
        var seriesName = ChartData?.SeriesName ?? "";

        // 整数类型的统计指标
        if (seriesName.Contains("次数") || seriesName.Contains("数量") || seriesName.Contains("占比"))
            return value.ToString("F0"); // 整数

        // 金额类型的统计指标
        if (seriesName.Contains("花费") || seriesName.Contains("金额") || seriesName.Contains("成本"))
            return value.ToString("F2"); // 2位小数

        // 默认1位小数
        return value.ToString("F1");
    }

    /// <summary>
    ///     获取主题文字颜色
    /// </summary>
    private SKColor GetThemeTextColor()
    {
        // 根据当前主题返回对应的颜色
        // 这里使用简单的判断，实际项目中可以通过依赖注入获取主题服务
        var isDarkTheme = IsDarkTheme();
        return isDarkTheme ? SKColors.White : SKColors.Black;
    }

    /// <summary>
    ///     判断当前是否为深色主题
    /// </summary>
    private bool IsDarkTheme()
    {
        // 使用 ThemeManager 获取当前主题
        return ThemeManager.IsDarkTheme;
    }

    /// <summary>
    ///     获取应用程序字体大小（从主题管理器获取，已包含 DPI 缩放）
    /// </summary>
    private float GetApplicationFontSize()
    {
        try
        {
            // 尝试从应用程序资源获取 BaseFontSize
            // ThemeManager.ApplyFontSize 已经将 DPI 缩放应用过了
            if (Application.Current?.Resources?.Contains("BaseFontSize") == true)
            {
                var fontSize = (double)Application.Current.Resources["BaseFontSize"];
                // 直接返回，不再重复应用 DPI 缩放
                return (float)fontSize;
            }
        }
        catch
        {
        }

        return 14; // 默认字体大小（Medium）
    }

    /// <summary>
    ///     根据卡片构建完整配置
    ///     策略：统计维度和显示样式始终使用 CustomConfig，时间范围和数据过滤根据 UseGlobalConfig 决定
    /// </summary>
    private StatisticCardConfig BuildConfigFromCard(DashboardCard card)
    {
        // 1. 获取基础配置（CustomConfig 或该统计类型的默认配置）
        var config = card.CustomConfig ?? CreateDefaultConfigForStatisticType(card.StatisticType, card.Name);

        // 2. 设置时间范围（根据 UseGlobalConfig 决定是否使用全局设置）
        if (card.UseGlobalConfig)
        {
            // 使用全局时间范围
            var globalConfig = _dashboardSettingsService.Config;
            config.TimeRange = globalConfig.GlobalTimeRange switch
            {
                TimeRangeType.Last3Months => "近 3 个月",
                TimeRangeType.Last6Months => "近 6 个月",
                TimeRangeType.Last12Months => "近 12 个月",
                TimeRangeType.CalendarYear => "自然年",
                TimeRangeType.CustomRange => "自定义时间段",
                _ => "近 12 个月"
            };
            config.CustomStartDate = globalConfig.GlobalCustomStartDate;
            config.CustomEndDate = globalConfig.GlobalCustomEndDate;

            // 使用全局数据过滤设置
            config.ExcludeRefundedTickets = globalConfig.ExcludeRefundedTickets;
            config.ExcludeDuplicateTickets = globalConfig.ExcludeDuplicateTickets;
        }
        else
        {
            // 使用卡片自己的时间范围
            config.TimeRange = card.TimeRange switch
            {
                TimeRangeType.Last3Months => "近 3 个月",
                TimeRangeType.Last6Months => "近 6 个月",
                TimeRangeType.Last12Months => "近 12 个月",
                TimeRangeType.CalendarYear => "自然年",
                TimeRangeType.CustomRange => "自定义时间段",
                _ => "近 12 个月"
            };
            config.CustomStartDate = card.CustomStartDate;
            config.CustomEndDate = card.CustomEndDate;

            // 使用 CustomConfig 中的数据过滤设置（已在第1步获取）
        }

        // 3. 应用图表类型：优先使用 CustomConfig（如果设置了自定义类型），否则使用卡片级别或全局设置
        if (config.UseCustomChartType && config.ChartType != ChartType.Auto)
        {
            // 已经在 CustomConfig 中设置了自定义类型，无需更改
        }
        else if (card.ChartType != ChartType.Auto)
        {
            // 使用卡片级别的图表类型覆盖
            config.ChartType = card.ChartType;
        }
        else
        {
            // 使用全局图表类型
            var globalConfig = _dashboardSettingsService.Config;
            if (globalConfig.GlobalChartType != ChartType.Auto) config.ChartType = globalConfig.GlobalChartType;
        }

        return config;
    }

    /// <summary>
    ///     根据统计类型创建默认配置
    /// </summary>
    private static StatisticCardConfig CreateDefaultConfigForStatisticType(StatisticType statisticType, string cardName)
    {
        return statisticType switch
        {
            StatisticType.TrainTypeRatio => new TrainTypeRatioConfig(),
            StatisticType.MonthlyTripStats => new MonthlyTripStatsConfig(),
            StatisticType.StationTopRanking => new StationTopRankingConfig(),
            StatisticType.SeatTypeRatio => new SeatTypeRatioConfig(),
            StatisticType.AnnualTripSummary => new AnnualTripSummaryConfig(),
            StatisticType.TripTimeDistribution => new TripTimeDistributionConfig(),
            StatisticType.PopularRouteStats => new PopularRouteStatsConfig(),
            StatisticType.TripCostAnalysis => new TripCostAnalysisConfig(),
            _ => new StatisticCardConfig
            {
                StatisticType = statisticType,
                CardName = cardName
            }
        };
    }
}