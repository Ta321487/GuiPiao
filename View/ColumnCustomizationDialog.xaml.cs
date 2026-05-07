using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GuiPiao.Model;
using GuiPiao.View;

namespace GuiPiao.Views;

/// <summary>
///     ColumnCustomizationDialog.xaml 的交互逻辑
/// </summary>
public partial class ColumnCustomizationDialog : Window
{
    private DataGridColumnConfig? _draggedItem;
    private Border? _dragSource;

    private Point _dragStartPoint;

    public ColumnCustomizationDialog(List<DataGridColumnConfig> columnConfigs)
    {
        InitializeComponent();

        // 深拷贝列配置
        ColumnConfigs = columnConfigs.Select(c => new DataGridColumnConfig
        {
            FieldName = c.FieldName,
            Header = c.Header,
            IsVisible = c.IsVisible,
            DisplayOrder = c.DisplayOrder,
            Width = c.Width,
            MinWidth = c.MinWidth,
            CanSort = c.CanSort,
            IsReadOnly = c.IsReadOnly
        }).ToList();

        ColumnsListBox.ItemsSource = ColumnConfigs;
    }

    /// <summary>
    ///     列配置列表
    /// </summary>
    public List<DataGridColumnConfig> ColumnConfigs { get; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // 检查是否至少选择了一个字段
        if (!ColumnConfigs.Any(c => c.IsVisible))
        {
            MessageBoxWindow.Show(this, "请至少选择一个字段", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 验证所有宽度值
        foreach (var config in ColumnConfigs)
        {
            if (string.IsNullOrWhiteSpace(config.Width))
            {
                MessageBoxWindow.Show(this, $"字段 [{config.Header}] 的宽度不能为空", "输入错误", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 检查是否为 * 或数字
            if (config.Width != "*")
            {
                if (!double.TryParse(config.Width, out var widthValue))
                {
                    MessageBoxWindow.Show(this, $"字段 [{config.Header}] 的宽度只能输入数字或*", "输入错误", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (widthValue < 10)
                {
                    MessageBoxWindow.Show(this, $"字段 [{config.Header}] 的宽度不能小于10像素", "输入错误", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (widthValue > 1000)
                {
                    MessageBoxWindow.Show(this, $"字段 [{config.Header}] 的宽度不能大于1000像素", "输入错误", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #region 拖拽排序

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _dragSource = sender as Border;
        _draggedItem = _dragSource?.DataContext as DataGridColumnConfig;
    }

    private void Border_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
        {
            var currentPosition = e.GetPosition(null);
            var diff = _dragStartPoint - currentPosition;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                // 开始拖拽
                DragDrop.DoDragDrop(_dragSource!, _draggedItem, DragDropEffects.Move);
        }
    }

    private void Border_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(DataGridColumnConfig)))
        {
            e.Effects = DragDropEffects.Move;
            var border = sender as Border;
            if (border != null)
            {
                // 使用主题色的淡色版本作为拖拽悬停背景
                var accentBrush = FindResource("AccentBrush") as SolidColorBrush;
                if (accentBrush != null)
                {
                    var hoverColor = accentBrush.Color;
                    hoverColor.A = 60; // 设置透明度为约25%
                    border.Background = new SolidColorBrush(hoverColor);
                }
                else
                {
                    border.Background = new SolidColorBrush(Colors.Gray) { Opacity = 0.3 };
                }
            }
        }
    }

    private void Border_DragLeave(object sender, DragEventArgs e)
    {
        var border = sender as Border;
        if (border != null) border.Background = (Brush)FindResource("ContentBackgroundBrush");
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        var targetBorder = sender as Border;
        var targetItem = targetBorder?.DataContext as DataGridColumnConfig;

        // 恢复样式
        if (targetBorder != null) targetBorder.Background = (Brush)FindResource("ContentBackgroundBrush");

        if (targetItem == null || _draggedItem == null || targetItem == _draggedItem)
            return;

        // 交换位置
        var sourceIndex = ColumnConfigs.IndexOf(_draggedItem);
        var targetIndex = ColumnConfigs.IndexOf(targetItem);

        if (sourceIndex >= 0 && targetIndex >= 0)
        {
            ColumnConfigs.RemoveAt(sourceIndex);
            ColumnConfigs.Insert(targetIndex, _draggedItem);

            // 更新 DisplayOrder
            for (var i = 0; i < ColumnConfigs.Count; i++) ColumnConfigs[i].DisplayOrder = i;

            // 刷新列表
            ColumnsListBox.Items.Refresh();
        }

        _draggedItem = null;
        _dragSource = null;
    }

    /// <summary>
    ///     处理鼠标滚轮事件，将滚动传递给 ScrollViewer
    /// </summary>
    private void ColumnsListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 将滚轮事件传递给 ScrollViewer
        var scrollViewer = ColumnsScrollViewer;
        if (scrollViewer != null)
        {
            if (e.Delta > 0)
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 30);
            else
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 30);
            e.Handled = true;
        }
    }

    #endregion

    #region 输入验证

    /// <summary>
    ///     预览文本输入，只允许数字和*
    /// </summary>
    private void WidthTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 只允许输入数字或*
        if (!IsValidWidthInput(e.Text)) e.Handled = true;
    }

    /// <summary>
    ///     粘贴事件处理
    /// </summary>
    private void WidthTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string));
            if (!IsValidWidthInput(pastedText)) e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    /// <summary>
    ///     验证宽度输入是否有效
    /// </summary>
    private bool IsValidWidthInput(string input)
    {
        foreach (var c in input)
            // 只允许数字和*
            if (!char.IsDigit(c) && c != '*')
                return false;

        return true;
    }

    #endregion
}