using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ManufacturingERP.Core;

public class BooleanToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string paramString)
        {
            var colors = paramString.Split('|');
            if (colors.Length == 2)
            {
                var colorString = boolValue ? colors[0] : colors[1];
                return (Brush)new BrushConverter().ConvertFrom(colorString)!;
            }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
