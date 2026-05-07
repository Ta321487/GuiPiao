using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Model;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

public class TrainRideRepository
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

    /// <summary>
    ///     获取排序列的 SQL 表达式（处理日期和时间字段）
    /// </summary>
    private string GetOrderByColumn(string sortColumn)
    {
        return sortColumn.ToLower() switch
        {
            "date" => "DATE(depart_date)",
            "departtime" => "TIME(depart_time)",
            "money" => "money",
            "train_no" => "train_no",
            "depart_station" => "depart_station",
            _ => "id"
        };
    }

    /// <summary>
    ///     标准化日期格式为 yyyy-MM-dd
    /// </summary>
    private string NormalizeDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return dateStr;

        // 尝试解析各种日期格式
        string[] formats = { "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy" };

        if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsedDate)) return parsedDate.ToString("yyyy-MM-dd");

        // 如果无法解析，尝试通用解析
        if (DateTime.TryParse(dateStr, out var generalParsedDate)) return generalParsedDate.ToString("yyyy-MM-dd");

        // 返回原始值（可能格式不正确，但避免数据丢失）
        return dateStr;
    }

    public async Task<int> AddTrainRideAsync(TrainRideInfo trainRide)
    {
        // 标准化日期格式
        trainRide.DepartDate = NormalizeDate(trainRide.DepartDate);

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    INSERT INTO train_ride_info (
                        ticket_number, check_in_location, depart_station, train_no, arrive_station,
                        depart_station_pinyin, arrive_station_pinyin, depart_date, depart_time, coach_no,
                        seat_no, money, seat_type, additional_info, ticket_purpose, ticket_modification_type,
                        ticket_type_flags, payment_channel_flags, hint, depart_station_code, arrive_station_code, status
                    ) VALUES (
                        @TicketNumber, @CheckInLocation, @DepartStation, @TrainNo, @ArriveStation,
                        @DepartStationPinyin, @ArriveStationPinyin, @DepartDate, @DepartTime, @CoachNo,
                        @SeatNo, @Money, @SeatType, @AdditionalInfo, @TicketPurpose, @TicketModificationType,
                        @TicketTypeFlags, @PaymentChannelFlags, @Hint, @DepartStationCode, @ArriveStationCode, @Status
                    );
                    SELECT last_insert_rowid();
                ";
            return await connection.QuerySingleAsync<int>(sql, trainRide);
        }
    }


    public async Task<TrainRideInfo> GetTrainRideByIdAsync(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 查询车票基本信息
            var sql = @"SELECT 
                    id AS Id, 
                    ticket_number AS TicketNumber, 
                    check_in_location AS CheckInLocation, 
                    depart_station AS DepartStation, 
                    train_no AS TrainNo, 
                    arrive_station AS ArriveStation, 
                    depart_station_pinyin AS DepartStationPinyin, 
                    arrive_station_pinyin AS ArriveStationPinyin, 
                    depart_date AS DepartDate, 
                    depart_time AS DepartTime, 
                    coach_no AS CoachNo, 
                    seat_no AS SeatNo, 
                    money AS Money, 
                    seat_type AS SeatType, 
                    additional_info AS AdditionalInfo, 
                    ticket_purpose AS TicketPurpose, 
                    ticket_modification_type AS TicketModificationType, 
                    ticket_type_flags AS TicketTypeFlags, 
                    payment_channel_flags AS PaymentChannelFlags, 
                    hint AS Hint, 
                    depart_station_code AS DepartStationCode, 
                    arrive_station_code AS ArriveStationCode,
                    status AS Status
                FROM train_ride_info WHERE id = @Id";

            var trainRide = await connection.QuerySingleOrDefaultAsync<TrainRideInfo>(sql, new { Id = id });

            if (trainRide != null)
            {
                // 查询标签关联
                var tagSql = @"
                        SELECT tt.id AS Id, tt.name AS Name, tt.color AS Color, 
                               tt.text_color AS TextColor, tt.sort_order AS SortOrder, 
                               tt.is_default AS IsDefault, tt.created_at AS CreatedAt
                        FROM ticket_tag tt
                        INNER JOIN train_ride_tag rt ON tt.id = rt.tag_id
                        WHERE rt.train_ride_id = @TrainRideId
                        ORDER BY tt.sort_order ASC, tt.id ASC";

                var tags = await connection.QueryAsync<TicketTag>(tagSql, new { TrainRideId = id });
                trainRide.Tags = tags.ToList();
            }

            return trainRide;
        }
    }

    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByDateAsync(string date)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"SELECT 
                    id AS Id, 
                    ticket_number AS TicketNumber, 
                    check_in_location AS CheckInLocation, 
                    depart_station AS DepartStation, 
                    train_no AS TrainNo, 
                    arrive_station AS ArriveStation, 
                    depart_station_pinyin AS DepartStationPinyin, 
                    arrive_station_pinyin AS ArriveStationPinyin, 
                    depart_date AS DepartDate, 
                    depart_time AS DepartTime, 
                    coach_no AS CoachNo, 
                    seat_no AS SeatNo, 
                    money AS Money, 
                    seat_type AS SeatType, 
                    additional_info AS AdditionalInfo, 
                    ticket_purpose AS TicketPurpose, 
                    ticket_modification_type AS TicketModificationType, 
                    ticket_type_flags AS TicketTypeFlags, 
                    payment_channel_flags AS PaymentChannelFlags, 
                    hint AS Hint, 
                    depart_station_code AS DepartStationCode, 
                    arrive_station_code AS ArriveStationCode,
                    status AS Status
                FROM train_ride_info WHERE depart_date = @Date";
            return await connection.QueryAsync<TrainRideInfo>(sql, new { Date = date });
        }
    }

    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByStationAsync(string station)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        id AS Id, 
                        ticket_number AS TicketNumber, 
                        check_in_location AS CheckInLocation, 
                        depart_station AS DepartStation, 
                        train_no AS TrainNo, 
                        arrive_station AS ArriveStation, 
                        depart_station_pinyin AS DepartStationPinyin, 
                        arrive_station_pinyin AS ArriveStationPinyin, 
                        depart_date AS DepartDate, 
                        depart_time AS DepartTime, 
                        coach_no AS CoachNo, 
                        seat_no AS SeatNo, 
                        money AS Money, 
                        seat_type AS SeatType, 
                        additional_info AS AdditionalInfo, 
                        ticket_purpose AS TicketPurpose, 
                        ticket_modification_type AS TicketModificationType, 
                        ticket_type_flags AS TicketTypeFlags, 
                        payment_channel_flags AS PaymentChannelFlags, 
                        hint AS Hint, 
                        depart_station_code AS DepartStationCode, 
                        arrive_station_code AS ArriveStationCode 
                    FROM train_ride_info
                    WHERE depart_station = @Station OR arrive_station = @Station
                ";
            return await connection.QueryAsync<TrainRideInfo>(sql, new { Station = station });
        }
    }

    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByTrainNoAsync(string trainNo)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"SELECT 
                    id AS Id, 
                    ticket_number AS TicketNumber, 
                    check_in_location AS CheckInLocation, 
                    depart_station AS DepartStation, 
                    train_no AS TrainNo, 
                    arrive_station AS ArriveStation, 
                    depart_station_pinyin AS DepartStationPinyin, 
                    arrive_station_pinyin AS ArriveStationPinyin, 
                    depart_date AS DepartDate, 
                    depart_time AS DepartTime, 
                    coach_no AS CoachNo, 
                    seat_no AS SeatNo, 
                    money AS Money, 
                    seat_type AS SeatType, 
                    additional_info AS AdditionalInfo, 
                    ticket_purpose AS TicketPurpose, 
                    ticket_modification_type AS TicketModificationType, 
                    ticket_type_flags AS TicketTypeFlags, 
                    payment_channel_flags AS PaymentChannelFlags, 
                    hint AS Hint, 
                    depart_station_code AS DepartStationCode, 
                    arrive_station_code AS ArriveStationCode,
                    status AS Status
                FROM train_ride_info WHERE train_no = @TrainNo";
            return await connection.QueryAsync<TrainRideInfo>(sql, new { TrainNo = trainNo });
        }
    }

    public async Task<int> UpdateTrainRideAsync(TrainRideInfo trainRide)
    {
        // 标准化日期格式
        trainRide.DepartDate = NormalizeDate(trainRide.DepartDate);

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    UPDATE train_ride_info
                    SET ticket_number = @TicketNumber, check_in_location = @CheckInLocation, depart_station = @DepartStation,
                        train_no = @TrainNo, arrive_station = @ArriveStation, depart_station_pinyin = @DepartStationPinyin,
                        arrive_station_pinyin = @ArriveStationPinyin, depart_date = @DepartDate, depart_time = @DepartTime,
                        coach_no = @CoachNo, seat_no = @SeatNo, money = @Money, seat_type = @SeatType,
                        additional_info = @AdditionalInfo, ticket_purpose = @TicketPurpose,
                        ticket_modification_type = @TicketModificationType, ticket_type_flags = @TicketTypeFlags,
                        payment_channel_flags = @PaymentChannelFlags, hint = @Hint, depart_station_code = @DepartStationCode,
                        arrive_station_code = @ArriveStationCode, status = @Status
                    WHERE id = @Id;
                ";
            return await connection.ExecuteAsync(sql, trainRide);
        }
    }

    public async Task<int> DeleteTrainRideAsync(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "DELETE FROM train_ride_info WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Id = id });
        }
    }

    /// <summary>
    ///     更新车票状态
    /// </summary>
    public async Task<int> UpdateStatusAsync(int id, int status)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "UPDATE train_ride_info SET status = @Status WHERE id = @Id";
            return await connection.ExecuteAsync(sql, new { Status = status, Id = id });
        }
    }

    /// <summary>
    ///     批量更新车票状态（使用事务）
    /// </summary>
    /// <param name="ids">车票ID列表</param>
    /// <param name="status">目标状态</param>
    /// <returns>更新的记录数</returns>
    public async Task<int> BatchUpdateStatusAsync(List<int> ids, int status)
    {
        if (ids == null || ids.Count == 0)
            return 0;

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var sql = "UPDATE train_ride_info SET status = @Status WHERE id = @Id";
                    var totalAffected = 0;

                    foreach (var id in ids)
                        totalAffected += await connection.ExecuteAsync(sql,
                            new { Status = status, Id = id }, transaction);

                    transaction.Commit();
                    return totalAffected;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public async Task<IEnumerable<TrainRideInfo>> SearchTrainRidesAsync(string keyword)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        id AS Id, 
                        ticket_number AS TicketNumber, 
                        check_in_location AS CheckInLocation, 
                        depart_station AS DepartStation, 
                        train_no AS TrainNo, 
                        arrive_station AS ArriveStation, 
                        depart_station_pinyin AS DepartStationPinyin, 
                        arrive_station_pinyin AS ArriveStationPinyin, 
                        depart_date AS DepartDate, 
                        depart_time AS DepartTime, 
                        coach_no AS CoachNo, 
                        seat_no AS SeatNo, 
                        money AS Money, 
                        seat_type AS SeatType, 
                        additional_info AS AdditionalInfo, 
                        ticket_purpose AS TicketPurpose, 
                        ticket_modification_type AS TicketModificationType, 
                        ticket_type_flags AS TicketTypeFlags, 
                        payment_channel_flags AS PaymentChannelFlags, 
                        hint AS Hint, 
                        depart_station_code AS DepartStationCode, 
                        arrive_station_code AS ArriveStationCode,
                        status AS Status
                    FROM train_ride_info
                    WHERE ticket_number LIKE @Keyword OR train_no LIKE @Keyword OR depart_station LIKE @Keyword
                    OR arrive_station LIKE @Keyword OR coach_no LIKE @Keyword OR seat_no LIKE @Keyword
                ";
            return await connection.QueryAsync<TrainRideInfo>(sql, new { Keyword = $"%{keyword}%" });
        }
    }

    public async Task<IEnumerable<TrainRideInfo>> GetAllTrainRidesAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"SELECT 
                    id AS Id, 
                    ticket_number AS TicketNumber, 
                    check_in_location AS CheckInLocation, 
                    depart_station AS DepartStation, 
                    train_no AS TrainNo, 
                    arrive_station AS ArriveStation, 
                    depart_station_pinyin AS DepartStationPinyin, 
                    arrive_station_pinyin AS ArriveStationPinyin, 
                    depart_date AS DepartDate, 
                    depart_time AS DepartTime, 
                    coach_no AS CoachNo, 
                    seat_no AS SeatNo, 
                    money AS Money, 
                    seat_type AS SeatType, 
                    additional_info AS AdditionalInfo, 
                    ticket_purpose AS TicketPurpose, 
                    ticket_modification_type AS TicketModificationType, 
                    ticket_type_flags AS TicketTypeFlags, 
                    payment_channel_flags AS PaymentChannelFlags, 
                    hint AS Hint, 
                    depart_station_code AS DepartStationCode, 
                    arrive_station_code AS ArriveStationCode,
                    status AS Status
                FROM train_ride_info";
            return await connection.QueryAsync<TrainRideInfo>(sql);
        }
    }

    // 分页查询方法（支持排序）
    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByPageAsync(int pageIndex, int pageSize,
        string sortColumn = "id", bool sortDesc = true)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 获取排序列 SQL 表达式
            var orderByColumn = GetOrderByColumn(sortColumn);
            var sortDirection = sortDesc ? "DESC" : "ASC";
            Debug.WriteLine(
                $"[GetTrainRidesByPageAsync] sortColumn: {sortColumn}, orderByColumn: {orderByColumn}, sortDirection: {sortDirection}");

            var sql = $@"
                    SELECT 
                        id AS Id, 
                        ticket_number AS TicketNumber, 
                        check_in_location AS CheckInLocation, 
                        depart_station AS DepartStation, 
                        train_no AS TrainNo, 
                        arrive_station AS ArriveStation, 
                        depart_station_pinyin AS DepartStationPinyin, 
                        arrive_station_pinyin AS ArriveStationPinyin, 
                        depart_date AS DepartDate, 
                        depart_time AS DepartTime, 
                        coach_no AS CoachNo, 
                        seat_no AS SeatNo, 
                        money AS Money, 
                        seat_type AS SeatType, 
                        additional_info AS AdditionalInfo, 
                        ticket_purpose AS TicketPurpose, 
                        ticket_modification_type AS TicketModificationType, 
                        ticket_type_flags AS TicketTypeFlags, 
                        payment_channel_flags AS PaymentChannelFlags, 
                        hint AS Hint, 
                        depart_station_code AS DepartStationCode, 
                        arrive_station_code AS ArriveStationCode,
                        status AS Status
                    FROM train_ride_info
                    ORDER BY {orderByColumn} {sortDirection}
                    LIMIT @PageSize OFFSET @Offset
                ";
            var offset = (pageIndex - 1) * pageSize;
            return await connection.QueryAsync<TrainRideInfo>(sql, new { PageSize = pageSize, Offset = offset });
        }
    }

    // 兼容旧代码的重载方法
    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByPageAsync(int pageIndex, int pageSize)
    {
        return await GetTrainRidesByPageAsync(pageIndex, pageSize, "id");
    }

    // 分页查询方法（支持排序和日期范围）
    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByPageAsync(int pageIndex, int pageSize,
        string sortColumn, bool sortDesc, DateTime? startDate, DateTime? endDate)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 获取排序列 SQL 表达式
            var orderByColumn = GetOrderByColumn(sortColumn);
            var sortDirection = sortDesc ? "DESC" : "ASC";

            var whereClause = "";
            if (startDate.HasValue && endDate.HasValue)
                whereClause = "WHERE DATE(depart_date) >= DATE(@StartDate) AND DATE(depart_date) <= DATE(@EndDate)";

            var sql = $@"
                    SELECT 
                        id AS Id, 
                        ticket_number AS TicketNumber, 
                        check_in_location AS CheckInLocation, 
                        depart_station AS DepartStation, 
                        train_no AS TrainNo, 
                        arrive_station AS ArriveStation, 
                        depart_station_pinyin AS DepartStationPinyin, 
                        arrive_station_pinyin AS ArriveStationPinyin, 
                        depart_date AS DepartDate, 
                        depart_time AS DepartTime, 
                        coach_no AS CoachNo, 
                        seat_no AS SeatNo, 
                        money AS Money, 
                        seat_type AS SeatType, 
                        additional_info AS AdditionalInfo, 
                        ticket_purpose AS TicketPurpose, 
                        ticket_modification_type AS TicketModificationType, 
                        ticket_type_flags AS TicketTypeFlags, 
                        payment_channel_flags AS PaymentChannelFlags, 
                        hint AS Hint, 
                        depart_station_code AS DepartStationCode, 
                        arrive_station_code AS ArriveStationCode,
                        status AS Status
                    FROM train_ride_info
                    {whereClause}
                    ORDER BY {orderByColumn} {sortDirection}
                    LIMIT @PageSize OFFSET @Offset
                ";
            var offset = (pageIndex - 1) * pageSize;

            if (startDate.HasValue && endDate.HasValue)
                return await connection.QueryAsync<TrainRideInfo>(sql, new
                {
                    PageSize = pageSize,
                    Offset = offset,
                    StartDate = startDate.Value.ToString("yyyy-MM-dd"),
                    EndDate = endDate.Value.ToString("yyyy-MM-dd")
                });

            return await connection.QueryAsync<TrainRideInfo>(sql, new { PageSize = pageSize, Offset = offset });
        }
    }

    // 获取指定日期范围内的总记录数
    public async Task<int> GetTotalTrainRidesCountAsync(DateTime? startDate, DateTime? endDate)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            string sql;
            if (startDate.HasValue && endDate.HasValue)
            {
                sql =
                    "SELECT COUNT(*) FROM train_ride_info WHERE DATE(depart_date) >= DATE(@StartDate) AND DATE(depart_date) <= DATE(@EndDate)";
                return await connection.QuerySingleAsync<int>(sql, new
                {
                    StartDate = startDate.Value.ToString("yyyy-MM-dd"),
                    EndDate = endDate.Value.ToString("yyyy-MM-dd")
                });
            }

            sql = "SELECT COUNT(*) FROM train_ride_info";
            return await connection.QuerySingleAsync<int>(sql);
        }
    }

    // 获取总记录数
    public async Task<int> GetTotalTrainRidesCountAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "SELECT COUNT(*) FROM train_ride_info";
            return await connection.QuerySingleAsync<int>(sql);
        }
    }

    #region 标签相关查询（预留接口）

    /// <summary>
    ///     根据标签ID查询行程（预留接口）
    /// </summary>
    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesByTagIdAsync(int tagId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        t.id AS Id, 
                        t.ticket_number AS TicketNumber, 
                        t.check_in_location AS CheckInLocation, 
                        t.depart_station AS DepartStation, 
                        t.train_no AS TrainNo, 
                        t.arrive_station AS ArriveStation, 
                        t.depart_station_pinyin AS DepartStationPinyin, 
                        t.arrive_station_pinyin AS ArriveStationPinyin, 
                        t.depart_date AS DepartDate, 
                        t.depart_time AS DepartTime, 
                        t.coach_no AS CoachNo, 
                        t.seat_no AS SeatNo, 
                        t.money AS Money, 
                        t.seat_type AS SeatType, 
                        t.additional_info AS AdditionalInfo, 
                        t.ticket_purpose AS TicketPurpose, 
                        t.ticket_modification_type AS TicketModificationType, 
                        t.ticket_type_flags AS TicketTypeFlags, 
                        t.payment_channel_flags AS PaymentChannelFlags, 
                        t.hint AS Hint, 
                        t.depart_station_code AS DepartStationCode, 
                        t.arrive_station_code AS ArriveStationCode,
                        t.status AS Status
                    FROM train_ride_info t
                    INNER JOIN train_ride_tag rt ON t.id = rt.train_ride_id
                    WHERE rt.tag_id = @TagId
                    ORDER BY DATE(t.depart_date) DESC
                ";
            return await connection.QueryAsync<TrainRideInfo>(sql, new { TagId = tagId });
        }
    }

    /// <summary>
    ///     获取行程及其标签的完整信息（单条）
    /// </summary>
    public async Task<TrainRideInfo> GetTrainRideWithTagsAsync(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 获取行程基本信息
            var rideSql = @"SELECT 
                    id AS Id, 
                    ticket_number AS TicketNumber, 
                    check_in_location AS CheckInLocation, 
                    depart_station AS DepartStation, 
                    train_no AS TrainNo, 
                    arrive_station AS ArriveStation, 
                    depart_station_pinyin AS DepartStationPinyin, 
                    arrive_station_pinyin AS ArriveStationPinyin, 
                    depart_date AS DepartDate, 
                    depart_time AS DepartTime, 
                    coach_no AS CoachNo, 
                    seat_no AS SeatNo, 
                    money AS Money, 
                    seat_type AS SeatType, 
                    additional_info AS AdditionalInfo, 
                    ticket_purpose AS TicketPurpose, 
                    ticket_modification_type AS TicketModificationType, 
                    ticket_type_flags AS TicketTypeFlags, 
                    payment_channel_flags AS PaymentChannelFlags, 
                    hint AS Hint, 
                    depart_station_code AS DepartStationCode, 
                    arrive_station_code AS ArriveStationCode,
                    status AS Status
                FROM train_ride_info WHERE id = @Id";

            var ride = await connection.QuerySingleOrDefaultAsync<TrainRideInfo>(rideSql, new { Id = id });

            if (ride != null)
            {
                // 获取标签信息
                var tagSql = @"
                        SELECT 
                            tt.id AS Id, 
                            tt.name AS Name, 
                            tt.color AS Color, 
                            tt.text_color AS TextColor, 
                            tt.sort_order AS SortOrder, 
                            tt.created_at AS CreatedAt 
                        FROM ticket_tag tt
                        INNER JOIN train_ride_tag rt ON tt.id = rt.tag_id
                        WHERE rt.train_ride_id = @RideId
                        ORDER BY tt.sort_order ASC, tt.id ASC
                    ";
                var tags = await connection.QueryAsync<TicketTag>(tagSql, new { RideId = id });
                ride.Tags = tags.ToList();
            }

            return ride;
        }
    }

    /// <summary>
    ///     分页获取行程及其标签（用于列表展示）
    /// </summary>
    public async Task<IEnumerable<TrainRideInfo>> GetTrainRidesWithTagsByPageAsync(int pageIndex, int pageSize,
        string sortColumn = "id", bool sortDesc = true, DateTime? startDate = null, DateTime? endDate = null)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 获取排序列 SQL 表达式
            var orderByColumn = GetOrderByColumn(sortColumn);
            var sortDirection = sortDesc ? "DESC" : "ASC";
            Debug.WriteLine(
                $"[GetTrainRidesWithTagsByPageAsync] sortColumn: {sortColumn}, orderByColumn: {orderByColumn}, sortDirection: {sortDirection}");

            // 构建日期筛选条件
            var dateFilter = "";
            if (startDate.HasValue && endDate.HasValue)
                dateFilter = "WHERE depart_date >= @StartDate AND depart_date <= @EndDate";

            // 获取行程列表
            var sql = $@"
                    SELECT 
                        id AS Id, 
                        ticket_number AS TicketNumber, 
                        check_in_location AS CheckInLocation, 
                        depart_station AS DepartStation, 
                        train_no AS TrainNo, 
                        arrive_station AS ArriveStation, 
                        depart_station_pinyin AS DepartStationPinyin, 
                        arrive_station_pinyin AS ArriveStationPinyin, 
                        depart_date AS DepartDate, 
                        depart_time AS DepartTime, 
                        coach_no AS CoachNo, 
                        seat_no AS SeatNo, 
                        money AS Money, 
                        seat_type AS SeatType, 
                        additional_info AS AdditionalInfo, 
                        ticket_purpose AS TicketPurpose, 
                        ticket_modification_type AS TicketModificationType, 
                        ticket_type_flags AS TicketTypeFlags, 
                        payment_channel_flags AS PaymentChannelFlags, 
                        hint AS Hint, 
                        depart_station_code AS DepartStationCode, 
                        arrive_station_code AS ArriveStationCode,
                        status AS Status
                    FROM train_ride_info
                    {dateFilter}
                    ORDER BY {orderByColumn} {sortDirection}
                    LIMIT @PageSize OFFSET @Offset
                ";
            var offset = (pageIndex - 1) * pageSize;
            var rides = (await connection.QueryAsync<TrainRideInfo>(sql, new
            {
                PageSize = pageSize,
                Offset = offset,
                StartDate = startDate?.ToString("yyyy-MM-dd"),
                EndDate = endDate?.ToString("yyyy-MM-dd")
            })).ToList();

            // 批量获取标签
            Debug.WriteLine($"[GetTrainRidesWithTagsByPageAsync] 获取到 {rides.Count} 条行程");
            if (rides.Any())
            {
                var rideIds = rides.Select(r => r.Id).ToList();
                Debug.WriteLine($"[GetTrainRidesWithTagsByPageAsync] 行程ID列表: {string.Join(",", rideIds)}");

                var tagSql = @"
                        SELECT 
                            rt.train_ride_id AS TrainRideId,
                            tt.id AS Id, 
                            tt.name AS Name, 
                            tt.color AS Color, 
                            tt.text_color AS TextColor, 
                            tt.sort_order AS SortOrder, 
                            tt.is_default AS IsDefault,
                            tt.created_at AS CreatedAt 
                        FROM ticket_tag tt
                        INNER JOIN train_ride_tag rt ON tt.id = rt.tag_id
                        WHERE rt.train_ride_id IN @RideIds
                        ORDER BY tt.sort_order ASC, tt.id ASC
                    ";
                Debug.WriteLine($"[GetTrainRidesWithTagsByPageAsync] 标签查询SQL: {tagSql}");

                var tagLookup = new Dictionary<int, List<TicketTag>>();
                var tagResults = await connection.QueryAsync<dynamic>(tagSql, new { RideIds = rideIds });
                var tagList = tagResults.ToList();
                Debug.WriteLine($"[GetTrainRidesWithTagsByPageAsync] 查询到 {tagList.Count} 条标签关联记录");

                foreach (var row in tagList)
                {
                    var rideId = (int)row.TrainRideId;
                    if (!tagLookup.ContainsKey(rideId)) tagLookup[rideId] = new List<TicketTag>();

                    tagLookup[rideId].Add(new TicketTag
                    {
                        Id = (int)row.Id,
                        Name = row.Name,
                        Color = row.Color,
                        TextColor = row.TextColor,
                        SortOrder = (int)row.SortOrder,
                        IsDefault = row.IsDefault == 1,
                        CreatedAt = row.CreatedAt
                    });
                }

                // 将标签分配给对应的行程
                foreach (var ride in rides)
                    if (tagLookup.ContainsKey(ride.Id))
                    {
                        ride.Tags = tagLookup[ride.Id];
                        Debug.WriteLine($"[GetTrainRidesWithTagsByPageAsync] 行程 {ride.Id} 分配了 {ride.Tags.Count} 个标签");
                    }
                    else
                    {
                        Debug.WriteLine($"[GetTrainRidesWithTagsByPageAsync] 行程 {ride.Id} 没有标签");
                    }
            }

            return rides;
        }
    }

    #endregion

    #region 统计查询方法（高性能，避免加载全部数据）

    /// <summary>
    ///     按月统计行程数量
    /// </summary>
    public async Task<Dictionary<int, int>> GetMonthlyTripCountsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        CAST(strftime('%m', depart_date) AS INTEGER) AS Month,
                        COUNT(*) AS Count
                    FROM train_ride_info
                    WHERE depart_date IS NOT NULL AND depart_date != ''
                    GROUP BY Month
                    ORDER BY Month
                ";
            var results = await connection.QueryAsync<(int Month, int Count)>(sql);
            return results.ToDictionary(r => r.Month, r => r.Count);
        }
    }

    /// <summary>
    ///     统计车次类型占比
    /// </summary>
    public async Task<(int GCount, int DCount, int OtherCount)> GetTrainTypeCountsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        SUM(CASE WHEN UPPER(SUBSTR(train_no, 1, 1)) = 'G' THEN 1 ELSE 0 END) AS GCount,
                        SUM(CASE WHEN UPPER(SUBSTR(train_no, 1, 1)) = 'D' THEN 1 ELSE 0 END) AS DCount,
                        SUM(CASE WHEN UPPER(SUBSTR(train_no, 1, 1)) NOT IN ('G', 'D') OR train_no IS NULL THEN 1 ELSE 0 END) AS OtherCount
                    FROM train_ride_info
                ";
            return await connection.QuerySingleAsync<(int GCount, int DCount, int OtherCount)>(sql);
        }
    }

    /// <summary>
    ///     获取热门车站（出发+到达）
    /// </summary>
    public async Task<List<(string StationName, int Count)>> GetHotStationsAsync(int topCount = 5)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT station_name AS StationName, SUM(count) AS Count
                    FROM (
                        SELECT depart_station AS station_name, COUNT(*) AS count
                        FROM train_ride_info
                        WHERE depart_station IS NOT NULL AND depart_station != ''
                        GROUP BY depart_station
                        UNION ALL
                        SELECT arrive_station AS station_name, COUNT(*) AS count
                        FROM train_ride_info
                        WHERE arrive_station IS NOT NULL AND arrive_station != ''
                        GROUP BY arrive_station
                    )
                    GROUP BY station_name
                    ORDER BY Count DESC
                    LIMIT @TopCount
                ";
            var results =
                await connection.QueryAsync<(string StationName, int Count)>(sql, new { TopCount = topCount });
            return results.ToList();
        }
    }

    /// <summary>
    ///     统计指定日期范围内的记录数
    /// </summary>
    public async Task<int> CountByDateRangeAsync(string startDate, string endDate)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT COUNT(*) 
                    FROM train_ride_info 
                    WHERE depart_date >= @StartDate AND depart_date <= @EndDate
                ";
            return await connection.QuerySingleAsync<int>(sql, new { StartDate = startDate, EndDate = endDate });
        }
    }

    /// <summary>
    ///     计算指定日期范围内的总金额
    /// </summary>
    public async Task<decimal> CalculateTotalAmountByDateRangeAsync(string startDate, string endDate)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT COALESCE(SUM(money), 0) 
                    FROM train_ride_info 
                    WHERE depart_date >= @StartDate AND depart_date <= @EndDate
                ";
            return await connection.QuerySingleAsync<decimal>(sql, new { StartDate = startDate, EndDate = endDate });
        }
    }

    /// <summary>
    ///     获取热门出发车站
    /// </summary>
    public async Task<List<(string StationName, int Count)>> GetHotDepartStationsAsync(int topCount = 10)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT depart_station AS StationName, COUNT(*) AS Count
                    FROM train_ride_info
                    WHERE depart_station IS NOT NULL AND depart_station != ''
                    GROUP BY depart_station
                    ORDER BY Count DESC
                    LIMIT @TopCount
                ";
            var results =
                await connection.QueryAsync<(string StationName, int Count)>(sql, new { TopCount = topCount });
            return results.ToList();
        }
    }

    /// <summary>
    ///     获取热门到达车站
    /// </summary>
    public async Task<List<(string StationName, int Count)>> GetHotArriveStationsAsync(int topCount = 10)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT arrive_station AS StationName, COUNT(*) AS Count
                    FROM train_ride_info
                    WHERE arrive_station IS NOT NULL AND arrive_station != ''
                    GROUP BY arrive_station
                    ORDER BY Count DESC
                    LIMIT @TopCount
                ";
            var results =
                await connection.QueryAsync<(string StationName, int Count)>(sql, new { TopCount = topCount });
            return results.ToList();
        }
    }

    /// <summary>
    ///     根据行程信息查找ID（用于删除操作）
    /// </summary>
    public async Task<int?> FindTrainRideIdAsync(string trainNo, string departStation, string arriveStation,
        string departDate, string departTime)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT id 
                    FROM train_ride_info 
                    WHERE train_no = @TrainNo 
                        AND depart_station = @DepartStation 
                        AND arrive_station = @ArriveStation 
                        AND depart_date = @DepartDate 
                        AND depart_time = @DepartTime
                    LIMIT 1
                ";
            return await connection.QuerySingleOrDefaultAsync<int?>(sql, new
            {
                TrainNo = trainNo,
                DepartStation = departStation,
                ArriveStation = arriveStation,
                DepartDate = departDate,
                DepartTime = departTime
            });
        }
    }

    #endregion

    #region 高级检索方法

    /// <summary>
    ///     高级检索 - 支持多条件组合查询（带分页）
    /// </summary>
    public async Task<(IEnumerable<TrainRideInfo> Items, int TotalCount)> SearchTrainRidesAdvancedAsync(
        AdvancedSearchCriteria criteria,
        int pageIndex,
        int pageSize,
        string sortColumn = "id",
        bool sortDesc = true)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // 构建动态查询条件
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            // 出发站条件（模糊匹配）
            if (!string.IsNullOrWhiteSpace(criteria.DepartStation))
            {
                conditions.Add("depart_station LIKE @DepartStation");
                parameters.Add("DepartStation", $"%{criteria.DepartStation}%");
            }

            // 到达站条件（模糊匹配）
            if (!string.IsNullOrWhiteSpace(criteria.ArriveStation))
            {
                conditions.Add("arrive_station LIKE @ArriveStation");
                parameters.Add("ArriveStation", $"%{criteria.ArriveStation}%");
            }

            // 车次条件（支持前缀+数字组合查询）
            if (!string.IsNullOrWhiteSpace(criteria.TrainNoPrefix) &&
                !string.IsNullOrWhiteSpace(criteria.TrainNoNumber))
            {
                // 同时有前缀和数字：精确匹配前缀+模糊匹配数字
                conditions.Add("train_no LIKE @TrainNoPattern");
                parameters.Add("TrainNoPattern", $"{criteria.TrainNoPrefix}{criteria.TrainNoNumber}%");
            }
            else if (!string.IsNullOrWhiteSpace(criteria.TrainNoPrefix))
            {
                // 只有前缀：匹配该前缀的所有车次
                conditions.Add("train_no LIKE @TrainNoPattern");
                parameters.Add("TrainNoPattern", $"{criteria.TrainNoPrefix}%");
            }
            else if (!string.IsNullOrWhiteSpace(criteria.TrainNoNumber))
            {
                // 只有数字：模糊匹配数字部分
                conditions.Add("train_no LIKE @TrainNoPattern");
                parameters.Add("TrainNoPattern", $"%{criteria.TrainNoNumber}%");
            }
            else if (!string.IsNullOrWhiteSpace(criteria.TrainNo))
            {
                // 兼容旧逻辑：完整车次号模糊匹配
                conditions.Add("train_no LIKE @TrainNo");
                parameters.Add("TrainNo", $"%{criteria.TrainNo}%");
            }

            // 日期范围条件（预设范围）
            if (!string.IsNullOrWhiteSpace(criteria.DateRange))
            {
                var (startDate, endDate) = ParseDateRange(criteria.DateRange);
                conditions.Add("DATE(depart_date) >= DATE(@StartDate) AND DATE(depart_date) <= DATE(@EndDate)");
                parameters.Add("StartDate", startDate);
                parameters.Add("EndDate", endDate);
            }

            // 自定义日期范围条件
            if (!string.IsNullOrWhiteSpace(criteria.StartDate) && !string.IsNullOrWhiteSpace(criteria.EndDate))
            {
                conditions.Add(
                    "DATE(depart_date) >= DATE(@CustomStartDate) AND DATE(depart_date) <= DATE(@CustomEndDate)");
                parameters.Add("CustomStartDate", criteria.StartDate);
                parameters.Add("CustomEndDate", criteria.EndDate);
            }

            // 状态条件
            if (!string.IsNullOrWhiteSpace(criteria.Status) && criteria.Status != "全部")
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                if (criteria.Status == "未出行")
                {
                    conditions.Add("DATE(depart_date) >= DATE(@Today)");
                    parameters.Add("Today", today);
                }
                else if (criteria.Status == "已完成")
                {
                    conditions.Add("DATE(depart_date) < DATE(@Today)");
                    parameters.Add("Today", today);
                }
            }

            // 标签筛选条件
            var hasTagFilter = criteria.TagId.HasValue && criteria.TagId.Value > 0;

            // 构建WHERE子句
            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : "";

            // 如果有标签筛选，需要使用子查询或JOIN
            var fromClause = "FROM train_ride_info";
            if (hasTagFilter)
            {
                fromClause = @"FROM train_ride_info t
                        INNER JOIN train_ride_tag rt ON t.id = rt.train_ride_id";
                if (conditions.Count > 0)
                    whereClause += " AND rt.tag_id = @TagId";
                else
                    whereClause = "WHERE rt.tag_id = @TagId";
                parameters.Add("TagId", criteria.TagId.Value);
            }

            // 先查询总数
            var countSql = $"SELECT COUNT(DISTINCT train_ride_info.id) {fromClause} {whereClause}";
            if (hasTagFilter) countSql = $"SELECT COUNT(DISTINCT t.id) {fromClause} {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            // 获取排序列 SQL 表达式
            var orderByColumn = GetOrderByColumn(sortColumn);
            var sortDirection = sortDesc ? "DESC" : "ASC";

            // 查询分页数据
            string dataSql;
            if (hasTagFilter)
                dataSql = $@"
                        SELECT DISTINCT
                            t.id AS Id, 
                            t.ticket_number AS TicketNumber, 
                            t.check_in_location AS CheckInLocation, 
                            t.depart_station AS DepartStation, 
                            t.train_no AS TrainNo, 
                            t.arrive_station AS ArriveStation, 
                            t.depart_station_pinyin AS DepartStationPinyin, 
                            t.arrive_station_pinyin AS ArriveStationPinyin, 
                            t.depart_date AS DepartDate, 
                            t.depart_time AS DepartTime, 
                            t.coach_no AS CoachNo, 
                            t.seat_no AS SeatNo, 
                            t.money AS Money, 
                            t.seat_type AS SeatType, 
                            t.additional_info AS AdditionalInfo, 
                            t.ticket_purpose AS TicketPurpose, 
                            t.ticket_modification_type AS TicketModificationType, 
                            t.ticket_type_flags AS TicketTypeFlags, 
                            t.payment_channel_flags AS PaymentChannelFlags, 
                            t.hint AS Hint, 
                            t.depart_station_code AS DepartStationCode, 
                            t.arrive_station_code AS ArriveStationCode,
                            t.status AS Status
                        {fromClause}
                        {whereClause}
                        ORDER BY {orderByColumn} {sortDirection}
                        LIMIT @PageSize OFFSET @Offset";
            else
                dataSql = $@"
                        SELECT 
                            id AS Id, 
                            ticket_number AS TicketNumber, 
                            check_in_location AS CheckInLocation, 
                            depart_station AS DepartStation, 
                            train_no AS TrainNo, 
                            arrive_station AS ArriveStation, 
                            depart_station_pinyin AS DepartStationPinyin, 
                            arrive_station_pinyin AS ArriveStationPinyin, 
                            depart_date AS DepartDate, 
                            depart_time AS DepartTime, 
                            coach_no AS CoachNo, 
                            seat_no AS SeatNo, 
                            money AS Money, 
                            seat_type AS SeatType, 
                            additional_info AS AdditionalInfo, 
                            ticket_purpose AS TicketPurpose, 
                            ticket_modification_type AS TicketModificationType, 
                            ticket_type_flags AS TicketTypeFlags, 
                            payment_channel_flags AS PaymentChannelFlags, 
                            hint AS Hint, 
                            depart_station_code AS DepartStationCode, 
                            arrive_station_code AS ArriveStationCode,
                            status AS Status
                        {fromClause}
                        {whereClause}
                        ORDER BY {orderByColumn} {sortDirection}
                        LIMIT @PageSize OFFSET @Offset";

            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (pageIndex - 1) * pageSize);

            var items = await connection.QueryAsync<TrainRideInfo>(dataSql, parameters);

            return (items, totalCount);
        }
    }

    /// <summary>
    ///     解析日期范围
    /// </summary>
    private (string StartDate, string EndDate) ParseDateRange(string dateRange)
    {
        var today = DateTime.Today;
        return dateRange switch
        {
            "今年" => ($"{today.Year}-01-01", $"{today.Year}-12-31"),
            "去年" => ($"{today.Year - 1}-01-01", $"{today.Year - 1}-12-31"),
            "最近3个月" => (today.AddMonths(-3).ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd")),
            "最近6个月" => (today.AddMonths(-6).ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd")),
            _ => ("1900-01-01", "2099-12-31")
        };
    }

    /// <summary>
    ///     获取用户车票中出现过的所有出发站（去重）
    /// </summary>
    public async Task<List<string>> GetUserDepartStationsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT DISTINCT depart_station 
                    FROM train_ride_info 
                    WHERE depart_station IS NOT NULL AND depart_station != ''
                    ORDER BY depart_station
                ";
            var result = await connection.QueryAsync<string>(sql);
            return result.ToList();
        }
    }

    /// <summary>
    ///     获取用户车票中出现过的所有到达站（去重）
    /// </summary>
    public async Task<List<string>> GetUserArriveStationsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT DISTINCT arrive_station 
                    FROM train_ride_info 
                    WHERE arrive_station IS NOT NULL AND arrive_station != ''
                    ORDER BY arrive_station
                ";
            var result = await connection.QueryAsync<string>(sql);
            return result.ToList();
        }
    }

    /// <summary>
    ///     根据关键词搜索用户车票中的出发站
    /// </summary>
    public async Task<List<string>> SearchUserDepartStationsAsync(string keyword)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT DISTINCT depart_station 
                    FROM train_ride_info 
                    WHERE depart_station IS NOT NULL AND depart_station != ''
                        AND depart_station LIKE @Keyword
                    ORDER BY depart_station
                    LIMIT 10
                ";
            var result = await connection.QueryAsync<string>(sql, new { Keyword = $"%{keyword}%" });
            return result.ToList();
        }
    }

    /// <summary>
    ///     根据关键词搜索用户车票中的到达站
    /// </summary>
    public async Task<List<string>> SearchUserArriveStationsAsync(string keyword)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT DISTINCT arrive_station 
                    FROM train_ride_info 
                    WHERE arrive_station IS NOT NULL AND arrive_station != ''
                        AND arrive_station LIKE @Keyword
                    ORDER BY arrive_station
                    LIMIT 10
                ";
            var result = await connection.QueryAsync<string>(sql, new { Keyword = $"%{keyword}%" });
            return result.ToList();
        }
    }

    #endregion

    #region 日期范围查询

    /// <summary>
    ///     获取数据库中最早的车票出发日期
    /// </summary>
    public async Task<DateTime?> GetEarliestTicketDateAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT MIN(DATE(depart_date)) 
                    FROM train_ride_info 
                    WHERE depart_date IS NOT NULL AND depart_date != ''
                ";
            var result = await connection.ExecuteScalarAsync<string>(sql);
            if (DateTime.TryParse(result, out var date)) return date;
            return null;
        }
    }

    /// <summary>
    ///     获取数据库中最晚的车票出发日期
    /// </summary>
    public async Task<DateTime?> GetLatestTicketDateAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT MAX(DATE(depart_date)) 
                    FROM train_ride_info 
                    WHERE depart_date IS NOT NULL AND depart_date != ''
                ";
            var result = await connection.ExecuteScalarAsync<string>(sql);
            if (DateTime.TryParse(result, out var date)) return date;
            return null;
        }
    }

    /// <summary>
    ///     获取数据库中车票的日期范围（最早和最晚）
    /// </summary>
    public async Task<(DateTime? Earliest, DateTime? Latest)> GetTicketDateRangeAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        MIN(DATE(depart_date)) AS Earliest,
                        MAX(DATE(depart_date)) AS Latest
                    FROM train_ride_info 
                    WHERE depart_date IS NOT NULL AND depart_date != ''
                ";
            var result = await connection.QueryFirstOrDefaultAsync(sql);
            if (result == null) return (null, null);

            DateTime? earliest = null;
            DateTime? latest = null;

            if (DateTime.TryParse(result.Earliest?.ToString(), out DateTime e)) earliest = e;
            if (DateTime.TryParse(result.Latest?.ToString(), out DateTime l)) latest = l;

            return (earliest, latest);
        }
    }

    #endregion
}