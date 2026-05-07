using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
///     UISettingsView.xaml 的交互逻辑
/// </summary>
public partial class UISettingsView : UserControl
{
    public UISettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     信息颜色选择
    /// </summary>
    private void InfoColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is UISettingsViewModel vm) vm.OpenColorPickerCommand.Execute("Info");
    }

    /// <summary>
    ///     警告颜色选择
    /// </summary>
    private void WarningColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is UISettingsViewModel vm) vm.OpenColorPickerCommand.Execute("Warning");
    }

    /// <summary>
    ///     错误颜色选择
    /// </summary>
    private void ErrorColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is UISettingsViewModel vm) vm.OpenColorPickerCommand.Execute("Error");
    }

    /// <summary>
    ///     致命颜色选择
    /// </summary>
    private void FatalColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is UISettingsViewModel vm) vm.OpenColorPickerCommand.Execute("Fatal");
    }

    /// <summary>
    ///     数字文本框输入预览 - 只允许输入数字
    /// </summary>
    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 只允许输入数字
        e.Handled = !int.TryParse(e.Text, out _);
    }

    /// <summary>
    ///     数字文本框失去焦点 - 验证并限制数值范围
    /// </summary>
    private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.Tag is string tag)
        {
            // 解析最小值和最大值 (格式: "min,max")
            var parts = tag.Split(',');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var minValue) &&
                int.TryParse(parts[1], out var maxValue))
            {
                var originalText = textBox.Text;
                var isValid = int.TryParse(originalText, out var currentValue) && currentValue >= 1;
                var needsAdjust = false;

                if (!isValid)
                {
                    // 输入无效，使用最小值
                    currentValue = minValue;
                    needsAdjust = true;
                }
                else if (currentValue < minValue)
                {
                    // 小于最小值
                    currentValue = minValue;
                    needsAdjust = true;
                }
                else if (currentValue > maxValue)
                {
                    // 大于最大值
                    currentValue = maxValue;
                    needsAdjust = true;
                }

                // 更新文本框显示
                textBox.Text = currentValue.ToString();

                // 如果需要调整且原始输入不正确，显示提示
                if (needsAdjust && originalText != currentValue.ToString())
                    MessageBoxWindow.Show(
                        Window.GetWindow(this),
                        $"输入值已自动调整为 {currentValue} px\n有效范围：{minValue} - {maxValue}px",
                        "输入调整");
            }
        }
    }
}