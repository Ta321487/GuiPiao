using GuiPiao.Model;
using GuiPiao.ViewModel;
using GuiPiao.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GuiPiao.View
{
    /// <summary>
    /// DashboardSettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class DashboardSettingsView : UserControl
    {
        private DragDropHelper? _dragDropHelper;

        public DashboardSettingsView()
        {
            InitializeComponent();
            InitializeDragDrop();
        }

        /// <summary>
        /// 初始化拖拽排序功能
        /// </summary>
        private void InitializeDragDrop()
        {
            _dragDropHelper = new DragDropHelper(
                onDropAction: (sourceIndex, targetIndex) =>
                {
                    if (DataContext is not DashboardSettingsViewModel viewModel) return;

                    // 移动卡片位置
                    DragDropHelper.MoveItem(viewModel.Cards, sourceIndex, targetIndex);

                    // 更新 SortOrder
                    for (int i = 0; i < viewModel.Cards.Count; i++)
                    {
                        viewModel.Cards[i].SortOrder = i;
                    }

                    // 刷新 DataGrid
                    CardsDataGrid.Items.Refresh();
                }
            );
        }

        private void GlobalTimeRangeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (DataContext is not DashboardSettingsViewModel viewModel) return;

            if (viewModel.GlobalTimeRange != TimeRangeType.CustomRange) return;

            // 使用 Dispatcher 延迟打开对话框，确保下拉菜单先关闭
            Dispatcher.BeginInvoke(new Action(() =>
            {
                OpenCustomTimeRangeDialog();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OpenCustomTimeRangeDialog()
        {
            if (DataContext is not DashboardSettingsViewModel viewModel) return;

            var window = new CustomTimeRangeWindow(
                viewModel.GlobalCustomStartDate,
                viewModel.GlobalCustomEndDate,
                string.Empty); // 全局配置不显示时间粒度建议
            // 不设置 Owner，避免最小化时影响主窗口

            var result = window.ShowDialog();
            if (result == true)
            {
                viewModel.GlobalCustomStartDate = window.SelectedStartDate;
                viewModel.GlobalCustomEndDate = window.SelectedEndDate;
            }
        }

        #region 拖拽排序事件处理

        /// <summary>
        /// 鼠标左键按下时记录拖拽起始行
        /// </summary>
        private void CardsDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragDropHelper?.OnPreviewMouseLeftButtonDown(sender, e, (originalSource) =>
            {
                return DragDropHelper.FindVisualParent<DataGridRow>(originalSource);
            });
        }

        /// <summary>
        /// 鼠标移动时开始拖拽
        /// </summary>
        private void CardsDataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            _dragDropHelper?.OnMouseMove(sender, e);
        }

        /// <summary>
        /// 拖拽悬停时更新视觉效果
        /// </summary>
        private void CardsDataGrid_DragOver(object sender, DragEventArgs e)
        {
            _dragDropHelper?.OnDragOver(sender, e);
        }

        /// <summary>
        /// 放置时更新排序
        /// </summary>
        private void CardsDataGrid_Drop(object sender, DragEventArgs e)
        {
            _dragDropHelper?.OnDrop(sender, e, (originalSource) =>
            {
                return DragDropHelper.FindVisualParent<DataGridRow>(originalSource);
            });
        }

        #endregion

        /// <summary>
        /// 卡片间距输入框失去焦点时验证输入
        /// </summary>
        private void CardSpacingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DashboardSettingsViewModel viewModel) return;

            var textBox = sender as TextBox;
            if (textBox == null) return;

            // 尝试解析输入值
            if (!int.TryParse(textBox.Text, out int inputValue))
            {
                // 输入不是有效数字
                MessageBoxWindow.Show(
                    Application.Current.MainWindow,
                    "请输入有效的数字（0-100）",
                    "输入错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                // 恢复为当前值
                textBox.Text = viewModel.CardSpacing.ToString();
                return;
            }

            // 检查范围
            if (inputValue < 0 || inputValue > 100)
            {
                // 超出范围
                MessageBoxWindow.Show(
                    Application.Current.MainWindow,
                    "卡片间距必须在 0-100 之间",
                    "输入错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                // 恢复为当前值
                textBox.Text = viewModel.CardSpacing.ToString();
                return;
            }

            // 有效值已通过绑定自动更新到 ViewModel
            // 确保显示的是实际设置的值（可能被限制后的值）
            textBox.Text = viewModel.CardSpacing.ToString();
        }
    }
}
