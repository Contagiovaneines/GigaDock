using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DockWindows.App.Converters;

public sealed class MediaColorsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = Color.FromRgb(65, 78, 96);
        try { if (value is string hex && hex != "#000000") color = (Color)ColorConverter.ConvertFromString(hex); }
        catch { }
        static Color Shade(Color c, double amount) => Color.FromRgb(
            (byte)(12 + c.R * amount), (byte)(14 + c.G * amount), (byte)(18 + c.B * amount));
        Brush brush;
        if (parameter as string == "Borda")
            brush = new SolidColorBrush(Color.FromArgb(190, (byte)(70 + color.R * .65), (byte)(70 + color.G * .65), (byte)(70 + color.B * .65)));
        else if (parameter as string == "Destaque")
            brush = new SolidColorBrush(Color.FromRgb((byte)(110 + color.R * .5), (byte)(110 + color.G * .5), (byte)(110 + color.B * .5)));
        else
            brush = new LinearGradientBrush(new GradientStopCollection {
                new(Shade(color, .44), 0), new(Shade(color, .25), .55), new(Shade(color, .12), 1)
            }, new System.Windows.Point(0, 0), new System.Windows.Point(1, 1));
        brush.Freeze();
        return brush;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
