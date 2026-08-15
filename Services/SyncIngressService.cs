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

                await ApplyEntityAsync(connection, tx, change);
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

    private static async Task ApplyEntityAsync(
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
                return;
            }

            var ride = SyncPayloadSerializer.ParseRide(change.Payload)
                       ?? throw new InvalidOperationException("ride payload 无效");
            if (!string.Equals(ride.SyncId, change.SyncId, StringComparison.Ordinal))
                ride.SyncId = change.SyncId;
            if (string.IsNullOrWhiteSpace(ride.UpdatedAt))
                ride.UpdatedAt = string.IsNullOrWhiteSpace(change.UpdatedAt)
                    ? SyncClock.UtcNowIso()
                    : change.UpdatedAt;
            await UpsertRideAsync(connection, tx, ride);
            return;
        }

        if (entity == SyncEntityTypes.Tag)
        {
            if (op == SyncOps.Delete)
            {
                await SoftDeleteTagAsync(connection, tx, change.SyncId, change.UpdatedAt);
                return;
            }

            var tag = SyncPayloadSerializer.ParseTag(change.Payload)
                      ?? throw new InvalidOperationException("tag payload 无效");
            if (!string.Equals(tag.SyncId, change.SyncId, StringComparison.Ordinal))
                tag.SyncId = change.SyncId;
            if (string.IsNullOrWhiteSpace(tag.UpdatedAt))
                tag.UpdatedAt = string.IsNullOrWhiteSpace(change.UpdatedAt)
                    ? SyncClock.UtcNowIso()
                    : change.UpdatedAt;
            await UpsertTagAsync(connection, tx, tag);
            return;
        }

        if (entity == SyncEntityTypes.RideTags)
        {
            var payload = SyncPayloadSerializer.ParseRideTags(change.Payload)
                          ?? throw new InvalidOperationException("ride_tags payload 无效");
            var rideSyncId = string.IsNullOrWhiteSpace(payload.RideSyncId) ? change.SyncId : payload.RideSyncId!;
            await ReplaceRideTagsAsync(connection, tx, rideSyncId, payload.TagSyncIds ?? new List<string>());
            return;
        }

        throw new InvalidOperationException($"未知实体类型: {change.Entity}");
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
