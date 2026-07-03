using System.Globalization;
using System.Windows.Data;

namespace ChatApp.Client.Converters;

public class GuidEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2) return false;

        Guid? a = values[0] switch { Guid g => g, _ => null };
        Guid? b = values[1] switch { Guid g => g, _ => null };

        return a.HasValue && b.HasValue && a.Value == b.Value;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
