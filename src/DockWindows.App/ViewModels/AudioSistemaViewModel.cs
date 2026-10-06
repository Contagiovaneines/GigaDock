using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Services;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class AudioSistemaViewModel : ObservableObject, IAtividadeWidget
{
    private readonly IAudioSystemService _service;
    private bool _disposed, _habilitado, _painelAberto, _microfoneMudo;
    public AudioSistemaViewModel(IAudioSystemService service)
    {
        _service = service;
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        AtualizarCommand = new RelayCommand(() => _ = AtualizarAsync());
        AlternarMicrofoneCommand = new RelayCommand(() => { if (_service.DefinirMicrofoneMudo(!MicrofoneMudo)) MicrofoneMudo = !MicrofoneMudo; });
        AbrirSomCommand = new RelayCommand(() => { try { Process.Start(new ProcessStartInfo { FileName = "ms-settings:sound", UseShellExecute = true }); } catch { } });
    }
    public ObservableCollection<SessaoAudioViewModel> Sessoes { get; } = new();
    public bool? EmExecucao => false;
    public SaudeWidget Saude => string.IsNullOrEmpty(_service.Erro) ? SaudeWidget.Disponivel : SaudeWidget.Erro;
    public string? MotivoEstado => _service.Erro ?? $"{Sessoes.Count} sessões de áudio";
    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public bool PainelAberto { get => _painelAberto; set { if (SetProperty(ref _painelAberto, value) && value) _ = AtualizarAsync(); } }
    public bool MicrofoneMudo { get => _microfoneMudo; private set { if (SetProperty(ref _microfoneMudo, value)) OnPropertyChanged(nameof(TextoMicrofone)); } }
    public string TextoMicrofone => MicrofoneMudo ? "Microfone mudo" : "Microfone ativo";
    public ICommand AlternarPainelCommand { get; }
    public ICommand AtualizarCommand { get; }
    public ICommand AlternarMicrofoneCommand { get; }
    public ICommand AbrirSomCommand { get; }
    public async Task AtualizarAsync()
    {
        await _service.AtualizarAsync();
        if (_disposed) return;
        Sessoes.Clear();
        foreach (var sessao in _service.Sessoes.OrderBy(x => x.Nome)) Sessoes.Add(new(sessao, _service));
        MicrofoneMudo = _service.MicrofoneMudo;
        OnPropertyChanged(nameof(TextoMicrofone)); OnPropertyChanged(nameof(MotivoEstado));
    }
    public void DefinirAtividade(EstadoAtividade estado) { if (!estado.Visual) PainelAberto = false; }
    public void Dispose() { _disposed = true; _service.Dispose(); Sessoes.Clear(); }
}

public sealed class SessaoAudioViewModel : ObservableObject
{
    private readonly IAudioSystemService _service;
    private float _volume;
    private bool _mudo;
    public SessaoAudioViewModel(DockWindows.Core.Models.SessaoAudioInfo info, IAudioSystemService service)
    {
        Id = info.Id; Nome = info.Nome; _volume = info.Volume * 100; _mudo = info.Mudo; _service = service;
        AlternarMudoCommand = new RelayCommand(() => { if (_service.DefinirMudoSessao(Id, !Mudo)) Mudo = !Mudo; });
    }
    public string Id { get; }
    public string Nome { get; }
    public float Volume { get => _volume; set { var v = Math.Clamp(value, 0, 100); if (SetProperty(ref _volume, v)) _service.DefinirVolumeSessao(Id, v / 100f); } }
    public bool Mudo { get => _mudo; private set => SetProperty(ref _mudo, value); }
    public ICommand AlternarMudoCommand { get; }
}
