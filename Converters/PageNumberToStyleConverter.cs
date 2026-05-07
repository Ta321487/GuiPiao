using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters
{
    /// <summary>
    /// 页码转换为是否当前页的转换器
    /// </summary>
    public class PageNumberToStyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int buttonPage && values[1] is int currentPage)
            {
                return buttonPage == currentPage;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
