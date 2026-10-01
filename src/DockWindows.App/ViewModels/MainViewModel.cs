using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ISettingsRepository _repository;
    private readonly ILauncherService _launcher;
    private readonly IIconExtractionService _iconService;
    private readonly IAutostartService _autostart;
    private readonly ITaskbarService _taskbarService;
    private readonly IWindowTrackingService _windowTrackingService;
    private readonly IWinKeyHookService _winKeyHookService;

    private Preferencias _preferencias;
    private EnvironmentViewModel? _ambienteAtivo;
        private bool _dockVisivel = true;
    private bool _ocultoPorTelaCheia = false;

    public bool OcultoPorTelaCheia
    {
        get => _ocultoPorTelaCheia;
        set => SetProperty(ref _ocultoPorTelaCheia, value);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    public MainViewModel(
        ISettingsRepository repository,
        ILauncherService launcher,
        IIconExtractionService iconService,
        IAutostartService autostart,
        ITaskbarService? taskbarService = null,
        IWindowTrackingService? windowTrackingService = null,
        IWinKeyHookService? winKeyHookService = null)
    {
        _repository = repository;
        _launcher = launcher;
        _iconService = iconService;
        _autostart = autostart;
        _taskbarService = taskbarService ?? new Win32TaskbarService();
        _windowTrackingService = windowTrackingService ?? new Win32WindowTrackingService();
        _winKeyHookService = winKeyHookService ?? new WinKeyHookService();

        _preferencias = _repository.Carregar();

        // Garante que as novas seÃ§Ãµes de mÃ­dia e clima existam (para usuÃ¡rios de versÃµes antigas)
        if (_preferencias.OrdemSecoes != null)
        {
            if (!_preferencias.OrdemSecoes.Any(s => s.Tipo == TipoSecaoDock.ClimaInline))
            {
                _preferencias.OrdemSecoes.Insert(1, new ConfigSecaoDock { Tipo = TipoSecaoDock.ClimaInline, Nome = "Clima Inline", Visivel = true, Ordem = 1 });
            }
            if (!_preferencias.OrdemSecoes.Any(s => s.Tipo == TipoSecaoDock.MidiaInline))
            {
                _preferencias.OrdemSecoes.Insert(2, new ConfigSecaoDock { Tipo = TipoSecaoDock.MidiaInline, Nome = "MÃ­dia Inline", Visivel = true, Ordem = 2 });
            }
            for (int i = 0; i < _preferencias.OrdemSecoes.Count; i++)
            {
                _preferencias.OrdemSecoes[i].Ordem = i;
            }
        }

        Ambientes = new ObservableCollection<EnvironmentViewModel>();
        Aplicativos = new ObservableCollection<AppItemViewModel>();
        ColecoesGlobais = new ObservableCollection<ColecaoAppViewModel>();
        TodasColecoesAtivas = new ObservableCollection<ColecaoAppViewModel>();
        Espacadores = new ObservableCollection<EspacadorConfig>();
        OrdemSecoes = new ObservableCollection<ConfigSecaoDock>();
        Clock = new ClockWidgetViewModel();
        Pomodoro = new PomodoroWidgetViewModel();
        Calendario = new CalendarioWidgetViewModel(onAbrirAjustes: () => AbrirAjustes("Widgets"));
        Midia = new MidiaWidgetViewModel();
        Notas = new NotasWidgetViewModel();
        MonitorSistema = new MonitorSistemaViewModel();
        Clima = new ClimaWidgetViewModel();

        TrocarAmbienteCommand = new RelayCommand<EnvironmentViewModel>(TrocarAmbiente);
        NovoAmbienteCommand = new RelayCommand(NovoAmbiente);
        RenomearAmbienteCommand = new RelayCommand<EnvironmentViewModel>(RenomearAmbiente);
        ExcluirAmbienteCommand = new RelayCommand<EnvironmentViewModel>(ExcluirAmbiente);

        AdicionarItemCommand = new RelayCommand(AdicionarItem);
        AdicionarAppPermanenteCommand = new RelayCommand(AdicionarAppPermanentePrompt);
        AbrirConfiguracoesCommand = new RelayCommand(() => AbrirAjustes("Geral"));
        AbrirPersonalizarCommand = new RelayCommand(() => AbrirAjustes("Aparencia"));
        AbrirAjustesCommand = new RelayCommand<string>(AbrirAjustes);
        RestaurarBarraWindowsCommand = new RelayCommand(RestaurarBarraWindows);
        AbrirMenuIniciarCommand = new RelayCommand(AbrirMenuIniciar);
        AbrirIniciarNativoWindowsCommand = new RelayCommand(AbrirIniciarNativoWindows);
        AbrirPesquisaCommand = new RelayCommand(AbrirPesquisa);
        AbrirExploradorCommand = new RelayCommand(AbrirExplorador);
        AlternarVisibilidadeCommand = new RelayCommand(AlternarVisibilidade);
        SairCommand = new RelayCommand(() => SolicitarFechamento?.Invoke());

        _winKeyHookService.WinKeyTapped += () =>
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                if (!DockVisivel)
                {
                    DockVisivel = true;
                }
                AtivarJanelaPrincipal?.Invoke();
                AbrirMenuIniciar();
            });
        };

        if (_preferencias.UsarComoBarraPrincipal && Application.Current != null)
        {
            _winKeyHookService.Iniciar();
        }

        _windowTrackingService.JanelasAlteradas += () =>
        {
            Application.Current?.Dispatcher?.InvokeAsync(AtualizarAplicativosAbertos);
        };
                _windowTrackingService.JanelaAtivada += hwnd =>
        {
            Application.Current?.Dispatcher?.InvokeAsync(AtualizarAplicativosAbertos);
        };
        _windowTrackingService.TelaCheiaAlterada += (ehTelaCheia) =>
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                OcultoPorTelaCheia = ehTelaCheia;
            });
        };

        CarregarDados();
        _windowTrackingService.Iniciar();
    }

    public Preferencias Preferencias => _preferencias;
    public ObservableCollection<EnvironmentViewModel> Ambientes { get; }
    public ObservableCollection<AppItemViewModel> Aplicativos { get; }
    public ObservableCollection<ColecaoAppViewModel> ColecoesGlobais { get; }
    public ObservableCollection<ColecaoAppViewModel> TodasColecoesAtivas { get; }
    public ObservableCollection<EspacadorConfig> Espacadores { get; }
    public ObservableCollection<ConfigSecaoDock> OrdemSecoes { get; }
    public ClockWidgetViewModel Clock { get; }
    public PomodoroWidgetViewModel Pomodoro { get; }
    public CalendarioWidgetViewModel Calendario { get; }
    public MidiaWidgetViewModel Midia { get; }
    public NotasWidgetViewModel Notas { get; }
    public MonitorSistemaViewModel MonitorSistema { get; }
    public ClimaWidgetViewModel Clima { get; }
    public ITaskbarService TaskbarService => _taskbarService;
    public IWindowTrackingService WindowTrackingService => _windowTrackingService;
    public IWinKeyHookService WinKeyHookService => _winKeyHookService;

    public EnvironmentViewModel? AmbienteAtivo
    {
        get => _ambienteAtivo;
        set
        {
            if (SetProperty(ref _ambienteAtivo, value))
            {
                if (value != null)
                {
                    _preferencias.AmbienteAtivoId = value.Id;
                    foreach (var a in Ambientes)
                    {
                        a.EstaAtivo = a.Id == value.Id;
                    }
                    Pomodoro.CarregarConfiguracao(value.Widgets);
                    Clock.Habilitado = value.Widgets.RelogioHabilitado;
                    SincronizarWidgetsAmbiente(value);
                    RecarregarTodasColecoes();
                    SalvarPreferencias();
                    OnPropertyChanged(nameof(WidgetsHabilitados));
                }
            }
        }
    }

    public bool DockVisivel
    {
        get => _dockVisivel;
        set => SetProperty(ref _dockVisivel, value);
    }

    // Temas e AparÃªncia V1.3
    public EstiloTema EstiloTema
    {
        get => _preferencias.EstiloTema;
        set
        {
            if (_preferencias.EstiloTema != value)
            {
                _preferencias.EstiloTema = value;
                var def = TemaDefinicao.ObterPorEstilo(value);
                _preferencias.RaioCantosDock = def.RaioCantos;
                _preferencias.OpacidadeDock = def.OpacidadePadrao;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RaioCantosDock));
                OnPropertyChanged(nameof(RaioCantosDockRadius));
                OnPropertyChanged(nameof(OpacidadeDock));
                AtualizarCoresTema();
                SalvarPreferencias();
            }
        }
    }

    public double RaioCantosDock
    {
        get => _preferencias.RaioCantosDock;
        set
        {
            if (Math.Abs(_preferencias.RaioCantosDock - value) > 0.1)
            {
                _preferencias.RaioCantosDock = Math.Clamp(value, 8.0, 100.0);
                OnPropertyChanged();
                OnPropertyChanged(nameof(RaioCantosDockRadius));
                SalvarPreferencias();
            }
        }
    }

    public CornerRadius RaioCantosDockRadius => new(24);

    public bool EfeitoDesfoque
    {
        get => _preferencias.EfeitoDesfoque;
        set
        {
            if (_preferencias.EfeitoDesfoque != value)
            {
                _preferencias.EfeitoDesfoque = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public string VelocidadeAnimacao
    {
        get => _preferencias.VelocidadeAnimacao;
        set
        {
            if (_preferencias.VelocidadeAnimacao != value)
            {
                _preferencias.VelocidadeAnimacao = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public TimeSpan ObterDuracaoTransicao()
    {
        if (DesativarAnimacoes || !SystemParameters.ClientAreaAnimation)
            return TimeSpan.Zero;

        return VelocidadeAnimacao switch
        {
            "Rapida" => TimeSpan.FromMilliseconds(60),
            "Lenta" => TimeSpan.FromMilliseconds(220),
            _ => TimeSpan.FromMilliseconds(110)
        };
    }

    public TemaDefinicao TemaAtual => TemaDefinicao.ObterPorEstilo(EstiloTema);

    public TemaModo Tema
    {
        get => _preferencias.Tema;
        set
        {
            if (_preferencias.Tema != value)
            {
                _preferencias.Tema = value;
                OnPropertyChanged();
                AtualizarCoresTema();
                SalvarPreferencias();
            }
        }
    }

    public bool EhTemaEscuro => true;

    public string FundoDockColor => TemaAtual.FundoDockColor;
    public string BordaDockColor => TemaAtual.BordaDockColor;
    public string TextoPrincipalColor => TemaAtual.TextoPrincipalColor;
    public string TextoSecundarioColor => TemaAtual.TextoSecundarioColor;
    public string FundoCardColor => TemaAtual.FundoCardColor;
    public string HighlightColor => TemaAtual.HighlightColor;
    public string HoverItemColor => EhVidroLiquido ? "#35FFFFFF" : "#25FFFFFF";
    public string SeparadorColor => EstiloTema == EstiloTema.Colorido 
        ? "#40A855F7" 
        : (EstiloTema == EstiloTema.ComBrilho ? "#60FFFFFF" : (EstiloTema == EstiloTema.VidroLiquido ? "#75FFFFFF" : "#28FFFFFF"));

    public bool EhVidroLiquido => EstiloTema == EstiloTema.VidroLiquido;
    public bool TemEfeitoVidro => EstiloTema == EstiloTema.VidroLiquido || EstiloTema == EstiloTema.ComBrilho;

    public bool WidgetsHabilitados => Clock.Habilitado || Pomodoro.Habilitado || Calendario.Habilitado || Notas.Habilitado || MonitorSistema.Habilitado;

    public void AtualizarCoresTema()
    {
        OnPropertyChanged(nameof(TemaAtual));
        OnPropertyChanged(nameof(FundoDockColor));
        OnPropertyChanged(nameof(BordaDockColor));
        OnPropertyChanged(nameof(TextoPrincipalColor));
        OnPropertyChanged(nameof(TextoSecundarioColor));
        OnPropertyChanged(nameof(FundoCardColor));
        OnPropertyChanged(nameof(HighlightColor));
        OnPropertyChanged(nameof(HoverItemColor));
        OnPropertyChanged(nameof(SeparadorColor));
        OnPropertyChanged(nameof(EhVidroLiquido));
        OnPropertyChanged(nameof(TemEfeitoVidro));
        OnPropertyChanged(nameof(WidgetsHabilitados));
    }

    public TamanhoIcone TamanhoIcones
    {
        get => _preferencias.TamanhoIcones;
        set
        {
            if (_preferencias.TamanhoIcones != value)
            {
                _preferencias.TamanhoIcones = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TamanhoIconeNumerico));
                OnPropertyChanged(nameof(AlturaBarra));
                SalvarPreferencias();
            }
        }
    }

    public double TamanhoIconeNumerico => (double)TamanhoIcones;

    public double AlturaBarra => TamanhoIcones switch
    {
        TamanhoIcone.Pequeno => 46.0,
        TamanhoIcone.Medio => 52.0,
        TamanhoIcone.Grande => 60.0,
        _ => 52.0
    };

    public double OpacidadeDock
    {
        get => _preferencias.OpacidadeDock;
        set
        {
            if (Math.Abs(_preferencias.OpacidadeDock - value) > 0.01)
            {
                _preferencias.OpacidadeDock = Math.Clamp(value, 0.5, 1.0);
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool SempreNoTopo
    {
        get => _preferencias.SempreNoTopo;
        set
        {
            if (_preferencias.SempreNoTopo != value)
            {
                _preferencias.SempreNoTopo = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool OcultarAutomaticamente
    {
        get => _preferencias.OcultarAutomaticamente;
        set
        {
            if (_preferencias.OcultarAutomaticamente != value)
            {
                _preferencias.OcultarAutomaticamente = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool UsarComoBarraPrincipal
    {
        get => _preferencias.UsarComoBarraPrincipal;
        set
        {
            if (_preferencias.UsarComoBarraPrincipal != value)
            {
                _preferencias.UsarComoBarraPrincipal = value;
                if (value)
                {
                    _taskbarService.OcultarBarraNativa(out var estadoAnt);
                    _preferencias.EstadoAnteriorBarraTarefas = estadoAnt;
                    _winKeyHookService.Iniciar();
                }
                else
                {
                    _winKeyHookService.Parar();
                    _taskbarService.RestaurarBarraNativa(_preferencias.EstadoAnteriorBarraTarefas);
                }
                OnPropertyChanged();
                SalvarPreferencias();
                NotificarReposicionamento?.Invoke();
            }
        }
    }

    public bool ExibirSeletorAmbientes
    {
        get => _preferencias.ExibirSeletorAmbientes;
        set
        {
            if (_preferencias.ExibirSeletorAmbientes != value)
            {
                _preferencias.ExibirSeletorAmbientes = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool ExibirItensFixados
    {
        get => _preferencias.ExibirItensFixados;
        set
        {
            if (_preferencias.ExibirItensFixados != value)
            {
                _preferencias.ExibirItensFixados = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool ExibirBotoesAcao
    {
        get => _preferencias.ExibirBotoesAcao;
        set
        {
            if (_preferencias.ExibirBotoesAcao != value)
            {
                _preferencias.ExibirBotoesAcao = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool ExibirClima
    {
        get => _preferencias.ExibirClima;
        set
        {
            if (_preferencias.ExibirClima != value)
            {
                _preferencias.ExibirClima = value;
                OnPropertyChanged();
                SalvarPreferencias();
                // trigger visibility change in sections
                CarregarOrdemSecoes();
            }
        }
    }

    public string LocalizacaoClima
    {
        get => _preferencias.LocalizacaoClima;
        set
        {
            if (_preferencias.LocalizacaoClima != value)
            {
                _preferencias.LocalizacaoClima = value;
                OnPropertyChanged();
                SalvarPreferencias();
                Clima.SincronizarLocalizacao(value);
            }
        }
    }

    public bool DesativarAnimacoes
    {
        get => _preferencias.DesativarAnimacoes;
        set
        {
            if (_preferencias.DesativarAnimacoes != value)
            {
                _preferencias.DesativarAnimacoes = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public int EspacamentoItens
    {
        get => _preferencias.EspacamentoItens;
        set
        {
            if (_preferencias.EspacamentoItens != value)
            {
                _preferencias.EspacamentoItens = Math.Clamp(value, 2, 24);
                OnPropertyChanged();
                OnPropertyChanged(nameof(MargemItem));
                SalvarPreferencias();
            }
        }
    }

    public Thickness MargemItem => new(EspacamentoItens / 2.0, 0, EspacamentoItens / 2.0, 0);

    public bool AppsFixadosGlobais
    {
        get => _preferencias.AppsFixadosGlobais;
        set
        {
            if (_preferencias.AppsFixadosGlobais != value)
            {
                _preferencias.AppsFixadosGlobais = value;
                OnPropertyChanged();
                SalvarPreferencias();
                CarregarAplicativos();
            }
        }
    }

    public bool ExibirSecaoIniciarPesquisa => ObterVisibilidadeSecao(TipoSecaoDock.IniciarPesquisa);
    public bool ExibirSecaoApps => ObterVisibilidadeSecao(TipoSecaoDock.Apps);
    public bool ExibirSecaoColecoes => ObterVisibilidadeSecao(TipoSecaoDock.Colecoes);
    public bool ExibirSecaoItensAmbiente => ObterVisibilidadeSecao(TipoSecaoDock.ItensAmbiente);
    public bool ExibirSecaoWidgets => ObterVisibilidadeSecao(TipoSecaoDock.Widgets);
    public bool ExibirSecaoRelogioControles => ObterVisibilidadeSecao(TipoSecaoDock.RelogioControles);

    private bool ObterVisibilidadeSecao(TipoSecaoDock tipo)
    {
        var secao = _preferencias.OrdemSecoes?.FirstOrDefault(s => s.Tipo == tipo);
        return secao?.Visivel ?? true;
    }

    public void AtualizarVisibilidadeSecoes()
    {
        OnPropertyChanged(nameof(ExibirSecaoIniciarPesquisa));
        OnPropertyChanged(nameof(ExibirSecaoApps));
        OnPropertyChanged(nameof(ExibirSecaoColecoes));
        OnPropertyChanged(nameof(ExibirSecaoItensAmbiente));
        OnPropertyChanged(nameof(ExibirSecaoWidgets));
        OnPropertyChanged(nameof(ExibirSecaoRelogioControles));
    }

    // Callbacks conectados Ã  View
    public Action<string, string>? MostrarAlerta { get; set; }
    public Func<string, string, string?>? PedirTexto { get; set; }
    public Func<ItemFixado?, ItemFixado?>? AbrirDialogoItem { get; set; }
    public Action<string?>? AbrirJanelaAjustes { get; set; }
    public Action? AbrirJanelaConfiguracoes { get; set; }
    public Action? AbrirJanelaPersonalizacao { get; set; }
    public Action? SolicitarFechamento { get; set; }
    public Action? NotificarReposicionamento { get; set; }
    public Action<EnvironmentViewModel>? TrocarAmbienteComTransicao { get; set; }

    public ICommand TrocarAmbienteCommand { get; }
    public ICommand NovoAmbienteCommand { get; }
    public ICommand RenomearAmbienteCommand { get; }
    public ICommand ExcluirAmbienteCommand { get; }
    public ICommand AdicionarItemCommand { get; }
    public ICommand AdicionarAppPermanenteCommand { get; }
    public ICommand AbrirConfiguracoesCommand { get; }
    public ICommand AbrirPersonalizarCommand { get; }
    public ICommand AbrirAjustesCommand { get; }
    public ICommand RestaurarBarraWindowsCommand { get; }
    public ICommand AbrirMenuIniciarCommand { get; }
    public ICommand AbrirIniciarNativoWindowsCommand { get; }
    public ICommand AbrirPesquisaCommand { get; }
    public ICommand AbrirExploradorCommand { get; }
    public ICommand AlternarVisibilidadeCommand { get; }
    public ICommand SairCommand { get; }

    public Action? FocarBuscaLaunchpad;
    public Action? AtivarJanelaPrincipal;

    private bool _menuIniciarAberto;
    public bool MenuIniciarAberto
    {
        get => _menuIniciarAberto;
        set
        {
            if (SetProperty(ref _menuIniciarAberto, value))
            {
                if (value)
                {
                    Clock.CalendarioAberto = false;
                    Pomodoro.PainelAberto = false;
                    Calendario.PainelAberto = false;
                    foreach (var col in TodasColecoesAtivas)
                    {
                        col.PainelAberto = false;
                    }
                    foreach (var app in Aplicativos)
                    {
                        app.MenuJanelasAberto = false;
                    }
                    TextoFiltroLaunchpad = string.Empty;
                }
            }
        }
    }

    private string _textoFiltroLaunchpad = string.Empty;
    public string TextoFiltroLaunchpad
    {
        get => _textoFiltroLaunchpad;
        set
        {
            if (SetProperty(ref _textoFiltroLaunchpad, value))
            {
                OnPropertyChanged(nameof(ItensLaunchpadFiltrados));
            }
        }
    }

    public void AbrirAjustes(string? secao = null)
    {
        AbrirJanelaAjustes?.Invoke(secao);
    }

    public void AlternarVisibilidade()
    {
        DockVisivel = !DockVisivel;
    }

    public void RestaurarBarraWindows()
    {
        _winKeyHookService.Parar();
        _taskbarService.RestaurarBarraNativa(_preferencias.EstadoAnteriorBarraTarefas);
        _preferencias.UsarComoBarraPrincipal = false;
        OnPropertyChanged(nameof(UsarComoBarraPrincipal));
        SalvarPreferencias();
        NotificarReposicionamento?.Invoke();
        MostrarAlerta?.Invoke("Barra de Tarefas Restaurada", "A barra de tarefas nativa do Windows foi restaurada com sucesso.");
    }

    public void AbrirMenuIniciar()
    {
        MenuIniciarAberto = !MenuIniciarAberto;
    }

    public void AbrirPesquisa()
    {
        MenuIniciarAberto = true;
        FocarBuscaLaunchpad?.Invoke();
    }

    public void AbrirIniciarNativoWindows()
    {
        MenuIniciarAberto = false;
        // Envia Ctrl + Esc para abrir o menu Iniciar nativo (nÃ£o interceptado pelo hook)
        keybd_event(0x11, 0, 0, 0); // Ctrl Down
        keybd_event(0x1B, 0, 0, 0); // Esc Down
        keybd_event(0x1B, 0, 2, 0); // Esc Up
        keybd_event(0x11, 0, 2, 0); // Ctrl Up
    }

    public void AbrirExplorador()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            });
        }
        catch { }
    }

    public void SalvarPreferencias()
    {
        try
        {
            _preferencias.Ambientes = Ambientes.Select(a => a.Model).ToList();
            if (Aplicativos.Count > 0)
            {
                _preferencias.AppsPermanentes = Aplicativos.Where(a => a.EstaFixado).Select((a, idx) => a.ToModel(idx)).ToList();
            }
            _preferencias.OrdemSecoes = OrdemSecoes.ToList();
            _preferencias.ColecoesGlobais = ColecoesGlobais.Select((c, idx) => { c.Model.Ordem = idx; return c.Model; }).ToList();
            _preferencias.Espacadores = Espacadores.ToList();
            _preferencias.CompromissosLocais = Calendario.Compromissos.ToList();
            _repository.Salvar(_preferencias);
        }
        catch (Exception ex)
        {
            MostrarAlerta?.Invoke("Erro ao Salvar", $"Falha ao salvar preferÃªncias: {ex.Message}");
        }
    }

    public void AtualizarPreferencias(Preferencias novasPrefs)
    {
        _preferencias = novasPrefs;
        CarregarDados();
        SalvarPreferencias();
        NotificarReposicionamento?.Invoke();
    }

    private void CarregarDados()
    {
        Ambientes.Clear();
        foreach (var amb in _preferencias.Ambientes)
        {
            Ambientes.Add(CriarAmbienteViewModel(amb));
        }

        CarregarOrdemSecoes();
        CarregarColecoes();
        CarregarEspacadores();
        CarregarAplicativos();

Calendario.SincronizarCompromissos(_preferencias.CompromissosLocais);
        Calendario.SincronizarUrlIcal(_preferencias.UrlIcal);
        Clima.SincronizarLocalizacao(_preferencias.LocalizacaoClima);

        AmbienteAtivo = Ambientes.FirstOrDefault(a => a.Id == _preferencias.AmbienteAtivoId)
                     ?? Ambientes.FirstOrDefault();

        AtualizarCoresTema();
        OnPropertyChanged(nameof(UsarComoBarraPrincipal));
        OnPropertyChanged(nameof(ExibirSeletorAmbientes));
        OnPropertyChanged(nameof(ExibirItensFixados));
        OnPropertyChanged(nameof(ExibirBotoesAcao));
        OnPropertyChanged(nameof(ExibirClima));
        OnPropertyChanged(nameof(DesativarAnimacoes));
        OnPropertyChanged(nameof(EspacamentoItens));
        OnPropertyChanged(nameof(MargemItem));
        OnPropertyChanged(nameof(TamanhoIcones));
        OnPropertyChanged(nameof(OpacidadeDock));
        OnPropertyChanged(nameof(RaioCantosDock));
        OnPropertyChanged(nameof(RaioCantosDockRadius));
        OnPropertyChanged(nameof(EfeitoDesfoque));
        OnPropertyChanged(nameof(VelocidadeAnimacao));
        OnPropertyChanged(nameof(EstiloTema));
        OnPropertyChanged(nameof(AppsFixadosGlobais));
        AtualizarVisibilidadeSecoes();
    }

    public void CarregarColecoes()
    {
        ColecoesGlobais.Clear();
        var globais = _preferencias.ColecoesGlobais ?? Preferencias.CriarColecoesGlobaisPadrao();
        foreach (var col in globais.OrderBy(c => c.Ordem))
        {
            var vm = new ColecaoAppViewModel(col, _launcher, _iconService, onEditarColecao: c => AbrirAjustes("Ambientes"), notificarErro: msg => MostrarAlerta?.Invoke("Erro", msg));
            ColecoesGlobais.Add(vm);
        }
        RecarregarTodasColecoes();
    }

    public void RecarregarTodasColecoes()
    {
        TodasColecoesAtivas.Clear();
        foreach (var col in ColecoesGlobais)
        {
            TodasColecoesAtivas.Add(col);
        }
        if (AmbienteAtivo != null)
        {
            foreach (var col in AmbienteAtivo.Colecoes)
            {
                TodasColecoesAtivas.Add(col);
            }
        }
    }

    public void CarregarEspacadores()
    {
        Espacadores.Clear();
        var lista = _preferencias.Espacadores ?? Preferencias.CriarEspacadoresPadrao();
        foreach (var esp in lista.OrderBy(e => e.Ordem))
        {
            Espacadores.Add(esp);
        }
    }

    public void SincronizarWidgetsAmbiente(EnvironmentViewModel? amb)
    {
        if (amb == null) return;

        if (amb.WidgetsInstalados.Count == 0)
        {
            var def = Preferencias.CriarWidgetsPadrao();
            foreach (var w in def)
            {
                amb.WidgetsInstalados.Add(w);
            }
        }

        var wRelogio = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Relogio);
        if (wRelogio != null)
        {
            Clock.Habilitado = wRelogio.Visivel;
            Clock.Formato = wRelogio.Formato;
        }

        var wPomodoro = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Pomodoro);
        if (wPomodoro != null)
        {
            Pomodoro.Habilitado = wPomodoro.Visivel;
            Pomodoro.Formato = wPomodoro.Formato;
        }

        var wCalendario = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.CalendarioCompromissos);
        if (wCalendario != null)
        {
            Calendario.Habilitado = wCalendario.Visivel;
            Calendario.Formato = wCalendario.Formato;
        }

        var wNotas = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Notas);
        if (wNotas != null)
        {
            Notas.Habilitado = wNotas.Visivel;
            Notas.Formato = wNotas.Formato;
        }

        var wMonitor = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.MonitorSistema);
        if (wMonitor != null)
        {
            MonitorSistema.Habilitado = wMonitor.Visivel;
            MonitorSistema.Formato = wMonitor.Formato;
        }
Calendario.SincronizarCompromissos(_preferencias.CompromissosLocais);
        Calendario.SincronizarUrlIcal(_preferencias.UrlIcal);
        Clima.SincronizarLocalizacao(_preferencias.LocalizacaoClima);
    }

    private void CarregarOrdemSecoes()
    {
        OrdemSecoes.Clear();
        if (_preferencias.OrdemSecoes == null || _preferencias.OrdemSecoes.Count == 0)
        {
            _preferencias.OrdemSecoes = Preferencias.CriarOrdemSecoesPadrao();
        }
        
        var secClima = _preferencias.OrdemSecoes.FirstOrDefault(s => s.Tipo == TipoSecaoDock.ClimaInline);
        if (secClima != null)
        {
            secClima.Visivel = _preferencias.ExibirClima;
        }

        foreach (var s in _preferencias.OrdemSecoes.OrderBy(o => o.Ordem))
        {
            OrdemSecoes.Add(s);
        }
    }

    public void CarregarAplicativos()
    {
        Aplicativos.Clear();
        var lista = _preferencias.AppsPermanentes ?? Preferencias.CriarAppsPermanentesPadrao();
        foreach (var item in lista.OrderBy(a => a.Ordem))
        {
            Aplicativos.Add(CriarAppItemViewModel(item));
        }
        AtualizarAplicativosAbertos();
    }

    private AppItemViewModel CriarAppItemViewModel(ItemFixado item)
    {
        var vm = new AppItemViewModel(
            item,
            _windowTrackingService,
            _iconService,
            onExecutar: ExecutarApp,
            onAlternarFixado: AlternarFixadoApp,
            onMoverEsquerda: MoverAppEsquerda,
            onMoverDireita: MoverAppDireita);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        return vm;
    }

    private AppItemViewModel CriarAppItemViewModelDeJanela(JanelaInfo janela)
    {
        var vm = new AppItemViewModel(
            janela,
            _windowTrackingService,
            _iconService,
            onExecutar: ExecutarApp,
            onAlternarFixado: AlternarFixadoApp,
            onMoverEsquerda: MoverAppEsquerda,
            onMoverDireita: MoverAppDireita);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        return vm;
    }

    public void AtualizarAplicativosAbertos()
    {
        var janelas = _windowTrackingService.ObterJanelasAbertas();
        var janelasNaoProcessadas = janelas.ToList();

        // 1. Atualizar aplicativos fixados
        foreach (var app in Aplicativos.Where(a => a.EstaFixado).ToList())
        {
            var correspondentes = janelasNaoProcessadas
                .Where(j => CorrespondeAoApp(app, j))
                .ToList();

            app.SincronizarJanelas(correspondentes);

            foreach (var c in correspondentes)
            {
                janelasNaoProcessadas.Remove(c);
            }
        }

        // 2. Agrupar janelas abertas nÃ£o fixadas por executÃ¡vel ou processo
        var grupos = janelasNaoProcessadas
            .GroupBy(j => !string.IsNullOrEmpty(j.CaminhoExecutavel) ? j.CaminhoExecutavel.ToLowerInvariant() : j.NomeProcesso.ToLowerInvariant())
            .ToList();

        var appsNaoFixados = Aplicativos.Where(a => !a.EstaFixado).ToList();

        foreach (var grupo in grupos)
        {
            var listaGrupo = grupo.ToList();
            var primeira = listaGrupo.First();

            var appExistente = appsNaoFixados.FirstOrDefault(a => CorrespondeAoApp(a, primeira));
            if (appExistente != null)
            {
                appExistente.SincronizarJanelas(listaGrupo);
                appsNaoFixados.Remove(appExistente);
            }
            else
            {
                var novoApp = CriarAppItemViewModelDeJanela(primeira);
                novoApp.SincronizarJanelas(listaGrupo);
                Aplicativos.Add(novoApp);
            }
        }

        // 3. Remover aplicativos nÃ£o fixados que foram fechados
        foreach (var appRemover in appsNaoFixados)
        {
            Aplicativos.Remove(appRemover);
        }
    }

    private static bool CorrespondeAoApp(AppItemViewModel app, JanelaInfo janela)
    {
        if (string.Equals(app.CaminhoExecutavel, janela.CaminhoExecutavel, StringComparison.OrdinalIgnoreCase))
            return true;

        var exeNomeApp = Path.GetFileName(app.CaminhoExecutavel);
        var exeNomeJanela = Path.GetFileName(janela.CaminhoExecutavel);

        if (!string.IsNullOrEmpty(exeNomeApp) && !string.IsNullOrEmpty(exeNomeJanela) &&
            string.Equals(exeNomeApp, exeNomeJanela, StringComparison.OrdinalIgnoreCase))
            return true;

        var procNomeApp = Path.GetFileNameWithoutExtension(app.CaminhoExecutavel);
        if (!string.IsNullOrEmpty(procNomeApp) && !string.IsNullOrEmpty(janela.NomeProcesso) &&
            string.Equals(procNomeApp, janela.NomeProcesso, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private void AlternarFixadoApp(AppItemViewModel app)
    {
        if (app.EstaFixado)
        {
            app.EstaFixado = false;
            _preferencias.AppsPermanentes.RemoveAll(a => a.Id == app.Id || CorrespondeAoApp(app, new JanelaInfo { CaminhoExecutavel = a.CaminhoOuUrl }));
            SalvarPreferencias();

            if (!app.EstaAberto)
            {
                Aplicativos.Remove(app);
            }
        }
        else
        {
            app.EstaFixado = true;
            if (!_preferencias.AppsPermanentes.Any(a => a.Id == app.Id || CorrespondeAoApp(app, new JanelaInfo { CaminhoExecutavel = a.CaminhoOuUrl })))
            {
                _preferencias.AppsPermanentes.Add(app.ToModel(_preferencias.AppsPermanentes.Count));
            }
            SalvarPreferencias();
        }
    }

    private void MoverAppEsquerda(AppItemViewModel app)
    {
        int idx = Aplicativos.IndexOf(app);
        if (idx > 0)
        {
            Aplicativos.Move(idx, idx - 1);
            SalvarPreferencias();
        }
    }

    private void MoverAppDireita(AppItemViewModel app)
    {
        int idx = Aplicativos.IndexOf(app);
        if (idx >= 0 && idx < Aplicativos.Count - 1)
        {
            Aplicativos.Move(idx, idx + 1);
            SalvarPreferencias();
        }
    }

    public void MoverAppParaAmbiente(AppItemViewModel app)
    {
        if (AmbienteAtivo == null) return;

        if (app.EstaFixado)
        {
            _preferencias.AppsPermanentes.RemoveAll(a => a.Id == app.Id || CorrespondeAoApp(app, new JanelaInfo { CaminhoExecutavel = a.CaminhoOuUrl }));
            if (!app.EstaAberto)
            {
                Aplicativos.Remove(app);
            }
            else
            {
                app.EstaFixado = false;
            }
        }

        var novo = new ItemFixado
        {
            Titulo = app.Titulo,
            CaminhoOuUrl = app.CaminhoExecutavel,
            Tipo = app.Tipo,
            Ordem = AmbienteAtivo.Itens.Count
        };
        AmbienteAtivo.Model.Itens.Add(novo);
        AmbienteAtivo.RecarregarItens();
        SalvarPreferencias();
    }

    public void MoverItemParaGlobal(ItemViewModel item)
    {
        if (AmbienteAtivo == null) return;

        AmbienteAtivo.Itens.Remove(item);
        AmbienteAtivo.SincronizarOrdemItens();

        var novo = new ItemFixado
        {
            Titulo = item.Titulo,
            CaminhoOuUrl = item.CaminhoOuUrl,
            Tipo = item.Tipo,
            Ordem = _preferencias.AppsPermanentes.Count
        };
        _preferencias.AppsPermanentes.Add(novo);
        Aplicativos.Add(CriarAppItemViewModel(novo));
        SalvarPreferencias();
        AtualizarAplicativosAbertos();
    }

    public void AdicionarAppPermanenteDireto(ItemFixado item)
    {
        if (!_preferencias.AppsPermanentes.Any(a => string.Equals(a.CaminhoOuUrl, item.CaminhoOuUrl, StringComparison.OrdinalIgnoreCase)))
        {
            item.Ordem = _preferencias.AppsPermanentes.Count;
            _preferencias.AppsPermanentes.Add(item);
            var vm = CriarAppItemViewModel(item);
            int ultimoFixado = -1;
            for (int i = 0; i < Aplicativos.Count; i++)
            {
                if (Aplicativos[i].EstaFixado) ultimoFixado = i;
            }
            if (ultimoFixado >= 0 && ultimoFixado + 1 < Aplicativos.Count)
            {
                Aplicativos.Insert(ultimoFixado + 1, vm);
            }
            else
            {
                Aplicativos.Add(vm);
            }
            SalvarPreferencias();
            AtualizarAplicativosAbertos();
        }
    }

    public void RemoverAppPermanenteDireto(string id, string caminho)
    {
        _preferencias.AppsPermanentes.RemoveAll(a => a.Id == id || string.Equals(a.CaminhoOuUrl, caminho, StringComparison.OrdinalIgnoreCase));
        var appVm = Aplicativos.FirstOrDefault(a => a.Id == id || string.Equals(a.CaminhoExecutavel, caminho, StringComparison.OrdinalIgnoreCase));
        if (appVm != null)
        {
            if (!appVm.EstaAberto)
            {
                Aplicativos.Remove(appVm);
            }
            else
            {
                appVm.EstaFixado = false;
            }
        }
    }

    private void AdicionarAppPermanentePrompt()
    {
        var novo = AbrirDialogoItem?.Invoke(null);
        if (novo != null)
        {
            AdicionarAppPermanenteDireto(novo);
        }
    }

    private void ExecutarApp(AppItemViewModel app)
    {
        var res = _launcher.ExecutarCaminho(app.CaminhoExecutavel);
        if (!res.Sucesso)
        {
            MostrarAlerta?.Invoke("Erro ao Abrir Aplicativo", res.MensagemErro ?? "NÃ£o foi possÃ­vel iniciar o aplicativo.");
        }
    }

    private EnvironmentViewModel CriarAmbienteViewModel(Ambiente amb)
    {
        return new EnvironmentViewModel(
            amb,
            _launcher,
            _iconService,
            onEditarItem: EditarItem,
            onRemoverItem: RemoverItem,
            onMoverEsquerda: MoverItemEsquerda,
            onMoverDireita: MoverItemDireita,
            notificarErro: msg => MostrarAlerta?.Invoke("Erro ao Executar", msg),
            onMoverParaGlobal: MoverItemParaGlobal,
            onEditarColecao: col => AbrirAjustes("Ambientes"));
    }

    private void TrocarAmbiente(EnvironmentViewModel? amb)
    {
        if (amb != null)
        {
            if (TrocarAmbienteComTransicao != null)
            {
                TrocarAmbienteComTransicao(amb);
            }
            else
            {
                AmbienteAtivo = amb;
            }
        }
    }

    private void NovoAmbiente()
    {
        var nome = PedirTexto?.Invoke("Novo Ambiente", "Digite o nome para o novo ambiente:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        var novoAmb = new Ambiente
        {
            Id = "amb-" + Guid.NewGuid().ToString("N")[..8],
            Nome = nome.Trim(),
            CorHex = "#0078D4",
            Icone = "ðŸ’¼",
            Widgets = new WidgetConfig(),
            WidgetsInstalados = Preferencias.CriarWidgetsPadrao(),
            Colecoes = new List<ColecaoApp>()
        };

        var vm = CriarAmbienteViewModel(novoAmb);
        Ambientes.Add(vm);
        AmbienteAtivo = vm;
        SalvarPreferencias();
    }

    private void RenomearAmbiente(EnvironmentViewModel? amb)
    {
        if (amb == null) return;
        var novoNome = PedirTexto?.Invoke("Renomear Ambiente", $"Novo nome para '{amb.Nome}':");
        if (string.IsNullOrWhiteSpace(novoNome)) return;

        amb.Nome = novoNome.Trim();
        SalvarPreferencias();
    }

    private void ExcluirAmbiente(EnvironmentViewModel? amb)
    {
        if (amb == null) return;
        if (Ambientes.Count <= 1)
        {
            MostrarAlerta?.Invoke("Aviso", "Ã‰ necessÃ¡rio manter ao menos um ambiente ativo.");
            return;
        }

        Ambientes.Remove(amb);
        if (AmbienteAtivo == amb)
        {
            AmbienteAtivo = Ambientes.First();
        }
        SalvarPreferencias();
    }

    public void AdicionarItem()
    {
        if (AmbienteAtivo == null) return;

        var novoItem = AbrirDialogoItem?.Invoke(null);
        if (novoItem != null)
        {
            novoItem.Ordem = AmbienteAtivo.Itens.Count;
            AmbienteAtivo.Model.Itens.Add(novoItem);
            AmbienteAtivo.RecarregarItens();
            SalvarPreferencias();
        }
    }

    public void AdicionarItemDireto(ItemFixado item)
    {
        if (AmbienteAtivo == null) return;
        item.Ordem = AmbienteAtivo.Itens.Count;
        AmbienteAtivo.Model.Itens.Add(item);
        AmbienteAtivo.RecarregarItens();
        SalvarPreferencias();
    }

    private void EditarItem(ItemViewModel itemVm)
    {
        if (AmbienteAtivo == null) return;

        var itemEditado = AbrirDialogoItem?.Invoke(itemVm.Model);
        if (itemEditado != null)
        {
            itemVm.Titulo = itemEditado.Titulo;
            itemVm.CaminhoOuUrl = itemEditado.CaminhoOuUrl;
            itemVm.Tipo = itemEditado.Tipo;
            itemVm.CarregarIcone();
            SalvarPreferencias();
        }
    }

    private void RemoverItem(ItemViewModel itemVm)
    {
        if (AmbienteAtivo == null) return;
        AmbienteAtivo.Itens.Remove(itemVm);
        AmbienteAtivo.SincronizarOrdemItens();
        SalvarPreferencias();
    }

    private void MoverItemEsquerda(ItemViewModel itemVm)
    {
        if (AmbienteAtivo == null) return;
        int idx = AmbienteAtivo.Itens.IndexOf(itemVm);
        if (idx > 0)
        {
            AmbienteAtivo.Itens.Move(idx, idx - 1);
            AmbienteAtivo.SincronizarOrdemItens();
            SalvarPreferencias();
        }
    }

    private void MoverItemDireita(ItemViewModel itemVm)
    {
        if (AmbienteAtivo == null) return;
        int idx = AmbienteAtivo.Itens.IndexOf(itemVm);
        if (idx >= 0 && idx < AmbienteAtivo.Itens.Count - 1)
        {
            AmbienteAtivo.Itens.Move(idx, idx + 1);
            AmbienteAtivo.SincronizarOrdemItens();
            SalvarPreferencias();
        }
    }

    public bool ExportarConfiguracoesJson(string caminhoArquivo)
    {
        try
        {
            SalvarPreferencias();
            var json = File.ReadAllText(_repository.ObterCaminhoConfiguracoes());
            File.WriteAllText(caminhoArquivo, json);
            return true;
        }
        catch (Exception ex)
        {
            MostrarAlerta?.Invoke("Erro ao Exportar", $"NÃ£o foi possÃ­vel salvar o backup: {ex.Message}");
            return false;
        }
    }

    public bool ImportarConfiguracoesJson(string caminhoArquivo)
    {
        try
        {
            var json = File.ReadAllText(caminhoArquivo);
            var prefs = JsonSerializer.Deserialize<Preferencias>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (prefs != null && prefs.Ambientes.Count > 0)
            {
                AtualizarPreferencias(prefs);
                return true;
            }
            MostrarAlerta?.Invoke("Arquivo InvÃ¡lido", "O arquivo JSON selecionado nÃ£o contÃ©m uma configuraÃ§Ã£o vÃ¡lida do Dock Windows.");
            return false;
        }
        catch (Exception ex)
        {
            MostrarAlerta?.Invoke("Erro ao Importar", $"Falha ao importar o arquivo de configuraÃ§Ã£o: {ex.Message}");
            return false;
        }
    }

    public void RestaurarPadroesFabrica()
    {
        var padrao = Preferencias.CriarPadrao();
        AtualizarPreferencias(padrao);
    }

    public IEnumerable<LaunchpadItemModel> ItensLaunchpadFiltrados
    {
        get
        {
            var lista = new List<LaunchpadItemModel>();
            var idsAdicionados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Aplicativos da Dock (fixados e abertos)
            foreach (var app in Aplicativos)
            {
                var chave = !string.IsNullOrEmpty(app.CaminhoExecutavel) ? app.CaminhoExecutavel : app.Titulo;
                if (idsAdicionados.Add(chave))
                {
                    lista.Add(new LaunchpadItemModel
                    {
                        Id = app.Id,
                        Titulo = app.Titulo,
                        Subtitulo = app.EstaAberto ? (app.EstaAtivo ? "Janela ativa" : $"{app.QuantidadeJanelas} janela(s)") : "Fixado na barra",
                        Icone = app.Icone,
                        IconeTexto = "âœ¦",
                        ExecutarCommand = new RelayCommand(() =>
                        {
                            MenuIniciarAberto = false;
                            if (app.EstaAberto)
                            {
                                var win = app.Janelas.FirstOrDefault();
                                if (win != null)
                                {
                                    _windowTrackingService.AtivarJanela(win.Hwnd);
                                }
                            }
                            else
                            {
                                ExecutarApp(app);
                            }
                        }),
                        EstaAberto = app.EstaAberto,
                        EstaAtivo = app.EstaAtivo,
                        Categoria = "Aplicativos"
                    });
                }
            }

            // 2. Itens do ambiente ativo
            if (AmbienteAtivo != null)
            {
                foreach (var item in AmbienteAtivo.Itens)
                {
                    var chave = !string.IsNullOrEmpty(item.CaminhoOuUrl) ? item.CaminhoOuUrl : item.Titulo;
                    if (idsAdicionados.Add(chave))
                    {
                        lista.Add(new LaunchpadItemModel
                        {
                            Id = item.Id,
                            Titulo = item.Titulo,
                            Subtitulo = item.Tipo == TipoItem.WebUrl ? "PÃ¡gina Web" : (item.Tipo == TipoItem.Pasta ? "Pasta" : "Atalho"),
                            Icone = item.Icone,
                            IconeTexto = item.Tipo == TipoItem.WebUrl ? "ðŸŒ" : (item.Tipo == TipoItem.Pasta ? "ðŸ“" : (item.Tipo == TipoItem.Arquivo ? "ðŸ“„" : "ðŸš€")),
                            ExecutarCommand = new RelayCommand(() =>
                            {
                                MenuIniciarAberto = false;
                                item.ExecutarCommand.Execute(null);
                            }),
                            EstaAberto = false,
                            EstaAtivo = false,
                            Categoria = AmbienteAtivo.Nome
                        });
                    }
                }
            }

            // 3. ColeÃ§Ãµes ativas
            foreach (var col in TodasColecoesAtivas)
            {
                if (idsAdicionados.Add("col-" + col.Id))
                {
                    lista.Add(new LaunchpadItemModel
                    {
                        Id = col.Id,
                        Titulo = col.Nome,
                        Subtitulo = $"{col.QuantidadeItens} itens",
                        Icone = col.Miniatura1,
                        IconeTexto = "ðŸ“",
                        ExecutarCommand = new RelayCommand(() =>
                        {
                            MenuIniciarAberto = false;
                            col.PainelAberto = true;
                        }),
                        EstaAberto = false,
                        EstaAtivo = false,
                        Categoria = "ColeÃ§Ãµes"
                    });
                }
            }

            if (string.IsNullOrWhiteSpace(TextoFiltroLaunchpad))
            {
                return lista;
            }

            var filtro = TextoFiltroLaunchpad.Trim();
            return lista.Where(i =>
                i.Titulo.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                i.Subtitulo.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                i.Categoria.Contains(filtro, StringComparison.OrdinalIgnoreCase));
        }
    }
}

public class LaunchpadItemModel
{
    public string Id { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Subtitulo { get; set; } = string.Empty;
    public System.Windows.Media.ImageSource? Icone { get; set; }
    public string? IconeTexto { get; set; }
    public ICommand ExecutarCommand { get; set; } = null!;
    public bool EstaAberto { get; set; }
    public bool EstaAtivo { get; set; }
    public string Categoria { get; set; } = "Aplicativos";
}






