using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GuiPiao.Model;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>
///     表格导入（CSV / Excel）共用：按表头别名映射列，再落到同一套 <see cref="TrainRideInfo"/>。
/// </summary>
public static class TableTicketImportParser
{
    /// <summary>简易 CSV 导出列顺序（无表头识别时的回退）。</summary>
    public static readonly string[] LegacyCsvHeaders =
    {
        "取票号", "检票位置", "出发车站", "车次号", "到达车站", "出发日期", "出发时间",
        "到达时间", "到达跨天", "车厢号", "座位号", "金额", "席别"
    };

    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(TrainRideInfo.TicketNumber)] = ["取票号", "票号"],
        [nameof(TrainRideInfo.CheckInLocation)] = ["检票位置", "检票口"],
        [nameof(TrainRideInfo.DepartStation)] = ["出发车站", "出发站"],
        [nameof(TrainRideInfo.TrainNo)] = ["车次号", "车次"],
        [nameof(TrainRideInfo.ArriveStation)] = ["到达车站", "到达站"],
        [nameof(TrainRideInfo.DepartDate)] = ["出发日期"],
        [nameof(TrainRideInfo.DepartTime)] = ["出发时间"],
        [nameof(TrainRideInfo.ArriveTime)] = ["到达时间"],
        [nameof(TrainRideInfo.ArriveDayOffset)] = ["到达跨天", "跨天"],
        [nameof(TrainRideInfo.CoachNo)] = ["车厢号", "车厢"],
        [nameof(TrainRideInfo.SeatNo)] = ["座位号", "座位"],
        [nameof(TrainRideInfo.Money)] = ["金额", "票价"],
        [nameof(TrainRideInfo.SeatType)] = ["席别"],
        [nameof(TrainRideInfo.AdditionalInfo)] = ["备注", "附加信息"]
    };

    private static readonly Regex ArriveDisplayRegex =
        new(@"^\s*(?<time>\d{1,2}:\d{2})(?:\s*\(\+(?<offset>[12])\))?\s*$", RegexOptions.Compiled);

    /// <summary>
    ///     将表头 + 数据行解析为行程列表。表头无法识别时，按 LegacyCsvHeaders 列序回退。
    /// </summary>
    public static IReadOnlyList<TrainRideInfo> ParseRows(IReadOnlyList<string>? headers, IEnumerable<IReadOnlyList<string>> dataRows)
    {
        var map = BuildColumnMap(headers);
        var useLegacy = map.Count == 0;
        var results = new List<TrainRideInfo>();

        foreach (var row in dataRows)
        {
            if (row == null || row.Count == 0)
                continue;

            var ride = useLegacy ? FromLegacyColumns(row) : FromMappedColumns(row, map);
            if (ride == null || IsBlankRide(ride))
                continue;

            results.Add(ride);
        }

        return results;
    }

    /// <summary>解析一行 CSV（支持双引号包裹、字段内逗号）。</summary>
    public static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        if (line == null)
            return fields;

        var sb = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }

    public static decimal ParseMoney(string? raw)
    {
        return MoneyFormat.TryParse(raw, out var money) ? money : 0m;
    }

    /// <summary>解析到达时间；支持 <c>04:52</c> 与导出展示 <c>04:52(+1)</c>。</summary>
    public static (string Time, int DayOffset) ParseArriveTime(string? raw, string? offsetCell)
    {
        var offsetFromCell = 0;
        if (!string.IsNullOrWhiteSpace(offsetCell) && int.TryParse(offsetCell.Trim(), out var parsedOffset))
            offsetFromCell = ArriveTimeFormat.NormalizeOffset(parsedOffset);

        if (string.IsNullOrWhiteSpace(raw))
            return (string.Empty, offsetFromCell);

        var m = ArriveDisplayRegex.Match(raw);
        if (m.Success)
        {
            var time = RideDateTime.NormalizeTime(m.Groups["time"].Value);
            var offset = offsetFromCell;
            if (m.Groups["offset"].Success && int.TryParse(m.Groups["offset"].Value, out var fromDisplay))
                offset = ArriveTimeFormat.NormalizeOffset(fromDisplay);
            return (time, offset);
        }

        return (RideDateTime.NormalizeTime(raw), offsetFromCell);
    }

    private static Dictionary<string, int> BuildColumnMap(IReadOnlyList<string>? headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (headers == null || headers.Count == 0)
            return map;

        for (var i = 0; i < headers.Count; i++)
        {
            var header = (headers[i] ?? string.Empty).Trim().TrimStart('\uFEFF');
            if (string.IsNullOrEmpty(header))
                continue;

            foreach (var (field, aliases) in HeaderAliases)
            {
                if (map.ContainsKey(field))
                    continue;
                if (aliases.Any(a => string.Equals(a, header, StringComparison.OrdinalIgnoreCase)))
                    map[field] = i;
            }
        }

        // 至少认出车次或起讫站，才视为「有表头」；否则走 Legacy 列序。
        var recognized = map.ContainsKey(nameof(TrainRideInfo.TrainNo))
                         || (map.ContainsKey(nameof(TrainRideInfo.DepartStation))
                             && map.ContainsKey(nameof(TrainRideInfo.ArriveStation)));
        return recognized ? map : new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    private static TrainRideInfo? FromLegacyColumns(IReadOnlyList<string> parts)
    {
        if (parts.Count < 13)
            return null;

        var (arriveTime, dayOffset) = ParseArriveTime(Cell(parts, 7), Cell(parts, 8));
        return new TrainRideInfo
        {
            TicketNumber = Cell(parts, 0),
            CheckInLocation = Cell(parts, 1),
            DepartStation = Cell(parts, 2),
            TrainNo = Cell(parts, 3),
            ArriveStation = Cell(parts, 4),
            DepartDate = RideDateTime.NormalizeDate(Cell(parts, 5)),
            DepartTime = RideDateTime.NormalizeTime(Cell(parts, 6)),
            ArriveTime = arriveTime,
            ArriveDayOffset = dayOffset,
            CoachNo = Cell(parts, 9),
            SeatNo = Cell(parts, 10),
            Money = ParseMoney(Cell(parts, 11)),
            SeatType = Cell(parts, 12)
        };
    }

    private static TrainRideInfo FromMappedColumns(IReadOnlyList<string> row, Dictionary<string, int> map)
    {
        var arriveRaw = Get(row, map, nameof(TrainRideInfo.ArriveTime));
        var offsetRaw = Get(row, map, nameof(TrainRideInfo.ArriveDayOffset));
        var (arriveTime, dayOffset) = ParseArriveTime(arriveRaw, offsetRaw);

        return new TrainRideInfo
        {
            TicketNumber = Get(row, map, nameof(TrainRideInfo.TicketNumber)),
            CheckInLocation = Get(row, map, nameof(TrainRideInfo.CheckInLocation)),
            DepartStation = Get(row, map, nameof(TrainRideInfo.DepartStation)),
            TrainNo = Get(row, map, nameof(TrainRideInfo.TrainNo)),
            ArriveStation = Get(row, map, nameof(TrainRideInfo.ArriveStation)),
            DepartDate = RideDateTime.NormalizeDate(Get(row, map, nameof(TrainRideInfo.DepartDate))),
            DepartTime = RideDateTime.NormalizeTime(Get(row, map, nameof(TrainRideInfo.DepartTime))),
            ArriveTime = arriveTime,
            ArriveDayOffset = dayOffset,
            CoachNo = Get(row, map, nameof(TrainRideInfo.CoachNo)),
            SeatNo = Get(row, map, nameof(TrainRideInfo.SeatNo)),
            Money = ParseMoney(Get(row, map, nameof(TrainRideInfo.Money))),
            SeatType = Get(row, map, nameof(TrainRideInfo.SeatType)),
            AdditionalInfo = Get(row, map, nameof(TrainRideInfo.AdditionalInfo))
        };
    }

    private static string Get(IReadOnlyList<string> row, Dictionary<string, int> map, string field)
    {
        return map.TryGetValue(field, out var index) ? Cell(row, index) : string.Empty;
    }

    private static string Cell(IReadOnlyList<string> row, int index)
    {
        if (index < 0 || index >= row.Count)
            return string.Empty;
        return (row[index] ?? string.Empty).Trim();
    }

    private static bool IsBlankRide(TrainRideInfo ride)
    {
        return string.IsNullOrWhiteSpace(ride.TrainNo)
               && string.IsNullOrWhiteSpace(ride.DepartStation)
               && string.IsNullOrWhiteSpace(ride.ArriveStation)
               && string.IsNullOrWhiteSpace(ride.TicketNumber);
    }
}
