using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DockWindows.App.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    private static readonly BooleanToVisibilityConverter BaseConverter = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var vis = (Visibility)BaseConverter.Convert(value, targetType, "Invert", culture);
        return vis;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            return v != Visibility.Visible;
        }
        return false;
    }
}
