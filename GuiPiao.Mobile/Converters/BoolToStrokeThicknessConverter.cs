using System.Globalization;

namespace GuiPiao.Mobile.Converters;

/// <summary>选中描边：true→2，false→1（未选中仍有淡边框）。</summary>
public sealed class BoolToStrokeThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 2d : 1d;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
