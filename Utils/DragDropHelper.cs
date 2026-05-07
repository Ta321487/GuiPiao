using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GuiPiao.Utils
{
    /// <summary>
    /// 拖拽排序帮助类 - 提供通用的拖拽排序功能
    /// </summary>
    public class DragDropHelper
    {
        private int _dragStartIndex = -1;
        private FrameworkElement? _draggedElement;
        private readonly Action<int, int>? _onDropAction;
        private readonly Action? _onDragStartAction;

        /// <summary>
        /// 创建拖拽排序帮助类实例
        /// </summary>
        /// <param name="onDropAction">放置时的回调，参数为(源索引, 目标索引)</param>
        /// <param name="onDragStartAction">开始拖拽时的回调（可选）</param>
        public DragDropHelper(Action<int, int> onDropAction, Action? onDragStartAction = null)
        {
            _onDropAction = onDropAction;
            _onDragStartAction = onDragStartAction;
        }

        /// <summary>
        /// 鼠标左键按下时记录拖拽起始项
        /// </summary>
        public void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e, Func<DependencyObject, FrameworkElement?>? findElementFunc = null)
        {
            if (sender is not ItemsControl itemsControl) return;

            // 获取点击的元素
            var element = findElementFunc?.Invoke(e.OriginalSource as DependencyObject)
                ?? FindVisualParent<FrameworkElement>(e.OriginalSource as DependencyObject);

            if (element == null) return;

            // 获取元素在集合中的索引
            _dragStartIndex = GetElementIndex(itemsControl, element);
            if (_dragStartIndex < 0) return;

            _draggedElement = element;
        }

        /// <summary>
        /// 鼠标移动时开始拖拽
        /// </summary>
        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                ResetDragState();
                return;
            }

            if (_dragStartIndex < 0 || _draggedElement == null) return;

            // 触发开始拖拽回调
            _onDragStartAction?.Invoke();

            // 开始拖拽操作
            DragDrop.DoDragDrop(_draggedElement, _draggedElement.DataContext, DragDropEffects.Move);
            ResetDragState();
        }

        /// <summary>
        /// 拖拽悬停时更新视觉效果
        /// </summary>
        public void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        /// <summary>
        /// 放置时执行排序
        /// </summary>
        public void OnDrop(object sender, DragEventArgs e, Func<DependencyObject, FrameworkElement?>? findElementFunc = null)
        {
            if (sender is not ItemsControl itemsControl) return;

            // 获取放置的目标元素
            var targetElement = findElementFunc?.Invoke(e.OriginalSource as DependencyObject)
                ?? FindVisualParent<FrameworkElement>(e.OriginalSource as DependencyObject);

            if (targetElement == null) return;

            var targetIndex = GetElementIndex(itemsControl, targetElement);
            if (targetIndex < 0 || targetIndex == _dragStartIndex) return;

            // 执行放置回调
            _onDropAction?.Invoke(_dragStartIndex, targetIndex);

            e.Handled = true;
        }

        /// <summary>
        /// 获取元素在ItemsControl中的索引
        /// </summary>
        private static int GetElementIndex(ItemsControl itemsControl, FrameworkElement element)
        {
            // 尝试从DataContext获取索引
            if (element.DataContext != null)
            {
                var items = itemsControl.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == element.DataContext)
                    {
                        return i;
                    }
                }
            }

            // 尝试从容器的Index获取
            if (element is DataGridRow dataGridRow)
            {
                return dataGridRow.GetIndex();
            }

            return -1;
        }

        /// <summary>
        /// 重置拖拽状态
        /// </summary>
        private void ResetDragState()
        {
            _dragStartIndex = -1;
            _draggedElement = null;
        }

        /// <summary>
        /// 查找可视树中的父元素
        /// </summary>
        public static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        /// <summary>
        /// 通用的集合项移动方法
        /// </summary>
        public static void MoveItem<T>(ObservableCollection<T> collection, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= collection.Count) return;
            if (targetIndex < 0 || targetIndex >= collection.Count) return;
            if (sourceIndex == targetIndex) return;

            collection.Move(sourceIndex, targetIndex);
        }

        /// <summary>
        /// 更新SortOrder属性（如果集合项实现了ISortable接口）
        /// </summary>
        public static void UpdateSortOrder<T>(ObservableCollection<T> collection) where T : ISortable
        {
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].SortOrder = i;
            }
        }
    }

    /// <summary>
    /// 可排序接口
    /// </summary>
    public interface ISortable
    {
        int SortOrder { get; set; }
    }
}
