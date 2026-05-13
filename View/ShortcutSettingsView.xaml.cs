using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GuiPiao.ViewModel;

namespace GuiPiao.View;

/// <summary>
///     ShortcutSettingsView.xaml 的交互逻辑
/// </summary>
public partial class ShortcutSettingsView : UserControl
{
    private ShortcutSettingsViewModel? _shortcutVm;

    public ShortcutSettingsView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachViewModel();
        if (DataContext is ShortcutSettingsViewModel vm)
        {
            _shortcutVm = vm;
            vm.PropertyChanged += ShortcutVmOnPropertyChanged;
            ApplyImeForWaitingState(vm.IsWaitingForKey);
        }
    }

    private void DetachViewModel()
    {
        if (_shortcutVm != null)
        {
            _shortcutVm.PropertyChanged -= ShortcutVmOnPropertyChanged;
            _shortcutVm = null;
        }
    }

    private void ShortcutVmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShortcutSettingsViewModel.IsWaitingForKey))
            return;

        if (sender is not ShortcutSettingsViewModel vm)
            return;

        // 等布局与点击处理结束后再抢焦点，否则焦点仍留在搜索框/DataGrid，IME 仍会抢键
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () => ApplyImeForWaitingState(vm.IsWaitingForKey));
    }

    /// <summary>
    ///     录制快捷键时关闭本控件子树的 IME，并把焦点收到本页，避免中文输入法吞键后出现 ImeProcessed 等无效绑定。
    /// </summary>
    private void ApplyImeForWaitingState(bool waitingForKey)
    {
        InputMethod.SetIsInputMethodEnabled(this, !waitingForKey);
        if (waitingForKey)
            Keyboard.Focus(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 勿仅用 Window.GetWindow(this)：在部分宿主/加载顺序下 Loaded 时仍为 null，会导致从未订阅按键。
        // 在 UserControl 上监听 PreviewKeyDown：隧道从根到焦点，焦点在本页子控件时仍会经过本控件。
        PreviewKeyDown -= ShortcutSettings_PreviewKeyDown;
        PreviewKeyDown += ShortcutSettings_PreviewKeyDown;

        if (DataContext is ShortcutSettingsViewModel vm)
            ApplyImeForWaitingState(vm.IsWaitingForKey);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PreviewKeyDown -= ShortcutSettings_PreviewKeyDown;
        DetachViewModel();
        InputMethod.SetIsInputMethodEnabled(this, true);
    }

    /// <summary>
    ///     <see cref="Key.System" /> 与中文 IME 下的 <see cref="Key.ImeProcessed" /> / <see cref="Key.DeadCharProcessed" /> 用 <see cref="KeyEventArgs.SystemKey" /> 取真实键。
    /// </summary>
    private static Key ResolveLogicalKey(KeyEventArgs e)
    {
        if (e.Key == Key.System)
        {
            var sk = e.SystemKey;
            if (sk != Key.None && sk is not (Key.ImeProcessed or Key.DeadCharProcessed))
                return sk;
        }

        if (e.Key is Key.ImeProcessed or Key.DeadCharProcessed)
        {
            var sk = e.SystemKey;
            if (sk != Key.None && sk is not (Key.ImeProcessed or Key.DeadCharProcessed))
                return sk;
        }

        return e.Key;
    }

    private void ShortcutSettings_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var viewModel = DataContext as ShortcutSettingsViewModel;
        if (viewModel?.IsWaitingForKey != true)
            return;

        if (e.Key == Key.Escape)
        {
            viewModel.CancelEditCommand.Execute(null);
            e.Handled = true;
            return;
        }

        var key = ResolveLogicalKey(e);
        if (key is Key.ImeProcessed or Key.DeadCharProcessed)
        {
            e.Handled = true;
            return;
        }

        viewModel.HandleKeyInput(key, Keyboard.Modifiers);
        e.Handled = true;
    }

    /// <summary>
    ///     快捷键边框点击事件 - 开始编辑
    /// </summary>
    private void ShortcutKeyBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: ShortcutItemViewModel shortcut })
        {
            var viewModel = DataContext as ShortcutSettingsViewModel;
            viewModel?.StartEditCommand.Execute(shortcut);
        }
    }
}
