using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GuiPiao.Model;

namespace GuiPiao.Services;

/// <summary>
///     Mock 图表数据服务 - 从 mock_tickets.json 读取数据并统计
/// </summary>
public class MockChartDataService : IChartDataService
{
    private readonly string _mockDataPath;
    private List<MockTicket> _tickets;

    public MockChartDataService()
    {
        _mockDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Map", "mock_tickets.json");

        // 设置基准日期为 2025-06-01，与 mock 数据的时间范围匹配
        ChartDataQueryBuilder.BaseDate = new DateTime(2025, 6, 1);

        LoadMockData();
    }

    /// <summary>
    ///     数据刷新事件
    /// </summary>
    public event EventHandler? DataRefreshed;

    /// <summary>
    ///     刷新数据 - 重新加载并触发刷新事件
    /// </summary>
    public void RefreshData()
    {
        LoadMockData();
        DataRefreshed?.Invoke(this, EventArgs.Empty);
        Debug.WriteLine("[MockChartDataService] 数据已刷新");
    }

    /// <summary>
    ///     清除缓存 - 清空数据并触发刷新事件
    /// </summary>
    public void ClearCache()
    {
        _tickets.Clear();
        DataRefreshed?.Invoke(this, EventArgs.Empty);
        Debug.WriteLine("[MockChartDataService] 缓存已清除");
    }

    public Task<ChartData> GetMonthlyTripStatsAsync(StatisticCardConfig config)
    {
        Debug.WriteLine(
            $"[MockChartDataService] GetMonthlyTripStatsAsync 被调用, StatisticIndicator={config.StatisticIndicator}");

        // 使用工具类过滤数据
        var filtered = _tickets
            .FilterByConfig(config, t => t.DepartDate, t => t.Status)
            .Where(t => DateTime.TryParse(t.DepartDate, out _))
            .ToList();

        Debug.WriteLine($"[MockChartDataService] 过滤后票数: {filtered.Count}");

        // 使用工具类按时间粒度分组
        var groupedData = filtered.GroupByTimeGranularity(
            config.TimeGranularity,
            t => DateTime.Parse(t.DepartDate));

        // 使用工具类根据统计指标计算值
        var monthlyData = groupedData
            .Select(g => new
            {
                Period = g.Key,
                Value = g.CalculateValue(
                    config.StatisticIndicator,
                    t => 1, // 出行次数
                    t => t.Price, // 总花费
                    t => CalculateDistance(t.DepartLat, t.DepartLng, t.ArriveLat, t.ArriveLng)) // 总里程
            });

        // 使用工具类排序
        var result = monthlyData
            .ApplySortOrder(config.SortOrder, x => x.Period, x => x.Value)
            .ToList();

        Debug.WriteLine($"[MockChartDataService] 月度数据: {result.Count} 个时间段");

        // 使用工具类生成标签
        var labels = ChartDataQueryBuilder.GenerateTimeLabels(
            result.Select(x => x.Period),
            config.TimeGranularity);

        var values = result.Select(x => x.Value).ToArray();

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = values[index].ToString("F1")
            }).ToList()
        });
    }

    public Task<ChartData> GetTrainTypeRatioAsync(StatisticCardConfig config)
    {
        var filtered = FilterTickets(config).ToList();
        Debug.WriteLine($"[GetTrainTypeRatioAsync] 过滤后车票数: {filtered.Count}, 统计指标: {config.StatisticIndicator}");

        // 打印前5条车票数据
        foreach (var t in filtered.Take(5))
            Debug.WriteLine($"  车票: {t.TrainNo}, {t.DepartDate}, {t.DepartStation}->{t.ArriveStation}");

        // 根据分类依据选择分组方式
        var groupedData = config.ClassificationBasis switch
        {
            "按席别" => filtered
                .Where(t => !string.IsNullOrEmpty(t.SeatType))
                .GroupBy(t => t.SeatType),
            "按出发时段" => filtered
                .Where(t => !string.IsNullOrEmpty(t.DepartTime))
                .GroupBy(t =>
                {
                    if (TimeSpan.TryParse(t.DepartTime, out var time))
                        return time.Hours switch
                        {
                            >= 6 and < 12 => "上午 (06:00-12:00)",
                            >= 12 and < 18 => "下午 (12:00-18:00)",
                            >= 18 and < 24 => "晚上 (18:00-24:00)",
                            _ => "凌晨 (00:00-06:00)"
                        };
                    return "未知时段";
                }),
            "按出行日期（工作日/周末）" => filtered
                .Where(t => DateTime.TryParse(t.DepartDate, out _))
                .GroupBy(t =>
                {
                    var date = DateTime.Parse(t.DepartDate);
                    return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                        ? "周末"
                        : "工作日";
                }),
            _ => filtered // 默认按车次类型
                .GroupBy(t =>
                {
                    if (string.IsNullOrEmpty(t.TrainNo)) return "其他";
                    if (t.TrainNo.StartsWith("G")) return "G字头";
                    if (t.TrainNo.StartsWith("D")) return "D字头";
                    return "其他";
                })
        };

        // 根据统计指标计算每个分组的值
        var groupList = groupedData
            .Select(g =>
            {
                var ticketCount = g.Count();
                var tripCount = g.Select(t => $"{t.DepartDate}_{t.DepartStation}_{t.ArriveStation}").Distinct().Count();
                var value = g.CalculateValue(
                    config.StatisticIndicator,
                    t => 1, // 车票数量
                    t => t.Price, // 总花费
                    t => CalculateDistance(t.DepartLat, t.DepartLng, t.ArriveLat, t.ArriveLng), // 总里程
                    t => $"{t.DepartDate}_{t.DepartStation}_{t.ArriveStation}"); // 行程唯一标识（日期+出发站+到达站）

                Debug.WriteLine($"  分组 {g.Key}: 车票数={ticketCount}, 出行次数={tripCount}, 计算值={value}");

                return new
                {
                    Label = g.Key,
                    Value = value
                };
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var total = groupList.Sum(x => x.Value);
        var labels = groupList.Select(x => x.Label).ToArray();
        var values = groupList.Select(x => x.Value).ToArray();
        var percentages = total > 0 ? groupList.Select(x => x.Value / total * 100).ToArray() : null;

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            Percentages = percentages,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = $"{values[index]:F0} ({percentages?[index]:F1}%)"
            }).ToList()
        });
    }

    public Task<ChartData> GetStationTopRankingAsync(StatisticCardConfig config)
    {
        var filtered = FilterTickets(config).ToList();

        // 根据分类依据选择分组字段
        var stationData = config.ClassificationBasis switch
        {
            "按到达站" => filtered.GroupBy(t => t.ArriveStation),
            _ => filtered.GroupBy(t => t.DepartStation) // 默认按出发站
        };

        // 根据统计指标计算每个站点的值
        var stationList = stationData
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => new
            {
                Station = g.Key,
                Value = g.CalculateValue(
                    config.StatisticIndicator,
                    t => 1, // 出行次数
                    t => t.Price, // 总花费
                    t => CalculateDistance(t.DepartLat, t.DepartLng, t.ArriveLat, t.ArriveLng), // 总里程
                    t => $"{t.DepartDate}_{t.DepartStation}_{t.ArriveStation}") // 行程唯一标识
            })
            .ToList();

        // 根据排序方式排序
        var sortedData = config.SortOrder switch
        {
            "按站点名称升序（拼音）" => stationList.OrderBy(x => x.Station).ToList(),
            _ => stationList.OrderByDescending(x => x.Value).ToList() // 默认按数值降序
        };

        // 取TOP N
        var topData = sortedData.Take(config.TopCount).ToList();

        var labels = topData.Select(x => x.Station).ToArray();
        var values = topData.Select(x => x.Value).ToArray();

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = values[index].ToString("F0")
            }).ToList()
        });
    }

    public Task<ChartData> GetSeatTypeRatioAsync(StatisticCardConfig config)
    {
        var filtered = FilterTickets(config).ToList();

        // 根据分类依据选择分组方式
        var groupedData = config.ClassificationBasis switch
        {
            "按车次类型" => filtered
                .Where(t => !string.IsNullOrEmpty(t.TrainNo))
                .GroupBy(t =>
                {
                    if (t.TrainNo.StartsWith("G")) return "G字头";
                    if (t.TrainNo.StartsWith("D")) return "D字头";
                    return "其他";
                }),
            "按出发时段" => filtered
                .Where(t => !string.IsNullOrEmpty(t.DepartTime))
                .GroupBy(t =>
                {
                    if (TimeSpan.TryParse(t.DepartTime, out var time))
                        return time.Hours switch
                        {
                            >= 6 and < 12 => "上午 (06:00-12:00)",
                            >= 12 and < 18 => "下午 (12:00-18:00)",
                            >= 18 and < 24 => "晚上 (18:00-24:00)",
                            _ => "凌晨 (00:00-06:00)"
                        };
                    return "未知时段";
                }),
            "按出行日期（工作日/周末）" => filtered
                .Where(t => DateTime.TryParse(t.DepartDate, out _))
                .GroupBy(t =>
                {
                    var date = DateTime.Parse(t.DepartDate);
                    return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                        ? "周末"
                        : "工作日";
                }),
            _ => filtered // 默认按席别
                .Where(t => !string.IsNullOrEmpty(t.SeatType))
                .GroupBy(t => t.SeatType)
        };

        // 根据统计指标计算每个分组的值
        var groupList = groupedData
            .Select(g => new
            {
                Label = g.Key,
                Value = g.CalculateValue(
                    config.StatisticIndicator,
                    t => 1, // 车票数量
                    t => t.Price, // 总花费
                    t => CalculateDistance(t.DepartLat, t.DepartLng, t.ArriveLat, t.ArriveLng), // 总里程
                    t => $"{t.DepartDate}_{t.DepartStation}_{t.ArriveStation}") // 行程唯一标识（日期+出发站+到达站）
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var total = groupList.Sum(x => x.Value);
        var labels = groupList.Select(x => x.Label).ToArray();
        var values = groupList.Select(x => x.Value).ToArray();
        var percentages = total > 0 ? groupList.Select(x => x.Value / total * 100).ToArray() : null;

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            Percentages = percentages,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = $"{values[index]:F0} ({percentages?[index]:F1}%)"
            }).ToList()
        });
    }

    public Task<ChartData> GetAnnualTripSummaryAsync(StatisticCardConfig config)
    {
        var filtered = FilterTickets(config)
            .Where(t => DateTime.TryParse(t.DepartDate, out _))
            .ToList();

        // 按年份分组并计算每年的统计值
        var yearlyData = filtered
            .GroupBy(t => DateTime.Parse(t.DepartDate).Year)
            .Select(g => new
            {
                Year = g.Key,
                Value = g.CalculateValue(
                    config.StatisticIndicator,
                    t => 1, // 出行次数
                    t => t.Price, // 总花费
                    t => CalculateDistance(t.DepartLat, t.DepartLng, t.ArriveLat, t.ArriveLng), // 总里程
                    t => $"{t.DepartDate}_{t.DepartStation}_{t.ArriveStation}"),
                TicketCount = g.Count(),
                Months = g.Select(t => DateTime.Parse(t.DepartDate).Month).Distinct().Count()
            })
            .OrderBy(x => x.Year)
            .ToList();

        // 根据统计指标计算最终值
        var processedData = yearlyData.Select(x =>
        {
            var value = config.StatisticIndicator switch
            {
                "平均每月出行次数" => x.Months > 0 ? x.TicketCount / x.Months : 0,
                _ => x.Value
            };
            return new { x.Year, Value = value };
        }).ToList();

        // 根据对比维度处理数据
        string[] labels;
        double[] values;
        double[]? comparisonValues = null;

        switch (config.ClassificationBasis)
        {
            case "与上一年对比":
                labels = processedData.Skip(1).Select(x => $"{x.Year}年").ToArray();
                values = processedData.Skip(1).Select(x => x.Value).ToArray();
                comparisonValues = processedData.Skip(1).Select((x, i) =>
                {
                    var prevYear = processedData.FirstOrDefault(y => y.Year == x.Year - 1);
                    return prevYear?.Value ?? 0;
                }).ToArray();
                break;

            case "与近三年均值对比":
                labels = processedData.Select(x => $"{x.Year}年").ToArray();
                values = processedData.Select(x => x.Value).ToArray();
                var avgOfLast3Years = processedData.Count >= 3
                    ? processedData.Skip(processedData.Count - 3).Average(x => x.Value)
                    : processedData.Average(x => x.Value);
                comparisonValues = processedData.Select(_ => avgOfLast3Years).ToArray();
                break;

            default: // "无对比"
                labels = processedData.Select(x => $"{x.Year}年").ToArray();
                values = processedData.Select(x => x.Value).ToArray();
                break;
        }

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            ComparisonValues = comparisonValues,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = comparisonValues != null
                    ? $"{values[index]:F0} (对比: {comparisonValues[index]:F0})"
                    : values[index].ToString("F0")
            }).ToList()
        });
    }

    public Task<ChartData> GetTripTimeDistributionAsync(StatisticCardConfig config)
    {
        var filtered = FilterTickets(config)
            .Where(t => !string.IsNullOrEmpty(t.DepartTime))
            .ToList();

        Debug.WriteLine(
            $"[GetTripTimeDistributionAsync] ClassificationBasis={config.ClassificationBasis}, CustomTimePeriods.Count={config.CustomTimePeriods?.Count ?? 0}");
        if (config.CustomTimePeriods?.Count > 0)
            foreach (var period in config.CustomTimePeriods)
                Debug.WriteLine($"  时段: {period.Name}, {period.TimeRange}");

        // 根据时段划分配置选择分组方式
        var timeData = filtered
            .Select(t =>
            {
                if (TimeSpan.TryParse(t.DepartTime, out var time))
                {
                    var period = config.ClassificationBasis switch
                    {
                        "6段" => time.Hours switch
                        {
                            >= 0 and < 4 => "凌晨 (00:00-04:00)",
                            >= 4 and < 8 => "早晨 (04:00-08:00)",
                            >= 8 and < 12 => "上午 (08:00-12:00)",
                            >= 12 and < 16 => "下午 (12:00-16:00)",
                            >= 16 and < 20 => "傍晚 (16:00-20:00)",
                            _ => "晚上 (20:00-24:00)"
                        },
                        "自定义时段" => GetCustomTimePeriodName(time, config.CustomTimePeriods),
                        _ => time.Hours switch // 默认4段（凌晨/早/中/晚）
                        {
                            >= 0 and < 6 => "凌晨",
                            >= 6 and < 12 => "上午",
                            >= 12 and < 18 => "下午",
                            _ => "晚上"
                        }
                    };
                    Debug.WriteLine($"  车票 {t.TrainNo} {t.DepartTime} -> {period}");
                    return period;
                }

                return "未知";
            })
            .Where(period => !string.IsNullOrEmpty(period))
            .GroupBy(period => period)
            .Select(g => new { Period = g.Key, Count = g.Count() })
            .OrderBy(x => GetPeriodOrder(x.Period, config.ClassificationBasis, config.CustomTimePeriods))
            .ToList();

        Debug.WriteLine($"[GetTripTimeDistributionAsync] 分组结果: {timeData.Count} 组");
        foreach (var item in timeData) Debug.WriteLine($"  {item.Period}: {item.Count}");

        var total = timeData.Sum(x => x.Count);
        var labels = timeData.Select(x => x.Period).ToArray();
        var values = timeData.Select(x => (double)x.Count).ToArray();
        var percentages = total > 0 ? timeData.Select(x => (double)x.Count / total * 100).ToArray() : null;

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            Percentages = percentages,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = $"{values[index]:F0} ({percentages?[index]:F1}%)"
            }).ToList()
        });
    }

    public Task<ChartData> GetPopularRouteStatsAsync(StatisticCardConfig config)
    {
        var filtered = FilterTickets(config)
            .Where(t => !string.IsNullOrEmpty(t.DepartStation) && !string.IsNullOrEmpty(t.ArriveStation));

        var routeData = filtered
            .GroupBy(t => $"{t.DepartStation}-{t.ArriveStation}")
            .Select(g => new { Route = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(config.TopCount)
            .ToList();

        var labels = routeData.Select(x => x.Route).ToArray();
        var values = routeData.Select(x => (double)x.Count).ToArray();

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = values[index].ToString("F0")
            }).ToList()
        });
    }

    public Task<ChartData> GetTripCostAnalysisAsync(StatisticCardConfig config)
    {
        var filtered = FilterTickets(config)
            .Where(t => DateTime.TryParse(t.DepartDate, out _));

        // 根据统计维度选择分组方式
        var costData = config.ClassificationBasis switch
        {
            "按车次类型" => filtered
                .GroupBy(t => GetTrainType(t.TrainNo))
                .Select(g => new { g.Key, TotalCost = g.Sum(t => t.Price) })
                .OrderByDescending(x => x.TotalCost)
                .ToList(),
            "按席别" => filtered
                .GroupBy(t => t.SeatType ?? "其他")
                .Select(g => new { g.Key, TotalCost = g.Sum(t => t.Price) })
                .OrderByDescending(x => x.TotalCost)
                .ToList(),
            "按出行时段" => filtered
                .GroupBy(t => GetTimePeriod(t.DepartTime))
                .Select(g => new { g.Key, TotalCost = g.Sum(t => t.Price) })
                .OrderBy(x => x.Key)
                .ToList(),
            _ => filtered // 默认按月份
                .GroupBy(t => DateTime.Parse(t.DepartDate).ToString("yyyy-MM"))
                .Select(g => new { g.Key, TotalCost = g.Sum(t => t.Price) })
                .OrderBy(x => x.Key)
                .ToList()
        };

        var labels = costData.Select(x => x.Key).ToArray();
        var values = costData.Select(x => x.TotalCost).ToArray();

        return Task.FromResult(new ChartData
        {
            Labels = labels,
            Values = values,
            SeriesName = config.StatisticIndicator,
            TextListItems = labels.Select((label, index) => new TextListItem
            {
                Label = label,
                Value = values[index].ToString("F2")
            }).ToList()
        });
    }

    private void LoadMockData()
    {
        try
        {
            Debug.WriteLine($"[MockChartDataService] 尝试加载数据: {_mockDataPath}");
            if (File.Exists(_mockDataPath))
            {
                var json = File.ReadAllText(_mockDataPath);
                _tickets = JsonSerializer.Deserialize<List<MockTicket>>(json) ?? new List<MockTicket>();
                Debug.WriteLine($"[MockChartDataService] 加载了 {_tickets.Count} 条数据");
            }
            else
            {
                Debug.WriteLine($"[MockChartDataService] 文件不存在: {_mockDataPath}");
                _tickets = new List<MockTicket>();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MockChartDataService] 加载数据失败: {ex.Message}");
            _tickets = new List<MockTicket>();
        }
    }

    /// <summary>
    ///     根据配置过滤数据
    /// </summary>
    private IEnumerable<MockTicket> FilterTickets(StatisticCardConfig config)
    {
        return _tickets.FilterByConfig(config, t => t.DepartDate, t => t.Status);
    }

    /// <summary>
    ///     根据自定义时段配置获取时段名称
    /// </summary>
    private string GetCustomTimePeriodName(TimeSpan time, List<CustomTimePeriod> customPeriods)
    {
        if (customPeriods == null || customPeriods.Count == 0)
            // 如果没有自定义时段配置，使用默认4段
            return time.Hours switch
            {
                >= 0 and < 6 => "凌晨",
                >= 6 and < 12 => "上午",
                >= 12 and < 18 => "下午",
                _ => "晚上"
            };

        foreach (var period in customPeriods.OrderBy(p => p.StartTotalMinutes))
        {
            var totalMinutes = (int)time.TotalMinutes;
            if (totalMinutes >= period.StartTotalMinutes && totalMinutes < period.EndTotalMinutes)
                return $"{period.Name} ({period.TimeRange})";
        }

        return "未知";
    }

    /// <summary>
    ///     从车次号提取车次类型
    /// </summary>
    private string GetTrainType(string trainNo)
    {
        if (string.IsNullOrEmpty(trainNo))
            return "其他";

        // 根据车次号首字母判断类型
        var firstChar = trainNo[0];
        return firstChar switch
        {
            'G' => "高铁",
            'D' => "动车",
            'C' => "城际",
            'Z' => "直达",
            'T' => "特快",
            'K' => "快速",
            _ => "其他"
        };
    }

    /// <summary>
    ///     获取出发时间所属时段
    /// </summary>
    private string GetTimePeriod(string? departTime)
    {
        if (string.IsNullOrEmpty(departTime) || !TimeSpan.TryParse(departTime, out var time))
            return "未知";

        return time.Hours switch
        {
            >= 0 and < 6 => "凌晨",
            >= 6 and < 12 => "上午",
            >= 12 and < 18 => "下午",
            _ => "晚上"
        };
    }

    #region 辅助方法

    /// <summary>
    ///     计算两点之间的距离（简化版，使用欧几里得距离）
    /// </summary>
    private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        // 简化的距离计算，实际应该使用 Haversine 公式
        var dLat = lat2 - lat1;
        var dLng = lng2 - lng1;
        return Math.Sqrt(dLat * dLat + dLng * dLng) * 111; // 粗略转换为公里
    }

    private int GetPeriodOrder(string period, string? classificationBasis = null,
        List<CustomTimePeriod>? customPeriods = null)
    {
        if (classificationBasis == "自定义时段" && customPeriods != null && customPeriods.Count > 0)
        {
            // 从自定义时段名称中提取时段名（去掉时间范围）
            var periodName = period.Split('(')[0].Trim();
            var customPeriod = customPeriods
                .Select((p, index) => new { Period = p, Index = index })
                .FirstOrDefault(x => x.Period.Name == periodName);
            return customPeriod?.Index ?? int.MaxValue;
        }

        return classificationBasis switch
        {
            "6段" => period switch
            {
                "凌晨 (00:00-04:00)" => 0,
                "早晨 (04:00-08:00)" => 1,
                "上午 (08:00-12:00)" => 2,
                "下午 (12:00-16:00)" => 3,
                "傍晚 (16:00-20:00)" => 4,
                "晚上 (20:00-24:00)" => 5,
                _ => 6
            },
            _ => period switch // 默认4段
            {
                "凌晨" => 0,
                "上午" => 1,
                "下午" => 2,
                "晚上" => 3,
                _ => 4
            }
        };
    }

    #endregion
}