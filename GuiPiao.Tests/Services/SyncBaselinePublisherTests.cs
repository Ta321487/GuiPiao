using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.DataAccess;
using GuiPiao.Model.Sync;
using GuiPiao.Services;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GuiPiao.Tests.Services;

[Collection("SyncDb")]
public class SyncBaselinePublisherTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public SyncBaselinePublisherTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"guipiao_baseline_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";
        ConfigManager.Instance.OverrideDatabaseConnectionStringForTests(_connectionString);
        CreateTables();
    }

    public void Dispose()
    {
        try
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task PublishMissing_WritesRideWithoutExistingChange()
    {
        var syncId = SyncClock.NewSyncId();
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync(
                @"INSERT INTO train_ride_info (depart_station, train_no, arrive_station, depart_date, sync_id, updated_at)
                  VALUES ('北京南', 'G1', '上海虹桥', '2026-08-01', @SyncId, @UpdatedAt)",
                new { SyncId = syncId, UpdatedAt = SyncClock.UtcNowIso() });
        }

        var publisher = new SyncBaselinePublisher();
        var first = await publisher.PublishMissingAsync();
        Assert.Equal(1, first.PublishedRides);
        Assert.True(first.MaxSeq >= 1);

        var changes = new SyncChangeRepository();
        var rows = (await changes.GetChangesSinceAsync(0)).ToList();
        Assert.Single(rows);
        Assert.Equal(SyncEntityTypes.Ride, rows[0].Entity);
        Assert.Equal(syncId, rows[0].SyncId);
        Assert.Equal(SyncOps.Upsert, rows[0].Op);
        Assert.Contains("G1", rows[0].Payload);

        var second = await publisher.PublishMissingAsync();
        Assert.Equal(0, second.PublishedRides);
        Assert.Equal(1, second.SkippedRides);
    }

    private void CreateTables()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE sync_change (
                change_id TEXT NOT NULL PRIMARY KEY,
                entity TEXT NOT NULL,
                sync_id TEXT NOT NULL,
                op TEXT NOT NULL,
                payload TEXT,
                updated_at TEXT NOT NULL,
                seq INTEGER NOT NULL UNIQUE,
                device_id TEXT
            );
            CREATE TABLE train_ride_info (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ticket_number TEXT, check_in_location TEXT, depart_station TEXT, train_no TEXT,
                arrive_station TEXT, depart_station_pinyin TEXT, arrive_station_pinyin TEXT,
                depart_date TEXT, depart_time TEXT, arrive_time TEXT, arrive_day_offset INTEGER DEFAULT 0,
                coach_no TEXT, seat_no TEXT, money REAL, seat_type TEXT, additional_info TEXT,
                ticket_purpose TEXT, ticket_modification_type TEXT, ticket_type_flags INTEGER DEFAULT 0,
                payment_channel_flags INTEGER DEFAULT 0, hint TEXT, depart_station_code TEXT,
                arrive_station_code TEXT, status INTEGER DEFAULT 0,
                sync_id TEXT, updated_at TEXT, deleted_at TEXT
            );
            CREATE TABLE ticket_tag (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                name TEXT, color TEXT, text_color TEXT, sort_order INTEGER, is_default INTEGER DEFAULT 0,
                created_at TEXT, sync_id TEXT, updated_at TEXT, deleted_at TEXT
            );
            CREATE TABLE train_ride_tag (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                train_ride_id INTEGER, tag_id INTEGER, created_at TEXT
            );";
        cmd.ExecuteNonQuery();
    }
}
