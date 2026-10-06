using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DockWindows.App.Controls;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views;

public sealed class JanelasPreviewWindow : DockFlyoutWindow
{
    public JanelasPreviewWindow(AppItemViewModel app) : base(app.Titulo)
    {
        var paginas = new StackPanel();
        var tiles = new StackPanel { Orientation = Orientation.Horizontal };
        int page = 0;
        int perPage = 3;
        void Render()
        {
            tiles.Children.Clear();
            foreach (var janela in app.Janelas.Skip(page * perPage).Take(perPage))
            {
                var tile = new StackPanel { Width = 216, Margin = new Thickness(4) };
                var titulo = new TextBlock { Text = janela.Titulo, FontSize = 12, Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(6) };
                var preview = new DwmPreviewControl { SourceHwnd = janela.Hwnd, Height = 132 };
                var button = new Button { Content = new StackPanel { Children = { titulo, preview } },
                    Padding = new Thickness(0), Background = Azul, BorderBrush = Brushes.SlateBlue, ToolTip = "Ativar esta janela" };
                button.Click += (_, _) => { app.AtivarJanelaCommand.Execute(janela); Close(); };
                tile.Children.Add(button);
                var fechar = new Button { Content = "Fechar janela", Foreground = Brushes.White, Background = Azul, Margin = new Thickness(0, 5, 0, 0) };
                fechar.Click += (_, _) => { app.FecharJanelaCommand.Execute(janela); if (app.Janelas.Count == 0) Close(); else { page = Math.Min(page, (app.Janelas.Count - 1) / perPage); Render(); } };
                tile.Children.Add(fechar);
                tiles.Children.Add(tile);
            }
        }
        // O painel sempre cabe no monitor; paginas evitam recortar miniaturas DWM.
        var anterior = new Button { Content = "← Anterior", Margin = new Thickness(4), Background = Azul, Foreground = Brushes.White };
        var proximo = new Button { Content = "Próximas →", Margin = new Thickness(4), Background = Azul, Foreground = Brushes.White };
        anterior.Click += (_, _) => { if (page > 0) { page--; Render(); } };
        proximo.Click += (_, _) => { if ((page + 1) * perPage < app.Janelas.Count) { page++; Render(); } };
        paginas.Children.Add(tiles);
        paginas.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { anterior, proximo } });
        Body.Children.Add(paginas);
        Loaded += (_, _) => { perPage = Math.Clamp((int)((MaxWidth - 40) / 224), 1, 3); Render(); };
        Render();
    }
}