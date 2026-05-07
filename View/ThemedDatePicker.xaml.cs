using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GuiPiao.View;

/// <summary>
///     日历显示模式
/// </summary>
public enum CalendarDisplayMode
{
    Days, // 日期视图
    Months, // 月份视图
    Years // 年份视图
}

/// <summary>
///     支持主题的日期选择器控件
/// </summary>
public class ThemedDatePicker : Control
{
    #region 事件

    public event EventHandler<DateChangedEventArgs>? SelectedDateChanged;

    #endregion

    #region 依赖属性

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(ThemedDatePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedDateChanged));

    public static readonly DependencyProperty DisplayDateProperty =
        DependencyProperty.Register(nameof(DisplayDate), typeof(DateTime), typeof(ThemedDatePicker),
            new PropertyMetadata(DateTime.Today, OnDisplayDateChanged));

    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(ThemedDatePicker),
            new PropertyMetadata(false, OnIsDropDownOpenChanged));

    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(ThemedDatePicker),
            new PropertyMetadata("选择日期..."));

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(ThemedDatePicker),
            new PropertyMetadata(""));

    #endregion

    #region 属性

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime DisplayDate
    {
        get => (DateTime)GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value);
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

    #region 私有字段

    private TextBox? _textBox;
    private Popup? _popup;
    private Border? _border;
    private UniformGrid? _daysGrid;
    private UniformGrid? _monthsGrid;
    private UniformGrid? _yearsGrid;
    private Grid? _weekDaysGrid;
    private Button? _dropDownButton;
    private Button? _previousButton;
    private Button? _nextButton;
    private Button? _previousYearButton;
    private Button? _nextYearButton;
    private Button? _headerButton;
    private readonly Button[] _dayButtons = new Button[42];
    private readonly Button[] _monthButtons = new Button[12];
    private readonly Button[] _yearButtons = new Button[12];
    private CalendarDisplayMode _displayMode = CalendarDisplayMode.Days;
    private int _yearRangeStart;

    private readonly string[] _monthNames =
        { "1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月" };

    #endregion

    #region 构造函数

    static ThemedDatePicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedDatePicker),
            new FrameworkPropertyMetadata(typeof(ThemedDatePicker)));
    }

    public ThemedDatePicker()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateHeaderText();
    }

    #endregion

    #region 生命周期

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTextBoxDisplay();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 解除旧事件绑定
        UnsubscribeEvents();

        // 获取模板部件
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _border = GetTemplateChild("Border") as Border;
        _daysGrid = GetTemplateChild("PART_DaysGrid") as UniformGrid;
        _monthsGrid = GetTemplateChild("PART_MonthsGrid") as UniformGrid;
        _yearsGrid = GetTemplateChild("PART_YearsGrid") as UniformGrid;
        _weekDaysGrid = GetTemplateChild("PART_WeekDaysGrid") as Grid;
        _dropDownButton = GetTemplateChild("PART_DropDownButton") as Button;
        _previousButton = GetTemplateChild("PART_PreviousButton") as Button;
        _nextButton = GetTemplateChild("PART_NextButton") as Button;
        _previousYearButton = GetTemplateChild("PART_PreviousYearButton") as Button;
        _nextYearButton = GetTemplateChild("PART_NextYearButton") as Button;
        _headerButton = GetTemplateChild("PART_HeaderButton") as Button;

        // 绑定新事件
        SubscribeEvents();

        // 初始化按钮
        InitializeDayButtons();
        InitializeMonthButtons();
        InitializeYearButtons();

        UpdateTextBoxDisplay();
        UpdateCalendarDisplay();
    }

    private void UnsubscribeEvents()
    {
        if (_dropDownButton != null)
            _dropDownButton.Click -= OnDropDownButtonClick;
        if (_previousButton != null)
            _previousButton.Click -= OnPreviousButtonClick;
        if (_nextButton != null)
            _nextButton.Click -= OnNextButtonClick;
        if (_previousYearButton != null)
            _previousYearButton.Click -= OnPreviousYearButtonClick;
        if (_nextYearButton != null)
            _nextYearButton.Click -= OnNextYearButtonClick;
        if (_headerButton != null)
            _headerButton.Click -= OnHeaderButtonClick;
        if (_border != null)
            _border.MouseLeftButtonDown -= OnBorderMouseLeftButtonDown;
        if (_textBox != null) _textBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
        if (_popup != null)
            _popup.Closed -= OnPopupClosed;
    }

    private void SubscribeEvents()
    {
        if (_dropDownButton != null)
            _dropDownButton.Click += OnDropDownButtonClick;
        if (_previousButton != null)
            _previousButton.Click += OnPreviousButtonClick;
        if (_nextButton != null)
            _nextButton.Click += OnNextButtonClick;
        if (_previousYearButton != null)
            _previousYearButton.Click += OnPreviousYearButtonClick;
        if (_nextYearButton != null)
            _nextYearButton.Click += OnNextYearButtonClick;
        if (_headerButton != null)
            _headerButton.Click += OnHeaderButtonClick;
        if (_border != null)
        {
            _border.MouseLeftButtonDown += OnBorderMouseLeftButtonDown;
            _border.Cursor = Cursors.Hand;
        }

        if (_textBox != null)
        {
            _textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
            _textBox.IsReadOnly = true;
            _textBox.IsReadOnlyCaretVisible = false;
            _textBox.IsHitTestVisible = false; // 让点击穿透到 Border
        }

        if (_popup != null)
            _popup.Closed += OnPopupClosed;
    }

    #endregion

    #region 事件处理

    private void OnDropDownButtonClick(object sender, RoutedEventArgs e)
    {
        TogglePopup();
    }

    private void OnPreviousButtonClick(object sender, RoutedEventArgs e)
    {
        switch (_displayMode)
        {
            case CalendarDisplayMode.Days:
                DisplayDate = DisplayDate.AddMonths(-1);
                break;
            case CalendarDisplayMode.Months:
                DisplayDate = DisplayDate.AddYears(-1);
                break;
            case CalendarDisplayMode.Years:
                _yearRangeStart -= 12;
                UpdateYearsDisplay();
                break;
        }
    }

    private void OnNextButtonClick(object sender, RoutedEventArgs e)
    {
        switch (_displayMode)
        {
            case CalendarDisplayMode.Days:
                DisplayDate = DisplayDate.AddMonths(1);
                break;
            case CalendarDisplayMode.Months:
                DisplayDate = DisplayDate.AddYears(1);
                break;
            case CalendarDisplayMode.Years:
                _yearRangeStart += 12;
                UpdateYearsDisplay();
                break;
        }
    }

    private void OnPreviousYearButtonClick(object sender, RoutedEventArgs e)
    {
        if (_displayMode == CalendarDisplayMode.Years)
        {
            _yearRangeStart -= 12;
            UpdateYearsDisplay();
        }
    }

    private void OnNextYearButtonClick(object sender, RoutedEventArgs e)
    {
        if (_displayMode == CalendarDisplayMode.Years)
        {
            _yearRangeStart += 12;
            UpdateYearsDisplay();
        }
    }

    private void OnHeaderButtonClick(object sender, RoutedEventArgs e)
    {
        switch (_displayMode)
        {
            case CalendarDisplayMode.Days:
                SwitchToMode(CalendarDisplayMode.Months);
                break;
            case CalendarDisplayMode.Months:
                _yearRangeStart = DisplayDate.Year - 6;
                SwitchToMode(CalendarDisplayMode.Years);
                break;
            case CalendarDisplayMode.Years:
                // 已经是年份视图，点击返回今天
                DisplayDate = DateTime.Today;
                SwitchToMode(CalendarDisplayMode.Days);
                break;
        }
    }

    private void OnDayButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DateTime date)
        {
            SelectedDate = date;
            DisplayDate = date;
            IsDropDownOpen = false;
        }
    }

    private void OnMonthButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int month)
        {
            DisplayDate = new DateTime(DisplayDate.Year, month, 1);
            SwitchToMode(CalendarDisplayMode.Days);
        }
    }

    private void OnYearButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int year)
        {
            DisplayDate = new DateTime(year, DisplayDate.Month, 1);
            SwitchToMode(CalendarDisplayMode.Months);
        }
    }

    private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            TogglePopup();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            IsDropDownOpen = false;
            e.Handled = true;
        }
    }

    private void OnBorderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_popup != null)
        {
            if (!IsDropDownOpen)
            {
                // 要打开 Popup，先设置 StaysOpen = true，防止点击事件导致立即关闭
                _popup.StaysOpen = true;
                IsDropDownOpen = true;
                // 延迟恢复 StaysOpen，确保 Popup 完全打开且鼠标事件处理完毕
                Task.Run(async () =>
                {
                    await Task.Delay(100);
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
            TogglePopup();
        }

        e.Handled = true;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        IsDropDownOpen = false;
        // 关闭时重置为日期视图
        _displayMode = CalendarDisplayMode.Days;
        UpdateCalendarDisplay();
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ThemedDatePicker)d;
        picker.UpdateTextBoxDisplay();
        picker.UpdateCalendarDisplay();

        picker.SelectedDateChanged?.Invoke(picker, new DateChangedEventArgs(
            e.OldValue as DateTime?, e.NewValue as DateTime?));
    }

    private static void OnDisplayDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ThemedDatePicker)d;
        picker.UpdateHeaderText();
        picker.UpdateCalendarDisplay();
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ThemedDatePicker)d;
        if ((bool)e.NewValue) picker.UpdateCalendarDisplay();
    }

    #endregion

    #region 私有方法

    private void TogglePopup()
    {
        IsDropDownOpen = !IsDropDownOpen;
    }

    private void SwitchToMode(CalendarDisplayMode mode)
    {
        _displayMode = mode;

        // 隐藏所有视图
        if (_daysGrid != null) _daysGrid.Visibility = Visibility.Collapsed;
        if (_monthsGrid != null) _monthsGrid.Visibility = Visibility.Collapsed;
        if (_yearsGrid != null) _yearsGrid.Visibility = Visibility.Collapsed;
        if (_weekDaysGrid != null) _weekDaysGrid.Visibility = Visibility.Collapsed;

        // 显示对应视图
        switch (mode)
        {
            case CalendarDisplayMode.Days:
                if (_daysGrid != null) _daysGrid.Visibility = Visibility.Visible;
                if (_weekDaysGrid != null) _weekDaysGrid.Visibility = Visibility.Visible;
                if (_previousYearButton != null) _previousYearButton.Visibility = Visibility.Collapsed;
                if (_nextYearButton != null) _nextYearButton.Visibility = Visibility.Collapsed;
                break;
            case CalendarDisplayMode.Months:
                if (_monthsGrid != null) _monthsGrid.Visibility = Visibility.Visible;
                if (_previousYearButton != null) _previousYearButton.Visibility = Visibility.Collapsed;
                if (_nextYearButton != null) _nextYearButton.Visibility = Visibility.Collapsed;
                break;
            case CalendarDisplayMode.Years:
                if (_yearsGrid != null) _yearsGrid.Visibility = Visibility.Visible;
                if (_previousYearButton != null) _previousYearButton.Visibility = Visibility.Visible;
                if (_nextYearButton != null) _nextYearButton.Visibility = Visibility.Visible;
                break;
        }

        UpdateHeaderText();
        UpdateCalendarDisplay();
    }

    private void UpdateHeaderText()
    {
        HeaderText = _displayMode switch
        {
            CalendarDisplayMode.Days => $"{DisplayDate.Year}年{DisplayDate.Month}月",
            CalendarDisplayMode.Months => $"{DisplayDate.Year}年",
            CalendarDisplayMode.Years => $"{_yearRangeStart} - {_yearRangeStart + 11}",
            _ => ""
        };
    }

    private void InitializeDayButtons()
    {
        if (_daysGrid == null) return;

        _daysGrid.Children.Clear();

        for (var i = 0; i < 42; i++)
        {
            var button = new Button
            {
                Style = (Style)FindResource("CalendarDayButtonStyle"),
                Content = ""
            };
            button.Click += OnDayButtonClick;
            _dayButtons[i] = button;
            _daysGrid.Children.Add(button);
        }
    }

    private void InitializeMonthButtons()
    {
        if (_monthsGrid == null) return;

        _monthsGrid.Children.Clear();

        for (var i = 0; i < 12; i++)
        {
            var month = i + 1;
            var button = new Button
            {
                Style = (Style)FindResource("CalendarMonthYearButtonStyle"),
                Content = _monthNames[i],
                Tag = month
            };
            button.Click += OnMonthButtonClick;
            _monthButtons[i] = button;
            _monthsGrid.Children.Add(button);
        }
    }

    private void InitializeYearButtons()
    {
        if (_yearsGrid == null) return;

        _yearsGrid.Children.Clear();

        for (var i = 0; i < 12; i++)
        {
            var button = new Button
            {
                Style = (Style)FindResource("CalendarMonthYearButtonStyle"),
                Content = ""
            };
            button.Click += OnYearButtonClick;
            _yearButtons[i] = button;
            _yearsGrid.Children.Add(button);
        }
    }

    private void UpdateCalendarDisplay()
    {
        switch (_displayMode)
        {
            case CalendarDisplayMode.Days:
                UpdateDaysDisplay();
                break;
            case CalendarDisplayMode.Months:
                UpdateMonthsDisplay();
                break;
            case CalendarDisplayMode.Years:
                UpdateYearsDisplay();
                break;
        }
    }

    private void UpdateDaysDisplay()
    {
        if (_daysGrid == null) return;

        var year = DisplayDate.Year;
        var month = DisplayDate.Month;

        // 获取该月第一天
        var firstDayOfMonth = new DateTime(year, month, 1);
        // 获取该月最后一天
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        // 获取第一天是星期几（0=周日）
        var startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        // 获取该月天数
        var daysInMonth = lastDayOfMonth.Day;

        // 清空所有按钮
        for (var i = 0; i < 42; i++)
        {
            var button = _dayButtons[i];
            button.Content = "";
            button.Tag = null;
            button.Visibility = Visibility.Hidden;
            button.ClearValue(BackgroundProperty);
            button.ClearValue(ForegroundProperty);
            button.BorderThickness = new Thickness(0);
        }

        // 填充日期按钮
        for (var day = 1; day <= daysInMonth; day++)
        {
            var index = startDayOfWeek + day - 1;
            if (index < 42)
            {
                var button = _dayButtons[index];
                var date = new DateTime(year, month, day);
                button.Content = day.ToString();
                button.Tag = date;
                button.Visibility = Visibility.Visible;

                // 设置选中状态样式
                if (SelectedDate.HasValue && SelectedDate.Value.Date == date.Date)
                {
                    button.Background = (Brush)FindResource("AccentBrush");
                    button.Foreground = Brushes.White;
                }
                // 设置今天样式
                else if (date.Date == DateTime.Today)
                {
                    button.BorderBrush = (Brush)FindResource("AccentBrush");
                    button.BorderThickness = new Thickness(1);
                    button.Foreground = (Brush)FindResource("AccentBrush");
                }
                // 设置其他日期样式
                else
                {
                    button.Foreground = (Brush)FindResource("TextPrimaryBrush");
                }
            }
        }

        // 显示上月的日期（灰色）
        var prevMonth = firstDayOfMonth.AddMonths(-1);
        var daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        for (var i = 0; i < startDayOfWeek; i++)
        {
            var day = daysInPrevMonth - startDayOfWeek + i + 1;
            var button = _dayButtons[i];
            var date = new DateTime(prevMonth.Year, prevMonth.Month, day);
            button.Content = day.ToString();
            button.Tag = date;
            button.Visibility = Visibility.Visible;
            button.ClearValue(BackgroundProperty);
            button.BorderThickness = new Thickness(0);
            button.Foreground = (Brush)FindResource("CalendarOtherMonthBrush");
        }

        // 显示下月的日期（灰色）
        var nextMonth = firstDayOfMonth.AddMonths(1);
        var daysShown = startDayOfWeek + daysInMonth;
        var nextMonthDay = 1;
        for (var i = daysShown; i < 42; i++)
        {
            var button = _dayButtons[i];
            var date = new DateTime(nextMonth.Year, nextMonth.Month, nextMonthDay);
            button.Content = nextMonthDay.ToString();
            button.Tag = date;
            button.Visibility = Visibility.Visible;
            button.ClearValue(BackgroundProperty);
            button.BorderThickness = new Thickness(0);
            button.Foreground = (Brush)FindResource("CalendarOtherMonthBrush");
            nextMonthDay++;
        }
    }

    private void UpdateMonthsDisplay()
    {
        for (var i = 0; i < 12; i++)
        {
            var button = _monthButtons[i];
            var month = i + 1;

            // 设置选中状态样式
            if (DisplayDate.Month == month)
            {
                button.Background = (Brush)FindResource("AccentBrush");
                button.Foreground = Brushes.White;
            }
            else
            {
                button.ClearValue(BackgroundProperty);
                button.ClearValue(ForegroundProperty);
            }
        }
    }

    private void UpdateYearsDisplay()
    {
        if (_yearRangeStart == 0) _yearRangeStart = DisplayDate.Year - 6;

        for (var i = 0; i < 12; i++)
        {
            var button = _yearButtons[i];
            var year = _yearRangeStart + i;
            button.Content = year.ToString();
            button.Tag = year;

            // 设置选中状态样式
            if (DisplayDate.Year == year)
            {
                button.Background = (Brush)FindResource("AccentBrush");
                button.Foreground = Brushes.White;
            }
            else
            {
                button.ClearValue(BackgroundProperty);
                button.ClearValue(ForegroundProperty);
            }
        }

        UpdateHeaderText();
    }

    private void UpdateTextBoxDisplay()
    {
        if (_textBox == null) return;

        _textBox.Text = SelectedDate.HasValue
            ? SelectedDate.Value.ToString("yyyy/M/d")
            : Watermark;
    }

    #endregion
}

/// <summary>
///     日期改变事件参数
/// </summary>
public class DateChangedEventArgs : EventArgs
{
    public DateChangedEventArgs(DateTime? oldDate, DateTime? newDate)
    {
        OldDate = oldDate;
        NewDate = newDate;
    }

    public DateTime? OldDate { get; }
    public DateTime? NewDate { get; }
}