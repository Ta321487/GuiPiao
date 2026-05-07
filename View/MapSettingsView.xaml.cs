using System.Windows.Controls;
using System.Windows.Input;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
///     MapSettingsView.xaml 的交互逻辑
/// </summary>
public partial class MapSettingsView : UserControl
{
    public MapSettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     已完成行程颜色选择
    /// </summary>
    private void CompletedColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MapSettingsViewModel viewModel) viewModel.OpenColorPickerCommand.Execute("Completed");
    }

    /// <summary>
    ///     未出行行程颜色选择
    /// </summary>
    private void PendingColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MapSettingsViewModel viewModel) viewModel.OpenColorPickerCommand.Execute("Pending");
    }

    /// <summary>
    ///     已改签行程颜色选择
    /// </summary>
    private void RescheduledColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MapSettingsViewModel viewModel) viewModel.OpenColorPickerCommand.Execute("Rescheduled");
    }

    /// <summary>
    ///     已退票行程颜色选择
    /// </summary>
    private void RefundedColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MapSettingsViewModel viewModel) viewModel.OpenColorPickerCommand.Execute("Refunded");
    }

    /// <summary>
    ///     站点标记颜色选择
    /// </summary>
    private void StationColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MapSettingsViewModel viewModel) viewModel.OpenColorPickerCommand.Execute("Station");
    }

    /// <summary>
    ///     选中行程颜色选择
    /// </summary>
    private void SelectedColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MapSettingsViewModel viewModel) viewModel.OpenColorPickerCommand.Execute("Selected");
    }
}