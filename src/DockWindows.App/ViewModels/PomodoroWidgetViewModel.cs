using System.Media;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public class PomodoroWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => TimerAtivo;

    private readonly PomodoroEngine _engine;
    private readonly DispatcherTimer _timer;
    private bool _painelAberto;
    private bool _habilitado = true;

    public PomodoroWidgetViewModel(WidgetConfig? config = null)
    {
        _engine = new PomodoroEngine(config);
        _engine.CicloConcluido += (_, _) => { TocarAlerta(); OnPropertyChanged(nameof(EstaExecutando)); };
        _engine.EstadoMudou += (_, _) => { if (_visual) NotificarMudancas(); };

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) =>
        {
            _engine.Tick();
            if (_visual) NotificarMudancas();
            AtualizarTimer();
        };

        IniciarPausarCommand = new RelayCommand(AlternarExecucao);
        ReiniciarCommand = new RelayCommand(Reiniciar);
        PularFaseCommand = new RelayCommand(AvancarFase);
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        FecharPainelCommand = new RelayCommand(() => PainelAberto = false);

        if (config != null)
        {
            Habilitado = config.PomodoroHabilitado;
        }
    }

    private bool _visual, _background, _disposed;
    public bool TimerAtivo => _timer.IsEnabled;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        if (_disposed) return;
        _visual = estado.Visual; _background = estado.SegundoPlano;
        _engine.Tick();
        if (_visual) NotificarMudancas(); else PainelAberto = false;
        AtualizarTimer();
    }
    private void AtualizarTimer()
    {
        _timer.Stop();
        if (_disposed || !_engine.EstaExecutando) return;
        // Oculto: apenas a conclusão precisa despertar o dispatcher.
        _timer.Interval = _visual ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(Math.Max(1, _engine.SegundosRestantes));
        if (_visual || _background) _timer.Start();
    }
    public void Dispose() { _disposed = true; _visual = _background = false; _timer.Stop(); _engine.Pausar(); }

    private FormatoWidget _formato = FormatoWidget.Compacto;

    public bool Habilitado
    {
        get => _habilitado;
        set => SetProperty(ref _habilitado, value);
    }

    public FormatoWidget Formato
    {
        get => _formato;
        set
        {
            if (SetProperty(ref _formato, value))
            {
                OnPropertyChanged(nameof(EhExpandido));
                OnPropertyChanged(nameof(TextoExibicao));
            }
        }
    }

    public bool EhExpandido => Formato == FormatoWidget.Expandido;

    public PomodoroEstado Estado => _engine.Estado;
    public string EstadoTexto => _engine.EstadoTexto;
    public string CorEstado => _engine.CorEstado;
    public string TempoFormatado => _engine.TempoFormatado;
    public string TextoExibicao => EhExpandido ? $"{TempoFormatado} • {EstadoTexto}" : TempoFormatado;
    public double Progresso => _engine.Progresso;
    public int CiclosConcluidos => _engine.CiclosConcluidos;
    public bool EstaExecutando => _engine.EstaExecutando;
    public string TextoBotaoExecutar => EstaExecutando ? "⏸" : "▶";

    public bool PainelAberto
    {
        get => _painelAberto;
        set => SetProperty(ref _painelAberto, value);
    }

    public ICommand IniciarPausarCommand { get; }
    public ICommand ReiniciarCommand { get; }
    public ICommand PularFaseCommand { get; }
    public ICommand AlternarPainelCommand { get; }
    public ICommand FecharPainelCommand { get; }

    public void CarregarConfiguracao(WidgetConfig config)
    {
        _engine.AtualizarConfiguracao(config);
        Habilitado = config.PomodoroHabilitado;
        NotificarMudancas();
    }

    private void AlternarExecucao()
    {
        if (_disposed) return;
        _engine.Alternar();
        NotificarMudancas();
        AtualizarTimer();
    }

    private void Reiniciar()
    {
        if (_disposed) return;
        _engine.Reiniciar();
        AtualizarTimer();
        NotificarMudancas();
    }

    private void AvancarFase()
    {
        if (_disposed) return;
        _engine.AvancarFase();
        AtualizarTimer();
        NotificarMudancas();
    }

    private void NotificarMudancas()
    {
        OnPropertyChanged(nameof(Estado));
        OnPropertyChanged(nameof(EstadoTexto));
        OnPropertyChanged(nameof(CorEstado));
        OnPropertyChanged(nameof(TempoFormatado));
        OnPropertyChanged(nameof(Progresso));
        OnPropertyChanged(nameof(CiclosConcluidos));
        OnPropertyChanged(nameof(EstaExecutando));
        OnPropertyChanged(nameof(TextoBotaoExecutar));
    }

    private static void TocarAlerta()
    {
        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch { }
    }
}
