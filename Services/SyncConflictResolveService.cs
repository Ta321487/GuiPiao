using System;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.DataAccess;
using GuiPiao.Model;
using GuiPiao.Model.Sync;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Services;

/// <summary>冲突箱：local=保留 PC；remote=采用手机推送稿并写入 sync_change。</summary>
public class SyncConflictResolveService
{
    private readonly SyncConflictRepository _conflicts = new();
    private readonly SyncChangeRepository _changes = new();
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

    public async Task<SyncConflictListResponse> ListOpenAsync()
    {
        var rows = await _conflicts.ListOpenAsync();
        return new SyncConflictListResponse { Conflicts = new System.Collections.Generic.List<SyncConflictDto>(rows) };
    }

    public async Task<SyncConflictResolveResponse> ResolveAsync(SyncConflictResolveRequest request)
    {
        if (request == null || request.Id <= 0)
            return new SyncConflictResolveResponse { Ok = false, Error = "invalid_id" };

        var conflict = await _conflicts.GetByIdAsync(request.Id);
        if (conflict == null)
            return new SyncConflictResolveResponse { Ok = false, Error = "not_found" };

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var resolvedAt = await connection.ExecuteScalarAsync<string?>(
                "SELECT resolved_at FROM sync_conflict WHERE id = @Id",
                new { request.Id });
            if (!string.IsNullOrWhiteSpace(resolvedAt))
                return new SyncConflictResolveResponse { Ok = false, Error = "already_resolved" };
        }

        var keep = (request.Keep ?? "local").Trim().ToLowerInvariant();
        if (keep != "local" && keep != "remote")
            return new SyncConflictResolveResponse { Ok = false, Error = "keep_must_be_local_or_remote" };

        if (keep == "remote")
        {
            var applied = await ApplyRemoteAsync(conflict);
            if (!applied.Ok)
                return applied;
        }

        await _conflicts.MarkResolvedAsync(request.Id);
        return new SyncConflictResolveResponse { Ok = true };
    }

    private async Task<SyncConflictResolveResponse> ApplyRemoteAsync(SyncConflictDto conflict)
    {
        var entity = (conflict.Entity ?? "").Trim().ToLowerInvariant();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var tx = connection.BeginTransaction();

        try
        {
            if (entity == SyncEntityTypes.Ride)
            {
                var ride = SyncPayloadSerializer.ParseRide(conflict.RemoteValue);
                if (ride == null)
                    return new SyncConflictResolveResponse { Ok = false, Error = "remote_ride_invalid" };
                if (string.IsNullOrWhiteSpace(ride.UpdatedAt))
                    ride.UpdatedAt = SyncClock.UtcNowIso();
                await UpsertRideForResolveAsync(connection, tx, ride);
                await _changes.AppendAsync(
                    SyncEntityTypes.Ride,
                    ride.SyncId,
                    string.IsNullOrWhiteSpace(ride.DeletedAt) ? SyncOps.Upsert : SyncOps.Delete,
                    conflict.RemoteValue,
                    deviceId: "conflict-resolve",
                    existingConnection: connection,
                    transaction: tx);
            }
            else if (entity == SyncEntityTypes.Tag)
            {
                var tag = SyncPayloadSerializer.ParseTag(conflict.RemoteValue);
                if (tag == null)
                    return new SyncConflictResolveResponse { Ok = false, Error = "remote_tag_invalid" };
                if (string.IsNullOrWhiteSpace(tag.UpdatedAt))
                    tag.UpdatedAt = SyncClock.UtcNowIso();
                await UpsertTagForResolveAsync(connection, tx, tag);
                await _changes.AppendAsync(
                    SyncEntityTypes.Tag,
                    tag.SyncId,
                    string.IsNullOrWhiteSpace(tag.DeletedAt) ? SyncOps.Upsert : SyncOps.Delete,
                    conflict.RemoteValue,
                    deviceId: "conflict-resolve",
                    existingConnection: connection,
                    transaction: tx);
            }
            else
            {
                return new SyncConflictResolveResponse { Ok = false, Error = "unsupported_entity" };
            }

            tx.Commit();
            return new SyncConflictResolveResponse { Ok = true };
        }
        catch (Exception ex)
        {
            try { tx.Rollback(); } catch { /* ignore */ }
            return new SyncConflictResolveResponse { Ok = false, Error = ex.Message };
        }
    }

    private static async Task UpsertRideForResolveAsync(
        SqliteConnection connection, SqliteTransaction tx, TrainRideInfo ride)
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

    private static async Task UpsertTagForResolveAsync(
        SqliteConnection connection, SqliteTransaction tx, TicketTag tag)
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
}
