using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GuiPiao.DataAccess;
using GuiPiao.Model;

namespace GuiPiao.Services;

public class TrainTicketService
{
    private readonly LogService _logService;
    private readonly Lazy<StationRepository> _stationRepository;
    private readonly Lazy<TrainRideRepository> _trainRideRepository;

    public TrainTicketService()
    {
        _trainRideRepository = new Lazy<TrainRideRepository>(() => new TrainRideRepository());
        _stationRepository = new Lazy<StationRepository>(() => new StationRepository());
        _logService = new LogService();
    }

    private TrainRideRepository TrainRideRepository => _trainRideRepository.Value;
    private StationRepository StationRepository => _stationRepository.Value;

    /// <summary>
    ///     导出火车票数据到CSV文件（使用流式读取，避免加载全部数据到内存）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否成功</returns>
    public async Task<bool> ExportToCsvAsync(string filePath)
    {
        try
        {
            // 使用分页方式导出，避免一次性加载全部数据
            var pageSize = 100;
            var pageIndex = 1;
            var totalExported = 0;

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("取票号,检票位置,出发车站,车次号,到达车站,出发日期,出发时间,车厢号,座位号,金额,席别");

                while (true)
                {
                    var trainRides = await TrainRideRepository.GetTrainRidesByPageAsync(pageIndex, pageSize);
                    var rides = trainRides.ToList();

                    if (rides.Count == 0)
                        break;

                    foreach (var ride in rides)
                        writer.WriteLine(
                            $"{ride.TicketNumber},{ride.CheckInLocation},{ride.DepartStation},{ride.TrainNo},{ride.ArriveStation},{ride.DepartDate},{ride.DepartTime},{ride.CoachNo},{ride.SeatNo},{ride.Money},{ride.SeatType}");

                    totalExported += rides.Count;

                    if (rides.Count < pageSize)
                        break;

                    pageIndex++;
                }
            }

            _logService.Info("TrainTicketService", $"导出CSV成功: {filePath}, 记录数: {totalExported}");
            return true;
        }
        catch (Exception ex)
        {
            _logService.Error("TrainTicketService", $"导出CSV失败: {ex.Message}");
            return false;
        }
    }

    public async Task<int> ImportFromCsvAsync(string filePath)
    {
        try
        {
            var count = 0;

            using (var reader = new StreamReader(filePath))
            {
                var line = await reader.ReadLineAsync();

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 11)
                    {
                        var trainRide = new TrainRideInfo
                        {
                            TicketNumber = parts[0],
                            CheckInLocation = parts[1],
                            DepartStation = parts[2],
                            TrainNo = parts[3],
                            ArriveStation = parts[4],
                            DepartDate = parts[5],
                            DepartTime = parts[6],
                            CoachNo = parts[7],
                            SeatNo = parts[8],
                            Money = decimal.TryParse(parts[9], out var money) ? money : 0m,
                            SeatType = parts[10]
                        };

                        await TrainRideRepository.AddTrainRideAsync(trainRide);
                        count++;
                    }
                }
            }

            _logService.Info("TrainTicketService", $"导入CSV成功: {filePath}, 导入记录数: {count}");
            return count;
        }
        catch (Exception ex)
        {
            _logService.Error("TrainTicketService", $"导入CSV失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    ///     统计指定日期范围内的火车票数量（使用SQL统计）
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>火车票数量</returns>
    public async Task<int> CountTrainRidesByDateRangeAsync(string startDate, string endDate)
    {
        return await TrainRideRepository.CountByDateRangeAsync(startDate, endDate);
    }

    /// <summary>
    ///     统计指定车站的出发火车票数量
    /// </summary>
    /// <param name="stationName">车站名称</param>
    /// <returns>火车票数量</returns>
    public async Task<int> CountTrainRidesByDepartStationAsync(string stationName)
    {
        var rides = await TrainRideRepository.GetTrainRidesByStationAsync(stationName);
        return rides.Count(r => r.DepartStation == stationName);
    }

    /// <summary>
    ///     统计指定车站的到达火车票数量
    /// </summary>
    /// <param name="stationName">车站名称</param>
    /// <returns>火车票数量</returns>
    public async Task<int> CountTrainRidesByArriveStationAsync(string stationName)
    {
        var rides = await TrainRideRepository.GetTrainRidesByStationAsync(stationName);
        return rides.Count(r => r.ArriveStation == stationName);
    }

    /// <summary>
    ///     计算指定日期范围内的火车票总金额（使用SQL统计）
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>总金额</returns>
    public async Task<decimal> CalculateTotalAmountByDateRangeAsync(string startDate, string endDate)
    {
        return await TrainRideRepository.CalculateTotalAmountByDateRangeAsync(startDate, endDate);
    }

    /// <summary>
    ///     获取热门出发车站（使用SQL统计）
    /// </summary>
    /// <param name="topCount">数量</param>
    /// <returns>热门车站列表</returns>
    public async Task<List<(string StationName, int Count)>> GetHotDepartStationsAsync(int topCount = 10)
    {
        return await TrainRideRepository.GetHotDepartStationsAsync(topCount);
    }

    /// <summary>
    ///     获取热门到达车站（使用SQL统计）
    /// </summary>
    /// <param name="topCount">数量</param>
    /// <returns>热门车站列表</returns>
    public async Task<List<(string StationName, int Count)>> GetHotArriveStationsAsync(int topCount = 10)
    {
        return await TrainRideRepository.GetHotArriveStationsAsync(topCount);
    }
}