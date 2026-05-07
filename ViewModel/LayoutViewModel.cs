using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Messages;
using GuiPiao.Services;
using GuiPiao.View;
using System;
using System.Windows;

namespace GuiPiao.ViewModel
{
    public partial class LayoutViewModel : ObservableObject, IDisposable
    {
        private int _leftPanelWidth = 180;
        public int LeftPanelWidth
        {
            get => _leftPanelWidth;
            set
            {
                if (_leftPanelWidth != value)
                {
                    _leftPanelWidth = value;
                    OnPropertyChanged(nameof(LeftPanelWidth));
                    OnPropertyChanged(nameof(LeftColumnWidth));
                }
            }
        }

        private bool _leftPanelLocked = true;
        public bool LeftPanelLocked
        {
            get => _leftPanelLocked;
            set
            {
                if (_leftPanelLocked != value)
                {
                    _leftPanelLocked = value;
                    OnPropertyChanged(nameof(LeftPanelLocked));
                    OnPropertyChanged(nameof(LeftSplitterVisible));
                    OnPropertyChanged(nameof(LeftColumnWidth));
                    OnPropertyChanged(nameof(LeftSplitterWidth));
                }
            }
        }

        private int _rightPanelWidth = 220;
        public int RightPanelWidth
        {
            get => _rightPanelWidth;
            set
            {
                if (_rightPanelWidth != value)
                {
                    _rightPanelWidth = value;
                    OnPropertyChanged(nameof(RightPanelWidth));
                    OnPropertyChanged(nameof(RightColumnWidth));
                }
            }
        }

        private bool _rightPanelLocked = true;
        public bool RightPanelLocked
        {
            get => _rightPanelLocked;
            set
            {
                if (_rightPanelLocked != value)
                {
                    _rightPanelLocked = value;
                    OnPropertyChanged(nameof(RightPanelLocked));
                    OnPropertyChanged(nameof(RightSplitterVisible));
                    OnPropertyChanged(nameof(RightColumnWidth));
                    OnPropertyChanged(nameof(RightSplitterWidth));
                }
            }
        }

        private int _bottomPanelHeight = 250;
        public int BottomPanelHeight
        {
            get => _bottomPanelHeight;
            set
            {
                if (_bottomPanelHeight != value)
                {
                    _bottomPanelHeight = value;
                    OnPropertyChanged(nameof(BottomPanelHeight));
                    OnPropertyChanged(nameof(BottomRowHeight));
                }
            }
        }

        private bool _bottomPanelLocked = true;
        public bool BottomPanelLocked
        {
            get => _bottomPanelLocked;
            set
            {
                if (_bottomPanelLocked != value)
                {
                    _bottomPanelLocked = value;
                    OnPropertyChanged(nameof(BottomPanelLocked));
                    OnPropertyChanged(nameof(BottomSplitterVisible));
                    OnPropertyChanged(nameof(BottomRowHeight));
                    OnPropertyChanged(nameof(BottomSplitterHeight));
                }
            }
        }

        [ObservableProperty]
        private string _scrollbarStyle = "Normal";

        [ObservableProperty]
        private bool _showActionButtonsOnHover = true;

        [ObservableProperty]
        private bool _showViewButton = true;

        [ObservableProperty]
        private bool _showEditButton = true;

        [ObservableProperty]
        private bool _showRescheduleButton = true;

        [ObservableProperty]
        private bool _showRefundButton = true;

        [ObservableProperty]
        private bool _showDeleteButton = true;

        [ObservableProperty]
        private bool _isTripListExpandedByDefault = true;

        [ObservableProperty]
        private bool _showTimestamp = true;

        [ObservableProperty]
        private bool _showModuleSource = true;

        [ObservableProperty]
        private string _logRowHeight = "Standard";

        public GridLength LeftColumnWidth => new GridLength(LeftPanelWidth);
        public GridLength RightColumnWidth => new GridLength(RightPanelWidth);
        public GridLength BottomRowHeight => new GridLength(BottomPanelHeight);
        public bool LeftSplitterVisible => !LeftPanelLocked;
        public bool RightSplitterVisible => !RightPanelLocked;
        public bool BottomSplitterVisible => !BottomPanelLocked;
        public GridLength LeftSplitterWidth => LeftPanelLocked ? new GridLength(0) : new GridLength(5);
        public GridLength RightSplitterWidth => RightPanelLocked ? new GridLength(0) : new GridLength(5);
        public GridLength BottomSplitterHeight => BottomPanelLocked ? new GridLength(0) : new GridLength(5);

        public UISettingsService UISettingsService { get; }

        private bool _isDisposed;

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
                System.Diagnostics.Debug.WriteLine($"[LayoutViewModel] 保存布局设置失败: {ex.Message}");
            }
        }

        public void ApplyScrollbarStyle()
        {
            try
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.ApplyScrollbarStyle(ScrollbarStyle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApplyScrollbarStyle] 应用滚动条样式失败: {ex.Message}");
            }
        }

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
    }
}
