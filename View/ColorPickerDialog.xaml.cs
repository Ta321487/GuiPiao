using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GuiPiao.View
{
    /// <summary>
    /// 颜色选择对话框（完全自定义，无英文）
    /// </summary>
    public partial class ColorPickerDialog : Window
    {
        // 预设颜色（主题色）
        private static readonly Color[] PresetColors = new Color[]
        {
            Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green,
            Colors.Cyan, Colors.Blue, Colors.Purple, Colors.Magenta,
            Colors.Brown, Colors.Pink, Colors.Lime, Colors.Teal,
            Colors.Indigo, Colors.Violet, Colors.Gold, Colors.Silver
        };

        // 标准颜色（灰度 + 常用色）
        private static readonly Color[] StandardColors = new Color[]
        {
            Colors.White, Colors.LightGray, Colors.Gray, Colors.DarkGray, Colors.Black,
            Colors.LightCoral, Colors.Salmon, Colors.DarkRed, Colors.Maroon,
            Colors.OrangeRed, Colors.DarkOrange, Colors.Goldenrod, Colors.Olive,
            Colors.YellowGreen, Colors.ForestGreen, Colors.DarkGreen, Colors.SeaGreen,
            Colors.LightBlue, Colors.SkyBlue, Colors.SteelBlue, Colors.Navy,
            Colors.Lavender, Colors.Plum, Colors.MediumPurple, Colors.DarkViolet
        };

        /// <summary>
        /// 选中的颜色
        /// </summary>
        public Color? SelectedColor { get; private set; }

        /// <summary>
        /// 是否确认选择
        /// </summary>
        public bool IsConfirmed { get; private set; }

        /// <summary>
        /// 原始颜色（用于取消时恢复）
        /// </summary>
        private readonly string _originalAccentColor;

        /// <summary>
        /// 是否启用主题预览（影响全局主题颜色）
        /// </summary>
        private readonly bool _enableThemePreview;

        /// <summary>
        /// 预览回调函数（用于日志颜色等非主题颜色预览）
        /// </summary>
        private readonly Action<string>? _previewCallback;

        /// <summary>
        /// 取消回调函数（用于恢复原始状态）
        /// </summary>
        private readonly Action? _cancelCallback;

        /// <summary>
        /// 是否显示预览按钮
        /// </summary>
        private readonly bool _showPreviewButton;

        public ColorPickerDialog(Color? initialColor = null, bool enableThemePreview = true, Action<string>? previewCallback = null, Action? cancelCallback = null, bool showPreviewButton = true)
        {
            InitializeComponent();

            // 应用当前主题
            Services.ThemeManager.ApplyThemeToWindow(this);

            // 保存原始强调色
            _originalAccentColor = GetCurrentAccentColor();

            // 设置是否启用主题预览
            _enableThemePreview = enableThemePreview;

            // 设置预览和取消回调
            _previewCallback = previewCallback;
            _cancelCallback = cancelCallback;

            // 设置是否显示预览按钮
            _showPreviewButton = showPreviewButton;

            // 初始化颜色
            SelectedColor = initialColor ?? Colors.Blue;
        }

        /// <summary>
        /// 获取当前强调色
        /// </summary>
        private string GetCurrentAccentColor()
        {
            try
            {
                if (Application.Current.Resources["AccentBrush"] is SolidColorBrush brush)
                {
                    var color = brush.Color;
                    return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }
            }
            catch { }
            return "#0078D4"; // 默认微软蓝
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // 窗口渲染完成后生成颜色按钮（确保资源已加载）
            if (SelectedColor.HasValue)
            {
                UpdateUI(SelectedColor.Value);
            }
            GenerateColorButtons(PresetColorsPanel, PresetColors);
            GenerateColorButtons(StandardColorsPanel, StandardColors);

            // 控制预览按钮显示/隐藏
            if (!_showPreviewButton && PreviewButton != null)
            {
                PreviewButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 生成颜色按钮
        /// </summary>
        private void GenerateColorButtons(Panel container, Color[] colors)
        {
            // 检查资源是否可用
            Style? buttonStyle = null;
            try
            {
                buttonStyle = (Style)FindResource("ColorButtonStyle");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"无法找到 ColorButtonStyle 资源: {ex.Message}");
            }

            foreach (var color in colors)
            {
                var button = new Button
                {
                    Background = new SolidColorBrush(color),
                    Tag = color
                };

                // 如果找到样式则应用，否则使用默认设置
                if (buttonStyle != null)
                {
                    button.Style = buttonStyle;
                }
                else
                {
                    // 默认设置
                    button.Width = 24;
                    button.Height = 24;
                    button.Margin = new Thickness(2);
                    button.BorderThickness = new Thickness(1);
                    button.BorderBrush = new SolidColorBrush(Colors.Gray);
                    button.Cursor = Cursors.Hand;
                }

                button.Click += ColorButton_Click;
                container.Children.Add(button);
            }
        }

        /// <summary>
        /// 颜色按钮点击事件
        /// </summary>
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Color color)
            {
                SelectedColor = color;
                UpdateUI(color);
            }
        }

        /// <summary>
        /// 滑块值改变事件
        /// </summary>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 检查控件是否已初始化
            if (RedSlider == null || GreenSlider == null || BlueSlider == null)
                return;

            var color = Color.FromRgb(
                (byte)RedSlider.Value,
                (byte)GreenSlider.Value,
                (byte)BlueSlider.Value);
            SelectedColor = color;
            UpdatePreview(color);
            UpdateHexValue(color);
        }

        /// <summary>
        /// 颜色文本框改变事件
        /// </summary>
        private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 检查控件是否已初始化
            if (RedValue == null || GreenValue == null || BlueValue == null)
                return;

            if (byte.TryParse(RedValue.Text, out byte r) &&
                byte.TryParse(GreenValue.Text, out byte g) &&
                byte.TryParse(BlueValue.Text, out byte b))
            {
                var color = Color.FromRgb(r, g, b);
                SelectedColor = color;
                UpdateSliders(color);
                UpdatePreview(color);
                UpdateHexValue(color);
            }
        }

        /// <summary>
        /// 十六进制文本框改变事件
        /// </summary>
        private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 检查控件是否已初始化
            if (HexValue == null)
                return;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(HexValue.Text);
                SelectedColor = color;
                UpdateSliders(color);
                UpdatePreview(color);
                UpdateColorValues(color);
            }
            catch { }
        }

        /// <summary>
        /// 更新整个 UI
        /// </summary>
        private void UpdateUI(Color color)
        {
            UpdateSliders(color);
            UpdateColorValues(color);
            UpdatePreview(color);
            UpdateHexValue(color);
        }

        /// <summary>
        /// 更新滑块
        /// </summary>
        private void UpdateSliders(Color color)
        {
            if (RedSlider == null || GreenSlider == null || BlueSlider == null)
                return;
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
        }

        /// <summary>
        /// 更新颜色值文本框
        /// </summary>
        private void UpdateColorValues(Color color)
        {
            if (RedValue == null || GreenValue == null || BlueValue == null)
                return;
            RedValue.Text = color.R.ToString();
            GreenValue.Text = color.G.ToString();
            BlueValue.Text = color.B.ToString();
        }

        /// <summary>
        /// 更新预览
        /// </summary>
        private void UpdatePreview(Color color)
        {
            if (ColorPreview == null)
                return;
            ColorPreview.Background = new SolidColorBrush(color);
        }

        /// <summary>
        /// 更新十六进制值
        /// </summary>
        private void UpdateHexValue(Color color)
        {
            if (HexValue == null)
                return;
            HexValue.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// 预览按钮点击
        /// </summary>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedColor.HasValue)
            {
                var hexColor = $"#{SelectedColor.Value.R:X2}{SelectedColor.Value.G:X2}{SelectedColor.Value.B:X2}";

                // 只有在启用主题预览时才应用全局主题颜色
                if (_enableThemePreview)
                {
                    Services.ThemeManager.ApplyAccentColor(hexColor);
                }

                // 调用预览回调（用于日志颜色等非主题颜色预览）
                _previewCallback?.Invoke(hexColor);
            }
        }

        /// <summary>
        /// 确认按钮点击
        /// </summary>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 恢复原始主题颜色
            Services.ThemeManager.ApplyAccentColor(_originalAccentColor);

            // 调用取消回调（用于恢复日志颜色等非主题颜色）
            _cancelCallback?.Invoke();

            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
