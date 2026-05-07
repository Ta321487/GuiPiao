using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuiPiao.Converters
{
    /// <summary>
    /// 布尔值转Thickness转换器
    /// </summary>
    public class BooleanToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;

            if (boolValue)
            {
                // 编辑状态显示边框
                return new Thickness(2);
            }
            else
            {
                // 非编辑状态无边框
                return new Thickness(0);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
