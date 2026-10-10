using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ShapePath = Avalonia.Controls.Shapes.Path;
namespace GigaDock.App.Linux;
internal static class Visuals
{
    public static IBrush AreiaBackground()
    {
        var drawings = new DrawingGroup();
        foreach (var (color, geometry) in new[] {
            ("#F2E9DC", "M0,0 H1000 V100 H0 Z"), ("#F7F0E6", "M0,0 H225 L130,70 Z"),
            ("#EAE0D1", "M75,100 L245,20 L350,100 Z"), ("#F8F2E9", "M310,0 H630 L490,90 Z"),
            ("#EBE1D3", "M610,100 L740,18 L850,100 Z"), ("#F7EFE3", "M800,0 H1000 V65 L940,100 Z") })
            drawings.Children.Add(new GeometryDrawing { Brush = Brush(color), Geometry = Geometry.Parse(geometry) });
        return new DrawingBrush { Drawing = drawings, Stretch = Stretch.Fill };
    }
    private static readonly Dictionary<string, Avalonia.Media.Imaging.Bitmap> IconCache = new();
    public static Control ItemIcon(DockWindows.Core.Models.ItemFixado item, string fallbackColor = "#D5DAE1")
    {
        var file = new GigaDock.Infrastructure.Linux.ThemeIconResolver().Resolve(item.IconeCustomizado ?? "");
        if (file is not null)
        {
            try
            {
                if (!IconCache.TryGetValue(file, out var bitmap) && IconCache.Count < 256)
                {
                    using var stream = File.OpenRead(file);
                    IconCache[file] = bitmap = Avalonia.Media.Imaging.Bitmap.DecodeToWidth(stream, 64);
                }
                if (bitmap is not null) return new Image { Source = bitmap, Width = 32, Height = 32 };
            }
            catch (Exception error) when (error is IOException or ArgumentException or NotSupportedException) { }
        }
        return Icon(item.Tipo == DockWindows.Core.Models.TipoItem.Pasta ? "Pasta" : "Aplicativo", fallbackColor, size: 28);
    }
    public static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
    public static TextBlock Text(string text, double size = 13, string color = "#FFFFFF") => new() { Text = text, FontSize = size, Foreground = Brush(color), TextWrapping = TextWrapping.Wrap };
    // Geometrias próprias, independentes das fontes de ícones do Windows.
    public static Control Icon(string kind, string color = "#D5DAE1", double size = 20)
    {
        var data = kind switch
        {
            "Pasta" => "M2,6 L10,6 L12,9 L22,9 L22,21 L2,21 Z",
            "Aplicativo" => "M2,2 L22,2 L22,22 L2,22 Z M2,7 L22,7",
            "Ambientes" => "M2,6 L10,6 L10,20 L2,20 Z M14,2 L22,2 L22,12 L14,12 Z M14,16 L22,16 L22,22 L14,22 Z",
            "Widgets" => "M2,2 L10,2 L10,10 L2,10 Z M14,2 L22,2 L22,10 L14,10 Z M2,14 L10,14 L10,22 L2,22 Z M14,14 L22,14 L22,22 L14,22 Z",
            "Pokédex" => "M12,1 L15,9 L23,12 L15,15 L12,23 L9,15 L1,12 L9,9 Z",
            "Divisores" => "M8,2 L8,22 M16,2 L16,22",
            "Aparência" => "M3,18 L16,5 L21,10 L8,23 L3,23 Z M14,7 L19,12",
            "Visualizações" => "M2,3 L22,3 L22,21 L2,21 Z M10,3 L10,21",
            "Geral" => "M12,2 L15,6 L20,5 L19,10 L23,12 L19,15 L20,20 L15,19 L12,23 L9,19 L4,20 L5,15 L1,12 L5,9 L4,4 L9,5 Z M9,12 A3,3 0 1 0 15,12 A3,3 0 1 0 9,12",
            "Adicionar" => "M12,4 L12,20 M4,12 L20,12",
            _ => "M12,2 A10,10 0 1 0 12,22 A10,10 0 1 0 12,2 M12,10 L12,17 M12,6 L12,7"
        };
        return new Viewbox { Width = size, Height = size, Child = new ShapePath { Data = Geometry.Parse(data), Stroke = Brush(color), StrokeThickness = 1.6, Width = 24, Height = 24 } };
    }
    public static Button Button(string label, Action action, Control? content = null)
    {
        var button = new Button { Content = content ?? Text(label), Padding = new Thickness(12, 8), Background = Brushes.Transparent, BorderThickness = new Thickness(1), BorderBrush = Brushes.Transparent, CornerRadius = new CornerRadius(8) };
        AutomationProperties.SetName(button, label); ToolTip.SetTip(button, label);
        button.Click += (_, _) => action(); return button;
    }
    public static Border Card(Control child) => new() { Background = Brush("#202026"), BorderBrush = Brush("#32323C"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(14), Child = child };
    public static void InstallStyles(Application app)
    {
        var hover = new Style(s => s.OfType<Button>().Class("dock").Class(":pointerover"));
        hover.Setters.Add(new Setter(Avalonia.Controls.Button.BackgroundProperty, Brush("#2EFFFFFF")));
        hover.Setters.Add(new Setter(Avalonia.Controls.Button.BorderBrushProperty, Brush("#55FFFFFF"))); app.Styles.Add(hover);
        var focus = new Style(s => s.OfType<Button>().Class(":focus-visible"));
        focus.Setters.Add(new Setter(Avalonia.Controls.Button.BorderBrushProperty, Brush("#58A6FF"))); app.Styles.Add(focus);
    }
}

