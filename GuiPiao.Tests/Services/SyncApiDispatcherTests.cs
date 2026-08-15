using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Model;
using GuiPiao.Model.Sync;
using GuiPiao.Services;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GuiPiao.Tests.Services;

[Collection("SyncDb")]
public class SyncApiDispatcherTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public SyncApiDispatcherTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"guipiao_sync_api_{Guid.NewGuid():N}.db");
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
            // 临时库删除失败不影响断言结果
        }
    }

    [Fact]
    public async Task Health_ReturnsOkAndMaxSeq()
    {
        var dispatcher = new SyncApiDispatcher();
        var result = await dispatcher.DispatchAsync(new SyncHttpRequest
        {
            Method = "GET",
            Path = "/v1/health"
        });

        Assert.Equal(200, result.StatusCode);
        var body = SyncPayloadSerializer.FromJson<SyncHealthResponse>(result.Body);
        Assert.NotNull(body);
        Assert.True(body!.Ok);
        Assert.Equal("1", body.ApiVersion);
        Assert.Equal(0, body.MaxSeq);
    }

    [Fact]
    public async Task Changes_RequiresAuth()
    {
        var dispatcher = new SyncApiDispatcher();
        var unauthorized = await dispatcher.DispatchAsync(new SyncHttpRequest
        {
            Method = "GET",
            Path = "/v1/changes",
            Query = new Dictionary<string, string> { ["after_seq"] = "0" }
        });
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task PairThenPushRide_AppliesAndIsIdempotent()
    {
        var pairing = new SyncPairingService();
        var code = await pairing.CreatePairingCodeAsync();
        var dispatcher = new SyncApiDispatcher(pairing);

        var pairResult = await dispatcher.DispatchAsync(new SyncHttpRequest
        {
            Method = "POST",
            Path = "/v1/pair",
            Body = SyncPayloadSerializer.ToJson(new SyncPairRequest
            {
                Code = code.Code,
                DeviceName = "TestPhone"
            })
        });
        Assert.Equal(200, pairResult.StatusCode);
        var pair = SyncPayloadSerializer.FromJson<SyncPairResponse>(pairResult.Body)!;
        Assert.False(string.IsNullOrWhiteSpace(pair.DeviceToken));

        var syncId = SyncClock.NewSyncId();
        var changeId = SyncClock.NewChangeId();
        var ride = new TrainRideInfo
        {
            SyncId = syncId,
            DepartStation = "北京南",
            ArriveStation = "上海虹桥",
            TrainNo = "G2",
            DepartDate = "2026-08-15",
            DepartTime = "10:00",
            UpdatedAt = SyncClock.UtcNowIso()
        };

        var body = SyncPayloadSerializer.ToJson(new SyncPushRequest
        {
            Changes =
            [
                new SyncChangeDto
                {
                    ChangeId = changeId,
                    Entity = SyncEntityTypes.Ride,
                    SyncId = syncId,
                    Op = SyncOps.Upsert,
                    UpdatedAt = ride.UpdatedAt,
                    Payload = SyncPayloadSerializer.Ride(ride)
                }
            ]
        });

        var first = await dispatcher.DispatchAsync(new SyncHttpRequest
        {
            Method = "POST",
            Path = "/v1/changes",
            Headers = AuthHeaders(pair.DeviceId, pair.DeviceToken),
            Body = body
        });
        Assert.Equal(200, first.StatusCode);
        var firstResp = SyncPayloadSerializer.FromJson<SyncPushResponse>(first.Body)!;
        Assert.Equal(1, firstResp.Accepted);
        Assert.Equal(0, firstResp.Skipped);
        Assert.Empty(firstResp.Errors);

        var second = await dispatcher.DispatchAsync(new SyncHttpRequest
        {
            Method = "POST",
            Path = "/v1/changes",
            Headers = AuthHeaders(pair.DeviceId, pair.DeviceToken),
            Body = body
        });
        var secondResp = SyncPayloadSerializer.FromJson<SyncPushResponse>(second.Body)!;
        Assert.Equal(0, secondResp.Accepted);
        Assert.Equal(1, secondResp.Skipped);

        var pull = await dispatcher.DispatchAsync(new SyncHttpRequest
        {
            Method = "GET",
            Path = "/v1/changes",
            Query = new Dictionary<string, string> { ["after_seq"] = "0" },
            Headers = AuthHeaders(pair.DeviceId, pair.DeviceToken)
        });
        Assert.Equal(200, pull.StatusCode);
        var pullBody = SyncPayloadSerializer.FromJson<SyncPullResponse>(pull.Body)!;
        Assert.Single(pullBody.Changes);
        Assert.Equal(changeId, pullBody.Changes[0].ChangeId);

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var station = await conn.ExecuteScalarAsync<string>(
            "SELECT depart_station FROM train_ride_info WHERE sync_id = @SyncId",
            new { SyncId = syncId });
        Assert.Equal("北京南", station);
    }

    private static Dictionary<string, string> AuthHeaders(string deviceId, string token) => new()
    {
        [SyncProtocol.DeviceIdHeader] = deviceId,
        ["Authorization"] = $"Bearer {token}"
    };

    private void CreateTables()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE sync_pairing_code (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                code_hash TEXT NOT NULL,
                created_at TEXT NOT NULL,
                expires_at TEXT NOT NULL,
                consumed_at TEXT
            );
            CREATE TABLE sync_paired_device (
                device_id TEXT NOT NULL PRIMARY KEY,
                device_name TEXT NOT NULL,
                token_hash TEXT NOT NULL,
                created_at TEXT NOT NULL,
                last_seen_at TEXT,
                revoked INTEGER NOT NULL DEFAULT 0
            );
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
