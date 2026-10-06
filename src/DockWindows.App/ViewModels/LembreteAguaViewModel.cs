using System.Media;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class LembreteAguaViewModel : ObservableObject, IAtividadeWidget
{
    private readonly DispatcherTimer _timer;
    private bool _disposed;
    private bool _visual;
    private bool _habilitado;
    private bool _alertaAtivo;
    private DateTime _proximoLembrete;
    private int _intervaloMinutos = 60;
    private int _horaInicio = 8;
    private int _horaFim = 22;
    private int _coposHoje;

    public LembreteAguaViewModel()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => VerificarLembrete();
        BeberAguaCommand = new RelayCommand(RegistrarCopo);
        AdiarCommand = new RelayCommand(() => Agendar(DateTime.Now.AddMinutes(10)));
    }

    public bool? EmExecucao => _timer.IsEnabled;
    public SaudeWidget Saude => SaudeWidget.Disponivel;
    public string? MotivoEstado => Habilitado ? $"Próximo lembrete: {TextoProximo}" : "Lembrete desativado";
    public bool Habilitado { get => _habilitado; set { if (SetProperty(ref _habilitado, value)) AtualizarAgendamento(); } }
    public bool AlertaAtivo { get => _alertaAtivo; private set => SetProperty(ref _alertaAtivo, value); }
    public int CoposHoje { get => _coposHoje; private set { if (SetProperty(ref _coposHoje, value)) OnPropertyChanged(nameof(TextoResumo)); } }
    public int IntervaloMinutos { get => _intervaloMinutos; private set => _intervaloMinutos = Math.Clamp(value, 15, 240); }
    public string TextoResumo => AlertaAtivo ? "Hora de beber água" : $"Água · {CoposHoje} hoje";
    public string TextoProximo => _proximoLembrete == default ? "—" : _proximoLembrete.ToString("HH:mm");
    public ICommand BeberAguaCommand { get; }
    public ICommand AdiarCommand { get; }

    public void Carregar(int intervaloMinutos, int horaInicio, int horaFim)
    {
        IntervaloMinutos = intervaloMinutos;
        _horaInicio = Math.Clamp(horaInicio, 0, 23);
        _horaFim = Math.Clamp(horaFim, 1, 24);
        AtualizarAgendamento();
    }

    public void DefinirAtividade(EstadoAtividade estado)
    {
        if (_disposed) return;
        _visual = estado.Visual;
        AtualizarAgendamento();
    }

    private void RegistrarCopo()
    {
        if (_disposed) return;
        CoposHoje++;
        AlertaAtivo = false;
        Agendar(DateTime.Now.AddMinutes(IntervaloMinutos));
    }

    private void VerificarLembrete()
    {
        _timer.Stop();
        if (_disposed || !Habilitado) return;
        var agora = DateTime.Now;
        if (agora >= _proximoLembrete && agora.Hour >= _horaInicio && agora.Hour < _horaFim)
        {
            AlertaAtivo = true;
            if (_visual) { try { SystemSounds.Asterisk.Play(); } catch { } }
            OnPropertyChanged(nameof(TextoResumo));
        }
        Agendar(agora.AddMinutes(IntervaloMinutos));
    }

    private void AtualizarAgendamento()
    {
        _timer.Stop();
        if (_disposed || !Habilitado) return;
        if (_proximoLembrete <= DateTime.Now) _proximoLembrete = DateTime.Now.AddMinutes(IntervaloMinutos);
        Agendar(_proximoLembrete);
    }

    private void Agendar(DateTime quando)
    {
        _timer.Stop();
        if (_disposed || !Habilitado) return;
        _proximoLembrete = quando;
        _timer.Interval = quando <= DateTime.Now ? TimeSpan.FromMilliseconds(50) : quando - DateTime.Now;
        _timer.Start();
        AlertaAtivo = false;
        OnPropertyChanged(nameof(TextoProximo));
        OnPropertyChanged(nameof(TextoResumo));
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Stop();
    }
}
