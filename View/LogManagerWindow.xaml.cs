using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

public partial class LogManagerWindow : Window
{
    public LogManagerWindow()
    {
        InitializeComponent();
        // 应用当前主题
        ThemeManager.ApplyThemeToWindow(this);
    }

    private void OpenLogSettings_Click(object sender, RoutedEventArgs e)
    {
        // 直接打开系统设置的日志页面
        var settingsWindow = new SettingsWindow(SettingsPageType.Log)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        settingsWindow.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is LogManagerViewModel vm)
        {
            foreach (var item in e.AddedItems.OfType<LogItem>()) vm.SelectLog(item);
            foreach (var item in e.RemovedItems.OfType<LogItem>()) vm.DeselectLog(item);
        }
    }

    /// <summary>
    ///     窗口关闭时，如果主窗口被隐藏，则不将焦点返回到主窗口
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        // 清除 Owner，防止关闭时 WPF 自动激活被隐藏的主窗口
        Owner = null;
        base.OnClosing(e);
    }
}