using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GuiPiao.View.Controls;

/// <summary>
///     报销凭证式箭头：一条水平细线，右端点接<strong>一根</strong>向左上方倾斜的短斜线（上半勾，无下半斜线、非实心三角）。
/// </summary>
public partial class TicketFaceArrowControl : UserControl
{
    public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
        nameof(Length),
        typeof(double),
        typeof(TicketFaceArrowControl),
        new PropertyMetadata(52.0, OnGeometryPropertyChanged));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(TicketFaceArrowControl),
        new PropertyMetadata(1.15, OnGeometryPropertyChanged));

    /// <summary>勾线在水平方向「回缩」长度（px，越大勾越长）；≤0 时按 <see cref="Length" /> 自动估算。</summary>
    public static readonly DependencyProperty HeadLengthProperty = DependencyProperty.Register(
        nameof(HeadLength),
        typeof(double),
        typeof(TicketFaceArrowControl),
        new PropertyMetadata(0.0, OnGeometryPropertyChanged));

    /// <summary>勾线竖向抬升（px，越大勾越陡）；≤0 时按 <see cref="HeadLength" /> 自动估算。</summary>
    public static readonly DependencyProperty HeadWidthProperty = DependencyProperty.Register(
        nameof(HeadWidth),
        typeof(double),
        typeof(TicketFaceArrowControl),
        new PropertyMetadata(0.0, OnGeometryPropertyChanged));

    public static readonly DependencyProperty ArrowStrokeProperty = DependencyProperty.Register(
        nameof(ArrowStroke),
        typeof(Brush),
        typeof(TicketFaceArrowControl),
        new PropertyMetadata(null, OnStrokePropertyChanged));

    public double Length
    {
        get => (double)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double HeadLength
    {
        get => (double)GetValue(HeadLengthProperty);
        set => SetValue(HeadLengthProperty, value);
    }

    public double HeadWidth
    {
        get => (double)GetValue(HeadWidthProperty);
        set => SetValue(HeadWidthProperty, value);
    }

    public Brush? ArrowStroke
    {
        get => (Brush?)GetValue(ArrowStrokeProperty);
        set => SetValue(ArrowStrokeProperty, value);
    }

    public TicketFaceArrowControl()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshGeometry();
        ApplyStroke();
    }

    private static void OnGeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TicketFaceArrowControl c) c.RefreshGeometry();
    }

    private static void OnStrokePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TicketFaceArrowControl c) c.ApplyStroke();
    }

    private void ApplyStroke()
    {
        var brush = ArrowStroke ?? Brushes.Black;
        ArrowPoly.Stroke = brush;
    }

    private void RefreshGeometry()
    {
        if (ArrowPoly == null) return;

        var L = Length;
        if (L < 4) L = 4;

        var t = StrokeThickness;
        if (t < 0.25) t = 0.25;
        if (t > 6) t = 6;

        // 勾：自右端 (L,cy) 至左上 (L-hx, cy-hy)，仅上半段
        var hx = HeadLength >= 0.5 ? HeadLength : System.Math.Clamp(L * 0.15, 3.2, 8.5);
        hx = System.Math.Min(hx, System.Math.Max(1.5, L - 1.0));
        var hy = HeadWidth >= 0.5 ? HeadWidth : System.Math.Clamp(hx * 0.58, 2.5, 6.0);

        const double cy = 9.0;
        var canvasH = System.Math.Max(14.0, hy + t + 6.0);
        ArrowCanvas.Height = canvasH;
        var cyUse = System.Math.Max(hy + t * 0.5 + 1.0, System.Math.Min(canvasH - t * 0.5 - 1.0, cy));

        ArrowCanvas.Width = L;
        ArrowPoly.StrokeThickness = t;
        ArrowPoly.Points = new PointCollection
        {
            new(0, cyUse),
            new(L, cyUse),
            new(L - hx, cyUse - hy)
        };
    }
}
