using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using GuiPiao.Model;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

public partial class TicketPreviewWindow : Window
{
    private bool _layoutSurfaceDragActive;

    private bool _spacePanDragging;
    private Point _spacePanStartMouse;
    private double _spacePanStartOffsetX;
    private double _spacePanStartOffsetY;

    public TicketPreviewWindow()
    {
        InitializeComponent();
        Closed += TicketPreviewWindow_Closed;
        Loaded += TicketPreviewWindow_Loaded;
    }

    private static bool IsTextInputFocused()
    {
        var el = Keyboard.FocusedElement;
        return el is TextBoxBase or PasswordBox;
    }

    private static bool IsComboBoxFocused() => Keyboard.FocusedElement is ComboBox;

    /// <summary>
    ///     空格用于画布平移。版式工作台中即便焦点在 CheckBox/Button 上也拦截空格，
    ///     避免「成组移动」等复选框被误切换；文本框与 ComboBox 仍保留空格输入。
    /// </summary>
    private bool IsSpacePanKeyboardContext()
    {
        if (IsTextInputFocused() || IsComboBoxFocused())
            return false;

        if (DataContext is TicketPreviewViewModel { IsLayoutWorkbench: true })
            return true;

        // 行程预览：焦点在按钮/复选框时让空格走控件默认（勾选），其余可平移
        return Keyboard.FocusedElement is not ButtonBase;
    }

    private void TicketPreviewWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryHandleLayoutWorkbenchFontSizeShortcuts(e))
            return;

        if (e.Key != Key.Space || e.IsRepeat || !IsSpacePanKeyboardContext()) return;
        Mouse.OverrideCursor = Cursors.Hand;
        e.Handled = true;
    }

    /// <summary>版面参数调整：Ctrl+C / Ctrl+V 复制/粘贴字号（焦点在文本框时不拦截，保留正常复制粘贴）。</summary>
    private bool TryHandleLayoutWorkbenchFontSizeShortcuts(KeyEventArgs e)
    {
        if (DataContext is not TicketPreviewViewModel vm || !vm.IsLayoutWorkbench)
            return false;

        if (Keyboard.Modifiers != ModifierKeys.Control || IsTextInputFocused())
            return false;

        if (e.Key == Key.C && vm.CanCopyWorkbenchFontSize)
        {
            vm.CopyWorkbenchFontSizeCommand.Execute(null);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.V && vm.CanPasteWorkbenchFontSize)
        {
            vm.PasteWorkbenchFontSizeCommand.Execute(null);
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void TicketPreviewWindow_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space) return;
        if (_spacePanDragging && TicketPreviewScrollHost != null)
            EndSpacePan(TicketPreviewScrollHost);
        else
            Mouse.OverrideCursor = null;
        if (IsSpacePanKeyboardContext())
            e.Handled = true;
    }

    private void TicketPreviewWindow_Deactivated(object? sender, EventArgs e) => ClearSpacePanState();

    private void ClearSpacePanState()
    {
        if (_spacePanDragging && TicketPreviewScrollHost != null)
            TicketPreviewScrollHost.ReleaseMouseCapture();
        _spacePanDragging = false;
        Mouse.OverrideCursor = null;
    }

    private void TicketPreviewScrollHost_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        if (!Keyboard.IsKeyDown(Key.Space) || !IsSpacePanKeyboardContext()) return;
        BeginScrollPan(sv, e.GetPosition(sv));
        e.Handled = true;
    }

    /// <summary>空格拖动 / 点空白拖动画布：按指针位移滚动 ScrollViewer。</summary>
    private void BeginScrollPan(ScrollViewer sv, Point mouseInSv)
    {
        _spacePanDragging = true;
        _spacePanStartMouse = mouseInSv;
        _spacePanStartOffsetX = sv.HorizontalOffset;
        _spacePanStartOffsetY = sv.VerticalOffset;
        Mouse.OverrideCursor = Cursors.Hand;
        sv.CaptureMouse();
    }

    private void TicketPreviewScrollHost_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_spacePanDragging || sender is not ScrollViewer sv) return;
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndSpacePan(sv);
            return;
        }

        var pos = e.GetPosition(sv);
        var targetX = _spacePanStartOffsetX - (pos.X - _spacePanStartMouse.X);
        var targetY = _spacePanStartOffsetY - (pos.Y - _spacePanStartMouse.Y);
        var maxX = Math.Max(0, sv.ExtentWidth - sv.ViewportWidth);
        var maxY = Math.Max(0, sv.ExtentHeight - sv.ViewportHeight);
        sv.ScrollToHorizontalOffset(Math.Min(maxX, Math.Max(0, targetX)));
        sv.ScrollToVerticalOffset(Math.Min(maxY, Math.Max(0, targetY)));
        e.Handled = true;
    }

    private void TicketPreviewScrollHost_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        if (!_spacePanDragging) return;
        EndSpacePan(sv);
        e.Handled = true;
    }

    private void TicketPreviewScrollHost_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;
        if (_spacePanDragging)
            EndSpacePan(sv);
    }

    private void EndSpacePan(ScrollViewer sv)
    {
        _spacePanDragging = false;
        sv.ReleaseMouseCapture();
        if (!Keyboard.IsKeyDown(Key.Space))
            Mouse.OverrideCursor = null;
    }

    private void TicketPreviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TicketPreviewViewModel vm)
            vm.ResyncLayoutEditorAfterViewLoaded();
        PushTicketPreviewHostSizeToViewModel();
    }

    private void PushTicketPreviewHostSizeToViewModel()
    {
        if (DataContext is not TicketPreviewViewModel vm) return;
        if (TicketPreviewScrollHost == null) return;
        vm.NotifyTicketPreviewHostSize(TicketPreviewScrollHost.ActualWidth, TicketPreviewScrollHost.ActualHeight);
    }

    private void TicketPreviewScrollHost_Loaded(object sender, RoutedEventArgs e) => PushTicketPreviewHostSizeToViewModel();

    private void TicketPreviewScrollHost_SizeChanged(object sender, SizeChangedEventArgs e) => PushTicketPreviewHostSizeToViewModel();

    public TicketPreviewWindow(GuiPiao.Model.TripItem tripItem) : this(tripItem, TicketPreviewSessionMode.UserTripPreview)
    {
    }

    public TicketPreviewWindow(GuiPiao.Model.TripItem tripItem, TicketPreviewSessionMode sessionMode) : this()
    {
        ApplySession(sessionMode);
        if (DataContext is TicketPreviewViewModel viewModel) viewModel.SetTripItem(tripItem);
    }

    public TicketPreviewWindow(IReadOnlyList<GuiPiao.Model.TripItem> tripItems) : this(tripItems, TicketPreviewSessionMode.UserTripPreview)
    {
    }

    public TicketPreviewWindow(IReadOnlyList<GuiPiao.Model.TripItem> tripItems, TicketPreviewSessionMode sessionMode) : this()
    {
        ApplySession(sessionMode);
        if (DataContext is TicketPreviewViewModel viewModel) viewModel.SetSourceTrips(tripItems);
    }

    private void ApplySession(TicketPreviewSessionMode sessionMode)
    {
        if (DataContext is TicketPreviewViewModel viewModel)
            viewModel.SessionMode = sessionMode;

        // 预览以票面为主；版式编辑需要左侧参数栏
        if (sessionMode == TicketPreviewSessionMode.UserTripPreview)
        {
            Width = 960;
            MinWidth = 720;
            Title = "票面预览";
        }
        else
        {
            Width = 1180;
            MinWidth = 1040;
            Title = "编辑票面版式";
        }
    }

    private void TicketPreviewWindow_Closed(object? sender, System.EventArgs e)
    {
        ClearSpacePanState();
        if (DataContext is TicketPreviewViewModel vm)
        {
            vm.TryPersistLayoutOnWorkbenchClosing();
            vm.DetachWindowListeners();
        }
    }

    private void TicketPreviewScrollHost_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is TicketPreviewViewModel viewModel)
        {
            viewModel.HandleMouseWheel(e.Delta);
            e.Handled = true;
        }
    }

    private void LayoutEditOverlay_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not TicketPreviewViewModel vm || !vm.IsVisualLayoutEdit) return;
        // 空格拖动画布优先（隧道阶段 ScrollViewer 已处理时一般到不了这里）
        if (_spacePanDragging) return;

        var pos = e.GetPosition(TicketPreviewSurface);

        var overlayWasHitTestVisible = LayoutEditOverlay.IsHitTestVisible;
        TicketFaceLayoutElementKind? hitKind = null;
        try
        {
            LayoutEditOverlay.IsHitTestVisible = false;
            // 单次 HitTest 只取最顶层；811 底图 Image、细线箭头 Polyline 等常导致无法解析到带 Kind 的块。
            // 从前到后遍历命中栈，直到父链上出现 LayoutWorkbenchHit.Kind。
            VisualTreeHelper.HitTest(
                TicketPreviewSurface,
                static _ => HitTestFilterBehavior.Continue,
                r =>
                {
                    var k = LayoutWorkbenchHit.TryResolveKind(r.VisualHit);
                    if (k.HasValue)
                    {
                        hitKind = k;
                        return HitTestResultBehavior.Stop;
                    }

                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(pos));
        }
        finally
        {
            LayoutEditOverlay.IsHitTestVisible = overlayWasHitTestVisible;
        }

        if (!hitKind.HasValue)
        {
            // 纯空白：拖动画布（滚动），不移动当前选中元素
            if (TicketPreviewScrollHost != null)
                BeginScrollPan(TicketPreviewScrollHost, e.GetPosition(TicketPreviewScrollHost));
            e.Handled = true;
            return;
        }

        vm.SelectWorkbenchLayoutElementByKind(hitKind.Value);
        vm.BeginWorkbenchSurfaceDrag(pos.X, pos.Y);
        _layoutSurfaceDragActive = true;
        if (sender is UIElement u)
            u.CaptureMouse();
        e.Handled = true;
    }

    private void LayoutEditOverlay_OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_layoutSurfaceDragActive || DataContext is not TicketPreviewViewModel vm) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(TicketPreviewSurface);
        vm.ApplyWorkbenchSurfaceDrag(pos.X, pos.Y);
        e.Handled = true;
    }

    private void LayoutEditOverlay_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _layoutSurfaceDragActive = false;
        if (sender is UIElement u)
            u.ReleaseMouseCapture();
    }
}
