using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DockWindows.App.Controls;

public class ReferenciaVisualControl : FrameworkElement
{
    public string Tipo { get; set; } = "Janelas";
    protected override void OnRender(DrawingContext dc)
    {
        var scale = Math.Min(ActualWidth / 340, ActualHeight / 180);
        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(43, 88, 174), Color.FromRgb(15, 26, 76), 90),
            new Pen(Brushes.SlateBlue, 1), new Rect(0, 0, 340, 180), 15, 15);
        void Texto(string texto, double x, double y, double size = 12, Brush? cor = null)
            => dc.DrawText(new FormattedText(texto, CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), size, cor ?? Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
        if (Tipo == "Janelas")
        {
            Texto("Navegador · 3 janelas abertas", 14, 10, 13);
            for (int i = 0; i < 3; i++)
            {
                double x = 12 + i * 108;
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(29, 41, 75)), new Pen(Brushes.SlateBlue, 1), new Rect(x, 39, 100, 128), 9, 9);
                Texto(new[] { "Pesquisa", "Projeto", "Leitura" }[i], x + 9, 45, 10);
                dc.DrawRoundedRectangle(new LinearGradientBrush(new[] { Colors.Coral, Colors.Turquoise, Colors.Khaki }[i],
                    Colors.MidnightBlue, 45), null, new Rect(x + 5, 65, 90, 80), 5, 5);
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), null, new Rect(x + 13, 76, 45, 4));
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), null, new Rect(x + 13, 86, 66, 3));
                Texto("Abrir janela", x + 13, 150, 9);
            }
        }
        else
        {
            Texto("Downloads", 13, 10, 13);
            Texto("Imagem.png", 176, 10, 12);
            var nomes = new[] { "Capa.jpg", "Paleta.png", "Imagem.png", "Anotações.txt", "Documento.pdf" };
            for (int i = 0; i < nomes.Length; i++)
            {
                if (i == 2) dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(65, 210, 225, 255)), null, new Rect(8, 35 + i * 25, 146, 25), 6, 6);
                dc.DrawRoundedRectangle(new SolidColorBrush(i < 3 ? Colors.MediumTurquoise : Colors.LightGray), null, new Rect(16, 41 + i * 25, 13, 15), 2, 2);
                Texto(nomes[i], 38, 40 + i * 25, 11);
            }
            dc.DrawRoundedRectangle(new LinearGradientBrush(Colors.HotPink, Colors.DarkSlateBlue, 45), null, new Rect(170, 40, 157, 115), 8, 8);
            dc.DrawEllipse(new RadialGradientBrush(Colors.Turquoise, Colors.Transparent), null, new Point(267, 101), 60, 49);
            Texto("Moodboard", 182, 54, 18);
            Texto("Abrir item", 216, 161, 10);
        }
        dc.Pop();
    }
}