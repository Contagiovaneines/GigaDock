using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class ClockWidgetViewModel : ObservableObject
{
    private static readonly CultureInfo CulturaBrasil = new("pt-BR");
    private readonly DispatcherTimer _timer;
    private string _horaFormatada = string.Empty;
    private string _horaComSegundos = string.Empty;
    private string _dataFormatada = string.Empty;
    private string _diaMesFormatado = string.Empty;
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
        _timer.Tick += (s, e) => AtualizarHorario();
        _timer.Start();

        AlternarCalendarioCommand = new RelayCommand(AlternarCalendario);
        FecharCalendarioCommand = new RelayCommand(() => CalendarioAberto = false);
    }

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
        HoraFormatada = agora.ToString("HH:mm", CulturaBrasil);
        HoraComSegundos = agora.ToString("HH:mm:ss", CulturaBrasil);
        DiaMesFormatado = agora.ToString("dd MMM", CulturaBrasil);
        DataFormatada = agora.ToString("dddd, dd 'de' MMMM 'de' yyyy", CulturaBrasil);
        OnPropertyChanged(nameof(TextoExibicao));
    }
}
