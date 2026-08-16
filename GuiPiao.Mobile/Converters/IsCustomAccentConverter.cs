using System.Globalization;
using GuiPiao.Mobile.Model;

namespace GuiPiao.Mobile.Converters;

public sealed class IsCustomAccentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is AccentColor.Custom;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
