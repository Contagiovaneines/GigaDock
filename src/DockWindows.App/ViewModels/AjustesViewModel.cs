using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using Microsoft.Win32;

namespace DockWindows.App.ViewModels;

public class AjustesViewModel : ObservableObject
{
    private readonly MainViewModel _mainVm;
    private readonly ISettingsRepository _repo;
    private readonly IAutostartService _autostart;

    private string _secaoAtiva = "Ambientes";
    private Ambiente? _ambienteSelecionado;
    private ItemFixado? _itemSelecionado;
    private ColecaoApp? _colecaoSelecionada;
    private WidgetInstanceConfig? _widgetSelecionado;
    private CompromissoLocal? _compromissoSelecionado;
    private EspacadorConfig? _espacadorSelecionado;
    private TemaDefinicao? _temaSelecionado;
    private string _categoriaGeralSelecionada = "BarraPrincipal";
    private string _categoriaUtilSelecionada = "Backup";

    public AjustesViewModel(MainViewModel mainVm, ISettingsRepository repo, IAutostartService autostart, string? secaoInicial = null)
    {
        _mainVm = mainVm;
        _repo = repo;
        _autostart = autostart;

        if (!string.IsNullOrWhiteSpace(secaoInicial))
        {
            _secaoAtiva = secaoInicial;
        }

        TemasPredefinidos = new ObservableCollection<TemaDefinicao>(TemaDefinicao.ObterTemasPredefinidos());
        Ambientes = _mainVm.Ambientes;
        Espacadores = _mainVm.Espacadores;
        Compromissos = _mainVm.Calendario.Compromissos;

        AmbienteSelecionado = _mainVm.AmbienteAtivo?.Model ?? Ambientes.FirstOrDefault()?.Model;
        TemaSelecionado = TemasPredefinidos.FirstOrDefault(t => t.Estilo == _mainVm.EstiloTema) ?? TemasPredefinidos.FirstOrDefault();
        EspacadorSelecionado = Espacadores.FirstOrDefault();

        // Comandos de Navegação
        NavegarCommand = new RelayCommand<string>(NavegarPara);

        // Comandos de Ambientes
        NovoAmbienteCommand = new RelayCommand(NovoAmbiente);
        ExcluirAmbienteCommand = new RelayCommand(ExcluirAmbiente, () => Ambientes.Count > 1);
        MoverAmbienteCimaCommand = new RelayCommand(MoverAmbienteCima, () => AmbienteSelecionado != null && ObterIndiceAmbienteSelecionado() > 0);
        MoverAmbienteBaixoCommand = new RelayCommand(MoverAmbienteBaixo, () => AmbienteSelecionado != null && ObterIndiceAmbienteSelecionado() < Ambientes.Count - 1);
        AtivarAmbienteSelecionadoCommand = new RelayCommand(AtivarAmbienteSelecionado, () => AmbienteSelecionado != null && _mainVm.AmbienteAtivo?.Id != AmbienteSelecionado.Id);
        DefinirCorAmbienteCommand = new RelayCommand<string>(DefinirCorAmbiente);

        // Comandos de Itens no Ambiente
        AdicionarAppExecutavelCommand = new RelayCommand(AdicionarAppExecutavel);
        AdicionarArquivoCommand = new RelayCommand(AdicionarArquivo);
        AdicionarPastaCommand = new RelayCommand(AdicionarPasta);
        AdicionarSiteUrlCommand = new RelayCommand(AdicionarSiteUrl);
        AdicionarColecaoAmbienteCommand = new RelayCommand(AdicionarColecaoAmbiente);
        EditarItemAmbienteCommand = new RelayCommand(EditarItemAmbiente, () => ItemSelecionado != null);
        RemoverItemAmbienteCommand = new RelayCommand(RemoverItemAmbiente, () => ItemSelecionado != null);
        MoverItemCimaCommand = new RelayCommand(MoverItemCima, () => ItemSelecionado != null && ItensAmbiente.IndexOf(ItemSelecionado) > 0);
        MoverItemBaixoCommand = new RelayCommand(MoverItemBaixo, () => ItemSelecionado != null && ItensAmbiente.IndexOf(ItemSelecionado) < ItensAmbiente.Count - 1);
        AlternarEscopoItemCommand = new RelayCommand(AlternarEscopoItem, () => ItemSelecionado != null);

        // Comandos de Coleções
        NovaColecaoCommand = new RelayCommand(NovaColecao);
        ExcluirColecaoCommand = new RelayCommand(ExcluirColecao, () => ColecaoSelecionada != null);
        AdicionarItemColecaoCommand = new RelayCommand(AdicionarItemColecao, () => ColecaoSelecionada != null);
        RemoverItemColecaoCommand = new RelayCommand<ItemFixado>(RemoverItemColecao);

        // Comandos de Widgets
                RemoverWidgetCommand = new RelayCommand(RemoverWidget, () => WidgetSelecionado != null);
        MoverWidgetCimaCommand = new RelayCommand(MoverWidgetCima, () => WidgetSelecionado != null && WidgetsAmbiente.IndexOf(WidgetSelecionado) > 0);
        MoverWidgetBaixoCommand = new RelayCommand(MoverWidgetBaixo, () => WidgetSelecionado != null && WidgetsAmbiente.IndexOf(WidgetSelecionado) < WidgetsAmbiente.Count - 1);
        EscolherEstiloWidgetCommand = new RelayCommand<WidgetInstanceConfig>(EscolherEstiloWidget);
        AlternarFormatoWidgetCommand = new RelayCommand<WidgetInstanceConfig>(AlternarFormatoWidget);
        AlternarVisibilidadeWidgetCommand = new RelayCommand<WidgetInstanceConfig>(AlternarVisibilidadeWidget);
        NovoCompromissoCommand = new RelayCommand(NovoCompromisso);
        RemoverCompromissoCommand = new RelayCommand(RemoverCompromisso, () => CompromissoSelecionado != null);
        ProcurarArquivoIcsCommand = new RelayCommand(ProcurarArquivoIcs);
        AbrirLojaWidgetsCommand = new RelayCommand(AbrirLojaWidgets);
        AbrirGaleriaAppsCommand = new RelayCommand(AbrirGaleriaApps);

        // Comandos de Espaçadores
        NovoEspacadorCommand = new RelayCommand(NovoEspacador);
        RemoverEspacadorCommand = new RelayCommand(RemoverEspacador, () => EspacadorSelecionado != null);
        MoverEspacadorCimaCommand = new RelayCommand(MoverEspacadorCima, () => EspacadorSelecionado != null && Espacadores.IndexOf(EspacadorSelecionado) > 0);
        MoverEspacadorBaixoCommand = new RelayCommand(MoverEspacadorBaixo, () => EspacadorSelecionado != null && Espacadores.IndexOf(EspacadorSelecionado) < Espacadores.Count - 1);

        // Comandos de Aparência
        SelecionarTemaCommand = new RelayCommand<TemaDefinicao>(SelecionarTema);

        // Comandos de Utilitários
        ExportarBackupCommand = new RelayCommand(ExportarBackup);
        ImportarBackupCommand = new RelayCommand(ImportarBackup);
        RestaurarPadroesFabricaCommand = new RelayCommand(RestaurarPadroesFabrica);
        RestaurarBarraWindowsCommand = new RelayCommand(() => _mainVm.RestaurarBarraWindows());
        AbrirPastaScriptsCommand = new RelayCommand(AbrirPastaScripts);

        // Fechar janela
        ConcluirCommand = new RelayCommand(() => FecharJanela?.Invoke());
    }

    public Action? FecharJanela { get; set; }
    public Func<string, string, string?>? PedirTexto { get; set; }
    public Func<ItemFixado?, ItemFixado?>? AbrirDialogoItem { get; set; }
    public Action<string, string>? MostrarAlerta { get; set; }
    public Func<string, string, bool>? ConfirmarAcao { get; set; }

    public string AppNome => "GigaDock";
    public string AppVersao => typeof(AjustesViewModel).Assembly.GetName().Version?.ToString(3) ?? "GigaDock";
    public EnvironmentViewModel? AmbienteAtivo => _mainVm.AmbienteAtivo;

    public string SecaoAtiva
    {
        get => _secaoAtiva;
        set
        {
            if (SetProperty(ref _secaoAtiva, value))
            {
                OnPropertyChanged(nameof(EhSecaoAmbientes));
                OnPropertyChanged(nameof(EhSecaoWidgets));
                OnPropertyChanged(nameof(EhSecaoEspacadores));
                OnPropertyChanged(nameof(EhSecaoAparencia));
                OnPropertyChanged(nameof(EhSecaoGeral));
                OnPropertyChanged(nameof(EhSecaoUtilitarios));
                OnPropertyChanged(nameof(EhSecaoSobre));
                OnPropertyChanged(nameof(EhSecaoVisualizacoes));
            }
        }
    }

    public bool EhSecaoVisualizacoes => SecaoAtiva == "Visualizacoes";
    public bool EhSecaoAmbientes => SecaoAtiva == "Ambientes";
    public bool EhSecaoWidgets => SecaoAtiva == "Widgets";
    public bool EhSecaoEspacadores => SecaoAtiva == "Espacadores";
    public bool EhSecaoAparencia => SecaoAtiva == "Aparencia";
    public bool EhSecaoGeral => SecaoAtiva == "Geral";
    public bool EhSecaoUtilitarios => SecaoAtiva == "Utilitarios";
    public bool EhSecaoSobre => SecaoAtiva == "Sobre";

    public ObservableCollection<TemaDefinicao> TemasPredefinidos { get; }
    public ObservableCollection<EnvironmentViewModel> Ambientes { get; }
    public ObservableCollection<EspacadorConfig> Espacadores { get; }
    public ObservableCollection<CompromissoLocal> Compromissos { get; }

    public ObservableCollection<ItemFixado> ItensAmbiente { get; } = new();
    public ObservableCollection<ColecaoApp> ColecoesAmbiente { get; } = new();
    public ObservableCollection<WidgetInstanceConfig> WidgetsAmbiente { get; } = new();

    public Ambiente? AmbienteSelecionado
    {
        get => _ambienteSelecionado;
        set
        {
            if (SetProperty(ref _ambienteSelecionado, value))
            {
                CarregarDadosAmbienteSelecionado();
                OnPropertyChanged(nameof(NomeAmbienteEditavel));
                OnPropertyChanged(nameof(CorAmbienteEditavel));
                OnPropertyChanged(nameof(IconeAmbienteEditavel));
                OnPropertyChanged(nameof(CorIndicadorAppsEditavel));
                OnPropertyChanged(nameof(EstiloIndicadorAppsEditavel));
                OnPropertyChanged(nameof(EstaAtivoAmbienteSelecionado));
                OnPropertyChanged(nameof(RelogioAmbienteHabilitado));
                OnPropertyChanged(nameof(PomodoroAmbienteHabilitado));
                OnPropertyChanged(nameof(CalendarioAmbienteHabilitado));
            }
        }
    }

    public bool EstaAtivoAmbienteSelecionado =>
        AmbienteSelecionado != null && _mainVm.AmbienteAtivo?.Id == AmbienteSelecionado.Id;

    public ItemFixado? ItemSelecionado
    {
        get => _itemSelecionado;
        set
        {
            if (SetProperty(ref _itemSelecionado, value))
            {
                OnPropertyChanged(nameof(ItemSelecionadoEhGlobal));
                OnPropertyChanged(nameof(TextoEscopoItemSelecionado));
            }
        }
    }

    public bool ItemSelecionadoEhGlobal
    {
        get
        {
            if (ItemSelecionado == null) return false;
            return _mainVm.Preferencias.AppsPermanentes.Any(a => a.Id == ItemSelecionado.Id);
        }
    }

    public string TextoEscopoItemSelecionado =>
        ItemSelecionadoEhGlobal ? "Global (em todos os ambientes)" : "Somente neste ambiente";

    public ColecaoApp? ColecaoSelecionada
    {
        get => _colecaoSelecionada;
        set => SetProperty(ref _colecaoSelecionada, value);
    }

    public WidgetInstanceConfig? WidgetSelecionado
    {
        get => _widgetSelecionado;
        set
        {
            if (SetProperty(ref _widgetSelecionado, value))
            {
                OnPropertyChanged(nameof(EhWidgetRelogio));
                OnPropertyChanged(nameof(EhWidgetPomodoro));
                OnPropertyChanged(nameof(EhWidgetCalendario));
                OnPropertyChanged(nameof(EhWidgetAgua));
                OnPropertyChanged(nameof(EhWidgetCotacao));
                OnPropertyChanged(nameof(NomeWidgetSelecionado));
                OnPropertyChanged(nameof(FusoHorarioWidgetSelecionado));
                OnPropertyChanged(nameof(IntervaloAguaSelecionado));
                OnPropertyChanged(nameof(HoraInicioAguaSelecionada));
                OnPropertyChanged(nameof(HoraFimAguaSelecionada));
                OnPropertyChanged(nameof(MoedaBaseSelecionada));
                OnPropertyChanged(nameof(MoedaDestinoSelecionada));
            }
        }
    }

    public bool EhWidgetRelogio => WidgetSelecionado?.Tipo == TipoWidget.Relogio;
    public bool EhWidgetPomodoro => WidgetSelecionado?.Tipo == TipoWidget.Pomodoro;
    public bool EhWidgetCalendario => WidgetSelecionado?.Tipo == TipoWidget.CalendarioCompromissos;
    public bool EhWidgetAgua => WidgetSelecionado?.Tipo == TipoWidget.LembreteAgua;
    public bool EhWidgetCotacao => WidgetSelecionado?.Tipo == TipoWidget.CotacaoMoedas;
    public IReadOnlyList<TimeZoneInfo> FusosHorarios { get; } = TimeZoneInfo.GetSystemTimeZones();
    public string NomeWidgetSelecionado
    {
        get => WidgetSelecionado?.Nome ?? string.Empty;
        set
        {
            if (WidgetSelecionado == null || string.IsNullOrWhiteSpace(value) || WidgetSelecionado.Nome == value.Trim()) return;
            WidgetSelecionado.Nome = value.Trim();
            OnPropertyChanged();
            AlternarVisibilidadeWidget(WidgetSelecionado);
        }
    }
    public string FusoHorarioWidgetSelecionado
    {
        get => WidgetSelecionado?.ObterConfiguracao("fusoHorarioId") ?? string.Empty;
        set
        {
            if (WidgetSelecionado?.Tipo != TipoWidget.Relogio) return;
            WidgetSelecionado.DefinirConfiguracao("fusoHorarioId", value);
            OnPropertyChanged();
            AlternarVisibilidadeWidget(WidgetSelecionado);
        }
    }
    public int IntervaloAguaSelecionado
    {
        get => int.TryParse(WidgetSelecionado?.ObterConfiguracao("intervaloMinutos", "60"), out var valor) ? valor : 60;
        set => SalvarConfiguracaoAgua("intervaloMinutos", Math.Clamp(value, 15, 240));
    }
    public int HoraInicioAguaSelecionada
    {
        get => int.TryParse(WidgetSelecionado?.ObterConfiguracao("horaInicio", "8"), out var valor) ? valor : 8;
        set => SalvarConfiguracaoAgua("horaInicio", Math.Clamp(value, 0, 23));
    }
    public int HoraFimAguaSelecionada
    {
        get => int.TryParse(WidgetSelecionado?.ObterConfiguracao("horaFim", "22"), out var valor) ? valor : 22;
        set => SalvarConfiguracaoAgua("horaFim", Math.Clamp(value, 1, 24));
    }
    private void SalvarConfiguracaoAgua(string chave, int valor)
    {
        if (WidgetSelecionado?.Tipo != TipoWidget.LembreteAgua) return;
        WidgetSelecionado.DefinirConfiguracao(chave, valor.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AlternarVisibilidadeWidget(WidgetSelecionado);
    }
    public string MoedaBaseSelecionada
    {
        get => WidgetSelecionado?.ObterConfiguracao("base", "USD") ?? "USD";
        set => SalvarConfiguracaoTexto("base", value, TipoWidget.CotacaoMoedas);
    }
    public string MoedaDestinoSelecionada
    {
        get => WidgetSelecionado?.ObterConfiguracao("destino", "BRL") ?? "BRL";
        set => SalvarConfiguracaoTexto("destino", value, TipoWidget.CotacaoMoedas);
    }
    private void SalvarConfiguracaoTexto(string chave, string? valor, TipoWidget tipo)
    {
        if (WidgetSelecionado?.Tipo != tipo || string.IsNullOrWhiteSpace(valor)) return;
        WidgetSelecionado.DefinirConfiguracao(chave, valor.ToUpperInvariant());
        AlternarVisibilidadeWidget(WidgetSelecionado);
    }

    public CompromissoLocal? CompromissoSelecionado
    {
        get => _compromissoSelecionado;
        set => SetProperty(ref _compromissoSelecionado, value);
    }

    public EspacadorConfig? EspacadorSelecionado
    {
        get => _espacadorSelecionado;
        set => SetProperty(ref _espacadorSelecionado, value);
    }

    public TemaDefinicao? TemaSelecionado
    {
        get => _temaSelecionado;
        set
        {
            if (SetProperty(ref _temaSelecionado, value) && value != null)
            {
                _mainVm.EstiloTema = value.Estilo;
                _mainVm.RaioCantosDock = value.RaioCantos;
                _mainVm.OpacidadeDock = value.OpacidadePadrao;
                OnPropertyChanged(nameof(EstiloTema));
                OnPropertyChanged(nameof(RaioCantosDock));
                OnPropertyChanged(nameof(OpacidadeDock));
                _mainVm.SalvarPreferencias();
            }
        }
    }

    public string CategoriaGeralSelecionada
    {
        get => _categoriaGeralSelecionada;
        set => SetProperty(ref _categoriaGeralSelecionada, value);
    }

    public string CategoriaUtilSelecionada
    {
        get => _categoriaUtilSelecionada;
        set => SetProperty(ref _categoriaUtilSelecionada, value);
    }

    // Campos Editáveis do Ambiente Selecionado
    public string NomeAmbienteEditavel
    {
        get => AmbienteSelecionado?.Nome ?? string.Empty;
        set
        {
            if (AmbienteSelecionado != null && !string.IsNullOrWhiteSpace(value) && AmbienteSelecionado.Nome != value)
            {
                AmbienteSelecionado.Nome = value.Trim();
                var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
                if (vm != null) vm.Nome = value.Trim();
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public string CorAmbienteEditavel
    {
        get => AmbienteSelecionado?.CorHex ?? "#0078D4";
        set
        {
            if (AmbienteSelecionado != null && !string.IsNullOrWhiteSpace(value) && AmbienteSelecionado.CorHex != value)
            {
                AmbienteSelecionado.CorHex = value;
                var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
                if (vm != null) vm.CorHex = value;
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public string IconeAmbienteEditavel
    {
        get => AmbienteSelecionado?.Icone ?? "💼";
        set
        {
            if (AmbienteSelecionado != null && !string.IsNullOrWhiteSpace(value) && AmbienteSelecionado.Icone != value)
            {
                AmbienteSelecionado.Icone = value;
                var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
                if (vm != null) vm.Icone = value;
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public string CorIndicadorAppsEditavel
    {
        get => AmbienteSelecionado?.CorIndicadorApps ?? "#0A84FF";
        set
        {
            if (AmbienteSelecionado != null && !string.IsNullOrWhiteSpace(value))
            {
                AmbienteSelecionado.CorIndicadorApps = value;
                var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
                if (vm != null) vm.CorIndicadorApps = value;
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public string EstiloIndicadorAppsEditavel
    {
        get => AmbienteSelecionado?.EstiloIndicadorApps ?? "Barra";
        set
        {
            if (AmbienteSelecionado != null && !string.IsNullOrWhiteSpace(value))
            {
                AmbienteSelecionado.EstiloIndicadorApps = value;
                var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
                if (vm != null) vm.EstiloIndicadorApps = value;
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public bool RelogioAmbienteHabilitado
    {
        get => AmbienteSelecionado?.Widgets.RelogioHabilitado ?? true;
        set
        {
            if (AmbienteSelecionado != null)
            {
                AmbienteSelecionado.Widgets.RelogioHabilitado = value;
                _mainVm.Clock.Habilitado = value;
                _mainVm.ForcarAtualizacaoLayout();
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public bool PomodoroAmbienteHabilitado
    {
        get => AmbienteSelecionado?.Widgets.PomodoroHabilitado ?? true;
        set
        {
            if (AmbienteSelecionado != null)
            {
                AmbienteSelecionado.Widgets.PomodoroHabilitado = value;
                _mainVm.Pomodoro.CarregarConfiguracao(AmbienteSelecionado.Widgets);
                _mainVm.ForcarAtualizacaoLayout();
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

        public bool CalendarioAmbienteHabilitado
    {
        get
        {
            var w = AmbienteSelecionado?.WidgetsInstalados.FirstOrDefault(x => x.Tipo == TipoWidget.CalendarioCompromissos);
            return w?.Visivel ?? true;
        }
        set
        {
            if (AmbienteSelecionado != null)
            {
                var w = AmbienteSelecionado.WidgetsInstalados.FirstOrDefault(x => x.Tipo == TipoWidget.CalendarioCompromissos);
                if (w == null)
                {
                    w = new WidgetInstanceConfig { Id = "wgt-calendario", Tipo = TipoWidget.CalendarioCompromissos, Nome = "Calendário", Visivel = value };
                    AmbienteSelecionado.WidgetsInstalados.Add(w);
                }
                else
                {
                    w.Visivel = value;
                }

                _mainVm.Calendario.Habilitado = value;
                _mainVm.ForcarAtualizacaoLayout();
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    // Configurações Gerais
    public bool IniciarComWindows
    {
        get => _mainVm.Preferencias.IniciarComWindows;
        set
        {
            if (_mainVm.Preferencias.IniciarComWindows != value)
            {
                _mainVm.Preferencias.IniciarComWindows = value;
                _autostart.Configurar(value);
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public bool OcultarAutomaticamente
    {
        get => _mainVm.OcultarAutomaticamente;
        set
        {
            _mainVm.OcultarAutomaticamente = value;
            OnPropertyChanged();
        }
    }

    public bool SempreNoTopo
    {
        get => _mainVm.SempreNoTopo;
        set
        {
            _mainVm.SempreNoTopo = value;
            OnPropertyChanged();
        }
    }

    public bool DesativarAnimacoes
    {
        get => _mainVm.DesativarAnimacoes;
        set
        {
            _mainVm.DesativarAnimacoes = value;
            OnPropertyChanged();
        }
    }

    public string VelocidadeAnimacao
    {
        get => _mainVm.VelocidadeAnimacao;
        set
        {
            _mainVm.VelocidadeAnimacao = value;
            OnPropertyChanged();
        }
    }

    public bool UsarComoBarraPrincipal
    {
        get => _mainVm.UsarComoBarraPrincipal;
        set
        {
            _mainVm.UsarComoBarraPrincipal = value;
            OnPropertyChanged();
        }
    }

    public bool ExibirSeletorAmbientes
    {
        get => _mainVm.ExibirSeletorAmbientes;
        set
        {
            _mainVm.ExibirSeletorAmbientes = value;
            OnPropertyChanged();
        }
    }

    public bool ExibirItensFixados
    {
        get => _mainVm.ExibirItensFixados;
        set
        {
            _mainVm.ExibirItensFixados = value;
            OnPropertyChanged();
        }
    }

    public string UrlIcal
    {
        get => _mainVm.UrlIcalAmbiente;
        set
        {
            if (_mainVm.UrlIcalAmbiente != value)
            {
                _mainVm.UrlIcalAmbiente = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ExibirBotoesAcao
    {
        get => _mainVm.ExibirBotoesAcao;
        set
        {
            _mainVm.ExibirBotoesAcao = value;
            OnPropertyChanged();
        }
    }

                public bool AbrirPlayerAoDuploClique
        {
            get => _mainVm.AbrirPlayerAoDuploClique;
            set
            {
                _mainVm.AbrirPlayerAoDuploClique = value;
                OnPropertyChanged();
            }
        }

        public string GitHubUsuario
        {
            get => _mainVm.GitHubUsuario;
            set
            {
                _mainVm.GitHubUsuario = value;
                OnPropertyChanged();
            }
        }

    public bool ExibirMidia
    {
        get => _mainVm.ExibirMidia;
        set
        {
            _mainVm.ExibirMidia = value;
            OnPropertyChanged();
        }
    }

        public bool ExibirContagemColecoes
    {
        get => _mainVm.ExibirContagemColecoes;
        set
        {
            _mainVm.ExibirContagemColecoes = value;
            OnPropertyChanged();
        }
    }


    public bool ModoGamerRgb
    {
        get => _mainVm.ModoGamerRgb;
        set { _mainVm.ModoGamerRgb = value; OnPropertyChanged(); }
    }
    public bool ModoRgbMedia
    {
        get => _mainVm.ModoRgbMedia;
        set
        {
            _mainVm.ModoRgbMedia = value;
            OnPropertyChanged();
        }
    }

public bool AlertasVisuaisHabilitados
    {
        get => _mainVm.AlertasVisuaisHabilitados;
        set
        {
            _mainVm.AlertasVisuaisHabilitados = value;
            OnPropertyChanged();
        }
    }

    public string ModoAberturaPaineis
    {
        get => _mainVm.ModoAberturaPaineis;
        set { _mainVm.ModoAberturaPaineis = value; OnPropertyChanged(); }
    }
    public bool PreviaJanelas
    {
        get => _mainVm.PreviaJanelas;
        set { if (_mainVm.PreviaJanelas == value) return; _mainVm.PreviaJanelas = value; OnPropertyChanged(); }
    }
    public int AtrasoAbrirPreviaMs { get => _mainVm.AtrasoAbrirPreviaMs; set { _mainVm.AtrasoAbrirPreviaMs = value; OnPropertyChanged(); } }
    public int AtrasoFecharPreviaMs { get => _mainVm.AtrasoFecharPreviaMs; set { _mainVm.AtrasoFecharPreviaMs = value; OnPropertyChanged(); } }
    public int AtrasoAposRemoverPreviaMs { get => _mainVm.AtrasoAposRemoverPreviaMs; set { _mainVm.AtrasoAposRemoverPreviaMs = value; OnPropertyChanged(); } }
    public bool FecharPreviaAoClicarFora { get => _mainVm.FecharPreviaAoClicarFora; set { _mainVm.FecharPreviaAoClicarFora = value; OnPropertyChanged(); } }
    public bool ClimaExpandido
    {
        get => _mainVm.ClimaExpandido;
        set { if (_mainVm.ClimaExpandido == value) return; _mainVm.ClimaExpandido = value; OnPropertyChanged(); }
    }
    public bool RelogioAnalogico
    {
        get => _mainVm.RelogioAnalogico;
        set { if (_mainVm.RelogioAnalogico == value) return; _mainVm.RelogioAnalogico = value; OnPropertyChanged(); }
    }
    public bool PreviaPastas
    {
        get => _mainVm.PreviaPastas;
        set { if (_mainVm.PreviaPastas == value) return; _mainVm.PreviaPastas = value; OnPropertyChanged(); }
    }
public bool ExibirBateria
    {
        get => _mainVm.ExibirBateria;
        set
        {
            if (_mainVm.ExibirBateria == value) return;
            _mainVm.ExibirBateria = value;
            OnPropertyChanged();
        }
    }

public bool ExibirLixeira
    {
        get => _mainVm.ExibirLixeira;
        set
        {
            if (_mainVm.ExibirLixeira != value)
            {
                _mainVm.ExibirLixeira = value;
                OnPropertyChanged();
            }
        }
    }


    public bool ExibirAppsAbertosNaoFixados
    {
        get => _mainVm.ExibirAppsAbertosNaoFixados;
        set
        {
            _mainVm.ExibirAppsAbertosNaoFixados = value;
            OnPropertyChanged();
        }
    }

    public bool ExibirClima
    {
        get => _mainVm.ExibirClima;
        set
        {
            _mainVm.ExibirClima = value;
            OnPropertyChanged();
        }
    }

    public string LocalizacaoClima
    {
        get => _mainVm.LocalizacaoClima;
        set
        {
            _mainVm.LocalizacaoClima = value;
            OnPropertyChanged();
        }
    }

    // Aparência
    public EstiloTema EstiloTema
    {
        get => _mainVm.EstiloTema;
        set
        {
            _mainVm.EstiloTema = value;
            TemaSelecionado = TemasPredefinidos.FirstOrDefault(t => t.Estilo == value) ?? TemaSelecionado;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RaioCantosDock));
            OnPropertyChanged(nameof(OpacidadeDock));
        }
    }

    public TamanhoIcone TamanhoIcones
    {
        get => _mainVm.TamanhoIcones;
        set
        {
            _mainVm.TamanhoIcones = value;
            OnPropertyChanged();
        }
    }

    public string TamanhoIconesStr
    {
        get => TamanhoIcones.ToString();
        set
        {
            if (Enum.TryParse<DockWindows.Core.Models.TamanhoIcone>(value, out var result))
            {
                TamanhoIcones = result;
                OnPropertyChanged();
            }
        }
    }

        public double RaioCantosDock
    {
        get => _mainVm.Preferencias.RaioCantosDock;
        set
        {
            _mainVm.RaioCantosDock = value;
            OnPropertyChanged();
        }
    }

    public double AlturaBarra
    {
        get => _mainVm.Preferencias.AlturaBarra;
        set
        {
            _mainVm.AlturaBarra = value;
            OnPropertyChanged();
        }
    }

    public double OpacidadeDock
    {
        get => _mainVm.OpacidadeDock;
        set
        {
            _mainVm.OpacidadeDock = value;
            OnPropertyChanged();
        }
    }

    public bool EfeitoDesfoque
    {
        get => _mainVm.EfeitoDesfoque;
        set
        {
            _mainVm.EfeitoDesfoque = value;
            OnPropertyChanged();
        }
    }

    public int EspacamentoItens
    {
        get => _mainVm.EspacamentoItens;
        set
        {
            _mainVm.EspacamentoItens = value;
            OnPropertyChanged();
        }
    }

    // Pomodoro timers
    public int PomodoroFocoMinutos
    {
        get => _mainVm.AmbienteAtivo?.Widgets.DuracaoFocoMinutos ?? 25;
        set
        {
            if (_mainVm.AmbienteAtivo != null && value >= 1 && value <= 120)
            {
                _mainVm.AmbienteAtivo.Widgets.DuracaoFocoMinutos = value;
                _mainVm.Pomodoro.CarregarConfiguracao(_mainVm.AmbienteAtivo.Widgets);
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public int PomodoroPausaCurtaMinutos
    {
        get => _mainVm.AmbienteAtivo?.Widgets.DuracaoPausaCurtaMinutos ?? 5;
        set
        {
            if (_mainVm.AmbienteAtivo != null && value >= 1 && value <= 60)
            {
                _mainVm.AmbienteAtivo.Widgets.DuracaoPausaCurtaMinutos = value;
                _mainVm.Pomodoro.CarregarConfiguracao(_mainVm.AmbienteAtivo.Widgets);
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    public int PomodoroPausaLongaMinutos
    {
        get => _mainVm.AmbienteAtivo?.Widgets.DuracaoPausaLongaMinutos ?? 15;
        set
        {
            if (_mainVm.AmbienteAtivo != null && value >= 1 && value <= 60)
            {
                _mainVm.AmbienteAtivo.Widgets.DuracaoPausaLongaMinutos = value;
                _mainVm.Pomodoro.CarregarConfiguracao(_mainVm.AmbienteAtivo.Widgets);
                _mainVm.SalvarPreferencias();
                OnPropertyChanged();
            }
        }
    }

    // Comandos
    public ICommand NavegarCommand { get; }
    public ICommand NovoAmbienteCommand { get; }
    public ICommand ExcluirAmbienteCommand { get; }
    public ICommand MoverAmbienteCimaCommand { get; }
    public ICommand MoverAmbienteBaixoCommand { get; }
    public ICommand AtivarAmbienteSelecionadoCommand { get; }
    public ICommand DefinirCorAmbienteCommand { get; }

    public ICommand AdicionarAppExecutavelCommand { get; }
    public ICommand AdicionarArquivoCommand { get; }
    public ICommand AdicionarPastaCommand { get; }
    public ICommand AdicionarSiteUrlCommand { get; }
    public ICommand AdicionarColecaoAmbienteCommand { get; }
    public ICommand EditarItemAmbienteCommand { get; }
    public ICommand RemoverItemAmbienteCommand { get; }
    public ICommand MoverItemCimaCommand { get; }
    public ICommand MoverItemBaixoCommand { get; }
    public ICommand AlternarEscopoItemCommand { get; }

    public ICommand NovaColecaoCommand { get; }
    public ICommand ExcluirColecaoCommand { get; }
    public ICommand AdicionarItemColecaoCommand { get; }
    public ICommand RemoverItemColecaoCommand { get; }

    public ICommand MoverWidgetCimaCommand { get; }
    public ICommand MoverWidgetBaixoCommand { get; }
    public ICommand EscolherEstiloWidgetCommand { get; }
    public ICommand AlternarFormatoWidgetCommand { get; }
    public ICommand AlternarVisibilidadeWidgetCommand { get; }
    public ICommand NovoCompromissoCommand { get; }
    public ICommand RemoverCompromissoCommand { get; }
    public ICommand ProcurarArquivoIcsCommand { get; }
    public ICommand AbrirLojaWidgetsCommand { get; }
    public ICommand AbrirGaleriaAppsCommand { get; }
    public ICommand RemoverWidgetCommand { get; }

    public ICommand NovoEspacadorCommand { get; }
    public ICommand RemoverEspacadorCommand { get; }
    public ICommand MoverEspacadorCimaCommand { get; }
    public ICommand MoverEspacadorBaixoCommand { get; }

    public ICommand SelecionarTemaCommand { get; }

    public ICommand ExportarBackupCommand { get; }
    public ICommand ImportarBackupCommand { get; }
    public ICommand RestaurarPadroesFabricaCommand { get; }
    public ICommand RestaurarBarraWindowsCommand { get; }
    public ICommand AbrirPastaScriptsCommand { get; }
    public ICommand ConcluirCommand { get; }

    public void NavegarPara(string? secao)
    {
        if (!string.IsNullOrWhiteSpace(secao))
        {
            SecaoAtiva = secao;
        }
    }

    private int ObterIndiceAmbienteSelecionado()
    {
        if (AmbienteSelecionado == null) return -1;
        for (int i = 0; i < Ambientes.Count; i++)
        {
            if (Ambientes[i].Id == AmbienteSelecionado.Id) return i;
        }
        return -1;
    }

    private void CarregarDadosAmbienteSelecionado()
    {
        ItensAmbiente.Clear();
        ColecoesAmbiente.Clear();
        WidgetsAmbiente.Clear();

        if (AmbienteSelecionado != null)
        {
            foreach (var it in _mainVm.Preferencias.AppsPermanentes.Concat(AmbienteSelecionado.Itens).GroupBy(i => i.Id).Select(g => g.First()).OrderBy(i => i.Ordem))
            {
                ItensAmbiente.Add(it);
            }
            foreach (var col in AmbienteSelecionado.Colecoes.OrderBy(c => c.Ordem))
            {
                ColecoesAmbiente.Add(col);
            }
            foreach (var wgt in AmbienteSelecionado.WidgetsInstalados.OrderBy(w => w.Ordem))
            {
                WidgetsAmbiente.Add(wgt);
            }
        }

        ItemSelecionado = ItensAmbiente.FirstOrDefault();
        ColecaoSelecionada = ColecoesAmbiente.FirstOrDefault();
        WidgetSelecionado = WidgetsAmbiente.FirstOrDefault();
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
            Icone = "💼",
            CorIndicadorApps = "#0A84FF",
            EstiloIndicadorApps = "Barra",
            Widgets = new WidgetConfig(),
            WidgetsInstalados = Preferencias.CriarWidgetsPadrao(),
            Colecoes = new List<ColecaoApp>()
        };

        _mainVm.Preferencias.Ambientes.Add(novoAmb);
        var novoVm = new EnvironmentViewModel(
            novoAmb,
            new Infrastructure.Windows.LauncherService(),
            new Infrastructure.Windows.IconExtractionService(),
            onEditarItem: vm => { },
            onRemoverItem: vm => { },
            onMoverEsquerda: vm => { },
            onMoverDireita: vm => { },
            notificarErro: msg => MostrarAlerta?.Invoke("Erro", msg),
            onMoverParaGlobal: _mainVm.MoverItemParaGlobal,
            onEditarColecao: col => NavegarPara("Ambientes"));

        Ambientes.Add(novoVm);
        AmbienteSelecionado = novoAmb;
        _mainVm.SalvarPreferencias();
    }

    private void ExcluirAmbiente()
    {
        if (AmbienteSelecionado == null) return;

        if (Ambientes.Count <= 1)
        {
            MostrarAlerta?.Invoke("Ação não permitida", "Não é possível excluir o único ambiente do aplicativo.");
            return;
        }

        bool confirmar = ConfirmarAcao?.Invoke(
            "Confirmar Exclusão de Ambiente",
            $"Tem certeza que deseja excluir o ambiente '{AmbienteSelecionado.Nome}'? Todos os seus itens e configurações serão removidos permanentemente.") ?? true;
        if (!confirmar) return;

        var ambVm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
        if (ambVm != null)
        {
            Ambientes.Remove(ambVm);
            _mainVm.Preferencias.Ambientes.RemoveAll(a => a.Id == AmbienteSelecionado.Id);
            if (_mainVm.AmbienteAtivo?.Id == AmbienteSelecionado.Id)
            {
                _mainVm.AmbienteAtivo = Ambientes.First();
            }
            AmbienteSelecionado = _mainVm.AmbienteAtivo?.Model;
            _mainVm.SalvarPreferencias();
        }
    }

    private void MoverAmbienteCima()
    {
        int idx = ObterIndiceAmbienteSelecionado();
        if (idx > 0 && AmbienteSelecionado != null)
        {
            var item = Ambientes[idx];
            Ambientes.Move(idx, idx - 1);
            var prefItem = _mainVm.Preferencias.Ambientes[idx];
            _mainVm.Preferencias.Ambientes.RemoveAt(idx);
            _mainVm.Preferencias.Ambientes.Insert(idx - 1, prefItem);
            _mainVm.SalvarPreferencias();
        }
    }

    private void MoverAmbienteBaixo()
    {
        int idx = ObterIndiceAmbienteSelecionado();
        if (idx >= 0 && idx < Ambientes.Count - 1 && AmbienteSelecionado != null)
        {
            var item = Ambientes[idx];
            Ambientes.Move(idx, idx + 1);
            var prefItem = _mainVm.Preferencias.Ambientes[idx];
            _mainVm.Preferencias.Ambientes.RemoveAt(idx);
            _mainVm.Preferencias.Ambientes.Insert(idx + 1, prefItem);
            _mainVm.SalvarPreferencias();
        }
    }

    private void AtivarAmbienteSelecionado()
    {
        if (AmbienteSelecionado == null) return;
        var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
        if (vm != null)
        {
            _mainVm.AmbienteAtivo = vm;
            OnPropertyChanged(nameof(EstaAtivoAmbienteSelecionado));
            OnPropertyChanged(nameof(AmbienteAtivo));
        }
    }

    private void DefinirCorAmbiente(string? corHex)
    {
        if (AmbienteSelecionado != null && !string.IsNullOrWhiteSpace(corHex))
        {
            AmbienteSelecionado.CorHex = corHex;
            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            if (vm != null) vm.CorHex = corHex;
            _mainVm.SalvarPreferencias();
            OnPropertyChanged(nameof(CorAmbienteEditavel));
        }
    }

    // Adição de itens de diferentes tipos
    private void AdicionarAppExecutavel()
    {
        if (AmbienteSelecionado == null) return;

        var dlg = new OpenFileDialog
        {
            Title = "Selecionar Aplicativo ou Atalho",
            Filter = "Aplicativos e Atalhos (*.exe;*.lnk)|*.exe;*.lnk|Todos os Arquivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() == true)
        {
            var caminho = dlg.FileName;
            var titulo = Path.GetFileNameWithoutExtension(caminho);
            var novoItem = new ItemFixado
            {
                Id = Guid.NewGuid().ToString(),
                Titulo = titulo,
                CaminhoOuUrl = caminho,
                Tipo = TipoItem.Aplicativo,
                Ordem = AmbienteSelecionado.Itens.Count
            };

            AmbienteSelecionado.Itens.Add(novoItem);
            ItensAmbiente.Add(novoItem);
            ItemSelecionado = novoItem;
            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            vm?.RecarregarItens();
            _mainVm.SalvarPreferencias();
        }
    }

    private void AdicionarArquivo()
    {
        if (AmbienteSelecionado == null) return;

        var dlg = new OpenFileDialog
        {
            Title = "Selecionar Arquivo ou Documento",
            Filter = "Todos os Arquivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() == true)
        {
            var caminho = dlg.FileName;
            var titulo = Path.GetFileName(caminho);
            var novoItem = new ItemFixado
            {
                Id = Guid.NewGuid().ToString(),
                Titulo = titulo,
                CaminhoOuUrl = caminho,
                Tipo = TipoItem.Arquivo,
                Ordem = AmbienteSelecionado.Itens.Count
            };

            AmbienteSelecionado.Itens.Add(novoItem);
            ItensAmbiente.Add(novoItem);
            ItemSelecionado = novoItem;
            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            vm?.RecarregarItens();
            _mainVm.SalvarPreferencias();
        }
    }

    private void AdicionarPasta()
    {
        if (AmbienteSelecionado == null) return;

        var dlg = new OpenFolderDialog
        {
            Title = "Selecionar Pasta para Fixar"
        };

        if (dlg.ShowDialog() == true)
        {
            var caminho = dlg.FolderName;
            var titulo = Path.GetFileName(caminho);
            if (string.IsNullOrWhiteSpace(titulo)) titulo = caminho;

            var novoItem = new ItemFixado
            {
                Id = Guid.NewGuid().ToString(),
                Titulo = titulo,
                CaminhoOuUrl = caminho,
                Tipo = TipoItem.Pasta,
                Ordem = AmbienteSelecionado.Itens.Count
            };

            AmbienteSelecionado.Itens.Add(novoItem);
            ItensAmbiente.Add(novoItem);
            ItemSelecionado = novoItem;
            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            vm?.RecarregarItens();
            _mainVm.SalvarPreferencias();
        }
    }

    private void AdicionarSiteUrl()
    {
        if (AmbienteSelecionado == null) return;

        var url = PedirTexto?.Invoke("Adicionar Site / URL", "Digite o endereço web (ex: https://github.com):");
        if (string.IsNullOrWhiteSpace(url)) return;

        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            MostrarAlerta?.Invoke("URL Inválida", "Por favor, digite um endereço de site válido.");
            return;
        }

        var titulo = PedirTexto?.Invoke("Nome do Site", "Digite o nome de exibição:");
        if (string.IsNullOrWhiteSpace(titulo))
        {
            titulo = new Uri(url).Host;
        }

        var novoItem = new ItemFixado
        {
            Id = Guid.NewGuid().ToString(),
            Titulo = titulo.Trim(),
            CaminhoOuUrl = url,
            Tipo = TipoItem.WebUrl,
            Ordem = AmbienteSelecionado.Itens.Count
        };

        AmbienteSelecionado.Itens.Add(novoItem);
        ItensAmbiente.Add(novoItem);
        ItemSelecionado = novoItem;
        var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
        vm?.RecarregarItens();
        _mainVm.SalvarPreferencias();
    }

    private void AdicionarColecaoAmbiente()
    {
        if (AmbienteSelecionado == null) return;

        var nome = PedirTexto?.Invoke("Nova Coleção no Ambiente", "Digite o nome da coleção:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        var nova = new ColecaoApp
        {
            Id = "col-" + Guid.NewGuid().ToString("N")[..8],
            Nome = nome.Trim(),
            Icone = "📁",
            EhGlobal = false,
            Ordem = AmbienteSelecionado.Colecoes.Count
        };

        AmbienteSelecionado.Colecoes.Add(nova);
        ColecoesAmbiente.Add(nova);
        ColecaoSelecionada = nova;
        var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
        vm?.RecarregarColecoes();
        _mainVm.SalvarPreferencias();
    }

    private void EditarItemAmbiente()
    {
        if (AmbienteSelecionado == null || ItemSelecionado == null) return;

        var editado = AbrirDialogoItem?.Invoke(ItemSelecionado);
        if (editado != null)
        {
            ItemSelecionado.Titulo = editado.Titulo;
            ItemSelecionado.CaminhoOuUrl = editado.CaminhoOuUrl;
            ItemSelecionado.Tipo = editado.Tipo;
            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            vm?.RecarregarItens();
            _mainVm.SalvarPreferencias();
            CarregarDadosAmbienteSelecionado();
        }
    }

    private void RemoverItemAmbiente()
    {
        if (AmbienteSelecionado == null || ItemSelecionado == null) return;

        bool confirmar = ConfirmarAcao?.Invoke(
            "Confirmar Remoção",
            $"Deseja remover o item '{ItemSelecionado.Titulo}' deste ambiente?") ?? true;
        if (!confirmar) return;

        var item = ItemSelecionado;
        if (ItemSelecionadoEhGlobal) _mainVm.RemoverAppPermanenteDireto(item.Id, item.CaminhoOuUrl);
        else AmbienteSelecionado.Itens.Remove(item);
        ItensAmbiente.Remove(item);
        var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
        vm?.RecarregarItens();
        _mainVm.SalvarPreferencias();
        _mainVm.CarregarAplicativos();
        ItemSelecionado = ItensAmbiente.FirstOrDefault();
    }

    private void MoverItemCima()
    {
        if (AmbienteSelecionado == null || ItemSelecionado == null) return;
        int idx = ItensAmbiente.IndexOf(ItemSelecionado);
        if (idx > 0)
        {
            ItensAmbiente.Move(idx, idx - 1);
            var globais = _mainVm.Preferencias.AppsPermanentes.Select(i => i.Id).ToHashSet();
            _mainVm.Preferencias.AppsPermanentes = ItensAmbiente.Where(i => globais.Contains(i.Id)).ToList();
            AmbienteSelecionado.Itens = ItensAmbiente.Where(i => !globais.Contains(i.Id)).ToList();
            for (int i = 0; i < _mainVm.Preferencias.AppsPermanentes.Count; i++) _mainVm.Preferencias.AppsPermanentes[i].Ordem = i;
            for (int i = 0; i < AmbienteSelecionado.Itens.Count; i++) AmbienteSelecionado.Itens[i].Ordem = i;
            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            vm?.RecarregarItens();
            _mainVm.SalvarPreferencias();
        }
    }

    private void MoverItemBaixo()
    {
        if (AmbienteSelecionado == null || ItemSelecionado == null) return;
        int idx = ItensAmbiente.IndexOf(ItemSelecionado);
        if (idx >= 0 && idx < ItensAmbiente.Count - 1)
        {
            ItensAmbiente.Move(idx, idx + 1);
            var globais = _mainVm.Preferencias.AppsPermanentes.Select(i => i.Id).ToHashSet();
            _mainVm.Preferencias.AppsPermanentes = ItensAmbiente.Where(i => globais.Contains(i.Id)).ToList();
            AmbienteSelecionado.Itens = ItensAmbiente.Where(i => !globais.Contains(i.Id)).ToList();
            for (int i = 0; i < _mainVm.Preferencias.AppsPermanentes.Count; i++) _mainVm.Preferencias.AppsPermanentes[i].Ordem = i;
            for (int i = 0; i < AmbienteSelecionado.Itens.Count; i++) AmbienteSelecionado.Itens[i].Ordem = i;
            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            vm?.RecarregarItens();
            _mainVm.SalvarPreferencias();
        }
    }

    private void AlternarEscopoItem()
    {
        if (AmbienteSelecionado == null || ItemSelecionado == null) return;

        var item = ItemSelecionado;
        if (ItemSelecionadoEhGlobal)
        {
            // Mover de Global para Ambiente
            _mainVm.RemoverAppPermanenteDireto(item.Id, item.CaminhoOuUrl);
            if (!AmbienteSelecionado.Itens.Any(i => i.Id == item.Id))
            {
                item.Ordem = AmbienteSelecionado.Itens.Count;
                AmbienteSelecionado.Itens.Add(item);
                if (!ItensAmbiente.Contains(item)) ItensAmbiente.Add(item);
            }
        }
        else
        {
            // Mover de Ambiente para Global
            AmbienteSelecionado.Itens.Remove(item);
            _mainVm.RemoverAppPermanenteDireto(item.Id, item.CaminhoOuUrl);
            _mainVm.AdicionarAppPermanenteDireto(item);
        }

        var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
        vm?.RecarregarItens();
        _mainVm.SalvarPreferencias();

        ItemSelecionado = item;
        _mainVm.CarregarAplicativos();
        OnPropertyChanged(nameof(ItemSelecionadoEhGlobal));
        OnPropertyChanged(nameof(TextoEscopoItemSelecionado));
    }

    private void NovaColecao()
    {
        var nome = PedirTexto?.Invoke("Nova Coleção", "Digite o nome da coleção:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        var nova = new ColecaoApp
        {
            Id = "col-" + Guid.NewGuid().ToString("N")[..8],
            Nome = nome.Trim(),
            Icone = "📁",
            EhGlobal = true,
            Ordem = _mainVm.ColecoesGlobais.Count
        };

        _mainVm.Preferencias.ColecoesGlobais.Add(nova);
        _mainVm.CarregarColecoes();
        _mainVm.SalvarPreferencias();
        CarregarDadosAmbienteSelecionado();
    }

    private void ExcluirColecao()
    {
        if (ColecaoSelecionada == null) return;

        bool confirmar = ConfirmarAcao?.Invoke("Excluir Coleção", $"Deseja remover a coleção '{ColecaoSelecionada.Nome}'?") ?? true;
        if (!confirmar) return;

        _mainVm.Preferencias.ColecoesGlobais.RemoveAll(c => c.Id == ColecaoSelecionada.Id);
        if (AmbienteSelecionado != null)
        {
            AmbienteSelecionado.Colecoes.RemoveAll(c => c.Id == ColecaoSelecionada.Id);
        }
        _mainVm.CarregarColecoes();
        _mainVm.SalvarPreferencias();
        CarregarDadosAmbienteSelecionado();
    }

    private void AdicionarItemColecao()
    {
        if (ColecaoSelecionada == null) return;

        var novo = AbrirDialogoItem?.Invoke(null);
        if (novo != null)
        {
            novo.Ordem = ColecaoSelecionada.Itens.Count;
            ColecaoSelecionada.Itens.Add(novo);
            _mainVm.CarregarColecoes();
            _mainVm.SalvarPreferencias();
            OnPropertyChanged(nameof(ColecaoSelecionada));
        }
    }

    private void RemoverItemColecao(ItemFixado? item)
    {
        if (ColecaoSelecionada == null || item == null) return;

        ColecaoSelecionada.Itens.Remove(item);
        _mainVm.CarregarColecoes();
        _mainVm.SalvarPreferencias();
        OnPropertyChanged(nameof(ColecaoSelecionada));
    }

    // Widgets
    private void MoverWidgetCima()
    {
        if (AmbienteSelecionado == null || WidgetSelecionado == null) return;
        int idx = WidgetsAmbiente.IndexOf(WidgetSelecionado);
        if (idx > 0)
        {
            WidgetsAmbiente.Move(idx, idx - 1);
            AmbienteSelecionado.WidgetsInstalados = WidgetsAmbiente.ToList();
            for (int i = 0; i < WidgetsAmbiente.Count; i++) WidgetsAmbiente[i].Ordem = i;
            _mainVm.SalvarPreferencias();
        }
    }

    private void MoverWidgetBaixo()
    {
        if (AmbienteSelecionado == null || WidgetSelecionado == null) return;
        int idx = WidgetsAmbiente.IndexOf(WidgetSelecionado);
        if (idx >= 0 && idx < WidgetsAmbiente.Count - 1)
        {
            WidgetsAmbiente.Move(idx, idx + 1);
            AmbienteSelecionado.WidgetsInstalados = WidgetsAmbiente.ToList();
            for (int i = 0; i < WidgetsAmbiente.Count; i++) WidgetsAmbiente[i].Ordem = i;
            _mainVm.SalvarPreferencias();
        }
    }

    private void EscolherEstiloWidget(WidgetInstanceConfig? widget)
    {
        if (widget == null || AmbienteSelecionado == null) return;
        var janela = new Views.EstilosWidgetWindow(widget, AmbienteSelecionado.Nome)
        {
            Owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w is Views.AjustesWindow) ?? System.Windows.Application.Current.MainWindow
        };
        if (janela.ShowDialog() == true && janela.EstiloSelecionado != null)
            AplicarEstiloWidget(widget, janela.EstiloSelecionado);
    }

    public void AplicarEstiloWidget(WidgetInstanceConfig widget, string estilo)
    {
        if (AmbienteSelecionado == null || !AmbienteSelecionado.WidgetsInstalados.Contains(widget)) return;
        if (!EstilosWidget.Aplicar(widget, estilo)) return;
        AlternarVisibilidadeWidget(widget);
        var index = WidgetsAmbiente.IndexOf(widget);
        if (index >= 0) { WidgetsAmbiente.RemoveAt(index); WidgetsAmbiente.Insert(index, widget); }
        WidgetSelecionado = widget;
    }

    private void AlternarFormatoWidget(WidgetInstanceConfig? widget) => EscolherEstiloWidget(widget);

        private void AlternarVisibilidadeWidget(WidgetInstanceConfig? wgt)
    {
        if (wgt != null)
        {
            var ambienteVm = _mainVm.Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado?.Id);
            if (ambienteVm != null && AmbienteSelecionado != null)
            {
                ambienteVm.WidgetsInstalados.Clear();
                foreach (var widget in AmbienteSelecionado.WidgetsInstalados)
                    ambienteVm.WidgetsInstalados.Add(widget);
            }
            // Cada ambiente tem sua própria configuração de widgets.
            // Só reflete na dock se o ambiente sendo editado for o ambiente ativo.
            if (_mainVm.AmbienteAtivo != null && AmbienteSelecionado != null &&
                _mainVm.AmbienteAtivo.Id == AmbienteSelecionado.Id)
            {
                _mainVm.SincronizarWidgetsAmbiente(_mainVm.AmbienteAtivo);
                _mainVm.AtualizarCoresTema(); // Update visibility states in UI
            }
            _mainVm.SalvarPreferencias();
        }
    }

        private void RemoverWidget()
    {
        if (AmbienteSelecionado == null || WidgetSelecionado == null) return;

        // Remove do ViewModel Principal se estiver visível
        WidgetSelecionado.Visivel = false;
        AlternarVisibilidadeWidget(WidgetSelecionado);

        AmbienteSelecionado.WidgetsInstalados.Remove(WidgetSelecionado);
        WidgetsAmbiente.Remove(WidgetSelecionado);
        AlternarVisibilidadeWidget(WidgetSelecionado);

        for (int i = 0; i < WidgetsAmbiente.Count; i++) WidgetsAmbiente[i].Ordem = i;
        _mainVm.SalvarPreferencias();
        WidgetSelecionado = WidgetsAmbiente.FirstOrDefault();
    }

    private void AbrirLojaWidgets()
    {
        if (AmbienteSelecionado == null) return;

        // Passa os tipos já instalados para a loja saber o que mostrar como "Remover"
        var tiposJaInstalados = WidgetsAmbiente.Select(w => w.Tipo).ToList();
        var janelaLoja = new Views.LojaWidgetsWindow(WidgetsAmbiente.ToList(), AmbienteSelecionado.Nome);
        janelaLoja.Owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.GetType().Name == "AjustesWindow") ?? System.Windows.Application.Current.MainWindow;
        janelaLoja.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        if (janelaLoja.ShowDialog() == true)
        {
            if (janelaLoja.Removeu && janelaLoja.WidgetParaRemover != null)
            {
                // Remover widget do tipo especificado
                var wgtRemover = !string.IsNullOrWhiteSpace(janelaLoja.WidgetIdParaRemover)
                    ? WidgetsAmbiente.FirstOrDefault(w => w.Id == janelaLoja.WidgetIdParaRemover)
                    : WidgetsAmbiente.FirstOrDefault(w => w.Tipo == janelaLoja.WidgetParaRemover);
                if (wgtRemover != null)
                {
                    wgtRemover.Visivel = false; // <<< OBRIGATORIO: desativa antes de atualizar o MainViewModel
                    WidgetsAmbiente.Remove(wgtRemover);
                    AmbienteSelecionado.WidgetsInstalados.Remove(wgtRemover);
                    AlternarVisibilidadeWidget(wgtRemover); // Disable in MainVM
                    _mainVm.SalvarPreferencias();
                    WidgetSelecionado = WidgetsAmbiente.FirstOrDefault();
                }
            }
            else if (janelaLoja.WidgetSelecionado != null)
            {
                // Adicionar novo widget
                var novoWidget = janelaLoja.WidgetSelecionado;
                var existente = WidgetCapabilities.PermiteMultiplasInstancias(novoWidget.Tipo)
                    ? null
                    : WidgetsAmbiente.FirstOrDefault(w => w.Tipo == novoWidget.Tipo);
                if (existente != null)
                {
                    AplicarEstiloWidget(existente, novoWidget.Estilo);
                }
                else
                {
                    novoWidget.Ordem = WidgetsAmbiente.Count;
                    WidgetsAmbiente.Add(novoWidget);
                    AmbienteSelecionado.WidgetsInstalados.Add(novoWidget);
                    AlternarVisibilidadeWidget(novoWidget);
                    WidgetSelecionado = novoWidget;
                }
            }
        }
    }

    private void AbrirGaleriaApps()
    {
        if (AmbienteSelecionado == null) return;

        var iconService = new DockWindows.Infrastructure.Windows.IconExtractionService();
        var janelaGaleria = new Views.AppGalleryWindow(iconService, AmbienteSelecionado.Itens);
        janelaGaleria.Owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.GetType().Name == "AjustesWindow") ?? System.Windows.Application.Current.MainWindow;
        janelaGaleria.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

        if (janelaGaleria.ShowDialog() == true)
        {
            // Sincroniza a seleção
            AmbienteSelecionado.Itens = janelaGaleria.ItensSelecionadosFinal;

            ItensAmbiente.Clear();
            foreach (var it in AmbienteSelecionado.Itens)
                ItensAmbiente.Add(it);

            var vm = Ambientes.FirstOrDefault(a => a.Id == AmbienteSelecionado.Id);
            vm?.RecarregarItens();
            if (_mainVm.AmbienteAtivo?.Id == AmbienteSelecionado.Id)
                _mainVm.CarregarAplicativos();

            _mainVm.SalvarPreferencias();
        }
    }

    private void ProcurarArquivoIcs()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar Arquivo de Calendário",
            Filter = "Arquivos iCalendar (*.ics)|*.ics|Todos os arquivos (*.*)|*.*",
            Multiselect = false
        };

        if (dlg.ShowDialog() == true)
        {
            UrlIcal = dlg.FileName;
        }
    }

    private void NovoCompromisso()
    {
        var titulo = PedirTexto?.Invoke("Novo Compromisso", "Título do compromisso ou evento:");
        if (string.IsNullOrWhiteSpace(titulo)) return;

        var novo = new CompromissoLocal
        {
            Id = Guid.NewGuid().ToString(),
            Titulo = titulo.Trim(),
            DataHora = DateTime.Now.AddHours(1),
            Descricao = "Lembrete configurado nas opções"
        };

        Compromissos.Add(novo);
        _mainVm.SalvarCompromissosAmbiente(Compromissos);
        CompromissoSelecionado = novo;
    }

    private void RemoverCompromisso()
    {
        if (CompromissoSelecionado == null) return;
        Compromissos.Remove(CompromissoSelecionado);
        _mainVm.SalvarCompromissosAmbiente(Compromissos);
        CompromissoSelecionado = Compromissos.FirstOrDefault();
    }

    // Espaçadores
    private void NovoEspacador()
    {
        var nome = PedirTexto?.Invoke("Novo Espaçador", "Nome do divisor (ex: Divisor Personalizado):");
        if (string.IsNullOrWhiteSpace(nome)) nome = $"Divisor {Espacadores.Count + 1}";

        var novo = new EspacadorConfig
        {
            Id = "esp-" + Guid.NewGuid().ToString("N")[..8],
            Nome = nome.Trim(),
            Estilo = EstiloEspacador.Linha,
            Largura = 8,
            Visivel = true,
            Ordem = Espacadores.Count
        };

        Espacadores.Add(novo);
        _mainVm.Preferencias.Espacadores = Espacadores.ToList();
        _mainVm.SalvarPreferencias();
        EspacadorSelecionado = novo;
    }

    private void RemoverEspacador()
    {
        if (EspacadorSelecionado == null) return;
        bool confirmar = ConfirmarAcao?.Invoke("Excluir Espaçador", $"Deseja remover o divisor '{EspacadorSelecionado.Nome}'?") ?? true;
        if (!confirmar) return;

        Espacadores.Remove(EspacadorSelecionado);
        _mainVm.Preferencias.Espacadores = Espacadores.ToList();
        _mainVm.SalvarPreferencias();
        EspacadorSelecionado = Espacadores.FirstOrDefault();
    }

    private void MoverEspacadorCima()
    {
        if (EspacadorSelecionado == null) return;
        int idx = Espacadores.IndexOf(EspacadorSelecionado);
        if (idx > 0)
        {
            Espacadores.Move(idx, idx - 1);
            for (int i = 0; i < Espacadores.Count; i++) Espacadores[i].Ordem = i;
            _mainVm.Preferencias.Espacadores = Espacadores.ToList();
            _mainVm.SalvarPreferencias();
        }
    }

    private void MoverEspacadorBaixo()
    {
        if (EspacadorSelecionado == null) return;
        int idx = Espacadores.IndexOf(EspacadorSelecionado);
        if (idx >= 0 && idx < Espacadores.Count - 1)
        {
            Espacadores.Move(idx, idx + 1);
            for (int i = 0; i < Espacadores.Count; i++) Espacadores[i].Ordem = i;
            _mainVm.Preferencias.Espacadores = Espacadores.ToList();
            _mainVm.SalvarPreferencias();
        }
    }

    // Temas
    private void SelecionarTema(TemaDefinicao? tema)
    {
        if (tema != null)
        {
            TemaSelecionado = tema;
            EstiloTema = tema.Estilo;
            RaioCantosDock = tema.RaioCantos;
            OpacidadeDock = tema.OpacidadePadrao;
            _mainVm.SalvarPreferencias();
        }
    }

    // Utilitários
    private void ExportarBackup()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Exportar Configurações do GigaDock",
            Filter = "Arquivo JSON (*.json)|*.json",
            FileName = $"dockwindows-backup-{DateTime.Now:yyyyMMdd-HHmm}.json"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                var json = JsonSerializer.Serialize(_mainVm.Preferencias, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dlg.FileName, json);
                MostrarAlerta?.Invoke("Backup Realizado", $"Configurações exportadas com sucesso para:\n{dlg.FileName}");
            }
            catch (Exception ex)
            {
                MostrarAlerta?.Invoke("Erro no Backup", $"Falha ao exportar arquivo: {ex.Message}");
            }
        }
    }

    private void ImportarBackup()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Importar Configurações do GigaDock",
            Filter = "Arquivo JSON (*.json)|*.json",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dlg.FileName);
                var prefs = JsonSerializer.Deserialize<Preferencias>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (prefs != null && prefs.Ambientes.Count > 0)
                {
                    _repo.Salvar(prefs);
                    MostrarAlerta?.Invoke("Backup Restaurado", "Configurações importadas com sucesso! O aplicativo será reiniciado para aplicar as alterações.");
                    Process.Start(Environment.ProcessPath ?? "DockWindows.App.exe");
                    Application.Current.Shutdown();
                }
                else
                {
                    MostrarAlerta?.Invoke("Arquivo Inválido", "O arquivo JSON selecionado não contém uma configuração válida do GigaDock.");
                }
            }
            catch (Exception ex)
            {
                MostrarAlerta?.Invoke("Erro na Restauração", $"Falha ao importar configurações: {ex.Message}");
            }
        }
    }

    private void RestaurarPadroesFabrica()
    {
        bool confirmar = ConfirmarAcao?.Invoke(
            "Restaurar Padrões de Fábrica",
            "Esta ação redefinirá todos os ambientes (Trabalho, Estudos e Pessoal), aplicativos, temas e opções para a configuração original de fábrica.\n\nDeseja continuar?") ?? true;

        if (confirmar)
        {
            var padrao = Preferencias.CriarPadrao();
            _repo.Salvar(padrao);
            MostrarAlerta?.Invoke("Redefinição Concluída", "Configurações restauradas com sucesso para os padrões originais.");
            Process.Start(Environment.ProcessPath ?? "DockWindows.App.exe");
            Application.Current.Shutdown();
        }
    }

    private void AbrirPastaScripts()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var pastaTools = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\tools"));
            if (!Directory.Exists(pastaTools))
            {
                pastaTools = Path.Combine(baseDir, "tools");
            }
            if (!Directory.Exists(pastaTools))
            {
                pastaTools = baseDir;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = pastaTools,
                UseShellExecute = true
            });
        }
        catch { }
    }
}

























