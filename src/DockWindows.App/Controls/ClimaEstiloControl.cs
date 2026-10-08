using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Controls;

public class ClimaEstiloControl : FrameworkElement
{
    public static readonly DependencyProperty EstiloProperty = DependencyProperty.Register(nameof(Estilo), typeof(string), typeof(ClimaEstiloControl), new FrameworkPropertyMetadata("compacto", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty DadosProperty = DependencyProperty.Register(nameof(Dados), typeof(ClimaWidgetViewModel), typeof(ClimaEstiloControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, DadosAlterados));
    public string Estilo { get => (string)GetValue(EstiloProperty); set => SetValue(EstiloProperty, value); }
    public ClimaWidgetViewModel? Dados { get => (ClimaWidgetViewModel?)GetValue(DadosProperty); set => SetValue(DadosProperty, value); }
    private bool _escutando;
    public ClimaEstiloControl() { Loaded += (_, _) => Escutar(true); Unloaded += (_, _) => Escutar(false); }
    private static void DadosAlterados(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (ClimaEstiloControl)d;
        if (c._escutando && e.OldValue is ClimaWidgetViewModel old) c.Assinar(old, false);
        c._escutando = false;
        if (c.IsLoaded) c.Escutar(true);
    }
    private void Escutar(bool escutar)
    {
        if (_escutando == escutar || Dados == null) return;
        Assinar(Dados, escutar); _escutando = escutar;
    }
    private void Assinar(ClimaWidgetViewModel vm, bool assinar)
    {
        if (assinar) { vm.PropertyChanged += Atualizar; vm.Horas.CollectionChanged += AtualizarLista; vm.Previsoes.CollectionChanged += AtualizarLista; }
        else { vm.PropertyChanged -= Atualizar; vm.Horas.CollectionChanged -= AtualizarLista; vm.Previsoes.CollectionChanged -= AtualizarLista; }
    }
    private void Atualizar(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();
    private void AtualizarLista(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
    public static double LarguraPara(string estilo) => estilo switch { "minimalista" => 112, "largo" => 236, "temperatura" => 128, "condicao" => 140, "vento" => 160, "horas" => 310, "compacto" => 260, "previsao" => 205, "local" => 280, "sol" => 220, _ => 90 };
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        var vm = Dados;
        var w = ActualWidth; var h = ActualHeight;
        if (Estilo == "detalhado")
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(45, 120, 199)), new Pen(Brushes.DimGray, 1), new Rect(0, 0, w, h), 12, 12);
        else
            dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromArgb(215, 29, 39, 52), Color.FromArgb(225, 15, 22, 32), 90), new Pen(new SolidColorBrush(Color.FromArgb(110, 101, 126, 150)), 1), new Rect(.5, .5, w - 1, h - 1), 12, 12);

        void Text(string value, double size, double x, double y, Brush? color = null, double max = 0)
        {
            var width = Math.Max(1, max > 0 ? max : w - x - 5);
            var t = new FormattedText(value, CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight, new Typeface("Segoe UI Semibold"), size, color ?? Brushes.WhiteSmoke, VisualTreeHelper.GetDpi(this).PixelsPerDip) { MaxTextWidth = width, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };
            dc.PushClip(new RectangleGeometry(new Rect(x, y, width, size * 1.5)));
            dc.DrawText(t, new Point(x, y));
            dc.Pop();
        }
        void Icon(string tipo, double x, double y, double size = 28) => DrawWeatherIcon(dc, tipo, new Rect(x, y, size, size));
        string temp = vm?.Temperatura ?? "—";
        string tipo = vm?.TipoAtual ?? "Indisponivel";
        
        if (Estilo == "minimalista") { Icon(tipo, 12, (h - 26) / 2, 26); Text(temp, 22, 48, (h - 32) / 2, max: w - 58); return; }
        if (Estilo == "largo")
        {
            var hoje = vm?.Previsoes.FirstOrDefault();
            Text(vm?.Local ?? "—", 10, 12, 8, Brushes.Silver, w - 24);
            Icon(tipo, 12, 29);
            Text(temp, 25, 50, 25, max: 70);
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(64, 81, 99)), 1), new Point(130, 28), new Point(130, 54));
            if (hoje == null) Text(vm?.Condicao ?? "—", 11, 142, 34, Brushes.LightSteelBlue, w - 154);
            else
            {
                Text($"Mín {hoje.Minima}", 10, 142, 27, Brushes.LightSteelBlue, w - 154);
                Text($"Máx {hoje.Temperatura}", 10, 142, 43, Brushes.WhiteSmoke, w - 154);
            }
            return;
        }
        if (Estilo == "detalhado") { Text(temp, 27, 12, 13); return; }
        if (Estilo == "temperatura") { Icon(tipo, 12, (h - 30) / 2, 30); Text(temp, 26, 52, (h - 38) / 2, max: w - 62); return; }
        if (Estilo == "local") { Text(vm?.Local ?? "—", 12, 12, 10, max: w - 126); Text($"{vm?.Vento ?? "—"} • {vm?.Precipitacao ?? "—"}", 10, 12, 34, Brushes.Silver, w - 126); Icon(tipo, w - 112, 18, 28); Text(temp, 24, w - 74, 17, max: 64); return; }
        if (Estilo == "condicao") { Icon(tipo, w / 2 - 14, 7); Text($"{vm?.Condicao ?? "—"}, {temp}", 11, 10, 42, max: w - 20); return; }
        if (Estilo is "compacto" or "previsao")
        {
            double start = Estilo == "compacto" ? 87 : 6;
            if (Estilo == "compacto") { Text(temp, 29, 12, 6, max: 73); Text("Hoje", 11, 12, 42, Brushes.Silver); }
            var days = vm?.Previsoes.Take(3).ToArray() ?? Array.Empty<PrevisaoClima>();
            double cell = (w - start - 6) / Math.Max(3, days.Length);
            for (int i = 0; i < days.Length; i++) { var x = start + i * cell; Text(days[i].Dia, 10, x, 6, Brushes.Silver, cell - 6); Icon(days[i].TipoIcone, x, 22, 20); Text(days[i].Temperatura, 11, x, 45, max: cell - 6); }
            return;
        }
        if (Estilo == "horas")
        {
            var hours = vm?.Horas.Take(5).ToArray() ?? Array.Empty<PrevisaoHora>();
            if (hours.Length == 0) { Text("Previsão por hora indisponível", 12, 10, 22); return; }
            double cell = (w - 12) / hours.Length;
            for (int i = 0; i < hours.Length; i++) { var x = 6 + i * cell; Text(hours[i].Hora, 9, x, 5, Brushes.Silver, cell - 6); Icon(hours[i].TipoIcone, x, 21, 20); Text(hours[i].Temperatura, 11, x, 45, max: cell - 6); }
            return;
        }
        if (Estilo == "vento") { Text("➤", 28, 8, 13, Brushes.Silver, 35); Text(vm?.Vento ?? "—", 19, 45, 7); Text(vm?.DirecaoVento ?? "—", 11, 45, 35, Brushes.Silver); return; }
        if (Estilo == "sol")
        {
            var arc = new StreamGeometry();
            using (var ctx = arc.Open()) { ctx.BeginFigure(new Point(12, 37), false, false); ctx.QuadraticBezierTo(new Point(w / 2, -5), new Point(w - 12, 37), true, false); }
            dc.DrawGeometry(null, new Pen(Brushes.SlateGray, 1.5), arc);
            dc.DrawLine(new Pen(Brushes.Gray, 1), new Point(12, 37), new Point(w - 12, 37));
            Text("↑ " + (vm?.NascerSol ?? "—"), 11, 9, 42, Brushes.SandyBrown);
            Text("↓ " + (vm?.PorSol ?? "—"), 11, w - 67, 42, Brushes.SandyBrown);
            if (TimeOnly.TryParseExact(vm?.NascerSol, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sunrise) && TimeOnly.TryParseExact(vm?.PorSol, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sunset) && vm?.Observacao is { } obs && sunset > sunrise)
            {
                double t = Math.Clamp((obs.TimeOfDay - sunrise.ToTimeSpan()).TotalMinutes / (sunset - sunrise).TotalMinutes, 0, 1);
                dc.DrawEllipse(Brushes.Gold, null, new Point(12 + (w - 24) * t, 37 - 84 * t * (1 - t)), 3.5, 3.5);
            }
        }
    }

    private static void DrawWeatherIcon(DrawingContext dc, string tipo, Rect bounds)
    {
        dc.PushClip(new RectangleGeometry(bounds));
        dc.PushTransform(new TranslateTransform(bounds.X, bounds.Y));
        dc.PushTransform(new ScaleTransform(bounds.Width / 32, bounds.Height / 32));
        var cloud = new SolidColorBrush(Color.FromRgb(225, 237, 249));
        var blue = new Pen(new SolidColorBrush(Color.FromRgb(97, 192, 255)), 2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (tipo == "Sol")
        {
            var sun = new SolidColorBrush(Color.FromRgb(255, 210, 104));
            for (var i = 0; i < 8; i++)
            {
                var a = i * Math.PI / 4;
                dc.DrawLine(new Pen(sun, 2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
                    new Point(16 + Math.Cos(a) * 11, 16 + Math.Sin(a) * 11), new Point(16 + Math.Cos(a) * 14, 16 + Math.Sin(a) * 14));
            }
            dc.DrawEllipse(sun, null, new Point(16, 16), 7, 7);
        }
        else if (tipo is "Nuvem" or "Chuva" or "Tempestade" or "Neve")
        {
            var y = tipo == "Nuvem" ? 2d : -2d;
            dc.DrawEllipse(cloud, null, new Point(10, 17 + y), 7, 6);
            dc.DrawEllipse(cloud, null, new Point(18, 13 + y), 8, 8);
            dc.DrawRoundedRectangle(cloud, null, new Rect(9, 15 + y, 21, 8), 4, 4);
            if (tipo == "Chuva")
                for (var i = 0; i < 3; i++) dc.DrawLine(blue, new Point(10 + i * 7, 25), new Point(8 + i * 7, 29));
            if (tipo == "Neve")
                for (var i = 0; i < 3; i++) dc.DrawEllipse(Brushes.LightSkyBlue, null, new Point(9 + i * 7, 27), 1.5, 1.5);
            if (tipo == "Tempestade")
            {
                var bolt = new StreamGeometry();
                using (var ctx = bolt.Open())
                {
                    ctx.BeginFigure(new Point(18, 20), true, true);
                    ctx.PolyLineTo(new[] { new Point(13, 26), new Point(17, 26), new Point(14, 32), new Point(23, 23), new Point(18, 23) }, true, false);
                }
                dc.DrawGeometry(Brushes.Gold, null, bolt);
            }
        }
        else dc.DrawLine(new Pen(cloud, 2), new Point(10, 16), new Point(22, 16));
        dc.Pop(); dc.Pop(); dc.Pop();
    }
}
