using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Model.Sync;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Services;

/// <summary>
///     将库中已有行程/标签补进 <c>sync_change</c>。
///     历史数据在迁移时只补了 sync_id，没有变更日志，手机 pull 会为空。
/// </summary>
public sealed class SyncBaselinePublisher
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;
    private readonly SyncChangeRepository _changes = new();

    public async Task<SyncBaselineResult> PublishMissingAsync()
    {
        var result = new SyncBaselineResult();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var rides = (await connection.QueryAsync<TrainRideInfo>(
            @"SELECT id AS Id, ticket_number AS TicketNumber, check_in_location AS CheckInLocation,
                     depart_station AS DepartStation, train_no AS TrainNo, arrive_station AS ArriveStation,
                     depart_station_pinyin AS DepartStationPinyin, arrive_station_pinyin AS ArriveStationPinyin,
                     depart_date AS DepartDate, depart_time AS DepartTime, arrive_time AS ArriveTime,
                     arrive_day_offset AS ArriveDayOffset, coach_no AS CoachNo, seat_no AS SeatNo,
                     money AS Money, seat_type AS SeatType, additional_info AS AdditionalInfo,
                     ticket_purpose AS TicketPurpose, ticket_modification_type AS TicketModificationType,
                     ticket_type_flags AS TicketTypeFlags, payment_channel_flags AS PaymentChannelFlags,
                     hint AS Hint, depart_station_code AS DepartStationCode, arrive_station_code AS ArriveStationCode,
                     status AS Status, sync_id AS SyncId, updated_at AS UpdatedAt, deleted_at AS DeletedAt
              FROM train_ride_info
              WHERE (deleted_at IS NULL OR deleted_at = '')
                AND sync_id IS NOT NULL AND TRIM(sync_id) <> ''")).ToList();

        foreach (var ride in rides)
        {
            if (await HasEntityChangeAsync(connection, SyncEntityTypes.Ride, ride.SyncId))
            {
                result.SkippedRides++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(ride.UpdatedAt))
                ride.UpdatedAt = SyncClock.UtcNowIso();

            var changeId = BaselineChangeId(SyncEntityTypes.Ride, ride.SyncId);
            using var tx = connection.BeginTransaction();
            var (inserted, _) = await _changes.TryAppendClientChangeAsync(
                changeId,
                SyncEntityTypes.Ride,
                ride.SyncId,
                SyncOps.Upsert,
                SyncPayloadSerializer.Ride(ride),
                "pc-baseline",
                connection,
                tx);
            tx.Commit();
            if (inserted) result.PublishedRides++;
            else result.SkippedRides++;
        }

        var tags = (await connection.QueryAsync<TicketTag>(
            @"SELECT id AS Id, name AS Name, color AS Color, text_color AS TextColor,
                     sort_order AS SortOrder, is_default AS IsDefault, created_at AS CreatedAt,
                     sync_id AS SyncId, updated_at AS UpdatedAt, deleted_at AS DeletedAt
              FROM ticket_tag
              WHERE (deleted_at IS NULL OR deleted_at = '')
                AND sync_id IS NOT NULL AND TRIM(sync_id) <> ''")).ToList();

        foreach (var tag in tags)
        {
            if (await HasEntityChangeAsync(connection, SyncEntityTypes.Tag, tag.SyncId))
            {
                result.SkippedTags++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(tag.UpdatedAt))
                tag.UpdatedAt = SyncClock.UtcNowIso();

            var changeId = BaselineChangeId(SyncEntityTypes.Tag, tag.SyncId);
            using var tx = connection.BeginTransaction();
            var (inserted, _) = await _changes.TryAppendClientChangeAsync(
                changeId,
                SyncEntityTypes.Tag,
                tag.SyncId,
                SyncOps.Upsert,
                SyncPayloadSerializer.Tag(tag),
                "pc-baseline",
                connection,
                tx);
            tx.Commit();
            if (inserted) result.PublishedTags++;
            else result.SkippedTags++;
        }

        var rideTagRows = (await connection.QueryAsync<(string RideSyncId, string TagSyncId)>(
            @"SELECT r.sync_id AS RideSyncId, t.sync_id AS TagSyncId
              FROM train_ride_tag rt
              INNER JOIN train_ride_info r ON r.id = rt.train_ride_id
              INNER JOIN ticket_tag t ON t.id = rt.tag_id
              WHERE (r.deleted_at IS NULL OR r.deleted_at = '')
                AND (t.deleted_at IS NULL OR t.deleted_at = '')
                AND r.sync_id IS NOT NULL AND TRIM(r.sync_id) <> ''
                AND t.sync_id IS NOT NULL AND TRIM(t.sync_id) <> ''")).ToList();

        foreach (var group in rideTagRows.GroupBy(x => x.RideSyncId, StringComparer.Ordinal))
        {
            var rideSyncId = group.Key;
            if (await HasEntityChangeAsync(connection, SyncEntityTypes.RideTags, rideSyncId))
            {
                result.SkippedRideTags++;
                continue;
            }

            var tagIds = group.Select(x => x.TagSyncId).Distinct(StringComparer.Ordinal).ToList();
            var changeId = BaselineChangeId(SyncEntityTypes.RideTags, rideSyncId);
            using var tx = connection.BeginTransaction();
            var (inserted, _) = await _changes.TryAppendClientChangeAsync(
                changeId,
                SyncEntityTypes.RideTags,
                rideSyncId,
                SyncOps.Upsert,
                SyncPayloadSerializer.RideTags(rideSyncId, tagIds),
                "pc-baseline",
                connection,
                tx);
            tx.Commit();
            if (inserted) result.PublishedRideTags++;
            else result.SkippedRideTags++;
        }

        result.MaxSeq = await _changes.GetMaxSeqAsync();
        return result;
    }

    private static string BaselineChangeId(string entity, string syncId) =>
        $"baseline:{entity}:{syncId}";

    private static async Task<bool> HasEntityChangeAsync(
        SqliteConnection connection,
        string entity,
        string syncId)
    {
        var n = await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1) FROM sync_change
              WHERE entity = @Entity AND sync_id = @SyncId",
            new { Entity = entity, SyncId = syncId });
        return n > 0;
    }
}

public sealed class SyncBaselineResult
{
    public int PublishedRides { get; set; }
    public int SkippedRides { get; set; }
    public int PublishedTags { get; set; }
    public int SkippedTags { get; set; }
    public int PublishedRideTags { get; set; }
    public int SkippedRideTags { get; set; }
    public long MaxSeq { get; set; }

    public int PublishedTotal => PublishedRides + PublishedTags + PublishedRideTags;

    public string SummaryText =>
        PublishedTotal == 0
            ? $"现有数据已在同步日志中（seq={MaxSeq}）。"
            : $"已补发行程 {PublishedRides}、标签 {PublishedTags}、行程标签 {PublishedRideTags}（seq={MaxSeq}）。手机请再执行「立即对齐」。";
}
