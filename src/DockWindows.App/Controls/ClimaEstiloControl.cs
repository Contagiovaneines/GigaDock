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
    public static double LarguraPara(string estilo) => estilo switch { "minimalista" => 76, "largo" => 205, "temperatura" => 100, "condicao" => 120, "vento" => 160, "horas" => 310, "compacto" => 260, "previsao" => 205, "local" => 245, "sol" => 220, _ => 90 };
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        var vm = Dados;
        var w = ActualWidth; var h = ActualHeight;
        dc.DrawRoundedRectangle(new SolidColorBrush(Estilo == "detalhado" ? Color.FromRgb(45, 120, 199) : Color.FromRgb(36, 39, 29)), new Pen(Brushes.DimGray, 1), new Rect(0, 0, w, h), 12, 12);
        void Text(string value, double size, double x, double y, Brush? color = null, double max = 0)
        {
            var t = new FormattedText(value, CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight, new Typeface("Segoe UI Semibold"), size, color ?? Brushes.WhiteSmoke, VisualTreeHelper.GetDpi(this).PixelsPerDip) { MaxTextWidth = Math.Max(1, max > 0 ? max : w - x - 5), Trimming = TextTrimming.CharacterEllipsis };
            dc.DrawText(t, new Point(x, y));
        }
        string temp = vm?.Temperatura ?? "—°", icon = PrevisaoClima.Simbolo(vm?.TipoAtual ?? "Indisponivel");
        if (Estilo == "minimalista") { Text(icon, 22, 8, 10, Brushes.Silver, 28); Text(temp, 20, 35, 17, max: 38); return; }
        if (Estilo == "largo")
        {
            var hoje = vm?.Previsoes.FirstOrDefault();
            Text(vm?.Local ?? "—", 9, 10, 7, Brushes.Silver, w - 24);
            Text(icon, 23, 10, 25, Brushes.Silver, 32);
            Text(temp, 26, 43, 20, max: 62);
            Text(hoje == null ? vm?.Condicao ?? "—" : $"{hoje.Minima} / {hoje.Temperatura}", 10, 108, 29, Brushes.Silver, w - 116);
            return;
        }
        if (Estilo == "detalhado") { Text(temp, 27, 12, 13); return; }
        if (Estilo == "temperatura") { Text(icon, 24, 8, 15, Brushes.Silver, 32); Text(temp, 27, 40, 13); return; }
        if (Estilo == "local") { Text(vm?.Local ?? "—", 12, 10, 8, max: w - 85); Text($"{vm?.Vento ?? "—"} · {vm?.Precipitacao ?? "—"}", 10, 10, 34, Brushes.Silver, w - 85); Text(icon + " " + temp, 24, w - 83, 18); return; }
        if (Estilo == "condicao") { Text(icon, 28, w / 2 - 15, 4); Text($"{vm?.Condicao ?? "—"}, {temp}", 11, 8, 39); return; }
        if (Estilo is "compacto" or "previsao")
        {
            double start = Estilo == "compacto" ? 87 : 6;
            if (Estilo == "compacto") { Text(temp, 29, 10, 4, max: 73); Text("Hoje", 11, 10, 40, Brushes.Silver); }
            var days = vm?.Previsoes.Take(3).ToArray() ?? Array.Empty<PrevisaoClima>();
            double cell = (w - start - 6) / Math.Max(3, days.Length);
            for (int i = 0; i < days.Length; i++) { var x = start + i * cell; Text(days[i].Dia, 9, x, 6, Brushes.Silver, cell); Text(days[i].Icone, 20, x + 4, 18, Brushes.Silver, cell); Text(days[i].Temperatura, 11, x, 43, max: cell); }
            return;
        }
        if (Estilo == "horas")
        {
            var hours = vm?.Horas.Take(5).ToArray() ?? Array.Empty<PrevisaoHora>();
            if (hours.Length == 0) { Text("Previsão por hora indisponível", 12, 10, 22); return; }
            double cell = (w - 12) / hours.Length;
            for (int i = 0; i < hours.Length; i++) { var x = 6 + i * cell; Text(hours[i].Hora, 9, x, 5, Brushes.Silver, cell); Text(hours[i].Icone, 20, x + 6, 18, Brushes.Silver, cell); Text(hours[i].Temperatura, 11, x + 3, 43, max: cell); }
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
}
