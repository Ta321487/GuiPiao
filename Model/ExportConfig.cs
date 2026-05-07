namespace GuiPiao.Model;

/// <summary>
///     导出格式枚举
/// </summary>
public enum ExportFormatOption
{
    Excel = 0, // Excel格式
    Csv = 1, // CSV格式
    Pdf = 2, // PDF格式
    Image = 3 // 图片格式
}

/// <summary>
///     CSV编码格式枚举
/// </summary>
public enum CsvEncodingOption
{
    UTF8 = 0, // UTF-8编码
    GBK = 1, // GBK编码(兼容Excel)
    Unicode = 2 // Unicode编码
}

/// <summary>
///     CSV分隔符枚举
/// </summary>
public enum CsvDelimiterOption
{
    Comma = 0, // 逗号
    Semicolon = 1, // 分号
    Tab = 2 // 制表符
}

/// <summary>
///     日期格式枚举
/// </summary>
public enum DateFormatOption
{
    yyyyMMdd = 0, // 2024-01-15
    yyyyMMddSlash = 1, // 2024/01/15
    yyyyMMddChinese = 2, // 2024年01月15日
    MMddyyyy = 3 // 01/15/2024
}

/// <summary>
///     金额格式枚举
/// </summary>
public enum MoneyFormatOption
{
    Yuan = 0, // 523.50
    YuanWithSymbol = 1, // ¥523.50
    Fen = 2 // 52350
}

/// <summary>
///     导出设置配置类
/// </summary>
public class ExportConfig
{
    // 默认导出设置
    public ExportFormatOption DefaultFormat { get; set; } = ExportFormatOption.Excel;
    public string DefaultSavePath { get; set; } = "";
    public string FileNameTemplate { get; set; } = "行程导出_{日期}";
    public bool OpenAfterExport { get; set; } = true;
    public bool ShowSuccessMessage { get; set; } = true;

    // Excel导出设置
    public string ExcelSheetName { get; set; } = "行程记录";
    public bool ExcelIncludeHeader { get; set; } = true;
    public bool ExcelAutoFitColumns { get; set; } = true;
    public DateFormatOption ExcelDateFormat { get; set; } = DateFormatOption.yyyyMMdd;
    public MoneyFormatOption ExcelMoneyFormat { get; set; } = MoneyFormatOption.YuanWithSymbol;

    // CSV导出设置
    public CsvEncodingOption CsvEncoding { get; set; } = CsvEncodingOption.UTF8;
    public CsvDelimiterOption CsvDelimiter { get; set; } = CsvDelimiterOption.Comma;
    public bool CsvIncludeHeader { get; set; } = true;
    public bool CsvUseTextQualifier { get; set; } = true;
    public DateFormatOption CsvDateFormat { get; set; } = DateFormatOption.yyyyMMdd;
    public MoneyFormatOption CsvMoneyFormat { get; set; } = MoneyFormatOption.Yuan;

    // PDF/图片导出设置
    public int PdfRowsPerPage { get; set; } = 20;
    public string PdfPaperSize { get; set; } = "A4";
    public bool PdfLandscape { get; set; } = false;
    public int PdfFontSize { get; set; } = 10;
    public int PdfMarginTop { get; set; } = 20;
    public int PdfMarginBottom { get; set; } = 20;
    public int PdfMarginLeft { get; set; } = 15;
    public int PdfMarginRight { get; set; } = 15;

    // 导出字段选择
    public bool ExportTicketNumber { get; set; } = true;
    public bool ExportTrainNo { get; set; } = true;
    public bool ExportDepartStation { get; set; } = true;
    public bool ExportArriveStation { get; set; } = true;
    public bool ExportDepartDate { get; set; } = true;
    public bool ExportDepartTime { get; set; } = true;
    public bool ExportCoachNo { get; set; } = true;
    public bool ExportSeatNo { get; set; } = true;
    public bool ExportSeatType { get; set; } = true;
    public bool ExportMoney { get; set; } = true;
    public bool ExportCheckInLocation { get; set; } = false;
    public bool ExportTags { get; set; } = false;
    public bool ExportAdditionalInfo { get; set; } = false;

    // 分组导出设置
    public bool EnableGroupExport { get; set; } = false;
}