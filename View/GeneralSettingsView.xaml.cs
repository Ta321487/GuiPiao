using GuiPiao.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace GuiPiao.View
{
    /// <summary>
    /// GeneralSettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class GeneralSettingsView : UserControl
    {
        public GeneralSettingsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 点击自定义颜色区域时触发
        /// </summary>
        private void CustomColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 获取 ViewModel 并调用打开颜色选择器命令
            if (DataContext is GeneralSettingsViewModel viewModel)
            {
                // 先设置 IsCustomColor 为 true
                viewModel.IsCustomColor = true;
                // 然后打开颜色选择器
                viewModel.OpenColorPickerCommand.Execute(null);
            }
            e.Handled = true;
        }
    }
}
