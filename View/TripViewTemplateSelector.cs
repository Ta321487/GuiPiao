using System.Windows;
using System.Windows.Controls;
using GuiPiao.Messages;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
/// 行程列表视图模板选择器，根据当前视图类型选择列表或卡片模板
/// </summary>
public class TripViewTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// 专业列表视图模板
    /// </summary>
    public DataTemplate? ListTemplate { get; set; }

    /// <summary>
    /// 简洁卡片视图模板
    /// </summary>
    public DataTemplate? CardTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (container is FrameworkElement fe)
        {
            // 从 MainWindow 的 DataContext (MainViewModel) 获取当前视图类型
            if (fe.DataContext is MainViewModel mainVm)
            {
                return mainVm.TripList.CurrentViewType == ViewType.Card ? CardTemplate : ListTemplate;
            }
        }

        return ListTemplate;
    }
}
