using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using GuiPiao.Model.Sync;
using GuiPiao.Utils;
using Microsoft.Data.Sqlite;

namespace GuiPiao.Services;

/// <summary>
///     配对码：短时展示码 → 兑换长期 device token（仅存哈希）。
///     展示 TTL 与兑换窗口以本类常量为唯一真相源（UI 必须同源）。
/// </summary>
public class SyncPairingService
{
    public const int CodeLength = 6;

    /// <summary>展示与自动换码周期（秒）。倒计时归零即作废展示并生成新码。</summary>
    public const int CodeTtlSeconds = 60;

    /// <summary>
    ///     上一窗兑换容错（秒）：展示已过期后仍可兑换一小段时间，避免刚刷新时手机输完被拒。
    ///     仅影响 Redeem，不影响 UI 倒计时。
    /// </summary>
    public const int PreviousWindowGraceSeconds = 30;

    private const int TokenBytes = 32;

    private readonly string _connectionString = ConfigManager.Instance.DatabaseConnectionString;
    private readonly LogService _logService = new();

    /// <summary>展示窗口是否已结束（与 UI 倒计时一致）。</summary>
    public static bool IsDisplayExpired(DateTime expiresAtUtc, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return now > expiresAtUtc;
    }

    /// <summary>兑换窗口是否已结束（展示过期 + 上一窗容错）。</summary>
    public static bool IsRedeemExpired(DateTime expiresAtUtc, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return now > expiresAtUtc.AddSeconds(PreviousWindowGraceSeconds);
    }

    /// <summary>剩余展示秒数（≥0），供 UI 倒计时绑定。</summary>
    public static int GetRemainingDisplaySeconds(DateTime expiresAtUtc, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var seconds = (int)Math.Ceiling((expiresAtUtc - now).TotalSeconds);
        return Math.Max(0, seconds);
    }

    /// <summary>6 位码格式化为「4 8 2 9 1 7」。</summary>
    public static string FormatCodeForDisplay(string code)
    {
        if (string.IsNullOrEmpty(code)) return string.Empty;
        return string.Join(" ", code.Select(c => c.ToString()));
    }

    /// <summary>生成配对码。未兑换且仍在兑换容错内的旧码保留，便于上一窗兑换。</summary>
    public async Task<SyncPairingCodeResult> CreatePairingCodeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var now = DateTime.UtcNow;
        var redeemCutoff = now.AddSeconds(-PreviousWindowGraceSeconds).ToString("o");

        await connection.ExecuteAsync(
            @"DELETE FROM sync_pairing_code
              WHERE consumed_at IS NOT NULL OR expires_at < @RedeemCutoff",
            new { RedeemCutoff = redeemCutoff });

        var code = GenerateNumericCode(CodeLength);
        var codeHash = Hash(code);
        var expires = now.AddSeconds(CodeTtlSeconds);

        await connection.ExecuteAsync(
            @"INSERT INTO sync_pairing_code (code_hash, created_at, expires_at, consumed_at)
              VALUES (@CodeHash, @CreatedAt, @ExpiresAt, NULL)",
            new
            {
                CodeHash = codeHash,
                CreatedAt = now.ToString("o"),
                ExpiresAt = expires.ToString("o")
            });

        _logService.Info("SyncPairingService", $"已生成配对码，展示 {CodeTtlSeconds} 秒有效");

        return new SyncPairingCodeResult
        {
            Code = code,
            ExpiresAtUtc = expires,
            TtlSeconds = CodeTtlSeconds
        };
    }

    /// <summary>作废所有未兑换配对码（停止展示会话时调用）。</summary>
    public async Task InvalidateActivePairingCodesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var now = SyncClock.UtcNowIso();
        await connection.ExecuteAsync(
            @"UPDATE sync_pairing_code SET consumed_at = @Now
              WHERE consumed_at IS NULL",
            new { Now = now });
        _logService.Info("SyncPairingService", "已作废当前未兑换配对码");
    }

    /// <summary>
    ///     手机用配对码兑换 device token。成功后码作废。
    /// </summary>
    public async Task<SyncPairingRedeemResult> RedeemPairingCodeAsync(string code, string deviceName)
    {
        if (string.IsNullOrWhiteSpace(code))
            return SyncPairingRedeemResult.Fail("配对码不能为空");

        var normalized = new string(code.Where(char.IsDigit).ToArray());
        if (normalized.Length != CodeLength)
            return SyncPairingRedeemResult.Fail("配对码格式不正确");

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var tx = connection.BeginTransaction();

        try
        {
            var nowIso = SyncClock.UtcNowIso();
            var codeHash = Hash(normalized);

            var row = await connection.QuerySingleOrDefaultAsync<dynamic>(
                @"SELECT id AS Id, expires_at AS ExpiresAt, consumed_at AS ConsumedAt
                  FROM sync_pairing_code
                  WHERE code_hash = @CodeHash
                  ORDER BY id DESC
                  LIMIT 1",
                new { CodeHash = codeHash },
                tx);

            if (row == null)
                return SyncPairingRedeemResult.Fail("配对码无效");

            if (row.ConsumedAt != null)
                return SyncPairingRedeemResult.Fail("配对码已使用");

            if (!DateTime.TryParse((string)row.ExpiresAt, null,
                    DateTimeStyles.RoundtripKind, out DateTime expiresAt)
                || IsRedeemExpired(expiresAt))
                return SyncPairingRedeemResult.Fail("配对码已过期");

            await connection.ExecuteAsync(
                "UPDATE sync_pairing_code SET consumed_at = @Now WHERE id = @Id",
                new { Now = nowIso, Id = (long)row.Id },
                tx);

            var deviceId = SyncClock.NewSyncId();
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes));
            var tokenHash = Hash(token);
            var name = string.IsNullOrWhiteSpace(deviceName) ? "手机" : deviceName.Trim();
            if (name.Length > 64) name = name[..64];

            await connection.ExecuteAsync(
                @"INSERT INTO sync_paired_device
                    (device_id, device_name, token_hash, created_at, last_seen_at, revoked)
                  VALUES (@DeviceId, @DeviceName, @TokenHash, @CreatedAt, @LastSeenAt, 0)",
                new
                {
                    DeviceId = deviceId,
                    DeviceName = name,
                    TokenHash = tokenHash,
                    CreatedAt = nowIso,
                    LastSeenAt = nowIso
                },
                tx);

            tx.Commit();

            _logService.Info("SyncPairingService", $"设备已配对: {name} ({deviceId})");

            return SyncPairingRedeemResult.Ok(deviceId, name, token);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logService.Error("SyncPairingService", $"兑换配对码失败: {ex.Message}");
            return SyncPairingRedeemResult.Fail("配对失败，请重试");
        }
    }

    /// <summary>校验设备 token；通过则刷新 last_seen。</summary>
    public async Task<SyncAuthResult> ValidateDeviceTokenAsync(string deviceId, string token)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(token))
            return SyncAuthResult.Fail();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var row = await connection.QuerySingleOrDefaultAsync<SyncPairedDevice>(
            @"SELECT device_id AS DeviceId, device_name AS DeviceName, token_hash AS TokenHash,
                     created_at AS CreatedAt, last_seen_at AS LastSeenAt, revoked AS Revoked
              FROM sync_paired_device WHERE device_id = @DeviceId",
            new { DeviceId = deviceId });

        if (row == null || row.Revoked)
            return SyncAuthResult.Fail();

        if (!FixedTimeEquals(row.TokenHash, Hash(token)))
            return SyncAuthResult.Fail();

        var now = SyncClock.UtcNowIso();
        await connection.ExecuteAsync(
            "UPDATE sync_paired_device SET last_seen_at = @Now WHERE device_id = @DeviceId",
            new { Now = now, DeviceId = deviceId });

        return SyncAuthResult.Ok(row.DeviceId, row.DeviceName);
    }

    public async Task<IReadOnlyList<SyncPairedDevice>> ListDevicesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var list = await connection.QueryAsync<SyncPairedDevice>(
            @"SELECT device_id AS DeviceId, device_name AS DeviceName, token_hash AS TokenHash,
                     created_at AS CreatedAt, last_seen_at AS LastSeenAt, revoked AS Revoked
              FROM sync_paired_device
              ORDER BY created_at DESC");
        return list.Select(d => new SyncPairedDevice
        {
            DeviceId = d.DeviceId,
            DeviceName = d.DeviceName,
            TokenHash = string.Empty,
            CreatedAt = d.CreatedAt,
            LastSeenAt = d.LastSeenAt,
            Revoked = d.Revoked
        }).ToList();
    }

    public async Task RevokeDeviceAsync(string deviceId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "UPDATE sync_paired_device SET revoked = 1 WHERE device_id = @DeviceId",
            new { DeviceId = deviceId });
        _logService.Info("SyncPairingService", $"已撤销设备: {deviceId}");
    }

    private static string GenerateNumericCode(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = (char)('0' + bytes[i] % 10);
        return new string(chars);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}

public class SyncPairingCodeResult
{
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>等于 <see cref="SyncPairingService.CodeTtlSeconds"/>，便于调用方无需再读常量。</summary>
    public int TtlSeconds { get; set; }
}

public class SyncPairingRedeemResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceToken { get; set; }

    public static SyncPairingRedeemResult Ok(string deviceId, string deviceName, string token) => new()
    {
        Success = true,
        DeviceId = deviceId,
        DeviceName = deviceName,
        DeviceToken = token
    };

    public static SyncPairingRedeemResult Fail(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}

public class SyncAuthResult
{
    public bool Success { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }

    public static SyncAuthResult Ok(string deviceId, string deviceName) => new()
    {
        Success = true,
        DeviceId = deviceId,
        DeviceName = deviceName
    };

    public static SyncAuthResult Fail() => new() { Success = false };
}
