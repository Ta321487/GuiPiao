using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GuiPiao.View;

/// <summary>
///     DashboardChartView.xaml 的交互逻辑
/// </summary>
public partial class DashboardChartView : UserControl
{
    public DashboardChartView()
    {
        InitializeComponent();
        //this.PreviewMouseMove += (s, e) => {
        //    var pos = e.GetPosition(this);
        //    System.Diagnostics.Debug.WriteLine($"Mouse at: {pos.X}, {pos.Y}");
        //};
    }

    /// <summary>
    ///     图表加载完成事件 - 强制刷新图表渲染
    /// </summary>
    private void Chart_Loaded(object sender, RoutedEventArgs e)
    {
        // 延迟执行以确保容器已正确计算大小
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (sender is FrameworkElement chart)
            {
                // 强制更新布局
                chart.UpdateLayout();

                // 触发大小改变事件以强制图表重绘
                var width = chart.ActualWidth;
                var height = chart.ActualHeight;

                Debug.WriteLine($"[DashboardChartView] 图表加载完成: {width}x{height}");
            }
        }), DispatcherPriority.Render);
    }
}