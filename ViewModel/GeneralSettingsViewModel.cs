using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

/// <summary>
///     常规设置视图模型
/// </summary>
public partial class GeneralSettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly GeneralSettingsService _settingsService;
    private bool _isLoadingConfig; // 标记是否正在加载配置
    private GeneralConfig _originalConfig;

    public GeneralSettingsViewModel()
    {
        // 检查是否在设计时
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

        _settingsService = new GeneralSettingsService();
        LoadConfig();
    }

    /// <summary>
    ///     是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get
        {
            if (_isLoadingConfig || _originalConfig == null)
                return false;

            var currentConfig = GetCurrentConfig();
            return !ConfigsEqual(_originalConfig, currentConfig);
        }
    }

    /// <summary>
    ///     重新加载设置（放弃更改）
    /// </summary>
    public void ReloadSettings()
    {
        LoadConfig();
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    /// <summary>
    ///     加载配置到视图模型
    /// </summary>
    private void LoadConfig()
    {
        _isLoadingConfig = true; // 开始加载配置
        try
        {
            _originalConfig = _settingsService.Config;

            // 外观与显示
            IsLightTheme = _originalConfig.ThemeMode == ThemeMode.Light;
            IsDarkTheme = _originalConfig.ThemeMode == ThemeMode.Dark;
            IsSystemTheme = _originalConfig.ThemeMode == ThemeMode.System;

            IsMicrosoftBlue = _originalConfig.AccentColor == AccentColor.MicrosoftBlue;
            IsFreshGreen = _originalConfig.AccentColor == AccentColor.FreshGreen;
            IsVitalityOrange = _originalConfig.AccentColor == AccentColor.VitalityOrange;
            IsDarkPurple = _originalConfig.AccentColor == AccentColor.DarkPurple;
            IsMinimalGray = _originalConfig.AccentColor == AccentColor.MinimalGray;
            IsCustomColor = _originalConfig.AccentColor == AccentColor.Custom;
            CustomColor = _originalConfig.CustomColor;

            FontSize = _originalConfig.FontSize.ToString();
            RowHeight = _originalConfig.RowHeight.ToString();

            // 程序启动与运行
            SingleInstance = _originalConfig.SingleInstance;
            StartupPage = _originalConfig.StartupPage.ToString();

            IsNormalWindow = _originalConfig.WindowState == WindowStateOption.Normal;
            IsMaximized = _originalConfig.WindowState == WindowStateOption.Maximized;
            IsMinimizedToTray = _originalConfig.WindowState == WindowStateOption.MinimizedToTray;

            AutoRefreshOnStartup = _originalConfig.AutoRefreshOnStartup;
            // 注意：AutoBackupOnExit 已移至数据库设置页面统一管理

            // 核心业务默认设置
            PageSize = _originalConfig.PageSize.ToString();
            DefaultSort = _originalConfig.DefaultSort.ToString();
            LoadRange = _originalConfig.LoadRange.ToString();
            DefaultSeatType = _originalConfig.DefaultSeatType.ToString();
            DefaultTicketStatus = _originalConfig.DefaultTicketStatus.ToString();
            OcrDirectSave = !_originalConfig.OcrEditConfirm;
            OcrEditConfirm = _originalConfig.OcrEditConfirm;
            DoubleClickAction = _originalConfig.DoubleClickAction.ToString();
            CardDefaultAction = _originalConfig.CardDefaultAction;
            CardActionTrigger = _originalConfig.CardActionTrigger;
            CardContentDensity = _originalConfig.CardContentDensity;

            // 操作防护与确认
            ConfirmOnDelete = _originalConfig.ConfirmOnDelete;
            ConfirmOnBatchDelete = _originalConfig.ConfirmOnBatchDelete;
            ConfirmOnRestore = _originalConfig.ConfirmOnRestore;
            EnableUndo = _originalConfig.EnableUndo;
            MaxUndoSteps = _originalConfig.MaxUndoSteps.ToString();

            _originalConfig = GetCurrentConfig();
        }
        finally
        {
            _isLoadingConfig = false; // 加载配置完成
        }
    }

    /// <summary>
    ///     从视图模型获取当前配置
    /// </summary>
    private GeneralConfig GetCurrentConfig()
    {
        return new GeneralConfig
        {
            // 外观与显示
            ThemeMode = IsLightTheme ? ThemeMode.Light : IsDarkTheme ? ThemeMode.Dark : ThemeMode.System,
            AccentColor = GetSelectedAccentColor(),
            CustomColor = CustomColor,
            FontSize = Enum.Parse<FontSizeOption>(FontSize),
            RowHeight = Enum.Parse<RowHeightOption>(RowHeight),

            // 程序启动与运行
            SingleInstance = SingleInstance,
            StartupPage = Enum.Parse<StartupPageOption>(StartupPage),
            LastPage = _originalConfig.LastPage, // 保留上次关闭的页面设置
            WindowState = IsNormalWindow ? WindowStateOption.Normal :
                IsMaximized ? WindowStateOption.Maximized : WindowStateOption.MinimizedToTray,
            AutoRefreshOnStartup = AutoRefreshOnStartup,
            // 注意：AutoBackupOnExit 已移至数据库设置页面统一管理

            // 窗口位置和大小（保留现有值）
            WindowLeft = _originalConfig.WindowLeft,
            WindowTop = _originalConfig.WindowTop,
            WindowWidth = _originalConfig.WindowWidth,
            WindowHeight = _originalConfig.WindowHeight,

            // 核心业务默认设置
            PageSize = ValidatePageSize(PageSize),
            DefaultSort = Enum.Parse<SortOption>(DefaultSort),
            LoadRange = Enum.Parse<LoadRangeOption>(LoadRange),
            DefaultSeatType = Enum.Parse<DefaultSeatTypeOption>(DefaultSeatType),
            DefaultTicketStatus = Enum.Parse<DefaultTicketStatusOption>(DefaultTicketStatus),
            OcrEditConfirm = OcrEditConfirm,
            DoubleClickAction = Enum.Parse<DoubleClickActionOption>(DoubleClickAction),
            CardDefaultAction = CardDefaultAction,
            CardActionTrigger = CardActionTrigger,
            CardContentDensity = CardContentDensity,

            // 操作防护与确认
            ConfirmOnDelete = ConfirmOnDelete,
            ConfirmOnBatchDelete = ConfirmOnBatchDelete,
            ConfirmOnRestore = ConfirmOnRestore,
            EnableUndo = EnableUndo,
            MaxUndoSteps = int.Parse(MaxUndoSteps)
        };
    }

    /// <summary>
    ///     验证每页显示数量（范围：5-100）
    /// </summary>
    private int ValidatePageSize(string pageSizeStr)
    {
        if (!int.TryParse(pageSizeStr, out var pageSize)) throw new ArgumentException("每页显示数量必须是有效的数字");

        if (pageSize < 5) throw new ArgumentException("每页显示数量不能小于 5");

        if (pageSize > 100) throw new ArgumentException("每页显示数量不能大于 100");

        return pageSize;
    }

    /// <summary>
    ///     获取选中的强调色
    /// </summary>
    private AccentColor GetSelectedAccentColor()
    {
        if (IsFreshGreen) return AccentColor.FreshGreen;
        if (IsVitalityOrange) return AccentColor.VitalityOrange;
        if (IsDarkPurple) return AccentColor.DarkPurple;
        if (IsMinimalGray) return AccentColor.MinimalGray;
        if (IsCustomColor) return AccentColor.Custom;
        return AccentColor.MicrosoftBlue;
    }

    /// <summary>
    ///     比较两个配置是否相等（排除自动管理的属性）
    /// </summary>
    private bool ConfigsEqual(GeneralConfig a, GeneralConfig b)
    {
        return a.ThemeMode == b.ThemeMode &&
               a.AccentColor == b.AccentColor &&
               a.CustomColor == b.CustomColor &&
               a.FontSize == b.FontSize &&
               a.RowHeight == b.RowHeight &&
               a.SingleInstance == b.SingleInstance &&
               a.StartupPage == b.StartupPage &&
               // 注意：LastPage 由程序自动管理（记录上次关闭的页面）
               // WindowLeft/WindowTop/WindowWidth/WindowHeight 由程序自动管理
               // 不在设置页面中比较，避免误判为未保存
               a.WindowState == b.WindowState &&
               a.AutoRefreshOnStartup == b.AutoRefreshOnStartup &&
               a.PageSize == b.PageSize &&
               a.DefaultSort == b.DefaultSort &&
               a.LoadRange == b.LoadRange &&
               a.DefaultSeatType == b.DefaultSeatType &&
               a.DefaultTicketStatus == b.DefaultTicketStatus &&
               a.OcrEditConfirm == b.OcrEditConfirm &&
               a.DoubleClickAction == b.DoubleClickAction &&
               a.CardDefaultAction == b.CardDefaultAction &&
               a.CardActionTrigger == b.CardActionTrigger &&
               a.CardContentDensity == b.CardContentDensity &&
               a.ConfirmOnDelete == b.ConfirmOnDelete &&
               a.ConfirmOnBatchDelete == b.ConfirmOnBatchDelete &&
               a.ConfirmOnRestore == b.ConfirmOnRestore &&
               a.EnableUndo == b.EnableUndo &&
               a.MaxUndoSteps == b.MaxUndoSteps;
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
    ///     保存设置内部实现
    /// </summary>
    private async Task SaveSettingsInternalAsync(bool showMessage)
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        // 校验每页显示数量
        if (!int.TryParse(PageSize, out var pageSize) || pageSize < 5 || pageSize > 200)
        {
            MessageBoxWindow.Show(settingsWindow, "每页显示数量必须在5-200之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 校验最大撤销步数
        if (!int.TryParse(MaxUndoSteps, out var maxUndoSteps) || maxUndoSteps < 0 || maxUndoSteps > 50)
        {
            MessageBoxWindow.Show(settingsWindow, "最大撤销步数必须在0-50之间", "校验失败", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var config = GetCurrentConfig();
            var wasMultiInstance = !_originalConfig.SingleInstance && config.SingleInstance;
            var previousConfig = _originalConfig;
            _settingsService.SaveConfig(config);
            _originalConfig = GetCurrentConfig();

            if (wasMultiInstance) WindowManager.EnforceSingleInstance();

            // 应用主题设置（字体大小、强调色、行高等）
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ThemeManager.ApplyTheme(config);
                    // 刷新所有打开窗口的主题
                    ThemeManager.RefreshAllWindows();
                });
            });

            // 发送设置变更消息，通知主界面刷新数据
            WeakReferenceMessenger.Default.Send(new SettingsChangedMessage("General"));

            // 通知所有表单窗口刷新撤销重做设置
            Debug.WriteLine("[GeneralSettingsViewModel] 发送 UndoRedoSettingsChangedMessage");
            WeakReferenceMessenger.Default.Send(new UndoRedoSettingsChangedMessage());

            if (showMessage)
            {
                var restartHints = new List<string>();
                if (config.StartupPage != previousConfig.StartupPage)
                    restartHints.Add("• 启动页面");
                if (config.WindowState != previousConfig.WindowState)
                    restartHints.Add("• 窗口启动状态");
                if (previousConfig.SingleInstance && !config.SingleInstance)
                    restartHints.Add("• 关闭单实例模式");
                if (config.AutoRefreshOnStartup != previousConfig.AutoRefreshOnStartup)
                    restartHints.Add("• 启动时自动刷新");

                var message = "常规设置已保存";
                if (restartHints.Count > 0)
                    message += $"\n\n以下设置需重启程序后生效：\n{string.Join("\n", restartHints)}";

                MessageBoxWindow.Show(settingsWindow, message, SettingsDialogMessages.SuccessTitle);
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(settingsWindow, $"{SettingsDialogMessages.SaveFailedPrefix}{ex.Message}",
                SettingsDialogMessages.ErrorTitle, MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     打开颜色选择器对话框命令
    /// </summary>
    [RelayCommand]
    private void OpenColorPicker()
    {
        // 获取设置窗口作为父窗口
        var settingsWindow = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext is SettingsViewModel);

        // 点击颜色图标时打开颜色选择器
        // 不需要检查 IsCustomColor，允许随时打开
        try
        {
            // 解析当前颜色
            var currentColor = Colors.Blue;
            try
            {
                currentColor = (Color)ColorConverter.ConvertFromString(CustomColor);
            }
            catch
            {
            }

            // 打开颜色选择对话框
            var dialog = new ColorPickerDialog(currentColor);
            // 不设置 Owner，避免最小化时影响主窗口

            if (dialog.ShowDialog() == true && dialog.SelectedColor.HasValue)
            {
                // 更新自定义颜色
                var color = dialog.SelectedColor.Value;
                CustomColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                OnPropertyChanged(nameof(CustomColorBrush));
            }
        }
        catch (Exception ex)
        {
            var message = $"打开颜色选择器失败：{ex.Message}";
            if (string.IsNullOrEmpty(ex.Message)) message = $"打开颜色选择器失败：{ex.GetType().FullName}";
            if (ex.InnerException != null) message += $"\n内部异常：{ex.InnerException.Message}";
            message += $"\n\n堆栈跟踪：{ex.StackTrace}";
            MessageBoxWindow.Show(settingsWindow, message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     当 IsCustomColor 属性变更时触发
    /// </summary>
    partial void OnIsCustomColorChanged(bool value)
    {
        // 只切换选中状态，不自动打开颜色选择器
        // 颜色选择器由用户点击颜色方块图标时手动打开
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
            SettingsDialogMessages.ConfirmTitle, MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        var defaultConfig = _settingsService.GetDefaultConfig();

        // 外观与显示
        IsLightTheme = defaultConfig.ThemeMode == ThemeMode.Light;
        IsDarkTheme = defaultConfig.ThemeMode == ThemeMode.Dark;
        IsSystemTheme = defaultConfig.ThemeMode == ThemeMode.System;

        IsMicrosoftBlue = defaultConfig.AccentColor == AccentColor.MicrosoftBlue;
        IsFreshGreen = defaultConfig.AccentColor == AccentColor.FreshGreen;
        IsVitalityOrange = defaultConfig.AccentColor == AccentColor.VitalityOrange;
        IsDarkPurple = defaultConfig.AccentColor == AccentColor.DarkPurple;
        IsMinimalGray = defaultConfig.AccentColor == AccentColor.MinimalGray;
        IsCustomColor = defaultConfig.AccentColor == AccentColor.Custom;
        CustomColor = defaultConfig.CustomColor;

        FontSize = defaultConfig.FontSize.ToString();
        RowHeight = defaultConfig.RowHeight.ToString();

        // 程序启动与运行
        SingleInstance = defaultConfig.SingleInstance;
        StartupPage = defaultConfig.StartupPage.ToString();

        IsNormalWindow = defaultConfig.WindowState == WindowStateOption.Normal;
        IsMaximized = defaultConfig.WindowState == WindowStateOption.Maximized;
        IsMinimizedToTray = defaultConfig.WindowState == WindowStateOption.MinimizedToTray;

        AutoRefreshOnStartup = defaultConfig.AutoRefreshOnStartup;
        // 注意：AutoBackupOnExit 已移至数据库设置页面统一管理

        // 核心业务默认设置
        PageSize = defaultConfig.PageSize.ToString();
        DefaultSort = defaultConfig.DefaultSort.ToString();
        LoadRange = defaultConfig.LoadRange.ToString();
        DefaultSeatType = defaultConfig.DefaultSeatType.ToString();
        DefaultTicketStatus = defaultConfig.DefaultTicketStatus.ToString();
        OcrDirectSave = !defaultConfig.OcrEditConfirm;
        OcrEditConfirm = defaultConfig.OcrEditConfirm;
        DoubleClickAction = defaultConfig.DoubleClickAction.ToString();

        // 卡片视图默认规则
        CardDefaultAction = defaultConfig.CardDefaultAction;
        CardActionTrigger = defaultConfig.CardActionTrigger;
        CardContentDensity = defaultConfig.CardContentDensity;

        // 操作防护与确认
        ConfirmOnDelete = defaultConfig.ConfirmOnDelete;
        ConfirmOnBatchDelete = defaultConfig.ConfirmOnBatchDelete;
        ConfirmOnRestore = defaultConfig.ConfirmOnRestore;
        EnableUndo = defaultConfig.EnableUndo;
        MaxUndoSteps = defaultConfig.MaxUndoSteps.ToString();

        // 立即应用主题设置到当前窗口
        await Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ThemeManager.ApplyTheme(defaultConfig);
                ThemeManager.RefreshAllWindows();
            });
        });

        MessageBoxWindow.Show(settingsWindow, SettingsDialogMessages.RestoreNeedSaveHint);
    }

    #region 外观与显示

    [ObservableProperty] private bool _isLightTheme = true;

    [ObservableProperty] private bool _isDarkTheme;

    [ObservableProperty] private bool _isSystemTheme;

    [ObservableProperty] private bool _isMicrosoftBlue = true;

    [ObservableProperty] private bool _isFreshGreen;

    [ObservableProperty] private bool _isVitalityOrange;

    [ObservableProperty] private bool _isDarkPurple;

    [ObservableProperty] private bool _isMinimalGray;

    [ObservableProperty] private bool _isCustomColor;

    [ObservableProperty] private string _customColor = "#0078D4";

    /// <summary>
    ///     自定义颜色画刷（用于预览）
    /// </summary>
    public Brush CustomColorBrush
    {
        get
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(CustomColor);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
    }

    [ObservableProperty] private string _fontSize = "Medium";

    [ObservableProperty] private string _rowHeight = "Standard";

    #endregion

    #region 程序启动与运行

    [ObservableProperty] private bool _singleInstance = true;

    [ObservableProperty] private string _startupPage = "MainList";

    [ObservableProperty] private bool _isNormalWindow;

    [ObservableProperty] private bool _isMaximized = true;

    [ObservableProperty] private bool _isMinimizedToTray;

    [ObservableProperty] private bool _autoRefreshOnStartup = true;

    // 注意：AutoBackupOnExit 已移至数据库设置页面统一管理

    #endregion

    #region 核心业务默认设置

    [ObservableProperty] private string _pageSize = "20";

    [ObservableProperty] private string _defaultSort = "DateDesc";

    [ObservableProperty] private string _loadRange = "ThisYear";

    [ObservableProperty] private string _defaultSeatType = "SecondClass";

    [ObservableProperty] private string _defaultTicketStatus = "Completed";

    [ObservableProperty] private bool _ocrDirectSave;

    [ObservableProperty] private bool _ocrEditConfirm = true;

    [ObservableProperty] private string _doubleClickAction = "Edit";

    [ObservableProperty] private string _cardDefaultAction = "View";

    [ObservableProperty] private string _cardActionTrigger = "DoubleClick";

    [ObservableProperty] private string _cardContentDensity = "Standard";

    public bool IsCardDefaultActionVisible => CardActionTrigger == "DoubleClick";

    partial void OnCardActionTriggerChanged(string value)
    {
        OnPropertyChanged(nameof(IsCardDefaultActionVisible));
    }

    #endregion

    #region 操作防护与确认

    [ObservableProperty] private bool _confirmOnDelete = true;

    [ObservableProperty] private bool _confirmOnBatchDelete = true;

    [ObservableProperty] private bool _confirmOnRestore = true;

    [ObservableProperty] private bool _enableUndo = true;

    [ObservableProperty] private string _maxUndoSteps = "5";

    #endregion
}