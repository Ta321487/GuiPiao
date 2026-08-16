using System;
using System.IO;
using System.Threading.Tasks;
using GuiPiao.Services;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GuiPiao.Tests.Services;

[Collection("SyncDb")]
public class SyncPairingServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public SyncPairingServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"guipiao_sync_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";
        ConfigManager.Instance.OverrideDatabaseConnectionStringForTests(_connectionString);
        CreateSyncTables();
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
            // ignore
        }
    }

    [Fact]
    public async Task CreateAndRedeemPairingCode_IssuesDeviceToken()
    {
        var service = new SyncPairingService();
        var codeResult = await service.CreatePairingCodeAsync();

        Assert.Equal(SyncPairingService.CodeLength, codeResult.Code.Length);
        Assert.Equal(SyncPairingService.CodeTtlSeconds, codeResult.TtlSeconds);
        Assert.True(codeResult.ExpiresAtUtc > DateTime.UtcNow);
        Assert.False(SyncPairingService.IsDisplayExpired(codeResult.ExpiresAtUtc));

        var redeem = await service.RedeemPairingCodeAsync(codeResult.Code, "Pixel-测试");
        Assert.True(redeem.Success, redeem.ErrorMessage);
        Assert.False(string.IsNullOrWhiteSpace(redeem.DeviceId));
        Assert.False(string.IsNullOrWhiteSpace(redeem.DeviceToken));

        var auth = await service.ValidateDeviceTokenAsync(redeem.DeviceId!, redeem.DeviceToken!);
        Assert.True(auth.Success);

        var reuse = await service.RedeemPairingCodeAsync(codeResult.Code, "other");
        Assert.False(reuse.Success);
        Assert.True(await service.IsPairingCodeConsumedAsync(codeResult.Code));
    }

    [Fact]
    public async Task RevokedDevice_CannotAuthenticate()
    {
        var service = new SyncPairingService();
        var code = await service.CreatePairingCodeAsync();
        var redeem = await service.RedeemPairingCodeAsync(code.Code, "phone");
        Assert.True(redeem.Success);

        await service.RevokeDeviceAsync(redeem.DeviceId!);
        var auth = await service.ValidateDeviceTokenAsync(redeem.DeviceId!, redeem.DeviceToken!);
        Assert.False(auth.Success);
    }

    [Fact]
    public void DisplayAndRedeemWindows_ShareSingleTtlConstants()
    {
        var expires = DateTime.UtcNow.AddSeconds(-1);
        Assert.True(SyncPairingService.IsDisplayExpired(expires));
        Assert.False(SyncPairingService.IsRedeemExpired(expires));

        var pastGrace = DateTime.UtcNow.AddSeconds(-(SyncPairingService.PreviousWindowGraceSeconds + 1));
        Assert.True(SyncPairingService.IsDisplayExpired(pastGrace));
        Assert.True(SyncPairingService.IsRedeemExpired(pastGrace));

        Assert.Equal(0, SyncPairingService.GetRemainingDisplaySeconds(expires));
        Assert.Equal("1 2 3 4 5 6", SyncPairingService.FormatCodeForDisplay("123456"));
    }

    [Fact]
    public async Task PreviousWindowCode_StillRedeemableAfterDisplayExpiry()
    {
        var service = new SyncPairingService();
        var first = await service.CreatePairingCodeAsync();

        // 模拟展示已过期但仍在上一窗容错内：直接改库 expires_at
        await SetExpiresAtAsync(first.Code, DateTime.UtcNow.AddSeconds(-5));

        Assert.True(SyncPairingService.IsDisplayExpired(DateTime.UtcNow.AddSeconds(-5)));

        var redeem = await service.RedeemPairingCodeAsync(first.Code, "late-phone");
        Assert.True(redeem.Success, redeem.ErrorMessage);
    }

    [Fact]
    public async Task InvalidateActiveCodes_BlocksRedeem()
    {
        var service = new SyncPairingService();
        var code = await service.CreatePairingCodeAsync();
        await service.InvalidateActivePairingCodesAsync();

        var redeem = await service.RedeemPairingCodeAsync(code.Code, "phone");
        Assert.False(redeem.Success);
    }

    private async Task SetExpiresAtAsync(string plainCode, DateTime expiresAtUtc)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        // 用最新一行更新（测试库通常只有一条未兑码）
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE sync_pairing_code SET expires_at = @Expires WHERE consumed_at IS NULL";
        cmd.Parameters.AddWithValue("@Expires", expiresAtUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
        _ = plainCode;
    }

    private void CreateSyncTables()
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
            );";
        cmd.ExecuteNonQuery();
    }
}
