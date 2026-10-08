using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DockWindows.App.Views.Sections;

public partial class SectionCalendarioInline : UserControl
{
    private DockWindows.App.Views.DockFlyoutWindow? _agenda;

    public SectionCalendarioInline()
    {
        InitializeComponent();
        Unloaded += (_, _) => { _agenda?.Close(); _agenda = null; };
    }

    private void Calendario_Click(object sender, MouseButtonEventArgs e) => AbrirAgenda(sender as FrameworkElement);

    private void Calendario_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space)) return;
        AbrirAgenda(sender as FrameworkElement);
        e.Handled = true;
    }

    private void AbrirAgenda(FrameworkElement? anchor)
    {
        if (anchor == null || DataContext is not DockWindows.App.ViewModels.MainViewModel main) return;
        _agenda?.Close();
        _agenda = new DockWindows.App.Views.DockFlyoutWindow("Próximos compromissos");
        var container = (Border)_agenda.Content;
        var body = (StackPanel)container.Child;
        var eventos = main.Calendario.TodosCompromissos
            .Where(c => c.DataHora >= DateTime.Now.Date).OrderBy(c => c.DataHora).Take(8).ToList();
        if (eventos.Count == 0)
            body.Children.Add(new TextBlock { Text = "Nenhum compromisso futuro.", Foreground = Brushes.Silver, Margin = new Thickness(0, 4, 0, 6) });
        foreach (var evento in eventos)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(27, 35, 44)), CornerRadius = new CornerRadius(10),
                Padding = new Thickness(11, 8, 11, 8), Margin = new Thickness(0, 0, 0, 7), Width = 330
            };
            var linha = new Grid();
            linha.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
            linha.ColumnDefinitions.Add(new ColumnDefinition());
            linha.Children.Add(new TextBlock
            {
                Text = evento.DataHora.ToString("ddd, dd/MM\nHH:mm", CultureInfo.GetCultureInfo("pt-BR")),
                Foreground = Brushes.LightSkyBlue, FontSize = 11
            });
            var detalhes = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(detalhes, 1);
            detalhes.Children.Add(new TextBlock
            {
                Text = evento.Titulo, Foreground = Brushes.White, FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 220
            });
            if (!string.IsNullOrWhiteSpace(evento.Local))
                detalhes.Children.Add(new TextBlock
                {
                    Text = evento.Local, Foreground = Brushes.Silver, FontSize = 10,
                    TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 220
                });
            linha.Children.Add(detalhes);
            card.Child = linha;
            body.Children.Add(card);
        }
        _agenda.MostrarPerto(anchor);
    }

    private void ConfigurarReuniao_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DockWindows.App.ViewModels.MainViewModel main) return;
        var atual = main.Calendario.ProximaReuniao;
        var editor = new DockWindows.App.Views.ReuniaoEditorWindow(atual) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true || editor.Resultado == null) return;
        var lista = main.Preferencias.CompromissosLocais;
        var index = atual == null ? -1 : lista.FindIndex(c => c.Id == atual.Id);
        if (index >= 0) lista[index] = editor.Resultado; else lista.Add(editor.Resultado);
        main.Calendario.SincronizarCompromissos(lista.ToList());
        main.SalvarPreferencias();
    }
}
