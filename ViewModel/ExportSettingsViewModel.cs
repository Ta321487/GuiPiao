using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;
using Application = System.Windows.Application;

namespace GuiPiao.ViewModel;

/// <summary>
///     导出设置视图模型
/// </summary>
public partial class ExportSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly ExportService _exportService;
    private readonly ExportSettingsService _settingsService;
    private readonly UISettingsService _uiSettingsService;

    [ObservableProperty] private ExportConfig _config;

    [ObservableProperty] private bool _hasUnsavedChanges;

    private ExportConfig _originalConfig;

    public ExportSettingsViewModel()
    {
        _settingsService = new ExportSettingsService();
        _exportService = new ExportService();
        _uiSettingsService = new UISettingsService();
        Config = _settingsService.Config;
        _originalConfig = CloneConfig(Config);
    }

    // 默认导出设置属性
    public ExportFormatOption DefaultFormat
    {
        get => Config.DefaultFormat;
        set
        {
            if (Config.DefaultFormat != value)
            {
                Config.DefaultFormat = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public string DefaultSavePath
    {
        get => Config.DefaultSavePath;
        set
        {
            if (Config.DefaultSavePath != value)
            {
                Config.DefaultSavePath = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public string FileNameTemplate
    {
        get => Config.FileNameTemplate;
        set
        {
            if (Config.FileNameTemplate != value)
            {
                Config.FileNameTemplate = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool OpenAfterExport
    {
        get => Config.OpenAfterExport;
        set
        {
            if (Config.OpenAfterExport != value)
            {
                Config.OpenAfterExport = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ShowSuccessMessage
    {
        get => Config.ShowSuccessMessage;
        set
        {
            if (Config.ShowSuccessMessage != value)
            {
                Config.ShowSuccessMessage = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    // Excel导出设置属性
    public string ExcelSheetName
    {
        get => Config.ExcelSheetName;
        set
        {
            if (Config.ExcelSheetName != value)
            {
                Config.ExcelSheetName = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExcelIncludeHeader
    {
        get => Config.ExcelIncludeHeader;
        set
        {
            if (Config.ExcelIncludeHeader != value)
            {
                Config.ExcelIncludeHeader = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExcelAutoFitColumns
    {
        get => Config.ExcelAutoFitColumns;
        set
        {
            if (Config.ExcelAutoFitColumns != value)
            {
                Config.ExcelAutoFitColumns = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public DateFormatOption ExcelDateFormat
    {
        get => Config.ExcelDateFormat;
        set
        {
            if (Config.ExcelDateFormat != value)
            {
                Config.ExcelDateFormat = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public MoneyFormatOption ExcelMoneyFormat
    {
        get => Config.ExcelMoneyFormat;
        set
        {
            if (Config.ExcelMoneyFormat != value)
            {
                Config.ExcelMoneyFormat = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    // CSV导出设置属性
    public CsvEncodingOption CsvEncoding
    {
        get => Config.CsvEncoding;
        set
        {
            if (Config.CsvEncoding != value)
            {
                Config.CsvEncoding = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public CsvDelimiterOption CsvDelimiter
    {
        get => Config.CsvDelimiter;
        set
        {
            if (Config.CsvDelimiter != value)
            {
                Config.CsvDelimiter = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool CsvIncludeHeader
    {
        get => Config.CsvIncludeHeader;
        set
        {
            if (Config.CsvIncludeHeader != value)
            {
                Config.CsvIncludeHeader = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool CsvUseTextQualifier
    {
        get => Config.CsvUseTextQualifier;
        set
        {
            if (Config.CsvUseTextQualifier != value)
            {
                Config.CsvUseTextQualifier = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public DateFormatOption CsvDateFormat
    {
        get => Config.CsvDateFormat;
        set
        {
            if (Config.CsvDateFormat != value)
            {
                Config.CsvDateFormat = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public MoneyFormatOption CsvMoneyFormat
    {
        get => Config.CsvMoneyFormat;
        set
        {
            if (Config.CsvMoneyFormat != value)
            {
                Config.CsvMoneyFormat = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    // PDF/图片导出设置属性
    public int PdfRowsPerPage
    {
        get => Config.PdfRowsPerPage;
        set
        {
            if (Config.PdfRowsPerPage != value)
            {
                Config.PdfRowsPerPage = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public string PdfPaperSize
    {
        get => Config.PdfPaperSize;
        set
        {
            if (Config.PdfPaperSize != value)
            {
                Config.PdfPaperSize = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool IsPdfPortrait
    {
        get => !Config.PdfLandscape;
        set
        {
            if (Config.PdfLandscape == value)
            {
                Config.PdfLandscape = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPdfLandscape));
                CheckForChanges();
            }
        }
    }

    public bool IsPdfLandscape
    {
        get => Config.PdfLandscape;
        set
        {
            if (Config.PdfLandscape != value)
            {
                Config.PdfLandscape = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPdfPortrait));
                CheckForChanges();
            }
        }
    }

    public int PdfFontSize
    {
        get => Config.PdfFontSize;
        set
        {
            if (Config.PdfFontSize != value)
            {
                Config.PdfFontSize = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public int PdfMarginTop
    {
        get => Config.PdfMarginTop;
        set
        {
            if (Config.PdfMarginTop != value)
            {
                Config.PdfMarginTop = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public int PdfMarginBottom
    {
        get => Config.PdfMarginBottom;
        set
        {
            if (Config.PdfMarginBottom != value)
            {
                Config.PdfMarginBottom = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public int PdfMarginLeft
    {
        get => Config.PdfMarginLeft;
        set
        {
            if (Config.PdfMarginLeft != value)
            {
                Config.PdfMarginLeft = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public int PdfMarginRight
    {
        get => Config.PdfMarginRight;
        set
        {
            if (Config.PdfMarginRight != value)
            {
                Config.PdfMarginRight = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    // 导出字段选择属性
    public bool ExportTicketNumber
    {
        get => Config.ExportTicketNumber;
        set
        {
            if (Config.ExportTicketNumber != value)
            {
                Config.ExportTicketNumber = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportTrainNo
    {
        get => Config.ExportTrainNo;
        set
        {
            if (Config.ExportTrainNo != value)
            {
                Config.ExportTrainNo = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportDepartStation
    {
        get => Config.ExportDepartStation;
        set
        {
            if (Config.ExportDepartStation != value)
            {
                Config.ExportDepartStation = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportArriveStation
    {
        get => Config.ExportArriveStation;
        set
        {
            if (Config.ExportArriveStation != value)
            {
                Config.ExportArriveStation = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportDepartDate
    {
        get => Config.ExportDepartDate;
        set
        {
            if (Config.ExportDepartDate != value)
            {
                Config.ExportDepartDate = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportDepartTime
    {
        get => Config.ExportDepartTime;
        set
        {
            if (Config.ExportDepartTime != value)
            {
                Config.ExportDepartTime = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportCoachNo
    {
        get => Config.ExportCoachNo;
        set
        {
            if (Config.ExportCoachNo != value)
            {
                Config.ExportCoachNo = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportSeatNo
    {
        get => Config.ExportSeatNo;
        set
        {
            if (Config.ExportSeatNo != value)
            {
                Config.ExportSeatNo = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportSeatType
    {
        get => Config.ExportSeatType;
        set
        {
            if (Config.ExportSeatType != value)
            {
                Config.ExportSeatType = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportMoney
    {
        get => Config.ExportMoney;
        set
        {
            if (Config.ExportMoney != value)
            {
                Config.ExportMoney = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportCheckInLocation
    {
        get => Config.ExportCheckInLocation;
        set
        {
            if (Config.ExportCheckInLocation != value)
            {
                Config.ExportCheckInLocation = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportTags
    {
        get => Config.ExportTags;
        set
        {
            if (Config.ExportTags != value)
            {
                Config.ExportTags = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    public bool ExportAdditionalInfo
    {
        get => Config.ExportAdditionalInfo;
        set
        {
            if (Config.ExportAdditionalInfo != value)
            {
                Config.ExportAdditionalInfo = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    // 分组导出设置
    public bool EnableGroupExport
    {
        get => Config.EnableGroupExport;
        set
        {
            if (Config.EnableGroupExport != value)
            {
                Config.EnableGroupExport = value;
                OnPropertyChanged();
                CheckForChanges();
            }
        }
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    /// <summary>
    ///     重新加载设置
    /// </summary>
    public void ReloadSettings()
    {
        _settingsService.RefreshConfig();
        Config = _settingsService.Config;
        _originalConfig = CloneConfig(Config);
        HasUnsavedChanges = false;

        // 触发所有属性变更通知
        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    ///     克隆配置对象用于比较
    /// </summary>
    private ExportConfig CloneConfig(ExportConfig source)
    {
        return new ExportConfig
        {
            DefaultFormat = source.DefaultFormat,
            DefaultSavePath = source.DefaultSavePath,
            FileNameTemplate = source.FileNameTemplate,
            OpenAfterExport = source.OpenAfterExport,
            ShowSuccessMessage = source.ShowSuccessMessage,
            ExcelSheetName = source.ExcelSheetName,
            ExcelIncludeHeader = source.ExcelIncludeHeader,
            ExcelAutoFitColumns = source.ExcelAutoFitColumns,
            ExcelDateFormat = source.ExcelDateFormat,
            ExcelMoneyFormat = source.ExcelMoneyFormat,
            CsvEncoding = source.CsvEncoding,
            CsvDelimiter = source.CsvDelimiter,
            CsvIncludeHeader = source.CsvIncludeHeader,
            CsvUseTextQualifier = source.CsvUseTextQualifier,
            CsvDateFormat = source.CsvDateFormat,
            CsvMoneyFormat = source.CsvMoneyFormat,
            PdfRowsPerPage = source.PdfRowsPerPage,
            PdfPaperSize = source.PdfPaperSize,
            PdfLandscape = source.PdfLandscape,
            PdfFontSize = source.PdfFontSize,
            PdfMarginTop = source.PdfMarginTop,
            PdfMarginBottom = source.PdfMarginBottom,
            PdfMarginLeft = source.PdfMarginLeft,
            PdfMarginRight = source.PdfMarginRight,
            ExportTicketNumber = source.ExportTicketNumber,
            ExportTrainNo = source.ExportTrainNo,
            ExportDepartStation = source.ExportDepartStation,
            ExportArriveStation = source.ExportArriveStation,
            ExportDepartDate = source.ExportDepartDate,
            ExportDepartTime = source.ExportDepartTime,
            ExportCoachNo = source.ExportCoachNo,
            ExportSeatNo = source.ExportSeatNo,
            ExportSeatType = source.ExportSeatType,
            ExportMoney = source.ExportMoney,
            ExportCheckInLocation = source.ExportCheckInLocation,
            ExportTags = source.ExportTags,
            ExportAdditionalInfo = source.ExportAdditionalInfo,
            EnableGroupExport = source.EnableGroupExport
        };
    }

    /// <summary>
    ///     检查是否有未保存的更改
    /// </summary>
    private void CheckForChanges()
    {
        HasUnsavedChanges = !ConfigsEqual(Config, _originalConfig);
    }

    /// <summary>
    ///     比较两个配置对象是否相等
    /// </summary>
    private bool ConfigsEqual(ExportConfig a, ExportConfig b)
    {
        return a.DefaultFormat == b.DefaultFormat &&
               a.DefaultSavePath == b.DefaultSavePath &&
               a.FileNameTemplate == b.FileNameTemplate &&
               a.OpenAfterExport == b.OpenAfterExport &&
               a.ShowSuccessMessage == b.ShowSuccessMessage &&
               a.ExcelSheetName == b.ExcelSheetName &&
               a.ExcelIncludeHeader == b.ExcelIncludeHeader &&
               a.ExcelAutoFitColumns == b.ExcelAutoFitColumns &&
               a.ExcelDateFormat == b.ExcelDateFormat &&
               a.ExcelMoneyFormat == b.ExcelMoneyFormat &&
               a.CsvEncoding == b.CsvEncoding &&
               a.CsvDelimiter == b.CsvDelimiter &&
               a.CsvIncludeHeader == b.CsvIncludeHeader &&
               a.CsvUseTextQualifier == b.CsvUseTextQualifier &&
               a.CsvDateFormat == b.CsvDateFormat &&
               a.CsvMoneyFormat == b.CsvMoneyFormat &&
               a.PdfRowsPerPage == b.PdfRowsPerPage &&
               a.PdfPaperSize == b.PdfPaperSize &&
               a.PdfLandscape == b.PdfLandscape &&
               a.PdfFontSize == b.PdfFontSize &&
               a.PdfMarginTop == b.PdfMarginTop &&
               a.PdfMarginBottom == b.PdfMarginBottom &&
               a.PdfMarginLeft == b.PdfMarginLeft &&
               a.PdfMarginRight == b.PdfMarginRight &&
               a.ExportTicketNumber == b.ExportTicketNumber &&
               a.ExportTrainNo == b.ExportTrainNo &&
               a.ExportDepartStation == b.ExportDepartStation &&
               a.ExportArriveStation == b.ExportArriveStation &&
               a.ExportDepartDate == b.ExportDepartDate &&
               a.ExportDepartTime == b.ExportDepartTime &&
               a.ExportCoachNo == b.ExportCoachNo &&
               a.ExportSeatNo == b.ExportSeatNo &&
               a.ExportSeatType == b.ExportSeatType &&
               a.ExportMoney == b.ExportMoney &&
               a.ExportCheckInLocation == b.ExportCheckInLocation &&
               a.ExportTags == b.ExportTags &&
               a.ExportAdditionalInfo == b.ExportAdditionalInfo &&
               a.EnableGroupExport == b.EnableGroupExport;
    }

    /// <summary>
    ///     保存设置内部实现
    /// </summary>
    private async Task SaveSettingsInternalAsync(bool showMessage)
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        // 校验导出字段选择 - 至少选择4个字段
        var selectedFieldCount = 0;
        if (Config.ExportTicketNumber) selectedFieldCount++;
        if (Config.ExportTrainNo) selectedFieldCount++;
        if (Config.ExportDepartStation) selectedFieldCount++;
        if (Config.ExportArriveStation) selectedFieldCount++;
        if (Config.ExportDepartDate) selectedFieldCount++;
        if (Config.ExportDepartTime) selectedFieldCount++;
        if (Config.ExportCoachNo) selectedFieldCount++;
        if (Config.ExportSeatNo) selectedFieldCount++;
        if (Config.ExportSeatType) selectedFieldCount++;
        if (Config.ExportMoney) selectedFieldCount++;
        if (Config.ExportCheckInLocation) selectedFieldCount++;
        if (Config.ExportTags) selectedFieldCount++;
        if (Config.ExportAdditionalInfo) selectedFieldCount++;

        if (selectedFieldCount < 4)
        {
            MessageBoxWindow.Show(settingsWindow, $"请至少选择4个导出字段（当前已选择{selectedFieldCount}个）", "校验失败",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 校验文件名模板
        if (string.IsNullOrWhiteSpace(Config.FileNameTemplate))
        {
            MessageBoxWindow.Show(settingsWindow, "文件名模板不能为空", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Config.FileNameTemplate.Length > 100)
        {
            MessageBoxWindow.Show(settingsWindow, "文件名模板长度不能超过100个字符", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 文件名模板不能包含非法字符
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        if (Config.FileNameTemplate.Any(c => invalidFileNameChars.Contains(c) && c != '{' && c != '}'))
        {
            MessageBoxWindow.Show(settingsWindow, "文件名模板包含非法字符", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 校验Excel工作表名称
        if (string.IsNullOrWhiteSpace(Config.ExcelSheetName))
        {
            MessageBoxWindow.Show(settingsWindow, "Excel工作表名称不能为空", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (Config.ExcelSheetName.Length > 31)
        {
            MessageBoxWindow.Show(settingsWindow, "Excel工作表名称不能超过31个字符", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Excel工作表名称不能包含非法字符
        var invalidSheetNameChars = @":\/?*[]";
        if (Config.ExcelSheetName.Any(c => invalidSheetNameChars.Contains(c)))
        {
            MessageBoxWindow.Show(settingsWindow, "Excel工作表名称包含非法字符（不能包含 : \\ / ? * [ ]）", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 校验PDF每页行数
        if (Config.PdfRowsPerPage < 5 || Config.PdfRowsPerPage > 100)
        {
            MessageBoxWindow.Show(settingsWindow, "PDF每页行数必须在5-100之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 校验PDF页边距
        if (Config.PdfMarginTop < 0 || Config.PdfMarginTop > 50)
        {
            MessageBoxWindow.Show(settingsWindow, "PDF上边距必须在0-50mm之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (Config.PdfMarginBottom < 0 || Config.PdfMarginBottom > 50)
        {
            MessageBoxWindow.Show(settingsWindow, "PDF下边距必须在0-50mm之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (Config.PdfMarginLeft < 0 || Config.PdfMarginLeft > 50)
        {
            MessageBoxWindow.Show(settingsWindow, "PDF左边距必须在0-50mm之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (Config.PdfMarginRight < 0 || Config.PdfMarginRight > 50)
        {
            MessageBoxWindow.Show(settingsWindow, "PDF右边距必须在0-50mm之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            _settingsService.SaveConfig(Config);
            _originalConfig = CloneConfig(Config);
            HasUnsavedChanges = false;

            if (showMessage) MessageBoxWindow.Show(settingsWindow, "导出设置已保存", SettingsDialogMessages.SuccessTitle);
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"{SettingsDialogMessages.SaveFailedPrefix}{ex.Message}",
                SettingsDialogMessages.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     浏览保存路径命令
    /// </summary>
    [RelayCommand]
    private void BrowsePath()
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "选择默认导出保存位置",
                SelectedPath = DefaultSavePath
            };

            if (dialog.ShowDialog() == DialogResult.OK) DefaultSavePath = dialog.SelectedPath;
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"打开文件夹选择对话框失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     保存设置命令
    /// </summary>
    [RelayCommand]
    private async Task SaveSettings()
    {
        await SaveSettingsInternalAsync(true);
    }

    /// <summary>
    ///     恢复默认设置命令
    /// </summary>
    [RelayCommand]
    private async Task RestoreDefaults()
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        var result = MessageBoxWindow.Show(settingsWindow, SettingsDialogMessages.RestoreConfirmBody,
            SettingsDialogMessages.ConfirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        var defaultConfig = _settingsService.GetDefaultConfig();

        // 默认导出设置
        DefaultFormat = defaultConfig.DefaultFormat;
        DefaultSavePath = defaultConfig.DefaultSavePath;
        FileNameTemplate = defaultConfig.FileNameTemplate;
        OpenAfterExport = defaultConfig.OpenAfterExport;
        ShowSuccessMessage = defaultConfig.ShowSuccessMessage;

        // Excel导出设置
        ExcelSheetName = defaultConfig.ExcelSheetName;
        ExcelIncludeHeader = defaultConfig.ExcelIncludeHeader;
        ExcelAutoFitColumns = defaultConfig.ExcelAutoFitColumns;
        ExcelDateFormat = defaultConfig.ExcelDateFormat;
        ExcelMoneyFormat = defaultConfig.ExcelMoneyFormat;

        // CSV导出设置
        CsvEncoding = defaultConfig.CsvEncoding;
        CsvDelimiter = defaultConfig.CsvDelimiter;
        CsvIncludeHeader = defaultConfig.CsvIncludeHeader;
        CsvUseTextQualifier = defaultConfig.CsvUseTextQualifier;
        CsvDateFormat = defaultConfig.CsvDateFormat;
        CsvMoneyFormat = defaultConfig.CsvMoneyFormat;

        // PDF/图片导出设置
        PdfRowsPerPage = defaultConfig.PdfRowsPerPage;
        PdfPaperSize = defaultConfig.PdfPaperSize;
        IsPdfPortrait = !defaultConfig.PdfLandscape;
        PdfFontSize = defaultConfig.PdfFontSize;
        PdfMarginTop = defaultConfig.PdfMarginTop;
        PdfMarginBottom = defaultConfig.PdfMarginBottom;
        PdfMarginLeft = defaultConfig.PdfMarginLeft;
        PdfMarginRight = defaultConfig.PdfMarginRight;

        // 导出字段选择
        ExportTicketNumber = defaultConfig.ExportTicketNumber;
        ExportTrainNo = defaultConfig.ExportTrainNo;
        ExportDepartStation = defaultConfig.ExportDepartStation;
        ExportArriveStation = defaultConfig.ExportArriveStation;
        ExportDepartDate = defaultConfig.ExportDepartDate;
        ExportDepartTime = defaultConfig.ExportDepartTime;
        ExportCoachNo = defaultConfig.ExportCoachNo;
        ExportSeatNo = defaultConfig.ExportSeatNo;
        ExportSeatType = defaultConfig.ExportSeatType;
        ExportMoney = defaultConfig.ExportMoney;
        ExportCheckInLocation = defaultConfig.ExportCheckInLocation;
        ExportTags = defaultConfig.ExportTags;
        ExportAdditionalInfo = defaultConfig.ExportAdditionalInfo;

        // 分组导出设置
        EnableGroupExport = defaultConfig.EnableGroupExport;

        HasUnsavedChanges = true;

        MessageBoxWindow.Show(settingsWindow, SettingsDialogMessages.RestoreNeedSaveHint);
    }

    /// <summary>
    ///     预览导出效果命令
    /// </summary>
    [RelayCommand]
    private async Task PreviewExport()
    {
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            // 生成临时文件路径
            var tempPath = Path.Combine(Path.GetTempPath(),
                "GuiPiao_Export_Preview_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempPath);

            var fileName = _exportService.GenerateFileName(Config.FileNameTemplate, DefaultFormat, "多车次");
            var filePath = Path.Combine(tempPath, fileName);

            // 执行导出
            ExportResult result;
            if (Config.EnableGroupExport)
            {
                var groupOption = _uiSettingsService.Config.DefaultGroup;
                var allRides = await _exportService.GetAllTrainRidesAsync();
                result = await _exportService.ExportGroupedAsync(filePath, DefaultFormat, allRides, groupOption);
            }
            else
            {
                result = await _exportService.ExportAllAsync(filePath, DefaultFormat);
            }

            if (result.Success)
            {
                if (Config.OpenAfterExport)
                    Process.Start(new ProcessStartInfo(result.FilePath) { UseShellExecute = true });

                if (Config.ShowSuccessMessage)
                    MessageBoxWindow.Show(settingsWindow,
                        $"预览导出成功！\n文件路径: {result.FilePath}\n导出记录数: {result.RecordCount}", "成功");
            }
            else
            {
                MessageBoxWindow.Show(settingsWindow, $"导出失败: {result.Message}", "错误", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"导出异常: {ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     导出测试文件命令
    /// </summary>
    [RelayCommand]
    private async Task ExportTest()
    {
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        try
        {
            // 确定保存路径
            var savePath = Config.DefaultSavePath;
            if (string.IsNullOrEmpty(savePath) || !Directory.Exists(savePath))
                savePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var fileName = _exportService.GenerateFileName(Config.FileNameTemplate, DefaultFormat, "多车次");
            var filePath = Path.Combine(savePath, fileName);

            // 如果文件已存在，添加序号
            var counter = 1;
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            while (File.Exists(filePath))
            {
                fileName = $"{baseFileName}_{counter:D2}{extension}";
                filePath = Path.Combine(savePath, fileName);
                counter++;
            }

            // 执行导出
            ExportResult result;
            if (Config.EnableGroupExport)
            {
                var groupOption = _uiSettingsService.Config.DefaultGroup;
                var allRides = await _exportService.GetAllTrainRidesAsync();
                result = await _exportService.ExportGroupedAsync(filePath, DefaultFormat, allRides, groupOption);
            }
            else
            {
                result = await _exportService.ExportAllAsync(filePath, DefaultFormat);
            }

            if (result.Success)
            {
                if (Config.OpenAfterExport)
                    Process.Start(new ProcessStartInfo(result.FilePath) { UseShellExecute = true });

                if (Config.ShowSuccessMessage)
                    MessageBoxWindow.Show(settingsWindow,
                        $"导出成功！\n文件路径: {result.FilePath}\n导出记录数: {result.RecordCount}", "成功");
            }
            else
            {
                MessageBoxWindow.Show(settingsWindow, $"导出失败: {result.Message}", "错误", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"导出异常: {ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}