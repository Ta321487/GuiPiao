using System;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Services;
using GuiPiao.View;

namespace GuiPiao.ViewModel;

public partial class LayoutViewModel : ObservableObject, IDisposable
{
    private int _bottomPanelHeight = 250;

    private bool _bottomPanelLocked = true;

    private bool _isDisposed;

    [ObservableProperty] private bool _isTripListExpandedByDefault = true;

    private bool _leftPanelLocked = true;
    private int _leftPanelWidth = 180;

    [ObservableProperty] private string _logRowHeight = "Standard";

    private bool _rightPanelLocked = true;

    private int _rightPanelWidth = 220;

    [ObservableProperty] private string _scrollbarStyle = "Normal";

    [ObservableProperty] private bool _showActionButtonsOnHover = true;

    [ObservableProperty] private bool _showDeleteButton = true;

    [ObservableProperty] private bool _showEditButton = true;

    [ObservableProperty] private bool _showModuleSource = true;

    [ObservableProperty] private bool _showRefundButton = true;

    [ObservableProperty] private bool _showRescheduleButton = true;

    [ObservableProperty] private bool _showTimestamp = true;

    [ObservableProperty] private bool _showViewButton = true;

    // 卡片视图设置
    [ObservableProperty] private int _cardsPerRow = 0;
    [ObservableProperty] private int _cardWidth = 280;
    [ObservableProperty] private int _cardSpacing = 8;
    [ObservableProperty] private int _cardCornerRadius = 8;
    [ObservableProperty] private string _cardContentDensity = "Standard";
    [ObservableProperty] private string _cardActionTrigger = "DoubleClick";
    [ObservableProperty] private string _cardDefaultAction = "View";
    [ObservableProperty] private bool _isCardActionRightClick = false;
    [ObservableProperty] private bool _cardShowViewAction = true;
    [ObservableProperty] private bool _cardShowEditAction = true;
    [ObservableProperty] private bool _cardShowRescheduleAction = true;
    [ObservableProperty] private bool _cardShowRefundAction = true;
    [ObservableProperty] private bool _cardShowDeleteAction = true;

    [ObservableProperty] private bool _cardEnableMultiSelect = true;

    // 卡片外观效果
    [ObservableProperty] private string _cardStatusPosition = "TopRight";
    [ObservableProperty] private bool _cardHoverHighlight = true;
    [ObservableProperty] private bool _cardShowShadow = true;
    [ObservableProperty] private bool _cardHoverScale = false;

    // 批量操作工具栏按钮显示
    [ObservableProperty] private bool _cardBatchShowView = true;
    [ObservableProperty] private bool _cardBatchShowEdit = true;
    [ObservableProperty] private bool _cardBatchShowReschedule = true;
    [ObservableProperty] private bool _cardBatchShowRefund = true;
    [ObservableProperty] private bool _cardBatchShowDelete = true;

    public LayoutViewModel()
    {
        UISettingsService = new UISettingsService();

        // 加载布局配置
        LoadLayoutSettings();

        // 订阅布局变更消息
        WeakReferenceMessenger.Default.Register<LayoutChangedMessage>(this, (recipient, message) =>
        {
            LeftPanelWidth = message.LeftPanelWidth;
            LeftPanelLocked = message.LeftPanelLocked;
            RightPanelWidth = message.RightPanelWidth;
            RightPanelLocked = message.RightPanelLocked;
            BottomPanelHeight = message.BottomPanelHeight;
            BottomPanelLocked = message.BottomPanelLocked;
        });

        // 订阅UI设置变更消息
        WeakReferenceMessenger.Default.Register<UISettingsChangedMessage>(this, (recipient, message) =>
        {
            ScrollbarStyle = message.ScrollbarStyle;
            ShowActionButtonsOnHover = message.ShowActionButtonsOnHover;
            ShowViewButton = message.ShowViewButton;
            ShowEditButton = message.ShowEditButton;
            ShowRescheduleButton = message.ShowRescheduleButton;
            ShowRefundButton = message.ShowRefundButton;
            ShowDeleteButton = message.ShowDeleteButton;
            IsTripListExpandedByDefault = message.IsTripListExpandedByDefault;
            ShowTimestamp = message.ShowTimestamp;
            ShowModuleSource = message.ShowModuleSource;
            LogRowHeight = message.LogRowHeight;

            ApplyScrollbarStyle();
        });

        // 订阅卡片视图设置变更消息
        WeakReferenceMessenger.Default.Register<CardViewSettingsChangedMessage>(this, (recipient, message) =>
        {
            CardsPerRow = message.CardsPerRow;
            CardWidth = message.CardWidth;
            CardSpacing = message.CardSpacing;
            CardCornerRadius = message.CardCornerRadius;
            CardShowViewAction = message.CardShowViewAction;
            CardShowEditAction = message.CardShowEditAction;
            CardShowRescheduleAction = message.CardShowRescheduleAction;
            CardShowRefundAction = message.CardShowRefundAction;
            CardShowDeleteAction = message.CardShowDeleteAction;
            CardEnableMultiSelect = message.CardEnableMultiSelect;
            CardBatchShowView = message.CardBatchShowView;
            CardBatchShowEdit = message.CardBatchShowEdit;
            CardBatchShowReschedule = message.CardBatchShowReschedule;
            CardBatchShowRefund = message.CardBatchShowRefund;
            CardBatchShowDelete = message.CardBatchShowDelete;
            CardStatusPosition = message.CardStatusPosition;
            CardHoverHighlight = message.CardHoverHighlight;
            CardShowShadow = message.CardShowShadow;
            CardHoverScale = message.CardHoverScale;

            OnPropertyChanged(nameof(CardsPerRow));
            OnPropertyChanged(nameof(CardMargin));
            OnPropertyChanged(nameof(CardContentDensity));
            OnPropertyChanged(nameof(CardStatusPosition));
            OnPropertyChanged(nameof(CardHoverHighlight));
            OnPropertyChanged(nameof(CardShowShadow));
            OnPropertyChanged(nameof(CardHoverScale));
            OnPropertyChanged(nameof(CardBatchShowView));
            OnPropertyChanged(nameof(CardBatchShowEdit));
            OnPropertyChanged(nameof(CardBatchShowReschedule));
            OnPropertyChanged(nameof(CardBatchShowRefund));
            OnPropertyChanged(nameof(CardBatchShowDelete));
        });

        // 订阅常规设置变更消息（刷新卡片默认规则）
        WeakReferenceMessenger.Default.Register<SettingsChangedMessage>(this, (recipient, message) =>
        {
            if (message.SettingType == "General")
            {
                var generalConfig = new GeneralSettingsService().Config;
                CardContentDensity = generalConfig.CardContentDensity;
                CardActionTrigger = generalConfig.CardActionTrigger;
                CardDefaultAction = generalConfig.CardDefaultAction;
                IsCardActionRightClick = generalConfig.CardActionTrigger == "RightClick";

                OnPropertyChanged(nameof(CardContentDensity));
                OnPropertyChanged(nameof(CardActionTrigger));
                OnPropertyChanged(nameof(CardDefaultAction));
                OnPropertyChanged(nameof(IsCardActionRightClick));
            }
        });
    }

    public const int LeftPanelMinWidth = 175;
    public const int LeftPanelMaxWidth = 300;
    public const int RightPanelMinWidth = 180;
    public const int RightPanelMaxWidth = 350;
    public const int BottomPanelMinHeight = 150;
    public const int BottomPanelMaxHeight = 400;

    public int LeftPanelWidth
    {
        get => _leftPanelWidth;
        set
        {
            var clamped = Math.Max(LeftPanelMinWidth, Math.Min(LeftPanelMaxWidth, value));
            if (_leftPanelWidth != clamped)
            {
                _leftPanelWidth = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LeftColumnWidth));
            }
        }
    }

    public bool LeftPanelLocked
    {
        get => _leftPanelLocked;
        set
        {
            if (_leftPanelLocked != value)
            {
                _leftPanelLocked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LeftSplitterVisible));
                OnPropertyChanged(nameof(LeftColumnWidth));
                OnPropertyChanged(nameof(LeftSplitterWidth));
            }
        }
    }

    public int RightPanelWidth
    {
        get => _rightPanelWidth;
        set
        {
            var clamped = Math.Max(RightPanelMinWidth, Math.Min(RightPanelMaxWidth, value));
            if (_rightPanelWidth != clamped)
            {
                _rightPanelWidth = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RightColumnWidth));
            }
        }
    }

    public bool RightPanelLocked
    {
        get => _rightPanelLocked;
        set
        {
            if (_rightPanelLocked != value)
            {
                _rightPanelLocked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RightSplitterVisible));
                OnPropertyChanged(nameof(RightColumnWidth));
                OnPropertyChanged(nameof(RightSplitterWidth));
            }
        }
    }

    public int BottomPanelHeight
    {
        get => _bottomPanelHeight;
        set
        {
            var clamped = Math.Max(BottomPanelMinHeight, Math.Min(BottomPanelMaxHeight, value));
            if (_bottomPanelHeight != clamped)
            {
                _bottomPanelHeight = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BottomRowHeight));
            }
        }
    }

    public bool BottomPanelLocked
    {
        get => _bottomPanelLocked;
        set
        {
            if (_bottomPanelLocked != value)
            {
                _bottomPanelLocked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BottomSplitterVisible));
                OnPropertyChanged(nameof(BottomRowHeight));
                OnPropertyChanged(nameof(BottomSplitterHeight));
            }
        }
    }

    public GridLength LeftColumnWidth => new(LeftPanelWidth);
    public GridLength RightColumnWidth => new(RightPanelWidth);
    public GridLength BottomRowHeight => new(BottomPanelHeight);
    public bool LeftSplitterVisible => !LeftPanelLocked;
    public bool RightSplitterVisible => !RightPanelLocked;
    public bool BottomSplitterVisible => !BottomPanelLocked;
    public GridLength LeftSplitterWidth => LeftPanelLocked ? new GridLength(0) : new GridLength(5);
    public GridLength RightSplitterWidth => RightPanelLocked ? new GridLength(0) : new GridLength(5);
    public GridLength BottomSplitterHeight => BottomPanelLocked ? new GridLength(0) : new GridLength(5);

    // 卡片视图计算属性
    public Thickness CardMargin => new(2);
    public bool IsCardContextMenuEnabled => IsCardActionRightClick;

    public UISettingsService UISettingsService { get; }

    public double LogRowHeightValue => LogRowHeight switch
    {
        "Compact" => 24,
        "Standard" => 32,
        "Loose" => 40,
        _ => 32
    };

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void LoadLayoutSettings()
    {
        var config = UISettingsService.Config;

        LeftPanelWidth = config.LeftPanelWidth;
        LeftPanelLocked = config.LeftPanelLocked;
        RightPanelWidth = config.RightPanelWidth;
        RightPanelLocked = config.RightPanelLocked;
        BottomPanelHeight = config.BottomPanelHeight;
        BottomPanelLocked = config.BottomPanelLocked;

        ScrollbarStyle = config.ScrollbarStyle;
        ShowActionButtonsOnHover = config.ShowActionButtonsOnHover;
        ShowViewButton = config.ShowViewButton;
        ShowEditButton = config.ShowEditButton;
        ShowRescheduleButton = config.ShowRescheduleButton;
        ShowRefundButton = config.ShowRefundButton;
        ShowDeleteButton = config.ShowDeleteButton;
        IsTripListExpandedByDefault = config.IsTripListExpandedByDefault;
        ShowTimestamp = config.ShowTimestamp;
        ShowModuleSource = config.ShowModuleSource;
        LogRowHeight = config.LogRowHeight;

        // 卡片视图设置
        CardsPerRow = config.CardsPerRow;
        CardWidth = config.CardWidth;
        CardSpacing = config.CardSpacing;
        CardCornerRadius = config.CardCornerRadius;
        CardShowViewAction = config.CardShowViewAction;
        CardShowEditAction = config.CardShowEditAction;
        CardShowRescheduleAction = config.CardShowRescheduleAction;
        CardShowRefundAction = config.CardShowRefundAction;
        CardShowDeleteAction = config.CardShowDeleteAction;
        var generalConfig = new GeneralSettingsService().Config;
        CardContentDensity = generalConfig.CardContentDensity;
        CardActionTrigger = generalConfig.CardActionTrigger;
        CardDefaultAction = generalConfig.CardDefaultAction;
        IsCardActionRightClick = generalConfig.CardActionTrigger == "RightClick";
        CardEnableMultiSelect = config.CardEnableMultiSelect;
        CardStatusPosition = config.CardStatusPosition;
        CardHoverHighlight = config.CardHoverHighlight;
        CardShowShadow = config.CardShowShadow;
        CardHoverScale = config.CardHoverScale;
        CardBatchShowView = config.CardBatchShowView;
        CardBatchShowEdit = config.CardBatchShowEdit;
        CardBatchShowReschedule = config.CardBatchShowReschedule;
        CardBatchShowRefund = config.CardBatchShowRefund;
        CardBatchShowDelete = config.CardBatchShowDelete;
    }

    public void SaveLayoutSettings()
    {
        try
        {
            var config = UISettingsService.Config;
            config.LeftPanelWidth = LeftPanelWidth;
            config.LeftPanelLocked = LeftPanelLocked;
            config.RightPanelWidth = RightPanelWidth;
            config.RightPanelLocked = RightPanelLocked;
            config.BottomPanelHeight = BottomPanelHeight;
            config.BottomPanelLocked = BottomPanelLocked;
            UISettingsService.SaveConfig(config);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LayoutViewModel] 保存布局设置失败: {ex.Message}");
        }
    }

    public void ApplyScrollbarStyle()
    {
        try
        {
            if (Application.Current.MainWindow is MainWindow mainWindow) mainWindow.ApplyScrollbarStyle(ScrollbarStyle);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ApplyScrollbarStyle] 应用滚动条样式失败: {ex.Message}");
        }
    }
}