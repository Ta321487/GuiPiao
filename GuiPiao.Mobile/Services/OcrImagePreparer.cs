using SkiaSharp;

namespace GuiPiao.Mobile.Services;

/// <summary>OCR 上传前压缩：缩边并转 JPEG，避免整图 Base64 撑爆内存。</summary>
public static class OcrImagePreparer
{
    public const int MaxEdge = 1600;
    public const int JpegQuality = 82;

    public static async Task<(byte[] Bytes, string FileName)> PrepareAsync(
        Stream input,
        string? originalFileName,
        CancellationToken ct = default)
    {
        await using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        var raw = buffer.ToArray();
        if (raw.Length == 0)
            throw new InvalidOperationException("empty_image");

        using var bitmap = SKBitmap.Decode(raw)
            ?? throw new InvalidOperationException("image_decode_failed");

        var maxSide = Math.Max(bitmap.Width, bitmap.Height);
        SKBitmap working = bitmap;
        SKBitmap? scaled = null;
        try
        {
            if (maxSide > MaxEdge)
            {
                var scale = MaxEdge / (double)maxSide;
                var w = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
                var h = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
                scaled = bitmap.Resize(new SKImageInfo(w, h), SKFilterQuality.Medium)
                         ?? throw new InvalidOperationException("image_resize_failed");
                working = scaled;
            }

            using var image = SKImage.FromBitmap(working);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            if (data == null || data.Size == 0)
                throw new InvalidOperationException("image_encode_failed");

            var name = string.IsNullOrWhiteSpace(originalFileName) ? "capture.jpg" : originalFileName!;
            return (data.ToArray(), Path.ChangeExtension(name, ".jpg") ?? "capture.jpg");
        }
        finally
        {
            scaled?.Dispose();
        }
    }
}
