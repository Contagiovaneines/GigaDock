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

public class MainViewModel : ObservableObject, IDisposable
{
    public DockWindows.Core.Widgets.GerenciadorAtividade Atividade { get; } = new();
    private readonly List<Action> _desassinar = new();
    private bool _disposed, _dockRenderizada, _syncOcupado, _syncPendente;
    private System.Threading.CancellationTokenSource? _alertaCancelamento;
    public bool VisualAtivo => !_disposed && _dockRenderizada && DockVisivel && !OcultoPorTelaCheia;
    public bool AnimacoesAtivas => VisualAtivo && !DesativarAnimacoes && !ModoEconomico && SystemParameters.ClientAreaAnimation;
    public void DefinirVisibilidadeReal(bool visivel) { _dockRenderizada = visivel; AtualizarAtividade(); }
    private void AtualizarAtividade()
    {
        Atividade.DefinirDock(VisualAtivo, AnimacoesAtivas);
        foreach (var runtime in WidgetsInstanciadosAdicionais)
        {
            if (VisualAtivo) runtime.Ativar(true);
            else runtime.Suspender();
        }
        OnPropertyChanged(nameof(VisualAtivo)); OnPropertyChanged(nameof(AnimacoesAtivas));
        if (Midia != null) Atividade.Definir("Midia", Midia.Habilitado, ModoRgbMedia);
        if (VisualAtivo) { AgendarNotificacoes(); AtualizarAplicativosAbertos(); }
    }
    private void RegistrarWidget(string id, ObservableObject vm, IAtividadeWidget atividade,
        Func<bool> habilitado, Func<bool>? background = null, bool independente = false, bool observarMontado = false)
    {
        Atividade.Registrar(id, atividade.DefinirAtividade, atividade.Dispose, independente, observarMontado, () => atividade.EmExecucao);
        void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            Atividade.InformarSaude(id, atividade.Saude, atividade.MotivoEstado);
            if (args.PropertyName is "Habilitado" or "EstaExecutando") Atividade.Definir(id, habilitado(), background?.Invoke() == true);
        }
        vm.PropertyChanged += Changed;
        _desassinar.Add(() => vm.PropertyChanged -= Changed);
        Atividade.Definir(id, habilitado(), background?.Invoke() == true);
        Atividade.InformarSaude(id, atividade.Saude, atividade.MotivoEstado);
    }
    private void AgendarNotificacoes()
    {
        if (_disposed || _syncNotificacoesTimer == null) return;
        if (_syncOcupado) { _syncPendente = true; return; }
        _syncNotificacoesTimer.Stop(); _syncNotificacoesTimer.Start();
    }
    private void ToastRecebido(string appName, bool isCall, string senderName) =>
        Application.Current?.Dispatcher?.InvokeAsync(() => { if (!_disposed) TratarNotificacaoToast(appName, isCall, senderName); });
    private void FinalizarSincronizacao()
    {
        _syncOcupado = false;
        if (_syncPendente) { _syncPendente = false; AgendarNotificacoes(); }
    }
    private void ToastPermissaoAlterada() => Application.Current?.Dispatcher?.InvokeAsync(() =>
    { if (_disposed) return; WhatsApp.EstadoNotificacoes = _toastService.EstadoPermissao; Teams.EstadoNotificacoes = _toastService.EstadoPermissao; });
    private void ToastContagemAlterada() => Application.Current?.Dispatcher?.InvokeAsync(AgendarNotificacoes);
    private void JanelasMudaram() => Application.Current?.Dispatcher?.InvokeAsync(() =>
    {
        if (_disposed) return;
        AtualizarAplicativosAbertos();
        // O painel de segundo plano é atualizado ao abrir. Atualizá-lo em toda
        // mudança de foco recriava os ícones enquanto o ponteiro passava sobre eles.
    });
    private void JanelaMudou(IntPtr hwnd) => JanelasMudaram();
    private void TelaCheiaMudou(bool telaCheia) => Application.Current?.Dispatcher?.InvokeAsync(() => { if (!_disposed) OcultoPorTelaCheia = telaCheia; });
    private void WinTapped() => Application.Current?.Dispatcher?.InvokeAsync(() =>
    { if (_disposed) return; ForcarExibicao(); AtivarJanelaPrincipal?.Invoke(); AbrirMenuIniciar(); });
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _alertaCancelamento?.Cancel(); _alertaCancelamento?.Dispose();
        _syncNotificacoesTimer?.Stop(); Notificacoes.Dispose();
        _toastService.OnNotificationReceived -= ToastRecebido;
        _toastService.ChamadasEncerradas -= ChamadaToastEncerrada;
        _toastService.PermissaoAlterada -= ToastPermissaoAlterada;
        _toastService.ContagensAlteradas -= ToastContagemAlterada; _toastService.Dispose();
        foreach (var remover in _desassinar) remover(); _desassinar.Clear();
        LimparWidgetsInstanciadosAdicionais();
        Atividade.Dispose(); ControlesRapidos.Dispose();
        _windowTrackingService.JanelasAlteradas -= JanelasMudaram;
        _windowTrackingService.JanelaAtivada -= JanelaMudou;
        _windowTrackingService.JanelaAtivada -= OnJanelaAtivada;
        _windowTrackingService.TelaCheiaAlterada -= TelaCheiaMudou;
        _windowTrackingService.Parar(); (_windowTrackingService as IDisposable)?.Dispose();
        _winKeyHookService.WinKeyTapped -= WinTapped; _winKeyHookService.Parar();
        (_winKeyHookService as IDisposable)?.Dispose();
        (_taskbarService as IDisposable)?.Dispose();
    }
    private readonly ISettingsRepository _repository;
    private readonly ILauncherService _launcher;
    private readonly IIconExtractionService _iconService;
    private readonly IAutostartService _autostart;
    private readonly ILixeiraDesktopService _lixeiraDesktop;
    private readonly ITaskbarService _taskbarService;
    private readonly IWindowTrackingService _windowTrackingService;
    private readonly IAplicativosSegundoPlanoService _aplicativosSegundoPlanoService;
    private readonly IWinKeyHookService _winKeyHookService;
    private int _whatsappGhosts = 0;
    private int _teamsGhosts = 0;
    private System.Windows.Threading.DispatcherTimer? _syncNotificacoesTimer;
    private readonly DockWindows.Infrastructure.Windows.ToastNotificationService _toastService;
    public NotificationCenterViewModel Notificacoes { get; }
    


        private Preferencias _preferencias;
    private EnvironmentViewModel? _ambienteAtivo;
    private bool _dockVisivel = true;
    private bool _ocultoPorTelaCheia = false;

    // Sistema de Alerta Global
    private bool _estaEmAlerta;
    private string _corAlerta = "#128C7E";

    public bool EstaEmAlerta { get => _estaEmAlerta; set { if (SetProperty(ref _estaEmAlerta, value)) OnPropertyChanged(nameof(GlowRgbVisivel)); } }
    public bool AlertaChamada { get; private set; }
    private readonly Queue<string> _pulsosAlerta = new();
    private bool _processandoPulsos;
        public string CorAlerta 
    { 
        get => _corAlerta; 
        set 
        { 
            if (SetProperty(ref _corAlerta, value)) 
                OnPropertyChanged(nameof(CorAlertaMedia)); 
        } 
    }
    public System.Windows.Media.Color CorAlertaMedia 
    { 
        get 
        { 
            try { return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_corAlerta); } 
            catch { return System.Windows.Media.Color.FromRgb(255,0,0); } 
        } 
    }

    public void DispararAlertaGlobal(string corHex, bool isCall = false)
    {
        void Aplicar()
        {
            if (_disposed || !AlertasVisuaisHabilitados) return;
            if (isCall)
            {
                AlertaChamada = true;
                OnPropertyChanged(nameof(AlertaChamada));
                CorAlerta = corHex;
                EstaEmAlerta = true;
                return;
            }
            _pulsosAlerta.Enqueue(corHex);
            if (!_processandoPulsos) _ = ProcessarPulsosAsync();
        }
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess()) dispatcher.InvokeAsync(Aplicar);
        else Aplicar();
    }

    private void ChamadaToastEncerrada() => Application.Current?.Dispatcher?.InvokeAsync(() =>
    {
        EncerrarAlertaGlobal();
    });

    private void EncerrarAlertaGlobal()
    {
        AlertaChamada = false;
        OnPropertyChanged(nameof(AlertaChamada));
        EstaEmAlerta = false;
    }

    private void EncerrarAlertaDoApp(AppItemViewModel app)
    {
        var identificacao = $"{app.Titulo} {app.CaminhoExecutavel}".ToLowerInvariant();
        if (identificacao.Contains("whatsapp") || identificacao.Contains("teams") ||
            identificacao.Contains("msteams") || identificacao.Contains("discord"))
        {
            EncerrarAlertaGlobal();
        }
    }

    private async Task ProcessarPulsosAsync()
    {
        _processandoPulsos = true;
        _alertaCancelamento ??= new();
        var token = _alertaCancelamento.Token;
        try
        {
            while (_pulsosAlerta.Count > 0 && !_disposed && AlertasVisuaisHabilitados)
            {
                while (AlertaChamada) await Task.Delay(200, token);
                CorAlerta = _pulsosAlerta.Dequeue();
                EstaEmAlerta = true;
                await Task.Delay(700, token);
                if (!AlertaChamada) EstaEmAlerta = false;
                await Task.Delay(150, token);
            }
        }
        catch (OperationCanceledException) { _pulsosAlerta.Clear(); }
        finally { _processandoPulsos = false; }
    }

            private void TratarNotificacaoToast(string appName, bool isCall = false, string senderName = "")
    {
        if (string.IsNullOrEmpty(appName)) return;
        string proc = appName.ToLowerInvariant();
        
        // Find matching app to increment badge
        var app = Aplicativos.FirstOrDefault(a => 
            (!string.IsNullOrEmpty(a.Titulo) && a.Titulo.ToLowerInvariant().Contains(proc)) ||
            (!string.IsNullOrEmpty(a.CaminhoExecutavel) && a.CaminhoExecutavel.ToLowerInvariant().Contains(proc))
        );

        if (app != null)
        {
            app.NumeroNotificacoes++;
        }
        if (AlertasVisuaisHabilitados)
        {
            string cor = string.Empty;
            if (proc.Contains("teams") || proc.Contains("msteams")) cor = "#8B7CFF"; // Roxo
            else if (proc.Contains("whatsapp")) cor = "#25D366"; // Verde
            else if (proc.Contains("discord")) cor = "#5865F2"; // Azul discord
            
            if (!string.IsNullOrEmpty(cor))
            {
                DispararAlertaGlobal(cor, isCall);
            }
        }
        
        if (proc.Contains("whatsapp")) WhatsApp.MensagensNaoLidas++;
        // Cada notificação incrementa cada indicador uma única vez.
        if (proc.Contains("teams") || proc.Contains("msteams"))
        {
            Teams.MensagensNaoLidas++;
            if (!string.IsNullOrEmpty(senderName)) Teams.ExibirMensagemDe(senderName);
        }
    }


        
    private void OnJanelaAtivada(IntPtr hwnd) => _ = ProcessarJanelaAtivadaAsync(hwnd);

    private async Task ProcessarJanelaAtivadaAsync(IntPtr hwnd)
    {
        try
        {
            var windows = _windowTrackingService.ObterJanelasAbertas();
            var activeWindow = System.Linq.Enumerable.FirstOrDefault(windows, w => w.Hwnd == hwnd);
            if (activeWindow == null) return;
            string exec = (activeWindow.CaminhoExecutavel ?? "").ToLowerInvariant();
            string name = (activeWindow.NomeProcesso ?? "").ToLowerInvariant();
            string title = (activeWindow.Titulo ?? "").ToLowerInvariant();

            if (exec.Contains("whatsapp") || name.Contains("whatsapp") || title.Contains("whatsapp"))
            {
                EncerrarAlertaGlobal();
                if (_disposed || _syncOcupado) return;
        _syncOcupado = true;
        Dictionary<string, int> dict;
        try { dict = await _toastService.ObterContagemNotificacoesPorAppAsync(); }
        finally { FinalizarSincronizacao(); }
        if (_disposed) return;
                _whatsappGhosts = dict.Where(x => x.Key.ToLowerInvariant().Contains("whatsapp")).Sum(x => x.Value);
                WhatsApp.MensagensNaoLidas = 0;
            }

            if (exec.Contains("teams") || exec.Contains("msteams") || name.Contains("teams") || name.Contains("msteams") || title.Contains("teams") || title.Contains("msteams"))
            {
                EncerrarAlertaGlobal();
                if (_disposed || _syncOcupado) return;
        _syncOcupado = true;
        Dictionary<string, int> dict;
        try { dict = await _toastService.ObterContagemNotificacoesPorAppAsync(); }
        finally { FinalizarSincronizacao(); }
        if (_disposed) return;
                _teamsGhosts = dict.Where(x => x.Key.ToLowerInvariant().Contains("teams") || x.Key.ToLowerInvariant().Contains("msteams")).Sum(x => x.Value);
                Teams.MensagensNaoLidas = 0;
            }
        }
        catch (Exception ex) { RegistrarFalhaNaoFatal("notificacoes-janelas.log", ex); }
    }

    private async System.Threading.Tasks.Task SincronizarNotificacoesComWindowsAsync()
    {
        try { await SincronizarNotificacoesComWindowsCoreAsync(); }
        catch (Exception ex) { RegistrarFalhaNaoFatal("sincronizacao-notificacoes.log", ex); }
    }

    private async System.Threading.Tasks.Task SincronizarNotificacoesComWindowsCoreAsync()
    {
        if (_disposed || _syncOcupado) return;
        _syncOcupado = true;
        Dictionary<string, int> dict;
        try { dict = await _toastService.ObterContagemNotificacoesPorAppAsync(); }
        finally { FinalizarSincronizacao(); }
        if (_disposed) return;
        int wappCount = 0;
        int teamsCount = 0;

        foreach (var kvp in dict)
        {
            string proc = kvp.Key.ToLowerInvariant();
            if (proc.Contains("whatsapp")) wappCount += kvp.Value;
            if (proc.Contains("teams") || proc.Contains("msteams")) teamsCount += kvp.Value;
        }

        if (wappCount < _whatsappGhosts) _whatsappGhosts = wappCount;
        if (teamsCount < _teamsGhosts) _teamsGhosts = teamsCount;

        int wappReal = System.Math.Max(0, wappCount - _whatsappGhosts);
        int teamsReal = System.Math.Max(0, teamsCount - _teamsGhosts);

        // Apenas atualiza se a flag não estiver forçando o zero
        // Na verdade, podemos apenas definir o valor
        WhatsApp.MensagensNaoLidas = wappReal;
        Teams.MensagensNaoLidas = teamsReal;
    }

    public void IncrementarNotificacaoApp(IntPtr hwnd)
    {
        var app = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<AppItemViewModel>(Aplicativos), a => System.Linq.Enumerable.Any(a.Janelas, j => j.Hwnd == hwnd));
        if (app != null)
        {
            app.NumeroNotificacoes++;
            
            if (AlertasVisuaisHabilitados)
            {
                string proc = (app.Titulo ?? string.Empty).ToLowerInvariant();
                string cor = string.Empty;
                
                if (proc.Contains("teams") || proc.Contains("msteams")) cor = "#8B7CFF"; // Roxo
                else if (proc.Contains("whatsapp")) cor = "#25D366"; // Verde
                else if (proc.Contains("discord")) cor = "#5865F2"; // Azul discord
                
                if (!string.IsNullOrEmpty(cor))
                {
                    DispararAlertaGlobal(cor);
                }
                
                if (proc.Contains("teams") || proc.Contains("msteams"))
                {
                    Teams.MensagensNaoLidas = app.NumeroNotificacoes;
                }
            }
        }
    }

    public bool OcultoPorTelaCheia
    {
        get => _ocultoPorTelaCheia;
        set { if (SetProperty(ref _ocultoPorTelaCheia, value)) AtualizarAtividade(); }
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
        IWinKeyHookService? winKeyHookService = null,
        ILixeiraDesktopService? lixeiraDesktopService = null,
        IAplicativosSegundoPlanoService? aplicativosSegundoPlanoService = null,
        IClipboardNotificationService? clipboardNotificationService = null,
        IConectividadeService? conectividadeService = null,
        IAudioSystemService? audioSystemService = null)
    {
        _repository = repository;
        _launcher = launcher;
        _iconService = iconService;
        _autostart = autostart;
        _lixeiraDesktop = lixeiraDesktopService ?? new LixeiraDesktopService();
        _taskbarService = taskbarService ?? new Win32TaskbarService();
        _windowTrackingService = windowTrackingService ?? new Win32WindowTrackingService();
        _aplicativosSegundoPlanoService = aplicativosSegundoPlanoService ?? new AplicativosSegundoPlanoService();
        _winKeyHookService = winKeyHookService ?? new WinKeyHookService();
        _toastService = new DockWindows.Infrastructure.Windows.ToastNotificationService();
        Notificacoes = new NotificationCenterViewModel(_toastService, _iconService,
            () => MessageBox.Show("Limpar todas as notificações também remove os avisos da central do Windows. Continuar?",
                "Limpar notificações", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes);
        _toastService.OnNotificationReceived += ToastRecebido;
        _toastService.ChamadasEncerradas += ChamadaToastEncerrada;
        _toastService.ContagensAlteradas += ToastContagemAlterada;
        _toastService.PermissaoAlterada += ToastPermissaoAlterada;
        if (Application.Current != null) _ = _toastService.Iniciar();





        _preferencias = _repository.Carregar();

        // Migração: Move apps globais antigos para o primeiro ambiente
        if (!_preferencias.AppsGlobaisMigrados && _preferencias.AppsPermanentes != null)
        {
            var primeiroAmbiente = _preferencias.Ambientes?.FirstOrDefault();
            if (primeiroAmbiente != null)
            {
                foreach (var app in _preferencias.AppsPermanentes)
                {
                    if (!primeiroAmbiente.Itens.Any(i => i.CaminhoOuUrl == app.CaminhoOuUrl))
                    {
                        app.Ordem = primeiroAmbiente.Itens.Count;
                        primeiroAmbiente.Itens.Add(app);
                    }
                }
            }
            _preferencias.AppsPermanentes.Clear();
            _preferencias.AppsGlobaisMigrados = true;
            _repository.Salvar(_preferencias);
        }

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
        AplicativosSegundoPlano = new ObservableCollection<AplicativoSegundoPlanoViewModel>();
        ColecoesGlobais = new ObservableCollection<ColecaoAppViewModel>();
        TodasColecoesAtivas = new ObservableCollection<ColecaoAppViewModel>();
        Espacadores = new ObservableCollection<EspacadorConfig>();
        OrdemSecoes = new ObservableCollection<ConfigSecaoDock>();
        WidgetsInstanciadosAdicionais = new ObservableCollection<WidgetInstanceRuntimeViewModel>();
        RelogiosAdicionais = new ObservableCollection<WidgetInstanceRuntimeViewModel>();
        NotasAdicionais = new ObservableCollection<WidgetInstanceRuntimeViewModel>();
        PersonalizarWidgetCommand = new RelayCommand<TipoWidget>(PersonalizarWidget);
        Clock = new ClockWidgetViewModel();
        Pomodoro = new PomodoroWidgetViewModel();
        Calendario = new CalendarioWidgetViewModel(onAbrirAjustes: () => AbrirAjustes("Widgets"));
        Midia = new MidiaWidgetViewModel(() => _preferencias.AbrirPlayerAoDuploClique);
        Midia.PropertyChanged += (s, e) => {
            if (e.PropertyName == "CorPredominanteHex" || e.PropertyName == "EstaTocando" || e.PropertyName == "TemMidia") {
                OnPropertyChanged(nameof(CorSombraDock));
                OnPropertyChanged(nameof(GlowRgbVisivel));
            }
        };
        Notas = new NotasWidgetViewModel();
        LembreteAgua = new LembreteAguaViewModel();
        CotacaoMoedas = new CotacaoMoedasViewModel();
        AreaTransferencia = new AreaTransferenciaViewModel(clipboardNotificationService ?? new ClipboardNotificationService());
        ArquivosRecentes = new ArquivosRecentesViewModel();
        Conectividade = new ConectividadeViewModel(conectividadeService ?? new ConectividadeService());
        AudioSistema = new AudioSistemaViewModel(audioSystemService ?? new AudioSystemService());
        EstanteArquivos = new EstanteArquivosViewModel(SalvarEstanteArquivos);
        MonitorSistema = new MonitorSistemaViewModel();
                        GitHub = new GitHubWidgetViewModel();
        GitHub.SincronizarUsuario(_preferencias.GitHubUsuario);
        GitHub.SelecionarAnimacao(_preferencias.GitHubAnimacao);
        GitHub.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(GitHubWidgetViewModel.AnimacaoSelecionada)) return;
            _preferencias.GitHubAnimacao = GitHub.AnimacaoSelecionada;
            AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.GitHubContribuicoes)
                ?.DefinirConfiguracao("animacao", GitHub.AnimacaoSelecionada);
            SalvarPreferencias();
        };
        Clima = new ClimaWidgetViewModel();
        MascotePokemon = new MascotePokemonViewModel(() => Clima.Condicao);
        Bateria = new BateriaViewModel { Habilitado = _preferencias.ExibirBateria };
        MonitorSistema.ModoEconomico = _preferencias.ModoEconomico;
        Bateria.ModoEconomico = _preferencias.ModoEconomico;
        Midia.ModoEconomico = _preferencias.ModoEconomico;
        IconExtractionService.DefinirModoEconomico(_preferencias.ModoEconomico);
        if (_windowTrackingService is Win32WindowTrackingService rastreamento) rastreamento.DefinirModoEconomico(_preferencias.ModoEconomico);

        ControlesRapidos = new ControlesRapidosViewModel(SalvarPreferencias,
            mensagem => MostrarAlerta?.Invoke("Controles rápidos", mensagem),
            () => System.Windows.MessageBox.Show("Suspender o computador agora?", "Suspender",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question,
                System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes,
            conectividade: Conectividade);

        WhatsApp = new WhatsAppWidgetViewModel(cor => DispararAlertaGlobal(cor));
        Teams = new TeamsWidgetViewModel(cor => DispararAlertaGlobal(cor));
        Discord = new DiscordWidgetViewModel(cor => DispararAlertaGlobal(cor));

        TrocarAmbienteCommand = new RelayCommand<EnvironmentViewModel>(TrocarAmbiente);
        NovoAmbienteCommand = new RelayCommand(NovoAmbiente);
        RenomearAmbienteCommand = new RelayCommand<EnvironmentViewModel>(RenomearAmbiente);
        ExcluirAmbienteCommand = new RelayCommand<EnvironmentViewModel>(ExcluirAmbiente);

                AdicionarItemCommand = new RelayCommand(AdicionarItem);
        AdicionarColecaoGlobalCommand = new RelayCommand(AdicionarColecaoGlobal);
        AdicionarAppPermanenteCommand = new RelayCommand(AdicionarAppPermanentePrompt);
        AbrirConfiguracoesCommand = new RelayCommand(() => AbrirAjustes("Geral"));
        AbrirPersonalizarCommand = new RelayCommand(() => AbrirAjustes("Aparencia"));
        SelecionarEstiloMidiaCommand = new RelayCommand<string>(SelecionarEstiloMidia);
        SelecionarEstiloClimaCommand = new RelayCommand<string>(SelecionarEstiloClima);
        OcultarMidiaCommand = new RelayCommand(() => ExibirMidia = false);
        AbrirAjustesCommand = new RelayCommand<string>(AbrirAjustes);
                RestaurarBarraWindowsCommand = new RelayCommand(RestaurarBarraWindows);
        ToggleBarraNativaCommand = new RelayCommand(ToggleBarraNativa);
        AbrirMenuIniciarCommand = new RelayCommand(AbrirMenuIniciar);
        AbrirIniciarNativoWindowsCommand = new RelayCommand(AbrirIniciarNativoWindows);
        AbrirPesquisaCommand = new RelayCommand(AbrirPesquisa);
        AbrirExploradorCommand = new RelayCommand(AbrirExplorador);
        AlternarVisibilidadeCommand = new RelayCommand(AlternarVisibilidade);
        SairCommand = new RelayCommand(() => SolicitarFechamento?.Invoke());
        AbrirLixeiraCommand = new RelayCommand(() => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = "shell:RecycleBinFolder", UseShellExecute = true }); } catch { } });
        AbrirBandejaOcultaCommand = new RelayCommand(() => _ = AbrirBandejaWindowsAsync());
        AbrirBandejaNativaCommand = new RelayCommand(() => _ = AbrirBandejaNativaAsync());

        _winKeyHookService.WinKeyTapped += WinTapped;

        if (_preferencias.UsarComoBarraPrincipal && Application.Current != null)
        {
            _winKeyHookService.Iniciar();
        }

        _windowTrackingService.JanelasAlteradas += JanelasMudaram;
        _windowTrackingService.JanelaAtivada += JanelaMudou;
        _windowTrackingService.TelaCheiaAlterada += TelaCheiaMudou;
        RegistrarWidget("Relogio", Clock, Clock, () => Clock.Habilitado, () => Clock.EstaExecutando, independente: true);
        RegistrarWidget("Pomodoro", Pomodoro, Pomodoro, () => Pomodoro.Habilitado, () => Pomodoro.EstaExecutando, independente: true);
        RegistrarWidget("Calendario", Calendario, Calendario, () => Calendario.Habilitado);
        RegistrarWidget("Notas", Notas, Notas, () => Notas.Habilitado);
        RegistrarWidget("LembreteAgua", LembreteAgua, LembreteAgua, () => LembreteAgua.Habilitado, () => LembreteAgua.Habilitado, independente: true);
        RegistrarWidget("CotacaoMoedas", CotacaoMoedas, CotacaoMoedas, () => CotacaoMoedas.Habilitado);
        RegistrarWidget("AreaTransferencia", AreaTransferencia, AreaTransferencia, () => AreaTransferencia.Habilitado);
        RegistrarWidget("ArquivosRecentes", ArquivosRecentes, ArquivosRecentes, () => ArquivosRecentes.Habilitado);
        RegistrarWidget("Conectividade", Conectividade, Conectividade, () => Conectividade.Habilitado);
        RegistrarWidget("AudioSistema", AudioSistema, AudioSistema, () => AudioSistema.Habilitado);
        RegistrarWidget("EstanteArquivos", EstanteArquivos, EstanteArquivos, () => EstanteArquivos.Habilitado);
        RegistrarWidget("Monitor", MonitorSistema, MonitorSistema, () => MonitorSistema.Habilitado);
        RegistrarWidget("Bateria", Bateria, Bateria, () => Bateria.Habilitado);
        RegistrarWidget("Clima", Clima, Clima, () => Clima.Habilitado);
        RegistrarWidget("GitHub", GitHub, GitHub, () => GitHub.Habilitado);
        RegistrarWidget("Midia", Midia, Midia, () => Midia.Habilitado, () => ModoRgbMedia, observarMontado: true);
        RegistrarWidget("Teams", Teams, Teams, () => Teams.Habilitado);
        RegistrarWidget("WhatsApp", WhatsApp, WhatsApp, () => WhatsApp.Habilitado);
        RegistrarWidget("Discord", Discord, Discord, () => Discord.Habilitado);
        RegistrarWidget("OBS", Obs, Obs, () => Obs.Habilitado);
        RegistrarWidget("MascotePokemon", MascotePokemon, MascotePokemon, () => MascotePokemon.Habilitado);

        CarregarDados();
        _syncNotificacoesTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(250) };
        _syncNotificacoesTimer.Tick += async (_, _) => { _syncNotificacoesTimer.Stop(); await SincronizarNotificacoesComWindowsAsync(); };
        _syncNotificacoesTimer.Start();
        _windowTrackingService.JanelaAtivada += OnJanelaAtivada;
        _windowTrackingService.Iniciar();
    }

    public Preferencias Preferencias => _preferencias;
    public ObservableCollection<EnvironmentViewModel> Ambientes { get; }
    public ObservableCollection<AppItemViewModel> Aplicativos { get; }
    public ObservableCollection<AplicativoSegundoPlanoViewModel> AplicativosSegundoPlano { get; }
    public ObservableCollection<ColecaoAppViewModel> ColecoesGlobais { get; }
    public ObservableCollection<ColecaoAppViewModel> TodasColecoesAtivas { get; }
    public ObservableCollection<EspacadorConfig> Espacadores { get; }
    public ObservableCollection<ConfigSecaoDock> OrdemSecoes { get; }
    public ObservableCollection<WidgetInstanceRuntimeViewModel> WidgetsInstanciadosAdicionais { get; }
    public ObservableCollection<WidgetInstanceRuntimeViewModel> RelogiosAdicionais { get; }
    public ObservableCollection<WidgetInstanceRuntimeViewModel> NotasAdicionais { get; }
    public ClockWidgetViewModel Clock { get; }
    public ICommand PersonalizarWidgetCommand { get; }
    public ICommand SelecionarEstiloMidiaCommand { get; }
    public ICommand SelecionarEstiloClimaCommand { get; }
    public ICommand OcultarMidiaCommand { get; }
    private void SelecionarEstiloMidia(string? estilo)
    {
        var ambiente = AmbienteAtivo;
        var widget = ambiente?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Midia);
        if (ambiente == null || widget == null || string.IsNullOrWhiteSpace(estilo)) return;
        AplicarEstiloAmbiente(ambiente.Id, widget.Id, estilo);
    }
    private void SelecionarEstiloClima(string? estilo)
    {
        var ambiente = AmbienteAtivo;
        var widget = ambiente?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Clima);
        if (ambiente == null || widget == null || string.IsNullOrWhiteSpace(estilo)) return;
        AplicarEstiloAmbiente(ambiente.Id, widget.Id, estilo);
    }
    private void PersonalizarWidget(TipoWidget tipo)
    {
        var ambiente = AmbienteAtivo;
        var widget = ambiente?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == tipo);
        if (widget == null || ambiente == null || Application.Current == null) return;
        var picker = new Views.EstilosWidgetWindow(widget, ambiente.Nome) { Owner = Application.Current.MainWindow };
        if (picker.ShowDialog() != true || picker.EstiloSelecionado == null) return;
        AplicarEstiloAmbiente(ambiente.Id, widget.Id, picker.EstiloSelecionado);
    }
    public bool AplicarEstiloAmbiente(string ambienteId, string widgetId, string estilo)
    {
        var ambiente = Ambientes.FirstOrDefault(a => a.Id == ambienteId);
        var widget = ambiente?.WidgetsInstalados.FirstOrDefault(w => w.Id == widgetId);
        if (widget == null || !EstilosWidget.Aplicar(widget, estilo)) return false;
        if (ambiente == AmbienteAtivo) SincronizarWidgetsAmbiente(ambiente);
        SalvarPreferencias();
        return true;
    }
    public PomodoroWidgetViewModel Pomodoro { get; }
    public CalendarioWidgetViewModel Calendario { get; }
    public MidiaWidgetViewModel Midia { get; }
    public NotasWidgetViewModel Notas { get; }
    public LembreteAguaViewModel LembreteAgua { get; }
    public CotacaoMoedasViewModel CotacaoMoedas { get; }
    public AreaTransferenciaViewModel AreaTransferencia { get; }
    public ArquivosRecentesViewModel ArquivosRecentes { get; }
    public ConectividadeViewModel Conectividade { get; }
    public AudioSistemaViewModel AudioSistema { get; }
    public EstanteArquivosViewModel EstanteArquivos { get; }
    public MonitorSistemaViewModel MonitorSistema { get; }
    public GitHubWidgetViewModel GitHub { get; }
    public ClimaWidgetViewModel Clima { get; }
    public MascotePokemonViewModel MascotePokemon { get; }
    public ControlesRapidosViewModel ControlesRapidos { get; }
    public BateriaViewModel Bateria { get; }
    public WhatsAppWidgetViewModel WhatsApp { get; }
    public TeamsWidgetViewModel Teams { get; }
    public DiscordWidgetViewModel Discord { get; }
    public ObsWidgetViewModel Obs { get; } = new();
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
                    // Cada ambiente tem seus próprios apps fixados: recarrega a lista da dock
                    if (Aplicativos != null) CarregarAplicativos();
                    RecarregarTodasColecoes();
                    SalvarPreferencias();
                    OnPropertyChanged(nameof(WidgetsHabilitados));
                    OnPropertyChanged(nameof(ModoAberturaPaineis));
                }
            }
        }
    }

    public bool DockVisivel
    {
        get => _dockVisivel;
        set { if (SetProperty(ref _dockVisivel, value)) AtualizarAtividade(); }
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
    public bool EhAreia => EstiloTema == EstiloTema.Areia;
    public string FundoControlesColor => EhAreia ? "#70FFFFFF" : FundoCardColor;
    public string HoverItemColor => EhVidroLiquido ? "#35FFFFFF" : "#25FFFFFF";
    public string SeparadorColor => EhAreia ? "#40A18D72" : EstiloTema == EstiloTema.Colorido
        ? "#40A855F7" 
        : (EstiloTema == EstiloTema.ComBrilho ? "#60FFFFFF" : (EstiloTema == EstiloTema.VidroLiquido ? "#75FFFFFF" : "#28FFFFFF"));

    public bool EhVidroLiquido => EstiloTema == EstiloTema.VidroLiquido;
    public bool TemEfeitoVidro => !ModoEconomico && (EstiloTema == EstiloTema.VidroLiquido || EstiloTema == EstiloTema.ComBrilho);

    public bool WidgetsHabilitados => Clock.Habilitado || Pomodoro.Habilitado || Calendario.Habilitado ||
        Notas.Habilitado || MonitorSistema.Habilitado || LembreteAgua.Habilitado || CotacaoMoedas.Habilitado ||
        AreaTransferencia.Habilitado || ArquivosRecentes.Habilitado || Conectividade.Habilitado || AudioSistema.Habilitado || EstanteArquivos.Habilitado || WidgetsInstanciadosAdicionais.Count > 0;

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
        OnPropertyChanged(nameof(EhAreia));
        OnPropertyChanged(nameof(FundoControlesColor));
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
                OnPropertyChanged(nameof(EscalaUI));
                OnPropertyChanged(nameof(MargemDock));
                OnPropertyChanged(nameof(AlturaBarra));
                SalvarPreferencias();
            }
        }
    }

            public double EscalaUI => AlturaBarra / 64.0;

    public Thickness MargemDock => new(8, 10 + TamanhoIconeNumerico * EscalaUI * .6, 8, 4);

    public double TamanhoIconeNumerico
    {
        get
        {
            // Scale icon size proportionally to bar height: at 64px (default) -> 40px icons
            double ratio = AlturaBarra / 64.0;
            double baseSize = (double)TamanhoIcones;
            return System.Math.Round(baseSize * ratio);
        }
    }

        public double AlturaBarra
    {
        get => _preferencias.AlturaBarra;
        set
        {
            if (_preferencias.AlturaBarra != value)
            {
                _preferencias.AlturaBarra = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TamanhoIconeNumerico));
                OnPropertyChanged(nameof(EscalaUI));
                OnPropertyChanged(nameof(MargemDock));
                SalvarPreferencias();
            }
        }
    }

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

    public bool ExibirContagemColecoes
    {
        get => _preferencias.ExibirContagemColecoes;
        set
        {
            if (_preferencias.ExibirContagemColecoes != value)
            {
                _preferencias.ExibirContagemColecoes = value;
                SalvarPreferencias();
                OnPropertyChanged();
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

                public bool AbrirPlayerAoDuploClique
        {
            get => _preferencias.AbrirPlayerAoDuploClique;
            set
            {
                if (_preferencias.AbrirPlayerAoDuploClique != value)
                {
                    _preferencias.AbrirPlayerAoDuploClique = value;
                    SalvarPreferencias();
                    OnPropertyChanged();
                }
            }
        }

        public string GitHubUsuario
        {
            get => AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.GitHubContribuicoes)
                ?.ObterConfiguracao("usuario", _preferencias.GitHubUsuario) ?? _preferencias.GitHubUsuario;
            set
            {
                var normalizado = value?.Trim() ?? string.Empty;
                var widget = AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.GitHubContribuicoes);
                if (GitHubUsuario != normalizado)
                {
                    _preferencias.GitHubUsuario = normalizado;
                    widget?.DefinirConfiguracao("usuario", normalizado);
                    SalvarPreferencias();
                    OnPropertyChanged();
                    GitHub.SincronizarUsuario(normalizado);
                }
            }
        }

    public bool ExibirMidia
    {
        get => Midia.Habilitado;
        set
        {
            if (ExibirMidia != value)
            {
                if (AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Midia) is { } midia) midia.Visivel = value;
                else _preferencias.ExibirMidia = value;
                Midia.Habilitado = value;
                SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    
    public bool ModoGamerRgb
    {
        get => _preferencias.ModoGamerRgb;
        set
        {
            if (_preferencias.ModoGamerRgb == value) return;
            _preferencias.ModoGamerRgb = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GlowRgbVisivel));
            SalvarPreferencias();
        }
    }
    public bool ModoRgbMedia
    {
        get => _preferencias.ModoRgbMedia;
        set
        {
            if (_preferencias.ModoRgbMedia != value)
            {
                _preferencias.ModoRgbMedia = value;
                AtualizarAtividade();
                OnPropertyChanged();
                OnPropertyChanged(nameof(CorSombraDock));
                OnPropertyChanged(nameof(GlowRgbVisivel));
                SalvarPreferencias();
            }
        }
    }

    public System.Windows.Media.Color CorSombraDock
    {
        get
        {
            return System.Windows.Media.Colors.Black; // Usaremos o arco-íris via GlowRgbVisivel
        }
    }

    public bool GlowRgbVisivel => !ModoEconomico && !EstaEmAlerta && (ModoGamerRgb || (ModoRgbMedia && Midia != null && Midia.EstaTocando && Midia.TemMidia));
public bool AlertasVisuaisHabilitados
    {
        get => _preferencias.AlertasVisuaisHabilitados;
        set
        {
            if (_preferencias.AlertasVisuaisHabilitados != value)
            {
                _preferencias.AlertasVisuaisHabilitados = value;
                if (!value) { _alertaCancelamento?.Cancel(); _alertaCancelamento?.Dispose(); _alertaCancelamento = null; AlertaChamada = false; OnPropertyChanged(nameof(AlertaChamada)); EstaEmAlerta = false; }
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public string ModoAberturaPaineis
    {
        get => AmbienteAtivo?.Model.ModoAberturaPaineis == "Mouse" ? "Mouse" : "Clique";
        set
        {
            if (AmbienteAtivo == null) return;
            var modo = value == "Mouse" ? "Mouse" : "Clique";
            if (ModoAberturaPaineis == modo) return;
            AmbienteAtivo.Model.ModoAberturaPaineis = modo;
            OnPropertyChanged();
            SalvarPreferencias();
        }
    }
    public bool PreviaJanelas
    {
        get => _preferencias.PreviaJanelas;
        set
        {
            if (_preferencias.PreviaJanelas == value) return;
            _preferencias.PreviaJanelas = value;
            OnPropertyChanged();
            SalvarPreferencias();
        }
    }

    public int AtrasoAbrirPreviaMs { get => _preferencias.AtrasoAbrirPreviaMs; set { var v = Math.Clamp(value, 0, 5000); if (_preferencias.AtrasoAbrirPreviaMs == v) return; _preferencias.AtrasoAbrirPreviaMs = v; OnPropertyChanged(); SalvarPreferencias(); } }
    public int AtrasoFecharPreviaMs { get => _preferencias.AtrasoFecharPreviaMs; set { var v = Math.Clamp(value, 1, 5000); if (_preferencias.AtrasoFecharPreviaMs == v) return; _preferencias.AtrasoFecharPreviaMs = v; OnPropertyChanged(); SalvarPreferencias(); } }
    public int AtrasoAposRemoverPreviaMs { get => _preferencias.AtrasoAposRemoverPreviaMs; set { var v = Math.Clamp(value, 0, 5000); if (_preferencias.AtrasoAposRemoverPreviaMs == v) return; _preferencias.AtrasoAposRemoverPreviaMs = v; OnPropertyChanged(); SalvarPreferencias(); } }
    public bool FecharPreviaAoClicarFora { get => _preferencias.FecharPreviaAoClicarFora; set { if (_preferencias.FecharPreviaAoClicarFora == value) return; _preferencias.FecharPreviaAoClicarFora = value; OnPropertyChanged(); SalvarPreferencias(); } }
    public bool ClimaExpandido
    {
        get => AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Clima) is { } clima ? EstilosWidget.Resolver(clima, climaLegado: _preferencias.ClimaExpandido) == "detalhado" : _preferencias.ClimaExpandido;
        set
        {
            if (ClimaExpandido == value) return;
            if (AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Clima) is { } clima)
            {
                EstilosWidget.Aplicar(clima, value ? "detalhado" : "compacto");
                Clima.Estilo = clima.Estilo;
            }
            else _preferencias.ClimaExpandido = value;
            OnPropertyChanged();
            SalvarPreferencias();
        }
    }
    public bool RelogioAnalogico
    {
        get => Clock.Estilo.StartsWith("analogico-", StringComparison.Ordinal);
        set
        {
            if (RelogioAnalogico == value) return;
            if (AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Relogio) is { } relogio)
                EstilosWidget.Aplicar(relogio, value ? "analogico-digital" : "hora-data");
            Clock.Estilo = value ? "analogico-digital" : "hora-data";
            OnPropertyChanged();
            SalvarPreferencias();
        }
    }
    public bool PreviaPastas
    {
        get => _preferencias.PreviaPastas;
        set
        {
            if (_preferencias.PreviaPastas == value) return;
            _preferencias.PreviaPastas = value;
            OnPropertyChanged();
            SalvarPreferencias();
        }
    }
public bool ExibirBateria
    {
        get => Bateria.Habilitado;
        set
        {
            if (ExibirBateria == value) return;
            if (AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Bateria) is { } bateria) bateria.Visivel = value;
            else _preferencias.ExibirBateria = value;
            Bateria.Habilitado = value;
            OnPropertyChanged();
            SalvarPreferencias();
        }
    }

public bool ExibirLixeira
    {
        get => _preferencias.ExibirLixeira;
        set
        {
            if (_preferencias.ExibirLixeira != value)
            {
                if (!_lixeiraDesktop.ConfigurarVisibilidade(!value, out var erro))
                {
                    OnPropertyChanged();
                    MostrarAlerta?.Invoke("Lixeira", erro ?? "Não foi possível atualizar a Lixeira na área de trabalho.");
                    return;
                }
                _preferencias.ExibirLixeira = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool ExibirAppsAbertosNaoFixados
    {
        get => _preferencias.ExibirAppsAbertosNaoFixados;
        set
        {
            if (_preferencias.ExibirAppsAbertosNaoFixados != value)
            {
                _preferencias.ExibirAppsAbertosNaoFixados = value;
                OnPropertyChanged();
                SalvarPreferencias();
                AtualizarAplicativosAbertos();
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
        get => AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Clima)
            ?.ObterConfiguracao("localizacao", _preferencias.LocalizacaoClima) ?? _preferencias.LocalizacaoClima;
        set
        {
            var normalizado = value?.Trim() ?? string.Empty;
            var widget = AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Clima);
            if (widget == null || widget.ObterConfiguracao("localizacao", _preferencias.LocalizacaoClima) == normalizado) return;
            widget.DefinirConfiguracao("localizacao", normalizado);
            OnPropertyChanged();
            SalvarPreferencias();
            Clima.SincronizarLocalizacao(normalizado);
        }
    }

    public string UrlIcalAmbiente
    {
        get => AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.CalendarioCompromissos)
            ?.ObterConfiguracao("urlIcal", _preferencias.UrlIcal) ?? _preferencias.UrlIcal;
        set
        {
            var widget = AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.CalendarioCompromissos);
            if (widget == null) return;
            widget.DefinirConfiguracao("urlIcal", value?.Trim() ?? string.Empty);
            SalvarPreferencias();
            Calendario.SincronizarUrlIcal(value ?? string.Empty);
        }
    }

    public void SalvarCompromissosAmbiente(IEnumerable<CompromissoLocal> compromissos)
    {
        var lista = compromissos.ToList();
        var widget = AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.CalendarioCompromissos);
        if (widget == null) return;
        widget.DefinirConfiguracao("compromissos", System.Text.Json.JsonSerializer.Serialize(lista));
        Calendario.SincronizarCompromissos(lista);
        SalvarPreferencias();
    }

    public IReadOnlyList<CompromissoLocal> ObterCompromissosAmbiente()
    {
        var json = AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.CalendarioCompromissos)
            ?.ObterConfiguracao("compromissos");
        if (!string.IsNullOrWhiteSpace(json))
            try { return System.Text.Json.JsonSerializer.Deserialize<List<CompromissoLocal>>(json) ?? new(); } catch { }
        return _preferencias.CompromissosLocais;
    }

    public bool DesativarAnimacoes
    {
        get => _preferencias.DesativarAnimacoes;
        set
        {
            if (_preferencias.DesativarAnimacoes != value)
            {
                _preferencias.DesativarAnimacoes = value;
                AtualizarAtividade();
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool ModoEconomico
    {
        get => _preferencias.ModoEconomico;
        set
        {
            if (_preferencias.ModoEconomico == value) return;
            _preferencias.ModoEconomico = value;
            MonitorSistema.ModoEconomico = value;
            Bateria.ModoEconomico = value;
            Midia.ModoEconomico = value;
            IconExtractionService.DefinirModoEconomico(value);
            if (_windowTrackingService is Win32WindowTrackingService rastreamento) rastreamento.DefinirModoEconomico(value);
            AtualizarAtividade();
            OnPropertyChanged();
            OnPropertyChanged(nameof(AnimacoesAtivas));
            OnPropertyChanged(nameof(TemEfeitoVidro));
            OnPropertyChanged(nameof(GlowRgbVisivel));
            OnPropertyChanged(nameof(SombraDockOpacity));
            SalvarPreferencias();
        }
    }

    public double SombraDockOpacity => ModoEconomico ? 0.16 : 0.55;

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
    public ICommand AdicionarColecaoGlobalCommand { get; }
    public ICommand AdicionarAppPermanenteCommand { get; }
    public ICommand AbrirConfiguracoesCommand { get; }
    public ICommand AbrirPersonalizarCommand { get; }
    public ICommand AbrirAjustesCommand { get; }
        public ICommand RestaurarBarraWindowsCommand { get; }
    public ICommand ToggleBarraNativaCommand { get; }
    public ICommand AbrirMenuIniciarCommand { get; }
    public ICommand AbrirIniciarNativoWindowsCommand { get; }
    public ICommand AbrirPesquisaCommand { get; }
    public ICommand AbrirExploradorCommand { get; }
    public ICommand AlternarVisibilidadeCommand { get; }
    public ICommand SairCommand { get; }
    public ICommand AbrirLixeiraCommand { get; }
    public ICommand AbrirBandejaOcultaCommand { get; }
    public ICommand AbrirBandejaNativaCommand { get; }

    private bool _painelAppsSegundoPlanoAberto;
    private string _bandejaMensagem = "";
    public string BandejaMensagem { get => _bandejaMensagem; private set => SetProperty(ref _bandejaMensagem, value); }
    private bool _bandejaAvisoAberto;
    public bool BandejaAvisoAberto { get => _bandejaAvisoAberto; set => SetProperty(ref _bandejaAvisoAberto, value); }
    private bool _abrindoBandejaWindows;
    private bool _bandejaEspelhadaAberta;
    public bool BandejaEspelhadaAberta { get => _bandejaEspelhadaAberta; set => SetProperty(ref _bandejaEspelhadaAberta, value); }
    public ObservableCollection<AplicativoSegundoPlanoViewModel> IconesBandejaEspelhada { get; } = new();
    private async System.Threading.Tasks.Task AbrirBandejaNativaAsync()
    {
        if (_disposed || _abrindoBandejaWindows) return;
        _abrindoBandejaWindows = true;
        BandejaEspelhadaAberta = false;
        BandejaAvisoAberto = false;
        try
        {
            var result = await new WindowsTrayController(_taskbarService, new WindowsTrayService())
                .OpenAsync(_preferencias.UsarComoBarraPrincipal, _preferencias.EstadoAnteriorBarraTarefas);
            if (_disposed) return;
            if (result.TaskbarRestored) _barraNativaVisivelTemporariamente = true;
            BandejaMensagem = result.Message;
            BandejaAvisoAberto = !result.Opened;
        }
        finally { _abrindoBandejaWindows = false; }
    }
    private async System.Threading.Tasks.Task AbrirBandejaWindowsAsync()
    {
        if (_disposed || _abrindoBandejaWindows) return;
        var restoreSession = new TrayTaskbarRestoreSession(_taskbarService,
            _preferencias.UsarComoBarraPrincipal && !_barraNativaVisivelTemporariamente);
        _abrindoBandejaWindows = true;
        PainelAppsSegundoPlanoAberto = false;
        BandejaEspelhadaAberta = false;
        BandejaAvisoAberto = false;
        try
        {
            var result = await new WindowsTrayController(_taskbarService, new WindowsTrayService())
                .OpenAsync(_preferencias.UsarComoBarraPrincipal && !_barraNativaVisivelTemporariamente, _preferencias.EstadoAnteriorBarraTarefas);
            restoreSession.MarkShown(result.TaskbarRestored);
            if (_disposed) return;
            if (result.TaskbarRestored) _barraNativaVisivelTemporariamente = true;
            BandejaMensagem = result.Message;
            BandejaAvisoAberto = !result.Opened;
            if (result.Opened)
            {
                await System.Threading.Tasks.Task.Delay(250);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var items = await System.Threading.Tasks.Task.Run(() => new WindowsTrayMirror().Read(timeout.Token)).WaitAsync(timeout.Token);
                if (_disposed) return;
                // O foco normalmente dispensa o flyout do Explorer sem alternar a seta novamente.
                FocarDockParaBandeja?.Invoke();
                await System.Threading.Tasks.Task.Delay(100, timeout.Token);
                var closed = await System.Threading.Tasks.Task.Run(() => new WindowsTrayMirror().CloseAsync(timeout.Token)).WaitAsync(timeout.Token);
                if (!closed)
                {
                    BandejaMensagem = "A bandeja do Windows permaneceu aberta. O painel da dock não será mostrado junto dela.";
                    BandejaAvisoAberto = true;
                    return;
                }
                if (_disposed) return;
                if (_preferencias.UsarComoBarraPrincipal && restoreSession.NeedsRestore)
                {
                    if (!restoreSession.Restore()) throw new InvalidOperationException();
                    _barraNativaVisivelTemporariamente = false;
                    // Explorer pode reagir ao fechamento do flyout depois do primeiro Hide.
                    await System.Threading.Tasks.Task.Delay(250, timeout.Token);
                    if (_disposed) return;
                    if (_preferencias.UsarComoBarraPrincipal && !_barraNativaVisivelTemporariamente)
                        _taskbarService.GarantirBarraOculta();
                }
                if (items.Count == 0)
                {
                    BandejaMensagem = "N\u00e3o foi poss\u00edvel ler os \u00edcones. Tente novamente ou abra a bandeja nativa explicitamente.";
                    BandejaAvisoAberto = true;
                    return;
                }
                IconesBandejaEspelhada.Clear();
                foreach (var item in items)
                    IconesBandejaEspelhada.Add(new(item.Name, "", false, item.Image,
                        () => _ = InvocarIconeEspelhadoAsync(item.Name, item.CanInvoke)));
                BandejaEspelhadaAberta = true;
            }
        }
        catch (Exception)
        {
            if (!_disposed) { BandejaMensagem = "Abra a bandeja pela barra do Windows."; BandejaAvisoAberto = true; }
        }
        finally
        {
            try
            {
                if (!_disposed && _preferencias.UsarComoBarraPrincipal && restoreSession.NeedsRestore)
                {
                    using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    try { await System.Threading.Tasks.Task.Run(() => new WindowsTrayMirror().CloseAsync(cleanupTimeout.Token)).WaitAsync(cleanupTimeout.Token); }
                    catch { }
                    if (restoreSession.Restore()) _barraNativaVisivelTemporariamente = false;
                    else
                    {
                        BandejaEspelhadaAberta = false;
                        BandejaMensagem = "N\u00e3o foi poss\u00edvel voltar a ocultar a barra do Windows. Use Mostrar barra do Windows para ajustar.";
                        BandejaAvisoAberto = true;
                    }
                }
            }
            finally { _abrindoBandejaWindows = false; }
        }
    }

    private async System.Threading.Tasks.Task InvocarIconeEspelhadoAsync(string name, bool supported)
    {
        if (_disposed || _abrindoBandejaWindows) return;
        BandejaEspelhadaAberta = false;
        if (!supported)
        {
            BandejaMensagem = "Este ícone não oferece clique pela acessibilidade do Windows. Use a bandeja nativa.";
            BandejaAvisoAberto = true;
            return;
        }
        var restoreSession = new TrayTaskbarRestoreSession(_taskbarService,
            _preferencias.UsarComoBarraPrincipal && !_barraNativaVisivelTemporariamente);
        _abrindoBandejaWindows = true;
        try
        {
            var result = await new WindowsTrayController(_taskbarService, new WindowsTrayService())
                .OpenAsync(_preferencias.UsarComoBarraPrincipal && !_barraNativaVisivelTemporariamente, _preferencias.EstadoAnteriorBarraTarefas);
            restoreSession.MarkShown(result.TaskbarRestored);
            if (_disposed) return;
            if (result.TaskbarRestored) _barraNativaVisivelTemporariamente = true;
            if (!result.Opened) throw new InvalidOperationException();
            await System.Threading.Tasks.Task.Delay(200);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var invoked = await System.Threading.Tasks.Task.Run(() => new WindowsTrayMirror().Invoke(name, timeout.Token)).WaitAsync(timeout.Token);
            if (!invoked) throw new InvalidOperationException();
        }
        catch (Exception)
        {
            BandejaMensagem = "Não foi possível acionar esse ícone. Use a bandeja nativa; o aplicativo não foi encerrado nem reiniciado.";
            BandejaAvisoAberto = true;
        }
        finally
        {
            try
            {
                if (!_disposed && _preferencias.UsarComoBarraPrincipal && restoreSession.NeedsRestore)
                {
                    using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    try { await System.Threading.Tasks.Task.Run(() => new WindowsTrayMirror().CloseAsync(cleanupTimeout.Token)).WaitAsync(cleanupTimeout.Token); }
                    catch { }
                    if (restoreSession.Restore()) _barraNativaVisivelTemporariamente = false;
                    else
                    {
                        BandejaEspelhadaAberta = false;
                        BandejaMensagem = "N\u00e3o foi poss\u00edvel voltar a ocultar a barra do Windows. Use Mostrar barra do Windows para ajustar.";
                        BandejaAvisoAberto = true;
                    }
                }
            }
            finally { _abrindoBandejaWindows = false; }
        }
    }
    public bool PainelAppsSegundoPlanoAberto
    {
        get => _painelAppsSegundoPlanoAberto;
        set
        {
            if (!SetProperty(ref _painelAppsSegundoPlanoAberto, value) || !value) return;
            _ = AtualizarAplicativosSegundoPlanoAsync();
        }
    }

    private bool _carregandoAppsSegundoPlano;
    public bool CarregandoAppsSegundoPlano
    {
        get => _carregandoAppsSegundoPlano;
        private set => SetProperty(ref _carregandoAppsSegundoPlano, value);
    }

    private async System.Threading.Tasks.Task AtualizarAplicativosSegundoPlanoAsync()
    {
        try { await AtualizarAplicativosSegundoPlanoCoreAsync(); }
        catch (Exception ex) { RegistrarFalhaNaoFatal("aplicativos-segundo-plano.log", ex); }
    }

    private async System.Threading.Tasks.Task AtualizarAplicativosSegundoPlanoCoreAsync()
    {
        if (_disposed || CarregandoAppsSegundoPlano) return;
        CarregandoAppsSegundoPlano = true;
        try
        {
            try { await InstalledAppsScanner.ListarAsync(); } catch { }
            var encontrados = await System.Threading.Tasks.Task.Run(_aplicativosSegundoPlanoService.ObterAplicativos);
            if (_disposed || !PainelAppsSegundoPlanoAberto) return;

            AplicativosSegundoPlano.Clear();
            foreach (var info in encontrados)
            {
                var icone = info.JanelaPrincipal != IntPtr.Zero ? _iconService.ObterIconeJanela(info.JanelaPrincipal) : null;
                icone ??= _iconService.ObterIcone(info.ReferenciaIcone ?? info.CaminhoExecutavel, TipoItem.Aplicativo);
                icone ??= _iconService.ObterIcone(info.CaminhoExecutavel, TipoItem.Aplicativo);
                if (icone == null) continue;
                AplicativosSegundoPlano.Add(new AplicativoSegundoPlanoViewModel(
                    info.Nome, info.CaminhoExecutavel, info.PossuiJanela, icone,
                    () => _ = AbrirAplicativoSegundoPlanoAsync(info)));
            }
        }
        finally { CarregandoAppsSegundoPlano = false; }
    }

    private bool _abrindoAplicativoSegundoPlano;
    private async System.Threading.Tasks.Task AbrirAplicativoSegundoPlanoAsync(AplicativoSegundoPlanoInfo info)
    {
        if (_disposed || _abrindoAplicativoSegundoPlano) return;
        _abrindoAplicativoSegundoPlano = true;
        PainelAppsSegundoPlanoAberto = false;
        try
        {
            // Liberar o popup/captura do mouse antes de ativar uma janela externa.
            await System.Threading.Tasks.Task.Yield();
            if (_disposed) return;
            var activated = await System.Threading.Tasks.Task.Run(() => _windowTrackingService.AtivarAplicativoEmExecucao(info.CaminhoExecutavel));
            if (!_disposed && !activated)
                MostrarAlerta?.Invoke("Abra pelo ícone na bandeja do Windows",
                    $"{info.Nome} não disponibilizou uma janela que possa ser ativada com segurança. Use o ícone do próprio aplicativo na bandeja do Windows. Se ele já foi encerrado, abra pelo atalho fixado.");
        }
        catch (Exception ex)
        {
            RegistrarFalhaNaoFatal("aplicativos-segundo-plano.log", ex);
            if (!_disposed) MostrarAlerta?.Invoke("Não foi possível mostrar o aplicativo", "Abra pelo ícone do próprio aplicativo na bandeja do Windows.");
        }
        finally { _abrindoAplicativoSegundoPlano = false; }
    }

    public Action? FocarBuscaLaunchpad;
    public Action? AtivarJanelaPrincipal;
    public Action? FocarDockParaBandeja;

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
                    _ = CarregarCatalogoLaunchpadAsync();
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
        if (!DockVisivel || OcultoPorTelaCheia) ForcarExibicao();
        else DockVisivel = false;
    }

    public void ForcarExibicao()
    {
        OcultoPorTelaCheia = false;
        DockVisivel = true;
    }

    private static void RegistrarFalhaNaoFatal(string nome, Exception ex)
    {
        try
        {
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "logs");
            Directory.CreateDirectory(pasta);
            File.AppendAllText(Path.Combine(pasta, nome), $"{DateTimeOffset.Now:O}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
    }

        private bool _barraNativaVisivelTemporariamente = false;
    public void ToggleBarraNativa()
    {
        if (!_preferencias.UsarComoBarraPrincipal) return;

        _barraNativaVisivelTemporariamente = !_barraNativaVisivelTemporariamente;
        if (_barraNativaVisivelTemporariamente)
        {
            _taskbarService.RestaurarBarraNativa(_preferencias.EstadoAnteriorBarraTarefas);
        }
        else
        {
            _taskbarService.OcultarBarraNativa(out var ignoreState);
        }
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

    public DecoracaoDock DecoracaoDock
    {
        get => Preferencias.DecoracaoDock;
        set
        {
            if (!Enum.IsDefined(value) || Preferencias.DecoracaoDock == value) return;
            var previous = Preferencias.DecoracaoDock; Preferencias.DecoracaoDock = value;
            if (!TentarSalvarPreferencias()) Preferencias.DecoracaoDock = previous;
            OnPropertyChanged();
        }
    }
    public bool TarefasHabilitadas => Preferencias.WidgetsGlobais.Concat(AmbienteAtivo?.Model.WidgetsInstalados ?? []).Any(w => w.Tipo == TipoWidget.Tarefas && w.Visivel);

    public void SalvarPreferencias() => TentarSalvarPreferencias();

    public bool TentarSalvarPreferencias()
    {
        try
        {
            _preferencias.Ambientes = Ambientes.Select(a => a.Model).ToList();
            _preferencias.OrdemSecoes = OrdemSecoes.ToList();
            _preferencias.ColecoesGlobais = ColecoesGlobais.Select((c, idx) => { c.Model.Ordem = idx; return c.Model; }).ToList();
            _preferencias.Espacadores = Espacadores.ToList();
            _repository.Salvar(_preferencias);
            return true;
        }
        catch (Exception ex)
        {
            MostrarAlerta?.Invoke("Erro ao Salvar", $"Falha ao salvar preferÃªncias: {ex.Message}");
            return false;
        }
    }

    public void AtualizarPreferencias(Preferencias novasPrefs)
    {
        _preferencias = novasPrefs;
        CarregarDados();
        AgendarNotificacoes();
        AtualizarAtividade();
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

        // Pré-carrega aplicativos instalados do sistema em background para a busca ser rápida


        Calendario.SincronizarCompromissos(ObterCompromissosAmbiente());
        Calendario.SincronizarUrlIcal(UrlIcalAmbiente);
        Clima.SincronizarLocalizacao(LocalizacaoClima);

        AmbienteAtivo = Ambientes.FirstOrDefault(a => a.Id == _preferencias.AmbienteAtivoId)
                     ?? Ambientes.FirstOrDefault();

        AtualizarCoresTema();
        OnPropertyChanged(nameof(UsarComoBarraPrincipal));
        OnPropertyChanged(nameof(ExibirSeletorAmbientes));
        OnPropertyChanged(nameof(ExibirItensFixados));
        OnPropertyChanged(nameof(ExibirBotoesAcao));
        OnPropertyChanged(nameof(ExibirClima));
        OnPropertyChanged(nameof(ExibirLixeira));
        Bateria.Habilitado = AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Bateria)?.Visivel ?? false;
        OnPropertyChanged(nameof(ExibirBateria));
        OnPropertyChanged(nameof(PreviaJanelas));
        OnPropertyChanged(nameof(ClimaExpandido));
        OnPropertyChanged(nameof(RelogioAnalogico));
        OnPropertyChanged(nameof(PreviaPastas));
        OnPropertyChanged(nameof(DesativarAnimacoes));
        MonitorSistema.ModoEconomico = _preferencias.ModoEconomico;
        Bateria.ModoEconomico = _preferencias.ModoEconomico;
        Midia.ModoEconomico = _preferencias.ModoEconomico;
        IconExtractionService.DefinirModoEconomico(_preferencias.ModoEconomico);
        if (_windowTrackingService is Win32WindowTrackingService rastreamento) rastreamento.DefinirModoEconomico(_preferencias.ModoEconomico);
        OnPropertyChanged(nameof(ModoEconomico));
        OnPropertyChanged(nameof(ModoGamerRgb));
        OnPropertyChanged(nameof(ModoRgbMedia));
        OnPropertyChanged(nameof(GlowRgbVisivel));
        OnPropertyChanged(nameof(SombraDockOpacity));
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
            var vm = new ColecaoAppViewModel(col, _launcher, _iconService, onEditarColecao: c => AbrirAjustes("Ambientes"), notificarErro: msg => MostrarAlerta?.Invoke("Erro", msg), onRemoverColecao: RemoverColecao);
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
        ControlesRapidos.Carregar(amb.Model);
        var migrarWidgets = !amb.Model.WidgetsSistemaMigrados;
        if (!amb.Model.WidgetsSistemaMigrados && !amb.WidgetsInstalados.Any(w => w.Tipo == TipoWidget.Midia))
            amb.WidgetsInstalados.Add(new WidgetInstanceConfig { Tipo = TipoWidget.Midia, Nome = "Mídia", Estilo = "capa", Visivel = _preferencias.ExibirMidia, Ordem = amb.WidgetsInstalados.Count });
        if (!amb.Model.WidgetsSistemaMigrados && !amb.WidgetsInstalados.Any(w => w.Tipo == TipoWidget.Bateria))
            amb.WidgetsInstalados.Add(new WidgetInstanceConfig { Tipo = TipoWidget.Bateria, Nome = "Bateria", Estilo = "compacto", Visivel = _preferencias.ExibirBateria, Ordem = amb.WidgetsInstalados.Count });
        amb.Model.WidgetsSistemaMigrados = true;

                var def = Preferencias.CriarWidgetsPadrao();
        if (migrarWidgets)
        {
            // Migração para usuários existentes: adiciona widgets novos que faltam
            foreach (var wDef in def)
            {
                if (!amb.WidgetsInstalados.Any(w => w.Tipo == wDef.Tipo))
                {
                    amb.WidgetsInstalados.Add(wDef);
                }
            }
        }

        foreach (var widget in amb.WidgetsInstalados)
            if (string.IsNullOrEmpty(widget.Estilo))
                widget.Estilo = EstilosWidget.Resolver(widget, _preferencias.RelogioAnalogico, _preferencias.ClimaExpandido);

        if (Clock.EstaExecutando && !Ambientes.Any(a => a.WidgetsInstalados.Any(w => w.Tipo == TipoWidget.Relogio))) Clock.ReiniciarControleCommand.Execute(null);
        var wRelogio = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Relogio);
        Clock.Habilitado = wRelogio?.Visivel ?? false;
        if (wRelogio != null) { Clock.Formato = wRelogio.Formato; Clock.Estilo = EstilosWidget.Resolver(wRelogio, _preferencias.RelogioAnalogico); Clock.FusoHorarioId = wRelogio.ObterConfiguracao("fusoHorarioId"); }
        OnPropertyChanged(nameof(RelogioAnalogico));
        OnPropertyChanged(nameof(ClimaExpandido));

        if (Pomodoro.EstaExecutando && !Ambientes.Any(a => a.WidgetsInstalados.Any(w => w.Tipo == TipoWidget.Pomodoro)))
            Pomodoro.ReiniciarCommand.Execute(null);
        var wPomodoro = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Pomodoro);
        Pomodoro.Habilitado = wPomodoro?.Visivel ?? false;
        if (wPomodoro != null) Pomodoro.Formato = wPomodoro.Formato;

        var wCalendario = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.CalendarioCompromissos);
        Calendario.Habilitado = wCalendario?.Visivel ?? false;
        if (wCalendario != null) { Calendario.Formato = wCalendario.Formato; Calendario.Estilo = EstilosWidget.Resolver(wCalendario); }

        var wNotas = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Notas);
        Notas.Habilitado = wNotas?.Visivel ?? false;
        if (wNotas != null) { Notas.Formato = wNotas.Formato; Notas.SincronizarInstancia($"{amb.Id}-{wNotas.Id}"); }

        var wAgua = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.LembreteAgua);
        LembreteAgua.Habilitado = wAgua?.Visivel ?? false;
        if (wAgua != null)
        {
            _ = int.TryParse(wAgua.ObterConfiguracao("intervaloMinutos", "60"), out var intervalo);
            _ = int.TryParse(wAgua.ObterConfiguracao("horaInicio", "8"), out var inicio);
            _ = int.TryParse(wAgua.ObterConfiguracao("horaFim", "22"), out var fim);
            LembreteAgua.Carregar(intervalo <= 0 ? 60 : intervalo, inicio, fim <= 0 ? 22 : fim);
        }
        var wCotacao = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.CotacaoMoedas);
        CotacaoMoedas.Habilitado = wCotacao?.Visivel ?? false;
        if (wCotacao != null) CotacaoMoedas.Configurar(wCotacao.ObterConfiguracao("base", "USD"), wCotacao.ObterConfiguracao("destino", "BRL"));
        var wClipboard = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.AreaTransferencia);
        AreaTransferencia.Habilitado = wClipboard?.Visivel ?? false;
        var wArquivos = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.ArquivosRecentes);
        ArquivosRecentes.Habilitado = wArquivos?.Visivel ?? false;
        var wConectividade = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Conectividade);
        Conectividade.Habilitado = wConectividade?.Visivel ?? false;
        var wAudio = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.AudioSistema);
        AudioSistema.Habilitado = wAudio?.Visivel ?? false;
        var wEstante = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.EstanteArquivos);
        EstanteArquivos.Habilitado = wEstante?.Visivel ?? false;
        if (wEstante != null) EstanteArquivos.Configurar(wEstante.ObterConfiguracao("arquivos", "[]"));

                var wMonitor = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.MonitorSistema);
        if (wMonitor != null)
        {
            MonitorSistema.Habilitado = wMonitor.Visivel;
            MonitorSistema.Formato = wMonitor.Formato;
            MonitorSistema.Estilo = EstilosWidget.Resolver(wMonitor);
        }
        else MonitorSistema.Habilitado = false;

        var wMidia = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Midia);
        Midia.Habilitado = wMidia?.Visivel ?? false;
        Midia.Estilo = wMidia == null ? "capa" : EstilosWidget.Resolver(wMidia);
        var wBateria = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Bateria);
        Bateria.Habilitado = wBateria?.Visivel ?? false;
        Bateria.Estilo = wBateria == null ? "compacto" : EstilosWidget.Resolver(wBateria);
        OnPropertyChanged(nameof(ExibirMidia));
        OnPropertyChanged(nameof(ExibirBateria));
        amb.Model.WidgetsInstalados = amb.WidgetsInstalados.ToList();

                var wGitHub = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.GitHubContribuicoes);
        if (wGitHub != null)
        {
            GitHub.Habilitado = wGitHub.Visivel;
            GitHub.Formato = wGitHub.Formato;
            GitHub.Estilo = EstilosWidget.Resolver(wGitHub);
            GitHub.SincronizarUsuario(wGitHub.ObterConfiguracao("usuario", _preferencias.GitHubUsuario));
            GitHub.SelecionarAnimacao(wGitHub.ObterConfiguracao("animacao", _preferencias.GitHubAnimacao));
        }
        else 
        {
            GitHub.Habilitado = false;
        }
        var wMascote = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.MascotePokemon);
        if (wMascote != null)
        {
            MascotePokemon.Habilitado = wMascote.Visivel;
            _ = MascotePokemon.SelecionarAsync(int.TryParse(wMascote.ObterConfiguracao("pokemonId", "1"), out var pokemonId) ? Math.Clamp(pokemonId, 1, 151) : 1);
        }
        else MascotePokemon.Habilitado = false;

                var wClima = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.Clima);
        if (wClima != null) { Clima.Habilitado = wClima.Visivel; Clima.Formato = wClima.Formato; Clima.Estilo = EstilosWidget.Resolver(wClima, climaLegado: _preferencias.ClimaExpandido); } else { Clima.Habilitado = false; }

        var wWhatsApp = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.WhatsAppNotificacoes);
        if (wWhatsApp != null) { WhatsApp.Habilitado = wWhatsApp.Visivel; WhatsApp.Formato = wWhatsApp.Formato; } else { WhatsApp.Habilitado = false; }

        var wTeams = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.TeamsStatus);
        if (wTeams != null) { Teams.Habilitado = wTeams.Visivel; Teams.Formato = wTeams.Formato; } else { Teams.Habilitado = false; }

        var wDiscord = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.DiscordVoz);
        if (wDiscord != null) { Discord.Habilitado = wDiscord.Visivel; Discord.Formato = wDiscord.Formato; } else { Discord.Habilitado = false; }

        var wObs = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.OBSStudio);
        if (wObs != null) { Obs.Habilitado = wObs.Visivel; } else { Obs.Habilitado = false; }
        foreach (var (id, tipo) in new[] { ("Relogio", TipoWidget.Relogio), ("Pomodoro", TipoWidget.Pomodoro),
            ("Calendario", TipoWidget.CalendarioCompromissos), ("Notas", TipoWidget.Notas),
            ("Monitor", TipoWidget.MonitorSistema), ("Bateria", TipoWidget.Bateria), ("Clima", TipoWidget.Clima),
            ("GitHub", TipoWidget.GitHubContribuicoes), ("Midia", TipoWidget.Midia), ("Teams", TipoWidget.TeamsStatus),
            ("WhatsApp", TipoWidget.WhatsAppNotificacoes), ("Discord", TipoWidget.DiscordVoz), ("OBS", TipoWidget.OBSStudio),
            ("LembreteAgua", TipoWidget.LembreteAgua), ("CotacaoMoedas", TipoWidget.CotacaoMoedas),
            ("AreaTransferencia", TipoWidget.AreaTransferencia), ("ArquivosRecentes", TipoWidget.ArquivosRecentes),
            ("Conectividade", TipoWidget.Conectividade), ("AudioSistema", TipoWidget.AudioSistema),
            ("EstanteArquivos", TipoWidget.EstanteArquivos) })
            Atividade.DefinirInstalado(id, Ambientes.Any(a => a.WidgetsInstalados.Any(w => w.Tipo == tipo)));
        Calendario.SincronizarCompromissos(ObterCompromissosAmbiente());
        Calendario.SincronizarUrlIcal(UrlIcalAmbiente);
        Clima.SincronizarLocalizacao(LocalizacaoClima);
        SincronizarWidgetsInstanciadosAdicionais(amb);
        OnPropertyChanged(nameof(TarefasHabilitadas));
    }

    private void SalvarEstanteArquivos(string json)
    {
        var widget = AmbienteAtivo?.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.EstanteArquivos);
        if (widget == null) return;
        widget.DefinirConfiguracao("arquivos", json);
        SalvarPreferencias();
    }

    private void SincronizarWidgetsInstanciadosAdicionais(EnvironmentViewModel ambiente)
    {
        LimparWidgetsInstanciadosAdicionais();

        foreach (var config in ambiente.WidgetsInstalados
                     .Where(w => w.Visivel && WidgetCapabilities.PermiteMultiplasInstancias(w.Tipo))
                     .GroupBy(w => w.Tipo)
                     .SelectMany(grupo => grupo.OrderBy(w => w.Ordem).Skip(1)))
        {
            try
            {
                var runtime = new WidgetInstanceRuntimeViewModel(config, ambiente.Id);
                WidgetsInstanciadosAdicionais.Add(runtime);
                if (runtime.EhRelogio) RelogiosAdicionais.Add(runtime);
                if (runtime.EhNotas) NotasAdicionais.Add(runtime);
                if (VisualAtivo) runtime.Ativar(true);
            }
            catch (NotSupportedException)
            {
                // A loja só libera duplicação depois que o tipo recebe runtime próprio.
            }
        }

        OnPropertyChanged(nameof(WidgetsHabilitados));
    }

    private void LimparWidgetsInstanciadosAdicionais()
    {
        foreach (var runtime in WidgetsInstanciadosAdicionais) runtime.Dispose();
        WidgetsInstanciadosAdicionais.Clear();
        RelogiosAdicionais.Clear();
        NotasAdicionais.Clear();
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
        if (AmbienteAtivo != null)
        {
            var itens = _preferencias.AppsPermanentes.Concat(AmbienteAtivo.Model.Itens).ToList();
            string Chave(ItemFixado item)
            {
                if (item.Tipo != TipoItem.Aplicativo) return item.CaminhoOuUrl;
                var caminho = item.CaminhoOuUrl;
                if (Path.GetFileName(caminho) == caminho)
                {
                    var explicitos = itens.Where(i => i.Tipo == TipoItem.Aplicativo && Path.IsPathRooted(i.CaminhoOuUrl)
                        && string.Equals(Path.GetFileName(i.CaminhoOuUrl), caminho, StringComparison.OrdinalIgnoreCase))
                        .Select(i => i.CaminhoOuUrl).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                    if (explicitos.Length == 1) return explicitos[0];
                }
                return IconExtractionService.ResolverCaminhoCompleto(caminho);
            }
            var lista = itens.GroupBy(Chave, StringComparer.OrdinalIgnoreCase).Select(g => g.First());
            var savedOrder = (AmbienteAtivo.Model.OrdemAplicativosDock ?? []).Where(id => !string.IsNullOrWhiteSpace(id)).Select((id, index) => (id, index))
                .GroupBy(entry => entry.id).ToDictionary(group => group.Key, group => group.First().index);
            foreach (var item in lista.OrderBy(a => savedOrder.GetValueOrDefault(a.Id, int.MaxValue)).ThenBy(a => a.Ordem))
            {
                Aplicativos.Add(CriarAppItemViewModel(item));
            }
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
            onMoverDireita: MoverAppDireita,
            onInteragir: EncerrarAlertaDoApp);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        vm.PropertyChanged += async (s, e) => {
            if (e.PropertyName == "NumeroNotificacoes") {
                SincronizarBadgeWidget(vm);
                
                if (vm.NumeroNotificacoes == 0)
                {
                    string t = (vm.Titulo ?? "").ToLowerInvariant();
                    string exec = (vm.CaminhoExecutavel ?? "").ToLowerInvariant();
                    if (t.Contains("whatsapp") || exec.Contains("whatsapp")) 
                    {
                        if (_disposed || _syncOcupado) return;
        _syncOcupado = true;
        Dictionary<string, int> dict;
        try { dict = await _toastService.ObterContagemNotificacoesPorAppAsync(); }
        finally { FinalizarSincronizacao(); }
        if (_disposed) return;
                        _whatsappGhosts = dict.Where(x => x.Key.ToLowerInvariant().Contains("whatsapp")).Sum(x => x.Value);
                        WhatsApp.MensagensNaoLidas = 0;
                    }
                    else if (t.Contains("teams") || t.Contains("msteams") || exec.Contains("teams") || exec.Contains("msteams"))
                    {
                        if (_disposed || _syncOcupado) return;
        _syncOcupado = true;
        Dictionary<string, int> dict;
        try { dict = await _toastService.ObterContagemNotificacoesPorAppAsync(); }
        finally { FinalizarSincronizacao(); }
        if (_disposed) return;
                        _teamsGhosts = dict.Where(x => x.Key.ToLowerInvariant().Contains("teams") || x.Key.ToLowerInvariant().Contains("msteams")).Sum(x => x.Value);
                        Teams.MensagensNaoLidas = 0;
                    }
                }
            }
        };
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
            onMoverDireita: MoverAppDireita,
            onInteragir: EncerrarAlertaDoApp);
        vm.OnMoverParaAmbiente = MoverAppParaAmbiente;
        vm.PropertyChanged += async (s, e) => {
            if (e.PropertyName == "NumeroNotificacoes") {
                SincronizarBadgeWidget(vm);
                
                if (vm.NumeroNotificacoes == 0)
                {
                    string t = (vm.Titulo ?? "").ToLowerInvariant();
                    string exec = (vm.CaminhoExecutavel ?? "").ToLowerInvariant();
                    if (t.Contains("whatsapp") || exec.Contains("whatsapp")) 
                    {
                        if (_disposed || _syncOcupado) return;
        _syncOcupado = true;
        Dictionary<string, int> dict;
        try { dict = await _toastService.ObterContagemNotificacoesPorAppAsync(); }
        finally { FinalizarSincronizacao(); }
        if (_disposed) return;
                        _whatsappGhosts = dict.Where(x => x.Key.ToLowerInvariant().Contains("whatsapp")).Sum(x => x.Value);
                        WhatsApp.MensagensNaoLidas = 0;
                    }
                    else if (t.Contains("teams") || t.Contains("msteams") || exec.Contains("teams") || exec.Contains("msteams"))
                    {
                        if (_disposed || _syncOcupado) return;
        _syncOcupado = true;
        Dictionary<string, int> dict;
        try { dict = await _toastService.ObterContagemNotificacoesPorAppAsync(); }
        finally { FinalizarSincronizacao(); }
        if (_disposed) return;
                        _teamsGhosts = dict.Where(x => x.Key.ToLowerInvariant().Contains("teams") || x.Key.ToLowerInvariant().Contains("msteams")).Sum(x => x.Value);
                        Teams.MensagensNaoLidas = 0;
                    }
                }
            }
        };
        return vm;
    }

    private void SincronizarBadgeWidget(AppItemViewModel app)
    {
        string titulo = (app.Titulo ?? string.Empty).ToLowerInvariant();
        string caminho = (app.CaminhoExecutavel ?? string.Empty).ToLowerInvariant();
        
        if (titulo.Contains("teams") || titulo.Contains("msteams") || caminho.Contains("ms-teams"))
        {
            Teams.MensagensNaoLidas = app.NumeroNotificacoes;
        }
        else if (titulo.Contains("whatsapp") || caminho.Contains("whatsapp"))
        {
            WhatsApp.MensagensNaoLidas = app.NumeroNotificacoes;
        }
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
            .GroupBy(j => !string.IsNullOrWhiteSpace(j.CaminhoExecutavel)
                ? j.CaminhoExecutavel.ToLowerInvariant()
                : !string.IsNullOrWhiteSpace(j.NomeProcesso)
                    ? j.NomeProcesso.ToLowerInvariant()
                    : $"janela-{j.Hwnd}")
            .ToList();

        // Toda janela de aplicativo válida aparece em qualquer ambiente. Fixação
        // define apenas quais itens permanecem depois que suas janelas fecham.

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
        => CorrespondeAoCaminho(app.CaminhoExecutavel, janela);

    private static bool CorrespondeAoCaminho(string caminho, JanelaInfo janela)
    {
        const string appsFolder = "shell:AppsFolder\\";
        if (caminho.StartsWith(appsFolder, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(janela.AppUserModelId))
            return string.Equals(caminho[appsFolder.Length..], janela.AppUserModelId, StringComparison.OrdinalIgnoreCase);
        if (string.Equals(caminho, janela.CaminhoExecutavel, StringComparison.OrdinalIgnoreCase))
            return true;

        var exeNomeApp = Path.GetFileName(caminho);
        var exeNomeJanela = Path.GetFileName(janela.CaminhoExecutavel);

        if (!string.IsNullOrEmpty(exeNomeApp) && !string.IsNullOrEmpty(exeNomeJanela) &&
            string.Equals(exeNomeApp, exeNomeJanela, StringComparison.OrdinalIgnoreCase))
            return true;

        var procNomeApp = Path.GetFileNameWithoutExtension(caminho);
        if (!string.IsNullOrEmpty(procNomeApp) && !string.IsNullOrEmpty(janela.NomeProcesso) &&
            string.Equals(procNomeApp, janela.NomeProcesso, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private void AlternarFixadoApp(AppItemViewModel app)
    {
        if (AmbienteAtivo == null) return;
        if (app.EstaFixado)
        {
            app.EstaFixado = false;
            AmbienteAtivo.Model.Itens.RemoveAll(a => a.Id == app.Id || CorrespondeAoApp(app, new JanelaInfo { CaminhoExecutavel = a.CaminhoOuUrl }));
            AmbienteAtivo.RecarregarItens();
            SalvarPreferencias();

            if (!app.EstaAberto)
            {
                Aplicativos.Remove(app);
            }
        }
        else
        {
            app.EstaFixado = true;
            if (!AmbienteAtivo.Model.Itens.Any(a => a.Id == app.Id || CorrespondeAoApp(app, new JanelaInfo { CaminhoExecutavel = a.CaminhoOuUrl })))
            {
                AmbienteAtivo.Model.Itens.Add(app.ToModel(AmbienteAtivo.Model.Itens.Count));
                AmbienteAtivo.RecarregarItens();
            }
            SalvarPreferencias();
        }
    }

    private void MoverAppEsquerda(AppItemViewModel app)
    {
        int idx = Aplicativos.IndexOf(app);
        if (idx > 0)
        {
            ReordenarAplicativo(app, Aplicativos[idx - 1]);
        }
    }

    private void MoverAppDireita(AppItemViewModel app)
    {
        int idx = Aplicativos.IndexOf(app);
        if (idx >= 0 && idx < Aplicativos.Count - 1)
        {
            ReordenarAplicativo(app, Aplicativos[idx + 1]);
        }
    }

    public void ReordenarAplicativo(AppItemViewModel origem, AppItemViewModel destino)
    {
        if (!origem.EstaFixado || !destino.EstaFixado || ReferenceEquals(origem, destino)) return;
        var de = Aplicativos.IndexOf(origem);
        var para = Aplicativos.IndexOf(destino);
        if (de < 0 || para < 0) return;
        var environment = AmbienteAtivo?.Model;
        if (environment is null) return;
        var previousOrder = environment.OrdemAplicativosDock;
        var previousRanks = _preferencias.AppsPermanentes.Concat(environment.Itens).ToDictionary(item => item, item => item.Ordem);
        Aplicativos.Move(de, para);
        environment.OrdemAplicativosDock = Aplicativos.Where(app => app.EstaFixado).Select(app => app.Id).ToList();

        var ordemGlobal = 0;
        var ordemAmbiente = 0;
        foreach (var app in Aplicativos.Where(a => a.EstaFixado))
        {
            var global = _preferencias.AppsPermanentes.FirstOrDefault(i => i.Id == app.Id);
            if (global != null) { global.Ordem = ordemGlobal++; continue; }
            var local = AmbienteAtivo?.Model.Itens.FirstOrDefault(i => i.Id == app.Id);
            if (local != null) local.Ordem = ordemAmbiente++;
        }
        if (!TentarSalvarPreferencias())
        {
            environment.OrdemAplicativosDock = previousOrder;
            foreach (var (item, order) in previousRanks) item.Ordem = order;
            Aplicativos.Move(para, de);
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
        _preferencias.AppsGlobaisMigrados = true;
        if (!_preferencias.AppsPermanentes.Any(a => string.Equals(a.CaminhoOuUrl, item.CaminhoOuUrl, StringComparison.OrdinalIgnoreCase)))
        {
            item.Ordem = _preferencias.AppsPermanentes.Count;
            _preferencias.AppsPermanentes.Add(item);
        }
        CarregarAplicativos();
        SalvarPreferencias();
    }

    public void RemoverAppPermanenteDireto(string id, string caminho)
    {
        _preferencias.AppsPermanentes.RemoveAll(a => a.Id == id || string.Equals(a.CaminhoOuUrl, caminho, StringComparison.OrdinalIgnoreCase));
        CarregarAplicativos();
        SalvarPreferencias();
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
        var caminhoAnterior = app.CaminhoExecutavel;
        var caminhoResolvido = LauncherService.ResolverAplicativoEmpacotado(caminhoAnterior);
        if (!string.Equals(caminhoAnterior, caminhoResolvido, StringComparison.OrdinalIgnoreCase))
        {
            AtualizarCaminhoAplicativoSalvo(caminhoAnterior, caminhoResolvido);
            app.CaminhoExecutavel = caminhoResolvido;
            SalvarPreferencias();
        }

        var res = _launcher.ExecutarCaminho(caminhoResolvido);
        if (!res.Sucesso)
        {
            MostrarAlerta?.Invoke("Erro ao Abrir Aplicativo", res.MensagemErro ?? "NÃ£o foi possÃ­vel iniciar o aplicativo.");
        }
    }

    private void AtualizarCaminhoAplicativoSalvo(string caminhoAnterior, string caminhoNovo)
    {
        static IEnumerable<ItemFixado> ItensDasColecoes(IEnumerable<ColecaoApp> colecoes) =>
            colecoes.SelectMany(colecao => colecao.Itens);

        var itens = _preferencias.AppsPermanentes
            .Concat(_preferencias.Ambientes.SelectMany(ambiente => ambiente.Itens))
            .Concat(ItensDasColecoes(_preferencias.ColecoesGlobais ?? []))
            .Concat(_preferencias.Ambientes.SelectMany(ambiente => ItensDasColecoes(ambiente.Colecoes)));

        foreach (var item in itens.Where(item => string.Equals(
                     item.CaminhoOuUrl, caminhoAnterior, StringComparison.OrdinalIgnoreCase)))
        {
            item.CaminhoOuUrl = caminhoNovo;
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
            onEditarColecao: col => AbrirAjustes("Ambientes"),
            onRemoverColecao: RemoverColecao);
    }

        private void RemoverColecao(ColecaoAppViewModel vm)
    {
        // Se for global
        if (ColecoesGlobais.Contains(vm))
        {
            ColecoesGlobais.Remove(vm);
            _preferencias.ColecoesGlobais?.Remove(vm.Model);
            SalvarPreferencias();
            RecarregarTodasColecoes();
            return;
        }

        // Se for do ambiente ativo
        if (AmbienteAtivo != null && AmbienteAtivo.Colecoes.Contains(vm))
        {
            AmbienteAtivo.Colecoes.Remove(vm);
            AmbienteAtivo.Model.Colecoes.Remove(vm.Model);
            SalvarPreferencias();
            RecarregarTodasColecoes();
        }
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

        public void AdicionarColecaoGlobal()
    {
        var nome = PedirTexto?.Invoke("Nova Coleção", "Digite o nome da nova coleção:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        var novaCol = new DockWindows.Core.Models.ColecaoApp
        {
            Nome = nome.Trim(),
            EhGlobal = true,
            Ordem = _preferencias.ColecoesGlobais?.Count ?? 0
        };

        _preferencias.ColecoesGlobais ??= new List<DockWindows.Core.Models.ColecaoApp>();
        _preferencias.ColecoesGlobais.Add(novaCol);
        
        SalvarPreferencias();
        RecarregarTodasColecoes();
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
                MostrarAlerta?.Invoke("Erro ao Exportar", $"Não foi possível salvar o backup: {ex.Message}");
                return false;
            }
        }

        public void ForcarAtualizacaoLayout()
        {
            OnPropertyChanged(nameof(OrdemSecoes));
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

    private IReadOnlyList<AppInstalado> _catalogoLaunchpad = Array.Empty<AppInstalado>();
    private bool _carregandoCatalogoLaunchpad;
    public string EstadoCatalogoLaunchpad { get; private set; } = "Aplicativos e atalhos do ambiente";

    private async System.Threading.Tasks.Task CarregarCatalogoLaunchpadAsync()
    {
        if (_carregandoCatalogoLaunchpad || _disposed) return;
        _carregandoCatalogoLaunchpad = true;
        EstadoCatalogoLaunchpad = "Carregando aplicativos instalados...";
        OnPropertyChanged(nameof(EstadoCatalogoLaunchpad));
        try
        {
            _catalogoLaunchpad = await InstalledAppsScanner.ListarAsync(forcarAtualizacao: true);
            if (_disposed) return;
            EstadoCatalogoLaunchpad = $"{_catalogoLaunchpad.Count} aplicativos instalados · atalhos do ambiente primeiro";
            OnPropertyChanged(nameof(ItensLaunchpadFiltrados));
        }
        catch (Exception ex)
        {
            EstadoCatalogoLaunchpad = "Não foi possível carregar todos os aplicativos. Reabra para tentar novamente.";
            RegistrarFalhaNaoFatal("launchpad.log", ex);
        }
        finally
        {
            _carregandoCatalogoLaunchpad = false;
            OnPropertyChanged(nameof(EstadoCatalogoLaunchpad));
        }
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

            // AppsFolder inclui aplicativos Win32 e Microsoft Store, sem bloquear a digita??o.
            foreach (var app in _catalogoLaunchpad)
            {
                if (!idsAdicionados.Add(app.CaminhoExecucao)) continue;
                lista.Add(new LaunchpadItemModel
                {
                    Id = app.ParsingName,
                    Titulo = app.Nome,
                    Subtitulo = "Aplicativo instalado",
                    Icone = _iconService.ObterIcone(app.CaminhoExecucao, TipoItem.Aplicativo),
                    IconeTexto = "▦",
                    ExecutarCommand = new RelayCommand(() =>
                    {
                        MenuIniciarAberto = false;
                        _launcher.ExecutarCaminho(app.CaminhoExecucao);
                    }),
                    Categoria = "Sistema"
                });
            }
            var filtro = TextoFiltroLaunchpad.Trim();
            if (filtro.Length == 0) return lista;
            var comparador = System.Globalization.CultureInfo.GetCultureInfo("pt-BR").CompareInfo;
            return lista.Where(i => comparador.IndexOf(i.Titulo, filtro,
                System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0
                || comparador.IndexOf(i.Subtitulo, filtro,
                System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0);

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
