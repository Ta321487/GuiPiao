using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Model.Sync;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

public class SyncConflictRepository
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;

    public async Task<long> InsertOpenAsync(
        string entity,
        string syncId,
        string field,
        string? localValue,
        string? remoteValue,
        string? localUpdatedAt,
        string? remoteUpdatedAt,
        SqliteConnection? existingConnection = null,
        SqliteTransaction? transaction = null)
    {
        var owns = existingConnection == null;
        var connection = existingConnection ?? new SqliteConnection(_connectionString);
        try
        {
            if (owns) await connection.OpenAsync();
            var id = await connection.ExecuteScalarAsync<long>(
                @"INSERT INTO sync_conflict (
                    entity, sync_id, field, local_value, remote_value,
                    local_updated_at, remote_updated_at, created_at, resolved_at
                  ) VALUES (
                    @Entity, @SyncId, @Field, @LocalValue, @RemoteValue,
                    @LocalUpdatedAt, @RemoteUpdatedAt, @CreatedAt, NULL
                  );
                  SELECT last_insert_rowid();",
                new
                {
                    Entity = entity,
                    SyncId = syncId,
                    Field = field,
                    LocalValue = localValue,
                    RemoteValue = remoteValue,
                    LocalUpdatedAt = localUpdatedAt,
                    RemoteUpdatedAt = remoteUpdatedAt,
                    CreatedAt = SyncClock.UtcNowIso()
                },
                transaction);
            return id;
        }
        finally
        {
            if (owns) await connection.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<SyncConflictDto>> ListOpenAsync(int limit = 200)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var rows = await connection.QueryAsync<SyncConflictDto>(
            @"SELECT id AS Id, entity AS Entity, sync_id AS SyncId, field AS Field,
                     local_value AS LocalValue, remote_value AS RemoteValue,
                     local_updated_at AS LocalUpdatedAt, remote_updated_at AS RemoteUpdatedAt,
                     created_at AS CreatedAt
              FROM sync_conflict
              WHERE resolved_at IS NULL OR resolved_at = ''
              ORDER BY id DESC
              LIMIT @Limit",
            new { Limit = limit });
        return rows.AsList();
    }

    public async Task<SyncConflictDto?> GetByIdAsync(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.QuerySingleOrDefaultAsync<SyncConflictDto>(
            @"SELECT id AS Id, entity AS Entity, sync_id AS SyncId, field AS Field,
                     local_value AS LocalValue, remote_value AS RemoteValue,
                     local_updated_at AS LocalUpdatedAt, remote_updated_at AS RemoteUpdatedAt,
                     created_at AS CreatedAt
              FROM sync_conflict WHERE id = @Id",
            new { Id = id });
    }

    public async Task MarkResolvedAsync(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "UPDATE sync_conflict SET resolved_at = @At WHERE id = @Id",
            new { Id = id, At = SyncClock.UtcNowIso() });
    }
}
