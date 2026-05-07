using GuiPiao.DataAccess;
using GuiPiao.Model;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuiPiao.Services
{
    /// <summary>
    /// 导出服务 - 支持Excel、CSV、PDF、图片格式导出
    /// </summary>
    public class ExportService
    {
        private readonly Lazy<TrainRideRepository> _trainRideRepository;
        private readonly Lazy<ExportSettingsService> _exportSettingsService;
        private readonly LogService _logService;

        public ExportService()
        {
            _trainRideRepository = new Lazy<TrainRideRepository>(() => new TrainRideRepository());
            _exportSettingsService = new Lazy<ExportSettingsService>(() => new ExportSettingsService());
            _logService = new LogService();
        }

        private TrainRideRepository TrainRideRepository => _trainRideRepository.Value;
        private ExportSettingsService ExportSettingsService => _exportSettingsService.Value;

        /// <summary>
        /// 导出数据
        /// </summary>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="format">导出格式</param>
        /// <param name="trainRides">要导出的数据</param>
        /// <returns>导出结果</returns>
        public async Task<ExportResult> ExportAsync(string filePath, ExportFormatOption format, List<TrainRideInfo> trainRides)
        {
            var config = ExportSettingsService.Config;
            return await ExportAsync(filePath, format, trainRides, config);
        }

        /// <summary>
        /// 导出数据（带指定配置）
        /// </summary>
        public async Task<ExportResult> ExportAsync(string filePath, ExportFormatOption format, List<TrainRideInfo> trainRides, ExportConfig config)
        {

            try
            {
                ExportResult result = format switch
                {
                    ExportFormatOption.Excel => await ExportToExcelAsync(filePath, trainRides, config),
                    ExportFormatOption.Csv => await ExportToCsvAsync(filePath, trainRides, config),
                    ExportFormatOption.Pdf => await ExportToPdfAsync(filePath, trainRides, config),
                    ExportFormatOption.Image => await ExportToImageAsync(filePath, trainRides, config),
                    _ => new ExportResult { Success = false, Message = "不支持的导出格式" }
                };

                if (result.Success)
                {
                    _logService.Info("ExportService", $"导出成功: {filePath}, 格式: {format}, 记录数: {trainRides.Count}");
                }
                else
                {
                    _logService.Error("ExportService", $"导出失败: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("ExportService", $"导出异常: {ex.Message}");
                return new ExportResult { Success = false, Message = $"导出失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 导出所有数据
        /// </summary>
        public async Task<ExportResult> ExportAllAsync(string filePath, ExportFormatOption format)
        {
            var allRides = await TrainRideRepository.GetAllTrainRidesAsync();
            return await ExportAsync(filePath, format, allRides.ToList());
        }

        /// <summary>
        /// 获取所有行程数据（用于分组导出）
        /// </summary>
        public async Task<List<TrainRideInfo>> GetAllTrainRidesAsync()
        {
            var allRides = await TrainRideRepository.GetAllTrainRidesAsync();
            return allRides.ToList();
        }

        /// <summary>
        /// 按分组导出数据
        /// </summary>
        /// <param name="filePath">导出文件路径</param>
        /// <param name="format">导出格式</param>
        /// <param name="trainRides">要导出的数据</param>
        /// <param name="groupOption">分组选项</param>
        /// <returns>导出结果</returns>
        public async Task<ExportResult> ExportGroupedAsync(string filePath, ExportFormatOption format, List<TrainRideInfo> trainRides, GroupOption groupOption)
        {
            var config = ExportSettingsService.Config;
            return await ExportGroupedAsync(filePath, format, trainRides, groupOption, config);
        }

        /// <summary>
        /// 按分组导出数据（带指定配置）
        /// </summary>
        public async Task<ExportResult> ExportGroupedAsync(string filePath, ExportFormatOption format, List<TrainRideInfo> trainRides, GroupOption groupOption, ExportConfig config)
        {

            try
            {
                ExportResult result = format switch
                {
                    ExportFormatOption.Excel => await ExportToExcelGroupedAsync(filePath, trainRides, config, groupOption),
                    ExportFormatOption.Csv => await ExportToCsvGroupedAsync(filePath, trainRides, config, groupOption),
                    ExportFormatOption.Pdf => await ExportToPdfGroupedAsync(filePath, trainRides, config, groupOption),
                    ExportFormatOption.Image => await ExportToImageAsync(filePath, trainRides, config),
                    _ => new ExportResult { Success = false, Message = "不支持的导出格式" }
                };

                if (result.Success)
                {
                    _logService.Info("ExportService", $"分组导出成功: {filePath}, 格式: {format}, 分组: {groupOption}, 记录数: {trainRides.Count}");
                }
                else
                {
                    _logService.Error("ExportService", $"分组导出失败: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logService.Error("ExportService", $"分组导出异常: {ex.Message}");
                return new ExportResult { Success = false, Message = $"导出失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 导出Excel
        /// </summary>
        private async Task<ExportResult> ExportToExcelAsync(string filePath, List<TrainRideInfo> trainRides, ExportConfig config)
        {
            // 检查文件是否被占用
            if (File.Exists(filePath))
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                }
                catch (IOException)
                {
                    return new ExportResult { Success = false, Message = $"文件已被占用，请关闭后重试：{filePath}" };
                }
            }

            return await Task.Run(() =>
            {
                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet(config.ExcelSheetName);

                // 创建单元格样式
                ICellStyle headerStyle = workbook.CreateCellStyle();
                IFont headerFont = workbook.CreateFont();
                headerFont.IsBold = true;
                headerStyle.SetFont(headerFont);

                int rowIndex = 0;

                // 写入表头
                if (config.ExcelIncludeHeader)
                {
                    IRow headerRow = sheet.CreateRow(rowIndex++);
                    int colIndex = 0;

                    if (config.ExportTicketNumber) headerRow.CreateCell(colIndex++).SetCellValue("票号");
                    if (config.ExportTrainNo) headerRow.CreateCell(colIndex++).SetCellValue("车次");
                    if (config.ExportDepartStation) headerRow.CreateCell(colIndex++).SetCellValue("出发站");
                    if (config.ExportArriveStation) headerRow.CreateCell(colIndex++).SetCellValue("到达站");
                    if (config.ExportDepartDate) headerRow.CreateCell(colIndex++).SetCellValue("出发日期");
                    if (config.ExportDepartTime) headerRow.CreateCell(colIndex++).SetCellValue("出发时间");
                    if (config.ExportCoachNo) headerRow.CreateCell(colIndex++).SetCellValue("车厢号");
                    if (config.ExportSeatNo) headerRow.CreateCell(colIndex++).SetCellValue("座位号");
                    if (config.ExportSeatType) headerRow.CreateCell(colIndex++).SetCellValue("席别");
                    if (config.ExportMoney) headerRow.CreateCell(colIndex++).SetCellValue("票价");
                    if (config.ExportCheckInLocation) headerRow.CreateCell(colIndex++).SetCellValue("检票口");
                    if (config.ExportTags) headerRow.CreateCell(colIndex++).SetCellValue("标签");
                    if (config.ExportAdditionalInfo) headerRow.CreateCell(colIndex++).SetCellValue("备注");

                    // 应用表头样式
                    for (int i = 0; i < colIndex; i++)
                    {
                        headerRow.GetCell(i).CellStyle = headerStyle;
                    }
                }

                // 写入数据
                foreach (var ride in trainRides)
                {
                    IRow row = sheet.CreateRow(rowIndex++);
                    int colIndex = 0;

                    if (config.ExportTicketNumber) row.CreateCell(colIndex++).SetCellValue(ride.TicketNumber);
                    if (config.ExportTrainNo) row.CreateCell(colIndex++).SetCellValue(ride.TrainNo);
                    if (config.ExportDepartStation) row.CreateCell(colIndex++).SetCellValue(ride.DepartStation);
                    if (config.ExportArriveStation) row.CreateCell(colIndex++).SetCellValue(ride.ArriveStation);
                    if (config.ExportDepartDate) row.CreateCell(colIndex++).SetCellValue(FormatDate(ride.DepartDate, config.ExcelDateFormat));
                    if (config.ExportDepartTime) row.CreateCell(colIndex++).SetCellValue(ride.DepartTime);
                    if (config.ExportCoachNo) row.CreateCell(colIndex++).SetCellValue(ride.CoachNo);
                    if (config.ExportSeatNo) row.CreateCell(colIndex++).SetCellValue(ride.SeatNo);
                    if (config.ExportSeatType) row.CreateCell(colIndex++).SetCellValue(ride.SeatType);
                    if (config.ExportMoney) row.CreateCell(colIndex++).SetCellValue(FormatMoney(ride.Money, config.ExcelMoneyFormat));
                    if (config.ExportCheckInLocation) row.CreateCell(colIndex++).SetCellValue(ride.CheckInLocation);
                    if (config.ExportTags) row.CreateCell(colIndex++).SetCellValue(""); // 标签字段待实现
                    if (config.ExportAdditionalInfo) row.CreateCell(colIndex++).SetCellValue(ride.AdditionalInfo);
                }

                // 自动调整列宽
                if (config.ExcelAutoFitColumns)
                {
                    int colCount = sheet.GetRow(0)?.LastCellNum ?? 0;
                    for (int i = 0; i < colCount; i++)
                    {
                        // 先自动调整
                        sheet.AutoSizeColumn(i);
                        
                        // 获取当前宽度（单位是1/256个字符宽度）
                        int currentWidth = sheet.GetColumnWidth(i);
                        
                        // 为中文字符增加额外宽度（至少3000，即约12个字符宽度）
                        int newWidth = Math.Max(currentWidth + 1000, 3000);
                        sheet.SetColumnWidth(i, newWidth);
                    }
                }

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }

                return new ExportResult { Success = true, FilePath = filePath, RecordCount = trainRides.Count };
            });
        }

        /// <summary>
        /// 导出CSV
        /// </summary>
        private async Task<ExportResult> ExportToCsvAsync(string filePath, List<TrainRideInfo> trainRides, ExportConfig config)
        {
            // 检查文件是否被占用
            if (File.Exists(filePath))
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                }
                catch (IOException)
                {
                    return new ExportResult { Success = false, Message = $"文件已被占用，请关闭后重试：{filePath}" };
                }
            }

            return await Task.Run(() =>
            {
                Encoding encoding = config.CsvEncoding switch
                {
                    CsvEncodingOption.UTF8 => new UTF8Encoding(true),
                    CsvEncodingOption.GBK => Encoding.GetEncoding("GBK"),
                    CsvEncodingOption.Unicode => Encoding.Unicode,
                    _ => Encoding.UTF8
                };

                string delimiter = config.CsvDelimiter switch
                {
                    CsvDelimiterOption.Comma => ",",
                    CsvDelimiterOption.Semicolon => ";",
                    CsvDelimiterOption.Tab => "\t",
                    _ => ","
                };

                using (var writer = new StreamWriter(filePath, false, encoding))
                {
                    // 写入表头
                    if (config.CsvIncludeHeader)
                    {
                        var headers = new List<string>();
                        if (config.ExportTicketNumber) headers.Add("票号");
                        if (config.ExportTrainNo) headers.Add("车次");
                        if (config.ExportDepartStation) headers.Add("出发站");
                        if (config.ExportArriveStation) headers.Add("到达站");
                        if (config.ExportDepartDate) headers.Add("出发日期");
                        if (config.ExportDepartTime) headers.Add("出发时间");
                        if (config.ExportCoachNo) headers.Add("车厢号");
                        if (config.ExportSeatNo) headers.Add("座位号");
                        if (config.ExportSeatType) headers.Add("席别");
                        if (config.ExportMoney) headers.Add("票价");
                        if (config.ExportCheckInLocation) headers.Add("检票口");
                        if (config.ExportTags) headers.Add("标签");
                        if (config.ExportAdditionalInfo) headers.Add("备注");

                        writer.WriteLine(string.Join(delimiter, headers.Select(h => CsvEscape(h, config.CsvUseTextQualifier))));
                    }

                    // 写入数据
                    foreach (var ride in trainRides)
                    {
                        var values = new List<string>();
                        if (config.ExportTicketNumber) values.Add(ride.TicketNumber);
                        if (config.ExportTrainNo) values.Add(ride.TrainNo);
                        if (config.ExportDepartStation) values.Add(ride.DepartStation);
                        if (config.ExportArriveStation) values.Add(ride.ArriveStation);
                        if (config.ExportDepartDate) values.Add(FormatDate(ride.DepartDate, config.CsvDateFormat));
                        if (config.ExportDepartTime) values.Add(ride.DepartTime);
                        if (config.ExportCoachNo) values.Add(ride.CoachNo);
                        if (config.ExportSeatNo) values.Add(ride.SeatNo);
                        if (config.ExportSeatType) values.Add(ride.SeatType);
                        if (config.ExportMoney) values.Add(FormatMoney(ride.Money, config.CsvMoneyFormat));
                        if (config.ExportCheckInLocation) values.Add(ride.CheckInLocation);
                        if (config.ExportTags) values.Add("");
                        if (config.ExportAdditionalInfo) values.Add(ride.AdditionalInfo ?? "");

                        writer.WriteLine(string.Join(delimiter, values.Select(v => CsvEscape(v, config.CsvUseTextQualifier))));
                    }
                }

                return new ExportResult { Success = true, FilePath = filePath, RecordCount = trainRides.Count };
            });
        }

        /// <summary>
        /// 导出PDF
        /// </summary>
        private async Task<ExportResult> ExportToPdfAsync(string filePath, List<TrainRideInfo> trainRides, ExportConfig config)
        {
            // 检查文件是否被占用
            if (File.Exists(filePath))
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                }
                catch (IOException)
                {
                    return new ExportResult { Success = false, Message = $"文件已被占用，请关闭后重试：{filePath}" };
                }
            }

            return await Task.Run(() =>
            {
                var document = new PdfDocument();
                document.Info.Title = "行程记录导出";
                document.Info.Author = "GuiPiao";

                // 计算页面尺寸
                XSize pageSize = config.PdfPaperSize switch
                {
                    "A5" => new XSize(XUnit.FromMillimeter(148).Point, XUnit.FromMillimeter(210).Point),
                    "Letter" => new XSize(XUnit.FromMillimeter(216).Point, XUnit.FromMillimeter(279).Point),
                    _ => new XSize(XUnit.FromMillimeter(210).Point, XUnit.FromMillimeter(297).Point) // A4
                };

                int rowsPerPage = config.PdfRowsPerPage;
                int totalPages = (int)Math.Ceiling((double)trainRides.Count / rowsPerPage);
                if (totalPages == 0) totalPages = 1;

                _logService?.Info("ExportService", $"PDF导出：共 {trainRides.Count} 条记录，每页 {rowsPerPage} 行，预计 {totalPages} 页");

                for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
                {
                    _logService?.Info("ExportService", $"正在生成第 {pageIndex + 1}/{totalPages} 页");
                    var page = document.AddPage();
                    if (config.PdfLandscape)
                    {
                        page.Width = pageSize.Height;
                        page.Height = pageSize.Width;
                    }
                    else
                    {
                        page.Width = pageSize.Width;
                        page.Height = pageSize.Height;
                    }

                    var gfx = XGraphics.FromPdfPage(page);

                    // 使用支持中文的字体
                    var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);
                    var font = new XFont("Microsoft YaHei", config.PdfFontSize, XFontStyle.Regular, fontOptions);
                    var headerFont = new XFont("Microsoft YaHei", config.PdfFontSize + 2, XFontStyle.Bold, fontOptions);

                    double marginLeft = XUnit.FromMillimeter(config.PdfMarginLeft).Point;
                    double marginTop = XUnit.FromMillimeter(config.PdfMarginTop).Point;
                    double marginRight = page.Width.Point - XUnit.FromMillimeter(config.PdfMarginRight).Point;
                    double marginBottom = page.Height.Point - XUnit.FromMillimeter(config.PdfMarginBottom).Point;

                    double y = marginTop;
                    double lineHeight = config.PdfFontSize * 1.5;

                    // 获取当前页的数据
                    var pageData = trainRides.Skip(pageIndex * rowsPerPage).Take(rowsPerPage).ToList();
                    _logService?.Info("ExportService", $"第 {pageIndex + 1} 页包含 {pageData.Count} 条数据");

                    // 计算列宽
                    var columns = GetExportColumns(config);
                    double tableWidth = marginRight - marginLeft;
                    double colWidth = tableWidth / columns.Count;

                    // 绘制表头
                    if (config.ExcelIncludeHeader || pageIndex == 0)
                    {
                        double x = marginLeft;
                        foreach (var col in columns)
                        {
                            gfx.DrawString(col, headerFont, XBrushes.Black, new XRect(x, y, colWidth, lineHeight), XStringFormats.CenterLeft);
                            x += colWidth;
                        }
                        y += lineHeight;
                        gfx.DrawLine(XPens.Black, marginLeft, y, marginRight, y);
                        y += 5;
                    }

                    // 绘制数据
                    foreach (var ride in pageData)
                    {
                        if (y + lineHeight > marginBottom)
                            break;

                        double x = marginLeft;
                        var values = GetExportValues(ride, config);
                        for (int i = 0; i < values.Count && i < columns.Count; i++)
                        {
                            gfx.DrawString(values[i], font, XBrushes.Black, new XRect(x, y, colWidth, lineHeight), XStringFormats.CenterLeft);
                            x += colWidth;
                        }
                        y += lineHeight;
                    }

                    // 页脚页码
                    if (totalPages > 1)
                    {
                        var pageNumText = $"第 {pageIndex + 1} 页 / 共 {totalPages} 页";
                        gfx.DrawString(pageNumText, font, XBrushes.Gray, new XRect(marginLeft, marginBottom - lineHeight, tableWidth, lineHeight), XStringFormats.Center);
                    }
                }

                document.Save(filePath);
                return new ExportResult { Success = true, FilePath = filePath, RecordCount = trainRides.Count };
            });
        }

        /// <summary>
        /// 导出图片（将PDF第一页转为图片）
        /// </summary>
        private async Task<ExportResult> ExportToImageAsync(string filePath, List<TrainRideInfo> trainRides, ExportConfig config)
        {
            // 图片导出暂时使用PDF导出作为基础，后续可以添加图片渲染逻辑
            // 这里先创建一个简单的文本图片表示
            return await Task.Run(() =>
            {
                // 暂时返回提示信息，图片导出需要额外的图像处理库
                return new ExportResult { Success = false, Message = "图片导出功能需要使用GDI+或SkiaSharp进行渲染，建议先使用PDF格式" };
            });
        }

        /// <summary>
        /// 生成文件名
        /// </summary>
        /// <param name="template">文件名模板</param>
        /// <param name="format">导出格式</param>
        /// <param name="trainNo">车次号</param>
        public string GenerateFileName(string template, ExportFormatOption format, string trainNo)
        {
            string extension = format switch
            {
                ExportFormatOption.Excel => ".xlsx",
                ExportFormatOption.Csv => ".csv",
                ExportFormatOption.Pdf => ".pdf",
                ExportFormatOption.Image => ".png",
                _ => ".txt"
            };

            string fileName = template
                .Replace("{日期}", DateTime.Now.ToString("yyyyMMdd"))
                .Replace("{时间}", DateTime.Now.ToString("HHmmss"))
                .Replace("{车次}", trainNo);

            return fileName + extension;
        }

        /// <summary>
        /// 获取导出列名
        /// </summary>
        private List<string> GetExportColumns(ExportConfig config)
        {
            var columns = new List<string>();
            if (config.ExportTicketNumber) columns.Add("票号");
            if (config.ExportTrainNo) columns.Add("车次");
            if (config.ExportDepartStation) columns.Add("出发站");
            if (config.ExportArriveStation) columns.Add("到达站");
            if (config.ExportDepartDate) columns.Add("出发日期");
            if (config.ExportDepartTime) columns.Add("出发时间");
            if (config.ExportCoachNo) columns.Add("车厢号");
            if (config.ExportSeatNo) columns.Add("座位号");
            if (config.ExportSeatType) columns.Add("席别");
            if (config.ExportMoney) columns.Add("票价");
            if (config.ExportCheckInLocation) columns.Add("检票口");
            if (config.ExportTags) columns.Add("标签");
            if (config.ExportAdditionalInfo) columns.Add("备注");
            return columns;
        }

        /// <summary>
        /// 获取导出值
        /// </summary>
        private List<string> GetExportValues(TrainRideInfo ride, ExportConfig config)
        {
            var values = new List<string>();
            if (config.ExportTicketNumber) values.Add(ride.TicketNumber);
            if (config.ExportTrainNo) values.Add(ride.TrainNo);
            if (config.ExportDepartStation) values.Add(ride.DepartStation);
            if (config.ExportArriveStation) values.Add(ride.ArriveStation);
            if (config.ExportDepartDate) values.Add(FormatDate(ride.DepartDate, DateFormatOption.yyyyMMdd));
            if (config.ExportDepartTime) values.Add(ride.DepartTime);
            if (config.ExportCoachNo) values.Add(ride.CoachNo);
            if (config.ExportSeatNo) values.Add(ride.SeatNo);
            if (config.ExportSeatType) values.Add(ride.SeatType);
            if (config.ExportMoney) values.Add(FormatMoney(ride.Money, MoneyFormatOption.Yuan));
            if (config.ExportCheckInLocation) values.Add(ride.CheckInLocation);
            if (config.ExportTags) values.Add("");
            if (config.ExportAdditionalInfo) values.Add(ride.AdditionalInfo ?? "");
            return values;
        }

        // 支持多种输入日期格式
        private static readonly string[] InputDateFormats = new[]
        {
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "dd/MM/yyyy",
            "dd-MM-yyyy",
            "MM/dd/yyyy",
            "yyyy-M-d",
            "yyyy/M/d",
            "d/M/yyyy",
            "d-M-yyyy"
        };

        /// <summary>
        /// 解析日期字符串为 DateTime
        /// </summary>
        private DateTime? ParseDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return null;

            // 尝试使用特定格式解析
            if (DateTime.TryParseExact(dateStr, InputDateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                return date;
            }

            // 尝试通用解析
            if (DateTime.TryParse(dateStr, out date))
            {
                return date;
            }

            return null;
        }

        /// <summary>
        /// 格式化日期
        /// </summary>
        private string FormatDate(string dateStr, DateFormatOption format)
        {
            var date = ParseDate(dateStr);
            if (!date.HasValue) return dateStr;

            return format switch
            {
                DateFormatOption.yyyyMMdd => date.Value.ToString("yyyy-MM-dd"),
                DateFormatOption.yyyyMMddSlash => date.Value.ToString("yyyy/MM/dd"),
                DateFormatOption.yyyyMMddChinese => date.Value.ToString("yyyy年MM月dd日"),
                DateFormatOption.MMddyyyy => date.Value.ToString("MM/dd/yyyy"),
                _ => date.Value.ToString("yyyy-MM-dd")
            };
        }

        /// <summary>
        /// 格式化金额
        /// </summary>
        private string FormatMoney(decimal money, MoneyFormatOption format)
        {
            return format switch
            {
                MoneyFormatOption.Yuan => money.ToString("F2"),
                MoneyFormatOption.YuanWithSymbol => "¥" + money.ToString("F2"),
                MoneyFormatOption.Fen => (money * 100).ToString("F0"),
                _ => money.ToString("F2")
            };
        }

        /// <summary>
        /// CSV转义
        /// </summary>
        private string CsvEscape(string value, bool useQualifier)
        {
            if (string.IsNullOrEmpty(value)) return "";

            bool needsQualifier = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");

            if (useQualifier || needsQualifier)
            {
                value = value.Replace("\"", "\"\"");
                return "\"" + value + "\"";
            }

            return value;
        }

        /// <summary>
        /// 获取分组键值
        /// </summary>
        private string GetGroupKey(TrainRideInfo ride, GroupOption groupOption, ExportConfig config)
        {
            return groupOption switch
            {
                GroupOption.DateDay => FormatDateForGroupKey(ride.DepartDate, config),
                GroupOption.DateMonth => GetMonthKey(ride.DepartDate, config),
                GroupOption.TrainNo => ride.TrainNo ?? "未知车次",
                GroupOption.Departure => ride.DepartStation ?? "未知出发站",
                GroupOption.Arrival => ride.ArriveStation ?? "未知到达站",
                _ => "全部"
            };
        }

        /// <summary>
        /// 获取分组键值（使用默认配置）
        /// </summary>
        private string GetGroupKey(TrainRideInfo ride, GroupOption groupOption)
        {
            return GetGroupKey(ride, groupOption, ExportSettingsService.Config);
        }

        /// <summary>
        /// 格式化日期用于分组键（Sheet名称）- 使用用户设置的格式，但替换非法字符
        /// </summary>
        private string FormatDateForGroupKey(string? dateStr, ExportConfig config)
        {
            if (string.IsNullOrEmpty(dateStr)) return "未知日期";

            var date = ParseDate(dateStr);
            if (!date.HasValue) return dateStr;

            // 根据用户设置的日期格式生成字符串
            string format = config.ExcelDateFormat switch
            {
                DateFormatOption.yyyyMMdd => "yyyy-MM-dd",
                DateFormatOption.yyyyMMddSlash => "yyyy-MM-dd", // 用-代替/
                DateFormatOption.yyyyMMddChinese => "yyyy年MM月dd日",
                DateFormatOption.MMddyyyy => "MM-dd-yyyy", // 用-代替/
                _ => "yyyy-MM-dd"
            };

            return date.Value.ToString(format);
        }

        /// <summary>
        /// 获取月份键值
        /// </summary>
        private string GetMonthKey(string? dateStr, ExportConfig config)
        {
            if (string.IsNullOrEmpty(dateStr)) return "未知月份";

            var date = ParseDate(dateStr);
            if (!date.HasValue) return dateStr;

            // 根据用户设置的日期格式生成月份字符串
            string format = config.ExcelDateFormat switch
            {
                DateFormatOption.yyyyMMdd => "yyyy-MM",
                DateFormatOption.yyyyMMddSlash => "yyyy-MM", // 用-代替/
                DateFormatOption.yyyyMMddChinese => "yyyy年MM月",
                DateFormatOption.MMddyyyy => "yyyy-MM", // 用-代替/
                _ => "yyyy-MM"
            };

            return date.Value.ToString(format);
        }

        /// <summary>
        /// 获取月份键值（使用默认配置）
        /// </summary>
        private string GetMonthKey(string? dateStr)
        {
            return GetMonthKey(dateStr, ExportSettingsService.Config);
        }

        /// <summary>
        /// 按分组导出Excel（每个分组一个Sheet）
        /// </summary>
        private async Task<ExportResult> ExportToExcelGroupedAsync(string filePath, List<TrainRideInfo> trainRides, ExportConfig config, GroupOption groupOption)
        {
            // 检查文件是否被占用
            if (File.Exists(filePath))
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                }
                catch (IOException)
                {
                    return new ExportResult { Success = false, Message = $"文件已被占用，请关闭后重试：{filePath}" };
                }
            }

            return await Task.Run(() =>
            {
                IWorkbook workbook = new XSSFWorkbook();

                // 按分组键分组
                var groupedData = trainRides.GroupBy(r => GetGroupKey(r, groupOption, config))
                                            .OrderBy(g => g.Key)
                                            .ToList();

                foreach (var group in groupedData)
                {
                    // 使用用户设置的工作表名称作为前缀，加上分组值
                    string sheetName = SanitizeSheetName($"{config.ExcelSheetName}-{group.Key}");
                    ISheet sheet = workbook.CreateSheet(sheetName);

                    // 创建单元格样式
                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    IFont headerFont = workbook.CreateFont();
                    headerFont.IsBold = true;
                    headerStyle.SetFont(headerFont);

                    int rowIndex = 0;

                    // 写入表头
                    if (config.ExcelIncludeHeader)
                    {
                        IRow headerRow = sheet.CreateRow(rowIndex++);
                        int colIndex = 0;

                        if (config.ExportTicketNumber) headerRow.CreateCell(colIndex++).SetCellValue("票号");
                        if (config.ExportTrainNo) headerRow.CreateCell(colIndex++).SetCellValue("车次");
                        if (config.ExportDepartStation) headerRow.CreateCell(colIndex++).SetCellValue("出发站");
                        if (config.ExportArriveStation) headerRow.CreateCell(colIndex++).SetCellValue("到达站");
                        if (config.ExportDepartDate) headerRow.CreateCell(colIndex++).SetCellValue("出发日期");
                        if (config.ExportDepartTime) headerRow.CreateCell(colIndex++).SetCellValue("出发时间");
                        if (config.ExportCoachNo) headerRow.CreateCell(colIndex++).SetCellValue("车厢号");
                        if (config.ExportSeatNo) headerRow.CreateCell(colIndex++).SetCellValue("座位号");
                        if (config.ExportSeatType) headerRow.CreateCell(colIndex++).SetCellValue("席别");
                        if (config.ExportMoney) headerRow.CreateCell(colIndex++).SetCellValue("票价");
                        if (config.ExportCheckInLocation) headerRow.CreateCell(colIndex++).SetCellValue("检票口");
                        if (config.ExportTags) headerRow.CreateCell(colIndex++).SetCellValue("标签");
                        if (config.ExportAdditionalInfo) headerRow.CreateCell(colIndex++).SetCellValue("备注");

                        // 应用表头样式
                        for (int i = 0; i < colIndex; i++)
                        {
                            headerRow.GetCell(i).CellStyle = headerStyle;
                        }
                    }

                    // 写入数据
                    foreach (var ride in group)
                    {
                        IRow row = sheet.CreateRow(rowIndex++);
                        int colIndex = 0;

                        if (config.ExportTicketNumber) row.CreateCell(colIndex++).SetCellValue(ride.TicketNumber);
                        if (config.ExportTrainNo) row.CreateCell(colIndex++).SetCellValue(ride.TrainNo);
                        if (config.ExportDepartStation) row.CreateCell(colIndex++).SetCellValue(ride.DepartStation);
                        if (config.ExportArriveStation) row.CreateCell(colIndex++).SetCellValue(ride.ArriveStation);
                        if (config.ExportDepartDate) row.CreateCell(colIndex++).SetCellValue(FormatDate(ride.DepartDate, config.ExcelDateFormat));
                        if (config.ExportDepartTime) row.CreateCell(colIndex++).SetCellValue(ride.DepartTime);
                        if (config.ExportCoachNo) row.CreateCell(colIndex++).SetCellValue(ride.CoachNo);
                        if (config.ExportSeatNo) row.CreateCell(colIndex++).SetCellValue(ride.SeatNo);
                        if (config.ExportSeatType) row.CreateCell(colIndex++).SetCellValue(ride.SeatType);
                        if (config.ExportMoney) row.CreateCell(colIndex++).SetCellValue(FormatMoney(ride.Money, config.ExcelMoneyFormat));
                        if (config.ExportCheckInLocation) row.CreateCell(colIndex++).SetCellValue(ride.CheckInLocation);
                        if (config.ExportTags) row.CreateCell(colIndex++).SetCellValue("");
                        if (config.ExportAdditionalInfo) row.CreateCell(colIndex++).SetCellValue(ride.AdditionalInfo);
                    }

                    // 自动调整列宽
                    if (config.ExcelAutoFitColumns)
                    {
                        int colCount = sheet.GetRow(0)?.LastCellNum ?? 0;
                        for (int i = 0; i < colCount; i++)
                        {
                            // 先自动调整
                            sheet.AutoSizeColumn(i);
                            
                            // 获取当前宽度（单位是1/256个字符宽度）
                            int currentWidth = sheet.GetColumnWidth(i);
                            
                            // 为中文字符增加额外宽度（至少3000，即约12个字符宽度）
                            int newWidth = Math.Max(currentWidth + 1000, 3000);
                            sheet.SetColumnWidth(i, newWidth);
                        }
                    }
                }

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }

                return new ExportResult { Success = true, FilePath = filePath, RecordCount = trainRides.Count };
            });
        }

        /// <summary>
        /// 清理Sheet名称（Excel Sheet名称有长度和字符限制）
        /// </summary>
        private string SanitizeSheetName(string name)
        {
            // Excel Sheet名称限制：最多31个字符，不能包含特殊字符
            string invalidChars = @":\/?*[]";
            foreach (char c in invalidChars)
            {
                name = name.Replace(c, '_');
            }

            if (name.Length > 31)
            {
                name = name.Substring(0, 31);
            }

            // 确保不以单引号开头或结尾
            name = name.Trim('\'');

            // 如果为空，使用默认名称
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Sheet1";
            }

            return name;
        }

        /// <summary>
        /// 按分组导出CSV（添加分组列）
        /// </summary>
        private async Task<ExportResult> ExportToCsvGroupedAsync(string filePath, List<TrainRideInfo> trainRides, ExportConfig config, GroupOption groupOption)
        {
            return await Task.Run(() =>
            {
                Encoding encoding = config.CsvEncoding switch
                {
                    CsvEncodingOption.UTF8 => new UTF8Encoding(true),
                    CsvEncodingOption.GBK => Encoding.GetEncoding("GBK"),
                    CsvEncodingOption.Unicode => Encoding.Unicode,
                    _ => Encoding.UTF8
                };

                string delimiter = config.CsvDelimiter switch
                {
                    CsvDelimiterOption.Comma => ",",
                    CsvDelimiterOption.Semicolon => ";",
                    CsvDelimiterOption.Tab => "\t",
                    _ => ","
                };

                using (var writer = new StreamWriter(filePath, false, encoding))
                {
                    // 写入表头（添加分组列）
                    if (config.CsvIncludeHeader)
                    {
                        var headers = new List<string>();
                        headers.Add("分组");
                        if (config.ExportTicketNumber) headers.Add("票号");
                        if (config.ExportTrainNo) headers.Add("车次");
                        if (config.ExportDepartStation) headers.Add("出发站");
                        if (config.ExportArriveStation) headers.Add("到达站");
                        if (config.ExportDepartDate) headers.Add("出发日期");
                        if (config.ExportDepartTime) headers.Add("出发时间");
                        if (config.ExportCoachNo) headers.Add("车厢号");
                        if (config.ExportSeatNo) headers.Add("座位号");
                        if (config.ExportSeatType) headers.Add("席别");
                        if (config.ExportMoney) headers.Add("票价");
                        if (config.ExportCheckInLocation) headers.Add("检票口");
                        if (config.ExportTags) headers.Add("标签");
                        if (config.ExportAdditionalInfo) headers.Add("备注");

                        writer.WriteLine(string.Join(delimiter, headers.Select(h => CsvEscape(h, config.CsvUseTextQualifier))));
                    }

                    // 按分组排序后写入数据
                    var sortedData = trainRides.OrderBy(r => GetGroupKey(r, groupOption, config))
                                               .ThenBy(r => r.DepartDate)
                                               .ThenBy(r => r.DepartTime)
                                               .ToList();

                    foreach (var ride in sortedData)
                    {
                        var values = new List<string>();
                        values.Add(GetGroupKey(ride, groupOption, config));
                        if (config.ExportTicketNumber) values.Add(ride.TicketNumber);
                        if (config.ExportTrainNo) values.Add(ride.TrainNo);
                        if (config.ExportDepartStation) values.Add(ride.DepartStation);
                        if (config.ExportArriveStation) values.Add(ride.ArriveStation);
                        if (config.ExportDepartDate) values.Add(FormatDate(ride.DepartDate, config.CsvDateFormat));
                        if (config.ExportDepartTime) values.Add(ride.DepartTime);
                        if (config.ExportCoachNo) values.Add(ride.CoachNo);
                        if (config.ExportSeatNo) values.Add(ride.SeatNo);
                        if (config.ExportSeatType) values.Add(ride.SeatType);
                        if (config.ExportMoney) values.Add(FormatMoney(ride.Money, config.CsvMoneyFormat));
                        if (config.ExportCheckInLocation) values.Add(ride.CheckInLocation);
                        if (config.ExportTags) values.Add("");
                        if (config.ExportAdditionalInfo) values.Add(ride.AdditionalInfo ?? "");

                        writer.WriteLine(string.Join(delimiter, values.Select(v => CsvEscape(v, config.CsvUseTextQualifier))));
                    }
                }

                return new ExportResult { Success = true, FilePath = filePath, RecordCount = trainRides.Count };
            });
        }

        /// <summary>
        /// 按分组导出PDF（按分组添加标题）
        /// </summary>
        private async Task<ExportResult> ExportToPdfGroupedAsync(string filePath, List<TrainRideInfo> trainRides, ExportConfig config, GroupOption groupOption)
        {
            return await Task.Run(() =>
            {
                var document = new PdfDocument();
                document.Info.Title = "行程记录导出";
                document.Info.Author = "GuiPiao";

                // 计算页面尺寸
                XSize pageSize = config.PdfPaperSize switch
                {
                    "A5" => new XSize(XUnit.FromMillimeter(148).Point, XUnit.FromMillimeter(210).Point),
                    "Letter" => new XSize(XUnit.FromMillimeter(216).Point, XUnit.FromMillimeter(279).Point),
                    _ => new XSize(XUnit.FromMillimeter(210).Point, XUnit.FromMillimeter(297).Point) // A4
                };

                // 按分组键分组
                var groupedData = trainRides.GroupBy(r => GetGroupKey(r, groupOption, config))
                                            .OrderBy(g => g.Key)
                                            .ToList();

                int rowsPerPage = config.PdfRowsPerPage;
                int totalGroups = groupedData.Count;
                int currentGroupIndex = 0;

                foreach (var group in groupedData)
                {
                    currentGroupIndex++;
                    var groupItems = group.ToList();
                    int totalPagesForGroup = (int)Math.Ceiling((double)groupItems.Count / rowsPerPage);
                    if (totalPagesForGroup == 0) totalPagesForGroup = 1;

                    for (int pageIndex = 0; pageIndex < totalPagesForGroup; pageIndex++)
                    {
                        var page = document.AddPage();
                        if (config.PdfLandscape)
                        {
                            page.Width = pageSize.Height;
                            page.Height = pageSize.Width;
                        }
                        else
                        {
                            page.Width = pageSize.Width;
                            page.Height = pageSize.Height;
                        }

                        var gfx = XGraphics.FromPdfPage(page);

                        // 使用支持中文的字体
                        var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);
                        var font = new XFont("Microsoft YaHei", config.PdfFontSize, XFontStyle.Regular, fontOptions);
                        var headerFont = new XFont("Microsoft YaHei", config.PdfFontSize + 2, XFontStyle.Bold, fontOptions);
                        var groupTitleFont = new XFont("Microsoft YaHei", config.PdfFontSize + 4, XFontStyle.Bold, fontOptions);

                        double marginLeft = XUnit.FromMillimeter(config.PdfMarginLeft).Point;
                        double marginTop = XUnit.FromMillimeter(config.PdfMarginTop).Point;
                        double marginRight = page.Width.Point - XUnit.FromMillimeter(config.PdfMarginRight).Point;
                        double marginBottom = page.Height.Point - XUnit.FromMillimeter(config.PdfMarginBottom).Point;

                        double y = marginTop;
                        double lineHeight = config.PdfFontSize * 1.5;

                        // 绘制分组标题（每页顶部）
                        string groupTitle = $"分组：{group.Key}";
                        gfx.DrawString(groupTitle, groupTitleFont, XBrushes.DarkBlue, new XRect(marginLeft, y, marginRight - marginLeft, lineHeight * 1.5), XStringFormats.CenterLeft);
                        y += lineHeight * 2;

                        // 获取当前页的数据
                        var pageData = groupItems.Skip(pageIndex * rowsPerPage).Take(rowsPerPage).ToList();

                        // 计算列宽
                        var columns = GetExportColumns(config);
                        double tableWidth = marginRight - marginLeft;
                        double colWidth = tableWidth / columns.Count;

                        // 绘制表头
                        if (config.ExcelIncludeHeader || pageIndex == 0)
                        {
                            double x = marginLeft;
                            foreach (var col in columns)
                            {
                                gfx.DrawString(col, headerFont, XBrushes.Black, new XRect(x, y, colWidth, lineHeight), XStringFormats.CenterLeft);
                                x += colWidth;
                            }
                            y += lineHeight;
                            gfx.DrawLine(XPens.Black, marginLeft, y, marginRight, y);
                            y += 5;
                        }

                        // 绘制数据
                        foreach (var ride in pageData)
                        {
                            if (y + lineHeight > marginBottom)
                                break;

                            double x = marginLeft;
                            var values = GetExportValues(ride, config);
                            for (int i = 0; i < values.Count && i < columns.Count; i++)
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(x, y, colWidth, lineHeight), XStringFormats.CenterLeft);
                                x += colWidth;
                            }
                            y += lineHeight;
                        }

                        // 页脚页码
                        if (document.PageCount > 1)
                        {
                            var pageNumText = $"第 {document.PageCount} 页";
                            gfx.DrawString(pageNumText, font, XBrushes.Gray, new XRect(marginLeft, marginBottom - lineHeight, tableWidth, lineHeight), XStringFormats.Center);
                        }
                    }
                }

                document.Save(filePath);
                return new ExportResult { Success = true, FilePath = filePath, RecordCount = trainRides.Count };
            });
        }
    }

    /// <summary>
    /// 导出结果
    /// </summary>
    public class ExportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string FilePath { get; set; } = "";
        public int RecordCount { get; set; }
    }
}
