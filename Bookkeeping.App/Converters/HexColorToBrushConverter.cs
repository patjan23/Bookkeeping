using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Bookkeeping.App.Converters;

/// <summary>Converts a "#RRGGBB" hex string to a SolidColorBrush.</summary>
[ValueConversion(typeof(string), typeof(SolidColorBrush))]
public class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is string hex)
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
        }
        catch { /* fall through */ }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
