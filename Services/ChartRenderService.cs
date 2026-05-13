using System;
using System.Linq;
using GuiPiao.Model;
using GuiPiao.ViewModel;
using SkiaSharp;

namespace GuiPiao.Services;

/// <summary>
///     图表渲染服务 - 生成统计图表图片
/// </summary>
public class ChartRenderService : IDisposable
{
    private const int ChartWidth = 800;
    private const int ChartHeight = 600;
    private const int TitleHeight = 50;
    private const int Padding = 40;

    // 配色方案
    private static readonly SKColor[] DefaultColors =
    {
        new(59, 130, 246),
        new(34, 197, 94),
        new(168, 85, 247),
        new(245, 115, 22),
        new(236, 72, 153),
        new(14, 165, 233),
        new(132, 204, 22),
        new(244, 63, 94)
    };

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    public SKImage RenderChart(DashboardChartViewModel chartViewModel)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChartRenderService));

        var surface = SKSurface.Create(new SKImageInfo(ChartWidth, ChartHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        try
        {
            ChartType chartType;
            if (chartViewModel?.Card == null)
            {
                // 如果 card 为空，使用简单判断
                if (chartViewModel?.IsPieChart == true)
                    chartType = ChartType.PieChart;
                else if (chartViewModel?.IsTextList == true)
                    chartType = ChartType.TextList;
                else
                    chartType = ChartType.BarChart;
            }
            else
            {
                // 使用和 LiveCharts 相同的逻辑获取最终图表类型
                var config = BuildConfigFromCard(chartViewModel.Card);
                chartType = config.ChartType;
            }

            var success = false;

            if (chartType == ChartType.PieChart)
            {
                success = TryDrawSimplePieChart(canvas, chartViewModel);
            }
            else if (chartType == ChartType.HorizontalBarChart)
            {
                success = TryDrawSimpleHorizontalBarChart(canvas, chartViewModel);
            }
            else if (chartType == ChartType.LineChart)
            {
                success = TryDrawSimpleLineChart(canvas, chartViewModel);
            }
            else if (chartType == ChartType.BarChart)
            {
                success = TryDrawSimpleBarChart(canvas, chartViewModel);
            }
            else if (chartType == ChartType.TextList)
            {
                DrawSimpleTextList(canvas, chartViewModel);
                success = true;
            }
            else // Auto
            {
                if (chartViewModel?.IsPieChart == true)
                {
                    success = TryDrawSimplePieChart(canvas, chartViewModel);
                }
                else if (chartViewModel?.IsTextList == true)
                {
                    DrawSimpleTextList(canvas, chartViewModel);
                    success = true;
                }
                else
                {
                    success = TryDrawSimpleBarChart(canvas, chartViewModel);
                }
            }

            if (!success) DrawSimpleTextList(canvas, chartViewModel);

            return surface.Snapshot();
        }
        catch
        {
            DrawSimpleTextList(canvas, chartViewModel);
            return surface.Snapshot();
        }
    }

    private StatisticCardConfig BuildConfigFromCard(DashboardCard card)
    {
        if (card == null)
            return new StatisticCardConfig();

        var config = card.CustomConfig ?? CreateDefaultConfigForStatisticType(card.StatisticType, card.Name ?? "未命名");
        var dashboardService = new DashboardSettingsService();

        if (card.UseGlobalConfig)
        {
            var globalConfig = dashboardService.Config;
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
            config.ExcludeRefundedTickets = globalConfig.ExcludeRefundedTickets;
            config.ExcludeDuplicateTickets = globalConfig.ExcludeDuplicateTickets;
        }
        else
        {
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
        }

        if (config.UseCustomChartType && config.ChartType != ChartType.Auto)
        {
        }
        else if (card.ChartType != ChartType.Auto)
        {
            config.ChartType = card.ChartType;
        }
        else
        {
            var globalConfig = dashboardService.Config;
            if (globalConfig.GlobalChartType != ChartType.Auto) config.ChartType = globalConfig.GlobalChartType;
        }

        return config;
    }

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

    private void DrawTitle(SKCanvas canvas, string title)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 20,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
        };

        canvas.DrawText(title, ChartWidth / 2f, 35f, paint);
    }

    private bool TryDrawSimpleBarChart(SKCanvas canvas, DashboardChartViewModel chartViewModel)
    {
        DrawTitle(canvas, chartViewModel.Title ?? chartViewModel.Card?.Name ?? "未命名图表");

        var data = chartViewModel.ChartData;
        if (data == null || data.Values.Length == 0) return false;

        var maxValue = (float)data.Values.DefaultIfEmpty(1).Max();
        var values = data.Values;
        var labels = data.Labels;

        var chartAreaWidth = (float)(ChartWidth - Padding * 2);
        var chartAreaHeight = (float)(ChartHeight - TitleHeight - Padding * 2 - 30);
        var startX = (float)Padding;
        var startY = (float)(TitleHeight + Padding);

        var barWidth = 50f;
        var barGap = 20f;
        var totalWidth = values.Length * barWidth + (values.Length - 1) * barGap;
        var scale = Math.Min(1f, chartAreaWidth / totalWidth);
        barWidth *= scale;
        barGap *= scale;

        var offsetX = (chartAreaWidth - (values.Length * barWidth + (values.Length - 1) * barGap)) / 2f;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var label = labels.Length > i ? labels[i] : $"项目 {i + 1}";
            var color = DefaultColors[i % DefaultColors.Length];

            var barHeight = chartAreaHeight * ((float)value / maxValue);
            var x = startX + offsetX + i * (barWidth + barGap);
            var y = startY + chartAreaHeight - barHeight;

            using var paint = new SKPaint { IsAntialias = true, Color = color };
            canvas.DrawRect(x, y, barWidth, barHeight, paint);

            using var valuePaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
            };
            canvas.DrawText(FormatValue(value), x + barWidth / 2f, y - 10f, valuePaint);

            using var labelPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 10,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
            };
            canvas.DrawText(label, x + barWidth / 2f, startY + chartAreaHeight + 18f, labelPaint);
        }

        return true;
    }

    private bool TryDrawSimplePieChart(SKCanvas canvas, DashboardChartViewModel chartViewModel)
    {
        DrawTitle(canvas, chartViewModel.Title ?? chartViewModel.Card?.Name ?? "未命名图表");

        var data = chartViewModel.ChartData;
        if (data == null || data.Values.Length == 0) return false;

        var total = data.Values.Sum();
        var centerX = ChartWidth / 2f;
        var centerY = (float)((ChartHeight - TitleHeight) / 2.0 + TitleHeight);
        var radius = (float)(Math.Min(ChartWidth, ChartHeight - TitleHeight) / 2.0 - Padding);

        var startAngle = -90f;

        var legendX = centerX + radius + 40f;
        var legendY = centerY - data.Values.Length * 15f;

        for (var i = 0; i < data.Values.Length; i++)
        {
            var value = data.Values[i];
            var label = data.Labels.Length > i ? data.Labels[i] : $"项目 {i + 1}";
            var color = DefaultColors[i % DefaultColors.Length];

            if (total > 0)
            {
                var sweepAngle = 360f * ((float)value / (float)total);
                if (sweepAngle > 359f) sweepAngle = 359f;

                using var paint = new SKPaint { IsAntialias = true, Color = color };
                using var path = new SKPath();
                path.MoveTo(centerX, centerY);
                path.ArcTo(
                    new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius),
                    startAngle, sweepAngle, false);
                path.Close();
                canvas.DrawPath(path, paint);
                startAngle += sweepAngle;
            }
            else if (value > 0)
            {
                using var paint = new SKPaint { IsAntialias = true, Color = color };
                canvas.DrawCircle(centerX, centerY, radius, paint);
            }

            using var rectPaint = new SKPaint { IsAntialias = true, Color = color };
            canvas.DrawRect(legendX, legendY + i * 30f, 16f, 16f, rectPaint);

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true,
                TextAlign = SKTextAlign.Left,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
            };
            canvas.DrawText($"{label}: {FormatValue(value)}", legendX + 24f, legendY + i * 30f + 12f, textPaint);
        }

        return true;
    }

    private bool TryDrawSimpleHorizontalBarChart(SKCanvas canvas, DashboardChartViewModel chartViewModel)
    {
        DrawTitle(canvas, chartViewModel.Title ?? chartViewModel.Card?.Name ?? "未命名图表");

        var data = chartViewModel.ChartData;
        if (data == null || data.Values.Length == 0) return false;

        var maxValue = (float)data.Values.DefaultIfEmpty(1).Max();
        var values = data.Values;
        var labels = data.Labels;

        var chartAreaWidth = (float)(ChartWidth - Padding * 2 - 150);
        var chartAreaHeight = (float)(ChartHeight - TitleHeight - Padding * 2);
        var startX = (float)(Padding + 150);
        var startY = (float)(TitleHeight + Padding);

        var barHeight = 40f;
        var barGap = 15f;
        var totalHeight = values.Length * barHeight + (values.Length - 1) * barGap;
        var scale = Math.Min(1f, chartAreaHeight / totalHeight);
        barHeight *= scale;
        barGap *= scale;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var label = labels.Length > i ? labels[i] : $"项目 {i + 1}";
            var color = DefaultColors[i % DefaultColors.Length];

            var barWidth = chartAreaWidth * ((float)value / maxValue);
            var x = startX;
            var y = startY + i * (barHeight + barGap);

            using var paint = new SKPaint { IsAntialias = true, Color = color };
            canvas.DrawRect(x, y, barWidth, barHeight, paint);

            using var labelPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 12,
                IsAntialias = true,
                TextAlign = SKTextAlign.Right,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
            };
            canvas.DrawText(label, startX - 10f, y + barHeight / 2f + 4f, labelPaint);

            using var valuePaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true,
                TextAlign = SKTextAlign.Left,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
            };
            canvas.DrawText(FormatValue(value), x + barWidth + 8f, y + barHeight / 2f + 4f, valuePaint);
        }

        return true;
    }

    private bool TryDrawSimpleLineChart(SKCanvas canvas, DashboardChartViewModel chartViewModel)
    {
        DrawTitle(canvas, chartViewModel.Title ?? chartViewModel.Card?.Name ?? "未命名图表");

        var data = chartViewModel.ChartData;
        if (data == null || data.Values.Length == 0) return false;

        var maxValue = (float)data.Values.DefaultIfEmpty(1).Max();
        var minValue = (float)data.Values.DefaultIfEmpty(0).Min();
        var values = data.Values;
        var labels = data.Labels;

        var chartAreaWidth = (float)(ChartWidth - Padding * 2 - 50); // 给Y轴标签留空间
        var chartAreaHeight = (float)(ChartHeight - TitleHeight - Padding * 2 - 25);
        var startX = (float)(Padding + 50); // Y轴起点
        var startY = (float)(TitleHeight + Padding);

        // 确保最大值和最小值有区别
        if (maxValue == minValue) maxValue += 1;

        using var axisPaint = new SKPaint
        {
            Color = SKColors.LightGray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };
        canvas.DrawLine(startX, startY + chartAreaHeight, startX + chartAreaWidth, startY + chartAreaHeight, axisPaint);
        canvas.DrawLine(startX, startY, startX, startY + chartAreaHeight, axisPaint);

        // 绘制Y轴刻度标签
        using var yLabelPaint = new SKPaint
        {
            Color = SKColors.Gray,
            TextSize = 10,
            IsAntialias = true,
            TextAlign = SKTextAlign.Right,
            Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
        };

        var yTicks = 5;
        for (var i = 0; i <= yTicks; i++)
        {
            var ratio = (float)i / yTicks;
            var value = minValue + (maxValue - minValue) * (1 - ratio);
            var y = startY + chartAreaHeight * ratio;

            // 绘制刻度线
            canvas.DrawLine(startX - 5, y, startX, y, axisPaint);

            // 绘制刻度值
            canvas.DrawText(FormatValue(value), startX - 8, y + 3, yLabelPaint);
        }

        var points = new SKPoint[values.Length];
        var pointSpacing = chartAreaWidth / Math.Max(1f, values.Length - 1);

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            var x = startX + i * pointSpacing;
            var y = startY + chartAreaHeight - chartAreaHeight * (((float)value - minValue) / (maxValue - minValue));
            points[i] = new SKPoint(x, y);
        }

        using var linePaint = new SKPaint
        {
            Color = DefaultColors[0],
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            IsAntialias = true
        };

        using var path = new SKPath();
        path.MoveTo(points[0]);
        for (var i = 1; i < points.Length; i++) path.LineTo(points[i]);
        canvas.DrawPath(path, linePaint);

        using var pointPaint = new SKPaint
        {
            Color = DefaultColors[0],
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var valuePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 12,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
        };

        for (var i = 0; i < points.Length; i++)
        {
            canvas.DrawCircle(points[i].X, points[i].Y, 5f, pointPaint);
            canvas.DrawText(FormatValue(values[i]), points[i].X, points[i].Y - 12f, valuePaint);
        }

        using var labelPaint = new SKPaint
        {
            Color = SKColors.Gray,
            TextSize = 10,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
        };

        for (var i = 0; i < labels.Length && i < points.Length; i++)
            canvas.DrawText(labels[i], points[i].X, startY + chartAreaHeight + 18f, labelPaint);

        return true;
    }

    private void DrawSimpleTextList(SKCanvas canvas, DashboardChartViewModel chartViewModel)
    {
        DrawTitle(canvas, chartViewModel.Title ?? chartViewModel.Card?.Name ?? "未命名图表");

        var data = chartViewModel.ChartData;
        if (data == null) return;

        var startY = (float)(TitleHeight + 40);

        using var labelPaint = new SKPaint
        {
            Color = SKColors.DarkGray,
            TextSize = 14,
            IsAntialias = true,
            TextAlign = SKTextAlign.Left,
            Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
        };

        using var valuePaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            TextAlign = SKTextAlign.Right,
            Typeface = SKTypeface.FromFamilyName("Microsoft YaHei")
        };

        var labels = data.Labels ?? Array.Empty<string>();
        var values = data.Values ?? Array.Empty<double>();

        for (var i = 0; i < Math.Min(values.Length, 20); i++)
        {
            var label = i < labels.Length ? labels[i] : $"项目 {i + 1}";
            var value = i < values.Length ? values[i] : 0;

            canvas.DrawText($"• {label}", Padding, startY, labelPaint);
            canvas.DrawText(FormatValue(value), ChartWidth - Padding, startY, valuePaint);
            startY += 28f;
        }

        if (values.Length > 20) canvas.DrawText($"... 共 {values.Length} 项", Padding, startY, labelPaint);
    }

    private string FormatValue(double value)
    {
        if (value >= 10000)
            return value.ToString("N0");
        if (value == Math.Floor(value))
            return value.ToString("F0");
        return value.ToString("F2");
    }
}