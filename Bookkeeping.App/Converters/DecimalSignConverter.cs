using Bookkeeping.Core.Models;
using System.Globalization;
using System.Windows.Data;

namespace Bookkeeping.App.Converters;

/// <summary>Prefixes amount with + or − based on TransactionType via MultiBinding.</summary>
public class DecimalSignConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return string.Empty;
        var amount = values[0] is decimal d ? d : 0m;
        var type   = values[1] is TransactionType t ? t : TransactionType.Expense;
        return type == TransactionType.Income
            ? $"+{amount:C}"
            : $"-{amount:C}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
