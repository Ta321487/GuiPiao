using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GuiPiao.Model;

namespace GuiPiao.View;

/// <summary>
///     自定义时段编辑器
/// </summary>
public partial class CustomTimePeriodEditor : UserControl
{
    /// <summary>
    ///     是否显示颜色指示器（仅在饼图时显示）
    /// </summary>
    public static readonly DependencyProperty ShowColorIndicatorProperty =
        DependencyProperty.Register(
            nameof(ShowColorIndicator),
            typeof(bool),
            typeof(CustomTimePeriodEditor),
            new PropertyMetadata(true, OnShowColorIndicatorChanged));

    private CustomTimePeriod? _editingPeriod;
    private List<CustomTimePeriod> _periods = new();

    public CustomTimePeriodEditor()
    {
        InitializeComponent();
        LoadDefaultPeriods();
    }

    public bool ShowColorIndicator
    {
        get => (bool)GetValue(ShowColorIndicatorProperty);
        set => SetValue(ShowColorIndicatorProperty, value);
    }

    /// <summary>
    ///     时段列表变更事件
    /// </summary>
    public event EventHandler<List<CustomTimePeriod>>? PeriodsChanged;

    /// <summary>
    ///     ShowColorIndicator 属性变更回调
    /// </summary>
    private static void OnShowColorIndicatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomTimePeriodEditor editor)
            editor.Dispatcher.BeginInvoke(() => editor.UpdateColorIndicatorsVisibility(), DispatcherPriority.Render);
    }

    /// <summary>
    ///     获取当前时段列表
    /// </summary>
    public List<CustomTimePeriod> GetPeriods()
    {
        return _periods.OrderBy(p => p.StartTotalMinutes).ToList();
    }

    /// <summary>
    ///     设置时段列表
    /// </summary>
    public void SetPeriods(List<CustomTimePeriod> periods)
    {
        _periods = new List<CustomTimePeriod>(periods);
        RefreshPeriodsList();

        // 根据时段内容自动选择预设模板
        AutoSelectTemplate(periods);
    }

    /// <summary>
    ///     根据时段内容自动选择预设模板
    /// </summary>
    private void AutoSelectTemplate(List<CustomTimePeriod> periods)
    {
        if (periods == null || periods.Count == 0)
        {
            TemplateComboBox.SelectedIndex = 0; // 默认4段
            return;
        }

        // 检查是否匹配4段模板
        if (IsMatchTemplate(periods, new[] { (0, 6), (6, 12), (12, 18), (18, 24) }))
        {
            TemplateComboBox.SelectedIndex = 0; // 4段
            return;
        }

        // 检查是否匹配6段模板
        if (IsMatchTemplate(periods, new[] { (0, 4), (4, 8), (8, 12), (12, 16), (16, 20), (20, 24) }))
        {
            TemplateComboBox.SelectedIndex = 1; // 6段
            return;
        }

        // 检查是否匹配通勤模式模板
        if (IsMatchTemplate(periods, new[] { (0, 6), (6, 9), (9, 12), (12, 14), (14, 18), (18, 21), (21, 24) }))
        {
            TemplateComboBox.SelectedIndex = 2; // 通勤模式
            return;
        }

        // 不匹配任何模板，保持当前选择或显示为自定义
        TemplateComboBox.SelectedIndex = -1; // 不选择任何模板
    }

    /// <summary>
    ///     检查时段列表是否匹配模板
    /// </summary>
    private bool IsMatchTemplate(List<CustomTimePeriod> periods, (int start, int end)[] template)
    {
        if (periods.Count != template.Length)
            return false;

        var sortedPeriods = periods.OrderBy(p => p.StartTotalMinutes).ToList();

        for (var i = 0; i < template.Length; i++)
        {
            var period = sortedPeriods[i];
            var (expectedStart, expectedEnd) = template[i];

            if (period.StartHour != expectedStart || period.EndHour != expectedEnd ||
                period.StartMinute != 0 || period.EndMinute != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    ///     加载默认时段（4段）
    /// </summary>
    private void LoadDefaultPeriods()
    {
        _periods = new List<CustomTimePeriod>
        {
            new() { Name = "凌晨", StartHour = 0, StartMinute = 0, EndHour = 6, EndMinute = 0, Color = "#1976D2" },
            new() { Name = "上午", StartHour = 6, StartMinute = 0, EndHour = 12, EndMinute = 0, Color = "#388E3C" },
            new() { Name = "下午", StartHour = 12, StartMinute = 0, EndHour = 18, EndMinute = 0, Color = "#FBC02D" },
            new() { Name = "晚上", StartHour = 18, StartMinute = 0, EndHour = 24, EndMinute = 0, Color = "#D32F2F" }
        };
        RefreshPeriodsList();
    }

    /// <summary>
    ///     刷新时段列表显示
    /// </summary>
    private void RefreshPeriodsList()
    {
        PeriodsItemsControl.ItemsSource = null;
        PeriodsItemsControl.ItemsSource = _periods.OrderBy(p => p.StartTotalMinutes).ToList();

        // 延迟更新颜色指示器可见性（等待UI渲染完成）
        Dispatcher.BeginInvoke(() => UpdateColorIndicatorsVisibility(), DispatcherPriority.Render);
    }

    /// <summary>
    ///     更新所有颜色指示器的可见性
    /// </summary>
    private void UpdateColorIndicatorsVisibility()
    {
        // 查找所有颜色指示器并设置可见性
        var colorIndicators = FindVisualChildren<Border>(PeriodsItemsControl)
            .Where(b => b.Name == "ColorIndicator")
            .ToList();

        foreach (var indicator in colorIndicators)
            indicator.Visibility = ShowColorIndicator ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    ///     查找所有指定类型的可视化子元素
    /// </summary>
    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) yield return typedChild;

            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }

    /// <summary>
    ///     应用模板按钮点击
    /// </summary>
    private void ApplyTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedIndex = TemplateComboBox.SelectedIndex;
        _periods = selectedIndex switch
        {
            1 => new List<CustomTimePeriod> // 6段
            {
                new() { Name = "凌晨", StartHour = 0, StartMinute = 0, EndHour = 4, EndMinute = 0, Color = "#1976D2" },
                new() { Name = "早晨", StartHour = 4, StartMinute = 0, EndHour = 8, EndMinute = 0, Color = "#388E3C" },
                new() { Name = "上午", StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Color = "#FBC02D" },
                new() { Name = "下午", StartHour = 12, StartMinute = 0, EndHour = 16, EndMinute = 0, Color = "#F57C00" },
                new() { Name = "傍晚", StartHour = 16, StartMinute = 0, EndHour = 20, EndMinute = 0, Color = "#E64A19" },
                new() { Name = "晚上", StartHour = 20, StartMinute = 0, EndHour = 24, EndMinute = 0, Color = "#7B1FA2" }
            },
            2 => new List<CustomTimePeriod> // 通勤模式
            {
                new() { Name = "凌晨", StartHour = 0, StartMinute = 0, EndHour = 6, EndMinute = 0, Color = "#1976D2" },
                new() { Name = "早高峰", StartHour = 6, StartMinute = 0, EndHour = 9, EndMinute = 0, Color = "#D32F2F" },
                new() { Name = "上午", StartHour = 9, StartMinute = 0, EndHour = 12, EndMinute = 0, Color = "#388E3C" },
                new() { Name = "中午", StartHour = 12, StartMinute = 0, EndHour = 14, EndMinute = 0, Color = "#FBC02D" },
                new() { Name = "下午", StartHour = 14, StartMinute = 0, EndHour = 18, EndMinute = 0, Color = "#F57C00" },
                new() { Name = "晚高峰", StartHour = 18, StartMinute = 0, EndHour = 21, EndMinute = 0, Color = "#D32F2F" },
                new() { Name = "晚上", StartHour = 21, StartMinute = 0, EndHour = 24, EndMinute = 0, Color = "#7B1FA2" }
            },
            _ => new List<CustomTimePeriod> // 默认4段
            {
                new() { Name = "凌晨", StartHour = 0, StartMinute = 0, EndHour = 6, EndMinute = 0, Color = "#1976D2" },
                new() { Name = "上午", StartHour = 6, StartMinute = 0, EndHour = 12, EndMinute = 0, Color = "#388E3C" },
                new() { Name = "下午", StartHour = 12, StartMinute = 0, EndHour = 18, EndMinute = 0, Color = "#FBC02D" },
                new() { Name = "晚上", StartHour = 18, StartMinute = 0, EndHour = 24, EndMinute = 0, Color = "#D32F2F" }
            }
        };
        RefreshPeriodsList();
        PeriodsChanged?.Invoke(this, _periods);
    }

    /// <summary>
    ///     添加新时段按钮点击
    /// </summary>
    private void AddPeriodButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[AddPeriodButton_Click] 点击添加按钮, 当前时段数={_periods.Count}");

        if (_periods.Count >= 8)
        {
            MessageBoxWindow.Show(Window.GetWindow(this), "最多只能添加8个时段");
            return;
        }

        _editingPeriod = null;
        PeriodNameTextBox.Text = "";
        StartHourTextBox.Text = "00";
        StartMinuteTextBox.Text = "00";
        EndHourTextBox.Text = "06";
        EndMinuteTextBox.Text = "00";
        EditPanel.Visibility = Visibility.Visible;

        Debug.WriteLine("[AddPeriodButton_Click] EditPanel 已显示");
    }

    /// <summary>
    ///     编辑按钮点击
    /// </summary>
    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CustomTimePeriod period)
        {
            _editingPeriod = period;
            PeriodNameTextBox.Text = period.Name;
            StartHourTextBox.Text = period.StartHour.ToString("D2");
            StartMinuteTextBox.Text = period.StartMinute.ToString("D2");
            EndHourTextBox.Text = period.EndHour.ToString("D2");
            EndMinuteTextBox.Text = period.EndMinute.ToString("D2");
            EditPanel.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    ///     删除按钮点击
    /// </summary>
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CustomTimePeriod period)
        {
            if (_periods.Count <= 2)
            {
                MessageBoxWindow.Show(Window.GetWindow(this), "至少需要保留2个时段");
                return;
            }

            _periods.Remove(period);
            RefreshPeriodsList();
            PeriodsChanged?.Invoke(this, _periods);
        }
    }

    /// <summary>
    ///     保存按钮点击
    /// </summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInput()) return;

        var name = PeriodNameTextBox.Text.Trim();
        var startHour = int.Parse(StartHourTextBox.Text);
        var startMinute = int.Parse(StartMinuteTextBox.Text);
        var endHour = int.Parse(EndHourTextBox.Text);
        var endMinute = int.Parse(EndMinuteTextBox.Text);

        // 检查时段是否重叠
        var newPeriod = new CustomTimePeriod
        {
            Name = name,
            StartHour = startHour,
            StartMinute = startMinute,
            EndHour = endHour,
            EndMinute = endMinute,
            Color = GetRandomColor()
        };

        if (_editingPeriod == null)
        {
            // 添加新时段
            _periods.Add(newPeriod);
        }
        else
        {
            // 更新现有时段
            var index = _periods.IndexOf(_editingPeriod);
            if (index >= 0)
            {
                newPeriod.Color = _editingPeriod.Color; // 保留原颜色
                _periods[index] = newPeriod;
            }
        }

        // 检查并修复时段连续性
        FixPeriodContinuity();

        RefreshPeriodsList();
        EditPanel.Visibility = Visibility.Collapsed;
        PeriodsChanged?.Invoke(this, _periods);
    }

    /// <summary>
    ///     取消按钮点击
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        EditPanel.Visibility = Visibility.Collapsed;
        _editingPeriod = null;
    }

    /// <summary>
    ///     时间输入框的预览文本输入事件（限制只能输入数字）
    /// </summary>
    private void TimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 只允许输入数字
        e.Handled = !IsTextAllowed(e.Text);
    }

    /// <summary>
    ///     时间输入框的粘贴事件（限制只能粘贴数字）
    /// </summary>
    private void TimeTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (!IsTextAllowed(text)) e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    /// <summary>
    ///     检查文本是否只包含数字
    /// </summary>
    private bool IsTextAllowed(string text)
    {
        return Regex.IsMatch(text, "^[0-9]+$");
    }

    /// <summary>
    ///     验证输入
    /// </summary>
    private bool ValidateInput()
    {
        var name = PeriodNameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBoxWindow.Show(Window.GetWindow(this), "请输入时段名称", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        // 验证开始时间
        if (!ValidateTimeInput(StartHourTextBox.Text, "开始小时", 0, 23, out var startHour))
            return false;

        if (!ValidateTimeInput(StartMinuteTextBox.Text, "开始分钟", 0, 59, out var startMinute))
            return false;

        // 验证结束时间
        if (!ValidateTimeInput(EndHourTextBox.Text, "结束小时", 0, 24, out var endHour))
            return false;

        if (!ValidateTimeInput(EndMinuteTextBox.Text, "结束分钟", 0, 59, out var endMinute))
            return false;

        // 特殊验证：如果结束小时是24，分钟必须是0
        if (endHour == 24 && endMinute != 0)
        {
            MessageBoxWindow.Show(Window.GetWindow(this), "当结束小时为24时，结束分钟必须为0", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var startTotal = startHour * 60 + startMinute;
        var endTotal = endHour * 60 + endMinute;

        if (endTotal <= startTotal)
        {
            MessageBoxWindow.Show(Window.GetWindow(this), "结束时间必须大于开始时间", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        // 检查时段名称是否重复（排除正在编辑的时段）
        var existingPeriod = _periods.FirstOrDefault(p => p.Name == name && p != _editingPeriod);
        if (existingPeriod != null)
        {
            MessageBoxWindow.Show(Window.GetWindow(this), "时段名称已存在", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     验证时间输入
    /// </summary>
    private bool ValidateTimeInput(string input, string fieldName, int min, int max, out int value)
    {
        value = 0;

        // 检查是否为空
        if (string.IsNullOrWhiteSpace(input))
        {
            MessageBoxWindow.Show(Window.GetWindow(this), $"{fieldName}不能为空", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        // 检查是否为有效数字
        if (!int.TryParse(input, out value))
        {
            MessageBoxWindow.Show(Window.GetWindow(this), $"{fieldName}必须是有效的数字", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        // 检查范围
        if (value < min || value > max)
        {
            MessageBoxWindow.Show(Window.GetWindow(this), $"{fieldName}必须在{min}-{max}之间", "提示", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     修复时段连续性（确保无重叠）
    /// </summary>
    private void FixPeriodContinuity()
    {
        // 按开始时间排序
        var sortedPeriods = _periods.OrderBy(p => p.StartTotalMinutes).ToList();
        _periods.Clear();

        for (var i = 0; i < sortedPeriods.Count; i++)
        {
            var period = sortedPeriods[i];

            // 修复与前一时段的间隙或重叠
            if (i > 0)
            {
                var prevPeriod = _periods[i - 1];
                if (period.StartTotalMinutes != prevPeriod.EndTotalMinutes)
                {
                    // 开始时间不等于前一时段的结束时间，调整当前时段的开始时间
                    period.StartHour = prevPeriod.EndHour;
                    period.StartMinute = prevPeriod.EndMinute;
                }
            }

            _periods.Add(period);
        }
    }

    /// <summary>
    ///     获取随机颜色
    /// </summary>
    private string GetRandomColor()
    {
        var colors = new[] { "#1976D2", "#388E3C", "#FBC02D", "#F57C00", "#D32F2F", "#7B1FA2", "#5E35B1", "#00796B" };
        var random = new Random();
        return colors[random.Next(colors.Length)];
    }
}