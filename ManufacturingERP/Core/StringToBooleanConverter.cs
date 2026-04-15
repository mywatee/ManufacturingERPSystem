using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ManufacturingERP.Core
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (bool)value ? parameter : DependencyProperty.UnsetValue;
        }
    }
}
