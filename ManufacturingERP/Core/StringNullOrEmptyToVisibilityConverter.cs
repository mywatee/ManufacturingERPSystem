using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ManufacturingERP.Core;

public sealed class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = string.Equals(parameter?.ToString(), "invert", StringComparison.OrdinalIgnoreCase);
        var isEmpty = string.IsNullOrWhiteSpace(value?.ToString());
        var isVisible = isEmpty;
        if (invert)
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

