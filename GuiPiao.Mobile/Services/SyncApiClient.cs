using System.Net.Http.Headers;
using System.Text;
using GuiPiao.Mobile.Model;
using GuiPiao.Model.Sync;

namespace GuiPiao.Mobile.Services;

/// <summary>PC SyncHttpServer 客户端。不使用 ConfigureAwait(false)，便于调用方回到 UI 上下文。</summary>
public sealed class SyncApiClient
{
    private readonly HttpClient _http;
    private readonly HttpClient _ocrHttp;

    public SyncApiClient(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _ocrHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public Task<SyncHealthResponse> HealthAsync(string baseUrl, CancellationToken ct = default) =>
        SendAsync<SyncHealthResponse>(_http, HttpMethod.Get, Combine(baseUrl, "/v1/health"), body: null, auth: null, ct);

    public Task<SyncPairResponse> PairAsync(
        string baseUrl, string code, string deviceName, CancellationToken ct = default) =>
        SendAsync<SyncPairResponse>(
            _http, HttpMethod.Post, Combine(baseUrl, "/v1/pair"),
            SyncJson.ToJson(new SyncPairRequest { Code = code.Trim(), DeviceName = deviceName.Trim() }),
            auth: null, ct);

    public Task<SyncPullResponse> PullAsync(
        SyncClientConfig config, long afterSeq, int limit = 500, CancellationToken ct = default) =>
        SendAsync<SyncPullResponse>(
            _http, HttpMethod.Get,
            $"{Combine(config.BaseUrl, "/v1/changes")}?after_seq={afterSeq}&limit={limit}",
            body: null, config, ct);

    public Task<SyncPushResponse> PushAsync(
        SyncClientConfig config, IReadOnlyList<SyncChangeDto> changes, CancellationToken ct = default) =>
        SendAsync<SyncPushResponse>(
            _http, HttpMethod.Post, Combine(config.BaseUrl, "/v1/changes"),
            SyncJson.ToJson(new SyncPushRequest { Changes = changes.ToList() }),
            config, ct);

    public Task<SyncOcrResponse> OcrAsync(
        SyncClientConfig config, byte[] imageBytes, string? fileName = null, CancellationToken ct = default) =>
        SendAsync<SyncOcrResponse>(
            _ocrHttp, HttpMethod.Post, Combine(config.BaseUrl, "/v1/ocr"),
            SyncJson.ToJson(new SyncOcrRequest
            {
                ImageBase64 = Convert.ToBase64String(imageBytes),
                FileName = fileName
            }),
            config, ct);

    public Task<SyncStationsResponse> StationsAsync(SyncClientConfig config, CancellationToken ct = default) =>
        SendAsync<SyncStationsResponse>(
            _http, HttpMethod.Get, Combine(config.BaseUrl, "/v1/stations"), body: null, config, ct);

    public Task<SyncConflictListResponse> ConflictsAsync(SyncClientConfig config, CancellationToken ct = default) =>
        SendAsync<SyncConflictListResponse>(
            _http, HttpMethod.Get, Combine(config.BaseUrl, "/v1/conflicts"), body: null, config, ct);

    public Task<SyncConflictResolveResponse> ResolveConflictAsync(
        SyncClientConfig config, long id, string keep, CancellationToken ct = default) =>
        SendAsync<SyncConflictResolveResponse>(
            _http, HttpMethod.Post, Combine(config.BaseUrl, "/v1/conflicts/resolve"),
            SyncJson.ToJson(new SyncConflictResolveRequest { Id = id, Keep = keep }),
            config, ct);

    private static async Task<T> SendAsync<T>(
        HttpClient client,
        HttpMethod method,
        string url,
        string? body,
        SyncClientConfig? auth,
        CancellationToken ct)
        where T : class
    {
        using var req = new HttpRequestMessage(method, url);
        if (body != null)
            req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        if (auth != null)
            ApplyAuth(req, auth);

        using var resp = await client.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        EnsureOk(resp, text);
        return SyncJson.FromJson<T>(text)
               ?? throw new InvalidOperationException("invalid_json");
    }

    private static void ApplyAuth(HttpRequestMessage req, SyncClientConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DeviceId) || string.IsNullOrWhiteSpace(config.DeviceToken))
            throw new InvalidOperationException("not_paired");

        req.Headers.TryAddWithoutValidation(SyncProtocol.DeviceIdHeader, config.DeviceId);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.DeviceToken);
    }

    private static string Combine(string baseUrl, string path)
    {
        var root = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("base_url_required");
        return root + path;
    }

    private static void EnsureOk(HttpResponseMessage resp, string body)
    {
        if (resp.IsSuccessStatusCode) return;
        var err = SyncJson.FromJson<SyncErrorResponse>(body)?.Error
                  ?? SyncJson.FromJson<SyncConflictResolveResponse>(body)?.Error;
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(err)
                ? $"http_{(int)resp.StatusCode}"
                : err);
    }
}
