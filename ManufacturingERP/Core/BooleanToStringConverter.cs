using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ManufacturingERP.Core
{
    public class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    return b ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
