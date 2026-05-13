using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GuiPiao.ViewModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SkiaSharp;

namespace GuiPiao.Services;

public class ChartExportService : IDisposable
{
    private readonly LogService _logService;
    private readonly ChartRenderService _renderService;
    private bool _disposed;

    public ChartExportService()
    {
        _logService = ServiceManager.Instance.LogService;
        _renderService = new ChartRenderService();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _renderService.Dispose();
    }

    public async Task<ExportResult> ExportAsync(
        string filePath,
        ExportChartFormat format,
        List<DashboardChartViewModel> charts,
        bool includeRawData,
        bool includeChartImage,
        bool useDefaultChartNames = true,
        string? folderName = null,
        string? baseFileName = null)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChartExportService));

        try
        {
            _logService.Info("ChartExportService", $"开始导出图表: 格式={format}, 图表数量={charts.Count}, 路径={filePath}");

            if (format != ExportChartFormat.Image)
                if (!CheckFileWriteAccess(filePath))
                    return new ExportResult { Success = false, Message = $"文件已被占用，请关闭后重试：{filePath}" };

            var result = format switch
            {
                ExportChartFormat.Excel => await ExportToExcelAsync(filePath, charts, includeRawData),
                ExportChartFormat.Pdf => await ExportToPdfAsync(filePath, charts, includeRawData, includeChartImage),
                ExportChartFormat.Image => await ExportToImageAsync(filePath, charts, includeChartImage,
                    useDefaultChartNames, folderName, baseFileName),
                _ => new ExportResult { Success = false, Message = "不支持的导出格式" }
            };

            if (result.Success)
                _logService.Info("ChartExportService", $"图表导出成功: {filePath}");
            else
                _logService.Error("ChartExportService", $"图表导出失败: {result.Message}");

            return result;
        }
        catch (Exception ex)
        {
            _logService.Error("ChartExportService", $"图表导出异常: {ex.Message}");
            return new ExportResult { Success = false, Message = $"导出失败: {ex.Message}" };
        }
    }

    private bool CheckFileWriteAccess(string filePath)
    {
        if (!File.Exists(filePath)) return true;

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private async Task<ExportResult> ExportToExcelAsync(string filePath, List<DashboardChartViewModel> charts,
        bool includeRawData)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var workbook = new XSSFWorkbook();

                var summarySheet = workbook.CreateSheet("统计概览");
                CreateSummarySheet(summarySheet, charts);

                foreach (var chart in charts)
                {
                    var sheetName = SanitizeSheetName(chart.Title ?? chart.Card?.Name ?? "未命名图表");
                    var sheet = workbook.CreateSheet(sheetName);

                    CreateChartSheet(sheet, chart, includeRawData);
                }

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                workbook.Write(fs);

                return new ExportResult { Success = true, FilePath = filePath, RecordCount = charts.Count };
            }
            catch (Exception ex)
            {
                _logService.Error("ChartExportService", $"Excel导出异常: {ex.Message}");
                return new ExportResult { Success = false, Message = $"Excel导出失败: {ex.Message}" };
            }
        });
    }

    private void CreateSummarySheet(ISheet sheet, List<DashboardChartViewModel> charts)
    {
        var rowIndex = 0;

        var titleRow = sheet.CreateRow(rowIndex++);
        titleRow.CreateCell(0).SetCellValue("统计报告概览");
        titleRow.CreateCell(1).SetCellValue($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        rowIndex++;

        var headerStyle = sheet.Workbook.CreateCellStyle();
        var headerFont = sheet.Workbook.CreateFont();
        headerFont.IsBold = true;
        headerStyle.SetFont(headerFont);

        var headerRow = sheet.CreateRow(rowIndex++);
        headerRow.CreateCell(0).SetCellValue("序号");
        headerRow.CreateCell(1).SetCellValue("图表名称");
        headerRow.CreateCell(2).SetCellValue("图表类型");
        headerRow.CreateCell(3).SetCellValue("统计指标");
        headerRow.CreateCell(4).SetCellValue("数据条数");

        for (var i = 0; i < 5; i++) headerRow.GetCell(i).CellStyle = headerStyle;

        var chartTypeMap = new Dictionary<string, string>
        {
            { "PieChart", "饼图" },
            { "BarChart", "柱状图" },
            { "HorizontalBarChart", "条形图" },
            { "LineChart", "折线图" },
            { "TextList", "文本列表" }
        };

        for (var i = 0; i < charts.Count; i++)
        {
            var chart = charts[i];
            var chartRow = sheet.CreateRow(rowIndex++);
            chartRow.CreateCell(0).SetCellValue(i + 1);
            chartRow.CreateCell(1).SetCellValue(chart.Title ?? chart.Card?.Name ?? "未命名图表");

            var chartTypeName = "未知";
            if (chart.IsPieChart)
            {
                chartTypeName = "饼图";
            }
            else if (chart.IsTextList)
            {
                chartTypeName = "文本列表";
            }
            else
            {
                var chartType = chart.Card?.ChartType.ToString() ?? "";
                chartTypeMap.TryGetValue(chartType, out chartTypeName);
            }

            chartRow.CreateCell(2).SetCellValue(chartTypeName);
            chartRow.CreateCell(3).SetCellValue(chart.ChartData?.SeriesName ?? "");
            chartRow.CreateCell(4).SetCellValue(chart.ChartData?.Values?.Length ?? 0);
        }

        sheet.SetColumnWidth(0, 2000);
        sheet.SetColumnWidth(1, 5000);
        sheet.SetColumnWidth(2, 3000);
        sheet.SetColumnWidth(3, 5000);
        sheet.SetColumnWidth(4, 3000);
    }

    private void CreateChartSheet(ISheet sheet, DashboardChartViewModel chart, bool includeRawData)
    {
        var rowIndex = 0;

        var titleRow = sheet.CreateRow(rowIndex++);
        titleRow.CreateCell(0).SetCellValue(chart.Title ?? chart.Card?.Name ?? "未命名图表");

        rowIndex++;

        if (chart.ChartData != null)
        {
            var infoRow = sheet.CreateRow(rowIndex++);
            infoRow.CreateCell(0).SetCellValue("统计指标:");
            infoRow.CreateCell(1).SetCellValue(chart.ChartData.SeriesName ?? "");

            var dataRow = sheet.CreateRow(rowIndex++);
            dataRow.CreateCell(0).SetCellValue("数据条数:");
            dataRow.CreateCell(1).SetCellValue(chart.ChartData.Values?.Length ?? 0);
        }

        rowIndex++;

        if (includeRawData && chart.ChartData != null)
        {
            var headerStyle = sheet.Workbook.CreateCellStyle();
            var headerFont = sheet.Workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);

            var headerRow = sheet.CreateRow(rowIndex++);
            headerRow.CreateCell(0).SetCellValue("标签");
            headerRow.CreateCell(1).SetCellValue("数值");

            headerRow.GetCell(0).CellStyle = headerStyle;
            headerRow.GetCell(1).CellStyle = headerStyle;

            var labels = chart.ChartData.Labels ?? Array.Empty<string>();
            var values = chart.ChartData.Values ?? Array.Empty<double>();
            var length = Math.Min(labels.Length, values.Length);

            for (var i = 0; i < length; i++)
            {
                var dataRow = sheet.CreateRow(rowIndex++);
                dataRow.CreateCell(0).SetCellValue(labels[i]);
                dataRow.CreateCell(1).SetCellValue(values[i]);
            }
        }

        sheet.SetColumnWidth(0, 5000);
        sheet.SetColumnWidth(1, 3000);
    }

    private async Task<ExportResult> ExportToPdfAsync(string filePath, List<DashboardChartViewModel> charts,
        bool includeRawData, bool includeChartImage)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = new PdfDocument();
                document.Info.Title = "统计报告";
                document.Info.Author = "GuiPiao";

                var pageWidth = XUnit.FromMillimeter(210).Point;
                var pageHeight = XUnit.FromMillimeter(297).Point;

                var coverPage = document.AddPage();
                coverPage.Width = pageWidth;
                coverPage.Height = pageHeight;
                var gfx = XGraphics.FromPdfPage(coverPage);
                CreatePdfCoverPage(gfx, pageWidth, pageHeight, charts);

                foreach (var chart in charts)
                {
                    var chartPage = document.AddPage();
                    chartPage.Width = pageWidth;
                    chartPage.Height = pageHeight;
                    var chartGfx = XGraphics.FromPdfPage(chartPage);
                    CreatePdfChartPage(chartGfx, pageWidth, pageHeight, chart, includeRawData, includeChartImage);
                }

                document.Save(filePath);
                return new ExportResult { Success = true, FilePath = filePath, RecordCount = charts.Count };
            }
            catch (Exception ex)
            {
                _logService.Error("ChartExportService", $"PDF导出异常: {ex.Message}");
                return new ExportResult { Success = false, Message = $"PDF导出失败: {ex.Message}" };
            }
        });
    }

    private void CreatePdfCoverPage(XGraphics gfx, double pageWidth, double pageHeight,
        List<DashboardChartViewModel> charts)
    {
        var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);
        var titleFont = new XFont("Microsoft YaHei", 24, XFontStyle.Bold, fontOptions);
        var subtitleFont = new XFont("Microsoft YaHei", 14, XFontStyle.Regular, fontOptions);
        var normalFont = new XFont("Microsoft YaHei", 12, XFontStyle.Regular, fontOptions);

        var centerX = pageWidth / 2;
        var startY = pageHeight * 0.3;

        gfx.DrawString("统计报告", titleFont, XBrushes.Black, new XPoint(centerX, startY), XStringFormats.Center);
        gfx.DrawString($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", subtitleFont, XBrushes.Gray,
            new XPoint(centerX, startY + 40), XStringFormats.Center);
        gfx.DrawString($"共计 {charts.Count} 个图表", subtitleFont, XBrushes.Gray, new XPoint(centerX, startY + 70),
            XStringFormats.Center);

        var listY = startY + 120;
        gfx.DrawString("图表列表:", normalFont, XBrushes.Black, new XPoint(50, listY), XStringFormats.CenterLeft);

        listY += 30;
        for (var i = 0; i < charts.Count; i++)
        {
            var chartName = charts[i].Title ?? charts[i].Card?.Name ?? "未命名图表";
            gfx.DrawString($"{i + 1}. {chartName}", normalFont, XBrushes.DarkGray, new XPoint(70, listY),
                XStringFormats.CenterLeft);
            listY += 25;
        }
    }

    private void CreatePdfChartPage(XGraphics gfx, double pageWidth, double pageHeight, DashboardChartViewModel chart,
        bool includeRawData, bool includeChartImage)
    {
        var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);
        var titleFont = new XFont("Microsoft YaHei", 16, XFontStyle.Bold, fontOptions);
        var headerFont = new XFont("Microsoft YaHei", 11, XFontStyle.Bold, fontOptions);
        var normalFont = new XFont("Microsoft YaHei", 10, XFontStyle.Regular, fontOptions);

        var marginLeft = XUnit.FromMillimeter(20).Point;
        var marginRight = pageWidth - XUnit.FromMillimeter(20).Point;
        var marginTop = XUnit.FromMillimeter(20).Point;
        var marginBottom = pageHeight - XUnit.FromMillimeter(20).Point;

        var y = marginTop;

        gfx.DrawString(chart.Title ?? chart.Card?.Name ?? "未命名图表", titleFont, XBrushes.Black, new XPoint(marginLeft, y),
            XStringFormats.CenterLeft);
        y += 30;

        if (chart.ChartData != null)
        {
            gfx.DrawString($"统计指标: {chart.ChartData.SeriesName}", normalFont, XBrushes.DarkGray,
                new XPoint(marginLeft, y), XStringFormats.CenterLeft);
            y += 20;
        }

        string? tempFile = null;
        XImage? xImage = null;

        try
        {
            if (includeChartImage)
            {
                using var skImage = _renderService.RenderChart(chart);
                using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);

                tempFile = Path.GetTempFileName() + ".png";
                using var fileStream = File.OpenWrite(tempFile);
                skData.SaveTo(fileStream);
                fileStream.Close();

                xImage = XImage.FromFile(tempFile);

                var maxWidth = pageWidth - marginLeft * 2;
                var maxHeight = pageHeight * 0.5;
                var ratio = Math.Min(maxWidth / xImage.PixelWidth, maxHeight / xImage.PixelHeight);
                var imageWidth = xImage.PixelWidth * ratio;
                var imageHeight = xImage.PixelHeight * ratio;

                var imageRect = new XRect(marginLeft, y, imageWidth, imageHeight);
                gfx.DrawImage(xImage, imageRect);

                y += imageHeight + 20;
            }
        }
        catch (Exception ex)
        {
            _logService.Info("ChartExportService", $"渲染图表图片失败: {ex.Message}");
        }
        finally
        {
            xImage?.Dispose();
            if (tempFile != null && File.Exists(tempFile))
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                }
        }

        if (includeRawData && chart.ChartData != null)
        {
            var tableWidth = marginRight - marginLeft;
            var colWidth = tableWidth / 2;

            var xLabels = chart.ChartData.Labels ?? Array.Empty<string>();
            var xValues = chart.ChartData.Values ?? Array.Empty<double>();
            var length = Math.Min(xLabels.Length, xValues.Length);

            var headerRow = new XRect(marginLeft, y, colWidth, 20);
            var valueHeaderRow = new XRect(marginLeft + colWidth, y, colWidth, 20);
            gfx.DrawString("标签", headerFont, XBrushes.Black, headerRow, XStringFormats.Center);
            gfx.DrawString("数值", headerFont, XBrushes.Black, valueHeaderRow, XStringFormats.Center);
            y += 20;

            gfx.DrawLine(XPens.LightGray, marginLeft, y, marginRight, y);
            y += 5;

            for (var i = 0; i < length; i++)
            {
                if (y + 20 > marginBottom) break;

                var labelRect = new XRect(marginLeft + 5, y, colWidth - 10, 20);
                var valueRect = new XRect(marginLeft + colWidth + 5, y, colWidth - 10, 20);

                gfx.DrawString(xLabels[i], normalFont, XBrushes.Black, labelRect, XStringFormats.CenterLeft);
                gfx.DrawString(FormatValueForPdf(xValues[i]), normalFont, XBrushes.Black, valueRect,
                    XStringFormats.CenterRight);

                y += 20;
            }
        }
    }

    private string FormatValueForPdf(double value)
    {
        if (value >= 1000)
            return value.ToString("N0");
        if (value == Math.Floor(value))
            return value.ToString("F0");
        return value.ToString("F2");
    }

    private async Task<ExportResult> ExportToImageAsync(string folderPath, List<DashboardChartViewModel> charts,
        bool includeChartImage, bool useDefaultChartNames, string? folderName, string? baseFileName)
    {
        if (charts.Count == 0) return new ExportResult { Success = false, Message = "没有可导出的图表" };

        return await Task.Run(() =>
        {
            try
            {
                var targetFolder = folderPath;

                // 如果有文件夹名称，创建子文件夹
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    targetFolder = Path.Combine(folderPath, SanitizeFileName(folderName));
                    if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
                }

                for (var i = 0; i < charts.Count; i++)
                {
                    var chart = charts[i];
                    string fileName;

                    if (useDefaultChartNames)
                    {
                        // 使用图表名称作为文件名
                        var chartName = chart.Title ?? chart.Card?.Name ?? $"图表_{i + 1}";
                        fileName = $"{SanitizeFileName(chartName)}.png";
                    }
                    else
                    {
                        // 使用用户输入的基础文件名 + 序号
                        var safeBaseName =
                            SanitizeFileName(string.IsNullOrWhiteSpace(baseFileName) ? "图表" : baseFileName);
                        fileName = $"{safeBaseName}_{i + 1}.png";
                    }

                    var chartFilePath = Path.Combine(targetFolder, fileName);

                    // 检查文件占用
                    if (!CheckFileWriteAccess(chartFilePath))
                        return new ExportResult { Success = false, Message = $"文件已被占用，请关闭后重试：{chartFilePath}" };

                    // 导出单张图表
                    using var skImage = _renderService.RenderChart(chart);
                    using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                    using var stream = File.OpenWrite(chartFilePath);
                    skData.SaveTo(stream);
                }

                return new ExportResult { Success = true, FilePath = targetFolder, RecordCount = charts.Count };
            }
            catch (Exception ex)
            {
                _logService.Error("ChartExportService", $"图片导出异常: {ex.Message}");
                return new ExportResult { Success = false, Message = $"图片导出失败: {ex.Message}" };
            }
        });
    }

    private string SanitizeFileName(string name)
    {
        // 移除文件名中的非法字符
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

        // 如果清理后为空，使用默认名称
        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "未命名图表";

        // 确保文件名长度合理
        if (sanitized.Length > 100) sanitized = sanitized.Substring(0, 97) + "...";

        return sanitized;
    }

    private string SanitizeSheetName(string name)
    {
        var invalidChars = @":\/?*[]";
        foreach (var c in invalidChars) name = name.Replace(c, '_');

        if (name.Length > 31) name = name.Substring(0, 31);

        name = name.Trim('\'');

        if (string.IsNullOrWhiteSpace(name)) name = "Sheet1";

        return name;
    }
}