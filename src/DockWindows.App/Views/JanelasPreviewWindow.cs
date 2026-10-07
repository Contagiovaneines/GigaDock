using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DockWindows.App.Controls;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Views;

public sealed class JanelasPreviewWindow : DockFlyoutWindow
{
    public int AtrasoAposRemoverMs { get; set; } = 200;
    public JanelasPreviewWindow(AppItemViewModel app) : base(app.Titulo)
    {
        var paginas = new StackPanel();
        var tiles = new StackPanel { Orientation = Orientation.Horizontal };
        int page = 0;
        int perPage = 3;
        var anterior = CriarBotao("← Anterior");
        var proximo = CriarBotao("Próximas →");
        var contador = new TextBlock { Foreground = Brushes.SlateGray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 12, 0), FontSize = 12 };
        void Render()
        {
            tiles.Children.Clear();
            anterior.IsEnabled = page > 0;
            proximo.IsEnabled = (page + 1) * perPage < app.Janelas.Count;
            contador.Text = $"{page + 1} / {Math.Max(1, (app.Janelas.Count + perPage - 1) / perPage)}";
            foreach (var janela in app.Janelas.Skip(page * perPage).Take(perPage))
            {
                var tile = new StackPanel { Width = 216, Margin = new Thickness(4, 0, 4, 0) };
                var titulo = new TextBlock { Text = janela.Titulo, FontSize = 12, Foreground = Brushes.White,
                    TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = janela.Titulo, Margin = new Thickness(10, 10, 10, 10) };
                var preview = new DwmPreviewControl { SourceHwnd = janela.Hwnd, Height = 132 };
                var button = CriarBotao("");
                button.Content = new StackPanel { Children = { titulo, preview } };
                button.Padding = new Thickness(1, 1, 1, 8);
                button.ToolTip = "Ativar esta janela";
                System.Windows.Automation.AutomationProperties.SetName(button, $"Ativar {janela.Titulo}");
                button.Click += (_, _) => { app.AtivarJanelaCommand.Execute(janela); Close(); };
                tile.Children.Add(button);
                var fechar = CriarBotao("Fechar janela");
                fechar.Margin = new Thickness(0, 8, 0, 0);
                System.Windows.Automation.AutomationProperties.SetName(fechar, $"Fechar {janela.Titulo}");
                fechar.Click += async (_, _) =>
                {
                    app.FecharJanelaCommand.Execute(janela);
                    if (app.Janelas.Count == 0) { Close(); return; }
                    await Task.Delay(Math.Clamp(AtrasoAposRemoverMs, 0, 5000));
                    if (!IsVisible) return;
                    page = Math.Min(page, (app.Janelas.Count - 1) / perPage);
                    Render();
                };
                tile.Children.Add(fechar);
                tiles.Children.Add(tile);
            }
        }
        // O painel sempre cabe no monitor; paginas evitam recortar miniaturas DWM.
        anterior.Click += (_, _) => { if (page > 0) { page--; Render(); } };
        proximo.Click += (_, _) => { if ((page + 1) * perPage < app.Janelas.Count) { page++; Render(); } };
        paginas.Children.Add(tiles);
        paginas.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 14, 0, 0), Children = { anterior, contador, proximo } });
        Body.Children.Add(paginas);
        Loaded += (_, _) => { perPage = Math.Clamp((int)((MaxWidth - 40) / 224), 1, 3); Render(); };
        Render();
    }
}
