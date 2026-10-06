using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DockWindows.App.Controls;

public class EstiloRelogioControl : FrameworkElement
{
    public static readonly DependencyProperty EstiloProperty = DependencyProperty.Register(nameof(Estilo), typeof(string), typeof(EstiloRelogioControl), new FrameworkPropertyMetadata("hora-data", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty HorarioProperty = DependencyProperty.Register(nameof(Horario), typeof(DateTime), typeof(EstiloRelogioControl), new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.AffectsRender));
    public string Estilo { get => (string)GetValue(EstiloProperty); set => SetValue(EstiloProperty, value); }
    public DateTime Horario { get => (DateTime)GetValue(HorarioProperty); set => SetValue(HorarioProperty, value); }
    public static readonly DependencyProperty TempoControleProperty = DependencyProperty.Register(nameof(TempoControle), typeof(string), typeof(EstiloRelogioControl), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
    public string TempoControle { get => (string)GetValue(TempoControleProperty); set => SetValue(TempoControleProperty, value); }
    private static readonly CultureInfo Pt = new("pt-BR");
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var w = ActualWidth; var h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        void Texto(string value, double size, Brush color, double y, double center)
        {
            var text = new FormattedText(value, Pt, FlowDirection.LeftToRight, new Typeface("Segoe UI Semibold"), size, color, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(text, new Point(center - text.Width / 2, y));
        }
        var analogico = Estilo.StartsWith("analogico-", StringComparison.Ordinal);
        if (Estilo is "cronometro" or "temporizador")
        {
            Texto(string.IsNullOrEmpty(TempoControle) ? (Estilo == "temporizador" ? "5:00 ▶" : "0:00 ▶") : TempoControle, 26, Brushes.WhiteSmoke, 6, w / 2);
            return;
        }
        if (analogico)
        {
            bool claro = Estilo == "analogico-claro", minimal = Estilo == "analogico-minimal", digital = Estilo == "analogico-digital";
            var c = new Point(digital ? h / 2 : w / 2, h / 2); var r = h / 2 - 3;
            var cor = claro ? Brushes.Black : Brushes.WhiteSmoke;
            dc.DrawEllipse(claro ? Brushes.WhiteSmoke : new SolidColorBrush(Color.FromRgb(23, 27, 30)), new Pen(Brushes.DimGray, 1), c, r, r);
            Point P(double a, double len) => new(c.X + Math.Sin(a * Math.PI / 180) * r * len, c.Y - Math.Cos(a * Math.PI / 180) * r * len);
            if (!minimal) for (int i = 0; i < 12; i++) dc.DrawLine(new Pen(Brushes.Gray, 1), P(i * 30, .8), P(i * 30, .92));
            dc.DrawLine(new Pen(cor, 2.5), c, P((Horario.Hour % 12 + Horario.Minute / 60.0) * 30, .5));
            dc.DrawLine(new Pen(minimal ? Brushes.DeepSkyBlue : cor, 2), c, P((Horario.Minute + Horario.Second / 60.0) * 6, .74));
            dc.DrawLine(new Pen(Brushes.IndianRed, 1), c, P(Horario.Second * 6, .82));
            if (digital) { Texto(Horario.ToString("HH:mm", Pt), 25, Brushes.WhiteSmoke, 6, (w + h) / 2); Texto(Horario.ToString("dd MMM", Pt), 11, Brushes.Silver, 35, (w + h) / 2); }
            return;
        }
        if (Estilo == "flip")
        {
            var digits = Horario.ToString("HHmm", Pt); var cw = (w - 12) / 4;
            for (int i = 0; i < 4; i++) { var x = i * (cw + 4); dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(58, 61, 48)), null, new Rect(x, 1, cw, h - 2), 5, 5); dc.DrawLine(new Pen(Brushes.DimGray, .7), new Point(x, h / 2), new Point(x + cw, h / 2)); Texto(digits[i].ToString(), 34, Brushes.WhiteSmoke, 1, x + cw / 2); }
            return;
        }
        if (Estilo == "mundial")
        {
            var zones = new[] { ("São Paulo", "E. South America Standard Time"), ("Londres", "GMT Standard Time"), ("Tóquio", "Tokyo Standard Time") };
            for (int i = 0; i < zones.Length; i++)
            {
                string hora;
                try { hora = TimeZoneInfo.ConvertTime(Horario, TimeZoneInfo.FindSystemTimeZoneById(zones[i].Item2)).ToString("HH:mm", Pt); }
                catch (TimeZoneNotFoundException) { hora = "—"; }
                Texto($"{zones[i].Item1}  {hora}", 12, Brushes.WhiteSmoke, i * 17, w / 2);
            }
            return;
        }
        var principal = Estilo switch { "segundos" => Horario.ToString("HH:mm:ss", Pt), "data" => Horario.ToString("dd 'de' MMMM", Pt), "dia" => Horario.ToString("dd", Pt), "hoje" => Horario.ToString("dd MMM, ddd", Pt), _ => Horario.ToString("HH:mm", Pt) };
        var sub = Estilo switch { "hora-data" => Horario.ToString("ddd, dd MMM", Pt).Replace(".", ""), "dia" => Horario.ToString("MMM", Pt), "data" => Horario.ToString("dddd", Pt), "hoje" => Horario.ToString("HH:mm", Pt), _ => "" };
        Texto(principal, Estilo is "data" or "hoje" ? 19 : 30, Brushes.WhiteSmoke, sub.Length == 0 ? 6 : 0, w / 2);
        if (sub.Length != 0) Texto(sub, 12, Brushes.Silver, 35, w / 2);
    }
}
