using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TripItemModel = GuiPiao.Model.TripItem;

namespace GuiPiao.View
{
    /// <summary>
    /// 批量修改状态窗口
    /// </summary>
    public partial class BatchUpdateStatusWindow : Window
    {
        /// <summary>
        /// 可编辑的车票列表（状态为未出行或已完成）
        /// </summary>
        public List<TripItemModel> EditableTickets { get; private set; }

        /// <summary>
        /// 被跳过的车票列表（状态为已改签或已退票）
        /// </summary>
        public List<TripItemModel> SkippedTickets { get; private set; }

        /// <summary>
        /// 选中的目标状态（0=未出行, 1=已完成）
        /// </summary>
        public int SelectedStatus { get; private set; }

        /// <summary>
        /// 是否点击了确定按钮
        /// </summary>
        public bool IsConfirmed { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="selectedTickets">所有选中的车票</param>
        public BatchUpdateStatusWindow(List<TripItemModel> selectedTickets)
        {
            InitializeComponent();

            // 分离可编辑和被跳过的车票
            EditableTickets = selectedTickets.Where(t => t.Status == 0 || t.Status == 1).ToList();
            SkippedTickets = selectedTickets.Where(t => t.Status == 2 || t.Status == 3).ToList();

            // 绑定数据
            TicketsDataGrid.ItemsSource = EditableTickets;

            // 显示跳过的车票提示
            if (SkippedTickets.Count > 0)
            {
                SkippedBorder.Visibility = Visibility.Visible;
                var skippedInfo = string.Join(", ", SkippedTickets.Select(t => $"{t.TrainNo}({GetStatusDisplay(t.Status)})"));
                SkippedTextBlock.Text = $"以下 {SkippedTickets.Count} 张车票已改签或已退票，将被跳过：{skippedInfo}";
            }

            // 默认选中"已完成"（假设用户通常要将未出行的改为已完成）
            CompletedRadio.IsChecked = true;
        }

        /// <summary>
        /// 获取状态显示文本
        /// </summary>
        private string GetStatusDisplay(int status)
        {
            return status switch
            {
                0 => "未出行",
                1 => "已完成",
                2 => "已改签",
                3 => "已退票",
                _ => "未知"
            };
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditableTickets.Count == 0)
            {
                MessageBoxWindow.Show("没有可以修改状态的车票", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DialogResult = false;
                Close();
                return;
            }

            // 获取选中的状态
            SelectedStatus = NotTraveledRadio.IsChecked == true ? 0 : 1;

            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
