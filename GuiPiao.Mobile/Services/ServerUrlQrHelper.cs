using System.Text.RegularExpressions;

namespace GuiPiao.Mobile.Services;

/// <summary>从二维码/文本解析 Sync 服务地址，以及可选的 6 位配对码。</summary>
public static class ServerUrlQrHelper
{
    private static readonly Regex HttpUrl = new(
        @"https?://[^\s""'<>#]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HostPort = new(
        @"^(?<host>(?:\d{1,3}\.){3}\d{1,3}|[a-zA-Z0-9][a-zA-Z0-9\.\-]*[a-zA-Z0-9])(?::(?<port>\d{1,5}))?/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SixDigits = new(@"^\d{6}$", RegexOptions.Compiled);

    /// <summary>
    /// 支持：
    /// - http://host:port
    /// - http://host:port#123456（PC 新二维码：地址+配对码）
    /// - GuiPiao|http://host:port|123456
    /// - host:port
    /// </summary>
    public static (string? Url, string? PairCode) ParseSyncQr(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, null);
        var t = text.Trim().Trim('\0', '\uFEFF', '"', '\'');

        if (t.StartsWith("GuiPiao|", StringComparison.OrdinalIgnoreCase))
        {
            var parts = t.Split('|');
            var urlPart = parts.Length >= 2 ? ExtractHttpUrl(parts[1]) : null;
            var codePart = parts.Length >= 3 && SixDigits.IsMatch(parts[2].Trim()) ? parts[2].Trim() : null;
            return (urlPart, codePart);
        }

        string? pairCode = null;
        var hash = t.LastIndexOf('#');
        if (hash >= 0 && hash < t.Length - 1)
        {
            var frag = t[(hash + 1)..].Trim();
            // 允许 #123456 或 #pair=123456
            if (frag.StartsWith("pair=", StringComparison.OrdinalIgnoreCase))
                frag = frag[5..].Trim();
            if (SixDigits.IsMatch(frag))
            {
                pairCode = frag;
                t = t[..hash].Trim();
            }
        }

        var q = t.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0 && pairCode == null)
        {
            var query = t[(q + 1)..];
            foreach (var part in query.Split('&'))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 &&
                    (kv[0].Equals("c", StringComparison.OrdinalIgnoreCase) ||
                     kv[0].Equals("pair", StringComparison.OrdinalIgnoreCase) ||
                     kv[0].Equals("code", StringComparison.OrdinalIgnoreCase)) &&
                    SixDigits.IsMatch(kv[1].Trim()))
                {
                    pairCode = kv[1].Trim();
                    t = t[..q].Trim();
                    break;
                }
            }
        }

        return (ExtractHttpUrl(t), pairCode);
    }

    public static string? ExtractHttpUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.Trim().Trim('\0', '\uFEFF', '"', '\'');

        // 若仍带 #code，先剥掉再解析地址
        var hash = t.LastIndexOf('#');
        if (hash >= 0)
            t = t[..hash].Trim();

        if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return NormalizeUrl(t);

        var m = HttpUrl.Match(t);
        if (m.Success)
            return NormalizeUrl(m.Value);

        var hp = HostPort.Match(t);
        if (hp.Success)
        {
            var host = hp.Groups["host"].Value;
            var port = hp.Groups["port"].Success ? hp.Groups["port"].Value : "17880";
            return NormalizeUrl($"http://{host}:{port}");
        }

        return null;
    }

    private static string NormalizeUrl(string url)
    {
        url = url.Trim().TrimEnd('/', ' ', '\r', '\n', '\t');
        var hash = url.IndexOf('#');
        if (hash >= 0) url = url[..hash];
        var q = url.IndexOf('?');
        if (q >= 0) url = url[..q];

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.TrimEnd('/');
        return $"{uri.Scheme}://{uri.Authority}";
    }

    public static string? TryDecodeQr(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0) return null;
        try
        {
            return QrImageDecoder.Decode(imageBytes);
        }
        catch
        {
            return null;
        }
    }
}
