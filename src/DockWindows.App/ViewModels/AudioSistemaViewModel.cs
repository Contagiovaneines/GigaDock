using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class AudioSistemaViewModel : ObservableObject, IAtividadeWidget
{
    private readonly IAudioSystemService _service;
    private readonly List<SessaoAudioViewModel> _todasSessoes = new();
    private readonly HashSet<string> _fixadas = new(StringComparer.Ordinal);
    private readonly HashSet<string> _ocultas = new(StringComparer.Ordinal);
    private bool _disposed, _habilitado, _painelAberto, _microfoneMudo, _volumeMestreMudo, _compacto;
    private float _volumeMestre;
    private int _paginaAtual, _aplicativosPorPagina = 5;
    private string _saidaAtual = "Saída padrão do Windows";

    public AudioSistemaViewModel(IAudioSystemService service)
    {
        _service = service;
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        AtualizarCommand = new RelayCommand(() => _ = AtualizarAsync());
        AlternarMudoMestreCommand = new RelayCommand(AlternarMudoMestre);
        AlternarMicrofoneCommand = new RelayCommand(() =>
        {
            if (_service.DefinirMicrofoneMudo(!MicrofoneMudo)) MicrofoneMudo = !MicrofoneMudo;
        });
        AlternarCompactoCommand = new RelayCommand(() => Compacto = !Compacto);
        PaginaAnteriorCommand = new RelayCommand(() => PaginaAtual--, () => PaginaAtual > 0);
        ProximaPaginaCommand = new RelayCommand(() => PaginaAtual++, () => PaginaAtual + 1 < TotalPaginas);
        AbrirSomCommand = new RelayCommand(() => AbrirConfiguracao("ms-settings:sound"));
        AbrirSomClassicoCommand = new RelayCommand(() =>
        {
            try { Process.Start(new ProcessStartInfo("control.exe", "mmsys.cpl") { UseShellExecute = true }); } catch { }
        });
    }

    public ObservableCollection<SessaoAudioViewModel> Sessoes { get; } = new();
    public bool? EmExecucao => false;
    public SaudeWidget Saude => string.IsNullOrEmpty(_service.Erro) ? SaudeWidget.Disponivel : SaudeWidget.Erro;
    public string? MotivoEstado => _service.Erro ?? $"{_todasSessoes.Count} sessões de áudio";
    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public bool PainelAberto { get => _painelAberto; set { if (SetProperty(ref _painelAberto, value) && value) _ = AtualizarAsync(); } }
    public bool MicrofoneMudo { get => _microfoneMudo; private set { if (SetProperty(ref _microfoneMudo, value)) OnPropertyChanged(nameof(TextoMicrofone)); } }
    public string TextoMicrofone => MicrofoneMudo ? "Ativar microfone" : "Silenciar microfone";
    public string SaidaAtual { get => _saidaAtual; private set => SetProperty(ref _saidaAtual, value); }
    public float VolumeMestre
    {
        get => _volumeMestre;
        set
        {
            var nivel = Math.Clamp(value, 0, 100);
            if (SetProperty(ref _volumeMestre, nivel))
            {
                _service.DefinirVolumeMestre(nivel / 100f);
                OnPropertyChanged(nameof(VolumeMestreTexto));
            }
        }
    }
    public string VolumeMestreTexto => $"{Math.Round(VolumeMestre)}%";
    public bool VolumeMestreMudo
    {
        get => _volumeMestreMudo;
        private set
        {
            if (SetProperty(ref _volumeMestreMudo, value))
            {
                OnPropertyChanged(nameof(IconeVolumeMestre));
                OnPropertyChanged(nameof(DicaVolumeMestre));
            }
        }
    }
    public string IconeVolumeMestre => VolumeMestreMudo ? "🔇" : VolumeMestre < 35 ? "🔈" : VolumeMestre < 70 ? "🔉" : "🔊";
    public string DicaVolumeMestre => VolumeMestreMudo ? "Ativar som principal" : "Silenciar som principal";
    public bool Compacto { get => _compacto; set { if (SetProperty(ref _compacto, value)) OnPropertyChanged(nameof(LarguraPainel)); } }
    public double LarguraPainel => Compacto ? 350 : 430;
    public int AplicativosPorPagina
    {
        get => _aplicativosPorPagina;
        set
        {
            if (SetProperty(ref _aplicativosPorPagina, Math.Clamp(value, 3, 12)))
            {
                PaginaAtual = 0;
                AtualizarPagina();
            }
        }
    }
    public int PaginaAtual { get => _paginaAtual; private set { if (SetProperty(ref _paginaAtual, Math.Clamp(value, 0, Math.Max(0, TotalPaginas - 1)))) AtualizarPagina(); } }
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(SessoesDisponiveis / (double)AplicativosPorPagina));
    public int SessoesDisponiveis => _todasSessoes.Count(x => !_ocultas.Contains(x.Id));
    public string TextoPagina => $"{PaginaAtual + 1} / {TotalPaginas}";

    public ICommand AlternarPainelCommand { get; }
    public ICommand AtualizarCommand { get; }
    public ICommand AlternarMudoMestreCommand { get; }
    public ICommand AlternarMicrofoneCommand { get; }
    public ICommand AlternarCompactoCommand { get; }
    public ICommand PaginaAnteriorCommand { get; }
    public ICommand ProximaPaginaCommand { get; }
    public ICommand AbrirSomCommand { get; }
    public ICommand AbrirSomClassicoCommand { get; }

    public async Task AtualizarAsync()
    {
        await _service.AtualizarAsync();
        if (_disposed) return;
        _todasSessoes.Clear();
        foreach (var info in _service.Sessoes)
        {
            var sessao = new SessaoAudioViewModel(info, _service, AlternarFixacao, Ocultar, CopiarNome);
            sessao.Fixada = _fixadas.Contains(sessao.Id);
            _todasSessoes.Add(sessao);
        }
        _volumeMestre = _service.VolumeMestre * 100;
        VolumeMestreMudo = _service.VolumeMestreMudo;
        MicrofoneMudo = _service.MicrofoneMudo;
        SaidaAtual = _service.Saidas.FirstOrDefault(x => x.Padrao)?.Nome ?? "Saída padrão do Windows";
        PaginaAtual = Math.Min(PaginaAtual, Math.Max(0, TotalPaginas - 1));
        AtualizarPagina();
        OnPropertyChanged(nameof(VolumeMestre));
        OnPropertyChanged(nameof(VolumeMestreTexto));
        OnPropertyChanged(nameof(IconeVolumeMestre));
        OnPropertyChanged(nameof(MotivoEstado));
    }

    public void AjustarVolumeMestre(int passos)
    {
        VolumeMestre = Math.Clamp(VolumeMestre + passos, 0, 100);
        if (VolumeMestreMudo && VolumeMestre > 0) AlternarMudoMestre();
    }

    public void AlternarMudoMestre()
    {
        if (_service.DefinirMudoMestre(!VolumeMestreMudo)) VolumeMestreMudo = !VolumeMestreMudo;
    }

    private void AlternarFixacao(SessaoAudioViewModel sessao)
    {
        if (!_fixadas.Add(sessao.Id)) _fixadas.Remove(sessao.Id);
        sessao.Fixada = _fixadas.Contains(sessao.Id);
        AtualizarPagina();
    }

    private void Ocultar(SessaoAudioViewModel sessao)
    {
        _ocultas.Add(sessao.Id);
        AtualizarPagina();
    }

    private static void CopiarNome(SessaoAudioViewModel sessao)
    {
        try { Clipboard.SetText(sessao.NomeProcesso); } catch { }
    }

    private void AtualizarPagina()
    {
        var itens = _todasSessoes
            .Where(x => !_ocultas.Contains(x.Id))
            .OrderByDescending(x => x.Fixada)
            .ThenBy(x => x.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Skip(PaginaAtual * AplicativosPorPagina)
            .Take(AplicativosPorPagina)
            .ToArray();
        Sessoes.Clear();
        foreach (var item in itens) Sessoes.Add(item);
        OnPropertyChanged(nameof(SessoesDisponiveis));
        OnPropertyChanged(nameof(TotalPaginas));
        OnPropertyChanged(nameof(TextoPagina));
        CommandManager.InvalidateRequerySuggested();
    }

    private static void AbrirConfiguracao(string destino)
    {
        try { Process.Start(new ProcessStartInfo { FileName = destino, UseShellExecute = true }); } catch { }
    }

    public void DefinirAtividade(EstadoAtividade estado) { if (!estado.Visual) PainelAberto = false; }
    public void Dispose() { _disposed = true; _service.Dispose(); Sessoes.Clear(); _todasSessoes.Clear(); }
}

public sealed class SessaoAudioViewModel : ObservableObject
{
    private readonly IAudioSystemService _service;
    private float _volume;
    private bool _mudo, _fixada;

    public SessaoAudioViewModel(SessaoAudioInfo info, IAudioSystemService service, Action<SessaoAudioViewModel> fixar, Action<SessaoAudioViewModel> ocultar, Action<SessaoAudioViewModel> copiar)
    {
        Id = info.Id;
        ProcessId = info.ProcessId;
        NomeProcesso = info.ProcessId == 0 ? "Sons do sistema" : info.Nome;
        Nome = NomeProcesso;
        _volume = info.Volume * 100;
        _mudo = info.Mudo;
        _service = service;
        AlternarMudoCommand = new RelayCommand(AlternarMudo);
        AlternarFixacaoCommand = new RelayCommand(() => fixar(this));
        OcultarCommand = new RelayCommand(() => ocultar(this));
        CopiarNomeCommand = new RelayCommand(() => copiar(this));
    }

    public string Id { get; }
    public uint ProcessId { get; }
    public string NomeProcesso { get; }
    public string Nome { get; set; }
    public string DicaNome => Nome == NomeProcesso ? Nome : $"{Nome} ({NomeProcesso})";
    public float Volume
    {
        get => _volume;
        set
        {
            var nivel = Math.Clamp(value, 0, 100);
            if (SetProperty(ref _volume, nivel))
            {
                _service.DefinirVolumeSessao(Id, nivel / 100f);
                OnPropertyChanged(nameof(VolumeTexto));
                OnPropertyChanged(nameof(IconeVolume));
            }
        }
    }
    public string VolumeTexto => $"{Math.Round(Volume)}%";
    public bool Mudo { get => _mudo; private set { if (SetProperty(ref _mudo, value)) OnPropertyChanged(nameof(IconeVolume)); } }
    public string IconeVolume => Mudo ? "🔇" : "🔊";
    public bool Fixada { get => _fixada; set { if (SetProperty(ref _fixada, value)) OnPropertyChanged(nameof(TextoFixacao)); } }
    public string TextoFixacao => Fixada ? "Desafixar do topo" : "Fixar no topo";
    public ICommand AlternarMudoCommand { get; }
    public ICommand AlternarFixacaoCommand { get; }
    public ICommand OcultarCommand { get; }
    public ICommand CopiarNomeCommand { get; }

    public void AlternarMudo()
    {
        if (_service.DefinirMudoSessao(Id, !Mudo)) Mudo = !Mudo;
    }

    public void AjustarVolume(int passos)
    {
        Volume = Math.Clamp(Volume + passos, 0, 100);
        if (Mudo && Volume > 0) AlternarMudo();
    }
}
