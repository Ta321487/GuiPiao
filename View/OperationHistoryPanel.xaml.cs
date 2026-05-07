using GuiPiao.Model;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GuiPiao.View
{
    /// <summary>
    /// 操作历史面板控件
    /// </summary>
    public partial class OperationHistoryPanel : UserControl
    {
        private ObservableCollection<OperationHistoryItem>? _historyItems;

        public OperationHistoryPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置历史记录集合
        /// </summary>
        public void SetHistoryItems(ObservableCollection<OperationHistoryItem> items)
        {
            _historyItems = items;
            HistoryItemsControl.ItemsSource = _historyItems;
        }

        private void OnExpandClick(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Visible;
            ExpandButton.Visibility = Visibility.Collapsed;
        }

        private void OnCollapseClick(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Collapsed;
            ExpandButton.Visibility = Visibility.Visible;
        }
    }
}
