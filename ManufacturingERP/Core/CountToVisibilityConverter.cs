using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ManufacturingERP.Core;

public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = string.Equals(parameter?.ToString(), "invert", StringComparison.OrdinalIgnoreCase);
        var count = GetCount(value);
        var isVisible = count > 0;
        if (invert)
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static int GetCount(object value)
    {
        if (value is null)
        {
            return 0;
        }

        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (value is double d) return (int)d;

        if (value is ICollection collection)
        {
            return collection.Count;
        }

        if (value is IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
                if (count > 0)
                {
                    break;
                }
            }

            return count;
        }

        return 0;
    }
}

