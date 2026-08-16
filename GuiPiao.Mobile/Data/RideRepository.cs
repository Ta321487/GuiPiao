using GuiPiao.Mobile.Model;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Mobile.Data;

public sealed class RideRepository
{
    private readonly MobileDatabase _db;

    public RideRepository(MobileDatabase db) => _db = db;

    public MobileRide? GetBySyncId(string syncId)
    {
        if (string.IsNullOrWhiteSpace(syncId)) return null;
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT * FROM train_ride_info
            WHERE sync_id = @sync_id
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@sync_id", syncId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    /// <summary>未软删的行程；详情/票面共用，避免各 VM 重复判 deleted_at。</summary>
    public MobileRide? GetActiveBySyncId(string syncId)
    {
        var ride = GetBySyncId(syncId);
        return ride == null || !string.IsNullOrWhiteSpace(ride.DeletedAt) ? null : ride;
    }

    public IReadOnlyList<MobileRide> ListActive(string? search = null, int limit = 500)
        => ListActivePage(search, pageIndex: 1, pageSize: limit, statusFilter: null);

    /// <summary>分页查询（pageIndex 从 1 起；默认每页 20，对齐 PC DefaultPageSize）。</summary>
    public IReadOnlyList<MobileRide> ListActivePage(
        string? search, int pageIndex, int pageSize, int? statusFilter = null)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 20;

        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        var offset = (pageIndex - 1) * pageSize;
        var where = BuildWhere(search, statusFilter, cmd);
        cmd.CommandText =
            $"""
            SELECT * FROM train_ride_info
            WHERE {where}
            ORDER BY depart_date DESC, depart_time DESC
            LIMIT @Limit OFFSET @Offset
            """;
        cmd.Parameters.AddWithValue("@Limit", pageSize);
        cmd.Parameters.AddWithValue("@Offset", offset);
        return ReadAll(cmd);
    }

    public int CountActive(string? search = null, int? statusFilter = null)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        var where = BuildWhere(search, statusFilter, cmd);
        cmd.CommandText = $"SELECT COUNT(1) FROM train_ride_info WHERE {where}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>按出发年份汇总行程数与票价（轻量统计）。</summary>
    public IReadOnlyList<(string Year, int Count, decimal Money)> StatsByDepartYear()
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT substr(depart_date, 1, 4) AS y, COUNT(1), COALESCE(SUM(money), 0)
            FROM train_ride_info
            WHERE (deleted_at IS NULL OR deleted_at = '')
              AND length(depart_date) >= 4
            GROUP BY y
            ORDER BY y DESC
            """;
        var list = new List<(string, int, decimal)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var year = reader.IsDBNull(0) ? "" : reader.GetString(0);
            if (string.IsNullOrWhiteSpace(year)) continue;
            list.Add((year, reader.GetInt32(1), reader.GetDecimal(2)));
        }

        return list;
    }

    private static string BuildWhere(string? search, int? statusFilter, SqliteCommand cmd)
    {
        var parts = new List<string> { "(deleted_at IS NULL OR deleted_at = '')" };
        if (statusFilter != null)
        {
            parts.Add("status = @Status");
            cmd.Parameters.AddWithValue("@Status", statusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add(
                """
                (
                  train_no LIKE @Q OR depart_station LIKE @Q OR arrive_station LIKE @Q
                  OR ticket_number LIKE @Q OR depart_date LIKE @Q
                )
                """);
            cmd.Parameters.AddWithValue("@Q", "%" + search.Trim() + "%");
        }

        return string.Join(" AND ", parts);
    }

    public void Upsert(MobileRide ride)
    {
        using var connection = _db.OpenConnection();
        using var tx = connection.BeginTransaction();
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText =
                """
                INSERT INTO train_ride_info (
                    sync_id, ticket_number, check_in_location, depart_station, train_no, arrive_station,
                    depart_station_pinyin, arrive_station_pinyin, depart_date, depart_time, arrive_time,
                    arrive_day_offset, coach_no, seat_no, money, seat_type, additional_info,
                    ticket_purpose, ticket_modification_type, ticket_type_flags, payment_channel_flags,
                    hint, depart_station_code, arrive_station_code, status, updated_at, deleted_at
                ) VALUES (
                    @sync_id, @ticket_number, @check_in_location, @depart_station, @train_no, @arrive_station,
                    @depart_station_pinyin, @arrive_station_pinyin, @depart_date, @depart_time, @arrive_time,
                    @arrive_day_offset, @coach_no, @seat_no, @money, @seat_type, @additional_info,
                    @ticket_purpose, @ticket_modification_type, @ticket_type_flags, @payment_channel_flags,
                    @hint, @depart_station_code, @arrive_station_code, @status, @updated_at, @deleted_at
                )
                ON CONFLICT(sync_id) DO UPDATE SET
                    ticket_number=excluded.ticket_number,
                    check_in_location=excluded.check_in_location,
                    depart_station=excluded.depart_station,
                    train_no=excluded.train_no,
                    arrive_station=excluded.arrive_station,
                    depart_station_pinyin=excluded.depart_station_pinyin,
                    arrive_station_pinyin=excluded.arrive_station_pinyin,
                    depart_date=excluded.depart_date,
                    depart_time=excluded.depart_time,
                    arrive_time=excluded.arrive_time,
                    arrive_day_offset=excluded.arrive_day_offset,
                    coach_no=excluded.coach_no,
                    seat_no=excluded.seat_no,
                    money=excluded.money,
                    seat_type=excluded.seat_type,
                    additional_info=excluded.additional_info,
                    ticket_purpose=excluded.ticket_purpose,
                    ticket_modification_type=excluded.ticket_modification_type,
                    ticket_type_flags=excluded.ticket_type_flags,
                    payment_channel_flags=excluded.payment_channel_flags,
                    hint=excluded.hint,
                    depart_station_code=excluded.depart_station_code,
                    arrive_station_code=excluded.arrive_station_code,
                    status=excluded.status,
                    updated_at=excluded.updated_at,
                    deleted_at=excluded.deleted_at
                """;
            Bind(cmd, ride);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public void SoftDelete(string syncId, string? deletedAt)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            UPDATE train_ride_info
            SET deleted_at = @deleted_at, updated_at = @updated_at
            WHERE sync_id = @sync_id
            """;
        var at = string.IsNullOrWhiteSpace(deletedAt) ? DateTime.UtcNow.ToString("o") : deletedAt;
        cmd.Parameters.AddWithValue("@deleted_at", at);
        cmd.Parameters.AddWithValue("@updated_at", at);
        cmd.Parameters.AddWithValue("@sync_id", syncId);
        cmd.ExecuteNonQuery();
    }

    private static void Bind(SqliteCommand cmd, MobileRide ride)
    {
        cmd.Parameters.AddWithValue("@sync_id", ride.SyncId);
        cmd.Parameters.AddWithValue("@ticket_number", ride.TicketNumber ?? "");
        cmd.Parameters.AddWithValue("@check_in_location", ride.CheckInLocation ?? "");
        cmd.Parameters.AddWithValue("@depart_station", ride.DepartStation ?? "");
        cmd.Parameters.AddWithValue("@train_no", ride.TrainNo ?? "");
        cmd.Parameters.AddWithValue("@arrive_station", ride.ArriveStation ?? "");
        cmd.Parameters.AddWithValue("@depart_station_pinyin", ride.DepartStationPinyin ?? "");
        cmd.Parameters.AddWithValue("@arrive_station_pinyin", ride.ArriveStationPinyin ?? "");
        cmd.Parameters.AddWithValue("@depart_date", ride.DepartDate ?? "");
        cmd.Parameters.AddWithValue("@depart_time", ride.DepartTime ?? "");
        cmd.Parameters.AddWithValue("@arrive_time", ride.ArriveTime ?? "");
        cmd.Parameters.AddWithValue("@arrive_day_offset", ride.ArriveDayOffset);
        cmd.Parameters.AddWithValue("@coach_no", ride.CoachNo ?? "");
        cmd.Parameters.AddWithValue("@seat_no", ride.SeatNo ?? "");
        cmd.Parameters.AddWithValue("@money", ride.Money);
        cmd.Parameters.AddWithValue("@seat_type", ride.SeatType ?? "");
        cmd.Parameters.AddWithValue("@additional_info", ride.AdditionalInfo ?? "");
        cmd.Parameters.AddWithValue("@ticket_purpose", ride.TicketPurpose ?? "");
        cmd.Parameters.AddWithValue("@ticket_modification_type", ride.TicketModificationType ?? "");
        cmd.Parameters.AddWithValue("@ticket_type_flags", ride.TicketTypeFlags);
        cmd.Parameters.AddWithValue("@payment_channel_flags", ride.PaymentChannelFlags);
        cmd.Parameters.AddWithValue("@hint", ride.Hint ?? "");
        cmd.Parameters.AddWithValue("@depart_station_code", ride.DepartStationCode ?? "");
        cmd.Parameters.AddWithValue("@arrive_station_code", ride.ArriveStationCode ?? "");
        cmd.Parameters.AddWithValue("@status", ride.Status);
        cmd.Parameters.AddWithValue("@updated_at", ride.UpdatedAt ?? "");
        cmd.Parameters.AddWithValue("@deleted_at", (object?)ride.DeletedAt ?? DBNull.Value);
    }

    private static List<MobileRide> ReadAll(SqliteCommand cmd)
    {
        var list = new List<MobileRide>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(Map(reader));
        return list;
    }

    private static MobileRide Map(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("id")),
        SyncId = r["sync_id"]?.ToString() ?? "",
        TicketNumber = r["ticket_number"]?.ToString() ?? "",
        CheckInLocation = r["check_in_location"]?.ToString() ?? "",
        DepartStation = r["depart_station"]?.ToString() ?? "",
        TrainNo = r["train_no"]?.ToString() ?? "",
        ArriveStation = r["arrive_station"]?.ToString() ?? "",
        DepartStationPinyin = r["depart_station_pinyin"]?.ToString() ?? "",
        ArriveStationPinyin = r["arrive_station_pinyin"]?.ToString() ?? "",
        DepartDate = r["depart_date"]?.ToString() ?? "",
        DepartTime = r["depart_time"]?.ToString() ?? "",
        ArriveTime = r["arrive_time"]?.ToString() ?? "",
        ArriveDayOffset = Convert.ToInt32(r["arrive_day_offset"] ?? 0),
        CoachNo = r["coach_no"]?.ToString() ?? "",
        SeatNo = r["seat_no"]?.ToString() ?? "",
        Money = Convert.ToDecimal(r["money"] ?? 0m),
        SeatType = r["seat_type"]?.ToString() ?? "",
        AdditionalInfo = r["additional_info"]?.ToString() ?? "",
        TicketPurpose = r["ticket_purpose"]?.ToString() ?? "",
        TicketModificationType = r["ticket_modification_type"]?.ToString() ?? "",
        TicketTypeFlags = Convert.ToInt32(r["ticket_type_flags"] ?? 0),
        PaymentChannelFlags = Convert.ToInt32(r["payment_channel_flags"] ?? 0),
        Hint = r["hint"]?.ToString() ?? "",
        DepartStationCode = r["depart_station_code"]?.ToString() ?? "",
        ArriveStationCode = r["arrive_station_code"]?.ToString() ?? "",
        Status = Convert.ToInt32(r["status"] ?? 0),
        UpdatedAt = r["updated_at"]?.ToString() ?? "",
        DeletedAt = r["deleted_at"] is DBNull ? null : r["deleted_at"]?.ToString()
    };
}
