using System.Collections.Generic;
using System.Windows;
using GuiPiao.Model;
using TripItemModel = GuiPiao.Model.TripItem;

namespace GuiPiao.View;

/// <summary>
///     批量更改标签窗口
/// </summary>
public partial class BatchUpdateTagWindow : Window
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="selectedTickets">选中的车票</param>
    /// <param name="allTags">所有可用标签</param>
    public BatchUpdateTagWindow(List<TripItemModel> selectedTickets, List<TicketTag> allTags)
    {
        InitializeComponent();

        SelectedTickets = selectedTickets;

        // 绑定数据
        TicketsDataGrid.ItemsSource = SelectedTickets;

        // 绑定标签列表
        TagComboBox.ItemsSource = allTags;

        // 如果没有标签，禁用确定按钮
        if (allTags == null || allTags.Count == 0) TagComboBox.IsEnabled = false;
    }

    /// <summary>
    ///     选中的车票列表
    /// </summary>
    public List<TripItemModel> SelectedTickets { get; }

    /// <summary>
    ///     选中的标签ID
    /// </summary>
    public int? SelectedTagId { get; private set; }

    /// <summary>
    ///     是否点击了确定按钮
    /// </summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>
    ///     标签更新模式：true为追加模式，false为替换模式
    /// </summary>
    public bool IsAppendMode { get; private set; }

    /// <summary>
    ///     确定按钮点击事件
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedTickets.Count == 0)
        {
            MessageBoxWindow.Show("没有选中的车票", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            DialogResult = false;
            Close();
            return;
        }

        // 获取选中的标签
        if (TagComboBox.SelectedValue != null)
        {
            SelectedTagId = (int)TagComboBox.SelectedValue;
        }
        else
        {
            MessageBoxWindow.Show("请选择一个标签", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 获取更新模式
        IsAppendMode = AppendRadioButton.IsChecked == true;

        IsConfirmed = true;
        DialogResult = true;
        Close();
    }

    /// <summary>
    ///     取消按钮点击事件
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        DialogResult = false;
        Close();
    }
}