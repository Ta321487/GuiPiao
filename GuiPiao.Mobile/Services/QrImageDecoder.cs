using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace GuiPiao.Mobile.Services;

internal static class QrImageDecoder
{
    public static string? Decode(byte[] imageBytes)
    {
        using var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap == null) return null;

        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                TryHarder = true
            }
        };

        var result = reader.Decode(new SKBitmapLuminanceSource(bitmap));
        return string.IsNullOrWhiteSpace(result?.Text) ? null : result.Text.Trim();
    }
}
