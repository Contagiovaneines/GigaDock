using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DockWindows.App.Controls;

public class IndicadorSistemaControl : FrameworkElement
{
    private static FrameworkPropertyMetadata Visual(object valor) => new(valor, FrameworkPropertyMetadataOptions.AffectsRender);
    public static readonly DependencyProperty EstiloProperty = DependencyProperty.Register(nameof(Estilo), typeof(string), typeof(IndicadorSistemaControl), Visual("cpu"));
    public static readonly DependencyProperty TituloProperty = DependencyProperty.Register(nameof(Titulo), typeof(string), typeof(IndicadorSistemaControl), Visual("CPU"));
    public static readonly DependencyProperty TextoProperty = DependencyProperty.Register(nameof(Texto), typeof(string), typeof(IndicadorSistemaControl), Visual("—"));
    public static readonly DependencyProperty SecundarioProperty = DependencyProperty.Register(nameof(Secundario), typeof(string), typeof(IndicadorSistemaControl), Visual(""));
    public static readonly DependencyProperty ValorProperty = DependencyProperty.Register(nameof(Valor), typeof(double), typeof(IndicadorSistemaControl), Visual(double.NaN));
    public static readonly DependencyProperty ValorSecundarioProperty = DependencyProperty.Register(nameof(ValorSecundario), typeof(double), typeof(IndicadorSistemaControl), Visual(double.NaN));
    public static readonly DependencyProperty SerieProperty = DependencyProperty.Register(nameof(Serie), typeof(IEnumerable<double>), typeof(IndicadorSistemaControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, SerieAlterada));
    public static readonly DependencyProperty SerieSecundariaProperty = DependencyProperty.Register(nameof(SerieSecundaria), typeof(IEnumerable<double>), typeof(IndicadorSistemaControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, SerieAlterada));
    public string Estilo { get => (string)GetValue(EstiloProperty); set => SetValue(EstiloProperty, value); }
    public string Titulo { get => (string)GetValue(TituloProperty); set => SetValue(TituloProperty, value); }
    public string Texto { get => (string)GetValue(TextoProperty); set => SetValue(TextoProperty, value); }
    public string Secundario { get => (string)GetValue(SecundarioProperty); set => SetValue(SecundarioProperty, value); }
    public double Valor { get => (double)GetValue(ValorProperty); set => SetValue(ValorProperty, value); }
    public double ValorSecundario { get => (double)GetValue(ValorSecundarioProperty); set => SetValue(ValorSecundarioProperty, value); }
    public IEnumerable<double>? Serie { get => (IEnumerable<double>?)GetValue(SerieProperty); set => SetValue(SerieProperty, value); }
    public IEnumerable<double>? SerieSecundaria { get => (IEnumerable<double>?)GetValue(SerieSecundariaProperty); set => SetValue(SerieSecundariaProperty, value); }
    private static void SerieAlterada(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        var control = (IndicadorSistemaControl)o;
        if (e.OldValue is INotifyCollectionChanged old) old.CollectionChanged -= control.ActualizarSerie;
        if (e.NewValue is INotifyCollectionChanged next) next.CollectionChanged += control.ActualizarSerie;
    }
    private void ActualizarSerie(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        var azul = Brushes.DeepSkyBlue;
        void Text(string value, double size, double x, double y, Brush? cor = null, double? largura = null)
        {
            var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI Semibold"), size, cor ?? Brushes.WhiteSmoke, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            text.MaxTextWidth = Math.Max(1, largura ?? w - x - 4); text.MaxTextHeight = size * 1.5; text.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(text, new Point(x, y));
        }
        if (Estilo == "medidores-cpu-ram")
        {
            void Gauge(double value, Point center, string label)
            {
                var radius = Math.Min(27, h / 2 - 2);
                var bezel = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(Color.FromRgb(238, 241, 244), 0),
                        new GradientStop(Color.FromRgb(91, 98, 105), .78),
                        new GradientStop(Color.FromRgb(225, 230, 234), 1)
                    }
                };
                dc.DrawEllipse(bezel, new Pen(new SolidColorBrush(Color.FromRgb(30, 34, 38)), 1), center, radius, radius);
                dc.DrawEllipse(new RadialGradientBrush(Color.FromRgb(42, 49, 55), Color.FromRgb(9, 12, 16)), null, center, radius - 4, radius - 4);

                const double startAngle = -130;
                const double sweep = 260;
                for (var i = 0; i < 20; i++)
                {
                    var a1 = (startAngle + i * sweep / 20) * Math.PI / 180;
                    var a2 = (startAngle + (i + .72) * sweep / 20) * Math.PI / 180;
                    var ringRadius = radius - 8;
                    var p1 = new Point(center.X + Math.Sin(a1) * ringRadius, center.Y - Math.Cos(a1) * ringRadius);
                    var p2 = new Point(center.X + Math.Sin(a2) * ringRadius, center.Y - Math.Cos(a2) * ringRadius);
                    var arc = new StreamGeometry();
                    using (var ctx = arc.Open())
                    {
                        ctx.BeginFigure(p1, false, false);
                        ctx.ArcTo(p2, new Size(ringRadius, ringRadius), 0, false, SweepDirection.Clockwise, true, false);
                    }
                    var color = i < 12 ? Color.FromRgb(35, 194, 255) : i < 16 ? Color.FromRgb(255, 184, 47) : Color.FromRgb(255, 77, 72);
                    dc.DrawGeometry(null, new Pen(new SolidColorBrush(color), 2), arc);
                }

                var safeValue = double.IsFinite(value) ? Math.Clamp(value, 0, 100) : 0;
                var needleAngle = (startAngle + safeValue * sweep / 100) * Math.PI / 180;
                var needleEnd = new Point(center.X + Math.Sin(needleAngle) * (radius - 11), center.Y - Math.Cos(needleAngle) * (radius - 11));
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 68, 55)), 2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, center, needleEnd);
                dc.DrawEllipse(Brushes.Silver, new Pen(Brushes.DarkSlateGray, 1), center, 3, 3);

                Text(label, 7, center.X - radius + 7, center.Y - 11, Brushes.Silver, radius * 2 - 14);
                Text(double.IsFinite(value) ? $"{value:F0}%" : "—", 11, center.X - radius + 7, center.Y + 5, Brushes.White, radius * 2 - 14);
            }

            var spacing = Math.Min(64, w / 2);
            var start = (w - spacing) / 2;
            Gauge(Valor, new Point(start, h / 2), "CPU");
            Gauge(ValorSecundario, new Point(start + spacing, h / 2), "RAM");
            return;
        }

        if (Estilo == "ventoinha-cpu")
        {
            dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(54, 59, 65), Color.FromRgb(24, 29, 37), 90), new Pen(new SolidColorBrush(Color.FromRgb(70, 78, 91)), 1), new Rect(0, 0, w, h), 14, 14);
            var center = new Point(w / 2, h / 2 - 4);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(12, 18, 29)), new Pen(new SolidColorBrush(Color.FromRgb(121, 134, 151)), 1.5), center, 20, 20);
            for (var i = 0; i < 7; i++)
            {
                dc.PushTransform(new RotateTransform(i * 360d / 7 + (double.IsFinite(Valor) ? Valor : 0), center.X, center.Y));
                dc.DrawEllipse(new LinearGradientBrush(Color.FromRgb(224, 232, 241), Color.FromRgb(100, 117, 141), 45), null, new Point(center.X, center.Y - 9), 5, 10);
                dc.Pop();
            }
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(215, 225, 237)), new Pen(new SolidColorBrush(Color.FromRgb(55, 67, 83)), 1), center, 4, 4);
            Text(double.IsFinite(Valor) ? $"{Valor:F0}%" : "—", 9, 8, h - 16, Brushes.WhiteSmoke, w - 16);
            return;
        }

        if (Estilo == "rede-compacta")
        {
            dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(13, 128, 119), Color.FromRgb(8, 48, 46), 90), new Pen(new SolidColorBrush(Color.FromRgb(21, 112, 106)), 1), new Rect(0, 0, w, h), 14, 14);
            Text(Texto, 10, 10, 15, new SolidColorBrush(Color.FromRgb(126, 255, 238)), w - 18);
            Text(Secundario, 10, 10, 32, new SolidColorBrush(Color.FromRgb(255, 223, 118)), w - 18);
            return;
        }

        if (Estilo is "atividade-compacta" or "atividade-larga")
        {
            dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(67, 68, 72), Color.FromRgb(37, 39, 43), 90), new Pen(new SolidColorBrush(Color.FromRgb(99, 102, 108)), 1), new Rect(0, 0, w, h), 14, 14);
            void ActivityRing(double value, Point center, Brush color, string label, double radius)
            {
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(54, 58, 64)), 6), center, radius, radius);
                if (double.IsFinite(value) && value > 0)
                {
                    var angle = Math.Clamp(value, 0, 99.99) * 2 * Math.PI / 100;
                    var arc = new StreamGeometry();
                    using (var ctx = arc.Open())
                    {
                        ctx.BeginFigure(new Point(center.X, center.Y - radius), false, false);
                        ctx.ArcTo(new Point(center.X + Math.Sin(angle) * radius, center.Y - Math.Cos(angle) * radius), new Size(radius, radius), 0, angle > Math.PI, SweepDirection.Clockwise, true, false);
                    }
                    dc.DrawGeometry(null, new Pen(color, 6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, arc);
                }
                Text(double.IsFinite(value) ? $"{value:F0}" : "—", Estilo == "atividade-larga" ? 14 : 10, center.X - radius + 4, center.Y - 12, Brushes.White, radius * 2 - 8);
                Text(label, Estilo == "atividade-larga" ? 8 : 7, center.X - radius + 4, center.Y + 5, color, radius * 2 - 8);
            }
            var radius = Estilo == "atividade-larga" ? 22d : 17d;
            var distance = Estilo == "atividade-larga" ? 70d : 40d;
            var first = (w - distance) / 2;
            ActivityRing(Valor, new Point(first, h / 2), Brushes.DodgerBlue, "CPU", radius);
            ActivityRing(ValorSecundario, new Point(first + distance, h / 2), Brushes.MediumPurple, "RAM", radius);
            return;
        }

        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 34, 29)), new Pen(new SolidColorBrush(Color.FromRgb(60, 67, 54)), 1), new Rect(0, 0, w, h), 10, 10);
        if (Estilo == "aneis-cpu-ram")
        {
            void Ring(double value, Point center, Brush color, string label)
            {
                const double radius = 18;
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(48, 59, 62)), 4), center, radius, radius);
                if (double.IsFinite(value) && value > 0)
                {
                    var angle = Math.Clamp(value, 0, 99.99) * 2 * Math.PI / 100;
                    var arc = new StreamGeometry();
                    using (var ctx = arc.Open())
                    {
                        ctx.BeginFigure(new Point(center.X, center.Y - radius), false, false);
                        ctx.ArcTo(new Point(center.X + Math.Sin(angle) * radius, center.Y - Math.Cos(angle) * radius), new Size(radius, radius), 0, angle > Math.PI, SweepDirection.Clockwise, true, false);
                    }
                    dc.DrawGeometry(null, new Pen(color, 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, arc);
                }
                var valueText = double.IsFinite(value) ? $"{value:F0}%" : "—";
                Text(valueText, 10, center.X - radius + 2, center.Y - 9, Brushes.WhiteSmoke, radius * 2 - 4);
                Text(label, 8, center.X - radius + 2, center.Y + 4, color, radius * 2 - 4);
            }

            var spacing = Math.Min(64, w / 2);
            var start = (w - spacing) / 2;
            Ring(Valor, new Point(start, h / 2), Brushes.DeepSkyBlue, "CPU");
            Ring(ValorSecundario, new Point(start + spacing, h / 2), Brushes.MediumPurple, "RAM");
            return;
        }
        if (Estilo is "cpu" or "ram" or "anel" or "armazenamento")
        {
            var c = new Point(h / 2, h / 2); double r = h * .36;
            Brush color = Estilo == "anel" ? Brushes.LimeGreen : Estilo == "armazenamento" ? Brushes.SandyBrown : azul;
            dc.DrawEllipse(null, new Pen(Brushes.DarkSlateGray, 4), c, r, r);
            if (double.IsFinite(Valor) && Valor > 0)
            {
                double angle = Math.Clamp(Valor, 0, 99.99) * 2 * Math.PI / 100;
                var arc = new StreamGeometry();
                using (var ctx = arc.Open()) { ctx.BeginFigure(new Point(c.X, c.Y - r), false, false); ctx.ArcTo(new Point(c.X + Math.Sin(angle) * r, c.Y - Math.Cos(angle) * r), new Size(r, r), 0, angle > Math.PI, SweepDirection.Clockwise, true, false); }
                dc.DrawGeometry(null, new Pen(color, 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, arc);
            }
            var percentual = double.IsFinite(Valor) ? $"{Valor:F0}%" : "—";
            Text(percentual, 14, c.X - r + 5, c.Y - 10, largura: r * 2 - 6);
            if (w > h + 30)
            {
                Text(Titulo, 10, h + 5, 10, color);
                Text(Estilo == "armazenamento" ? Texto : "Uso atual", 12, h + 5, 29, Brushes.Silver);
            }
            return;
        }
        if (Estilo.EndsWith("-grafico", StringComparison.Ordinal))
        {
            Text(Titulo, 10, 7, 4, azul, w * .38); Text(Texto, 10, w * .46, 3);
            if (!string.IsNullOrEmpty(Secundario)) Text(Secundario, 9, w * .46, 16, Brushes.MediumPurple);
            var primary = Serie?.ToArray() ?? Array.Empty<double>(); var secondary = SerieSecundaria?.ToArray() ?? Array.Empty<double>();
            double max = Estilo == "rede-grafico" ? Math.Max(1, primary.Concat(secondary).DefaultIfEmpty(0).Max()) : 100;
            void Curve(double[] values, Brush color)
            {
                if (values.Length < 2) return;
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open()) for (int i = 0; i < values.Length; i++)
                {
                    var p = new Point(7 + i * (w - 14) / Math.Max(29, values.Length - 1), h - 6 - Math.Clamp(values[i] / max, 0, 1) * (h - 27));
                    if (i == 0) ctx.BeginFigure(p, false, false); else ctx.LineTo(p, true, false);
                }
                dc.DrawGeometry(null, new Pen(color, 2), geometry);
            }
            Curve(primary, azul); if (Estilo == "rede-grafico") Curve(secondary, Brushes.MediumPurple);
            if (primary.Length < 2) Text("Aguardando leituras…", 10, 8, 30, Brushes.Silver);
            return;
        }
        if (Estilo == "compacto" && Texto.Contains('·'))
        {
            var partes = Texto.Split('·');
            Text("CPU", 10, 10, 8, azul, w / 2 - 14);
            Text(partes[0].Replace("CPU", "").Trim(), 16, 10, 25, largura: w / 2 - 14);
            Text("RAM", 10, w / 2 + 6, 8, Brushes.MediumPurple);
            Text(partes[1].Replace("RAM", "").Trim(), 16, w / 2 + 6, 25);
            return;
        }
        Text(Titulo, 10, 8, 4, azul); Text(Texto, Estilo is "compacto" or "expandido" ? 13 : 16, 8, 20);
        if (!string.IsNullOrEmpty(Secundario)) Text(Secundario, 12, 8, 39, Brushes.Silver);
    }
}
