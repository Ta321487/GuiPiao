using System.Windows;

namespace GuiPiao.View;

/// <summary>
///     启动过渡窗：主题色底，避免主窗口未就绪时露出系统白底。
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void SetStatus(string status)
    {
        StatusText.Text = status;
        // 立刻刷一帧，否则长时间初始化时文案不更新
        Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
    }
}
