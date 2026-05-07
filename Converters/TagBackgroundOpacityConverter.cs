using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GuiPiao.Converters
{
    /// <summary>
    /// 标签背景透明度转换器 - 将颜色转换为半透明画刷
    /// </summary>
    public class TagBackgroundOpacityConverter : IValueConverter
    {
        /// <summary>
        /// 背景透明度 (0-1)
        /// </summary>
        public double Opacity { get; set; } = 0.85;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor)
            {
                try
                {
                    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
                    brush.Opacity = Opacity;
                    return brush;
                }
                catch
                {
                    var defaultBrush = new SolidColorBrush(Colors.Gray);
                    defaultBrush.Opacity = Opacity;
                    return defaultBrush;
                }
            }
            var fallbackBrush = new SolidColorBrush(Colors.Gray);
            fallbackBrush.Opacity = Opacity;
            return fallbackBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return "#808080";
        }
    }
}
