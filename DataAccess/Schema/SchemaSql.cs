using System;
using System.Collections.Generic;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.DataAccess.Schema;

/// <summary>迁移步骤内可复用的 DDL 辅助（仅用于迁移脚本，业务代码勿直接依赖）。</summary>
internal static class SchemaSql
{
    public static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public static bool ColumnExists(SqliteConnection connection, SqliteTransaction transaction, string table,
        string column)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = @name";
        cmd.Parameters.AddWithValue("@name", column);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public static bool TableExists(SqliteConnection connection, SqliteTransaction transaction, string table)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name";
        cmd.Parameters.AddWithValue("@name", table);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    ///     仅当列不存在时 ADD COLUMN。
    ///     用于 v1/v2：兼容 user_version=0 的历史库（可能已部分改过结构）。
    ///     v3+ 新迁移应假定前序版本完整，直接 ALTER。
    /// </summary>
    public static void AddColumnIfMissing(SqliteConnection connection, SqliteTransaction transaction, string table,
        string column, string columnDefSql)
    {
        if (ColumnExists(connection, transaction, table, column))
            return;

        Execute(connection, transaction, $"ALTER TABLE {table} ADD COLUMN {columnDefSql}");
    }

    public static void BackfillSyncIds(SqliteConnection connection, SqliteTransaction transaction, string table)
    {
        using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = $"SELECT id FROM {table} WHERE sync_id IS NULL OR sync_id = ''";
        using var reader = select.ExecuteReader();
        var ids = new List<long>();
        while (reader.Read())
            ids.Add(reader.GetInt64(0));
        reader.Close();

        if (ids.Count == 0) return;

        var now = DateTime.UtcNow.ToString("o");
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            $"UPDATE {table} SET sync_id = @SyncId, updated_at = COALESCE(NULLIF(updated_at, ''), @UpdatedAt) WHERE id = @Id";
        var pSync = update.Parameters.Add("@SyncId", SqliteType.Text);
        var pUpdated = update.Parameters.Add("@UpdatedAt", SqliteType.Text);
        var pId = update.Parameters.Add("@Id", SqliteType.Integer);
        pUpdated.Value = now;

        foreach (var id in ids)
        {
            pId.Value = id;
            pSync.Value = Guid.NewGuid().ToString("D");
            update.ExecuteNonQuery();
        }
    }

    public static void NormalizeRideDateTimes(SqliteConnection connection, SqliteTransaction transaction)
    {
        if (!TableExists(connection, transaction, "train_ride_info"))
            return;

        using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = "SELECT id, depart_date, depart_time, arrive_time FROM train_ride_info";
        using var reader = select.ExecuteReader();
        var updates = new List<(long Id, string DepartDate, string DepartTime, string ArriveTime)>();
        while (reader.Read())
        {
            var id = reader.GetInt64(0);
            var departDate = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            var departTime = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var arriveTime = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);

            var normDate = RideDateTime.NormalizeDate(departDate);
            var normDepartTime = RideDateTime.NormalizeTime(departTime);
            var normArriveTime = RideDateTime.NormalizeTime(arriveTime);

            if (normDate != departDate || normDepartTime != departTime || normArriveTime != arriveTime)
                updates.Add((id, normDate, normDepartTime, normArriveTime));
        }

        reader.Close();
        if (updates.Count == 0) return;

        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            @"UPDATE train_ride_info
              SET depart_date = @DepartDate, depart_time = @DepartTime, arrive_time = @ArriveTime
              WHERE id = @Id";
        var pId = update.Parameters.Add("@Id", SqliteType.Integer);
        var pDate = update.Parameters.Add("@DepartDate", SqliteType.Text);
        var pDepartTime = update.Parameters.Add("@DepartTime", SqliteType.Text);
        var pArriveTime = update.Parameters.Add("@ArriveTime", SqliteType.Text);

        foreach (var row in updates)
        {
            pId.Value = row.Id;
            pDate.Value = row.DepartDate;
            pDepartTime.Value = string.IsNullOrEmpty(row.DepartTime) ? DBNull.Value : row.DepartTime;
            pArriveTime.Value = string.IsNullOrEmpty(row.ArriveTime) ? DBNull.Value : row.ArriveTime;
            update.ExecuteNonQuery();
        }
    }
}
