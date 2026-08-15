using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Model;
using GuiPiao.Model.Sync;
using GuiPiao.Services;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess;

/// <summary>
///     标签数据访问类
/// </summary>
public class TicketTagRepository
{
    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;
    private readonly SyncChangeRepository _syncChanges = new();

    private const string ActiveTagSql = "(deleted_at IS NULL OR deleted_at = '')";

    private static void EnsureTagSyncMetadata(TicketTag tag)
    {
        if (string.IsNullOrWhiteSpace(tag.SyncId))
            tag.SyncId = SyncClock.NewSyncId();
        tag.UpdatedAt = SyncClock.UtcNowIso();
    }

    #region 基础CRUD

    /// <summary>
    ///     添加标签
    /// </summary>
    public async Task<int> AddTagAsync(TicketTag tag)
    {
        EnsureTagSyncMetadata(tag);
        tag.DeletedAt = null;
        tag.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
                    INSERT INTO ticket_tag (name, color, text_color, sort_order, is_default, created_at, sync_id, updated_at, deleted_at)
                    VALUES (@Name, @Color, @TextColor, @SortOrder, @IsDefault, @CreatedAt, @SyncId, @UpdatedAt, @DeletedAt);
                    SELECT last_insert_rowid();
                ";
                var id = await connection.QuerySingleAsync<int>(sql, tag, transaction);
                tag.Id = id;

                await _syncChanges.AppendAsync(
                    SyncEntityTypes.Tag,
                    tag.SyncId,
                    SyncOps.Upsert,
                    SyncPayloadSerializer.Tag(tag),
                    existingConnection: connection,
                    transaction: transaction);

                transaction.Commit();
                return id;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <summary>
    ///     根据ID获取标签
    /// </summary>
    public async Task<TicketTag?> GetTagByIdAsync(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"SELECT 
                    id AS Id, 
                    name AS Name, 
                    color AS Color, 
                    text_color AS TextColor, 
                    sort_order AS SortOrder, 
                    is_default AS IsDefault,
                    created_at AS CreatedAt,
                    sync_id AS SyncId,
                    updated_at AS UpdatedAt,
                    deleted_at AS DeletedAt
                FROM ticket_tag WHERE id = @Id";
            return await connection.QuerySingleOrDefaultAsync<TicketTag>(sql, new { Id = id });
        }
    }

    /// <summary>
    ///     获取所有标签
    /// </summary>
    public async Task<IEnumerable<TicketTag>> GetAllTagsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = $@"SELECT 
                    id AS Id, 
                    name AS Name, 
                    color AS Color, 
                    text_color AS TextColor, 
                    sort_order AS SortOrder, 
                    is_default AS IsDefault,
                    created_at AS CreatedAt,
                    sync_id AS SyncId,
                    updated_at AS UpdatedAt,
                    deleted_at AS DeletedAt
                FROM ticket_tag 
                WHERE {ActiveTagSql}
                ORDER BY sort_order ASC, id ASC";
            return await connection.QueryAsync<TicketTag>(sql);
        }
    }

    /// <summary>
    ///     获取默认标签
    /// </summary>
    public async Task<IEnumerable<TicketTag>> GetDefaultTagsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = $@"SELECT 
                    id AS Id, 
                    name AS Name, 
                    color AS Color, 
                    text_color AS TextColor, 
                    sort_order AS SortOrder, 
                    is_default AS IsDefault,
                    created_at AS CreatedAt,
                    sync_id AS SyncId,
                    updated_at AS UpdatedAt,
                    deleted_at AS DeletedAt
                FROM ticket_tag 
                WHERE is_default = 1 AND {ActiveTagSql}
                ORDER BY sort_order ASC, id ASC";
            return await connection.QueryAsync<TicketTag>(sql);
        }
    }

    /// <summary>
    ///     更新标签
    /// </summary>
    public async Task<int> UpdateTagAsync(TicketTag tag)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                if (string.IsNullOrWhiteSpace(tag.SyncId))
                {
                    var existing = await connection.ExecuteScalarAsync<string>(
                        "SELECT sync_id FROM ticket_tag WHERE id = @Id", new { tag.Id }, transaction);
                    tag.SyncId = string.IsNullOrWhiteSpace(existing) ? SyncClock.NewSyncId() : existing;
                }

                tag.UpdatedAt = SyncClock.UtcNowIso();

                var sql = @"
                    UPDATE ticket_tag
                    SET name = @Name, color = @Color, text_color = @TextColor, 
                        sort_order = @SortOrder, is_default = @IsDefault,
                        sync_id = @SyncId, updated_at = @UpdatedAt
                    WHERE id = @Id;
                ";
                var affected = await connection.ExecuteAsync(sql, tag, transaction);
                if (affected > 0)
                {
                    await _syncChanges.AppendAsync(
                        SyncEntityTypes.Tag,
                        tag.SyncId,
                        SyncOps.Upsert,
                        SyncPayloadSerializer.Tag(tag),
                        existingConnection: connection,
                        transaction: transaction);
                }

                transaction.Commit();
                return affected;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <summary>
    ///     删除标签（软删）
    /// </summary>
    public async Task<int> DeleteTagAsync(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                var row = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    "SELECT sync_id AS SyncId, deleted_at AS DeletedAt FROM ticket_tag WHERE id = @Id",
                    new { Id = id },
                    transaction);
                if (row == null) return 0;
                if (!string.IsNullOrEmpty((string?)row.DeletedAt)) return 0;

                var syncId = (string?)row.SyncId;
                if (string.IsNullOrWhiteSpace(syncId))
                    syncId = SyncClock.NewSyncId();

                var now = SyncClock.UtcNowIso();
                var affected = await connection.ExecuteAsync(
                    @"UPDATE ticket_tag SET deleted_at = @DeletedAt, updated_at = @UpdatedAt, sync_id = @SyncId WHERE id = @Id",
                    new { DeletedAt = now, UpdatedAt = now, SyncId = syncId, Id = id },
                    transaction);

                if (affected > 0)
                {
                    await _syncChanges.AppendAsync(
                        SyncEntityTypes.Tag,
                        syncId,
                        SyncOps.Delete,
                        null,
                        existingConnection: connection,
                        transaction: transaction);
                }

                transaction.Commit();
                return affected;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <summary>
    ///     清除所有默认标签标记（用于设置新的默认标签）
    /// </summary>
    public async Task ClearAllDefaultTagsAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "UPDATE ticket_tag SET is_default = 0 WHERE is_default = 1";
            await connection.ExecuteAsync(sql);
        }
    }

    #endregion

    #region 行程标签关联操作

    /// <summary>
    ///     为行程添加标签
    /// </summary>
    public async Task<bool> AddTagToRideAsync(int trainRideId, int tagId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    INSERT OR IGNORE INTO train_ride_tag (train_ride_id, tag_id, created_at)
                    VALUES (@TrainRideId, @TagId, @CreatedAt);
                ";
            var result = await connection.ExecuteAsync(sql, new
            {
                TrainRideId = trainRideId,
                TagId = tagId,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
            return result > 0;
        }
    }

    /// <summary>
    ///     移除行程的标签
    /// </summary>
    public async Task<bool> RemoveTagFromRideAsync(int trainRideId, int tagId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    DELETE FROM train_ride_tag 
                    WHERE train_ride_id = @TrainRideId AND tag_id = @TagId;
                ";
            var result = await connection.ExecuteAsync(sql, new
            {
                TrainRideId = trainRideId,
                TagId = tagId
            });
            return result > 0;
        }
    }

    /// <summary>
    ///     获取行程的所有标签
    /// </summary>
    public async Task<IEnumerable<TicketTag>> GetTagsByRideIdAsync(int trainRideId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        t.id AS Id, 
                        t.name AS Name, 
                        t.color AS Color, 
                        t.text_color AS TextColor, 
                        t.sort_order AS SortOrder, 
                        t.is_default AS IsDefault,
                        t.created_at AS CreatedAt 
                    FROM ticket_tag t
                    INNER JOIN train_ride_tag rt ON t.id = rt.tag_id
                    WHERE rt.train_ride_id = @TrainRideId
                    ORDER BY t.sort_order ASC, t.id ASC
                ";
            return await connection.QueryAsync<TicketTag>(sql, new { TrainRideId = trainRideId });
        }
    }

    /// <summary>
    ///     批量获取多个行程的标签（用于列表展示优化）
    /// </summary>
    public async Task<Dictionary<int, List<TicketTag>>> GetTagsByRideIdsAsync(IEnumerable<int> trainRideIds)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = @"
                    SELECT 
                        rt.train_ride_id AS TrainRideId,
                        t.id AS Id, 
                        t.name AS Name, 
                        t.color AS Color, 
                        t.text_color AS TextColor, 
                        t.sort_order AS SortOrder, 
                        t.is_default AS IsDefault,
                        t.created_at AS CreatedAt 
                    FROM ticket_tag t
                    INNER JOIN train_ride_tag rt ON t.id = rt.tag_id
                    WHERE rt.train_ride_id IN @TrainRideIds
                    ORDER BY t.sort_order ASC, t.id ASC
                ";

            var lookup = new Dictionary<int, List<TicketTag>>();
            var results = await connection.QueryAsync<dynamic>(sql, new { TrainRideIds = trainRideIds });

            foreach (var row in results)
            {
                int rideId = row.TrainRideId;
                if (!lookup.ContainsKey(rideId)) lookup[rideId] = new List<TicketTag>();

                lookup[rideId].Add(new TicketTag
                {
                    Id = row.Id,
                    Name = row.Name,
                    Color = row.Color,
                    TextColor = row.TextColor,
                    SortOrder = row.SortOrder,
                    IsDefault = row.IsDefault == 1,
                    CreatedAt = row.CreatedAt
                });
            }

            return lookup;
        }
    }

    /// <summary>
    ///     清空行程的所有标签
    /// </summary>
    public async Task<int> ClearTagsFromRideAsync(int trainRideId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "DELETE FROM train_ride_tag WHERE train_ride_id = @TrainRideId";
            return await connection.ExecuteAsync(sql, new { TrainRideId = trainRideId });
        }
    }

    /// <summary>
    ///     设置行程的标签（先清空再添加），并写 ride_tags 同步变更。
    /// </summary>
    public async Task SetTagsToRideAsync(int trainRideId, IEnumerable<int> tagIds)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var rideSyncId = await connection.ExecuteScalarAsync<string>(
                        "SELECT sync_id FROM train_ride_info WHERE id = @Id",
                        new { Id = trainRideId },
                        transaction);
                    if (string.IsNullOrWhiteSpace(rideSyncId))
                    {
                        rideSyncId = SyncClock.NewSyncId();
                        await connection.ExecuteAsync(
                            "UPDATE train_ride_info SET sync_id = @SyncId, updated_at = @UpdatedAt WHERE id = @Id",
                            new { SyncId = rideSyncId, UpdatedAt = SyncClock.UtcNowIso(), Id = trainRideId },
                            transaction);
                    }

                    // 先清空现有标签
                    var deleteSql = "DELETE FROM train_ride_tag WHERE train_ride_id = @TrainRideId";
                    await connection.ExecuteAsync(deleteSql, new { TrainRideId = trainRideId }, transaction);

                    var distinctTagIds = tagIds?.Distinct().ToList() ?? new List<int>();
                    var tagSyncIds = new List<string>();

                    if (distinctTagIds.Count > 0)
                    {
                        var insertSql = @"
                                INSERT INTO train_ride_tag (train_ride_id, tag_id, created_at)
                                VALUES (@TrainRideId, @TagId, @CreatedAt);
                            ";
                        var createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        foreach (var tagId in distinctTagIds)
                        {
                            await connection.ExecuteAsync(insertSql, new
                            {
                                TrainRideId = trainRideId,
                                TagId = tagId,
                                CreatedAt = createdAt
                            }, transaction);

                            var tagSyncId = await connection.ExecuteScalarAsync<string>(
                                "SELECT sync_id FROM ticket_tag WHERE id = @Id",
                                new { Id = tagId },
                                transaction);
                            if (!string.IsNullOrWhiteSpace(tagSyncId))
                                tagSyncIds.Add(tagSyncId);
                        }
                    }

                    await _syncChanges.AppendAsync(
                        SyncEntityTypes.RideTags,
                        rideSyncId,
                        SyncOps.Upsert,
                        SyncPayloadSerializer.RideTags(rideSyncId, tagSyncIds),
                        existingConnection: connection,
                        transaction: transaction);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    #endregion

    #region 排序和默认标签设置

    /// <summary>
    ///     将标签置顶（设置最小排序值）
    /// </summary>
    public async Task MoveTagToTopAsync(int tagId)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // 获取当前最小排序值
                    var getMinSql = "SELECT COALESCE(MIN(sort_order), 0) FROM ticket_tag";
                    var minSortOrder = await connection.ExecuteScalarAsync<int>(getMinSql, null, transaction);

                    // 将目标标签设置为最小值-1
                    var updateSql = "UPDATE ticket_tag SET sort_order = @NewSortOrder WHERE id = @Id";
                    await connection.ExecuteAsync(updateSql, new { NewSortOrder = minSortOrder - 1, Id = tagId },
                        transaction);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    /// <summary>
    ///     设置标签为默认标签
    /// </summary>
    public async Task SetTagAsDefaultAsync(int tagId, bool isDefault)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            var sql = "UPDATE ticket_tag SET is_default = @IsDefault WHERE id = @Id";
            await connection.ExecuteAsync(sql, new { IsDefault = isDefault ? 1 : 0, Id = tagId });
        }
    }

    /// <summary>
    ///     重新整理排序值（将排序值重置为连续的1,2,3...）
    /// </summary>
    public async Task ReorganizeSortOrderAsync()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // 获取所有标签按当前排序
                    var selectSql = "SELECT id FROM ticket_tag ORDER BY sort_order ASC, id ASC";
                    var ids = await connection.QueryAsync<int>(selectSql, null, transaction);

                    // 重新设置排序值
                    var updateSql = "UPDATE ticket_tag SET sort_order = @SortOrder WHERE id = @Id";
                    var sortOrder = 1;
                    foreach (var id in ids)
                    {
                        await connection.ExecuteAsync(updateSql, new { SortOrder = sortOrder, Id = id }, transaction);
                        sortOrder++;
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    #endregion
}