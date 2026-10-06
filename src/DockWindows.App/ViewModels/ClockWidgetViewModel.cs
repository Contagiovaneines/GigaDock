using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class ClockWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => TimerAtivo;

    private static readonly CultureInfo CulturaBrasil = new("pt-BR");
    private readonly DispatcherTimer _timer;
    private string _horaFormatada = string.Empty;
    private string _horaComSegundos = string.Empty;
    private string _dataFormatada = string.Empty;
    private string _diaMesFormatado = string.Empty;
    private string _dataResumida = string.Empty;
    private DateTime _horario;
    public DateTime Horario { get => _horario; private set => SetProperty(ref _horario, value); }
    private bool _calendarioAberto;
    private bool _habilitado = true;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private DateTime _dataSelecionada = DateTime.Today;
    private DateTime _dataExibicao = DateTime.Today;

    public DateTime DataSelecionada
    {
        get => _dataSelecionada;
        set => SetProperty(ref _dataSelecionada, value);
    }

    public DateTime DataExibicao
    {
        get => _dataExibicao;
        set => SetProperty(ref _dataExibicao, value);
    }

    public ClockWidgetViewModel()
    {
        AtualizarHorario();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (_, _) => AtualizarVisual();

        AlternarCalendarioCommand = new RelayCommand(AlternarCalendario);
        FecharCalendarioCommand = new RelayCommand(() => CalendarioAberto = false);
        _temporizador.Concluido += () => { OnPropertyChanged(nameof(EstaExecutando)); TemporizadorConcluido?.Invoke(); };
        AcaoPrincipalCommand = new RelayCommand(() =>
        {
            if (_disposed) return;
            if (Estilo is not ("cronometro" or "temporizador")) { AlternarCalendario(); return; }
            if (Estilo == "temporizador") _temporizador.Alternar();
            else if (_cronometro.IsRunning) _cronometro.Stop(); else _cronometro.Start();
            OnPropertyChanged(nameof(EstaExecutando));
            AtualizarAgendamento();
            OnPropertyChanged(nameof(TempoControle));
        });
        ReiniciarControleCommand = new RelayCommand(() => { _cronometro.Reset(); _temporizador.Reiniciar(); OnPropertyChanged(nameof(EstaExecutando)); AtualizarAgendamento(); OnPropertyChanged(nameof(TempoControle)); });
    }

    private bool _visual, _background, _disposed;
    private readonly DockWindows.Core.Widgets.TemporizadorEngine _temporizador = new(TimeSpan.FromMinutes(5));
    public bool EstaExecutando => _temporizador.EstaExecutando;
    public event Action? TemporizadorConcluido;
    public TimeSpan IntervaloAtual => _timer.Interval;
    public bool TimerAtivo => _timer.IsEnabled;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    { _visual = estado.Visual && !_disposed; _background = estado.SegundoPlano && !_disposed; _temporizador.Tick(); AtualizarAgendamento(); if (!_visual) CalendarioAberto = false; }
    private void AtualizarVisual()
    { if (_disposed) return; _temporizador.Tick(); if (_visual) { AtualizarHorario(); OnPropertyChanged(nameof(TempoControle)); } AtualizarAgendamento(); }
    private void AtualizarAgendamento()
    {
        _timer.Stop();
        if (_disposed) return;
        if (!_visual)
        {
            if (_background && _temporizador.EstaExecutando) { _timer.Interval = TimeSpan.FromSeconds(Math.Max(.001, _temporizador.Restante.TotalSeconds)); _timer.Start(); }
            return;
        }
        AtualizarHorario();
        if (Estilo == "temporizador" && !_temporizador.EstaExecutando || Estilo == "cronometro" && !_cronometro.IsRunning)
        { if (_temporizador.EstaExecutando) { _timer.Interval = _temporizador.Restante > TimeSpan.Zero ? _temporizador.Restante : TimeSpan.FromMilliseconds(1); _timer.Start(); } return; }
        var agora = DateTime.Now;
        bool segundos = Estilo == "segundos" || Estilo.StartsWith("analogico-", StringComparison.Ordinal) ||
            (Estilo == "cronometro" && _cronometro.IsRunning || Estilo == "temporizador" && _temporizador.EstaExecutando);
        _timer.Interval = segundos ? TimeSpan.FromMilliseconds(1000 - agora.Millisecond) :
            Estilo is "data" or "dia" ? DateTime.Today.AddDays(1) - agora :
            TimeSpan.FromMilliseconds(60000 - (agora.Second * 1000 + agora.Millisecond));
        _timer.Start();
    }
    public void Dispose() { _disposed = true; _timer.Stop(); _cronometro.Stop(); _temporizador.Reiniciar(); TemporizadorConcluido = null; }

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
                OnPropertyChanged(nameof(HoraPrincipal));
            }
        }
    }

    private string _estilo = "hora-data";
    public string Estilo { get => _estilo; set { if (SetProperty(ref _estilo, value)) { CalendarioAberto = false; AtualizarAgendamento(); OnPropertyChanged(nameof(TempoControle)); } } }
    private readonly System.Diagnostics.Stopwatch _cronometro = new();
    public string TempoControle
    {
        get
        {
            var tempo = Estilo == "temporizador" ? _temporizador.Restante : _cronometro.Elapsed;
            if (tempo < TimeSpan.Zero) { tempo = TimeSpan.Zero; _cronometro.Stop(); }
            return $"{(int)tempo.TotalMinutes}:{tempo.Seconds:00} {((Estilo == "temporizador" ? _temporizador.EstaExecutando : _cronometro.IsRunning) ? "Ⅱ" : "▶")}";
        }
    }
    public ICommand AcaoPrincipalCommand { get; }
    public ICommand ReiniciarControleCommand { get; }

    public bool EhExpandido => Formato == FormatoWidget.Expandido;

    public string HoraFormatada
    {
        get => _horaFormatada;
        private set => SetProperty(ref _horaFormatada, value);
    }

    public string HoraComSegundos
    {
        get => _horaComSegundos;
        private set => SetProperty(ref _horaComSegundos, value);
    }

    public string HoraPrincipal => HoraFormatada;

    public string DataResumida
    {
        get => _dataResumida;
        private set => SetProperty(ref _dataResumida, value);
    }

    public string TextoExibicao => EhExpandido ? $"{HoraComSegundos} • {DiaMesFormatado}" : HoraFormatada;

    public string DataFormatada
    {
        get => _dataFormatada;
        private set => SetProperty(ref _dataFormatada, value);
    }

    public string DiaMesFormatado
    {
        get => _diaMesFormatado;
        private set => SetProperty(ref _diaMesFormatado, value);
    }

    public bool CalendarioAberto
    {
        get => _calendarioAberto;
        set => SetProperty(ref _calendarioAberto, value);
    }

    public ICommand AlternarCalendarioCommand { get; }
    public ICommand FecharCalendarioCommand { get; }

    private void AlternarCalendario()
    {
        CalendarioAberto = !CalendarioAberto;
    }

    private void AtualizarHorario()
    {
        var agora = DateTime.Now;
        Horario = agora;
        HoraFormatada = agora.ToString("HH:mm", CulturaBrasil);
        HoraComSegundos = agora.ToString("HH:mm:ss", CulturaBrasil);
        DiaMesFormatado = agora.ToString("dd MMM", CulturaBrasil);
        DataResumida = CulturaBrasil.TextInfo.ToTitleCase(agora.ToString("ddd, dd MMM", CulturaBrasil).Replace(".", ""));
        DataFormatada = agora.ToString("dddd, dd 'de' MMMM 'de' yyyy", CulturaBrasil);
        OnPropertyChanged(nameof(TextoExibicao));
    }
}

