using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DockWindows.App.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool flag = false;
        if (value is bool b)
        {
            flag = b;
        }
        else if (value is string sVal)
        {
            flag = !string.IsNullOrWhiteSpace(sVal);
        }
        else
        {
            flag = value != null;
        }

        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}
