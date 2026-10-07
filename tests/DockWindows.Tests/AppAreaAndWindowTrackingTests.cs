using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;
using Xunit;

namespace DockWindows.Tests;

public class AppAreaAndWindowTrackingTests
{
    [Fact]
    public void AlertaChamada_PriorizaCorFixaSobreRgbGamerEMensagens()
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.AlertasVisuaisHabilitados = true;
        using var main = new MainViewModel(new FakeSettingsRepository(prefs), new FakeLauncherService(),
            new FakeIconExtractionService(), new FakeAutostartService(), windowTrackingService: new FakeWindowTrackingService());
        main.ModoGamerRgb = true;
        Assert.True(main.GlowRgbVisivel);
        main.DispararAlertaGlobal("#25D366", true);
        Assert.True(main.AlertaChamada);
        Assert.True(main.EstaEmAlerta);
        Assert.False(main.GlowRgbVisivel);
        main.DispararAlertaGlobal("#8B7CFF");
        Assert.Equal("#25D366", main.CorAlerta);
        Assert.True(main.AlertaChamada);
        main.AlertasVisuaisHabilitados = false;
        Assert.False(main.EstaEmAlerta);
        Assert.False(main.AlertaChamada);
        Assert.True(main.GlowRgbVisivel);
    }

    private sealed class FakeLixeiraDesktopService : ILixeiraDesktopService
    {
        public List<bool> Visibilidades { get; } = new();
        public bool Sucesso { get; set; } = true;
        public bool ConfigurarVisibilidade(bool visivel, out string? erro)
        {
            Visibilidades.Add(visivel);
            erro = Sucesso ? null : "Acesso negado";
            return Sucesso;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Lixeira_ApenasMudancaExplicitaAlteraDesktop(bool inicialmenteAtiva)
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.ExibirLixeira = inicialmenteAtiva;
        var repo = new FakeSettingsRepository(prefs);
        var desktop = new FakeLixeiraDesktopService();
        using var main = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), windowTrackingService: new FakeWindowTrackingService(), lixeiraDesktopService: desktop);
        var ajustes = new AjustesViewModel(main, repo, new FakeAutostartService());
        Assert.Empty(desktop.Visibilidades);
        ajustes.ExibirLixeira = inicialmenteAtiva;
        Assert.Empty(desktop.Visibilidades);
        ajustes.ExibirLixeira = !inicialmenteAtiva;
        Assert.Equal(new[] { inicialmenteAtiva }, desktop.Visibilidades);
        Assert.Equal(!inicialmenteAtiva, repo.Prefs.ExibirLixeira);
        ajustes.ExibirLixeira = inicialmenteAtiva;
        Assert.Equal(new[] { inicialmenteAtiva, !inicialmenteAtiva }, desktop.Visibilidades);
        Assert.Equal(inicialmenteAtiva, repo.Prefs.ExibirLixeira);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Lixeira_FalhaMantemOpcaoAnteriorENotifica(bool inicialmenteAtiva)
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.ExibirLixeira = inicialmenteAtiva;
        var repo = new FakeSettingsRepository(prefs);
        var desktop = new FakeLixeiraDesktopService { Sucesso = false };
        using var main = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), windowTrackingService: new FakeWindowTrackingService(), lixeiraDesktopService: desktop);
        string? alerta = null;
        main.MostrarAlerta = (_, texto) => alerta = texto;
        main.ExibirLixeira = !inicialmenteAtiva;
        Assert.Equal(inicialmenteAtiva, main.ExibirLixeira);
        Assert.Equal(inicialmenteAtiva, repo.Prefs.ExibirLixeira);
        Assert.Equal("Acesso negado", alerta);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ambientes_IsolamAppsAbertosESalvamento(bool exibirNaoFixados)
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.AppsPermanentes.Clear();
        prefs.ExibirAppsAbertosNaoFixados = exibirNaoFixados;
        prefs.Ambientes = new List<Ambiente>
        {
            new() { Id = "trabalho", Nome = "Trabalho", Itens = new()
            {
                new() { Titulo = "Editor", CaminhoOuUrl = @"C:\Apps\editor.exe", Tipo = TipoItem.Aplicativo }
            }},
            new() { Id = "pessoal", Nome = "Pessoal", Itens = new()
            {
                new() { Titulo = "Chat", CaminhoOuUrl = @"C:\Apps\chat.exe", Tipo = TipoItem.Aplicativo },
                new() { Titulo = "Editor", CaminhoOuUrl = @"C:\Apps\editor.exe", Tipo = TipoItem.Aplicativo }
            }}
        };
        prefs.AmbienteAtivoId = "trabalho";
        var tracking = new FakeWindowTrackingService { Janelas = new()
        {
            new() { Hwnd = (nint)1, CaminhoExecutavel = @"C:\Apps\editor.exe", NomeProcesso = "editor" },
            new() { Hwnd = (nint)2, CaminhoExecutavel = @"C:\Apps\chat.exe", NomeProcesso = "chat" },
            new() { Hwnd = (nint)3, CaminhoExecutavel = @"C:\Apps\outro.exe", NomeProcesso = "outro" }
        }};
        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: tracking);

        Assert.False(Assert.Single(vm.Aplicativos, a => a.NomeProcesso == "chat").EstaFixado);
        Assert.Contains(vm.Aplicativos, a => a.NomeProcesso == "outro");
        Assert.True(Assert.Single(vm.Aplicativos, a => a.NomeProcesso == "editor").EstaAberto);
        vm.AmbienteAtivo = vm.Ambientes.Single(a => a.Id == "pessoal");
        Assert.Single(vm.Aplicativos, a => a.NomeProcesso == "editor");
        Assert.True(Assert.Single(vm.Aplicativos, a => a.NomeProcesso == "chat").EstaFixado);
        vm.AmbienteAtivo = vm.Ambientes.Single(a => a.Id == "trabalho");
        vm.AtualizarAplicativosAbertos();
        Assert.False(Assert.Single(vm.Aplicativos, a => a.NomeProcesso == "chat").EstaFixado);
        vm.SalvarPreferencias();
        Assert.Empty(repo.Prefs.AppsPermanentes);
        Assert.Single(repo.Prefs.Ambientes[0].Itens);
        Assert.Equal(2, repo.Prefs.Ambientes[1].Itens.Count);

        var reaberto = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: tracking);
        Assert.Single(reaberto.Ambientes.Single(a => a.Id == "trabalho").Model.Itens);
        Assert.False(Assert.Single(reaberto.Aplicativos, a => a.NomeProcesso == "chat").EstaFixado);
        Assert.Equal(3, tracking.Janelas.Count);
        Assert.Equal(IntPtr.Zero, tracking.JanelaMinimizadaUltima);
    }
    [Fact]
    public void Visualizacoes_AjustesAplicamESalvamAsQuatroEscolhas()
    {
        var repo = new FakeSettingsRepository(Preferencias.CriarPadrao());
        var main = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        var ajustes = new AjustesViewModel(main, repo, new FakeAutostartService());
        Assert.False(main.PreviaJanelas);
        Assert.False(main.ClimaExpandido);
        Assert.False(main.RelogioAnalogico);
        Assert.False(main.PreviaPastas);
        ajustes.NavegarPara("Visualizacoes");
        Assert.True(ajustes.EhSecaoVisualizacoes);
        ajustes.PreviaJanelas = true;
        ajustes.ClimaExpandido = true;
        ajustes.RelogioAnalogico = true;
        ajustes.PreviaPastas = true;
        var saved = System.Text.Json.JsonSerializer.Deserialize<Preferencias>(
            System.Text.Json.JsonSerializer.Serialize(repo.Prefs))!;
        Assert.True(saved.PreviaJanelas && saved.PreviaPastas);
        var ativo = saved.Ambientes.Single(a => a.Id == saved.AmbienteAtivoId);
        Assert.Equal("analogico-digital", ativo.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Relogio).Estilo);
        Assert.True(main.ClimaExpandido);
        var notificadas = new HashSet<string?>();
        main.PropertyChanged += (_, e) => notificadas.Add(e.PropertyName);
        main.AtualizarPreferencias(Preferencias.CriarPadrao());
        Assert.False(main.PreviaJanelas || main.ClimaExpandido || main.RelogioAnalogico || main.PreviaPastas);
        Assert.Contains(nameof(MainViewModel.PreviaJanelas), notificadas);
        Assert.Contains(nameof(MainViewModel.ClimaExpandido), notificadas);
        Assert.Contains(nameof(MainViewModel.RelogioAnalogico), notificadas);
        Assert.Contains(nameof(MainViewModel.PreviaPastas), notificadas);
    }
    [Fact]
    public void EstilosWidget_EditaAmbienteInativoSemAlterarDockEPersisteAoReabrir()
    {
        var repo = new FakeSettingsRepository(Preferencias.CriarPadrao());
        var main = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        var ajustes = new AjustesViewModel(main, repo, new FakeAutostartService());
        var ativo = main.AmbienteAtivo!;
        var outro = main.Ambientes.First(a => a.Id != ativo.Id);
        ajustes.AmbienteSelecionado = outro.Model;
        var relogio = outro.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Relogio);
        ajustes.AplicarEstiloWidget(relogio, "analogico-claro");
        Assert.Equal("hora-data", main.Clock.Estilo);
        Assert.Equal("analogico-claro", relogio.Estilo);
        Assert.NotEqual("analogico-claro", ativo.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Relogio).Estilo);
        main.AmbienteAtivo = outro;
        Assert.Equal("analogico-claro", main.Clock.Estilo);
        ajustes.AplicarEstiloWidget(relogio, "segundos");
        Assert.Equal("segundos", main.Clock.Estilo);
        ajustes.AplicarEstiloWidget(relogio, "estilo-invalido");
        Assert.Equal("segundos", relogio.Estilo);
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<Preferencias>(System.Text.Json.JsonSerializer.Serialize(repo.Prefs))!;
        Assert.Equal("segundos", snapshot.Ambientes.Single(a => a.Id == outro.Id).WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Relogio).Estilo);
        var reaberto = new MainViewModel(new FakeSettingsRepository(snapshot), new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        Assert.Equal("segundos", reaberto.Clock.Estilo);
    }

    [Fact]
    public void EstilosWidget_MigraFormatoAntigoEFiltraAgenda()
    {
        var legado = new WidgetInstanceConfig { Tipo = TipoWidget.Relogio };
        Assert.Equal("hora-data", EstilosWidget.Resolver(legado));
        Assert.Equal("analogico-digital", EstilosWidget.Resolver(legado, analogicoLegado: true));
        Assert.False(EstilosWidget.Aplicar(legado, "agenda"));
        var calendario = new CalendarioWidgetViewModel { Formato = FormatoWidget.Expandido };
        calendario.Compromissos.Add(new CompromissoLocal { Titulo = "Hoje", DataHora = DateTime.Today.AddHours(12) });
        calendario.Compromissos.Add(new CompromissoLocal { Titulo = "Amanhã", DataHora = DateTime.Today.AddDays(1).AddHours(12) });
        Assert.Equal("Hoje", Assert.Single(calendario.EventosVisiveis).Titulo);
        calendario.Formato = FormatoWidget.Compacto;
        Assert.All(calendario.EventosVisiveis, e => Assert.True(e.DataHora >= DateTime.Now));
        var clock = new ClockWidgetViewModel { Estilo = "temporizador" };
        Assert.StartsWith("5:00", clock.TempoControle);
        clock.AcaoPrincipalCommand.Execute(null);
        Assert.Contains("Ⅱ", clock.TempoControle);
        clock.ReiniciarControleCommand.Execute(null);
        Assert.Equal("5:00 ▶", clock.TempoControle);
    }

    [Fact]
    public void EstilosMidiaBateria_SaoIndependentesPorAmbienteERemocaoPersiste()
    {
        var repo = new FakeSettingsRepository(Preferencias.CriarPadrao());
        var main = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        var primeiro = main.AmbienteAtivo!;
        var segundo = main.Ambientes.First(a => a.Id != primeiro.Id);
        var ajustes = new AjustesViewModel(main, repo, new FakeAutostartService()) { AmbienteSelecionado = primeiro.Model };
        var midia = primeiro.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Midia);
        var bateria = primeiro.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Bateria);
        ajustes.AplicarEstiloWidget(midia, "mini");
        ajustes.AplicarEstiloWidget(bateria, "anel");
        main.ExibirBateria = true;
        Assert.Equal("mini", main.Midia.Estilo);
        Assert.Equal("anel", main.Bateria.Estilo);
        main.AmbienteAtivo = segundo;
        Assert.Equal("capa", main.Midia.Estilo);
        Assert.Equal("compacto", main.Bateria.Estilo);
        Assert.False(main.Bateria.Habilitado);
        main.AmbienteAtivo = primeiro;
        Assert.True(main.Bateria.Habilitado);
        ajustes.WidgetSelecionado = midia;
        ajustes.RemoverWidgetCommand.Execute(null);
        Assert.False(main.Midia.Habilitado);
        main.SincronizarWidgetsAmbiente(primeiro);
        Assert.DoesNotContain(primeiro.WidgetsInstalados, w => w.Tipo == TipoWidget.Midia);
        main.SalvarPreferencias();
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<Preferencias>(System.Text.Json.JsonSerializer.Serialize(repo.Prefs))!;
        Assert.Equal("anel", snapshot.Ambientes.Single(a => a.Id == primeiro.Id).WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Bateria).Estilo);
        var reaberto = new MainViewModel(new FakeSettingsRepository(snapshot), new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        Assert.False(reaberto.Midia.Habilitado);
        Assert.Equal("anel", reaberto.Bateria.Estilo);
    }

    [Fact]
    public void PersonalizarClima_AplicaEstilosNoAmbienteCorretoEPreservaVisibilidade()
    {
        var repo = new FakeSettingsRepository(Preferencias.CriarPadrao());
        var main = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(),
            new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        var ativo = main.AmbienteAtivo!;
        var outro = main.Ambientes.First(a => a.Id != ativo.Id);
        var climaAtivo = ativo.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Clima);
        var climaOutro = outro.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Clima);
        var visivel = climaOutro.Visivel;
        Assert.True(main.AplicarEstiloAmbiente(outro.Id, climaOutro.Id, "vento"));
        Assert.Equal("compacto", main.Clima.Estilo);
        Assert.Equal(visivel, climaOutro.Visivel);
        main.AmbienteAtivo = outro;
        Assert.Equal("vento", main.Clima.Estilo);
        Assert.True(main.AplicarEstiloAmbiente(outro.Id, climaOutro.Id, "sol"));
        Assert.Equal("sol", main.Clima.Estilo);
        Assert.False(main.AplicarEstiloAmbiente(outro.Id, climaOutro.Id, "invalido"));
        main.AmbienteAtivo = ativo;
        Assert.Equal("compacto", main.Clima.Estilo);
        Assert.Equal("compacto", climaAtivo.Estilo);
        var salvo = repo.Prefs.Ambientes.Single(a => a.Id == outro.Id).WidgetsInstalados.Single(w => w.Tipo == TipoWidget.Clima);
        Assert.Equal("sol", salvo.Estilo);
        Assert.Equal(9, EstilosWidget.Para(TipoWidget.Clima).Count);
    }

    [Fact]
    public void AppFixadoEmOutroAmbiente_ApareceEnquantoAberto()
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.ExibirAppsAbertosNaoFixados = false;
        foreach (var ambiente in prefs.Ambientes) ambiente.Itens.Clear();
        var ativo = prefs.Ambientes.First(a => a.Id == prefs.AmbienteAtivoId);
        var outro = prefs.Ambientes.First(a => a.Id != ativo.Id);
        outro.Itens.Add(new ItemFixado
        {
            Id = "app-outro-ambiente",
            Titulo = "Aplicativo de outro ambiente",
            CaminhoOuUrl = @"C:\Apps\OutroApp.exe",
            Tipo = TipoItem.Aplicativo
        });
        var tracking = new FakeWindowTrackingService
        {
            Janelas = new List<JanelaInfo>
            {
                new() { Hwnd = (nint)404, Titulo = "Outro App", NomeProcesso = "OutroApp", CaminhoExecutavel = @"C:\Apps\OutroApp.exe" }
            }
        };

        using var vm = new MainViewModel(new FakeSettingsRepository(prefs), new FakeLauncherService(),
            new FakeIconExtractionService(), new FakeAutostartService(), windowTrackingService: tracking);

        var exibido = Assert.Single(vm.Aplicativos, a => a.CaminhoExecutavel.EndsWith("OutroApp.exe", StringComparison.OrdinalIgnoreCase));
        Assert.False(exibido.EstaFixado);
        Assert.True(exibido.EstaAberto);
    }

    [Fact]
    public void FecharUltimaJanela_LimpaEstadoAtivoImediatamente()
    {
        var janela = new JanelaInfo
        {
            Hwnd = (nint)405, Titulo = "Editor", NomeProcesso = "Editor",
            CaminhoExecutavel = @"C:\Apps\Editor.exe", EstaAtiva = true
        };
        var tracking = new FakeWindowTrackingService { Janelas = new List<JanelaInfo> { janela } };
        var app = new AppItemViewModel(janela, tracking, new FakeIconExtractionService(), _ => { }, _ => { });

        app.FecharTodasJanelasCommand.Execute(null);

        Assert.False(app.EstaAberto);
        Assert.False(app.EstaAtivo);
        Assert.Equal(0, app.QuantidadeJanelas);
    }

    [Fact]
    public void ExibirDock_LimpaEstadoAntigoDeTelaCheia()
    {
        var prefs = Preferencias.CriarPadrao();
        using var vm = new MainViewModel(new FakeSettingsRepository(prefs), new FakeLauncherService(),
            new FakeIconExtractionService(), new FakeAutostartService(), windowTrackingService: new FakeWindowTrackingService());
        vm.DockVisivel = true;
        vm.OcultoPorTelaCheia = true;

        vm.AlternarVisibilidade();

        Assert.True(vm.DockVisivel);
        Assert.False(vm.OcultoPorTelaCheia);
    }

    private class FakeWindowTrackingService : IWindowTrackingService
    {
        public List<JanelaInfo> Janelas { get; set; } = new();
        public IntPtr JanelaAtivadaUltima { get; private set; }
        public IntPtr JanelaMinimizadaUltima { get; private set; }
        public IntPtr JanelaFechadaUltima { get; private set; }

        public event Action? JanelasAlteradas;
        public event Action<IntPtr>? JanelaAtivada;
        public event Action<bool>? TelaCheiaAlterada
        {
            add { }
            remove { }
        }

        public IReadOnlyList<JanelaInfo> ObterJanelasAbertas() => Janelas;

        public IntPtr ObterJanelaAtiva() => Janelas.FirstOrDefault(j => j.EstaAtiva)?.Hwnd ?? IntPtr.Zero;

        public void Iniciar() { }
        public void Parar() { }

        public bool AtivarJanela(IntPtr hWnd)
        {
            JanelaAtivadaUltima = hWnd;
            JanelaAtivada?.Invoke(hWnd);
            return true;
        }

        public bool MinimizarJanela(IntPtr hWnd)
        {
            JanelaMinimizadaUltima = hWnd;
            return true;
        }

        public bool FecharJanela(IntPtr hWnd)
        {
            JanelaFechadaUltima = hWnd;
            Janelas.RemoveAll(j => j.Hwnd == hWnd);
            JanelasAlteradas?.Invoke();
            return true;
        }

        public void DispararJanelasAlteradas() => JanelasAlteradas?.Invoke();

        public void Dispose() { }
    }

    private class FakeSettingsRepository : ISettingsRepository
    {
        public Preferencias Prefs { get; set; }

        public FakeSettingsRepository(Preferencias prefs)
        {
            Prefs = prefs;
        }

        public Preferencias Carregar() => Prefs;
        public void Salvar(Preferencias prefs) => Prefs = prefs;
        public Task SalvarAsync(Preferencias prefs)
        {
            Prefs = prefs;
            return Task.CompletedTask;
        }
        public string ObterCaminhoConfiguracoes() => @"C:\Fake\settings.json";
    }

    private class FakeLauncherService : ILauncherService
    {
        public LaunchResult Executar(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult AbrirLocal(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult ExecutarCaminho(string caminho, string? argumentos = null) => LaunchResult.Ok();
    }

    private class FakeIconExtractionService : IIconExtractionService
    {
        public ImageSource? ObterIcone(ItemFixado item) => null;
        public ImageSource? ObterIcone(string caminhoOuUrl, TipoItem tipo = TipoItem.Aplicativo) => null;
        public ImageSource? ObterIconeJanela(IntPtr hWnd) => null;
        public System.Windows.Media.ImageSource? ObterIconeAppModernoJanela(IntPtr hWnd) => null;
    }

    private class FakeAutostartService : IAutostartService
    {
        public bool EstaHabilitado() => false;
        public bool Configurar(bool habilitar) => true;
    }

    [Fact]
    public void MesclagemDeApps_NaoDuplicaAppFixadoQuandoAberto()
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.AppsPermanentes = new List<ItemFixado>
        {
            new() { Titulo = "Bloco de Notas", CaminhoOuUrl = @"C:\Windows\System32\notepad.exe", Tipo = TipoItem.Aplicativo, Ordem = 0 }
        };

        var fakeTracking = new FakeWindowTrackingService
        {
            Janelas = new List<JanelaInfo>
            {
                new()
                {
                    Hwnd = (nint)1234,
                    Titulo = "Sem tÃ­tulo - Bloco de Notas",
                    NomeProcesso = "notepad",
                    CaminhoExecutavel = @"C:\Windows\System32\notepad.exe",
                    EstaAtiva = true
                }
            }
        };

        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: fakeTracking);

        // Bloco de notas deve aparecer exatamente 1 vez na lista Aplicativos
        var appsBlocoNotas = vm.Aplicativos.Where(a => a.NomeProcesso.Equals("notepad", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(appsBlocoNotas);

        var app = appsBlocoNotas[0];
        Assert.True(app.EstaFixado, "Deveria estar marcado como fixado");
        Assert.True(app.EstaAberto, "Deveria estar marcado como aberto");
        Assert.True(app.EstaAtivo, "Deveria estar marcado como ativo no primeiro plano");
        Assert.Single(app.Janelas);
    }

    [Fact]
    public void MesclagemDeApps_ExibeAppAbertoNaoFixado()
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.ExibirAppsAbertosNaoFixados = true;
        foreach (var amb in prefs.Ambientes) amb.Itens.Clear();
        prefs.AppsPermanentes = new List<ItemFixado>(); // Sem apps fixados

        var fakeTracking = new FakeWindowTrackingService
        {
            Janelas = new List<JanelaInfo>
            {
                new()
                {
                    Hwnd = (nint)5678,
                    Titulo = "Calculadora",
                    NomeProcesso = "CalculatorApp",
                    CaminhoExecutavel = @"C:\Program Files\WindowsApps\CalculatorApp.exe",
                    EstaAtiva = false
                }
            }
        };

        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: fakeTracking);

        Assert.Single(vm.Aplicativos);
        var calc = vm.Aplicativos[0];
        Assert.False(calc.EstaFixado, "NÃ£o deve estar fixado");
        Assert.True(calc.EstaAberto, "Deve estar aberto");
        Assert.False(calc.EstaAtivo, "NÃ£o estÃ¡ em primeiro plano");
    }

    [Fact]
    public void MultiplasJanelas_ContadorEBadgeCorretos()
    {
        var prefs = Preferencias.CriarPadrao();
        prefs.AppsPermanentes = new List<ItemFixado>
        {
            new() { Titulo = "Terminal", CaminhoOuUrl = "cmd.exe", Tipo = TipoItem.Aplicativo, Ordem = 0 }
        };

        var fakeTracking = new FakeWindowTrackingService
        {
            Janelas = new List<JanelaInfo>
            {
                new() { Hwnd = (nint)101, Titulo = "Prompt 1", NomeProcesso = "cmd", CaminhoExecutavel = @"C:\Windows\System32\cmd.exe", EstaAtiva = false },
                new() { Hwnd = (nint)102, Titulo = "Prompt 2", NomeProcesso = "cmd", CaminhoExecutavel = @"C:\Windows\System32\cmd.exe", EstaAtiva = true }
            }
        };

        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: fakeTracking);

        var terminal = vm.Aplicativos.FirstOrDefault(a => a.NomeProcesso.Equals("cmd", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(terminal);
        Assert.True(terminal.TemMultiplasJanelas);
        Assert.Equal(2, terminal.QuantidadeJanelas);
        Assert.True(terminal.EstaAtivo, "Se uma das janelas estÃ¡ ativa, o app deve estar ativo");
    }

    [Fact]
    public void TrocaDeAmbiente_MostraItensDoAmbienteSelecionado()
    {
        var prefs = Preferencias.CriarPadrao();

        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());

        var appsIniciais = vm.Aplicativos.Where(a => a.EstaFixado).Select(a => a.Titulo).ToList();
        Assert.NotEmpty(appsIniciais);

        // Troca para o ambiente Estudos
        var ambEstudos = vm.Ambientes.FirstOrDefault(a => a.Nome == "Estudos");
        Assert.NotNull(ambEstudos);
        vm.AmbienteAtivo = ambEstudos;

        var appsAposTroca = vm.Aplicativos.Where(a => a.EstaFixado).Select(a => a.Titulo).ToList();
        Assert.Equal(ambEstudos.Model.Itens.OrderBy(i => i.Ordem).Select(i => i.Titulo), appsAposTroca);

        // Troca para Pessoal
        var ambPessoal = vm.Ambientes.FirstOrDefault(a => a.Nome == "Pessoal");
        Assert.NotNull(ambPessoal);
        vm.AmbienteAtivo = ambPessoal;

        var appsAposSegundaTroca = vm.Aplicativos.Where(a => a.EstaFixado).Select(a => a.Titulo).ToList();
        Assert.Equal(ambPessoal.Model.Itens.OrderBy(i => i.Ordem).Select(i => i.Titulo), appsAposSegundaTroca);
    }

    [Fact]
    public void PersonalizarDock_ReordenacaoEVisibilidadeSecoes()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new FakeSettingsRepository(prefs);
        var mainVm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        var custVm = new CustomizeViewModel(mainVm, repo);

        Assert.Equal(8, custVm.Secoes.Count);

        // Oculta a seÃ§Ã£o de Widgets
        var secWidgets = custVm.Secoes.First(s => s.Tipo == TipoSecaoDock.Widgets);
        secWidgets.Visivel = false;

        // Move a seÃ§Ã£o Apps para a primeira posiÃ§Ã£o
        var secApps = custVm.Secoes.First(s => s.Tipo == TipoSecaoDock.Apps);
        custVm.SecaoSelecionada = secApps;
        custVm.MoverSecaoCimaCommand.Execute(null);

        Assert.Equal(2, secApps.Ordem);
        Assert.False(secWidgets.Visivel);

        // Salva e aplica
        custVm.SalvarCommand.Execute(null);

        Assert.Equal(2, mainVm.OrdemSecoes.First(s => s.Tipo == TipoSecaoDock.Apps).Ordem);
        Assert.False(mainVm.OrdemSecoes.First(s => s.Tipo == TipoSecaoDock.Widgets).Visivel);
    }

    [Fact]
    public void FixarEDesafixarApp_AtualizaListaEPersistencia()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());

        int countInicial = vm.Aplicativos.Count(a => a.EstaFixado);

        // Adiciona um app permanente
        var novo = new ItemFixado
        {
            Titulo = "Meu App Teste",
            CaminhoOuUrl = @"C:\Test\test.exe",
            Tipo = TipoItem.Aplicativo
        };
        vm.AdicionarAppPermanenteDireto(novo);

        Assert.Equal(countInicial + 1, vm.Aplicativos.Count(a => a.EstaFixado));

        var appAdicionado = vm.Aplicativos.First(a => a.Titulo == "Meu App Teste");
        Assert.True(appAdicionado.EstaFixado);

        // Desafixa pelo comando do AppItemViewModel
        appAdicionado.FixarDesafixarCommand.Execute(null);
        Assert.False(appAdicionado.EstaFixado);
    }

    [Fact]
    public void FecharJanela_DisparaServicoTracking()
    {
        var janela = new JanelaInfo
        {
            Hwnd = (nint)999,
            Titulo = "Janela para Fechar",
            NomeProcesso = "app",
            CaminhoExecutavel = @"C:\app.exe"
        };
        var fakeTracking = new FakeWindowTrackingService
        {
            Janelas = new List<JanelaInfo> { janela }
        };

        var prefs = Preferencias.CriarPadrao();
        prefs.ExibirAppsAbertosNaoFixados = true;
        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: fakeTracking);

        var app = vm.Aplicativos.First(a => a.NomeProcesso == "app");
        Assert.True(app.EstaAberto);

        // Fecha todas as janelas do app
        app.FecharTodasJanelasCommand.Execute(null);

        Assert.Equal((nint)999, fakeTracking.JanelaFechadaUltima);
    }

    [Fact]
    public void ResolverCaminhoCompleto_EncontraExecutaveisDoSistema()
    {
        var caminhoExplorer = IconExtractionService.ResolverCaminhoCompleto("explorer.exe");
        Assert.True(System.IO.File.Exists(caminhoExplorer), "Deve resolver o caminho completo do explorer.exe no Windows");

        var caminhoCmd = IconExtractionService.ResolverCaminhoCompleto("cmd.exe");
        Assert.True(System.IO.File.Exists(caminhoCmd), "Deve resolver o caminho completo do cmd.exe no Windows");
    }

    [Fact]
    public void MainViewModel_AlturasBarra_CompactasEstiloApple()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new FakeSettingsRepository(prefs);
        var vm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService());

        vm.TamanhoIcones = TamanhoIcone.Pequeno;
        Assert.Equal(64.0, vm.AlturaBarra);
        Assert.Equal((double)TamanhoIcone.Pequeno, vm.TamanhoIconeNumerico);

        vm.TamanhoIcones = TamanhoIcone.Medio;
        Assert.Equal(64.0, vm.AlturaBarra);
        Assert.Equal((double)TamanhoIcone.Medio, vm.TamanhoIconeNumerico);

        vm.TamanhoIcones = TamanhoIcone.Grande;
        Assert.Equal(64.0, vm.AlturaBarra);
        Assert.Equal((double)TamanhoIcone.Grande, vm.TamanhoIconeNumerico);
    }

    [Fact]
    public void CalendarioWidget_BadgePropriedadesApple_RetornaValoresCorretos()
    {
        var calVm = new CalendarioWidgetViewModel();
        Assert.False(string.IsNullOrWhiteSpace(calVm.DiaDoMes));
        Assert.False(string.IsNullOrWhiteSpace(calVm.DiaDaSemanaCurto));
        Assert.False(string.IsNullOrWhiteSpace(calVm.TituloEventoCurto));
        Assert.False(string.IsNullOrWhiteSpace(calVm.HoraEventoCurto));
    }

    [Fact]
    public void ColecaoAppViewModel_MiniaturasEItens_AtualizamCorretamente()
    {
        var model = new ColecaoApp
        {
            Id = "col-teste",
            Nome = "Teste",
            Itens = new List<ItemFixado>
            {
                new() { Titulo = "Item 1", CaminhoOuUrl = "notepad.exe" }
            }
        };

        var colVm = new ColecaoAppViewModel(model, new FakeLauncherService(), new FakeIconExtractionService());
        Assert.True(colVm.TemItens);
        Assert.Equal(1, colVm.QuantidadeItens);
    }
}
