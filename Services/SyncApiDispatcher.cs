using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GuiPiao.DataAccess;
using GuiPiao.Model.Sync;

namespace GuiPiao.Services;

/// <summary>
///     同步 HTTP 路由处理（与传输无关，便于单测）。
///     路径：/v1/health、/v1/pair、/v1/changes
/// </summary>
public class SyncApiDispatcher
{
    private readonly SyncPairingService _pairing;
    private readonly SyncChangeRepository _changes;
    private readonly SyncIngressService _ingress;

    public SyncApiDispatcher(
        SyncPairingService? pairing = null,
        SyncChangeRepository? changes = null,
        SyncIngressService? ingress = null)
    {
        _pairing = pairing ?? new SyncPairingService();
        _changes = changes ?? new SyncChangeRepository();
        _ingress = ingress ?? new SyncIngressService();
    }

    public async Task<SyncHttpResult> DispatchAsync(SyncHttpRequest request)
    {
        var path = NormalizePath(request.Path);
        var method = (request.Method ?? "GET").Trim().ToUpperInvariant();

        try
        {
            if (method == "GET" && path == "/v1/health")
                return await HealthAsync();

            if (method == "POST" && path == "/v1/pair")
                return await PairAsync(request.Body);

            if (method == "GET" && path == "/v1/changes")
                return await PullAsync(request);

            if (method == "POST" && path == "/v1/changes")
                return await PushAsync(request);

            return SyncHttpResult.Json(404, new SyncErrorResponse { Error = "not_found" });
        }
        catch (Exception ex)
        {
            return SyncHttpResult.Json(500, new SyncErrorResponse { Error = ex.Message });
        }
    }

    private async Task<SyncHttpResult> HealthAsync()
    {
        var maxSeq = await _changes.GetMaxSeqAsync();
        return SyncHttpResult.Json(200, new SyncHealthResponse
        {
            Ok = true,
            ApiVersion = SyncProtocol.ApiVersion,
            MaxSeq = maxSeq
        });
    }

    private async Task<SyncHttpResult> PairAsync(string? body)
    {
        var req = SyncPayloadSerializer.FromJson<SyncPairRequest>(body);
        if (req == null)
            return SyncHttpResult.Json(400, new SyncErrorResponse { Error = "invalid_json" });

        var redeem = await _pairing.RedeemPairingCodeAsync(req.Code, req.DeviceName);
        if (!redeem.Success)
            return SyncHttpResult.Json(400, new SyncErrorResponse { Error = redeem.ErrorMessage ?? "pair_failed" });

        return SyncHttpResult.Json(200, new SyncPairResponse
        {
            DeviceId = redeem.DeviceId!,
            DeviceName = redeem.DeviceName!,
            DeviceToken = redeem.DeviceToken!
        });
    }

    private async Task<SyncHttpResult> PullAsync(SyncHttpRequest request)
    {
        var auth = await AuthenticateAsync(request);
        if (auth == null)
            return SyncHttpResult.Json(401, new SyncErrorResponse { Error = "unauthorized" });

        var afterSeq = ParseLong(request.Query, "after_seq", 0);
        var limit = (int)Math.Clamp(ParseLong(request.Query, "limit", 500), 1, 1000);
        var rows = (await _changes.GetChangesSinceAsync(afterSeq, limit + 1)).ToList();
        var hasMore = rows.Count > limit;
        if (hasMore) rows = rows.Take(limit).ToList();

        var maxSeq = await _changes.GetMaxSeqAsync();
        return SyncHttpResult.Json(200, new SyncPullResponse
        {
            Changes = rows.Select(ToDto).ToList(),
            MaxSeq = maxSeq,
            HasMore = hasMore
        });
    }

    private async Task<SyncHttpResult> PushAsync(SyncHttpRequest request)
    {
        var auth = await AuthenticateAsync(request);
        if (auth == null)
            return SyncHttpResult.Json(401, new SyncErrorResponse { Error = "unauthorized" });

        var body = SyncPayloadSerializer.FromJson<SyncPushRequest>(request.Body);
        if (body?.Changes == null)
            return SyncHttpResult.Json(400, new SyncErrorResponse { Error = "invalid_json" });

        var result = await _ingress.ApplyPushAsync(auth.DeviceId!, body.Changes);
        return SyncHttpResult.Json(200, result);
    }

    private async Task<SyncAuthResult?> AuthenticateAsync(SyncHttpRequest request)
    {
        if (!request.Headers.TryGetValue(SyncProtocol.DeviceIdHeader, out var deviceId) ||
            string.IsNullOrWhiteSpace(deviceId))
            return null;

        var token = ExtractBearer(request.Headers);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var auth = await _pairing.ValidateDeviceTokenAsync(deviceId.Trim(), token.Trim());
        return auth.Success ? auth : null;
    }

    private static string? ExtractBearer(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Authorization", out var raw) &&
            !headers.TryGetValue("authorization", out raw))
            return null;

        const string prefix = "Bearer ";
        if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return raw[prefix.Length..].Trim();
        return null;
    }

    private static SyncChangeDto ToDto(SyncChangeRecord r) => new()
    {
        ChangeId = r.ChangeId,
        Entity = r.Entity,
        SyncId = r.SyncId,
        Op = r.Op,
        Payload = r.Payload,
        UpdatedAt = r.UpdatedAt,
        Seq = r.Seq,
        DeviceId = r.DeviceId
    };

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var p = path.Trim();
        var q = p.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0) p = p[..q];
        if (!p.StartsWith('/')) p = "/" + p;
        if (p.Length > 1 && p.EndsWith('/')) p = p.TrimEnd('/');
        return p;
    }

    private static long ParseLong(IReadOnlyDictionary<string, string> query, string key, long fallback)
    {
        if (!query.TryGetValue(key, out var raw)) return fallback;
        return long.TryParse(raw, out var n) ? n : fallback;
    }
}

public class SyncHttpRequest
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/";
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Query { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class SyncHttpResult
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = "application/json; charset=utf-8";
    public string Body { get; set; } = string.Empty;

    public static SyncHttpResult Json(int status, object payload) => new()
    {
        StatusCode = status,
        Body = SyncPayloadSerializer.ToJson(payload)
    };
}
