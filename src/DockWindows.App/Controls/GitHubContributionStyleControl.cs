using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Controls;

public sealed class GitHubContributionStyleControl : FrameworkElement
{
    private readonly HashSet<INotifyPropertyChanged> _itensAssinados = new();
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(GitHubContributionStyleControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, AoTrocarItens));

    public static readonly DependencyProperty TotalProperty = DependencyProperty.Register(
        nameof(Total), typeof(int), typeof(GitHubContributionStyleControl),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty EstiloProperty = DependencyProperty.Register(
        nameof(Estilo), typeof(string), typeof(GitHubContributionStyleControl),
        new FrameworkPropertyMetadata("grade-compacta", FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty PreviewProperty = DependencyProperty.Register(
        nameof(Preview), typeof(bool), typeof(GitHubContributionStyleControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool Preview { get => (bool)GetValue(PreviewProperty); set => SetValue(PreviewProperty, value); }
    public static readonly DependencyProperty TotalConfirmadoProperty = DependencyProperty.Register(
        nameof(TotalConfirmado), typeof(bool), typeof(GitHubContributionStyleControl), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool TotalConfirmado { get => (bool)GetValue(TotalConfirmadoProperty); set => SetValue(TotalConfirmadoProperty, value); }

    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public int Total { get => (int)GetValue(TotalProperty); set => SetValue(TotalProperty, value); }
    public string Estilo { get => (string)GetValue(EstiloProperty); set => SetValue(EstiloProperty, value); }

    public GitHubContributionStyleControl()
    {
        Loaded += (_, _) => Assinar(ItemsSource);
        Unloaded += (_, _) => Desassinar(ItemsSource);
    }

    private static void AoTrocarItens(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var controle = (GitHubContributionStyleControl)d;
        controle.Desassinar(e.OldValue as IEnumerable);
        if (controle.IsLoaded) controle.Assinar(e.NewValue as IEnumerable);
        controle.InvalidateVisual();
    }

    private void Assinar(IEnumerable? fonte)
    {
        if (fonte is INotifyCollectionChanged colecao) colecao.CollectionChanged += ColecaoAlterada;
        if (fonte != null)
            foreach (var item in fonte.OfType<INotifyPropertyChanged>()) AssinarItem(item);
    }

    private void Desassinar(IEnumerable? fonte)
    {
        if (fonte is INotifyCollectionChanged colecao) colecao.CollectionChanged -= ColecaoAlterada;
        foreach (var item in _itensAssinados) item.PropertyChanged -= ItemAlterado;
        _itensAssinados.Clear();
    }

    private void ColecaoAlterada(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var item in _itensAssinados) item.PropertyChanged -= ItemAlterado;
            _itensAssinados.Clear();
            if (ItemsSource != null)
                foreach (var item in ItemsSource.OfType<INotifyPropertyChanged>()) AssinarItem(item);
        }
        else
        {
            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<INotifyPropertyChanged>())
                    if (_itensAssinados.Remove(item)) item.PropertyChanged -= ItemAlterado;
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>()) AssinarItem(item);
        }
        InvalidateVisual();
    }

    private void AssinarItem(INotifyPropertyChanged item)
    {
        if (_itensAssinados.Add(item)) item.PropertyChanged += ItemAlterado;
    }

    private void ItemAlterado(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    protected override Size MeasureOverride(Size availableSize) =>
        Estilo == "resumo-anual" ? new Size(300, 68) : new Size(68, 68);

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var largo = Estilo == "resumo-anual";
        var rect = new Rect(1, 1, Math.Max(1, ActualWidth - 2), Math.Max(1, ActualHeight - 2));
        dc.DrawRoundedRectangle(largo
                ? new LinearGradientBrush(Color.FromRgb(103, 105, 107), Color.FromRgb(68, 71, 73), 90)
                : new LinearGradientBrush(Color.FromRgb(60, 67, 70), Color.FromRgb(24, 31, 33), 90),
            new Pen(new SolidColorBrush(largo ? Color.FromRgb(158, 161, 162) : Color.FromRgb(76, 86, 88)), 1), rect, 16, 16);
        var colunas = largo ? 36 : 7;
        var dias = ItemsSource?.OfType<ContribuicaoDia>().Take(colunas * 7).ToArray() ?? Array.Empty<ContribuicaoDia>();
        var culture = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        double inicioX;
        if (largo)
        {
            var total = Preview || TotalConfirmado ? Total.ToString("N0", culture) : "—";
            var typeface = new Typeface("Segoe UI Semibold");
            var texto = new FormattedText(total, culture, FlowDirection.LeftToRight,
                typeface, 18, Brushes.White, dpi) { MaxTextWidth = 64, Trimming = TextTrimming.CharacterEllipsis };
            dc.DrawText(texto, new Point(12, 20));
            dc.DrawText(new FormattedText("no período", culture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 9, new SolidColorBrush(Color.FromRgb(218, 221, 222)), dpi), new Point(13, 42));
            inicioX = 80;
        }
        else inicioX = (ActualWidth - 47.5) / 2;

        var celula = largo ? 4.7 : 5.5;
        var passo = largo ? 6 : 7;
        var inicioY = (ActualHeight - (6 * passo + celula)) / 2;
        var pacManAtivo = dias.Any(d => d.EhPacMan);
        for (var i = 0; i < colunas * 7; i++)
        {
            var dia = i < dias.Length ? dias[i] : null;
            var nivel = dia?.Nivel ?? (Preview ? (i * 7 + i / 3) % 5 : 0);
            var x = inicioX + i % colunas * passo;
            var y = inicioY + i / colunas * passo;
            var cell = new Rect(x, y, celula, celula);
            var animado = dia != null && (dia.MarcaArcade > 0 || dia.EhCobra || dia.EhCabecaCobra || dia.EhPacMan || dia.EhFantasma);
            var brush = animado && dia is not { EhPacMan: true } && dia is not { EhFantasma: true } ? dia!.Cor : Cores[Math.Clamp(nivel, 0, 4)];
            dc.DrawRoundedRectangle(brush, null, cell, 1, 1);
            if (pacManAtivo && nivel > 0 && dia is not { EhPacMan: true } && dia is not { EhFantasma: true })
                dc.DrawEllipse(Brushes.Gold, null, new Point(x + celula / 2, y + celula / 2), .8, .8);
            if (dia != null && !string.IsNullOrEmpty(dia.Simbolo))
            {
                var simbolo = new FormattedText(dia.Simbolo, culture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), celula, Brushes.White, dpi);
                dc.DrawText(simbolo, new Point(x + (celula - simbolo.Width) / 2, y + (celula - simbolo.Height) / 2));
            }
        }
        // Sobrepor os personagens depois da grade preserva suas silhuetas nos dois estilos.
        for (var i = 0; i < dias.Length; i++)
        {
            var dia = dias[i];
            if (!dia.EhPacMan && !dia.EhFantasma) continue;
            var size = passo - .3;
            var x = inicioX + i % colunas * passo + (celula - size) / 2;
            var y = inicioY + i / colunas * passo + (celula - size) / 2;
            GitHubPacManSprites.Draw(dc, new Rect(x, y, size, size), dia);
        }
    }

    private static readonly Brush[] Cores = new[] { "#303638", "#0E6332", "#0A913B", "#16BF4B", "#28D95C" }
        .Select(c => { var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(c)!; brush.Freeze(); return (Brush)brush; }).ToArray();
}
