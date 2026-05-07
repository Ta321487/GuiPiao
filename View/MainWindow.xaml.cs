using CommunityToolkit.Mvvm.Messaging;
using GuiPiao.Converters;
using GuiPiao.Messages;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace GuiPiao.View
{
    public partial class MainWindow : Window
    {
        private Forms.NotifyIcon? _notifyIcon;
        private bool _isMinimizedToTray = false;
        private DragDropHelper? _dashboardDragDropHelper;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDashboardDragDrop();

            // 检查 DataContext
            this.Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext: {DataContext?.GetType().Name}");

                // 监听 DashboardCharts 变化，更新 Grid 布局
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    viewModel.PropertyChanged += (sender, args) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] PropertyChanged: {args.PropertyName}");
                        if (args.PropertyName == nameof(viewModel.DashboardCharts))
                        {
                            Dispatcher.BeginInvoke(() => UpdateDashboardGridLayout(viewModel), System.Windows.Threading.DispatcherPriority.Render);
                        }
                        else if (args.PropertyName == nameof(viewModel.IsTripListExpanded))
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] IsTripListExpanded changed to: {viewModel.IsTripListExpanded}");
                            Dispatcher.BeginInvoke(() => UpdateTripListLayout(viewModel), System.Windows.Threading.DispatcherPriority.Render);
                        }
                    };

                    // 初始更新（延迟等待 ItemsControl 加载完成）
                    Dispatcher.BeginInvoke(() => UpdateDashboardGridLayout(viewModel), System.Windows.Threading.DispatcherPriority.Loaded);

                    // 应用启动页面设置（在窗口加载完成后调用）
                    // 避免在窗口未显示时设置子窗口的 Owner 属性导致错误
                    System.Diagnostics.Debug.WriteLine("[MainWindow] 调用 ApplyStartupPageSetting");
                    viewModel.ApplyStartupPageSetting();

                    // 初始化行程列表布局（应用启动时的展开/折叠状态）
                    Dispatcher.BeginInvoke(() => UpdateTripListLayout(viewModel), System.Windows.Threading.DispatcherPriority.Loaded);

                    // 初始化快捷键管理器
                    InitializeShortcuts(viewModel);
                }
            };

            // 初始化系统托盘图标
            InitializeNotifyIcon();

            // 初始应用日志列显示设置
            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                UpdateLogColumnsVisibility(viewModel);
            }
        }

        /// <summary>
        /// 窗口 SourceInitialized 事件 - 应用窗口状态设置
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 在窗口源初始化后应用窗口状态设置
            // 这样可以确保设置不会被 WPF 的默认行为覆盖
            ApplyWindowStateSetting();
        }

        /// <summary>
        /// 更新日志列的显示/隐藏和行高
        /// </summary>
        public void UpdateLogColumnsVisibility(ViewModel.MainViewModel viewModel)
        {
            if (TimeColumn != null)
            {
                TimeColumn.Visibility = viewModel.ShowTimestamp ? Visibility.Visible : Visibility.Collapsed;
            }
            if (ModuleColumn != null)
            {
                ModuleColumn.Visibility = viewModel.ShowModuleSource ? Visibility.Visible : Visibility.Collapsed;
            }

            // 更新日志行高 - 通过更新资源字典中的值
            double rowHeight = viewModel.LogRowHeightValue;
            if (LogDataGrid != null)
            {
                LogDataGrid.Resources["DataGridRowHeight"] = rowHeight;
            }
        }

        /// <summary>
        /// 初始化快捷键管理器
        /// </summary>
        private void InitializeShortcuts(ViewModel.MainViewModel viewModel)
        {
            // 初始化快捷键管理器
            ShortcutManager.Instance.Initialize(this);

            // 注册快捷键动作 - 票务操作
            ShortcutManager.Instance.RegisterAction("NewTicket", () => viewModel.NewTicketRecordCommand.Execute(null));
            ShortcutManager.Instance.RegisterAction("OcrTicket", () => viewModel.OcrRecognizeTicketCommand.Execute(null));
            ShortcutManager.Instance.RegisterAction("PreviewTicket", () => viewModel.TicketPreviewCommand.Execute(null));

            // 注册快捷键动作 - 行程管理
            ShortcutManager.Instance.RegisterAction("OpenMap", () => viewModel.OpenTicketMapCommand.Execute(null));
            ShortcutManager.Instance.RegisterAction("RefreshData", () => viewModel.TripMenuCommand("RefreshList"));
            ShortcutManager.Instance.RegisterAction("PreviousPage", () => viewModel.PreviousPageCommand());
            ShortcutManager.Instance.RegisterAction("NextPage", () => viewModel.NextPageCommand());

            // 注册快捷键动作 - 工具操作
            ShortcutManager.Instance.RegisterAction("OpenLogManager", () => viewModel.OpenLogManager());

            // 注册快捷键动作 - 系统设置
            ShortcutManager.Instance.RegisterAction("OpenSettings", () => viewModel.OpenSettings(null));

            // 注册快捷键动作 - 帮助操作
            ShortcutManager.Instance.RegisterAction("HelpDoc", () => viewModel.HelpMenuCommand("HelpDoc"));

            // 注册快捷键动作 - 文件存储
            ShortcutManager.Instance.RegisterAction("ExitApp", () => viewModel.StorageMenuCommand("Exit"));

            // 注册快捷键动作 - 编辑和删除（需要选中项）
            ShortcutManager.Instance.RegisterAction("EditTicket", () =>
            {
                if (viewModel.TripList.SelectedTripItem != null)
                    viewModel.TripList.EditTripCommand(viewModel.TripList.SelectedTripItem);
            });
            ShortcutManager.Instance.RegisterAction("DeleteTicket", () =>
            {
                if (viewModel.TripList.SelectedTripItem != null)
                    viewModel.TripList.DeleteTripCommand(viewModel.TripList.SelectedTripItem);
            });

            // 注册快捷键动作 - 跳转到页
            ShortcutManager.Instance.RegisterAction("GotoPage", () =>
            {
                // 可以弹出一个输入框让用户输入页码
                viewModel.GoToPageCommand(viewModel.TripList.CurrentPage);
            });
        }

        #region 仪表盘拖拽排序

        /// <summary>
        /// 初始化仪表盘拖拽排序功能
        /// </summary>
        private void InitializeDashboardDragDrop()
        {
            _dashboardDragDropHelper = new DragDropHelper(
                onDropAction: (sourceIndex, targetIndex) =>
                {
                    if (DataContext is not ViewModel.MainViewModel viewModel) return;
                    if (viewModel.DashboardCharts == null || viewModel.DashboardCharts.Count == 0) return;

                    // 移动图表位置
                    DragDropHelper.MoveItem(viewModel.DashboardCharts, sourceIndex, targetIndex);

                    // 更新卡片的 SortOrder 和 Grid 位置
                    for (int i = 0; i < viewModel.DashboardCharts.Count; i++)
                    {
                        var chartVm = viewModel.DashboardCharts[i];
                        if (chartVm?.Card != null)
                        {
                            chartVm.Card.SortOrder = i;
                            // 重新计算 Grid 位置（根据布局类型）
                            UpdateCardGridPosition(chartVm.Card, i, viewModel.DashboardConfig?.LayoutType ?? LayoutType.TwoColumn);
                        }
                    }

                    // 刷新布局
                    UpdateDashboardGridLayout(viewModel);

                    // 保存设置到配置文件
                    SaveDashboardCardOrder(viewModel);
                }
            );
        }

        /// <summary>
        /// 更新卡片在 Grid 中的位置
        /// </summary>
        private static void UpdateCardGridPosition(DashboardCard card, int index, LayoutType layoutType)
        {
            int columns = layoutType switch
            {
                LayoutType.ThreeColumn => 3,
                LayoutType.TwoColumn => 2,
                LayoutType.LeftOneRightTwo => index == 0 ? 1 : 2,
                LayoutType.TopOneBottomTwo => 2,
                _ => 2
            };

            card.GridRow = index / columns;
            card.GridColumn = index % columns;
            card.GridRowSpan = 1;
            card.GridColumnSpan = 1;
        }

        /// <summary>
        /// 保存仪表盘卡片顺序到配置文件
        /// </summary>
        private static void SaveDashboardCardOrder(ViewModel.MainViewModel viewModel)
        {
            try
            {
                var settingsService = new DashboardSettingsService();
                var config = settingsService.Config;

                // 更新卡片顺序
                for (int i = 0; i < viewModel.DashboardCharts.Count; i++)
                {
                    var chartVm = viewModel.DashboardCharts[i];
                    if (chartVm?.Card != null)
                    {
                        var card = config.Cards.FirstOrDefault(c => c.Id == chartVm.Card.Id);
                        if (card != null)
                        {
                            card.SortOrder = chartVm.Card.SortOrder;
                            card.GridRow = chartVm.Card.GridRow;
                            card.GridColumn = chartVm.Card.GridColumn;
                        }
                    }
                }

                settingsService.SaveConfig(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] 保存仪表盘卡片顺序失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 鼠标左键按下时记录拖拽起始项
        /// </summary>
        private void DashboardItemsControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查是否点击在拖拽手柄上
            var source = e.OriginalSource as DependencyObject;
            var textBlock = DragDropHelper.FindVisualParent<TextBlock>(source);

            // 只有点击拖拽手柄（☰）时才启用拖拽
            if (textBlock == null || textBlock.Text != "☰")
            {
                return;
            }

            _dashboardDragDropHelper?.OnPreviewMouseLeftButtonDown(sender, e, (originalSource) =>
            {
                // 查找 DashboardChartView 容器
                return DragDropHelper.FindVisualParent<ContentPresenter>(originalSource);
            });
        }

        /// <summary>
        /// 鼠标移动时开始拖拽
        /// </summary>
        private void DashboardItemsControl_MouseMove(object sender, MouseEventArgs e)
        {
            _dashboardDragDropHelper?.OnMouseMove(sender, e);
        }

        /// <summary>
        /// 拖拽悬停时更新视觉效果
        /// </summary>
        private void DashboardItemsControl_DragOver(object sender, DragEventArgs e)
        {
            _dashboardDragDropHelper?.OnDragOver(sender, e);
        }

        /// <summary>
        /// 放置时更新排序
        /// </summary>
        private void DashboardItemsControl_Drop(object sender, DragEventArgs e)
        {
            _dashboardDragDropHelper?.OnDrop(sender, e, (originalSource) =>
            {
                // 查找 DashboardChartView 容器
                return DragDropHelper.FindVisualParent<ContentPresenter>(originalSource);
            });
        }

        #endregion

        /// <summary>
        /// 更新仪表盘 Grid 布局的行列定义
        /// </summary>
        private void UpdateDashboardGridLayout(ViewModel.MainViewModel viewModel)
        {
            // 获取 ItemsControl 的 ItemsPanel 中的 Grid
            if (DashboardItemsControl == null) return;

            // 通过 VisualTreeHelper 获取 ItemsPanel
            var itemsPanel = GetItemsPanel(DashboardItemsControl);
            if (itemsPanel is not Grid dashboardGrid) return;

            dashboardGrid.RowDefinitions.Clear();
            dashboardGrid.ColumnDefinitions.Clear();

            // 从 ViewModel 获取配置
            var config = viewModel.DashboardConfig;
            if (config == null) return;

            var cards = config.Cards ?? new ObservableCollection<DashboardCard>();
            int rows = Utils.DashboardLayoutManager.GetRequiredRows(cards, config.LayoutType);
            int columns = Utils.DashboardLayoutManager.GetRequiredColumns(config.LayoutType);

            System.Diagnostics.Debug.WriteLine($"[UpdateDashboardGridLayout] 准备更新布局: {rows}行 x {columns}列, 类型={config.LayoutType}, 卡片数={config.Cards?.Count ?? 0}");

            // 添加行定义
            for (int i = 0; i < rows; i++)
            {
                dashboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            // 添加列定义
            for (int i = 0; i < columns; i++)
            {
                dashboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // 强制更新所有容器的 Grid 位置
            Dispatcher.BeginInvoke(() =>
            {
                UpdateItemContainersGridPosition();
            }, System.Windows.Threading.DispatcherPriority.Render);

            System.Diagnostics.Debug.WriteLine($"[UpdateDashboardGridLayout] 布局更新完成: {rows}行 x {columns}列");
        }

        /// <summary>
        /// 更新所有 ItemContainer 的 Grid 位置
        /// </summary>
        private void UpdateItemContainersGridPosition()
        {
            if (DashboardItemsControl == null) return;

            System.Diagnostics.Debug.WriteLine($"[UpdateItemContainersGridPosition] Items.Count={DashboardItemsControl.Items.Count}, Generator.Status={DashboardItemsControl.ItemContainerGenerator.Status}");

            for (int i = 0; i < DashboardItemsControl.Items.Count; i++)
            {
                var container = DashboardItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                var chartVm = DashboardItemsControl.Items[i] as ViewModel.DashboardChartViewModel;

                System.Diagnostics.Debug.WriteLine($"[UpdateItemContainersGridPosition] 索引 {i}: container={(container != null ? "有" : "无")}, chartVm={(chartVm != null ? "有" : "无")}");

                if (container == null)
                {
                    // 尝试从可视化树查找
                    var itemsPanel = GetItemsPanel(DashboardItemsControl);
                    if (itemsPanel is Grid grid)
                    {
                        var presenters = grid.Children.OfType<ContentPresenter>().ToList();
                        System.Diagnostics.Debug.WriteLine($"[UpdateItemContainersGridPosition] 从 Grid 找到 {presenters.Count} 个 ContentPresenter");
                        if (i < presenters.Count)
                        {
                            container = presenters[i];
                            System.Diagnostics.Debug.WriteLine($"[UpdateItemContainersGridPosition] 使用 Grid.Children[{i}] 作为容器");
                        }
                    }
                }

                if (container == null) continue;
                if (chartVm?.Card == null) continue;

                var card = chartVm.Card;
                System.Diagnostics.Debug.WriteLine($"[UpdateItemContainersGridPosition] 设置容器 {i}: Row={card.GridRow}, Col={card.GridColumn}, RowSpan={card.GridRowSpan}, ColSpan={card.GridColumnSpan}");

                Grid.SetRow(container, card.GridRow);
                Grid.SetColumn(container, card.GridColumn);
                Grid.SetRowSpan(container, card.GridRowSpan);
                Grid.SetColumnSpan(container, card.GridColumnSpan);

                // 强制刷新容器
                container.InvalidateArrange();
            }

            // 强制刷新整个 ItemsControl
            DashboardItemsControl.InvalidateArrange();
        }

        /// <summary>
        /// 获取 ItemsControl 的 ItemsPanel (IsItemsHost=true 的 Grid)
        /// </summary>
        private static Grid? GetItemsPanel(ItemsControl itemsControl)
        {
            // ItemsControl 的 ItemsPanel 是通过 ItemsPresenter 承载的
            // 我们需要找到 IsItemsHost="True" 的那个 Grid
            return FindItemsHostGrid(itemsControl);
        }

        /// <summary>
        /// 查找 ItemsHost Grid (IsItemsHost=true)
        /// </summary>
        private static Grid? FindItemsHostGrid(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 检查是否是 Grid 且是 ItemsHost
                if (child is Grid grid)
                {
                    // 通过检查父元素是否是 ItemsPresenter 来判断是否是 ItemsHost
                    if (VisualTreeHelper.GetParent(grid) is ItemsPresenter)
                    {
                        return grid;
                    }
                }

                // 递归查找
                var result = FindItemsHostGrid(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon { Icon = SystemIcons.Application, Text = "GuiPiao - 火车票管理", Visible = false };

            // 创建托盘菜单
            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("显示窗口", null, (s, e) => ShowWindowFromTray());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => Close());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // 双击托盘图标显示窗口
            _notifyIcon.DoubleClick += (s, e) => ShowWindowFromTray();
        }

        /// <summary>
        /// 从托盘显示窗口
        /// </summary>
        private void ShowWindowFromTray()
        {
            _isMinimizedToTray = false;
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Show();
            this.Activate();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        /// <summary>
        /// 最小化到托盘
        /// </summary>
        private void MinimizeToTray()
        {
            _isMinimizedToTray = true;
            this.ShowInTaskbar = false;
            this.Hide();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                // 显示系统通知
                _notifyIcon.ShowBalloonTip(
                    3000, // 显示3秒
                    "GuiPiao",
                    "程序已最小化到系统托盘，双击图标可恢复窗口",
                    Forms.ToolTipIcon.Info
                );
            }
        }

        /// <summary>
        /// 应用窗口状态设置
        /// </summary>
        private void ApplyWindowStateSetting()
        {
            var config = new GeneralSettingsService().Config;

            switch (config.WindowState)
            {
                case WindowStateOption.Maximized:
                    this.WindowState = WindowState.Maximized;
                    break;
                case WindowStateOption.Normal:
                    // 应用保存的窗口大小和位置
                    if (!double.IsNaN(config.WindowWidth) && config.WindowWidth > 0)
                    {
                        this.Width = config.WindowWidth;
                    }
                    if (!double.IsNaN(config.WindowHeight) && config.WindowHeight > 0)
                    {
                        this.Height = config.WindowHeight;
                    }
                    if (!double.IsNaN(config.WindowLeft))
                    {
                        this.Left = config.WindowLeft;
                    }
                    if (!double.IsNaN(config.WindowTop))
                    {
                        this.Top = config.WindowTop;
                    }
                    this.WindowState = WindowState.Normal;
                    break;
                case WindowStateOption.MinimizedToTray:
                    // 最小化到托盘
                    MinimizeToTray();
                    break;
            }
        }

        /// <summary>
        /// 保存上次关闭的页面状态
        /// </summary>
        private void SaveLastPageState()
        {
            try
            {
                // 使用 WindowStateManager 获取当前窗口状态
                var lastPage = WindowStateManager.Instance.GetCurrentWindowType();

                System.Diagnostics.Debug.WriteLine($"[MainWindow] SaveLastPageState: 保存 lastPage={lastPage}");

                // 使用 UpdateConfig 方法保存，避免覆盖其他设置
                JsonConfigManager.Instance.UpdateConfig("generalsettings.json", new GeneralConfig(), config =>
                {
                    config.LastPage = lastPage;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] 保存上次页面状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存窗口大小和位置
        /// </summary>
        private void SaveWindowPositionAndSize()
        {
            try
            {
                // 只在窗口状态为 Normal 时保存位置和大小
                if (this.WindowState == WindowState.Normal)
                {
                    var left = this.Left;
                    var top = this.Top;
                    var width = this.Width;
                    var height = this.Height;

                    // 使用 UpdateConfig 方法，确保在保存前重新加载最新配置
                    // 避免覆盖其他设置项
                    JsonConfigManager.Instance.UpdateConfig("generalsettings.json", new GeneralConfig(), config =>
                    {
                        config.WindowLeft = left;
                        config.WindowTop = top;
                        config.WindowWidth = width;
                        config.WindowHeight = height;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] 保存窗口位置和大小失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口关闭事件处理
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] OnClosing 被调用");

            // 首先保存上次关闭的页面状态（在子窗口关闭前保存）
            // 注意：这里必须在最前面保存，因为后面的 MessageBox 可能会阻塞或抛出异常
            System.Diagnostics.Debug.WriteLine("[MainWindow] 准备调用 SaveLastPageState");
            SaveLastPageState();

            // 询问用户是最小化到托盘还是退出
            var result = MessageBoxWindow.Show(
                this,
                "您希望如何关闭火车票管理系统？\n\n" +
                "【最小化到托盘】程序继续在后台运行，点击托盘图标可恢复窗口\n" +
                "【退出程序】完全关闭程序，释放所有资源\n" +
                "【取消】不执行任何操作，回到主界面",
                "关闭确认",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                yesText: "最小化到托盘",
                noText: "退出程序",
                cancelText: "取消");

            switch (result)
            {
                case MessageBoxResult.Yes:
                    // 最小化到托盘
                    e.Cancel = true; // 取消关闭
                    MinimizeToTray();
                    return;
                case MessageBoxResult.No:
                    // 退出程序，继续执行关闭逻辑
                    break;
                case MessageBoxResult.Cancel:
                    // 取消关闭
                    e.Cancel = true;
                    return;
            }

            // 关闭所有子窗口（新增车票、编辑车票、地图、日志管理等）
            CloseAllChildWindows();

            // 保存窗口大小和位置
            SaveWindowPositionAndSize();

            // 释放托盘图标资源
            _notifyIcon?.Dispose();

            // 释放 ViewModel 资源
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnClosing(e);

            // 注意：退出自动备份功能已移至数据库设置页面统一管理
            // 相关逻辑在 DatabaseLifecycleService.OnExitAsync() 中实现
        }
        /// <summary>
        /// 关闭所有子窗口
        /// </summary>
        private void CloseAllChildWindows()
        {
            try
            {
                // 获取所有需要关闭的窗口类型
                var windowsToClose = Application.Current.Windows
                    .OfType<Window>()
                    .Where(w => w != this && w.IsVisible)
                    .ToList();

                foreach (var window in windowsToClose)
                {
                    try
                    {
                        window.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] 关闭窗口失败: {window.GetType().Name}, 错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] CloseAllChildWindows 失败: {ex.Message}");
            }
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                viewModel.ToggleTripListCommand();
            }
        }

        /// <summary>
        /// 更新行程列表布局（处理折叠/展开时的高度）
        /// </summary>
        private void UpdateTripListLayout(ViewModel.MainViewModel viewModel)
        {
            if (TripListBorder == null || MainGrid == null) return;

            if (viewModel.IsTripListExpanded)
            {
                // 展开状态：恢复用户设置的高度
                TripListBorder.ClearValue(Border.HeightProperty);
                MainGrid.RowDefinitions[2].Height = viewModel.BottomRowHeight;
                System.Diagnostics.Debug.WriteLine($"[UpdateTripListLayout] 展开状态，恢复高度: {viewModel.BottomRowHeight}");
            }
            else
            {
                // 折叠状态：设置Grid行高度为Auto，让行跟随Border高度
                MainGrid.RowDefinitions[2].Height = GridLength.Auto;
                TripListBorder.Height = 40;
                System.Diagnostics.Debug.WriteLine("[UpdateTripListLayout] 折叠状态，设置行高度为Auto，Border高度为40px");
            }
        }

        /// <summary>
        /// 改签按钮点击事件处理
        /// </summary>
        private async void RescheduleButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RescheduleButton_Click] 事件被触发");
            if (sender is Button button && button.Tag is GuiPiao.Model.TripItem trip)
            {
                System.Diagnostics.Debug.WriteLine($"[RescheduleButton_Click] 获取到 trip: {trip.TrainNo}");
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    await viewModel.TripList.RescheduleTripCommand(trip);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[RescheduleButton_Click] 无法获取 trip 对象");
            }
        }

        /// <summary>
        /// 退票按钮点击事件处理
        /// </summary>
        private async void RefundButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[RefundButton_Click] 事件被触发");
            if (sender is Button button && button.Tag is GuiPiao.Model.TripItem trip)
            {
                System.Diagnostics.Debug.WriteLine($"[RefundButton_Click] 获取到 trip: {trip.TrainNo}");
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    await viewModel.TripList.RefundTripCommand(trip);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[RefundButton_Click] 无法获取 trip 对象");
            }
        }

        /// <summary>
        /// 查看按钮点击事件处理
        /// </summary>
        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[ViewButton_Click] 事件被触发");
            if (sender is Button button && button.Tag is GuiPiao.Model.TripItem trip)
            {
                System.Diagnostics.Debug.WriteLine($"[ViewButton_Click] 获取到 trip: {trip.TrainNo}");
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    viewModel.TripList.ViewTripCommand(trip);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ViewButton_Click] 无法获取 trip 对象");
            }
        }

        /// <summary>
        /// 编辑按钮点击事件处理
        /// </summary>
        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[EditButton_Click] 事件被触发");
            if (sender is Button button && button.Tag is GuiPiao.Model.TripItem trip)
            {
                System.Diagnostics.Debug.WriteLine($"[EditButton_Click] 获取到 trip: {trip.TrainNo}");
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    await viewModel.TripList.EditTripCommand(trip);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[EditButton_Click] 无法获取 trip 对象");
            }
        }

        /// <summary>
        /// 删除按钮点击事件处理
        /// </summary>
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[DeleteButton_Click] 事件被触发");
            if (sender is Button button && button.Tag is GuiPiao.Model.TripItem trip)
            {
                System.Diagnostics.Debug.WriteLine($"[DeleteButton_Click] 获取到 trip: {trip.TrainNo}");
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    await viewModel.TripList.DeleteTripCommand(trip);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DeleteButton_Click] 无法获取 trip 对象");
            }
        }

        /// <summary>
        /// 行程列表双击事件处理
        /// </summary>
        private void TripDataGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid dataGrid || DataContext is not ViewModel.MainViewModel viewModel)
                return;

            // 获取双击位置
            var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
            if (hitTestResult == null)
                return;

            // 检查是否点击在 DataGridRow 上
            var row = FindParent<DataGridRow>(hitTestResult.VisualHit);

            if (row != null && dataGrid.SelectedItem is GuiPiao.Model.TripItem selectedItem)
            {
                // 双击在行上 - 触发编辑
                viewModel.HandleTripDoubleClick(selectedItem);
            }
        }

        /// <summary>
        /// 查找指定类型的父元素
        /// </summary>
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// DataGrid加载完成后根据配置创建列
        /// </summary>
        private void TripDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[TripDataGrid_Loaded] DataGrid加载完成");
            InitializeDataGridColumns();

            // 应用滚动条样式
            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                ApplyScrollbarStyle(viewModel.ScrollbarStyle);
            }
        }

        /// <summary>
        /// DataGrid排序事件处理 - 使用数据库排序
        /// </summary>
        private async void TripDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // 取消默认排序行为
            e.Handled = true;

            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                var column = e.Column;
                var sortMemberPath = column.SortMemberPath;

                if (string.IsNullOrEmpty(sortMemberPath))
                    return;

                // 确定新的排序方向
                ListSortDirection newDirection;
                if (column.SortDirection == null)
                {
                    // 当前没有排序，默认升序
                    newDirection = ListSortDirection.Ascending;
                }
                else if (column.SortDirection == ListSortDirection.Ascending)
                {
                    // 当前升序，切换为降序
                    newDirection = ListSortDirection.Descending;
                }
                else
                {
                    // 当前降序，切换为升序
                    newDirection = ListSortDirection.Ascending;
                }

                // 更新列头排序指示器
                column.SortDirection = newDirection;

                // 清除其他列的排序指示器
                if (sender is DataGrid dataGrid)
                {
                    foreach (var otherColumn in dataGrid.Columns)
                    {
                        if (otherColumn != column)
                        {
                            otherColumn.SortDirection = null;
                        }
                    }
                }

                // 保存排序信息并重新加载数据（数据库排序）
                await viewModel.SaveDataSortInfoAsync(sortMemberPath, newDirection);
            }
        }

        /// <summary>
        /// 根据配置初始化DataGrid列
        /// </summary>
        public void InitializeDataGridColumns()
        {
            if (TripDataGrid == null) return;

            System.Diagnostics.Debug.WriteLine("[InitializeDataGridColumns] 开始初始化列");

            // 获取列配置（从ViewModel或默认配置）
            List<DataGridColumnConfig> columnConfigs;
            if (DataContext is ViewModel.MainViewModel viewModel && viewModel.UISettingsService?.Config?.DataGridColumns != null && viewModel.UISettingsService.Config.DataGridColumns.Count > 0)
            {
                // 去重：按 FieldName 分组，只保留第一个
                columnConfigs = viewModel.UISettingsService.Config.DataGridColumns
                    .GroupBy(c => c.FieldName)
                    .Select(g => g.First())
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"[InitializeDataGridColumns] 从配置加载 {columnConfigs.Count} 列");
            }
            else
            {
                columnConfigs = DataGridColumnConfig.GetDefaultColumns();
                System.Diagnostics.Debug.WriteLine($"[InitializeDataGridColumns] 使用默认 {columnConfigs.Count} 列");
            }

            // 清空现有列
            TripDataGrid.Columns.Clear();

            // 按显示顺序排序
            var sortedConfigs = columnConfigs.Where(c => c.IsVisible).OrderBy(c => c.DisplayOrder).ToList();
            System.Diagnostics.Debug.WriteLine($"[InitializeDataGridColumns] 可见列数: {sortedConfigs.Count}");

            double totalWidth = 0;
            foreach (var config in sortedConfigs)
            {
                DataGridColumn column = CreateColumnFromConfig(config);
                if (column != null)
                {
                    TripDataGrid.Columns.Add(column);
                    // 计算实际宽度：如果是Star类型，使用MinWidth
                    double actualWidth = column.Width.IsStar ? column.MinWidth : column.Width.Value;
                    System.Diagnostics.Debug.WriteLine($"[InitializeDataGridColumns] 添加列: {config.FieldName}, 宽度: {column.Width}, MinWidth: {column.MinWidth}, 实际宽度: {actualWidth}");
                    totalWidth += actualWidth;
                }
            }

            // 添加填充列（自动填充剩余空间，使垂直滚动条始终固定在右侧）
            AddFillerColumn();
            
            // 添加操作列（固定在最右侧）
            AddActionColumn();
            
            // 计算操作列宽度并加入总宽度
            if (DataContext is ViewModel.MainViewModel vm)
            {
                int visibleButtonCount = 0;
                if (vm.Layout.ShowViewButton) visibleButtonCount++;
                if (vm.Layout.ShowEditButton) visibleButtonCount++;
                if (vm.Layout.ShowRescheduleButton) visibleButtonCount++;
                if (vm.Layout.ShowRefundButton) visibleButtonCount++;
                if (vm.Layout.ShowDeleteButton) visibleButtonCount++;
                double actionColumnWidth = visibleButtonCount * 32 + 20;
                totalWidth += actionColumnWidth;
                System.Diagnostics.Debug.WriteLine($"[InitializeDataGridColumns] 操作列宽度: {actionColumnWidth}");
            }

            System.Diagnostics.Debug.WriteLine($"[InitializeDataGridColumns] 列总宽度: {totalWidth}");
            System.Diagnostics.Debug.WriteLine($"[InitializeDataGridColumns] DataGrid.ActualWidth: {TripDataGrid.ActualWidth}");
        }

        /// <summary>
        /// 添加填充列（自动填充剩余空间，使垂直滚动条始终固定在右侧）
        /// </summary>
        private void AddFillerColumn()
        {
            var fillerColumn = new DataGridTemplateColumn
            {
                Header = "",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 0,
                IsReadOnly = true,
                CanUserReorder = false,
                CanUserResize = false
            };

            // 创建空单元格模板
            var cellTemplate = new DataTemplate();
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetValue(TextBlock.TextProperty, "");
            cellTemplate.VisualTree = textBlockFactory;
            fillerColumn.CellTemplate = cellTemplate;

            TripDataGrid.Columns.Add(fillerColumn);
        }

        /// <summary>
        /// 添加操作列（固定在最右侧，宽度根据按钮数量自动计算）
        /// </summary>
        private void AddActionColumn()
        {
            if (DataContext is not ViewModel.MainViewModel viewModel) return;

            // 计算需要显示的按钮数量
            int visibleButtonCount = 0;
            if (viewModel.Layout.ShowViewButton) visibleButtonCount++;
            if (viewModel.Layout.ShowEditButton) visibleButtonCount++;
            if (viewModel.Layout.ShowRescheduleButton) visibleButtonCount++;
            if (viewModel.Layout.ShowRefundButton) visibleButtonCount++;
            if (viewModel.Layout.ShowDeleteButton) visibleButtonCount++;

            // 每个按钮约32px（包括margin），加上padding和border
            double columnWidth = visibleButtonCount * 32 + 20;

            var actionColumn = new DataGridTemplateColumn
            {
                Header = "操作",
                Width = new DataGridLength(columnWidth),
                MinWidth = columnWidth,
                IsReadOnly = true,
                CanUserReorder = false,  // 禁止拖动改变顺序
                CanUserResize = false    // 禁止拖动改变宽度
            };

            // 创建单元格模板（空模板，因为按钮是在RowStyle中定义的）
            var cellTemplate = new DataTemplate();
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetValue(TextBlock.TextProperty, "...");
            textBlockFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            textBlockFactory.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.Transparent);
            cellTemplate.VisualTree = textBlockFactory;
            actionColumn.CellTemplate = cellTemplate;

            TripDataGrid.Columns.Add(actionColumn);
        }

        /// <summary>
        /// 根据配置创建列
        /// </summary>
        private DataGridColumn CreateColumnFromConfig(DataGridColumnConfig config)
        {
            // 解析宽度值 - 所有列使用固定宽度，确保水平滚动条正常工作
            DataGridLength columnWidth;
            if (config.Width == "*")
            {
                // Star类型转为固定宽度（使用MinWidth）
                columnWidth = new DataGridLength(config.MinWidth > 0 ? config.MinWidth : 120);
            }
            else if (double.TryParse(config.Width, out double widthValue) && widthValue > 0)
            {
                columnWidth = new DataGridLength(widthValue);
            }
            else
            {
                // 默认宽度
                columnWidth = new DataGridLength(100);
            }

            // 标签列使用模板列
            if (config.FieldName == "Tags")
            {
                var templateColumn = new DataGridTemplateColumn
                {
                    Header = config.Header,
                    Width = columnWidth,
                    MinWidth = config.MinWidth,
                    IsReadOnly = config.IsReadOnly
                };

                // 创建单元格模板 - 使用 StackPanel 水平排列标签
                var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                stackPanelFactory.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
                stackPanelFactory.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);
                stackPanelFactory.SetValue(StackPanel.MarginProperty, new Thickness(2));

                // 创建 ItemsControl 显示标签列表（限制显示数量）
                var itemsControlFactory = new FrameworkElementFactory(typeof(ItemsControl));
                itemsControlFactory.SetBinding(ItemsControl.ItemsSourceProperty, new System.Windows.Data.Binding(config.FieldName)
                {
                    Converter = new TagListLimitConverter()
                });
                itemsControlFactory.SetValue(ItemsControl.VerticalAlignmentProperty, VerticalAlignment.Center);

                // 设置 ItemsPanel 为 StackPanel（水平排列，不换行）
                var itemsPanelTemplate = new ItemsPanelTemplate();
                var innerStackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
                innerStackPanelFactory.SetValue(StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);
                itemsPanelTemplate.VisualTree = innerStackPanelFactory;
                itemsControlFactory.SetValue(ItemsControl.ItemsPanelProperty, itemsPanelTemplate);

                // 设置 ItemTemplate - 显示为圆角标签样式
                var itemTemplate = new DataTemplate(typeof(TicketTag));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                // 使用半透明背景，让选中行的背景色能够透过来
                borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Color")
                {
                    Converter = new TagBackgroundOpacityConverter()
                });
                borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("Color")
                {
                    Converter = new GuiPiao.Converters.HexToBrushConverter()
                });
                borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
                borderFactory.SetValue(Border.PaddingProperty, new Thickness(6, 2, 6, 2));
                borderFactory.SetValue(Border.MarginProperty, new Thickness(0, 0, 4, 0));

                var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
                textBlockFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
                textBlockFactory.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("TextColor")
                {
                    Converter = new GuiPiao.Converters.HexToBrushConverter()
                });
                textBlockFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
                textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

                borderFactory.AppendChild(textBlockFactory);
                itemTemplate.VisualTree = borderFactory;
                itemsControlFactory.SetValue(ItemsControl.ItemTemplateProperty, itemTemplate);

                stackPanelFactory.AppendChild(itemsControlFactory);

                // 创建溢出指示器（当标签数量超过限制时显示）
                var overflowTextFactory = new FrameworkElementFactory(typeof(TextBlock));
                overflowTextFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                overflowTextFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
                overflowTextFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.Gray));
                overflowTextFactory.SetValue(TextBlock.MarginProperty, new Thickness(4, 0, 0, 0));
                overflowTextFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(config.FieldName)
                {
                    Converter = new TagOverflowIndicatorConverter()
                });
                overflowTextFactory.SetBinding(TextBlock.VisibilityProperty, new System.Windows.Data.Binding(config.FieldName)
                {
                    Converter = new TagOverflowVisibilityConverter()
                });
                stackPanelFactory.AppendChild(overflowTextFactory);

                var cellTemplate = new DataTemplate();
                cellTemplate.VisualTree = stackPanelFactory;
                templateColumn.CellTemplate = cellTemplate;

                // 设置单元格样式 - 添加 Tooltip 显示完整标签列表，选中时背景透明
                var cellStyle = new Style(typeof(DataGridCell));
                cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, System.Windows.Media.Brushes.Transparent));
                cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, System.Windows.Media.Brushes.Transparent));
                cellStyle.Setters.Add(new Setter(DataGridCell.ToolTipProperty, new System.Windows.Data.Binding(config.FieldName)
                {
                    Converter = new TagListToTooltipConverter()
                }));
                // 选中时保持背景透明，让行的选中色显示出来
                cellStyle.Triggers.Add(new Trigger
                {
                    Property = DataGridCell.IsSelectedProperty,
                    Value = true,
                    Setters =
                    {
                        new Setter(DataGridCell.BackgroundProperty, System.Windows.Media.Brushes.Transparent),
                        new Setter(DataGridCell.BorderBrushProperty, System.Windows.Media.Brushes.Transparent)
                    }
                });
                templateColumn.CellStyle = cellStyle;

                return templateColumn;
            }

            // 普通文本列
            var binding = new System.Windows.Data.Binding(config.FieldName);

            // 日期列使用格式化转换器
            if (config.FieldName == "DepartDate")
            {
                binding.Converter = new GuiPiao.Converters.DateFormatConverter();
            }

            var textColumn = new DataGridTextColumn
            {
                Header = config.Header,
                Binding = binding,
                SortMemberPath = config.FieldName,
                Width = columnWidth,
                MinWidth = config.MinWidth,
                IsReadOnly = config.IsReadOnly
            };

            // 设置单元格样式（添加Tooltip和省略号）
            var elementStyle = new Style(typeof(TextBlock));
            elementStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            elementStyle.Setters.Add(new Setter(TextBlock.ToolTipProperty, new System.Windows.Data.Binding(config.FieldName)));
            elementStyle.Setters.Add(new Setter(ToolTipService.ShowDurationProperty, 5000));
            textColumn.ElementStyle = elementStyle;

            return textColumn;
        }

        /// <summary>
        /// 刷新DataGrid列（供外部调用）
        /// </summary>
        public void RefreshDataGridColumns()
        {
            // 先刷新配置，确保从文件加载最新配置
            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                viewModel.UISettingsService?.RefreshConfig();
            }
            InitializeDataGridColumns();
        }

        /// <summary>
        /// 获取选中的行程项列表
        /// </summary>
        public List<Model.TripItem> GetSelectedTripItems()
        {
            var selectedItems = new List<Model.TripItem>();

            if (TripDataGrid != null && TripDataGrid.SelectedItems.Count > 0)
            {
                foreach (var item in TripDataGrid.SelectedItems)
                {
                    if (item is Model.TripItem tripItem)
                    {
                        selectedItems.Add(tripItem);
                    }
                }
            }

            return selectedItems;
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // 阻止事件冒泡，防止触发标题栏的折叠/展开
            e.Handled = true;
        }

        /// <summary>
        /// 批量修改状态菜单点击事件
        /// </summary>
        private void BatchUpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] BatchUpdateStatus_Click 被调用");
            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] 调用 viewModel.TicketMenuCommand");
                viewModel.TicketMenuCommand("BatchUpdateStatus");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext 不是 MainViewModel，实际类型: {DataContext?.GetType().Name}");
            }
        }

        /// <summary>
        /// 批量更改标签菜单点击事件
        /// </summary>
        private void BatchUpdateTag_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] BatchUpdateTag_Click 被调用");
            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] 调用 viewModel.TicketMenuCommand");
                viewModel.TicketMenuCommand("BatchUpdateTag");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext 不是 MainViewModel，实际类型: {DataContext?.GetType().Name}");
            }
        }

        private void BatchDelete_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] BatchDelete_Click 被调用");
            if (DataContext is ViewModel.MainViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] 调用 viewModel.TicketMenuCommand");
                viewModel.TicketMenuCommand("BatchDelete");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext 不是 MainViewModel，实际类型: {DataContext?.GetType().Name}");
            }
        }

        /// <summary>
        /// 新建标签菜单点击事件
        /// </summary>
        private void NewTag_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] NewTag_Click 被调用");
            var editWindow = new TagEditWindow(null);
            editWindow.Owner = this;
            editWindow.ShowDialog();
        }

        /// <summary>
        /// 管理标签菜单点击事件
        /// </summary>
        private void ManageTags_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] ManageTags_Click 被调用");
            var manageWindow = new TagManagerWindow();
            manageWindow.Owner = this;
            manageWindow.ShowDialog();
        }

        /// <summary>
        /// 左侧分割条拖动完成事件 - 同步更新ViewModel中的宽度值并保存配置
        /// </summary>
        private void LeftSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // 恢复光标
            Mouse.OverrideCursor = null;

            if (DataContext is ViewModel.MainViewModel viewModel && TopGrid != null)
            {
                // 获取左侧列的实际宽度并更新ViewModel
                double actualWidth = TopGrid.ColumnDefinitions[0].ActualWidth;
                // 限制宽度在120-800范围内
                int limitedWidth = (int)Math.Max(120, Math.Min(800, actualWidth));
                viewModel.Layout.LeftPanelWidth = limitedWidth;
                // 保存到配置文件（无论锁定还是解锁状态都保存宽度值）
                viewModel.SaveLayoutSettings();
            }
        }

        /// <summary>
        /// 左侧分割条拖动中事件 - 实时限制宽度
        /// </summary>
        private void LeftSplitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (TopGrid != null)
            {
                // 获取左侧列的当前宽度
                double currentWidth = TopGrid.ColumnDefinitions[0].ActualWidth;
                double newWidth = currentWidth + e.HorizontalChange;

                // 限制宽度在120-800范围内
                if (newWidth < 120)
                {
                    // 阻止进一步缩小
                    e.Handled = true;
                    TopGrid.ColumnDefinitions[0].Width = new GridLength(120);
                    TopGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                    // 显示禁止光标
                    Mouse.OverrideCursor = Cursors.No;
                }
                else if (newWidth > 800)
                {
                    // 阻止进一步放大
                    e.Handled = true;
                    TopGrid.ColumnDefinitions[0].Width = new GridLength(800);
                    // 显示禁止光标
                    Mouse.OverrideCursor = Cursors.No;
                }
                else
                {
                    // 恢复正常光标
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// 右侧分割条拖动完成事件 - 同步更新ViewModel中的宽度值并保存配置
        /// </summary>
        private void RightSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // 恢复光标
            Mouse.OverrideCursor = null;

            if (DataContext is ViewModel.MainViewModel viewModel && TopGrid != null)
            {
                // 获取右侧列的实际宽度并更新ViewModel
                double actualWidth = TopGrid.ColumnDefinitions[4].ActualWidth;
                // 限制宽度在180-800范围内
                int limitedWidth = (int)Math.Max(180, Math.Min(800, actualWidth));
                viewModel.Layout.RightPanelWidth = limitedWidth;
                // 保存到配置文件（无论锁定还是解锁状态都保存宽度值）
                viewModel.SaveLayoutSettings();
            }
        }

        /// <summary>
        /// 右侧分割条拖动中事件 - 实时限制宽度
        /// </summary>
        private void RightSplitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (TopGrid != null)
            {
                // 获取右侧列的当前宽度
                double currentWidth = TopGrid.ColumnDefinitions[4].ActualWidth;
                double newWidth = currentWidth - e.HorizontalChange;

                // 限制宽度在180-800范围内
                if (newWidth < 180)
                {
                    // 阻止进一步缩小
                    e.Handled = true;
                    TopGrid.ColumnDefinitions[4].Width = new GridLength(180);
                    TopGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                    // 显示禁止光标
                    Mouse.OverrideCursor = Cursors.No;
                }
                else if (newWidth > 800)
                {
                    // 阻止进一步放大
                    e.Handled = true;
                    TopGrid.ColumnDefinitions[4].Width = new GridLength(800);
                    // 显示禁止光标
                    Mouse.OverrideCursor = Cursors.No;
                }
                else
                {
                    // 恢复正常光标
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// 底部分割条拖动中事件 - 实时限制高度
        /// </summary>
        private void BottomSplitter_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (MainGrid != null)
            {
                // 获取底部行的当前高度
                double currentHeight = MainGrid.RowDefinitions[2].ActualHeight;
                double newHeight = currentHeight - e.VerticalChange;

                // 限制高度在120-400范围内
                if (newHeight < 120)
                {
                    // 阻止进一步缩小
                    e.Handled = true;
                    MainGrid.RowDefinitions[2].Height = new GridLength(120);
                    MainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    // 显示禁止光标
                    Mouse.OverrideCursor = Cursors.No;
                }
                else if (newHeight > 400)
                {
                    // 阻止进一步放大
                    e.Handled = true;
                    MainGrid.RowDefinitions[2].Height = new GridLength(400);
                    // 显示禁止光标
                    Mouse.OverrideCursor = Cursors.No;
                }
                else
                {
                    // 恢复正常光标
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// 底部分割条拖动完成事件 - 恢复光标
        /// </summary>
        private void BottomSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // 恢复光标
            Mouse.OverrideCursor = null;

            if (DataContext is ViewModel.MainViewModel viewModel && MainGrid != null)
            {
                // 获取底部行的实际高度并更新ViewModel
                double actualHeight = MainGrid.RowDefinitions[2].ActualHeight;
                // 限制高度在120-400范围内
                int limitedHeight = (int)Math.Max(120, Math.Min(400, actualHeight));
                viewModel.Layout.BottomPanelHeight = limitedHeight;
                // 保存到配置文件（无论锁定还是解锁状态都保存高度值）
                viewModel.SaveLayoutSettings();
            }
        }

        /// <summary>
        /// 更新列头排序指示器
        /// </summary>
        public void UpdateColumnSortDirection(string sortColumn, bool isDescending)
        {
            if (TripDataGrid == null) return;

            // 清除所有列的排序指示器
            foreach (var column in TripDataGrid.Columns)
            {
                column.SortDirection = null;
            }

            // 找到对应的列并设置排序指示器
            var targetColumn = TripDataGrid.Columns.FirstOrDefault(c => c.SortMemberPath == sortColumn);
            if (targetColumn != null)
            {
                targetColumn.SortDirection = isDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
        }

        /// <summary>
        /// 应用滚动条样式到ScrollViewer
        /// </summary>
        public void ApplyScrollbarStyle(string scrollbarStyle)
        {
            if (TripDataGrid == null) return;
            
            // 应用滚动条样式到DataGrid的资源
            Style? scrollBarStyle = scrollbarStyle switch
            {
                "Minimal" => System.Windows.Application.Current.FindResource("MinimalScrollBarStyle") as Style,
                "Hover" => Application.Current.FindResource("HoverScrollBarStyle") as Style,
                _ => System.Windows.Application.Current.FindResource("NormalScrollBarStyle") as Style
            };

            if (scrollBarStyle != null)
            {
                // 将滚动条样式添加到DataGrid的资源中
                if (TripDataGrid.Resources.Contains(typeof(ScrollBar)))
                {
                    TripDataGrid.Resources.Remove(typeof(ScrollBar));
                }
                TripDataGrid.Resources.Add(typeof(ScrollBar), scrollBarStyle);
            }
        }

        /// <summary>
        /// 统计配置按钮点击事件
        /// </summary>
        private void OnStatisticsConfigClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] 统计配置按钮被点击");
            // 直接调用 ViewModel 方法
            if (DataContext is ViewModel.MainViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] 调用 StatisticsConfigCommand");
                var method = typeof(ViewModel.MainViewModel).GetMethod("StatisticsConfigCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(vm, null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext 不是 MainViewModel，而是 {DataContext?.GetType().Name}");
            }
        }

        /// <summary>
        /// 刷新统计按钮点击事件
        /// </summary>
        private void OnRefreshStatisticsClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] 刷新统计按钮被点击");
            // 直接调用 ViewModel 方法
            if (DataContext is ViewModel.MainViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] 调用 RefreshStatisticsCommand");
                var method = typeof(ViewModel.MainViewModel).GetMethod("RefreshStatisticsCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(vm, null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext 不是 MainViewModel，而是 {DataContext?.GetType().Name}");
            }
        }

        /// <summary>
        /// 出发站联想项点击事件
        /// </summary>
        private void DepartStationSuggestion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string selected)
            {
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    viewModel.SearchPanel.SelectDepartStationCommand.Execute(selected);
                }
            }
        }

        /// <summary>
        /// 到达站联想项点击事件
        /// </summary>
        private void ArriveStationSuggestion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string selected)
            {
                if (DataContext is ViewModel.MainViewModel viewModel)
                {
                    viewModel.SearchPanel.SelectArriveStationCommand.Execute(selected);
                }
            }
        }
    }

    // 辅助类：日志数据项
    public class DataGridItem
    {
        public string Time { get; set; }
        public string Content { get; set; }

        public DataGridItem()
        {
        }

        public DataGridItem(string time, string content)
        {
            Time = time;
            Content = content;
        }
    }

    // 辅助类：行程数据项
    public class TripItem
    {
        public int Id { get; set; }
        public string TrainNo { get; set; }
        public string DepartStation { get; set; }
        public string ArriveStation { get; set; }
        public string DepartDate { get; set; }
        public string DepartTime { get; set; }
        public string SeatType { get; set; }
        public string Money { get; set; }
        public string Status { get; set; }

        public TripItem()
        {
        }

        public TripItem(int id, string trainNo, string departStation, string arriveStation, string departDate, string departTime, string seatType, string money, string status)
        {
            Id = id;
            TrainNo = trainNo;
            DepartStation = departStation;
            ArriveStation = arriveStation;
            DepartDate = departDate;
            DepartTime = departTime;
            SeatType = seatType;
            Money = money;
            Status = status;
        }
    }
}