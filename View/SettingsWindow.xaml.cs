using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using GuiPiao.Model;
using GuiPiao.Services;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
///     SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsPageType? _initialPage;
    private bool _isShowing;

    public SettingsWindow()
    {
        InitializeComponent();
        // 应用当前主题
        ThemeManager.ApplyThemeToWindow(this);
        // 订阅窗口加载事件，延迟初始化视图
        Loaded += OnWindowLoaded;
        // 订阅窗口关闭事件
        Closing += OnWindowClosing;
        // 订阅窗口状态变化事件
        StateChanged += OnWindowStateChanged;
    }

    /// <summary>
    ///     带初始页面的构造函数
    /// </summary>
    public SettingsWindow(SettingsPageType initialPage) : this()
    {
        _initialPage = initialPage;
    }

    /// <summary>
    ///     显示设置窗口（模态对话框方式）
    /// </summary>
    public new bool? ShowDialog()
    {
        if (_isShowing) return null;
        _isShowing = true;

        // 确保设置 Owner，使窗口与主窗口关联
        // 如果已经设置了 Owner（如从地图窗口打开），则保持原设置
        if (Owner == null) Owner = Application.Current.MainWindow;

        // 使用 base.ShowDialog() 显示真正的模态对话框
        var result = base.ShowDialog();

        _isShowing = false;
        return result;
    }

    /// <summary>
    ///     窗口状态变化时，确保设置窗口始终在最前面
    /// </summary>
    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // 最小化时，设置窗口恢复后仍然保持激活
            // 主窗口保持禁用状态
        }
        else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
        {
            // 恢复时，确保设置窗口在最前面
            Activate();
            Topmost = true;
            Topmost = false;
        }
    }

    /// <summary>
    ///     窗口加载完成后初始化视图
    /// </summary>
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"[SettingsWindow] OnWindowLoaded called, DataContext: {DataContext?.GetType().Name}");
        if (DataContext is SettingsViewModel vm)
        {
            // 如果有指定初始页面，导航到该页面
            if (_initialPage.HasValue)
            {
                Debug.WriteLine($"[SettingsWindow] 导航到初始页面: {_initialPage.Value}");
                vm.NavigateToPage(_initialPage.Value);
            }
            else
            {
                // 否则初始化默认页面
                Debug.WriteLine("[SettingsWindow] 初始化默认页面");
                vm.InitializeDefaultPage();
            }
        }
        else
        {
            Debug.WriteLine("[SettingsWindow] DataContext 不是 SettingsViewModel");
        }

        // 取消订阅，避免重复调用
        Loaded -= OnWindowLoaded;
    }

    /// <summary>
    ///     窗口关闭前检查是否有未保存的更改
    /// </summary>
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // 通知OCR设置页面暂停下载
        if (DataContext is SettingsViewModel vm)
        {
            vm.OnWindowClosing();

            if (vm.HasUnsavedChanges)
            {
                // 显示确认对话框
                var result = MessageBoxWindow.Show(
                    "您有未保存的设置更改。\n\n是否保存更改？",
                    "未保存的更改",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // 保存所有设置
                        vm.SaveAllCommand.Execute(null);
                        break;
                    case MessageBoxResult.No:
                        // 放弃更改，直接关闭
                        vm.DiscardAllChanges();
                        break;
                    case MessageBoxResult.Cancel:
                        // 取消关闭
                        e.Cancel = true;
                        break;
                }
            }
        }
    }
}