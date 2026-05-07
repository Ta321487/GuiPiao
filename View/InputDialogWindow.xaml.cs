using System.Windows;

namespace GuiPiao.View;

/// <summary>
///     输入对话框窗口
/// </summary>
public partial class InputDialogWindow : Window
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="title">窗口标题</param>
    /// <param name="defaultText">默认文本</param>
    public InputDialogWindow(string prompt, string title = "输入", string defaultText = "")
    {
        InitializeComponent();
        Title = title;
        PromptTextBlock.Text = prompt;
        InputTextBox.Text = defaultText;
        InputTextBox.Focus();
    }

    /// <summary>
    ///     用户输入的文本
    /// </summary>
    public string InputText { get; private set; } = string.Empty;

    /// <summary>
    ///     确定按钮点击事件
    /// </summary>
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        InputText = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    /// <summary>
    ///     取消按钮点击事件
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}