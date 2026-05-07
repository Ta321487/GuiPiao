using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GuiPiao.Controls
{
    /// <summary>
    /// 自动完成文本框控件
    /// </summary>
    public partial class AutoCompleteTextBox : UserControl
    {
        // 标记是否正在从代码设置值（而非用户输入）
        private bool _isSettingValueFromCode = false;

        public AutoCompleteTextBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 从代码设置文本值（不会触发下拉框）
        /// </summary>
        public void SetTextFromCode(string text)
        {
            _isSettingValueFromCode = true;
            try
            {
                Text = text;
            }
            finally
            {
                _isSettingValueFromCode = false;
            }
        }

        #region 依赖属性

        /// <summary>
        /// 输入文本
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AutoCompleteTextBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        /// <summary>
        /// 联想建议列表
        /// </summary>
        public ObservableCollection<string> Suggestions
        {
            get => (ObservableCollection<string>)GetValue(SuggestionsProperty);
            set => SetValue(SuggestionsProperty, value);
        }

        public static readonly DependencyProperty SuggestionsProperty =
            DependencyProperty.Register(nameof(Suggestions), typeof(ObservableCollection<string>), typeof(AutoCompleteTextBox),
                new PropertyMetadata(new ObservableCollection<string>()));

        /// <summary>
        /// 是否显示下拉框
        /// </summary>
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(AutoCompleteTextBox),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 选中项索引
        /// </summary>
        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(AutoCompleteTextBox),
                new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 文本改变命令
        /// </summary>
        public ICommand TextChangedCommand
        {
            get => (ICommand)GetValue(TextChangedCommandProperty);
            set => SetValue(TextChangedCommandProperty, value);
        }

        public static readonly DependencyProperty TextChangedCommandProperty =
            DependencyProperty.Register(nameof(TextChangedCommand), typeof(ICommand), typeof(AutoCompleteTextBox));

        /// <summary>
        /// 选择项命令
        /// </summary>
        public ICommand SelectItemCommand
        {
            get => (ICommand)GetValue(SelectItemCommandProperty);
            set => SetValue(SelectItemCommandProperty, value);
        }

        public static readonly DependencyProperty SelectItemCommandProperty =
            DependencyProperty.Register(nameof(SelectItemCommand), typeof(ICommand), typeof(AutoCompleteTextBox));

        /// <summary>
        /// 是否只读
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(AutoCompleteTextBox),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteTextBox control && e.NewValue is bool isReadOnly)
            {
                control.InputTextBox.IsReadOnly = isReadOnly;
            }
        }

        #endregion

        #region 事件处理

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteTextBox control && e.NewValue is string newText)
            {
                // 如果是从代码设置值，不触发下拉框
                if (!control._isSettingValueFromCode)
                {
                    control.TextChangedCommand?.Execute(newText);
                }
            }
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsDropDownOpen || Suggestions == null || Suggestions.Count == 0)
                return;

            switch (e.Key)
            {
                case Key.Down:
                    e.Handled = true;
                    SelectedIndex = (SelectedIndex + 1) % Suggestions.Count;
                    break;

                case Key.Up:
                    e.Handled = true;
                    SelectedIndex = SelectedIndex <= 0 ? Suggestions.Count - 1 : SelectedIndex - 1;
                    break;

                case Key.Enter:
                    if (SelectedIndex >= 0 && SelectedIndex < Suggestions.Count)
                    {
                        e.Handled = true;
                        SelectItem(Suggestions[SelectedIndex]);
                    }
                    break;

                case Key.Escape:
                    e.Handled = true;
                    IsDropDownOpen = false;
                    break;
            }
        }

        private void SuggestionsList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is string selected)
            {
                SelectItem(selected);
            }
        }

        private void SelectItem(string item)
        {
            Text = item;
            IsDropDownOpen = false;
            SelectedIndex = -1;
            SelectItemCommand?.Execute(item);
        }

        #endregion
    }
}
