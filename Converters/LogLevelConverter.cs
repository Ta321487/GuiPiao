using GuiPiao.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace GuiPiao.Converters
{
    public class LogLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level && parameter is string param)
            {
                if (Enum.TryParse<LogLevel>(param, out LogLevel paramLevel))
                {
                    return level == paramLevel;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter is string param)
            {
                if (Enum.TryParse<LogLevel>(param, out LogLevel level))
                {
                    return level;
                }
            }
            return LogLevel.INFO;
        }
    }
}
