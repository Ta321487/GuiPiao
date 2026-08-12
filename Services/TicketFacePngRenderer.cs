using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GuiPiao.Services;

/// <summary>
///     将票面画布渲染为 811×509 PNG（与预览窗「导出 PNG」一致）。
/// </summary>
public static class TicketFacePngRenderer
{
    public const int TicketWidth = 811;
    public const int TicketHeight = 509;

    public static void SavePng(FrameworkElement previewSurface, string filePath)
    {
        previewSurface.Measure(new Size(TicketWidth, TicketHeight));
        previewSurface.Arrange(new Rect(0, 0, TicketWidth, TicketHeight));
        previewSurface.UpdateLayout();

        var rt = new RenderTargetBitmap(TicketWidth, TicketHeight, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, TicketWidth, TicketHeight));
            var vb = new VisualBrush(previewSurface)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
            dc.DrawRectangle(vb, null, new Rect(0, 0, TicketWidth, TicketHeight));
        }

        rt.Render(dv);

        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rt));
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        using var fs = File.Create(filePath);
        enc.Save(fs);
    }
}
