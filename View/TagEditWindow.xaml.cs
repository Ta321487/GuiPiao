using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GuiPiao.DataAccess;
using GuiPiao.Model;

namespace GuiPiao.View;

/// <summary>
///     标签编辑窗口（新建/编辑）
/// </summary>
public partial class TagEditWindow : Window
{
    // 预定义颜色列表
    private static readonly List<ColorOption> PresetColors = new()
    {
        new ColorOption { Name = "蓝色", Value = "#2196F3" },
        new ColorOption { Name = "绿色", Value = "#4CAF50" },
        new ColorOption { Name = "橙色", Value = "#FF9800" },
        new ColorOption { Name = "红色", Value = "#F44336" },
        new ColorOption { Name = "紫色", Value = "#9C27B0" },
        new ColorOption { Name = "青色", Value = "#00BCD4" },
        new ColorOption { Name = "粉色", Value = "#E91E63" },
        new ColorOption { Name = "灰色", Value = "#9E9E9E" },
        new ColorOption { Name = "黄色", Value = "#FFEB3B" },
        new ColorOption { Name = "浅绿", Value = "#8BC34A" }
    };

    private readonly TicketTag _existingTag;
    private readonly TicketTagRepository _tagRepository;
    private string _selectedColor = "#2196F3";
    private string _selectedTextColor = "#FFFFFF";

    public TagEditWindow(TicketTag existingTag)
    {
        InitializeComponent();
        _tagRepository = new TicketTagRepository();
        _existingTag = existingTag;

        // 初始化颜色选项
        InitializeColorOptions();

        // 如果是编辑模式，填充现有数据
        if (existingTag != null)
        {
            Title = "编辑标签";
            TitleTextBlock.Text = "编辑标签";
            NameTextBox.Text = existingTag.Name;
            _selectedColor = existingTag.Color;
            _selectedTextColor = existingTag.TextColor;

            // 设置文字颜色单选按钮
            if (existingTag.TextColor == "#000000" || existingTag.TextColor?.ToUpper() == "#000000")
                BlackTextRadio.IsChecked = true;
            else
                WhiteTextRadio.IsChecked = true;

            // 设置默认标签状态
            DefaultTagCheckBox.IsChecked = existingTag.IsDefault;
        }
        else
        {
            Title = "新建标签";
            TitleTextBlock.Text = "新建标签";
            WhiteTextRadio.IsChecked = true;
            DefaultTagCheckBox.IsChecked = false;
        }

        // 订阅文本变化事件
        NameTextBox.TextChanged += (s, e) => UpdatePreview();
        WhiteTextRadio.Checked += (s, e) =>
        {
            _selectedTextColor = "#FFFFFF";
            UpdatePreview();
        };
        BlackTextRadio.Checked += (s, e) =>
        {
            _selectedTextColor = "#000000";
            UpdatePreview();
        };

        // 初始更新预览
        UpdatePreview();
    }

    /// <summary>
    ///     初始化颜色选项
    /// </summary>
    private void InitializeColorOptions()
    {
        foreach (var color in PresetColors)
        {
            var border = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color.Value)),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand,
                Tag = color.Value
            };

            // 如果匹配当前选中的颜色，添加边框
            if (color.Value == _selectedColor)
            {
                border.BorderBrush = new SolidColorBrush(Colors.White);
                border.BorderThickness = new Thickness(2);
            }

            border.MouseLeftButtonDown += (s, e) =>
            {
                // 清除其他选中状态
                foreach (var child in ColorPanel.Children)
                    if (child is Border b)
                        b.BorderThickness = new Thickness(0);

                // 设置当前选中
                border.BorderBrush = new SolidColorBrush(Colors.White);
                border.BorderThickness = new Thickness(2);
                _selectedColor = color.Value;
                UpdatePreview();
            };

            ColorPanel.Children.Add(border);
        }
    }

    /// <summary>
    ///     更新预览
    /// </summary>
    private void UpdatePreview()
    {
        PreviewBorder.Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(_selectedColor));
        PreviewTextBlock.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(_selectedTextColor));
        PreviewTextBlock.Text = string.IsNullOrWhiteSpace(NameTextBox.Text)
            ? "标签预览"
            : NameTextBox.Text;
    }

    /// <summary>
    ///     确定按钮点击事件
    /// </summary>
    private async void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // 验证标签名称
        var name = NameTextBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBoxWindow.Show("请输入标签名称", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (name.Length > 20)
        {
            MessageBoxWindow.Show("标签名称不能超过20个字符", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // 检查名称是否重复
            var allTags = await _tagRepository.GetAllTagsAsync();
            var duplicateTag = allTags.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                (_existingTag == null || t.Id != _existingTag.Id));

            if (duplicateTag != null)
            {
                MessageBoxWindow.Show("标签名称已存在，请使用其他名称", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 处理默认标签逻辑
            var isDefault = DefaultTagCheckBox.IsChecked ?? false;
            if (isDefault)
                // 如果设置为默认标签，先清除其他标签的默认标记
                await _tagRepository.ClearAllDefaultTagsAsync();

            // 保存标签
            if (_existingTag == null)
            {
                // 新建
                var newTag = new TicketTag
                {
                    Name = name,
                    Color = _selectedColor,
                    TextColor = _selectedTextColor,
                    SortOrder = allTags.Count(),
                    IsDefault = isDefault
                };
                await _tagRepository.AddTagAsync(newTag);
                MessageBoxWindow.Show("标签添加成功");
            }
            else
            {
                // 编辑
                _existingTag.Name = name;
                _existingTag.Color = _selectedColor;
                _existingTag.TextColor = _selectedTextColor;
                _existingTag.IsDefault = isDefault;
                await _tagRepository.UpdateTagAsync(_existingTag);
                MessageBoxWindow.Show("标签修改成功");
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBoxWindow.Show($"保存标签失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     取消按钮点击事件
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    ///     颜色选项
    /// </summary>
    private class ColorOption
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}