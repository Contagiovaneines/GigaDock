using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class ArquivosRecentesViewModel : ObservableObject, IAtividadeWidget
{
    private readonly DispatcherTimer _debounce;
    private readonly List<FileSystemWatcher> _watchers = new();
    private bool _disposed;
    private bool _habilitado;
    private bool _painelAberto;
    private string _estado = "Nenhum arquivo recente";

    public ArquivosRecentesViewModel()
    {
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Recarregar(); };
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        AtualizarCommand = new RelayCommand(Recarregar);
        CapturarCommand = new RelayCommand(AbrirCapturaWindows);
    }

    public ObservableCollection<ArquivoRecenteViewModel> Itens { get; } = new();
    public bool? EmExecucao => _watchers.Count > 0;
    public SaudeWidget Saude => SaudeWidget.Disponivel;
    public string? MotivoEstado => Estado;
    public bool Habilitado { get => _habilitado; set { if (SetProperty(ref _habilitado, value)) AtualizarWatchers(); } }
    public bool PainelAberto { get => _painelAberto; set { if (SetProperty(ref _painelAberto, value) && value) Recarregar(); } }
    public string Estado { get => _estado; private set => SetProperty(ref _estado, value); }
    public ICommand AlternarPainelCommand { get; }
    public ICommand AtualizarCommand { get; }
    public ICommand CapturarCommand { get; }

    public void DefinirAtividade(EstadoAtividade estado)
    {
        if (_disposed) return;
        if (!estado.Visual) PainelAberto = false;
        AtualizarWatchers();
    }

    private IEnumerable<string> PastasConhecidas()
    {
        var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var imagens = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        yield return Path.Combine(perfil, "Downloads");
        if (!string.IsNullOrWhiteSpace(imagens))
        {
            yield return imagens;
            yield return Path.Combine(imagens, "Screenshots");
            yield return Path.Combine(imagens, "Capturas de Tela");
        }
    }

    private void AtualizarWatchers()
    {
        EncerrarWatchers();
        if (_disposed || !Habilitado) return;
        foreach (var pasta in PastasConhecidas().Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
        {
            try
            {
                var watcher = new FileSystemWatcher(pasta) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite, IncludeSubdirectories = false, EnableRaisingEvents = true };
                watcher.Created += ArquivoAlterado;
                watcher.Changed += ArquivoAlterado;
                watcher.Renamed += ArquivoAlterado;
                watcher.Deleted += ArquivoAlterado;
                _watchers.Add(watcher);
            }
            catch { }
        }
        Recarregar();
    }

    private void ArquivoAlterado(object sender, FileSystemEventArgs e) => System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
    {
        if (_disposed) return;
        _debounce.Stop();
        _debounce.Start();
    });

    private void Recarregar()
    {
        if (_disposed || !Habilitado) return;
        var arquivos = new List<FileInfo>();
        foreach (var pasta in PastasConhecidas().Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
        {
            try { arquivos.AddRange(new DirectoryInfo(pasta).EnumerateFiles().OrderByDescending(f => f.LastWriteTimeUtc).Take(25)); } catch { }
        }
        var recentes = arquivos.GroupBy(f => f.FullName, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
            .OrderByDescending(f => f.LastWriteTimeUtc).Take(20).ToList();
        Itens.Clear();
        foreach (var arquivo in recentes) Itens.Add(new ArquivoRecenteViewModel(arquivo.FullName, AbrirArquivo));
        Estado = Itens.Count == 0 ? "Nenhum arquivo recente" : $"{Itens.Count} arquivos recentes";
    }

    private void AbrirArquivo(ArquivoRecenteViewModel item)
    {
        if (!File.Exists(item.Caminho)) { Estado = "O arquivo foi removido ou movido."; Recarregar(); return; }
        try { Process.Start(new ProcessStartInfo { FileName = item.Caminho, UseShellExecute = true }); }
        catch { Estado = "O Windows não conseguiu abrir este arquivo."; }
    }

    private void AbrirCapturaWindows()
    {
        try { Process.Start(new ProcessStartInfo { FileName = "ms-screenclip:", UseShellExecute = true }); }
        catch { Estado = "A ferramenta de captura do Windows não está disponível."; }
    }

    private void EncerrarWatchers()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= ArquivoAlterado; watcher.Changed -= ArquivoAlterado;
            watcher.Renamed -= ArquivoAlterado; watcher.Deleted -= ArquivoAlterado;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

    public void Dispose() { if (_disposed) return; _disposed = true; _debounce.Stop(); EncerrarWatchers(); }
}

public sealed class ArquivoRecenteViewModel
{
    public ArquivoRecenteViewModel(string caminho, Action<ArquivoRecenteViewModel> abrir)
    {
        Caminho = caminho;
        AbrirCommand = new RelayCommand(() => abrir(this));
    }
    public string Caminho { get; }
    public string Nome => Path.GetFileName(Caminho);
    public string Pasta => Path.GetFileName(Path.GetDirectoryName(Caminho)) ?? string.Empty;
    public ICommand AbrirCommand { get; }
}
