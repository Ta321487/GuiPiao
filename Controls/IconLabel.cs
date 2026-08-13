using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GuiPiao.Icons;

namespace GuiPiao.Controls;

/// <summary>
///     Segoe MDL2 图标 + 可选文本。
///     FontSize/FontWeight/Foreground 必须是本类型上的 DependencyProperty，
///     否则 XAML 的 DynamicResource 无法绑定（会直接导致主窗口加载失败）。
/// </summary>
public class IconLabel : StackPanel
{
    private readonly TextBlock _glyphBlock;
    private readonly TextBlock _textBlock;

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(IconLabel),
            new PropertyMetadata(string.Empty, OnGlyphChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(IconLabel),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty GlyphSizeProperty =
        DependencyProperty.Register(nameof(GlyphSize), typeof(double), typeof(IconLabel),
            new PropertyMetadata(14.0, OnGlyphSizeChanged));

    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(IconLabel),
            new PropertyMetadata(6.0, OnSpacingChanged));

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(IconLabel),
            new FrameworkPropertyMetadata(
                SystemColors.ControlTextBrush,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnForegroundChanged));

    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(nameof(FontSize), typeof(double), typeof(IconLabel),
            new FrameworkPropertyMetadata(
                SystemFonts.MessageFontSize,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                OnFontSizeChanged));

    public static readonly DependencyProperty FontWeightProperty =
        DependencyProperty.Register(nameof(FontWeight), typeof(FontWeight), typeof(IconLabel),
            new FrameworkPropertyMetadata(
                FontWeights.Normal,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                OnFontWeightChanged));

    public IconLabel()
    {
        Orientation = Orientation.Horizontal;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = false;

        _glyphBlock = new TextBlock
        {
            FontFamily = new FontFamily(AppIcons.FontFamilyName),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsHitTestVisible = false
        };
        _textBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

        Children.Add(_glyphBlock);
        Children.Add(_textBlock);
        UpdateGlyphMargin();
        SyncForeground();
        SyncTextTypography();
    }

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double GlyphSize
    {
        get => (double)GetValue(GlyphSizeProperty);
        set => SetValue(GlyphSizeProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
        {
            label._glyphBlock.Text = e.NewValue as string ?? string.Empty;
            label.UpdateGlyphMargin();
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
        {
            var text = e.NewValue as string ?? string.Empty;
            label._textBlock.Text = text;
            label._textBlock.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
            label.UpdateGlyphMargin();
        }
    }

    private static void OnGlyphSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
            label._glyphBlock.FontSize = (double)e.NewValue;
    }

    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
            label.UpdateGlyphMargin();
    }

    private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
            label.SyncForeground();
    }

    private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
            label.SyncTextTypography();
    }

    private static void OnFontWeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconLabel label)
            label.SyncTextTypography();
    }

    private void SyncForeground()
    {
        // 未显式设置时清掉子级本地值，让按钮悬停/禁用色能继承进来
        if (DependencyPropertyHelper.GetValueSource(this, ForegroundProperty).BaseValueSource
            == BaseValueSource.Default)
        {
            _glyphBlock.ClearValue(TextBlock.ForegroundProperty);
            _textBlock.ClearValue(TextBlock.ForegroundProperty);
            return;
        }

        var brush = Foreground;
        _glyphBlock.Foreground = brush;
        _textBlock.Foreground = brush;
    }

    private void SyncTextTypography()
    {
        if (DependencyPropertyHelper.GetValueSource(this, FontSizeProperty).BaseValueSource
            == BaseValueSource.Default)
            _textBlock.ClearValue(TextBlock.FontSizeProperty);
        else
            _textBlock.FontSize = FontSize;

        if (DependencyPropertyHelper.GetValueSource(this, FontWeightProperty).BaseValueSource
            == BaseValueSource.Default)
            _textBlock.ClearValue(TextBlock.FontWeightProperty);
        else
            _textBlock.FontWeight = FontWeight;
    }

    private void UpdateGlyphMargin()
    {
        var hasText = !string.IsNullOrEmpty(Text);
        _glyphBlock.Margin = hasText ? new Thickness(0, 0, Spacing, 0) : new Thickness(0);
    }

    public static IconLabel Create(string glyph, string text, double glyphSize = 14) =>
        new() { Glyph = glyph, Text = text, GlyphSize = glyphSize };
}
