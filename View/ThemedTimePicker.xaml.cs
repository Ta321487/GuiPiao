using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace GuiPiao.View
{
    public class ThemedTimePicker : Control
    {
        #region Dependency Properties

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedTime),
                typeof(DateTime?),
                typeof(ThemedTimePicker),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedTimeChanged));

        public static readonly DependencyProperty SelectedHourProperty =
            DependencyProperty.Register(
                nameof(SelectedHour),
                typeof(int),
                typeof(ThemedTimePicker),
                new PropertyMetadata(0, OnSelectedHourChanged));

        public static readonly DependencyProperty SelectedMinuteProperty =
            DependencyProperty.Register(
                nameof(SelectedMinute),
                typeof(int),
                typeof(ThemedTimePicker),
                new PropertyMetadata(0, OnSelectedMinuteChanged));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(ThemedTimePicker),
                new PropertyMetadata(false));

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(
                nameof(Watermark),
                typeof(string),
                typeof(ThemedTimePicker),
                new PropertyMetadata("选择时间"));

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(
                nameof(HeaderText),
                typeof(string),
                typeof(ThemedTimePicker),
                new PropertyMetadata("选择时间"));

        #endregion

        #region Properties

        public DateTime? SelectedTime
        {
            get => (DateTime?)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        public int SelectedHour
        {
            get => (int)GetValue(SelectedHourProperty);
            set => SetValue(SelectedHourProperty, value);
        }

        public int SelectedMinute
        {
            get => (int)GetValue(SelectedMinuteProperty);
            set => SetValue(SelectedMinuteProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            set => SetValue(HeaderTextProperty, value);
        }

        #endregion

        #region Events

        public static readonly RoutedEvent SelectedTimeChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(SelectedTimeChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ThemedTimePicker));

        public event RoutedEventHandler SelectedTimeChanged
        {
            add => AddHandler(SelectedTimeChangedEvent, value);
            remove => RemoveHandler(SelectedTimeChangedEvent, value);
        }

        #endregion

        #region Template Parts

        private TextBox? _textBox;
        private Popup? _popup;
        private Border? _border;
        private Button? _hourUpButton;
        private Button? _hourDownButton;
        private Button? _minuteUpButton;
        private Button? _minuteDownButton;
        private Button? _time00Button;
        private Button? _time06Button;
        private Button? _time08Button;
        private Button? _time09Button;
        private Button? _time12Button;
        private Button? _time14Button;
        private Button? _time18Button;
        private Button? _time20Button;
        private Button? _time22Button;
        private Button? _time23Button;
        private Button? _nowButton;
        private Button? _confirmButton;
        private Button? _cancelButton;

        // 临时存储选择的时间，点击确定后才应用到 SelectedTime
        private int _tempHour;
        private int _tempMinute;

        #endregion

        #region Constructors

        static ThemedTimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ThemedTimePicker),
                new FrameworkPropertyMetadata(typeof(ThemedTimePicker)));
        }

        public ThemedTimePicker()
        {
            Loaded += OnLoaded;
        }

        #endregion

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 初始化时间值
            if (SelectedTime.HasValue)
            {
                SelectedHour = SelectedTime.Value.Hour;
                SelectedMinute = SelectedTime.Value.Minute;
                _tempHour = SelectedHour;
                _tempMinute = SelectedMinute;
            }
            // 如果 SelectedTime 为 null，保持小时和分钟为 0，文本框会显示空
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 获取模板部件
            _textBox = GetTemplateChild("PART_TextBox") as TextBox;
            _popup = GetTemplateChild("PART_Popup") as Popup;
            _border = GetTemplateChild("Border") as Border;
            _hourUpButton = GetTemplateChild("PART_HourUp") as Button;

            // Border 点击事件
            if (_border != null)
            {
                Debug.WriteLine($"Border found, attaching MouseLeftButtonDown event");
                _border.MouseLeftButtonDown += (s, e) =>
                {
                    Debug.WriteLine($"Border MouseLeftButtonDown fired, current IsDropDownOpen: {IsDropDownOpen}");
                    if (_popup != null)
                    {
                        if (!IsDropDownOpen)
                        {
                            // 要打开 Popup，先设置 StaysOpen = true
                            _popup.StaysOpen = true;
                            IsDropDownOpen = true;
                            // 延迟恢复 StaysOpen，确保 Popup 完全打开且鼠标事件处理完毕
                            System.Threading.Tasks.Task.Run(async () =>
                            {
                                await System.Threading.Tasks.Task.Delay(100);
                                await Dispatcher.BeginInvoke(() =>
                                {
                                    if (_popup != null)
                                        _popup.StaysOpen = false;
                                });
                            });
                        }
                        else
                        {
                            // 要关闭 Popup，直接关闭
                            IsDropDownOpen = false;
                        }
                    }
                    else
                    {
                        IsDropDownOpen = !IsDropDownOpen;
                    }
                    Debug.WriteLine($"After toggle, IsDropDownOpen: {IsDropDownOpen}");
                    e.Handled = true;
                };
            }
            else
            {
                Debug.WriteLine($"Border is null!");
            }
            _hourDownButton = GetTemplateChild("PART_HourDown") as Button;
            _minuteUpButton = GetTemplateChild("PART_MinuteUp") as Button;
            _minuteDownButton = GetTemplateChild("PART_MinuteDown") as Button;
            _time00Button = GetTemplateChild("PART_Time00") as Button;
            _time06Button = GetTemplateChild("PART_Time06") as Button;
            _time08Button = GetTemplateChild("PART_Time08") as Button;
            _time09Button = GetTemplateChild("PART_Time09") as Button;
            _time12Button = GetTemplateChild("PART_Time12") as Button;
            _time14Button = GetTemplateChild("PART_Time14") as Button;
            _time18Button = GetTemplateChild("PART_Time18") as Button;
            _time20Button = GetTemplateChild("PART_Time20") as Button;
            _time22Button = GetTemplateChild("PART_Time22") as Button;
            _time23Button = GetTemplateChild("PART_Time23") as Button;
            _nowButton = GetTemplateChild("PART_Now") as Button;
            _confirmButton = GetTemplateChild("PART_Confirm") as Button;
            _cancelButton = GetTemplateChild("PART_Cancel") as Button;

            // Popup 打开时，从当前 SelectedTime 初始化临时值
            if (_popup != null)
            {
                _popup.Opened += (s, e) =>
                {
                    Debug.WriteLine("Popup Opened event fired");
                    // 打开时从当前值初始化临时值
                    if (SelectedTime.HasValue)
                    {
                        _tempHour = SelectedTime.Value.Hour;
                        _tempMinute = SelectedTime.Value.Minute;
                    }
                    else
                    {
                        _tempHour = 0;
                        _tempMinute = 0;
                    }
                    // 更新显示
                    SelectedHour = _tempHour;
                    SelectedMinute = _tempMinute;
                };

                _popup.Closed += (s, e) =>
                {
                    Debug.WriteLine("Popup Closed event fired");
                };
            }

            // 小时调整按钮 - 只调整临时值
            if (_hourUpButton != null)
                _hourUpButton.Click += (s, e) => ChangeTempHour(1);

            if (_hourDownButton != null)
                _hourDownButton.Click += (s, e) => ChangeTempHour(-1);

            // 分钟调整按钮 - 只调整临时值
            if (_minuteUpButton != null)
                _minuteUpButton.Click += (s, e) => ChangeTempMinute(1);

            if (_minuteDownButton != null)
                _minuteDownButton.Click += (s, e) => ChangeTempMinute(-1);

            // 快速选择按钮 - 只设置临时值
            if (_time00Button != null)
                _time00Button.Click += (s, e) => SetTempTime(0, 0);

            if (_time06Button != null)
                _time06Button.Click += (s, e) => SetTempTime(6, 0);

            if (_time08Button != null)
                _time08Button.Click += (s, e) => SetTempTime(8, 0);

            if (_time09Button != null)
                _time09Button.Click += (s, e) => SetTempTime(9, 0);

            if (_time12Button != null)
                _time12Button.Click += (s, e) => SetTempTime(12, 0);

            if (_time14Button != null)
                _time14Button.Click += (s, e) => SetTempTime(14, 0);

            if (_time18Button != null)
                _time18Button.Click += (s, e) => SetTempTime(18, 0);

            if (_time20Button != null)
                _time20Button.Click += (s, e) => SetTempTime(20, 0);

            if (_time22Button != null)
                _time22Button.Click += (s, e) => SetTempTime(22, 0);

            if (_time23Button != null)
                _time23Button.Click += (s, e) => SetTempTime(23, 0);

            // 现在按钮 - 只设置临时值
            if (_nowButton != null)
                _nowButton.Click += (s, e) => SetTempTimeToNow();

            // 确定按钮 - 应用临时值到 SelectedTime
            if (_confirmButton != null)
                _confirmButton.Click += (s, e) => ConfirmTime();

            // 取消按钮 - 关闭 Popup，不应用更改
            if (_cancelButton != null)
                _cancelButton.Click += (s, e) => CancelTime();

            // 初始化文本框内容
            UpdateTextBox();

            // 设置 Popup 的滚动处理
            SetupPopupScrollHandling();
        }

        /// <summary>
        /// 设置 Popup 的滚动处理，当父容器滚动时关闭 Popup
        /// </summary>
        private void SetupPopupScrollHandling()
        {
            if (_popup == null) return;

            // 当 Popup 打开时，监听父容器的滚动事件
            _popup.Opened += (s, e) =>
            {
                var scrollViewer = FindParentScrollViewer(this);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged += OnParentScrollChanged;
                }
            };

            // 当 Popup 关闭时，移除监听
            _popup.Closed += (s, e) =>
            {
                var scrollViewer = FindParentScrollViewer(this);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged -= OnParentScrollChanged;
                }
            };
        }

        /// <summary>
        /// 父容器滚动时关闭 Popup
        /// </summary>
        private void OnParentScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 当发生滚动时，关闭 Popup
            if (e.VerticalChange != 0 || e.HorizontalChange != 0)
            {
                IsDropDownOpen = false;
            }
        }

        /// <summary>
        /// 查找父级 ScrollViewer
        /// </summary>
        private ScrollViewer? FindParentScrollViewer(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is ScrollViewer scrollViewer)
                {
                    return scrollViewer;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private void OnDropDownButtonClick(object sender, RoutedEventArgs e)
        {
            IsDropDownOpen = !IsDropDownOpen;
        }

        #endregion

        #region Private Methods

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (ThemedTimePicker)d;

            if (e.NewValue is DateTime newTime)
            {
                picker.SelectedHour = newTime.Hour;
                picker.SelectedMinute = newTime.Minute;
            }

            picker.UpdateTextBox();
            picker.RaiseEvent(new RoutedEventArgs(SelectedTimeChangedEvent, picker));
        }

        private static void OnSelectedHourChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 只更新显示，不自动更新 SelectedTime
            // 临时值通过 ChangeTempHour 方法更新
        }

        private static void OnSelectedMinuteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 只更新显示，不自动更新 SelectedTime
            // 临时值通过 ChangeTempMinute 方法更新
        }

        private void UpdateSelectedTime()
        {
            // 确保小时和分钟在有效范围内
            var hour = Math.Max(0, Math.Min(23, SelectedHour));
            var minute = Math.Max(0, Math.Min(59, SelectedMinute));

            // 更新 SelectedTime - 只在 ConfirmTime 中调用
            var today = SelectedTime?.Date ?? DateTime.Today;
            SelectedTime = new DateTime(today.Year, today.Month, today.Day, hour, minute, 0);
        }

        private void UpdateTextBox()
        {
            if (_textBox != null)
            {
                if (SelectedTime.HasValue)
                {
                    _textBox.Text = $"{SelectedTime.Value.Hour:D2}:{SelectedTime.Value.Minute:D2}";
                }
                else
                {
                    _textBox.Text = string.Empty;
                }
            }
        }

        private void ChangeHour(int delta)
        {
            SelectedHour = (SelectedHour + delta + 24) % 24;
        }

        private void ChangeMinute(int delta)
        {
            SelectedMinute = (SelectedMinute + delta + 60) % 60;
        }

        private void SetTime(int hour, int minute)
        {
            SelectedHour = hour;
            SelectedMinute = minute;
            IsDropDownOpen = false;
        }

        private void SetTimeToNow()
        {
            var now = DateTime.Now;
            SelectedHour = now.Hour;
            SelectedMinute = now.Minute;
            IsDropDownOpen = false;
        }

        private void ClearTime()
        {
            SelectedTime = null;
            UpdateTextBox();
            IsDropDownOpen = false;
        }

        // ========== 临时值操作方法（不立即应用到 SelectedTime）==========

        private void ChangeTempHour(int delta)
        {
            _tempHour = (_tempHour + delta + 24) % 24;
            SelectedHour = _tempHour; // 更新显示
        }

        private void ChangeTempMinute(int delta)
        {
            _tempMinute = (_tempMinute + delta + 60) % 60;
            SelectedMinute = _tempMinute; // 更新显示
        }

        private void SetTempTime(int hour, int minute)
        {
            _tempHour = hour;
            _tempMinute = minute;
            SelectedHour = _tempHour;
            SelectedMinute = _tempMinute;
        }

        private void SetTempTimeToNow()
        {
            var now = DateTime.Now;
            _tempHour = now.Hour;
            _tempMinute = now.Minute;
            SelectedHour = _tempHour;
            SelectedMinute = _tempMinute;
        }

        private void ConfirmTime()
        {
            // 应用临时值到 SelectedTime
            var today = DateTime.Today;
            SelectedTime = new DateTime(today.Year, today.Month, today.Day, _tempHour, _tempMinute, 0);
            IsDropDownOpen = false;
        }

        private void CancelTime()
        {
            // 不应用更改，直接关闭
            IsDropDownOpen = false;
        }

        #endregion
    }
}
