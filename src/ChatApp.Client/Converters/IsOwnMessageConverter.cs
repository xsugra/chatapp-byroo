using System.Globalization;
using System.Windows.Data;

namespace ChatApp.Client.Converters;

public class IsOwnMessageConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is string senderName && values[1] is string currentUser)
            return string.Equals(senderName, currentUser, StringComparison.OrdinalIgnoreCase);
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
