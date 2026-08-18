using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

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
    public async Task<bool> ExportToCsvAsync(string filePath)
    {
        try
        {
            var pageSize = 100;
            var pageIndex = 1;
            var totalExported = 0;

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine(string.Join(",", TableTicketImportParser.LegacyCsvHeaders));

                while (true)
                {
                    var trainRides = await TrainRideRepository.GetTrainRidesByPageAsync(pageIndex, pageSize);
                    var rides = trainRides.ToList();

                    if (rides.Count == 0)
                        break;

                    foreach (var ride in rides)
                        writer.WriteLine(
                            $"{ride.TicketNumber},{ride.CheckInLocation},{ride.DepartStation},{ride.TrainNo},{ride.ArriveStation},{ride.DepartDate},{ride.DepartTime},{ride.ArriveTime},{ride.ArriveDayOffset},{ride.CoachNo},{ride.SeatNo},{ride.Money},{ride.SeatType}");

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

    /// <summary>
    ///     从表格文件导入行程。按扩展名选择 CSV / Excel 读表，字段映射与入库共用。
    /// </summary>
    public async Task<int> ImportFromTableAsync(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            IReadOnlyList<TrainRideInfo> rides = extension switch
            {
                ".csv" => await ReadCsvRowsAsync(filePath),
                ".xlsx" or ".xls" => await Task.Run(() => ReadExcelRows(filePath)),
                _ => throw new NotSupportedException($"不支持的表格格式: {extension}")
            };

            var count = 0;
            foreach (var trainRide in rides)
            {
                await TrainRideRepository.AddTrainRideAsync(trainRide);
                count++;
            }

            _logService.Info("TrainTicketService", $"表格导入成功: {filePath}, 导入记录数: {count}");
            return count;
        }
        catch (Exception ex)
        {
            _logService.Error("TrainTicketService", $"表格导入失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>兼容旧调用名，等价于 <see cref="ImportFromTableAsync"/>（仅 CSV）。</summary>
    public Task<int> ImportFromCsvAsync(string filePath) => ImportFromTableAsync(filePath);

    private static async Task<IReadOnlyList<TrainRideInfo>> ReadCsvRowsAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        var headerLine = await reader.ReadLineAsync();
        var headers = string.IsNullOrWhiteSpace(headerLine)
            ? null
            : (IReadOnlyList<string>)TableTicketImportParser.SplitCsvLine(headerLine);

        var dataRows = new List<IReadOnlyList<string>>();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            dataRows.Add(TableTicketImportParser.SplitCsvLine(line));
        }

        return TableTicketImportParser.ParseRows(headers, dataRows);
    }

    private static IReadOnlyList<TrainRideInfo> ReadExcelRows(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using IWorkbook workbook = Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase)
            ? new HSSFWorkbook(stream)
            : new XSSFWorkbook(stream);

        var sheet = workbook.NumberOfSheets > 0 ? workbook.GetSheetAt(0) : null;
        if (sheet == null)
            return Array.Empty<TrainRideInfo>();

        var formatter = new DataFormatter(CultureInfo.InvariantCulture);
        var headerRow = sheet.GetRow(sheet.FirstRowNum);
        IReadOnlyList<string>? headers = null;
        var startRow = sheet.FirstRowNum;

        if (headerRow != null)
        {
            headers = ReadExcelRow(headerRow, formatter);
            startRow = sheet.FirstRowNum + 1;
        }

        var dataRows = new List<IReadOnlyList<string>>();
        for (var r = startRow; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row == null)
                continue;
            dataRows.Add(ReadExcelRow(row, formatter));
        }

        return TableTicketImportParser.ParseRows(headers, dataRows);
    }

    private static List<string> ReadExcelRow(IRow row, DataFormatter formatter)
    {
        var last = row.LastCellNum > 0 ? row.LastCellNum : (short)0;
        var cells = new List<string>(last);
        for (var c = 0; c < last; c++)
        {
            var cell = row.GetCell(c);
            cells.Add(cell == null ? string.Empty : formatter.FormatCellValue(cell)?.Trim() ?? string.Empty);
        }

        return cells;
    }

    /// <summary>
    ///     统计指定日期范围内的火车票数量（使用SQL统计）
    /// </summary>
    public async Task<int> CountTrainRidesByDateRangeAsync(string startDate, string endDate)
    {
        return await TrainRideRepository.CountByDateRangeAsync(startDate, endDate);
    }

    public async Task<int> CountTrainRidesByDepartStationAsync(string stationName)
    {
        var rides = await TrainRideRepository.GetTrainRidesByStationAsync(stationName);
        return rides.Count(r => r.DepartStation == stationName);
    }

    public async Task<int> CountTrainRidesByArriveStationAsync(string stationName)
    {
        var rides = await TrainRideRepository.GetTrainRidesByStationAsync(stationName);
        return rides.Count(r => r.ArriveStation == stationName);
    }

    public async Task<decimal> CalculateTotalAmountByDateRangeAsync(string startDate, string endDate)
    {
        return await TrainRideRepository.CalculateTotalAmountByDateRangeAsync(startDate, endDate);
    }

    public async Task<List<(string StationName, int Count)>> GetHotDepartStationsAsync(int topCount = 10)
    {
        return await TrainRideRepository.GetHotDepartStationsAsync(topCount);
    }

    public async Task<List<(string StationName, int Count)>> GetHotArriveStationsAsync(int topCount = 10)
    {
        return await TrainRideRepository.GetHotArriveStationsAsync(topCount);
    }
}
