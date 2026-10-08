using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DockWindows.App.Controls;

public class ReferenciaMidiaControl : FrameworkElement
{
    public string Estilo { get; set; } = "tocando";
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 34, 29)), new Pen(Brushes.DarkSlateGray, 1), new Rect(0, 0, w, h), 8, 8);
        void Text(string text, double size, double x, double y, Brush brush)
        {
            dc.DrawText(new FormattedText(text, CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
        }
        if (Estilo is "compacto-player" or "ultralargo" or "expansivel")
        {
            dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(38, 132, 174), Color.FromRgb(97, 52, 139), 35), null, new Rect(7, 7, 42, 42), 8, 8);
            Text("♫", 21, 18, 14, Brushes.White);
            if (Estilo == "compacto-player")
            {
                Text("◀   Ⅱ   ▶", 13, 62, 20, Brushes.White);
                return;
            }
            Text("Nome da música", 11, 58, 7, Brushes.White);
            Text("Artista", 9, 58, 23, Brushes.Silver);
            if (Estilo == "ultralargo") Text("◀  Ⅱ  ▶", 12, w - 67, 20, Brushes.White);
            else
            {
                Text("Ⅱ", 17, w - 32, 16, Brushes.White);
                Text("Passe o mouse para expandir", 8, 58, 38, Brushes.LightSkyBlue);
            }
            return;
        }
        if (Estilo == "mini") { dc.DrawEllipse(null, new Pen(Brushes.Gray, 2), new Point(w / 2, h / 2), 19, 19); Text("Ⅱ", 24, w / 2 - 9, h / 2 - 17, Brushes.White); return; }
        double x = 9;
        if (Estilo == "capa") { dc.DrawRoundedRectangle(new LinearGradientBrush(Colors.SteelBlue, Colors.SeaGreen, 45), null, new Rect(6, 7, 40, 42), 6, 6); Text("♫", 22, 15, 14, Brushes.White); x = 54; }
        Text("Nome da música", 11, x, 7, Brushes.White);
        Text("Artista", 9, x, 23, Brushes.Silver);
        Text("◀   Ⅱ   ▶", 13, Estilo == "capa" ? 54 : w - 64, Estilo == "capa" ? 35 : 21, Brushes.White);
        if (Estilo == "barra") { dc.DrawLine(new Pen(Brushes.DimGray, 2), new Point(9, 42), new Point(w - 9, 42)); dc.DrawLine(new Pen(Brushes.DeepSkyBlue, 2), new Point(9, 42), new Point(w * .55, 42)); Text("1:24", 8, 9, 45, Brushes.Silver); Text("3:42", 8, w - 26, 45, Brushes.Silver); }
    }
}
