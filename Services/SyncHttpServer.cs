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
///     PC 内嵌同步 HTTP 服务。使用 TcpListener，不经 HTTP.sys，无需 urlacl。
///     allowLan=false 时仅 Loopback；true 时绑定 IPAddress.Any。
/// </summary>
public sealed class SyncHttpServer : IDisposable
{
    public const int DefaultPort = 17880;

    private static readonly Lazy<SyncHttpServer> LazyInstance = new(() => new SyncHttpServer());
    public static SyncHttpServer Instance => LazyInstance.Value;

    private readonly SyncApiDispatcher _dispatcher = new();
    private readonly LogService _log = new();
    private readonly object _gate = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; } = DefaultPort;
    public bool AllowLan { get; private set; }
    public string? LastError { get; private set; }
    public string? LastWarning { get; private set; }
    public IReadOnlyList<string> ListenUrls { get; private set; } = Array.Empty<string>();

    public event EventHandler? StateChanged;

    public void Start(int port = DefaultPort, bool allowLan = false)
    {
        lock (_gate)
        {
            if (IsRunning) return;
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(port));

            LastError = null;
            LastWarning = null;

            if (!TryStartListener(port, allowLan, out var usedLan, out var warning, out var error))
            {
                LastError = error;
                _log.Error("SyncHttpServer", LastError ?? "启动失败");
                StateChanged?.Invoke(this, EventArgs.Empty);
                throw new InvalidOperationException(LastError);
            }

            AllowLan = usedLan;
            LastWarning = warning;
            if (!string.IsNullOrWhiteSpace(warning))
                _log.Warn("SyncHttpServer", warning);

            Port = port;
            _cts = new CancellationTokenSource();
            IsRunning = true;
            _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));
            _log.Info("SyncHttpServer", $"同步服务已启动: {string.Join(", ", ListenUrls)}");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool TryStartListener(
        int port,
        bool allowLan,
        out bool usedLan,
        out string? warning,
        out string? error)
    {
        usedLan = false;
        warning = null;
        error = null;

        if (allowLan)
        {
            if (TryBind(port, allowLan: true))
            {
                usedLan = true;
                return true;
            }

            if (TryBind(port, allowLan: false))
            {
                usedLan = false;
                warning = $"端口 {port} 无法绑定到所有网卡，已回退为本机回环。请检查端口占用或防火墙。";
                return true;
            }

            error = BuildBindFailureMessage(port, triedLan: true);
            return false;
        }

        if (TryBind(port, allowLan: false))
        {
            usedLan = false;
            return true;
        }

        error = BuildBindFailureMessage(port, triedLan: false);
        return false;
    }

    private bool TryBind(int port, bool allowLan)
    {
        TcpListener? listener = null;
        try
        {
            var address = allowLan ? IPAddress.Any : IPAddress.Loopback;
            listener = new TcpListener(address, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            _listener = listener;
            ListenUrls = BuildListenUrls(port, allowLan);
            return true;
        }
        catch (SocketException ex)
        {
            _log.Warn("SyncHttpServer", $"绑定失败 ({(allowLan ? "Any" : "Loopback")}): {ex.SocketErrorCode} {ex.Message}");
            try
            {
                listener?.Stop();
            }
            catch
            {
                // ignore
            }

            return false;
        }
        catch (Exception ex)
        {
            _log.Warn("SyncHttpServer", $"绑定失败: {ex.Message}");
            try
            {
                listener?.Stop();
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }

    private static string BuildBindFailureMessage(int port, bool triedLan)
    {
        var scope = triedLan ? "本机与局域网" : "本机";
        return $"无法监听端口 {port}（{scope}）。端口可能已被占用，请在高级设置中更换端口。";
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
            }
            catch (Exception ex)
            {
                _log.Warn("SyncHttpServer", $"停止服务时: {ex.Message}");
            }

            _listener = null;
            _cts = null;
            _loop = null;
            IsRunning = false;
            AllowLan = false;
            ListenUrls = Array.Empty<string>();
            _log.Info("SyncHttpServer", "同步服务已停止");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() => Stop();

    public static string GetPreferredBaseUrl(int port = DefaultPort, bool preferLan = false)
    {
        if (preferLan)
        {
            var ip = GetLocalIPv4Addresses().FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip))
                return $"http://{ip}:{port}";
        }

        return $"http://127.0.0.1:{port}";
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

    /// <summary>对外展示用监听地址（非 HTTP.sys 前缀）。</summary>
    public static IReadOnlyList<string> BuildPrefixes(int port, bool allowLan) =>
        BuildListenUrls(port, allowLan);

    private static IReadOnlyList<string> BuildListenUrls(int port, bool allowLan)
    {
        var urls = new List<string> { $"http://127.0.0.1:{port}" };
        if (allowLan)
        {
            foreach (var ip in GetLocalIPv4Addresses())
                urls.Add($"http://{ip}:{port}");
        }

        return urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                var listener = _listener;
                if (listener == null) break;
                client = await listener.AcceptTcpClientAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (ct.IsCancellationRequested) break;
                continue;
            }
            catch (Exception ex)
            {
                _log.Warn("SyncHttpServer", $"接受连接失败: {ex.Message}");
                continue;
            }

            if (client != null)
            {
                var c = client;
                _ = Task.Run(() => HandleClientAsync(c), CancellationToken.None);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                using var stream = client.GetStream();
                stream.ReadTimeout = 180000;
                stream.WriteTimeout = 180000;

                var request = await ReadHttpRequestAsync(stream);
                if (request == null)
                {
                    await WriteHttpResponseAsync(stream, 400, "text/plain; charset=utf-8", "bad_request");
                    return;
                }

                var result = await _dispatcher.DispatchAsync(request);
                await WriteHttpResponseAsync(stream, result.StatusCode, result.ContentType, result.Body);
            }
            catch (Exception ex)
            {
                _log.Warn("SyncHttpServer", $"处理请求失败: {ex.Message}");
            }
        }
    }

    private static async Task<SyncHttpRequest?> ReadHttpRequestAsync(NetworkStream stream)
    {
        var buffer = new byte[8192];
        using var ms = new MemoryStream();
        var headerEnd = -1;

        while (headerEnd < 0 && ms.Length < 64 * 1024)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read <= 0) break;
            ms.Write(buffer, 0, read);
            headerEnd = IndexOfHeaderEnd(ms.GetBuffer(), (int)ms.Length);
        }

        if (headerEnd < 0) return null;

        var raw = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
        var headerText = raw[..headerEnd];
        var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
        if (lines.Length == 0) return null;

        var parts = lines[0].Split(' ');
        if (parts.Length < 2) return null;
        var method = parts[0];
        var target = parts[1];

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line)) break;
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
        }

        var path = target;
        var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var q = target.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0)
        {
            path = target[..q];
            ParseQuery(target[(q + 1)..], query);
        }

        var bodyBytesAlready = (int)ms.Length - (headerEnd + 4);
        var contentLength = 0;
        if (headers.TryGetValue("Content-Length", out var clRaw))
            int.TryParse(clRaw, out contentLength);

        string? body = null;
        if (contentLength > 16 * 1024 * 1024)
            throw new InvalidOperationException("request_body_too_large");

        if (contentLength > 0)
        {
            var bodyBuffer = new byte[contentLength];
            var copied = Math.Max(0, bodyBytesAlready);
            if (copied > 0)
                Buffer.BlockCopy(ms.GetBuffer(), headerEnd + 4, bodyBuffer, 0, Math.Min(copied, contentLength));

            var offset = Math.Min(copied, contentLength);
            while (offset < contentLength)
            {
                var read = await stream.ReadAsync(bodyBuffer.AsMemory(offset, contentLength - offset));
                if (read <= 0) break;
                offset += read;
            }

            body = Encoding.UTF8.GetString(bodyBuffer, 0, offset);
        }

        return new SyncHttpRequest
        {
            Method = method,
            Path = path,
            Body = body,
            Headers = headers,
            Query = query
        };
    }

    private static int IndexOfHeaderEnd(byte[] data, int length)
    {
        for (var i = 0; i + 3 < length; i++)
        {
            if (data[i] == (byte)'\r' && data[i + 1] == (byte)'\n' &&
                data[i + 2] == (byte)'\r' && data[i + 3] == (byte)'\n')
                return i;
        }

        return -1;
    }

    private static void ParseQuery(string query, Dictionary<string, string> target)
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq < 0)
            {
                target[Uri.UnescapeDataString(pair)] = string.Empty;
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..eq]);
            var value = Uri.UnescapeDataString(pair[(eq + 1)..]);
            target[key] = value;
        }
    }

    private static async Task WriteHttpResponseAsync(
        NetworkStream stream,
        int statusCode,
        string contentType,
        string body)
    {
        var payload = Encoding.UTF8.GetBytes(body ?? string.Empty);
        var reason = statusCode switch
        {
            200 => "OK",
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "OK"
        };

        var header =
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {payload.Length}\r\n" +
            "Connection: close\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "\r\n";

        var headerBytes = Encoding.ASCII.GetBytes(header);
        await stream.WriteAsync(headerBytes);
        if (payload.Length > 0)
            await stream.WriteAsync(payload);
        await stream.FlushAsync();
    }
}
