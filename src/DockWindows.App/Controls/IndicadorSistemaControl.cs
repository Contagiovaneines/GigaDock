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
    public static readonly DependencyProperty SerieProperty = DependencyProperty.Register(nameof(Serie), typeof(IEnumerable<double>), typeof(IndicadorSistemaControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, SerieAlterada));
    public static readonly DependencyProperty SerieSecundariaProperty = DependencyProperty.Register(nameof(SerieSecundaria), typeof(IEnumerable<double>), typeof(IndicadorSistemaControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, SerieAlterada));
    public string Estilo { get => (string)GetValue(EstiloProperty); set => SetValue(EstiloProperty, value); }
    public string Titulo { get => (string)GetValue(TituloProperty); set => SetValue(TituloProperty, value); }
    public string Texto { get => (string)GetValue(TextoProperty); set => SetValue(TextoProperty, value); }
    public string Secundario { get => (string)GetValue(SecundarioProperty); set => SetValue(SecundarioProperty, value); }
    public double Valor { get => (double)GetValue(ValorProperty); set => SetValue(ValorProperty, value); }
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
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(28, 34, 29)), new Pen(new SolidColorBrush(Color.FromRgb(60, 67, 54)), 1), new Rect(0, 0, w, h), 10, 10);
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
