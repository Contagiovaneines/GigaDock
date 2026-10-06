using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DockWindows.App.Common;
using DockWindows.Core.Services;
using DockWindows.Core.Widgets;
using DockWindows.Infrastructure.Files;
using Microsoft.Win32;

namespace DockWindows.App.ViewModels;

public sealed class EstanteArquivosViewModel : ObservableObject, IAtividadeWidget
{
    private readonly IReadOnlyList<IFilePreviewProvider> _providers = new IFilePreviewProvider[] { new TextFilePreviewProvider(), new ImageFilePreviewProvider(), new MetadataFilePreviewProvider() };
    private readonly Action<string> _salvar;
    private CancellationTokenSource? _previewCts;
    private bool _disposed, _habilitado, _painelAberto;
    private ArquivoEstanteViewModel? _selecionado;
    private string _previewTitulo = "Selecione um arquivo", _previewDescricao = string.Empty, _previewTexto = string.Empty;
    private BitmapImage? _previewImagem;

    public EstanteArquivosViewModel(Action<string> salvar)
    {
        _salvar = salvar;
        AdicionarCommand = new RelayCommand(Adicionar);
        RemoverCommand = new RelayCommand(RemoverSelecionado);
        AbrirCommand = new RelayCommand(AbrirSelecionado);
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
    }
    public ObservableCollection<ArquivoEstanteViewModel> Itens { get; } = new();
    public bool? EmExecucao => false;
    public SaudeWidget Saude => SaudeWidget.Disponivel;
    public string? MotivoEstado => $"{Itens.Count} arquivos na estante";
    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public bool PainelAberto { get => _painelAberto; set => SetProperty(ref _painelAberto, value); }
    public ArquivoEstanteViewModel? Selecionado { get => _selecionado; set { if (SetProperty(ref _selecionado, value)) _ = AtualizarPreviewAsync(); } }
    public string PreviewTitulo { get => _previewTitulo; private set => SetProperty(ref _previewTitulo, value); }
    public string PreviewDescricao { get => _previewDescricao; private set => SetProperty(ref _previewDescricao, value); }
    public string PreviewTexto { get => _previewTexto; private set => SetProperty(ref _previewTexto, value); }
    public BitmapImage? PreviewImagem { get => _previewImagem; private set { if (SetProperty(ref _previewImagem, value)) OnPropertyChanged(nameof(TemImagem)); } }
    public bool TemImagem => PreviewImagem != null;
    public ICommand AdicionarCommand { get; } public ICommand RemoverCommand { get; } public ICommand AbrirCommand { get; } public ICommand AlternarPainelCommand { get; }

    public void Configurar(string? json)
    {
        if (_disposed) return;
        List<string> caminhos;
        try { caminhos = string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<List<string>>(json) ?? new(); } catch { caminhos = new(); }
        Itens.Clear();
        foreach (var caminho in caminhos.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(30)) Itens.Add(new(caminho));
        Selecionado = Itens.FirstOrDefault();
    }

    private void Adicionar()
    {
        if (_disposed) return;
        var dialogo = new OpenFileDialog { Multiselect = true, Title = "Adicionar arquivos à estante" };
        if (dialogo.ShowDialog() != true) return;
        foreach (var caminho in dialogo.FileNames.Where(File.Exists)) if (!Itens.Any(x => string.Equals(x.Caminho, caminho, StringComparison.OrdinalIgnoreCase)) && Itens.Count < 30) Itens.Add(new(caminho));
        Selecionado ??= Itens.FirstOrDefault(); Salvar();
    }
    private void RemoverSelecionado() { if (Selecionado == null) return; var indice = Itens.IndexOf(Selecionado); Itens.Remove(Selecionado); Selecionado = Itens.ElementAtOrDefault(Math.Min(indice, Itens.Count - 1)); Salvar(); }
    private void AbrirSelecionado() { if (Selecionado == null || !File.Exists(Selecionado.Caminho)) { PreviewDescricao = "O arquivo foi removido ou movido."; return; } try { Process.Start(new ProcessStartInfo { FileName = Selecionado.Caminho, UseShellExecute = true }); } catch { PreviewDescricao = "O Windows não conseguiu abrir este arquivo."; } }
    private void Salvar() => _salvar(JsonSerializer.Serialize(Itens.Select(x => x.Caminho).ToList()));

    private async Task AtualizarPreviewAsync()
    {
        if (_disposed) return;
        _previewCts?.Cancel(); _previewCts?.Dispose(); _previewCts = new(); var token = _previewCts.Token;
        PreviewImagem = null; PreviewTexto = string.Empty;
        if (Selecionado == null) { PreviewTitulo = "Selecione um arquivo"; PreviewDescricao = string.Empty; return; }
        if (!File.Exists(Selecionado.Caminho)) { PreviewTitulo = Selecionado.Nome; PreviewDescricao = "Arquivo ausente"; return; }
        try
        {
            var provider = _providers.First(x => x.PodeAbrir(Selecionado.Caminho));
            var result = await provider.CriarAsync(Selecionado.Caminho, token);
            if (token.IsCancellationRequested) return;
            PreviewTitulo = result.Titulo; PreviewDescricao = result.Descricao; PreviewTexto = result.Texto ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(result.CaminhoImagem))
            {
                var imagem = new BitmapImage(); imagem.BeginInit(); imagem.CacheOption = BitmapCacheOption.OnLoad; imagem.DecodePixelWidth = 600; imagem.UriSource = new Uri(result.CaminhoImagem); imagem.EndInit(); imagem.Freeze(); PreviewImagem = imagem;
            }
        }
        catch (OperationCanceledException) { }
        catch { PreviewDescricao = "Não foi possível gerar a prévia com segurança."; }
    }
    public void DefinirAtividade(EstadoAtividade estado) { if (!estado.Visual) PainelAberto = false; }
    public void Dispose() { _disposed = true; _previewCts?.Cancel(); _previewCts?.Dispose(); }
}

public sealed class ArquivoEstanteViewModel
{
    public ArquivoEstanteViewModel(string caminho) => Caminho = caminho;
    public string Caminho { get; }
    public string Nome => Path.GetFileName(Caminho);
    public bool Existe => File.Exists(Caminho);
}
