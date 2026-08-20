using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        e.Handled = !int.TryParse(e.Text, out _);
    }

    /// <summary>
    ///     数字文本框失去焦点 - 验证并限制数值范围
    /// </summary>
    private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.Tag is not string tag)
            return;

        var parts = tag.Split(',');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var minValue) ||
            !int.TryParse(parts[1], out var maxValue))
            return;

        var originalText = textBox.Text;
        var needsAdjust = false;
        int currentValue;

        if (!int.TryParse(originalText, out currentValue))
        {
            currentValue = minValue;
            needsAdjust = true;
        }
        else if (currentValue < minValue)
        {
            currentValue = minValue;
            needsAdjust = true;
        }
        else if (currentValue > maxValue)
        {
            currentValue = maxValue;
            needsAdjust = true;
        }

        if (needsAdjust && originalText != currentValue.ToString())
        {
            textBox.Text = currentValue.ToString();
            MessageBoxWindow.Show(
                Window.GetWindow(this),
                $"输入值已自动调整为 {currentValue} px\n有效范围：{minValue} - {maxValue}px",
                "输入调整");
        }

        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }
}
