using GuiPiao.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GuiPiao.View
{
    /// <summary>
    /// ShortcutSettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class ShortcutSettingsView : UserControl
    {
        public ShortcutSettingsView()
        {
            InitializeComponent();
            DataContext = new ShortcutSettingsViewModel();

            // 订阅键盘事件
            Loaded += (s, e) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown += Window_PreviewKeyDown;
                }
            };

            Unloaded += (s, e) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.PreviewKeyDown -= Window_PreviewKeyDown;
                }
            };
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var viewModel = DataContext as ShortcutSettingsViewModel;
            if (viewModel?.IsWaitingForKey == true)
            {
                // 处理 Esc 键取消编辑
                if (e.Key == Key.Escape)
                {
                    viewModel.CancelEditCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                // 将按键传递给 ViewModel
                viewModel.HandleKeyInput(e.Key, Keyboard.Modifiers);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 快捷键边框点击事件 - 开始编辑
        /// </summary>
        private void ShortcutKeyBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ShortcutItemViewModel shortcut)
            {
                var viewModel = DataContext as ShortcutSettingsViewModel;
                viewModel?.StartEditCommand.Execute(shortcut);
            }
        }
    }
}
