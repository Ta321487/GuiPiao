using System.Text.RegularExpressions;

namespace GuiPiao.Mobile.Services;

/// <summary>从二维码图或文本中提取 Sync ListenUrl。</summary>
public static class ServerUrlQrHelper
{
    private static readonly Regex HttpUrl = new(
        @"https?://[^\s""'<>]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? ExtractHttpUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.Trim();
        if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return t.TrimEnd('/', ' ', '\r', '\n');

        var m = HttpUrl.Match(t);
        return m.Success ? m.Value.TrimEnd('/', ' ', '\r', '\n') : null;
    }

    /// <summary>
    /// 尝试解码二维码。无 ZXing 绑定时返回 null，由调用方回退剪贴板。
    /// </summary>
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
