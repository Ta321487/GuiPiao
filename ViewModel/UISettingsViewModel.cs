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
using GuiPiao.Views;
using TripItem = GuiPiao.Model.TripItem;

namespace GuiPiao.ViewModel;

/// <summary>
///     界面设置视图模型
/// </summary>
public partial class UISettingsViewModel : ObservableObject, ISettingsViewModel
{
    private readonly UISettingsService _settingsService;

    #region 高级界面选项

    [ObservableProperty] private string _dpiScaling = "System";

    #endregion

    private bool _isLoadingConfig;
    private UISettingsConfig _originalConfig;

    public UISettingsViewModel()
    {
        // 检查是否在设计时
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject())) return;

        _settingsService = new UISettingsService();
        LoadSettings();
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
        LoadSettings();
    }

    /// <summary>
    ///     保存设置（接口实现）
    /// </summary>
    async Task ISettingsViewModel.SaveSettingsAsync(bool showMessage)
    {
        await SaveSettingsInternalAsync(showMessage);
    }

    /// <summary>
    ///     从十六进制字符串创建画刷
    /// </summary>
    private Brush CreateBrushFromHex(string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    /// <summary>
    ///     加载设置
    /// </summary>
    private void LoadSettings()
    {
        _isLoadingConfig = true;
        try
        {
            _originalConfig = _settingsService.Config;

            // 票面预览显示设置
            DefaultZoom = _originalConfig.DefaultZoom;
            AllowMouseWheelZoom = _originalConfig.AllowMouseWheelZoom;
            DisplayBrightness = _originalConfig.DisplayBrightness;
            TicketCentered = _originalConfig.TicketCentered;

            // 主界面布局配置 - 从配置文件加载（手动拖动后已自动保存）
            LeftPanelWidth = _originalConfig.LeftPanelWidth;
            LeftPanelLocked = _originalConfig.LeftPanelLocked;
            RightPanelWidth = _originalConfig.RightPanelWidth;
            RightPanelLocked = _originalConfig.RightPanelLocked;
            BottomPanelHeight = _originalConfig.BottomPanelHeight;
            BottomPanelLocked = _originalConfig.BottomPanelLocked;

            // 行程列表显示设置
            DefaultGroup = _originalConfig.DefaultGroup;
            RememberColumnOrder = _originalConfig.RememberColumnOrder;
            RememberDataSort = _originalConfig.RememberDataSort;
            ScrollbarStyle = _originalConfig.ScrollbarStyle;
            ShowActionButtonsOnHover = _originalConfig.ShowActionButtonsOnHover;
            ShowViewButton = _originalConfig.ShowViewButton;
            ShowEditButton = _originalConfig.ShowEditButton;
            ShowRescheduleButton = _originalConfig.ShowRescheduleButton;
            ShowRefundButton = _originalConfig.ShowRefundButton;
            ShowDeleteButton = _originalConfig.ShowDeleteButton;
            IsTripListExpandedByDefault = _originalConfig.IsTripListExpandedByDefault;
            DataGridColumns = _originalConfig.DataGridColumns ?? DataGridColumnConfig.GetDefaultColumns();
            CurrentTripListView = _originalConfig.DefaultTripListView;

            // 卡片视图显示设置
            CardsPerRow = _originalConfig.CardsPerRow;
            CardWidth = _originalConfig.CardWidth;
            CardSpacing = _originalConfig.CardSpacing;
            CardCornerRadius = _originalConfig.CardCornerRadius;
            CardShowViewAction = _originalConfig.CardShowViewAction;
            CardShowEditAction = _originalConfig.CardShowEditAction;
            CardShowRescheduleAction = _originalConfig.CardShowRescheduleAction;
            CardShowRefundAction = _originalConfig.CardShowRefundAction;
            CardShowDeleteAction = _originalConfig.CardShowDeleteAction;
            CardEnableMultiSelect = _originalConfig.CardEnableMultiSelect;
            CardBatchShowView = _originalConfig.CardBatchShowView;
            CardBatchShowEdit = _originalConfig.CardBatchShowEdit;
            CardBatchShowReschedule = _originalConfig.CardBatchShowReschedule;
            CardBatchShowRefund = _originalConfig.CardBatchShowRefund;
            CardBatchShowDelete = _originalConfig.CardBatchShowDelete;
            CardStatusPosition = _originalConfig.CardStatusPosition;
            CardHoverHighlight = _originalConfig.CardHoverHighlight;
            CardShowShadow = _originalConfig.CardShowShadow;
            CardHoverScale = _originalConfig.CardHoverScale;

            // 日志面板显示设置
            LogRowHeight = _originalConfig.LogRowHeight;
            InfoColor = _originalConfig.InfoColor;
            WarningColor = _originalConfig.WarningColor;
            ErrorColor = _originalConfig.ErrorColor;
            FatalColor = _originalConfig.FatalColor;
            ShowTimestamp = _originalConfig.ShowTimestamp;
            ShowModuleSource = _originalConfig.ShowModuleSource;

            // 高级界面选项
            DpiScaling = _originalConfig.DpiScaling;

            _originalConfig = GetCurrentConfig();
        }
        finally
        {
            _isLoadingConfig = false;
        }
    }

    /// <summary>
    ///     从视图模型获取当前配置
    /// </summary>
    private UISettingsConfig GetCurrentConfig()
    {
        return new UISettingsConfig
        {
            // 票面预览显示设置
            DefaultZoom = DefaultZoom,
            AllowMouseWheelZoom = AllowMouseWheelZoom,
            DisplayBrightness = DisplayBrightness,
            TicketCentered = TicketCentered,

            // 主界面布局配置
            LeftPanelWidth = LeftPanelWidth,
            LeftPanelLocked = LeftPanelLocked,
            RightPanelWidth = RightPanelWidth,
            RightPanelLocked = RightPanelLocked,
            BottomPanelHeight = BottomPanelHeight,
            BottomPanelLocked = BottomPanelLocked,

            // 行程列表显示设置
            DefaultGroup = DefaultGroup,
            RememberColumnOrder = RememberColumnOrder,
            RememberDataSort = RememberDataSort,
            ScrollbarStyle = ScrollbarStyle,
            ShowActionButtonsOnHover = ShowActionButtonsOnHover,
            ShowViewButton = ShowViewButton,
            ShowEditButton = ShowEditButton,
            ShowRescheduleButton = ShowRescheduleButton,
            ShowRefundButton = ShowRefundButton,
            ShowDeleteButton = ShowDeleteButton,
            IsTripListExpandedByDefault = IsTripListExpandedByDefault,
            DataGridColumns = DataGridColumns?.GroupBy(c => c.FieldName).Select(g => g.First()).ToList() ??
                              DataGridColumnConfig.GetDefaultColumns(),
            DefaultTripListView = CurrentTripListView,

            // 卡片视图显示设置
            CardsPerRow = CardsPerRow,
            CardWidth = CardWidth,
            CardSpacing = CardSpacing,
            CardCornerRadius = CardCornerRadius,
            CardShowViewAction = CardShowViewAction,
            CardShowEditAction = CardShowEditAction,
            CardShowRescheduleAction = CardShowRescheduleAction,
            CardShowRefundAction = CardShowRefundAction,
            CardShowDeleteAction = CardShowDeleteAction,
            CardEnableMultiSelect = CardEnableMultiSelect,
            CardBatchShowView = CardBatchShowView,
            CardBatchShowEdit = CardBatchShowEdit,
            CardBatchShowReschedule = CardBatchShowReschedule,
            CardBatchShowRefund = CardBatchShowRefund,
            CardBatchShowDelete = CardBatchShowDelete,
            CardStatusPosition = CardStatusPosition,
            CardHoverHighlight = CardHoverHighlight,
            CardShowShadow = CardShowShadow,
            CardHoverScale = CardHoverScale,

            // 日志面板显示设置
            LogRowHeight = LogRowHeight,
            InfoColor = InfoColor,
            WarningColor = WarningColor,
            ErrorColor = ErrorColor,
            FatalColor = FatalColor,
            ShowTimestamp = ShowTimestamp,
            ShowModuleSource = ShowModuleSource,

            // 高级界面选项
            DpiScaling = DpiScaling
        };
    }

    /// <summary>
    ///     比较两个配置是否相等
    /// </summary>
    private bool ConfigsEqual(UISettingsConfig a, UISettingsConfig b)
    {
        return a.DefaultZoom == b.DefaultZoom &&
               a.AllowMouseWheelZoom == b.AllowMouseWheelZoom &&
               a.DisplayBrightness == b.DisplayBrightness &&
               a.TicketCentered == b.TicketCentered &&
               a.LeftPanelWidth == b.LeftPanelWidth &&
               a.LeftPanelLocked == b.LeftPanelLocked &&
               a.RightPanelWidth == b.RightPanelWidth &&
               a.RightPanelLocked == b.RightPanelLocked &&
               a.BottomPanelHeight == b.BottomPanelHeight &&
               a.BottomPanelLocked == b.BottomPanelLocked &&
               a.DefaultGroup == b.DefaultGroup &&
               a.RememberColumnOrder == b.RememberColumnOrder &&
               a.RememberDataSort == b.RememberDataSort &&
               a.ScrollbarStyle == b.ScrollbarStyle &&
               a.ShowActionButtonsOnHover == b.ShowActionButtonsOnHover &&
               a.ShowViewButton == b.ShowViewButton &&
               a.ShowEditButton == b.ShowEditButton &&
               a.ShowRescheduleButton == b.ShowRescheduleButton &&
               a.ShowRefundButton == b.ShowRefundButton &&
               a.ShowDeleteButton == b.ShowDeleteButton &&
               a.IsTripListExpandedByDefault == b.IsTripListExpandedByDefault &&
               a.DefaultTripListView == b.DefaultTripListView &&
               a.CardsPerRow == b.CardsPerRow &&
               a.CardWidth == b.CardWidth &&
               a.CardSpacing == b.CardSpacing &&
               a.CardCornerRadius == b.CardCornerRadius &&
               a.CardShowViewAction == b.CardShowViewAction &&
               a.CardShowEditAction == b.CardShowEditAction &&
               a.CardShowRescheduleAction == b.CardShowRescheduleAction &&
               a.CardShowRefundAction == b.CardShowRefundAction &&
               a.CardShowDeleteAction == b.CardShowDeleteAction &&
               a.CardEnableMultiSelect == b.CardEnableMultiSelect &&
               a.CardBatchShowView == b.CardBatchShowView &&
               a.CardBatchShowEdit == b.CardBatchShowEdit &&
               a.CardBatchShowReschedule == b.CardBatchShowReschedule &&
               a.CardBatchShowRefund == b.CardBatchShowRefund &&
               a.CardBatchShowDelete == b.CardBatchShowDelete &&
               a.CardStatusPosition == b.CardStatusPosition &&
               a.CardHoverHighlight == b.CardHoverHighlight &&
               a.CardShowShadow == b.CardShowShadow &&
               a.CardHoverScale == b.CardHoverScale &&
               a.LogRowHeight == b.LogRowHeight &&
               a.InfoColor == b.InfoColor &&
               a.WarningColor == b.WarningColor &&
               a.ErrorColor == b.ErrorColor &&
               a.FatalColor == b.FatalColor &&
               a.ShowTimestamp == b.ShowTimestamp &&
               a.ShowModuleSource == b.ShowModuleSource &&
               a.DpiScaling == b.DpiScaling &&
               DataGridColumnsEqual(a.DataGridColumns, b.DataGridColumns);
    }

    private bool DataGridColumnsEqual(List<DataGridColumnConfig> a, List<DataGridColumnConfig> b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (a[i].FieldName != b[i].FieldName ||
                a[i].IsVisible != b[i].IsVisible ||
                a[i].DisplayOrder != b[i].DisplayOrder ||
                a[i].Width != b[i].Width)
                return false;
        }
        return true;
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

        try
        {
            Debug.WriteLine($"[SaveSettingsInternal] START - DefaultGroup property: {DefaultGroup}");
            var config = GetCurrentConfig();
            Debug.WriteLine($"[SaveSettingsInternal] Config created - DefaultGroup: {config.DefaultGroup}");
            _settingsService.SaveConfig(config);
            Debug.WriteLine($"[SaveSettingsInternal] SAVED - DefaultGroup: {config.DefaultGroup}");
            _originalConfig = GetCurrentConfig();

            // 发送布局变更消息，直接传递当前布局值，让主界面立即应用
            WeakReferenceMessenger.Default.Send(new LayoutChangedMessage(
                LeftPanelWidth,
                LeftPanelLocked,
                RightPanelWidth,
                RightPanelLocked,
                BottomPanelHeight,
                BottomPanelLocked));

            // 发送列配置变更消息，让主窗口更新列显示
            WeakReferenceMessenger.Default.Send(new DataGridColumnsChangedMessage(DataGridColumns));

            // 发送分组设置变更消息，让主窗口重新加载数据应用分组
            WeakReferenceMessenger.Default.Send(new GroupSettingChangedMessage(DefaultGroup));

            // 发送切换视图消息，让主窗口立即切换列表/卡片视图
            WeakReferenceMessenger.Default.Send(new SwitchViewMessage(CurrentTripListView));

            // 发送UI设置变更消息，让主窗口应用滚动条样式、操作按钮设置和日志面板显示设置
            WeakReferenceMessenger.Default.Send(new UISettingsChangedMessage(
                ScrollbarStyle,
                ShowActionButtonsOnHover,
                ShowViewButton,
                ShowEditButton,
                ShowRescheduleButton,
                ShowRefundButton,
                ShowDeleteButton,
                IsTripListExpandedByDefault,
                ShowTimestamp,
                ShowModuleSource,
                LogRowHeight));

            // 发送卡片视图设置变更消息
            WeakReferenceMessenger.Default.Send(new CardViewSettingsChangedMessage(
                CardsPerRow,
                CardWidth,
                CardSpacing,
                CardCornerRadius,
                CardShowViewAction,
                CardShowEditAction,
                CardShowRescheduleAction,
                CardShowRefundAction,
                CardShowDeleteAction,
                CardEnableMultiSelect,
                CardBatchShowView,
                CardBatchShowEdit,
                CardBatchShowReschedule,
                CardBatchShowRefund,
                CardBatchShowDelete,
                CardStatusPosition,
                CardHoverHighlight,
                CardShowShadow,
                CardHoverScale));

            // 发送日志颜色变更消息，通知所有使用日志的地方刷新显示
            WeakReferenceMessenger.Default.Send(new LogColorsChangedMessage(InfoColor, WarningColor, ErrorColor,
                FatalColor));

            // 刷新ConfigManager中的UI设置配置，确保日志颜色等设置立即生效
            ConfigManager.Instance.RefreshUISettingsConfig();

            // 应用DPI缩放设置
            ThemeManager.ApplyDpiScaling(DpiScaling);

            if (showMessage)
                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxWindow.Show(settingsWindow, "界面设置已保存", "成功");
                    });
                });
        }
        catch (Exception ex)
        {
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxWindow.Show(settingsWindow, $"保存失败：{ex.Message}", "错误", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            });
        }
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

        var result = await Task.Run(() =>
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                return MessageBoxWindow.Show(settingsWindow, "确定要恢复默认设置吗？", "确认", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });
        });

        if (result != MessageBoxResult.Yes)
            return;

        var defaultConfig = _settingsService.GetDefaultConfig();

        // 票面预览显示设置
        DefaultZoom = defaultConfig.DefaultZoom;
        AllowMouseWheelZoom = defaultConfig.AllowMouseWheelZoom;
        DisplayBrightness = defaultConfig.DisplayBrightness;
        TicketCentered = defaultConfig.TicketCentered;

        // 主界面布局配置
        LeftPanelWidth = defaultConfig.LeftPanelWidth;
        LeftPanelLocked = defaultConfig.LeftPanelLocked;
        RightPanelWidth = defaultConfig.RightPanelWidth;
        RightPanelLocked = defaultConfig.RightPanelLocked;
        BottomPanelHeight = defaultConfig.BottomPanelHeight;
        BottomPanelLocked = defaultConfig.BottomPanelLocked;

        // 行程列表显示设置
        DefaultGroup = defaultConfig.DefaultGroup;
        RememberColumnOrder = defaultConfig.RememberColumnOrder;
        RememberDataSort = defaultConfig.RememberDataSort;
        ScrollbarStyle = defaultConfig.ScrollbarStyle;
        ShowActionButtonsOnHover = defaultConfig.ShowActionButtonsOnHover;
        ShowViewButton = defaultConfig.ShowViewButton;
        ShowEditButton = defaultConfig.ShowEditButton;
        ShowRescheduleButton = defaultConfig.ShowRescheduleButton;
        ShowRefundButton = defaultConfig.ShowRefundButton;
        ShowDeleteButton = defaultConfig.ShowDeleteButton;
        IsTripListExpandedByDefault = defaultConfig.IsTripListExpandedByDefault;

        // 日志面板显示设置
        LogRowHeight = defaultConfig.LogRowHeight;
        InfoColor = defaultConfig.InfoColor;
        WarningColor = defaultConfig.WarningColor;
        ErrorColor = defaultConfig.ErrorColor;
        FatalColor = defaultConfig.FatalColor;
        ShowTimestamp = defaultConfig.ShowTimestamp;
        ShowModuleSource = defaultConfig.ShowModuleSource;

        // 高级界面选项
        DpiScaling = defaultConfig.DpiScaling;

        // 通知属性变更
        OnPropertyChanged(nameof(InfoColorBrush));
        OnPropertyChanged(nameof(WarningColorBrush));
        OnPropertyChanged(nameof(ErrorColorBrush));
        OnPropertyChanged(nameof(FatalColorBrush));

        await Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBoxWindow.Show(settingsWindow, "已恢复默认设置，请点击保存设置按钮保存更改。");
            });
        });
    }

    /// <summary>
    ///     保存当前布局命令
    /// </summary>
    [RelayCommand]
    private void SaveLayout()
    {
        try
        {
            // 只保存布局相关的配置
            var config = GetCurrentConfig();
            _settingsService.SaveConfig(config);

            // 发送布局变更消息，直接传递当前布局值，让主界面立即应用
            WeakReferenceMessenger.Default.Send(new LayoutChangedMessage(
                LeftPanelWidth,
                LeftPanelLocked,
                RightPanelWidth,
                RightPanelLocked,
                BottomPanelHeight,
                BottomPanelLocked));

            MessageBoxWindow.Show(Application.Current.MainWindow, "当前布局已保存", "成功");
        }
        catch (Exception ex)
        {
            // 获取设置窗口作为父窗口
            var settingsWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext is SettingsViewModel);
            MessageBoxWindow.Show(settingsWindow, $"保存失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     重置布局默认命令
    /// </summary>
    [RelayCommand]
    private void ResetLayout()
    {
        var defaultConfig = _settingsService.GetDefaultConfig();

        LeftPanelWidth = defaultConfig.LeftPanelWidth;
        RightPanelWidth = defaultConfig.RightPanelWidth;
        BottomPanelHeight = defaultConfig.BottomPanelHeight;
        LeftPanelLocked = defaultConfig.LeftPanelLocked;
        RightPanelLocked = defaultConfig.RightPanelLocked;
        BottomPanelLocked = defaultConfig.BottomPanelLocked;

        MessageBoxWindow.Show(Application.Current.MainWindow, "布局已重置为默认值，请点击保存当前布局按钮保存更改。");
    }

    /// <summary>
    ///     <summary>
    ///         自定义列表字段命令
    ///     </summary>
    [RelayCommand]
    private void CustomizeFields()
    {
        // 打开自定义字段对话框
        var dialog = new ColumnCustomizationDialog(DataGridColumns);
        // 不设置 Owner，避免最小化时影响主窗口
        if (dialog.ShowDialog() == true)
        {
            // 只更新属性，不发送消息，等待保存设置时统一应用
            DataGridColumns = dialog.ColumnConfigs;
            // 标记设置已更改，启用保存按钮
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }
    }

    /// <summary>
    ///     重置列表默认命令
    /// </summary>
    [RelayCommand]
    private void ResetListDefaults()
    {
        var defaultConfig = _settingsService.GetDefaultConfig();

        DefaultGroup = defaultConfig.DefaultGroup;
        RememberColumnOrder = defaultConfig.RememberColumnOrder;
        RememberDataSort = defaultConfig.RememberDataSort;
        ScrollbarStyle = defaultConfig.ScrollbarStyle;
        ShowActionButtonsOnHover = defaultConfig.ShowActionButtonsOnHover;
        DataGridColumns = DataGridColumnConfig.GetDefaultColumns();

        // 标记设置已更改，启用保存按钮
        OnPropertyChanged(nameof(HasUnsavedChanges));

        MessageBoxWindow.Show(Application.Current.MainWindow, "列表设置已重置为默认值，请点击保存设置以应用");
    }

    /// <summary>
    ///     重置所有界面设置命令
    /// </summary>
    [RelayCommand]
    private void ResetAllUISettings()
    {
        var result = MessageBoxWindow.Show(Application.Current.MainWindow, "确定要重置所有界面设置为默认吗？此操作不可恢复。", "确认",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        RestoreDefaults();
    }

    /// <summary>
    ///     测试票面预览命令
    /// </summary>
    [RelayCommand]
    private void TestTicketPreview()
    {
        try
        {
            // 先保存当前设置
            var config = GetCurrentConfig();
            _settingsService.SaveConfig(config);
            _originalConfig = GetCurrentConfig();

            // 打开预览窗口
            var testTrip = new TripItem
            {
                TrainNo = "G1234",
                DepartStation = "北京南",
                ArriveStation = "上海虹桥",
                DepartDate = DateTime.Now.ToString("yyyy-MM-dd"),
                DepartTime = "08:00",
                SeatType = "二等座",
                Money = "553.5",
                Status = 0
            };

            var previewWindow = new TicketPreviewWindow(testTrip);
            // 不设置 Owner，避免最小化时影响主窗口
            previewWindow.ShowDialog();

            // 预览窗口关闭后，同步设置回来
            if (previewWindow.DataContext is TicketPreviewViewModel previewVM)
            {
                var newZoom = previewVM.GetCurrentZoomSetting();
                var newBrightness = previewVM.GetCurrentBrightnessSetting();

                // 更新设置值
                DefaultZoom = newZoom;
                DisplayBrightness = newBrightness;

                // 通知有未保存的修改（不自动保存，让用户点击保存按钮）
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(Application.Current.MainWindow, $"打开预览窗口失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     打开颜色选择器命令
    /// </summary>
    [RelayCommand]
    private void OpenColorPicker(string colorType)
    {
        try
        {
            var currentColor = colorType switch
            {
                "Info" => InfoColor,
                "Warning" => WarningColor,
                "Error" => ErrorColor,
                "Fatal" => FatalColor,
                _ => "#0078D4"
            };

            var color = Colors.Blue;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(currentColor);
            }
            catch
            {
            }

            // 日志颜色选择不启用主题预览，常规颜色选择启用主题预览
            var enableThemePreview = colorType switch
            {
                "Info" => false,
                "Warning" => false,
                "Error" => false,
                "Fatal" => false,
                _ => true
            };

            // 保存原始颜色（用于取消时恢复）
            var originalInfoColor = InfoColor;
            var originalWarningColor = WarningColor;
            var originalErrorColor = ErrorColor;
            var originalFatalColor = FatalColor;

            // 预览回调：临时更新颜色并刷新日志显示
            Action<string>? previewCallback = null;
            Action? cancelCallback = null;

            if (!enableThemePreview) // 日志颜色
            {
                previewCallback = hexColor =>
                {
                    // 临时更新ViewModel属性
                    switch (colorType)
                    {
                        case "Info":
                            InfoColor = hexColor;
                            OnPropertyChanged(nameof(InfoColorBrush));
                            break;
                        case "Warning":
                            WarningColor = hexColor;
                            OnPropertyChanged(nameof(WarningColorBrush));
                            break;
                        case "Error":
                            ErrorColor = hexColor;
                            OnPropertyChanged(nameof(ErrorColorBrush));
                            break;
                        case "Fatal":
                            FatalColor = hexColor;
                            OnPropertyChanged(nameof(FatalColorBrush));
                            break;
                    }

                    // 临时更新ConfigManager配置（用于日志预览）
                    var config = ConfigManager.Instance.UISettingsService.Config;
                    config.InfoColor = InfoColor;
                    config.WarningColor = WarningColor;
                    config.ErrorColor = ErrorColor;
                    config.FatalColor = FatalColor;
                    // 发送消息刷新日志显示（临时预览效果）
                    WeakReferenceMessenger.Default.Send(new LogColorsChangedMessage(InfoColor, WarningColor, ErrorColor,
                        FatalColor));
                };

                cancelCallback = () =>
                {
                    // 恢复原始颜色到ViewModel
                    InfoColor = originalInfoColor;
                    WarningColor = originalWarningColor;
                    ErrorColor = originalErrorColor;
                    FatalColor = originalFatalColor;
                    OnPropertyChanged(nameof(InfoColorBrush));
                    OnPropertyChanged(nameof(WarningColorBrush));
                    OnPropertyChanged(nameof(ErrorColorBrush));
                    OnPropertyChanged(nameof(FatalColorBrush));
                    // 恢复原始颜色到ConfigManager（用于日志预览恢复）
                    var config = ConfigManager.Instance.UISettingsService.Config;
                    config.InfoColor = originalInfoColor;
                    config.WarningColor = originalWarningColor;
                    config.ErrorColor = originalErrorColor;
                    config.FatalColor = originalFatalColor;
                    // 发送消息刷新日志显示（恢复原色）
                    WeakReferenceMessenger.Default.Send(new LogColorsChangedMessage(InfoColor, WarningColor, ErrorColor,
                        FatalColor));
                };
            }

            // 日志颜色显示预览按钮，点击预览可实时看到日志颜色变化
            // 配置的实际保存由 SaveSettings 方法统一处理
            var dialog = new ColorPickerDialog(color, enableThemePreview, previewCallback, cancelCallback,
                !enableThemePreview);
            // 不设置 Owner，避免最小化时影响主窗口

            if (dialog.ShowDialog() == true && dialog.SelectedColor.HasValue)
            {
                var selectedColor = dialog.SelectedColor.Value;
                var hexColor = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";

                switch (colorType)
                {
                    case "Info":
                        InfoColor = hexColor;
                        OnPropertyChanged(nameof(InfoColorBrush));
                        break;
                    case "Warning":
                        WarningColor = hexColor;
                        OnPropertyChanged(nameof(WarningColorBrush));
                        break;
                    case "Error":
                        ErrorColor = hexColor;
                        OnPropertyChanged(nameof(ErrorColorBrush));
                        break;
                    case "Fatal":
                        FatalColor = hexColor;
                        OnPropertyChanged(nameof(FatalColorBrush));
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show(Application.Current.MainWindow, $"打开颜色选择器失败：{ex.Message}", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #region 票面预览显示设置

    [ObservableProperty] private string _defaultZoom = "FitWindow";

    [ObservableProperty] private bool _allowMouseWheelZoom = true;

    [ObservableProperty] private int _displayBrightness = 100;

    [ObservableProperty] private bool _ticketCentered = true;

    #endregion

    #region 主界面布局配置

    [ObservableProperty] private int _leftPanelWidth = 180;

    [ObservableProperty] private bool _leftPanelLocked = true;

    [ObservableProperty] private int _rightPanelWidth = 220;

    [ObservableProperty] private bool _rightPanelLocked = true;

    [ObservableProperty] private int _bottomPanelHeight = 250;

    [ObservableProperty] private bool _bottomPanelLocked = true;

    #endregion

    #region 行程列表显示设置

    [ObservableProperty] private GroupOption _defaultGroup = GroupOption.None;

    [ObservableProperty] private bool _rememberColumnOrder = true;

    [ObservableProperty] private bool _rememberDataSort = true;

    [ObservableProperty] private string _scrollbarStyle = "Normal";

    [ObservableProperty] private bool _showActionButtonsOnHover = true;

    [ObservableProperty] private bool _showViewButton = true;

    [ObservableProperty] private bool _showEditButton = true;

    [ObservableProperty] private bool _showRescheduleButton = true;

    [ObservableProperty] private bool _showRefundButton = true;

    [ObservableProperty] private bool _showDeleteButton = true;

    [ObservableProperty] private bool _isTripListExpandedByDefault = true;

    /// <summary>
    ///     DataGrid列配置
    /// </summary>
    [ObservableProperty] private List<DataGridColumnConfig> _dataGridColumns = DataGridColumnConfig.GetDefaultColumns();

    /// <summary>
    ///     当前行程列表视图类型（切换视图用）
    /// </summary>
    [ObservableProperty] private ViewType _currentTripListView = ViewType.List;

    #endregion

    #region 卡片视图显示设置

    [ObservableProperty] private int _cardsPerRow = 0;

    [ObservableProperty] private int _cardWidth = 280;

    [ObservableProperty] private int _cardSpacing = 8;

    [ObservableProperty] private int _cardCornerRadius = 8;

    [ObservableProperty] private bool _cardShowViewAction = true;

    [ObservableProperty] private bool _cardShowEditAction = true;

    [ObservableProperty] private bool _cardShowRescheduleAction = true;

    [ObservableProperty] private bool _cardShowRefundAction = true;

    [ObservableProperty] private bool _cardShowDeleteAction = true;

    [ObservableProperty] private bool _cardEnableMultiSelect = true;

    [ObservableProperty] private bool _cardBatchShowView = true;

    [ObservableProperty] private bool _cardBatchShowEdit = true;

    [ObservableProperty] private bool _cardBatchShowReschedule = true;

    [ObservableProperty] private bool _cardBatchShowRefund = true;

    [ObservableProperty] private bool _cardBatchShowDelete = true;

    [ObservableProperty] private string _cardStatusPosition = "TopRight";

    [ObservableProperty] private bool _cardHoverHighlight = true;

    [ObservableProperty] private bool _cardShowShadow = true;

    [ObservableProperty] private bool _cardHoverScale = false;

    #endregion

    #region 日志面板显示设置

    [ObservableProperty] private string _logRowHeight = "Standard";

    [ObservableProperty] private string _infoColor = "#0078D4";

    [ObservableProperty] private string _warningColor = "#FD7E14";

    [ObservableProperty] private string _errorColor = "#DC3545";

    [ObservableProperty] private string _fatalColor = "#6F42C1";

    [ObservableProperty] private bool _showTimestamp = true;

    [ObservableProperty] private bool _showModuleSource = true;

    /// <summary>
    ///     信息颜色画刷
    /// </summary>
    public Brush InfoColorBrush => CreateBrushFromHex(InfoColor);

    /// <summary>
    ///     警告颜色画刷
    /// </summary>
    public Brush WarningColorBrush => CreateBrushFromHex(WarningColor);

    /// <summary>
    ///     错误颜色画刷
    /// </summary>
    public Brush ErrorColorBrush => CreateBrushFromHex(ErrorColor);

    /// <summary>
    ///     致命错误颜色画刷
    /// </summary>
    public Brush FatalColorBrush => CreateBrushFromHex(FatalColor);

    #endregion

    #region 卡片视图设置联动方法

    /// <summary>
    #endregion
}