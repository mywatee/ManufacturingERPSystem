using System;
using System.Globalization;
using System.Windows.Data;

namespace ManufacturingERP.Core
{
    public class ViewModelTypeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            
            // Compare the type name of the active viewmodel with the provided parameter string
            return value.GetType().Name == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
