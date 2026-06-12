using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;

namespace GuiPiao.Services;

/// <summary>
///     票面编码区二维码（ECC Q，黑字透明底），与文档一致。
/// </summary>
public static class TicketPreviewQrService
{
    public static BitmapImage? CreateQrBitmap(string? text, int pixelsPerModule = 6)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            using var gen = new QRCodeGenerator();
            var data = gen.CreateQrCode(text.Trim(), QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(pixelsPerModule);

            var img = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                img.BeginInit();
                img.StreamSource = ms;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
            }

            if (img.CanFreeze)
                img.Freeze();
            return img;
        }
        catch
        {
            return null;
        }
    }
}
