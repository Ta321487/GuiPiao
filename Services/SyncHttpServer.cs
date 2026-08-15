using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GuiPiao.Utils;

namespace GuiPiao.Services;

/// <summary>
///     PC 内嵌同步 HTTP 服务（HttpListener）。默认端口见 <see cref="DefaultPort"/>。
/// </summary>
public sealed class SyncHttpServer : IDisposable
{
    public const int DefaultPort = 17880;

    private static readonly Lazy<SyncHttpServer> LazyInstance = new(() => new SyncHttpServer());
    public static SyncHttpServer Instance => LazyInstance.Value;

    private readonly SyncApiDispatcher _dispatcher = new();
    private readonly LogService _log = new();
    private readonly object _gate = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; } = DefaultPort;
    public string? LastError { get; private set; }
    public IReadOnlyList<string> ListenUrls { get; private set; } = Array.Empty<string>();

    public event EventHandler? StateChanged;

    public void Start(int port = DefaultPort)
    {
        lock (_gate)
        {
            if (IsRunning) return;
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(port));

            LastError = null;
            var listener = new HttpListener();
            var prefixes = BuildPrefixes(port);
            foreach (var p in prefixes)
                listener.Prefixes.Add(p);

            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                listener.Close();
                LastError =
                    $"无法监听端口 {port}：{ex.Message}。可尝试以管理员执行 netsh http add urlacl url=http://+:{port}/ user=Everyone，或换端口。";
                _log.Error("SyncHttpServer", LastError);
                StateChanged?.Invoke(this, EventArgs.Empty);
                throw new InvalidOperationException(LastError, ex);
            }

            _listener = listener;
            Port = port;
            ListenUrls = prefixes.Select(p => p.TrimEnd('/')).ToList();
            _cts = new CancellationTokenSource();
            IsRunning = true;
            _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));
            _log.Info("SyncHttpServer", $"同步服务已启动: {string.Join(", ", ListenUrls)}");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!IsRunning) return;
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener?.Close();
            }
            catch (Exception ex)
            {
                _log.Warn("SyncHttpServer", $"停止服务时: {ex.Message}");
            }

            _listener = null;
            _cts = null;
            _loop = null;
            IsRunning = false;
            ListenUrls = Array.Empty<string>();
            _log.Info("SyncHttpServer", "同步服务已停止");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() => Stop();

    /// <summary>供 UI 展示的首选局域网地址（优先非回环 IPv4）。</summary>
    public static string GetPreferredBaseUrl(int port = DefaultPort)
    {
        var ip = GetLocalIPv4Addresses().FirstOrDefault() ?? "127.0.0.1";
        return $"http://{ip}:{port}";
    }

    public static IReadOnlyList<string> GetLocalIPv4Addresses()
    {
        var list = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                var s = ua.Address.ToString();
                if (s.StartsWith("169.254.", StringComparison.Ordinal)) continue;
                list.Add(s);
            }
        }

        return list.Distinct().ToList();
    }

    private static List<string> BuildPrefixes(int port)
    {
        var prefixes = new List<string> { $"http://127.0.0.1:{port}/" };
        foreach (var ip in GetLocalIPv4Addresses())
            prefixes.Add($"http://{ip}:{port}/");
        return prefixes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext? ctx = null;
            try
            {
                var listener = _listener;
                if (listener == null || !listener.IsListening) break;
                ctx = await listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Warn("SyncHttpServer", $"接受连接失败: {ex.Message}");
                continue;
            }

            if (ctx != null)
                _ = Task.Run(() => HandleContextAsync(ctx), CancellationToken.None);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            string? body = null;
            if (req.HasEntityBody)
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                body = await reader.ReadToEndAsync();
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in req.Headers.AllKeys)
            {
                if (key == null) continue;
                headers[key] = req.Headers[key] ?? string.Empty;
            }

            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in req.QueryString.AllKeys)
            {
                if (key == null) continue;
                query[key] = req.QueryString[key] ?? string.Empty;
            }

            var result = await _dispatcher.DispatchAsync(new SyncHttpRequest
            {
                Method = req.HttpMethod,
                Path = req.Url?.AbsolutePath ?? "/",
                Body = body,
                Headers = headers,
                Query = query
            });

            var bytes = Encoding.UTF8.GetBytes(result.Body);
            ctx.Response.StatusCode = result.StatusCode;
            ctx.Response.ContentType = result.ContentType;
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.OutputStream.Close();
        }
        catch (Exception ex)
        {
            _log.Warn("SyncHttpServer", $"处理请求失败: {ex.Message}");
            try
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
            catch
            {
                // ignore
            }
        }
    }
}
