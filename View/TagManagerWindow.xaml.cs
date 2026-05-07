using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GuiPiao.DataAccess;
using GuiPiao.Model;

namespace GuiPiao.View;

/// <summary>
///     标签管理窗口
/// </summary>
public partial class TagManagerWindow : Window
{
    private readonly TicketTagRepository _tagRepository;
    private List<TicketTag> _allTags;

    public TagManagerWindow()
    {
        InitializeComponent();
        _tagRepository = new TicketTagRepository();
        _allTags = new List<TicketTag>();
        Loaded += TagManagerWindow_Loaded;
    }

    private async void TagManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTagsAsync();
    }

    /// <summary>
    ///     加载标签列表
    /// </summary>
    private async Task LoadTagsAsync()
    {
        try
        {
            Debug.WriteLine("[TagManagerWindow] 开始加载标签...");
            var tags = await _tagRepository.GetAllTagsAsync();
            _allTags = tags.ToList();
            Debug.WriteLine($"[TagManagerWindow] 加载到 {_allTags.Count} 个标签");
            foreach (var tag in _allTags) Debug.WriteLine($"[TagManagerWindow] 标签: Id={tag.Id}, Name={tag.Name}");

            // 使用 Dispatcher 确保在 UI 线程上更新
            Dispatcher.Invoke(() =>
            {
                FilterTags();
                Debug.WriteLine($"[TagManagerWindow] FilterTags 完成，DataGrid.Items.Count={TagsDataGrid.Items.Count}");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TagManagerWindow] 加载标签失败: {ex.Message}");
            MessageBoxWindow.Show($"加载标签失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     根据搜索关键字过滤标签
    /// </summary>
    private void FilterTags()
    {
        var keyword = SearchTextBox.Text?.Trim().ToLower() ?? "";
        Debug.WriteLine($"[TagManagerWindow] FilterTags: keyword='{keyword}', _allTags.Count={_allTags.Count}");

        var filteredTags = string.IsNullOrEmpty(keyword)
            ? _allTags
            : _allTags.Where(t => t.Name.ToLower().Contains(keyword)).ToList();

        Debug.WriteLine($"[TagManagerWindow] FilterTags: filteredTags.Count={filteredTags.Count}");

        TagsDataGrid.ItemsSource = null; // 先清空，强制刷新
        TagsDataGrid.ItemsSource = filteredTags;
        CountTextBlock.Text = $"共 {filteredTags.Count} 个标签";

        Debug.WriteLine("[TagManagerWindow] FilterTags: ItemsSource 已设置");
    }

    /// <summary>
    ///     搜索按钮点击事件
    /// </summary>
    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        FilterTags();
    }

    /// <summary>
    ///     新建标签按钮点击事件
    /// </summary>
    private async void NewTagButton_Click(object sender, RoutedEventArgs e)
    {
        var editWindow = new TagEditWindow(null);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true) await LoadTagsAsync();
    }

    /// <summary>
    ///     编辑按钮点击事件
    /// </summary>
    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TicketTag tag)
        {
            var editWindow = new TagEditWindow(tag);
            editWindow.Owner = this;
            if (editWindow.ShowDialog() == true) await LoadTagsAsync();
        }
    }

    /// <summary>
    ///     删除按钮点击事件
    /// </summary>
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TicketTag tag)
        {
            var result = MessageBoxWindow.Show(
                $"确定要删除标签\"{tag.Name}\"吗？\n\n注意：删除后，已关联的车票将失去此标签。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
                try
                {
                    await _tagRepository.DeleteTagAsync(tag.Id);
                    await LoadTagsAsync();
                }
                catch (Exception ex)
                {
                    MessageBoxWindow.Show($"删除标签失败：{ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
        }
    }

    /// <summary>
    ///     置顶按钮点击事件
    /// </summary>
    private async void MoveTopButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TicketTag tag)
            try
            {
                await _tagRepository.MoveTagToTopAsync(tag.Id);
                await _tagRepository.ReorganizeSortOrderAsync();
                await LoadTagsAsync();
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show($"置顶标签失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
    }

    /// <summary>
    ///     关闭按钮点击事件
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}