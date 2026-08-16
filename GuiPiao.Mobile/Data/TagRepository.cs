using GuiPiao.Mobile.Model;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Mobile.Data;

public sealed class TagRepository
{
    private readonly MobileDatabase _db;

    public TagRepository(MobileDatabase db) => _db = db;

    public void Upsert(MobileTag tag)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO ticket_tag (
                sync_id, name, color, text_color, sort_order, is_default, updated_at, deleted_at
            ) VALUES (
                @sync_id, @name, @color, @text_color, @sort_order, @is_default, @updated_at, @deleted_at
            )
            ON CONFLICT(sync_id) DO UPDATE SET
                name=excluded.name,
                color=excluded.color,
                text_color=excluded.text_color,
                sort_order=excluded.sort_order,
                is_default=excluded.is_default,
                updated_at=excluded.updated_at,
                deleted_at=excluded.deleted_at
            """;
        cmd.Parameters.AddWithValue("@sync_id", tag.SyncId);
        cmd.Parameters.AddWithValue("@name", tag.Name ?? "");
        cmd.Parameters.AddWithValue("@color", tag.Color ?? "");
        cmd.Parameters.AddWithValue("@text_color", tag.TextColor ?? "");
        cmd.Parameters.AddWithValue("@sort_order", tag.SortOrder);
        cmd.Parameters.AddWithValue("@is_default", tag.IsDefault ? 1 : 0);
        cmd.Parameters.AddWithValue("@updated_at", tag.UpdatedAt ?? "");
        cmd.Parameters.AddWithValue("@deleted_at", (object?)tag.DeletedAt ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void SoftDelete(string syncId, string? deletedAt)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            UPDATE ticket_tag
            SET deleted_at = @deleted_at, updated_at = @updated_at
            WHERE sync_id = @sync_id
            """;
        var at = string.IsNullOrWhiteSpace(deletedAt) ? DateTime.UtcNow.ToString("o") : deletedAt;
        cmd.Parameters.AddWithValue("@deleted_at", at);
        cmd.Parameters.AddWithValue("@updated_at", at);
        cmd.Parameters.AddWithValue("@sync_id", syncId);
        cmd.ExecuteNonQuery();
    }

    public void ReplaceRideTags(string rideSyncId, IEnumerable<string> tagSyncIds)
    {
        using var connection = _db.OpenConnection();
        using var tx = connection.BeginTransaction();
        using (var del = connection.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM ride_tag WHERE ride_sync_id = @ride";
            del.Parameters.AddWithValue("@ride", rideSyncId);
            del.ExecuteNonQuery();
        }

        foreach (var tagId in tagSyncIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            using var ins = connection.CreateCommand();
            ins.Transaction = tx;
            ins.CommandText =
                "INSERT OR IGNORE INTO ride_tag (ride_sync_id, tag_sync_id) VALUES (@ride, @tag)";
            ins.Parameters.AddWithValue("@ride", rideSyncId);
            ins.Parameters.AddWithValue("@tag", tagId);
            ins.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public IReadOnlyList<string> GetTagSyncIdsForRide(string rideSyncId)
    {
        if (string.IsNullOrWhiteSpace(rideSyncId)) return Array.Empty<string>();
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT tag_sync_id FROM ride_tag WHERE ride_sync_id = @ride";
        cmd.Parameters.AddWithValue("@ride", rideSyncId);
        var list = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(id))
                list.Add(id);
        }

        return list;
    }

    public IReadOnlyList<string> GetTagNamesForRide(string rideSyncId)
    {
        if (string.IsNullOrWhiteSpace(rideSyncId)) return Array.Empty<string>();
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT t.name FROM ticket_tag t
            INNER JOIN ride_tag rt ON rt.tag_sync_id = t.sync_id
            WHERE rt.ride_sync_id = @ride
              AND (t.deleted_at IS NULL OR t.deleted_at = '')
            ORDER BY t.sort_order ASC, t.name ASC
            """;
        cmd.Parameters.AddWithValue("@ride", rideSyncId);
        var list = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(name))
                list.Add(name);
        }

        return list;
    }

    public IReadOnlyList<MobileTag> ListActive()
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT sync_id, name, color, text_color, sort_order, is_default, updated_at, deleted_at
            FROM ticket_tag
            WHERE deleted_at IS NULL OR deleted_at = ''
            ORDER BY sort_order ASC, name ASC
            """;
        var list = new List<MobileTag>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new MobileTag
            {
                SyncId = reader["sync_id"]?.ToString() ?? "",
                Name = reader["name"]?.ToString() ?? "",
                Color = reader["color"]?.ToString() ?? "",
                TextColor = reader["text_color"]?.ToString() ?? "",
                SortOrder = Convert.ToInt32(reader["sort_order"] ?? 0),
                IsDefault = Convert.ToInt32(reader["is_default"] ?? 0) != 0,
                UpdatedAt = reader["updated_at"]?.ToString() ?? "",
                DeletedAt = reader["deleted_at"] is DBNull ? null : reader["deleted_at"]?.ToString()
            });
        }

        return list;
    }
}
