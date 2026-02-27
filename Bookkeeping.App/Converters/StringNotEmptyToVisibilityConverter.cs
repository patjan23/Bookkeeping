using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Bookkeeping.App.Converters;

/// <summary>Visibility.Visible when string is non-null and non-empty; Collapsed otherwise.</summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
