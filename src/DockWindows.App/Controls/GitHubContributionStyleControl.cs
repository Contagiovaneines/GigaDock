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
        new FrameworkPropertyMetadata("grade-compacta", FrameworkPropertyMetadataOptions.AffectsRender));

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
        Estilo == "resumo-anual" ? new Size(286, 68) : new Size(82, 68);

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var largo = Estilo == "resumo-anual";
        var rect = new Rect(1, 1, Math.Max(1, ActualWidth - 2), Math.Max(1, ActualHeight - 2));
        dc.DrawRoundedRectangle(new LinearGradientBrush(Color.FromRgb(55, 60, 63), Color.FromRgb(34, 38, 41), 90),
            new Pen(new SolidColorBrush(Color.FromRgb(93, 101, 105)), 1), rect, 18, 18);

        var niveis = ItemsSource?.Cast<object>().OfType<ContribuicaoDia>().Select(x => x.Nivel).Take(104).ToArray();
        if (niveis is not { Length: > 0 })
            niveis = Enumerable.Range(0, largo ? 104 : 49).Select(i => (i * 7 + i / 3) % 5).ToArray();

        double inicioX;
        int colunas;
        int linhas;
        if (largo)
        {
            var total = Total > 0 ? Total.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) : "1.284";
            var typeface = new Typeface("Segoe UI Semibold");
            dc.DrawText(new FormattedText(total, System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight,
                typeface, 20, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(15, 13));
            dc.DrawText(new FormattedText("neste ano", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 10, new SolidColorBrush(Color.FromRgb(202, 207, 210)), VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(16, 38));
            inicioX = 103; colunas = 26; linhas = 4;
        }
        else { inicioX = 12; colunas = 7; linhas = 7; }

        var cores = new[] { "#3B4143", "#0E6B35", "#0D913F", "#19C657", "#39E86D" }.Select(ColorConverter.ConvertFromString).Cast<Color>().Select(c => new SolidColorBrush(c)).ToArray();
        const double celula = 6, espaco = 2;
        for (var i = 0; i < Math.Min(niveis.Length, colunas * linhas); i++)
        {
            var x = inicioX + (i % colunas) * (celula + espaco);
            var y = 14 + (i / colunas) * (celula + espaco);
            dc.DrawRoundedRectangle(cores[Math.Clamp(niveis[i], 0, 4)], null, new Rect(x, y, celula, celula), 1.5, 1.5);
        }
    }
}
