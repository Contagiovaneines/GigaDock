using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DockWindows.App.Converters;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch { }
        }

        return new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Azul padrão
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color.ToString();
        }
        return "#0078D4";
    }
}
