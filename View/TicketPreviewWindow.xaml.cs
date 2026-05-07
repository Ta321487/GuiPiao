using System.Windows;
using System.Windows.Input;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
///     TicketPreviewWindow.xaml 的交互逻辑
/// </summary>
public partial class TicketPreviewWindow : Window
{
    public TicketPreviewWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     带行程数据的构造函数
    /// </summary>
    public TicketPreviewWindow(Model.TripItem tripItem) : this()
    {
        if (DataContext is TicketPreviewViewModel viewModel) viewModel.SetTripItem(tripItem);
    }

    /// <summary>
    ///     窗口鼠标滚轮事件
    /// </summary>
    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is TicketPreviewViewModel viewModel)
        {
            viewModel.HandleMouseWheel(e.Delta);
            e.Handled = true;
        }
    }
}