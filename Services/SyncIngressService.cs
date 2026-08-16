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
///     接收手机推送的变更：按 change_id 幂等；应用实体后写入 PC 权威 seq。
///     不经 Repository 写路径，避免二次生成 change_id。
/// </summary>
public class SyncIngressService
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;
    private readonly SyncChangeRepository _changes = new();
    private readonly SyncConflictRepository _conflicts = new();

    public async Task<SyncPushResponse> ApplyPushAsync(string deviceId, IReadOnlyList<SyncChangeDto> changes)
    {
        var response = new SyncPushResponse();
        if (changes == null || changes.Count == 0)
        {
            response.MaxSeq = await _changes.GetMaxSeqAsync();
            return response;
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var change in changes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(change.ChangeId) ||
                    string.IsNullOrWhiteSpace(change.Entity) ||
                    string.IsNullOrWhiteSpace(change.SyncId) ||
                    string.IsNullOrWhiteSpace(change.Op))
                {
                    response.Errors.Add("变更缺少 change_id/entity/sync_id/op");
                    continue;
                }

                using var tx = connection.BeginTransaction();
                if (await _changes.ChangeIdExistsAsync(connection, change.ChangeId, tx))
                {
                    response.Skipped++;
                    tx.Commit();
                    continue;
                }

                var applied = await ApplyEntityAsync(connection, tx, change);
                if (!applied)
                {
                    // 冲突箱已写入；不写入 sync_change，避免 pull 把被拒稿再分发
                    tx.Commit();
                    response.Skipped++;
                    continue;
                }

                var (_, seq) = await _changes.TryAppendClientChangeAsync(
                    change.ChangeId,
                    change.Entity,
                    change.SyncId,
                    change.Op,
                    change.Payload,
                    deviceId,
                    connection,
                    tx);
                tx.Commit();
                response.Accepted++;
                response.MaxSeq = seq;
            }
            catch (Exception ex)
            {
                response.Errors.Add($"{change.ChangeId}: {ex.Message}");
            }
        }

        if (response.MaxSeq == 0)
            response.MaxSeq = await _changes.GetMaxSeqAsync();

        return response;
    }

    /// <returns>false = 写入冲突箱且未覆盖本地。</returns>
    private async Task<bool> ApplyEntityAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        SyncChangeDto change)
    {
        var entity = change.Entity.Trim().ToLowerInvariant();
        var op = change.Op.Trim().ToLowerInvariant();

        if (entity == SyncEntityTypes.Ride)
        {
            if (op == SyncOps.Delete)
            {
                await SoftDeleteRideAsync(connection, tx, change.SyncId, change.UpdatedAt);
                return true;
            }

            var ride = SyncPayloadSerializer.ParseRide(change.Payload)
                       ?? throw new InvalidOperationException("ride payload 无效");
            if (!string.Equals(ride.SyncId, change.SyncId, StringComparison.Ordinal))
                ride.SyncId = change.SyncId;
            if (string.IsNullOrWhiteSpace(ride.UpdatedAt))
                ride.UpdatedAt = string.IsNullOrWhiteSpace(change.UpdatedAt)
                    ? SyncClock.UtcNowIso()
                    : change.UpdatedAt;

            var existing = await connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT updated_at AS UpdatedAt FROM train_ride_info WHERE sync_id = @SyncId",
                new { ride.SyncId }, tx);
            if (existing != null)
            {
                string? localAt = existing.UpdatedAt;
                if (IsLocalNewer(localAt, ride.UpdatedAt))
                {
                    var localPayload = await BuildLocalRidePayloadAsync(connection, tx, ride.SyncId);
                    await _conflicts.InsertOpenAsync(
                        SyncEntityTypes.Ride,
                        ride.SyncId,
                        "*",
                        localPayload,
                        change.Payload,
                        localAt,
                        ride.UpdatedAt,
                        connection,
                        tx);
                    return false;
                }
            }

            await UpsertRideAsync(connection, tx, ride);
            return true;
        }

        if (entity == SyncEntityTypes.Tag)
        {
            if (op == SyncOps.Delete)
            {
                await SoftDeleteTagAsync(connection, tx, change.SyncId, change.UpdatedAt);
                return true;
            }

            var tag = SyncPayloadSerializer.ParseTag(change.Payload)
                      ?? throw new InvalidOperationException("tag payload 无效");
            if (!string.Equals(tag.SyncId, change.SyncId, StringComparison.Ordinal))
                tag.SyncId = change.SyncId;
            if (string.IsNullOrWhiteSpace(tag.UpdatedAt))
                tag.UpdatedAt = string.IsNullOrWhiteSpace(change.UpdatedAt)
                    ? SyncClock.UtcNowIso()
                    : change.UpdatedAt;

            var existingTag = await connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT updated_at AS UpdatedAt FROM ticket_tag WHERE sync_id = @SyncId",
                new { tag.SyncId }, tx);
            if (existingTag != null)
            {
                string? localAt = existingTag.UpdatedAt;
                if (IsLocalNewer(localAt, tag.UpdatedAt))
                {
                    await _conflicts.InsertOpenAsync(
                        SyncEntityTypes.Tag,
                        tag.SyncId,
                        "*",
                        SyncPayloadSerializer.Tag(await LoadTagAsync(connection, tx, tag.SyncId) ?? tag),
                        change.Payload,
                        localAt,
                        tag.UpdatedAt,
                        connection,
                        tx);
                    return false;
                }
            }

            await UpsertTagAsync(connection, tx, tag);
            return true;
        }

        if (entity == SyncEntityTypes.RideTags)
        {
            var payload = SyncPayloadSerializer.ParseRideTags(change.Payload)
                          ?? throw new InvalidOperationException("ride_tags payload 无效");
            var rideSyncId = string.IsNullOrWhiteSpace(payload.RideSyncId) ? change.SyncId : payload.RideSyncId!;
            await ReplaceRideTagsAsync(connection, tx, rideSyncId, payload.TagSyncIds ?? new List<string>());
            return true;
        }

        throw new InvalidOperationException($"未知实体类型: {change.Entity}");
    }

    private static bool IsLocalNewer(string? localUpdatedAt, string? remoteUpdatedAt)
    {
        if (string.IsNullOrWhiteSpace(localUpdatedAt)) return false;
        if (string.IsNullOrWhiteSpace(remoteUpdatedAt)) return true;
        if (DateTime.TryParse(localUpdatedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var l) &&
            DateTime.TryParse(remoteUpdatedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var r))
            return l > r;
        return string.CompareOrdinal(localUpdatedAt, remoteUpdatedAt) > 0;
    }

    private static async Task<string?> BuildLocalRidePayloadAsync(
        SqliteConnection connection, SqliteTransaction tx, string syncId)
    {
        var ride = await connection.QuerySingleOrDefaultAsync<TrainRideInfo>(
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
              FROM train_ride_info WHERE sync_id = @SyncId",
            new { SyncId = syncId }, tx);
        return ride == null ? null : SyncPayloadSerializer.Ride(ride);
    }

    private static async Task<TicketTag?> LoadTagAsync(
        SqliteConnection connection, SqliteTransaction tx, string syncId)
    {
        return await connection.QuerySingleOrDefaultAsync<TicketTag>(
            @"SELECT id AS Id, name AS Name, color AS Color, text_color AS TextColor,
                     sort_order AS SortOrder, is_default AS IsDefault, created_at AS CreatedAt,
                     sync_id AS SyncId, updated_at AS UpdatedAt, deleted_at AS DeletedAt
              FROM ticket_tag WHERE sync_id = @SyncId",
            new { SyncId = syncId }, tx);
    }

    private static async Task UpsertRideAsync(SqliteConnection connection, SqliteTransaction tx, TrainRideInfo ride)
    {
        ride.DepartDate = RideDateTime.NormalizeDate(ride.DepartDate);
        ride.DepartTime = RideDateTime.NormalizeTime(ride.DepartTime);
        ride.ArriveTime = RideDateTime.NormalizeTime(ride.ArriveTime);

        var existingId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM train_ride_info WHERE sync_id = @SyncId",
            new { ride.SyncId }, tx);

        if (existingId == null)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO train_ride_info (
                    ticket_number, check_in_location, depart_station, train_no, arrive_station,
                    depart_station_pinyin, arrive_station_pinyin, depart_date, depart_time, arrive_time, arrive_day_offset, coach_no,
                    seat_no, money, seat_type, additional_info, ticket_purpose, ticket_modification_type,
                    ticket_type_flags, payment_channel_flags, hint, depart_station_code, arrive_station_code, status,
                    sync_id, updated_at, deleted_at
                  ) VALUES (
                    @TicketNumber, @CheckInLocation, @DepartStation, @TrainNo, @ArriveStation,
                    @DepartStationPinyin, @ArriveStationPinyin, @DepartDate, @DepartTime, @ArriveTime, @ArriveDayOffset, @CoachNo,
                    @SeatNo, @Money, @SeatType, @AdditionalInfo, @TicketPurpose, @TicketModificationType,
                    @TicketTypeFlags, @PaymentChannelFlags, @Hint, @DepartStationCode, @ArriveStationCode, @Status,
                    @SyncId, @UpdatedAt, @DeletedAt
                  )",
                ride, tx);
        }
        else
        {
            ride.Id = existingId.Value;
            await connection.ExecuteAsync(
                @"UPDATE train_ride_info SET
                    ticket_number=@TicketNumber, check_in_location=@CheckInLocation, depart_station=@DepartStation,
                    train_no=@TrainNo, arrive_station=@ArriveStation, depart_station_pinyin=@DepartStationPinyin,
                    arrive_station_pinyin=@ArriveStationPinyin, depart_date=@DepartDate, depart_time=@DepartTime,
                    arrive_time=@ArriveTime, arrive_day_offset=@ArriveDayOffset, coach_no=@CoachNo, seat_no=@SeatNo,
                    money=@Money, seat_type=@SeatType, additional_info=@AdditionalInfo, ticket_purpose=@TicketPurpose,
                    ticket_modification_type=@TicketModificationType, ticket_type_flags=@TicketTypeFlags,
                    payment_channel_flags=@PaymentChannelFlags, hint=@Hint, depart_station_code=@DepartStationCode,
                    arrive_station_code=@ArriveStationCode, status=@Status, updated_at=@UpdatedAt, deleted_at=@DeletedAt
                  WHERE sync_id=@SyncId",
                ride, tx);
        }
    }

    private static async Task SoftDeleteRideAsync(
        SqliteConnection connection, SqliteTransaction tx, string syncId, string? updatedAt)
    {
        var when = string.IsNullOrWhiteSpace(updatedAt) ? SyncClock.UtcNowIso() : updatedAt;
        await connection.ExecuteAsync(
            @"UPDATE train_ride_info
              SET deleted_at = @DeletedAt, updated_at = @UpdatedAt
              WHERE sync_id = @SyncId AND (deleted_at IS NULL OR deleted_at = '')",
            new { SyncId = syncId, DeletedAt = when, UpdatedAt = when }, tx);
    }

    private static async Task UpsertTagAsync(SqliteConnection connection, SqliteTransaction tx, TicketTag tag)
    {
        var existingId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM ticket_tag WHERE sync_id = @SyncId",
            new { tag.SyncId }, tx);

        if (existingId == null)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO ticket_tag (name, color, text_color, sort_order, is_default, created_at, sync_id, updated_at, deleted_at)
                  VALUES (@Name, @Color, @TextColor, @SortOrder, @IsDefault, @CreatedAt, @SyncId, @UpdatedAt, @DeletedAt)",
                new
                {
                    tag.Name,
                    tag.Color,
                    tag.TextColor,
                    tag.SortOrder,
                    tag.IsDefault,
                    CreatedAt = SyncClock.UtcNowIso(),
                    tag.SyncId,
                    tag.UpdatedAt,
                    tag.DeletedAt
                }, tx);
        }
        else
        {
            await connection.ExecuteAsync(
                @"UPDATE ticket_tag SET
                    name=@Name, color=@Color, text_color=@TextColor, sort_order=@SortOrder,
                    is_default=@IsDefault, updated_at=@UpdatedAt, deleted_at=@DeletedAt
                  WHERE sync_id=@SyncId",
                tag, tx);
        }
    }

    private static async Task SoftDeleteTagAsync(
        SqliteConnection connection, SqliteTransaction tx, string syncId, string? updatedAt)
    {
        var when = string.IsNullOrWhiteSpace(updatedAt) ? SyncClock.UtcNowIso() : updatedAt;
        await connection.ExecuteAsync(
            @"UPDATE ticket_tag
              SET deleted_at = @DeletedAt, updated_at = @UpdatedAt
              WHERE sync_id = @SyncId AND (deleted_at IS NULL OR deleted_at = '')",
            new { SyncId = syncId, DeletedAt = when, UpdatedAt = when }, tx);
    }

    private static async Task ReplaceRideTagsAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        string rideSyncId,
        IReadOnlyList<string> tagSyncIds)
    {
        var rideId = await connection.ExecuteScalarAsync<int?>(
            "SELECT id FROM train_ride_info WHERE sync_id = @SyncId",
            new { SyncId = rideSyncId }, tx);
        if (rideId == null)
            throw new InvalidOperationException($"找不到行程 sync_id={rideSyncId}");

        await connection.ExecuteAsync(
            "DELETE FROM train_ride_tag WHERE train_ride_id = @RideId",
            new { RideId = rideId.Value }, tx);

        var createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        foreach (var tagSyncId in tagSyncIds.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
        {
            var tagId = await connection.ExecuteScalarAsync<int?>(
                "SELECT id FROM ticket_tag WHERE sync_id = @SyncId AND (deleted_at IS NULL OR deleted_at = '')",
                new { SyncId = tagSyncId }, tx);
            if (tagId == null) continue;

            await connection.ExecuteAsync(
                @"INSERT INTO train_ride_tag (train_ride_id, tag_id, created_at)
                  VALUES (@RideId, @TagId, @CreatedAt)",
                new { RideId = rideId.Value, TagId = tagId.Value, CreatedAt = createdAt }, tx);
        }
    }
}
