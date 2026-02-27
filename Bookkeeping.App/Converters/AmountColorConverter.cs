using Bookkeeping.Core.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Bookkeeping.App.Converters;

/// <summary>Converts TransactionType → green (income) or red (expense) brush.</summary>
[ValueConversion(typeof(TransactionType), typeof(Brush))]
public class AmountColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Green = new(Color.FromRgb(56, 161, 105));
    private static readonly SolidColorBrush Red   = new(Color.FromRgb(229, 62, 62));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TransactionType t && t == TransactionType.Income ? Green : Red;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
