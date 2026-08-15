using System;
using System.IO;
using GuiPiao.DataAccess.Schema;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GuiPiao.Tests.DataAccess;

public class SchemaMigratorTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteConnection _connection;

    public SchemaMigratorTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"guipiao_schema_{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void EmptyDatabase_ReachesCurrentVersion_WithCoreAndSyncTables()
    {
        Assert.Equal(0, SchemaMigrator.GetUserVersion(_connection));

        SchemaMigrator.Apply(_connection);

        Assert.Equal(SchemaMigrator.CurrentVersion, SchemaMigrator.GetUserVersion(_connection));
        Assert.True(TableExists("train_ride_info"));
        Assert.True(TableExists("ticket_tag"));
        Assert.True(TableExists("sync_change"));
        Assert.True(TableExists("sync_paired_device"));
        Assert.True(ColumnExists("train_ride_info", "sync_id"));
        Assert.True(ColumnExists("ticket_tag", "is_default"));
    }

    [Fact]
    public void Apply_IsIdempotent_WhenAlreadyCurrent()
    {
        SchemaMigrator.Apply(_connection);
        var v = SchemaMigrator.GetUserVersion(_connection);
        SchemaMigrator.Apply(_connection);
        Assert.Equal(v, SchemaMigrator.GetUserVersion(_connection));
    }

    [Fact]
    public void LegacyDatabase_WithoutUserVersion_UpgradesAndBackfillsSyncId()
    {
        // 模拟 user_version=0 的旧库：仅有简版行程表
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE train_ride_info (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    train_no TEXT,
                    depart_station TEXT,
                    arrive_station TEXT,
                    depart_date TEXT,
                    depart_time TEXT
                );
                INSERT INTO train_ride_info (train_no, depart_station, arrive_station, depart_date, depart_time)
                VALUES ('G1', '北京南', '上海虹桥', '2026-01-01', '08:00');
            ";
            cmd.ExecuteNonQuery();
        }

        Assert.Equal(0, SchemaMigrator.GetUserVersion(_connection));
        SchemaMigrator.Apply(_connection);

        Assert.Equal(SchemaMigrator.CurrentVersion, SchemaMigrator.GetUserVersion(_connection));
        Assert.True(ColumnExists("train_ride_info", "status"));
        Assert.True(ColumnExists("train_ride_info", "sync_id"));
        Assert.True(TableExists("sync_change"));

        using var check = _connection.CreateCommand();
        check.CommandText = "SELECT sync_id FROM train_ride_info WHERE id = 1";
        var syncId = check.ExecuteScalar() as string;
        Assert.False(string.IsNullOrWhiteSpace(syncId));
    }

    private bool TableExists(string name)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$n";
        cmd.Parameters.AddWithValue("$n", name);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private bool ColumnExists(string table, string column)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name=$n";
        cmd.Parameters.AddWithValue("$n", column);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
