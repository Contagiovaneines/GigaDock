using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DockWindows.Core.Models;
using DockWindows.Core.Validation;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.Views;

public sealed class PastaPreviewWindow : DockFlyoutWindow
{
    private readonly ListBox _files = new() { Width = 215, MaxHeight = 330, Background = Brushes.Transparent, Foreground = Brushes.White };
    private readonly StackPanel _preview = new() { Width = 280, Margin = new Thickness(16, 0, 0, 0) };
    private readonly LauncherService _launcher = new();
    private string? _selected;
    private int _generation;
    private bool _closed;
    private readonly CancellationTokenSource _cancelamento = new();
    private sealed record Arquivo(string Caminho, string Nome) { public override string ToString() => Nome; }

    public PastaPreviewWindow(string pasta) : base(Path.GetFileName(pasta.TrimEnd(Path.DirectorySeparatorChar)))
    {
        Closed += (_, _) => { _closed = true; _generation++; _selected = null; _cancelamento.Cancel(); _cancelamento.Dispose(); _files.ItemsSource = null; _preview.Children.Clear(); };
        var path = Environment.ExpandEnvironmentVariables(pasta);
        var result = WindowsItemPathValidator.Instance.ValidarPasta(path);
        if (!result.Valido || !Directory.Exists(path))
        {
            Body.Children.Add(new TextBlock { Text = result.MensagemErro ?? "Esta pasta não oferece prévia local.", TextWrapping = TextWrapping.Wrap, MaxWidth = 400 });
            return;
        }
        Body.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { _files, _preview } });
        var abrirPasta = new Button { Content = "Abrir pasta no Explorador", Background = Azul, Foreground = Brushes.White, Margin = new Thickness(0, 12, 0, 0) };
        abrirPasta.Click += (_, _) => Abrir(path, TipoItem.Pasta);
        Body.Children.Add(abrirPasta);
        _files.SelectionChanged += async (_, _) => await SelecionarAsync();
        Body.Children.Add(new TextBlock { Text = "Carregando itens…", FontSize = 11, Margin = new Thickness(0, 8, 0, 0) });
        Loaded += async (_, _) =>
        {
            try
            {
                var token = _cancelamento.Token;
                var itens = await Task.Run(() => Directory.EnumerateFileSystemEntries(path)
                    .Take(101).Select(p => { token.ThrowIfCancellationRequested(); return new Arquivo(p, Path.GetFileName(p)); }).OrderBy(p => p.Nome).ToList(), token);
                if (_closed) return;
                _files.ItemsSource = itens.Take(100).ToList();
                ((TextBlock)Body.Children[^1]).Text = itens.Count > 100 ? "Mostrando 100 itens. Abra a pasta para ver todos." : itens.Count == 0 ? "Pasta vazia." : "Selecione um item para visualizar.";
                _files.SelectedIndex = itens.Count > 0 ? 0 : -1;
            }
            catch { if (!_closed) ((TextBlock)Body.Children[^1]).Text = "Não foi possível ler esta pasta. Verifique o acesso."; }
        };
    }

    private async Task SelecionarAsync()
    {
        if (_closed || _files.SelectedItem is not Arquivo arquivo) return;
        var generation = ++_generation;
        _selected = arquivo.Caminho;
        _preview.Children.Clear();
        _preview.Children.Add(new TextBlock { Text = arquivo.Nome, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        var content = new ContentControl { Height = 240 };
        _preview.Children.Add(content);
        var abrir = new Button { Content = "Abrir item", Background = Azul, Foreground = Brushes.White, Margin = new Thickness(0, 12, 0, 0) };
        abrir.Click += (_, _) => { if (_selected != null) Abrir(_selected, Directory.Exists(_selected) ? TipoItem.Pasta : TipoItem.Arquivo); };
        _preview.Children.Add(abrir);
        try
        {
            if (Directory.Exists(arquivo.Caminho)) { content.Content = new TextBlock { Text = "Pasta\nUse Abrir item para acessar.", TextWrapping = TextWrapping.Wrap }; return; }
            var info = new FileInfo(arquivo.Caminho);
            var ext = info.Extension.ToLowerInvariant();
            if (new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" }.Contains(ext) && info.Length <= 20 * 1024 * 1024)
            {
                var image = await Task.Run(() =>
                {
                    // A decodificacao reduzida evita carregar imagens enormes na interface.
                    using var stream = File.OpenRead(arquivo.Caminho);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit(); bitmap.StreamSource = stream; bitmap.DecodePixelWidth = 560;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit(); bitmap.Freeze();
                    return bitmap;
                });
                if (!_closed && generation == _generation) content.Content = new Image { Source = image, Stretch = Stretch.Uniform };
            }
            else if (new[] { ".txt", ".md", ".log", ".csv", ".json" }.Contains(ext) && info.Length <= 1024 * 1024)
            {
                var text = await Task.Run(() =>
                {
                    using var reader = new StreamReader(arquivo.Caminho);
                    var chars = new char[4000]; int n = reader.ReadBlock(chars, 0, chars.Length);
                    return new string(chars, 0, n);
                });
                if (!_closed && generation == _generation) content.Content = new ScrollViewer { Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12 } };
            }
            else content.Content = new TextBlock { Text = $"Arquivo {ext}\n{info.Length / 1024.0:N0} KB\n\nPrévia não disponível para este formato ou tamanho.", TextWrapping = TextWrapping.Wrap };
        }
        catch { if (generation == _generation) content.Content = new TextBlock { Text = "Não foi possível carregar a prévia.", TextWrapping = TextWrapping.Wrap }; }
    }

    private void Abrir(string path, TipoItem tipo)
    {
        var result = _launcher.Executar(new ItemFixado { Titulo = Path.GetFileName(path), CaminhoOuUrl = path, Tipo = tipo });
        if (!result.Sucesso) MessageBox.Show(this, result.MensagemErro ?? "Não foi possível abrir o item.", "Pasta", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
