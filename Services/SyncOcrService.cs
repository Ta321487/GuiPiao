using System;
using System.IO;
using System.Threading.Tasks;
using GuiPiao.Model.Sync;
using GuiPiao.Models;
using GuiPiao.ViewModel;

namespace GuiPiao.Services;

/// <summary>手机经 HTTP 调用本机 CnOCR（不把引擎塞进手机）。</summary>
public class SyncOcrService
{
    private readonly OcrRecognitionService _ocr = new();

    public async Task<SyncOcrResponse> RecognizeAsync(SyncOcrRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ImageBase64))
            throw new InvalidOperationException("image_base64_required");

        byte[] bytes;
        try
        {
            var raw = request.ImageBase64.Trim();
            var comma = raw.IndexOf(',');
            if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                raw = raw[(comma + 1)..];
            bytes = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("invalid_base64");
        }

        if (bytes.Length == 0)
            throw new InvalidOperationException("empty_image");
        if (bytes.Length > 12 * 1024 * 1024)
            throw new InvalidOperationException("image_too_large");

        var ext = GuessExtension(request.FileName);
        var path = Path.Combine(Path.GetTempPath(), "guipiao-ocr-" + Guid.NewGuid().ToString("N") + ext);
        try
        {
            await File.WriteAllBytesAsync(path, bytes);
            var results = await _ocr.RecognizeAsync(path);
            var text = OcrRecognizeTicketViewModel.JoinOcrTexts(results);
            return new SyncOcrResponse
            {
                Text = text ?? string.Empty,
                SourceHint = "图片OCR"
            };
        }
        finally
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // ignore temp cleanup
            }
        }
    }

    private static string GuessExtension(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? "");
        if (string.IsNullOrWhiteSpace(ext)) return ".jpg";
        return ext.Length <= 8 ? ext : ".jpg";
    }
}
