using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using GuiPiao.Model;
using GuiPiao.ViewModel.TrainTicketForm;

namespace GuiPiao.View;

/// <summary>
///     火车票表单用户控件，用于添加和编辑车票的共享UI
/// </summary>
public partial class TrainTicketFormView : UserControl
{
    public TrainTicketFormView()
    {
        InitializeComponent();
        // 订阅数据变化以更新标签选中状态
        DataContextChanged += OnDataContextChanged;
        // 订阅容器生成状态变化，确保容器生成后更新标签状态
        TagsItemsControl.ItemContainerGenerator.StatusChanged += OnItemContainerGeneratorStatusChanged;
    }

    /// <summary>
    ///     容器生成状态变化事件处理
    /// </summary>
    private void OnItemContainerGeneratorStatusChanged(object? sender, EventArgs e)
    {
        if (TagsItemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            UpdateTagVisualStates();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TrainTicketFormViewModelBase oldVm) oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is TrainTicketFormViewModelBase newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            // 初始更新标签状态（如果容器已生成）
            UpdateTagVisualStates();
        }
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TrainTicketFormViewModelBase.SelectedTagIds) ||
            e.PropertyName == nameof(TrainTicketFormViewModelBase.AvailableTags))
            UpdateTagVisualStates();
    }

    /// <summary>
    ///     更新所有标签的视觉状态
    /// </summary>
    private void UpdateTagVisualStates()
    {
        if (DataContext is not TrainTicketFormViewModelBase viewModel)
        {
            Debug.WriteLine("[UpdateTagVisualStates] DataContext 为空，跳过更新");
            return;
        }

        // 获取 ItemsControl 的容器
        if (TagsItemsControl.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
        {
            Debug.WriteLine("[UpdateTagVisualStates] 容器未生成，跳过更新");
            return;
        }

        Debug.WriteLine(
            $"[UpdateTagVisualStates] 开始更新，AvailableTags 数量={viewModel.AvailableTags.Count}, SelectedTagIds=[{string.Join(",", viewModel.SelectedTagIds)}]");

        var updatedCount = 0;
        foreach (var tag in viewModel.AvailableTags)
        {
            Debug.WriteLine($"[UpdateTagVisualStates] 处理标签: {tag.Name}(Id={tag.Id})");

            var container = TagsItemsControl.ItemContainerGenerator.ContainerFromItem(tag) as ContentPresenter;
            if (container == null)
            {
                Debug.WriteLine($"[UpdateTagVisualStates] 标签 {tag.Name} 的容器为空");
                continue;
            }

            Debug.WriteLine($"[UpdateTagVisualStates] 标签 {tag.Name} 的容器类型: {container.GetType().Name}");

            // 等待容器加载完成
            container.ApplyTemplate();

            var border = FindVisualChild<Border>(container);
            if (border == null)
            {
                Debug.WriteLine($"[UpdateTagVisualStates] 标签 {tag.Name} 的 Border 为空，尝试查找所有子元素...");
                // 打印视觉树以便调试
                PrintVisualTree(container, 0);
                continue;
            }

            var isSelected = viewModel.SelectedTagIds.Contains(tag.Id);
            Debug.WriteLine($"[UpdateTagVisualStates] 标签 {tag.Name}(Id={tag.Id}): isSelected={isSelected}, 更新样式");
            UpdateTagBorderStyle(border, isSelected);
            updatedCount++;
        }

        Debug.WriteLine($"[UpdateTagVisualStates] 更新完成，共更新 {updatedCount} 个标签");
    }

    /// <summary>
    ///     打印视觉树用于调试
    /// </summary>
    private void PrintVisualTree(DependencyObject parent, int depth)
    {
        var indent = new string(' ', depth * 2);
        Debug.WriteLine($"{indent}{parent.GetType().Name}");

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            PrintVisualTree(child, depth + 1);
        }
    }

    /// <summary>
    ///     标签点击事件处理
    /// </summary>
    private void OnTagClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;

        // 找到对应的 TicketTag
        var tag = FindTagFromBorder(border);
        if (tag == null) return;

        if (DataContext is TrainTicketFormViewModelBase viewModel)
        {
            Debug.WriteLine($"[OnTagClicked] 点击标签: Id={tag.Id}, Name={tag.Name}");
            Debug.WriteLine($"[OnTagClicked] 点击前 SelectedTagIds: [{string.Join(",", viewModel.SelectedTagIds)}]");

            // 使用命令切换标签选择状态（支持撤销重做）
            // UI 更新由 OnViewModelPropertyChanged 统一处理
            viewModel.ToggleTagSelectionCommand.Execute(tag.Id);

            Debug.WriteLine($"[OnTagClicked] 点击后 SelectedTagIds: [{string.Join(",", viewModel.SelectedTagIds)}]");
        }

        e.Handled = true;
    }

    /// <summary>
    ///     从 Border 找到对应的 TicketTag
    /// </summary>
    private TicketTag? FindTagFromBorder(Border border)
    {
        // 通过 DataContext 获取 Tag
        if (border.DataContext is TicketTag tag)
            return tag;
        return null;
    }

    /// <summary>
    ///     更新标签边框的样式
    /// </summary>
    private void UpdateTagBorderStyle(Border border, bool isSelected)
    {
        Debug.WriteLine($"[UpdateTagBorderStyle] 设置 isSelected={isSelected}, border.Name={border.Name}");
        if (isSelected)
        {
            border.Opacity = 1.0;
            border.BorderBrush = (Brush)FindResource("AccentBrush");
            Debug.WriteLine("[UpdateTagBorderStyle] 已设置选中样式: Opacity=1.0, BorderBrush=AccentBrush");
        }
        else
        {
            border.Opacity = 0.5;
            border.BorderBrush = Brushes.Transparent;
            Debug.WriteLine("[UpdateTagBorderStyle] 已设置未选中样式: Opacity=0.5, BorderBrush=Transparent");
        }
    }

    /// <summary>
    ///     查找视觉树中的子元素
    /// </summary>
    private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
                return descendant;
        }

        return null;
    }

    /// <summary>
    ///     提示信息 ComboBox 选择变更事件
    /// </summary>
    private void HintComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && DataContext is TrainTicketFormViewModelBase viewModel)
            // 如果选择了"自定义"，显示输入对话框
            if (comboBox.SelectedItem?.ToString() == "自定义")
            {
                var dialog = new InputDialogWindow("请输入自定义提示信息", "自定义提示", viewModel.Hint);
                if (dialog.ShowDialog() == true)
                {
                    var newHint = dialog.InputText;
                    viewModel.Hint = newHint;
                    // 如果新值不在选项列表中，添加它
                    if (!viewModel.HintOptions.Contains(newHint))
                    {
                        var customIndex = viewModel.HintOptions.IndexOf("自定义");
                        if (customIndex >= 0)
                            viewModel.HintOptions.Insert(customIndex, newHint);
                        else
                            viewModel.HintOptions.Add(newHint);
                    }

                    viewModel.SelectedHint = newHint;
                }
                else
                {
                    // 取消时恢复之前的选中项
                    comboBox.SelectedItem = viewModel.Hint;
                }
            }
    }

    /// <summary>
    ///     车次号输入验证 - 只允许输入数字
    /// </summary>
    private void TrainNoNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 只允许输入数字
        e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
    }
}