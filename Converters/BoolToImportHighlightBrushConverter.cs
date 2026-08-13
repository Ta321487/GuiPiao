using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GuiPiao.Converters;

/// <summary>
///     OCR/导入需核对字段底色；取主题 <c>ImportFieldHighlightBrush</c>（深浅色分别适配）。
/// </summary>
public class BoolToImportHighlightBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush TransparentBrush = CreateFrozen(Colors.Transparent);

    private static SolidColorBrush CreateFrozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not true)
            return TransparentBrush;

        if (Application.Current?.TryFindResource("ImportFieldHighlightBrush") is Brush themed)
            return themed;

        // 主题未加载时的兜底（偏深色，避免白字糊掉）
        return CreateFrozen(Color.FromRgb(0x5C, 0x4A, 0x1F));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
