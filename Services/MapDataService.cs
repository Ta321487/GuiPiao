using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>
///     地图数据服务 - 从数据库加载车票和车站数据
/// </summary>
public class MapDataService
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;
    private readonly StationRepository _stationRepository = new();
    private readonly TrainRideRepository _trainRideRepository = new();

    /// <summary>
    ///     加载地图数据（包含经纬度信息）
    /// </summary>
    /// <returns>地图数据结果，包含有效车票列表和缺少经纬度的车站列表</returns>
    public async Task<MapDataResult> LoadMapDataAsync()
    {
        var result = new MapDataResult
        {
            ValidTickets = new List<MapTicketData>(),
            StationsWithoutCoordinates = new List<string>()
        };

        try
        {
            Debug.WriteLine($"[MapDataService] 数据库连接字符串: {_connectionString}");
            Debug.WriteLine($"[MapDataService] 当前工作目录: {Directory.GetCurrentDirectory()}");

            // 获取所有车票数据
            var trainRides = await _trainRideRepository.GetAllTrainRidesAsync();
            Debug.WriteLine($"[MapDataService] 从数据库加载了 {trainRides.Count()} 条车票记录");

            // 获取所有车站信息（用于查询经纬度）
            var stations = await _stationRepository.GetAllStationsAsync();
            Debug.WriteLine($"[MapDataService] 从数据库加载了 {stations.Count()} 个车站");

            // 处理重复车站名称，保留第一个
            var stationDict = new Dictionary<string, StationInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var station in stations)
                if (!string.IsNullOrEmpty(station.StationName) && !stationDict.ContainsKey(station.StationName))
                    stationDict.Add(station.StationName, station);

            Debug.WriteLine($"[MapDataService] 去重后剩余 {stationDict.Count} 个车站");

            foreach (var ride in trainRides)
            {
                // 获取出发站和到达站的经纬度
                var departStation = GetStationInfo(ride.DepartStation, stationDict);
                var arriveStation = GetStationInfo(ride.ArriveStation, stationDict);

                // 检查出发站经纬度
                var hasDepartCoords = TryParseCoordinates(departStation?.Longitude, departStation?.Latitude,
                    out var departLng, out var departLat);
                if (!hasDepartCoords && !string.IsNullOrEmpty(ride.DepartStation))
                    if (!result.StationsWithoutCoordinates.Contains(ride.DepartStation))
                        result.StationsWithoutCoordinates.Add(ride.DepartStation);

                // 检查到达站经纬度
                var hasArriveCoords = TryParseCoordinates(arriveStation?.Longitude, arriveStation?.Latitude,
                    out var arriveLng, out var arriveLat);
                if (!hasArriveCoords && !string.IsNullOrEmpty(ride.ArriveStation))
                    if (!result.StationsWithoutCoordinates.Contains(ride.ArriveStation))
                        result.StationsWithoutCoordinates.Add(ride.ArriveStation);

                // 如果两个站都有经纬度，则添加到有效车票列表
                if (hasDepartCoords && hasArriveCoords)
                {
                    // 额外检查：确保坐标在中国范围内（北纬18°-54°，东经73°-135°）
                    var departInChina = departLat >= 18 && departLat <= 54 && departLng >= 73 && departLng <= 135;
                    var arriveInChina = arriveLat >= 18 && arriveLat <= 54 && arriveLng >= 73 && arriveLng <= 135;

                    if (!departInChina || !arriveInChina)
                    {
                        Debug.WriteLine(
                            $"[MapDataService] ⚠️  跳过异常坐标车票: {ride.TrainNo} {ride.DepartStation}({departLat},{departLng}) -> {ride.ArriveStation}({arriveLat},{arriveLng})");

                        // 收集缺少有效坐标的车站
                        if (!departInChina && !string.IsNullOrEmpty(ride.DepartStation) &&
                            !result.StationsWithoutCoordinates.Contains(ride.DepartStation))
                            result.StationsWithoutCoordinates.Add(ride.DepartStation);
                        if (!arriveInChina && !string.IsNullOrEmpty(ride.ArriveStation) &&
                            !result.StationsWithoutCoordinates.Contains(ride.ArriveStation))
                            result.StationsWithoutCoordinates.Add(ride.ArriveStation);

                        continue;
                    }

                    result.ValidTickets.Add(new MapTicketData
                    {
                        Id = ride.Id.ToString(),
                        TrainNo = ride.TrainNo ?? string.Empty,
                        DepartStation = ride.DepartStation ?? string.Empty,
                        ArriveStation = ride.ArriveStation ?? string.Empty,
                        DepartDate = RideDateTime.NormalizeDate(ride.DepartDate),
                        DepartTime = RideDateTime.NormalizeTime(ride.DepartTime ?? string.Empty),
                        ArriveTime = ArriveTimeFormat.Format(
                            RideDateTime.NormalizeTime(ride.ArriveTime ?? string.Empty),
                            ride.ArriveDayOffset),
                        Status = ConvertStatusToString(ride.Status),
                        SeatType = ride.SeatType ?? string.Empty,
                        Price = (double)ride.Money,
                        DepartLat = departLat,
                        DepartLng = departLng,
                        ArriveLat = arriveLat,
                        ArriveLng = arriveLng
                    });
                }
            }

            Debug.WriteLine(
                $"[MapDataService] 返回结果: {result.ValidTickets.Count} 条有效车票, {result.StationsWithoutCoordinates.Count} 个缺少经纬度的车站");

            // 打印所有车票的坐标，并特别标记可能异常的
            Debug.WriteLine("[MapDataService] 所有车票的坐标信息:");
            for (var i = 0; i < result.ValidTickets.Count; i++)
            {
                var ticket = result.ValidTickets[i];
                var hasIssue = false;
                var issueMsg = "";

                // 检查经度是否在正常中国范围外
                if (ticket.DepartLng < 70 || ticket.DepartLng > 135)
                {
                    hasIssue = true;
                    issueMsg += $" 出发站经度异常({ticket.DepartLng})";
                }

                if (ticket.ArriveLng < 70 || ticket.ArriveLng > 135)
                {
                    hasIssue = true;
                    issueMsg += $" 到达站经度异常({ticket.ArriveLng})";
                }

                if (ticket.DepartLat < 15 || ticket.DepartLat > 55)
                {
                    hasIssue = true;
                    issueMsg += $" 出发站纬度异常({ticket.DepartLat})";
                }

                if (ticket.ArriveLat < 15 || ticket.ArriveLat > 55)
                {
                    hasIssue = true;
                    issueMsg += $" 到达站纬度异常({ticket.ArriveLat})";
                }

                var prefix = hasIssue ? "⚠️" : "  ";
                Debug.WriteLine(
                    $"{prefix} {i + 1}. {ticket.TrainNo}: {ticket.DepartStation}({ticket.DepartLat:F6},{ticket.DepartLng:F6}) -> {ticket.ArriveStation}({ticket.ArriveLat:F6},{ticket.ArriveLng:F6}){issueMsg}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MapDataService] 加载地图数据失败: {ex}");
            throw;
        }
    }

    /// <summary>
    ///     获取车站信息（优先通过车站代码，其次通过车站名称）
    /// </summary>
    private StationInfo? GetStationInfo(string? stationName, Dictionary<string, StationInfo> stationDict)
    {
        if (string.IsNullOrEmpty(stationName))
            return null;

        // 尝试通过名称查找
        if (stationDict.TryGetValue(stationName, out var station)) return station;

        return null;
    }

    /// <summary>
    ///     尝试解析经纬度字符串
    /// </summary>
    private bool TryParseCoordinates(string? longitude, string? latitude, out double lng, out double lat)
    {
        lng = 0;
        lat = 0;

        if (string.IsNullOrWhiteSpace(longitude) || string.IsNullOrWhiteSpace(latitude))
            return false;

        double val1 = 0;
        double val2 = 0;

        // 先解析两个值
        var success1 = double.TryParse(longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out val1);
        var success2 = double.TryParse(latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out val2);

        if (!success1 || !success2)
        {
            success1 = double.TryParse(longitude, out val1);
            success2 = double.TryParse(latitude, out val2);
        }

        if (!success1 || !success2)
            return false;

        // 尝试两种组合：(val1=经度, val2=纬度) 和 (val1=纬度, val2=经度)
        var option1_lng = val1;
        var option1_lat = val2;
        var option1_valid = option1_lat >= -90 && option1_lat <= 90 && option1_lng >= -180 && option1_lng <= 180;
        var option1_inChina = option1_lat >= 15 && option1_lat <= 55 && option1_lng >= 70 && option1_lng <= 140;

        var option2_lng = val2;
        var option2_lat = val1;
        var option2_valid = option2_lat >= -90 && option2_lat <= 90 && option2_lng >= -180 && option2_lng <= 180;
        var option2_inChina = option2_lat >= 15 && option2_lat <= 55 && option2_lng >= 70 && option2_lng <= 140;

        // 优先选择在中国范围内的
        if (option1_inChina)
        {
            lng = option1_lng;
            lat = option1_lat;
            return true;
        }

        if (option2_inChina)
        {
            Debug.WriteLine(
                $"[MapDataService] 检测到经纬度反了，已交换: 原经度={longitude}, 原纬度={latitude} -> 新经度={option2_lng:F6}, 新纬度={option2_lat:F6}");
            lng = option2_lng;
            lat = option2_lat;
            return true;
        }

        // 如果都不在中国，选择纬度绝对值较小的那个（因为纬度范围是-90~90，经度是-180~180）
        if (option1_valid && Math.Abs(option1_lat) <= Math.Abs(option2_lat))
        {
            lng = option1_lng;
            lat = option1_lat;
            return true;
        }

        if (option2_valid)
        {
            Debug.WriteLine(
                $"[MapDataService] 经纬度可能反了，尝试交换: 原经度={longitude}, 原纬度={latitude} -> 新经度={option2_lng:F6}, 新纬度={option2_lat:F6}");
            lng = option2_lng;
            lat = option2_lat;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     将状态枚举转换为字符串
    /// </summary>
    private string ConvertStatusToString(int status)
    {
        return status switch
        {
            (int)TrainRideStatus.NotTraveled => "未出行",
            (int)TrainRideStatus.Completed => "已完成",
            (int)TrainRideStatus.Rescheduled => "已改签",
            (int)TrainRideStatus.Refunded => "已退票",
            _ => "未出行"
        };
    }
}

/// <summary>
///     地图数据加载结果
/// </summary>
public class MapDataResult
{
    /// <summary>
    ///     有效的车票列表（包含完整经纬度信息）
    /// </summary>
    public List<MapTicketData> ValidTickets { get; set; } = new();

    /// <summary>
    ///     缺少经纬度信息的车站名称列表
    /// </summary>
    public List<string> StationsWithoutCoordinates { get; set; } = new();
}

/// <summary>
///     地图车票数据模型
/// </summary>
public class MapTicketData
{
    public string Id { get; set; } = string.Empty;
    public string TrainNo { get; set; } = string.Empty;
    public string DepartStation { get; set; } = string.Empty;
    public string ArriveStation { get; set; } = string.Empty;
    public string DepartDate { get; set; } = string.Empty;
    public string DepartTime { get; set; } = string.Empty;
    public string ArriveTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SeatType { get; set; } = string.Empty;
    public double Price { get; set; }
    public double DepartLat { get; set; }
    public double DepartLng { get; set; }
    public double ArriveLat { get; set; }
    public double ArriveLng { get; set; }
}