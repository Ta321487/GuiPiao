using System.Diagnostics;
using System.Windows;
using GuiPiao.Services;

namespace GuiPiao.View;

/// <summary>
///     自定义消息框窗口（支持深色模式）
/// </summary>
public partial class MessageBoxWindow : Window
{
    private MessageBoxResult _result = MessageBoxResult.None;

    public MessageBoxWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string Message
    {
        get => MessageTextBlock.Text;
        set => MessageTextBlock.Text = value;
    }

    // 按钮文字属性
    public string YesButtonText { get; set; } = "是";
    public string NoButtonText { get; set; } = "否";
    public string OkButtonText { get; set; } = "确定";
    public string CancelButtonText { get; set; } = "取消";

    /// <summary>
    ///     显示消息框
    /// </summary>
    public static MessageBoxResult Show(string message, string title = "提示",
        MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information,
        string? yesText = null, string? noText = null, string? okText = null, string? cancelText = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.WriteLine("[MessageBoxWindow] Show 收到空消息，已替换为占位文案。请检查调用栈或日志定位来源。");
            message = "（无提示内容。若刚启动即出现，请查看日志中同时间的错误或联系开发者。）";
        }

        var window = new MessageBoxWindow
        {
            Message = message,
            Title = title
        };

        // 设置自定义按钮文字
        if (yesText != null) window.YesButtonText = yesText;
        if (noText != null) window.NoButtonText = noText;
        if (okText != null) window.OkButtonText = okText;
        if (cancelText != null) window.CancelButtonText = cancelText;

        // 设置图标
        window.SetIcon(icon);

        // 设置按钮可见性和文字
        window.SetButtons(button);

        // 应用当前主题
        ThemeManager.ApplyThemeToWindow(window);

        window.ShowDialog();
        return window._result;
    }

    /// <summary>
    ///     显示消息框（带所有者窗口）
    /// </summary>
    public static MessageBoxResult Show(Window owner, string message, string title = "提示",
        MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information,
        string? yesText = null, string? noText = null, string? okText = null, string? cancelText = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.WriteLine("[MessageBoxWindow] Show(owner) 收到空消息，已替换为占位文案。请检查调用栈或日志定位来源。");
            message = "（无提示内容。若刚启动即出现，请查看日志中同时间的错误或联系开发者。）";
        }

        var window = new MessageBoxWindow
        {
            Message = message,
            Title = title
            // 不设置 Owner，避免最小化时影响主窗口
        };

        // 如果父窗口是置顶窗口，则消息框也设置为置顶，确保显示在最前面
        if (owner != null && owner.Topmost) window.Topmost = true;

        // 设置自定义按钮文字
        if (yesText != null) window.YesButtonText = yesText;
        if (noText != null) window.NoButtonText = noText;
        if (okText != null) window.OkButtonText = okText;
        if (cancelText != null) window.CancelButtonText = cancelText;

        window.SetIcon(icon);
        window.SetButtons(button);
        ThemeManager.ApplyThemeToWindow(window);

        window.ShowDialog();
        return window._result;
    }

    /// <summary>
    ///     显示进度对话框（非模态，返回窗口实例供关闭）
    /// </summary>
    public static MessageBoxWindow ShowProgress(string message, string title = "正在处理")
    {
        var window = new MessageBoxWindow
        {
            Message = message,
            Title = title,
            Topmost = true
        };

        // 使用 Dispatcher 确保 UI 更新在正确的线程上
        window.Dispatcher.Invoke(() =>
        {
            // 设置进度图标
            window.IconTextBlock.Text = "📤";

            // 显示进度条，隐藏按钮
            window.ProgressBar.Visibility = Visibility.Visible;
            window.ButtonPanel.Visibility = Visibility.Collapsed;

            // 应用当前主题
            ThemeManager.ApplyThemeToWindow(window);
        });

        // 非模态显示
        window.Show();
        return window;
    }

    private void SetIcon(MessageBoxImage icon)
    {
        switch (icon)
        {
            case MessageBoxImage.Error:
                IconTextBlock.Text = "❌";
                break;
            case MessageBoxImage.Warning:
                IconTextBlock.Text = "⚠️";
                break;
            case MessageBoxImage.Question:
                IconTextBlock.Text = "❓";
                break;
            case MessageBoxImage.Information:
            default:
                IconTextBlock.Text = "ℹ️";
                break;
        }
    }

    private void SetButtons(MessageBoxButton button)
    {
        YesButton.Visibility = Visibility.Collapsed;
        NoButton.Visibility = Visibility.Collapsed;
        OkButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;

        switch (button)
        {
            case MessageBoxButton.OK:
                OkButton.Content = OkButtonText;
                OkButton.Visibility = Visibility.Visible;
                break;
            case MessageBoxButton.OKCancel:
                OkButton.Content = OkButtonText;
                CancelButton.Content = CancelButtonText;
                OkButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                break;
            case MessageBoxButton.YesNo:
                YesButton.Content = YesButtonText;
                NoButton.Content = NoButtonText;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                break;
            case MessageBoxButton.YesNoCancel:
                YesButton.Content = YesButtonText;
                NoButton.Content = NoButtonText;
                CancelButton.Content = CancelButtonText;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
                break;
        }
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        _result = MessageBoxResult.Yes;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        _result = MessageBoxResult.No;
        Close();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _result = MessageBoxResult.OK;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _result = MessageBoxResult.Cancel;
        Close();
    }
}