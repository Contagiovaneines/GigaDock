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
    private class FakeWindowTrackingService : IWindowTrackingService
    {
        public List<JanelaInfo> Janelas { get; set; } = new();
        public IntPtr JanelaAtivadaUltima { get; private set; }
        public IntPtr JanelaMinimizadaUltima { get; private set; }
        public IntPtr JanelaFechadaUltima { get; private set; }

        public event Action? JanelasAlteradas;
        public event Action<IntPtr>? JanelaAtivada;
        public event Action<bool>? TelaCheiaAlterada;

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
                    Titulo = "Sem título - Bloco de Notas",
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
        Assert.False(calc.EstaFixado, "Não deve estar fixado");
        Assert.True(calc.EstaAberto, "Deve estar aberto");
        Assert.False(calc.EstaAtivo, "Não está em primeiro plano");
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
        Assert.True(terminal.EstaAtivo, "Se uma das janelas está ativa, o app deve estar ativo");
    }

    [Fact]
    public void TrocaDeAmbiente_PreservaAppsPermanentes()
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
        Assert.Equal(appsIniciais, appsAposTroca);

        // Troca para Pessoal
        var ambPessoal = vm.Ambientes.FirstOrDefault(a => a.Nome == "Pessoal");
        Assert.NotNull(ambPessoal);
        vm.AmbienteAtivo = ambPessoal;

        var appsAposSegundaTroca = vm.Aplicativos.Where(a => a.EstaFixado).Select(a => a.Titulo).ToList();
        Assert.Equal(appsIniciais, appsAposSegundaTroca);
    }

    [Fact]
    public void PersonalizarDock_ReordenacaoEVisibilidadeSecoes()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new FakeSettingsRepository(prefs);
        var mainVm = new MainViewModel(repo, new FakeLauncherService(), new FakeIconExtractionService(), new FakeAutostartService(), taskbarService: null, windowTrackingService: new FakeWindowTrackingService());
        var custVm = new CustomizeViewModel(mainVm, repo);

        Assert.Equal(8, custVm.Secoes.Count);

        // Oculta a seção de Widgets
        var secWidgets = custVm.Secoes.First(s => s.Tipo == TipoSecaoDock.Widgets);
        secWidgets.Visivel = false;

        // Move a seção Apps para a primeira posição
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
        Assert.Equal(46.0, vm.AlturaBarra);

        vm.TamanhoIcones = TamanhoIcone.Medio;
        Assert.Equal(52.0, vm.AlturaBarra);

        vm.TamanhoIcones = TamanhoIcone.Grande;
        Assert.Equal(60.0, vm.AlturaBarra);
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

