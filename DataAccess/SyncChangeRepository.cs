using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Model.Sync;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

/// <summary>
///     同步变更日志。PC 端分配单调 seq，作为两端对齐水位。
/// </summary>
public class SyncChangeRepository
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

    public async Task<long> AppendAsync(
        string entity,
        string syncId,
        string op,
        string? payloadJson,
        string? deviceId = null,
        SqliteConnection? existingConnection = null,
        SqliteTransaction? transaction = null)
    {
        var ownsConnection = existingConnection == null;
        var connection = existingConnection ?? new SqliteConnection(_connectionString);
        try
        {
            if (ownsConnection)
                await connection.OpenAsync();

            var updatedAt = SyncClock.UtcNowIso();
            var changeId = SyncClock.NewChangeId();

            // 同事务内取下一 seq，避免并发空洞过大（SQLite 写锁串行化）
            var nextSeq = await connection.ExecuteScalarAsync<long>(
                "SELECT COALESCE(MAX(seq), 0) + 1 FROM sync_change",
                transaction: transaction);

            await connection.ExecuteAsync(
                @"INSERT INTO sync_change (change_id, entity, sync_id, op, payload, updated_at, seq, device_id)
                  VALUES (@ChangeId, @Entity, @SyncId, @Op, @Payload, @UpdatedAt, @Seq, @DeviceId)",
                new
                {
                    ChangeId = changeId,
                    Entity = entity,
                    SyncId = syncId,
                    Op = op,
                    Payload = payloadJson,
                    UpdatedAt = updatedAt,
                    Seq = nextSeq,
                    DeviceId = deviceId
                },
                transaction);

            return nextSeq;
        }
        finally
        {
            if (ownsConnection)
                await connection.DisposeAsync();
        }
    }

    public async Task<long> GetMaxSeqAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<long>("SELECT COALESCE(MAX(seq), 0) FROM sync_change");
    }

    public async Task<IEnumerable<SyncChangeRecord>> GetChangesSinceAsync(long afterSeq, int limit = 500)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QueryAsync<SyncChangeRecord>(
            @"SELECT change_id AS ChangeId, entity AS Entity, sync_id AS SyncId, op AS Op,
                     payload AS Payload, updated_at AS UpdatedAt, seq AS Seq, device_id AS DeviceId
              FROM sync_change
              WHERE seq > @AfterSeq
              ORDER BY seq ASC
              LIMIT @Limit",
            new { AfterSeq = afterSeq, Limit = limit });
    }

    public async Task<bool> ChangeIdExistsAsync(string changeId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return await ChangeIdExistsAsync(connection, changeId, null);
    }

    public async Task<bool> ChangeIdExistsAsync(
        SqliteConnection connection,
        string changeId,
        SqliteTransaction? transaction)
    {
        var n = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sync_change WHERE change_id = @ChangeId",
            new { ChangeId = changeId },
            transaction);
        return n > 0;
    }

    /// <summary>
    ///     写入客户端带来的 change_id（幂等）。已存在则返回 inserted=false 与当前 max seq。
    /// </summary>
    public async Task<(bool Inserted, long Seq)> TryAppendClientChangeAsync(
        string changeId,
        string entity,
        string syncId,
        string op,
        string? payloadJson,
        string? deviceId,
        SqliteConnection connection,
        SqliteTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(changeId))
            throw new ArgumentException("change_id 不能为空", nameof(changeId));

        if (await ChangeIdExistsAsync(connection, changeId, transaction))
        {
            var max = await connection.ExecuteScalarAsync<long>(
                "SELECT COALESCE(MAX(seq), 0) FROM sync_change",
                transaction: transaction);
            return (false, max);
        }

        var updatedAt = SyncClock.UtcNowIso();
        var nextSeq = await connection.ExecuteScalarAsync<long>(
            "SELECT COALESCE(MAX(seq), 0) + 1 FROM sync_change",
            transaction: transaction);

        await connection.ExecuteAsync(
            @"INSERT INTO sync_change (change_id, entity, sync_id, op, payload, updated_at, seq, device_id)
              VALUES (@ChangeId, @Entity, @SyncId, @Op, @Payload, @UpdatedAt, @Seq, @DeviceId)",
            new
            {
                ChangeId = changeId,
                Entity = entity,
                SyncId = syncId,
                Op = op,
                Payload = payloadJson,
                UpdatedAt = updatedAt,
                Seq = nextSeq,
                DeviceId = deviceId
            },
            transaction);

        return (true, nextSeq);
    }
}
