using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GuiPiao.View;

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
    private TextBox? _hourInput;
    private TextBox? _minuteInput;
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
    private bool _syncingSegmentText;

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
        if (SelectedTime.HasValue)
        {
            SelectedHour = SelectedTime.Value.Hour;
            SelectedMinute = SelectedTime.Value.Minute;
            _tempHour = SelectedHour;
            _tempMinute = SelectedMinute;
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _border = GetTemplateChild("Border") as Border;
        _hourUpButton = GetTemplateChild("PART_HourUp") as Button;

        if (_border != null)
        {
            _border.MouseLeftButtonDown += (s, e) =>
            {
                if (_popup != null)
                {
                    if (!IsDropDownOpen)
                    {
                        _popup.StaysOpen = true;
                        IsDropDownOpen = true;
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
                        IsDropDownOpen = false;
                    }
                }
                else
                {
                    IsDropDownOpen = !IsDropDownOpen;
                }

                e.Handled = true;
            };
        }

        _hourDownButton = GetTemplateChild("PART_HourDown") as Button;
        _minuteUpButton = GetTemplateChild("PART_MinuteUp") as Button;
        _minuteDownButton = GetTemplateChild("PART_MinuteDown") as Button;
        _hourInput = GetTemplateChild("PART_HourInput") as TextBox;
        _minuteInput = GetTemplateChild("PART_MinuteInput") as TextBox;
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

        WireSegmentInputs();

        if (_popup != null)
        {
            _popup.Opened += (s, e) =>
            {
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

                SelectedHour = _tempHour;
                SelectedMinute = _tempMinute;
                SyncSegmentInputsFromTemp();
            };

            _popup.Closed += (s, e) => { Debug.WriteLine("Popup Closed event fired"); };
        }

        if (_hourUpButton != null)
            _hourUpButton.Click += (s, e) => ChangeTempHour(1);

        if (_hourDownButton != null)
            _hourDownButton.Click += (s, e) => ChangeTempHour(-1);

        if (_minuteUpButton != null)
            _minuteUpButton.Click += (s, e) => ChangeTempMinute(1);

        if (_minuteDownButton != null)
            _minuteDownButton.Click += (s, e) => ChangeTempMinute(-1);

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

        if (_nowButton != null)
            _nowButton.Click += (s, e) => SetTempTimeToNow();

        if (_confirmButton != null)
            _confirmButton.Click += (s, e) => ConfirmTime();

        if (_cancelButton != null)
            _cancelButton.Click += (s, e) => CancelTime();

        UpdateTextBox();
        SyncSegmentInputsFromTemp();
        SetupPopupScrollHandling();
    }

    private void WireSegmentInputs()
    {
        if (_hourInput != null)
        {
            _hourInput.PreviewTextInput += (_, e) => OnSegmentPreviewTextInput(_hourInput, e, 23);
            _hourInput.PreviewKeyDown += OnHourPreviewKeyDown;
            _hourInput.GotKeyboardFocus += (_, _) => _hourInput.SelectAll();
            _hourInput.LostKeyboardFocus += (_, _) => CommitHourInput();
            _hourInput.TextChanged += (_, _) => OnHourTextChanged();
            DataObject.AddPastingHandler(_hourInput, OnSegmentPaste);
        }

        if (_minuteInput != null)
        {
            _minuteInput.PreviewTextInput += (_, e) => OnSegmentPreviewTextInput(_minuteInput, e, 59);
            _minuteInput.PreviewKeyDown += OnMinutePreviewKeyDown;
            _minuteInput.GotKeyboardFocus += (_, _) => _minuteInput.SelectAll();
            _minuteInput.LostKeyboardFocus += (_, _) => CommitMinuteInput();
            _minuteInput.TextChanged += (_, _) => OnMinuteTextChanged();
            DataObject.AddPastingHandler(_minuteInput, OnSegmentPaste);
        }
    }

    private void OnHourTextChanged()
    {
        if (_syncingSegmentText || _hourInput == null) return;
        if (_hourInput.Text?.Length == 2 &&
            int.TryParse(_hourInput.Text, out var hour) &&
            hour <= 23)
        {
            CommitHourInput();
            _minuteInput?.Focus();
            _minuteInput?.SelectAll();
        }
    }

    private void OnMinuteTextChanged()
    {
        if (_syncingSegmentText || _minuteInput == null) return;
        if (_minuteInput.Text?.Length == 2 &&
            int.TryParse(_minuteInput.Text, out var minute) &&
            minute <= 59)
            CommitMinuteInput();
    }

    private static void OnSegmentPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (!Regex.IsMatch(text, @"^\d{1,2}$"))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    private void OnSegmentPreviewTextInput(TextBox box, TextCompositionEventArgs e, int maxValue)
    {
        if (e.Text.Length == 0 || !char.IsDigit(e.Text[0]))
        {
            e.Handled = true;
            return;
        }

        var digit = e.Text[0];
        var selectionStart = box.SelectionStart;
        var selectionLength = box.SelectionLength;
        var current = box.Text ?? string.Empty;
        var next = current.Remove(selectionStart, selectionLength).Insert(selectionStart, digit.ToString());

        if (next.Length > 2)
        {
            e.Handled = true;
            return;
        }

        if (next.Length == 2)
        {
            if (!int.TryParse(next, out var value) || value > maxValue)
            {
                e.Handled = true;
                return;
            }
        }
        else if (next.Length == 1)
        {
            var d = digit - '0';
            if (maxValue == 23 && d > 2)
            {
                e.Handled = true;
                return;
            }

            if (maxValue == 59 && d > 5)
            {
                e.Handled = true;
                return;
            }
        }
    }

    private void OnHourPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Tab)
        {
            CommitHourInput();
            _minuteInput?.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Right && _hourInput != null &&
            _hourInput.CaretIndex >= (_hourInput.Text?.Length ?? 0) &&
            _hourInput.SelectionLength == 0)
        {
            _minuteInput?.Focus();
            e.Handled = true;
        }
    }

    private void OnMinutePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitMinuteInput();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Left && _minuteInput != null &&
            _minuteInput.CaretIndex == 0 &&
            _minuteInput.SelectionLength == 0)
        {
            _hourInput?.Focus();
            e.Handled = true;
        }
    }

    private void CommitHourInput()
    {
        if (_hourInput == null) return;
        var text = (_hourInput.Text ?? string.Empty).Trim();
        if (!int.TryParse(text, out var hour))
            hour = _tempHour;
        hour = Math.Clamp(hour, 0, 23);
        _tempHour = hour;
        SelectedHour = hour;
        SetSegmentText(_hourInput, hour);
    }

    private void CommitMinuteInput()
    {
        if (_minuteInput == null) return;
        var text = (_minuteInput.Text ?? string.Empty).Trim();
        if (!int.TryParse(text, out var minute))
            minute = _tempMinute;
        minute = Math.Clamp(minute, 0, 59);
        _tempMinute = minute;
        SelectedMinute = minute;
        SetSegmentText(_minuteInput, minute);
    }

    private void SyncSegmentInputsFromTemp()
    {
        SetSegmentText(_hourInput, _tempHour);
        SetSegmentText(_minuteInput, _tempMinute);
    }

    private void SetSegmentText(TextBox? box, int value)
    {
        if (box == null) return;
        _syncingSegmentText = true;
        try
        {
            var text = value.ToString("D2");
            if (box.Text != text)
                box.Text = text;
        }
        finally
        {
            _syncingSegmentText = false;
        }
    }

    private void SetupPopupScrollHandling()
    {
        if (_popup == null) return;

        _popup.Opened += (s, e) =>
        {
            var scrollViewer = FindParentScrollViewer(this);
            if (scrollViewer != null) scrollViewer.ScrollChanged += OnParentScrollChanged;
        };

        _popup.Closed += (s, e) =>
        {
            var scrollViewer = FindParentScrollViewer(this);
            if (scrollViewer != null) scrollViewer.ScrollChanged -= OnParentScrollChanged;
        };
    }

    private void OnParentScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0 || e.HorizontalChange != 0) IsDropDownOpen = false;
    }

    private ScrollViewer? FindParentScrollViewer(DependencyObject child)
    {
        var parent = VisualTreeHelper.GetParent(child);

        while (parent != null)
        {
            if (parent is ScrollViewer scrollViewer) return scrollViewer;
            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
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
            picker._tempHour = newTime.Hour;
            picker._tempMinute = newTime.Minute;
            picker.SyncSegmentInputsFromTemp();
        }

        picker.UpdateTextBox();
        picker.RaiseEvent(new RoutedEventArgs(SelectedTimeChangedEvent, picker));
    }

    private static void OnSelectedHourChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ThemedTimePicker)d;
        if (!picker._syncingSegmentText)
            picker.SetSegmentText(picker._hourInput, picker.SelectedHour);
    }

    private static void OnSelectedMinuteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ThemedTimePicker)d;
        if (!picker._syncingSegmentText)
            picker.SetSegmentText(picker._minuteInput, picker.SelectedMinute);
    }

    private void UpdateTextBox()
    {
        if (_textBox != null)
        {
            if (SelectedTime.HasValue)
                _textBox.Text = $"{SelectedTime.Value.Hour:D2}:{SelectedTime.Value.Minute:D2}";
            else
                _textBox.Text = string.Empty;
        }
    }

    private void ChangeTempHour(int delta)
    {
        _tempHour = (_tempHour + delta + 24) % 24;
        SelectedHour = _tempHour;
        SyncSegmentInputsFromTemp();
    }

    private void ChangeTempMinute(int delta)
    {
        _tempMinute = (_tempMinute + delta + 60) % 60;
        SelectedMinute = _tempMinute;
        SyncSegmentInputsFromTemp();
    }

    private void SetTempTime(int hour, int minute)
    {
        _tempHour = hour;
        _tempMinute = minute;
        SelectedHour = _tempHour;
        SelectedMinute = _tempMinute;
        SyncSegmentInputsFromTemp();
    }

    private void SetTempTimeToNow()
    {
        var now = DateTime.Now;
        _tempHour = now.Hour;
        _tempMinute = now.Minute;
        SelectedHour = _tempHour;
        SelectedMinute = _tempMinute;
        SyncSegmentInputsFromTemp();
    }

    private void ConfirmTime()
    {
        CommitHourInput();
        CommitMinuteInput();
        var today = DateTime.Today;
        SelectedTime = new DateTime(today.Year, today.Month, today.Day, _tempHour, _tempMinute, 0);
        IsDropDownOpen = false;
    }

    private void CancelTime()
    {
        IsDropDownOpen = false;
    }

    #endregion
}
