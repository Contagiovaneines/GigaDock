using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;
using Xunit;

namespace DockWindows.Tests;

public class TaskbarSubstitutionAndWinKeyTests
{
    private class FakeRepo : ISettingsRepository
    {
        public Preferencias Prefs { get; set; } = Preferencias.CriarPadrao();
        public Preferencias Carregar() => Prefs;
        public void Salvar(Preferencias p) => Prefs = p;
        public Task SalvarAsync(Preferencias p)
        {
            Prefs = p;
            return Task.CompletedTask;
        }
        public string ObterCaminhoConfiguracoes() => "fake_settings.json";
    }

    private class FakeLauncher : ILauncherService
    {
        public LaunchResult Executar(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult AbrirLocal(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult ExecutarCaminho(string caminho, string? argumentos = null) => LaunchResult.Ok();
    }

    private class FakeIconExtraction : IIconExtractionService
    {
        public ImageSource? ObterIcone(ItemFixado item) => null;
        public ImageSource? ObterIcone(string caminhoOuUrl, TipoItem tipo = TipoItem.Aplicativo) => null;
        public ImageSource? ObterIconeJanela(IntPtr hWnd) => null;
    }

    private class FakeAutostart : IAutostartService
    {
        public bool EstaHabilitado() => false;
        public bool Configurar(bool habilitar) => true;
    }

    private class FakeTaskbar : ITaskbarService
    {
        public bool Oculto { get; private set; }
        public bool WatchdogAtivo { get; private set; }

        public int ObterEstadoAtual() => 2; // ABS_ALWAYSONTOP
        public bool OcultarBarraNativa(out int estadoAnterior)
        {
            estadoAnterior = 2;
            Oculto = true;
            WatchdogAtivo = true;
            return true;
        }
        public bool RestaurarBarraNativa(int? estadoAnterior = null)
        {
            Oculto = false;
            WatchdogAtivo = false;
            return true;
        }
        public void GarantirBarraOculta() { }
        public void Dispose()
        {
            WatchdogAtivo = false;
        }
    }

    private class FakeWindowTracking : IWindowTrackingService
    {
#pragma warning disable CS0067
        public event Action? JanelasAlteradas;
        public event Action<IntPtr>? JanelaAtivada;
        public event Action<bool>? TelaCheiaAlterada;
#pragma warning restore CS0067
        public IReadOnlyList<JanelaInfo> ObterJanelasAbertas() => Array.Empty<JanelaInfo>();
        public IntPtr ObterJanelaAtiva() => IntPtr.Zero;
        public void Iniciar() { }
        public void Parar() { }
        public bool AtivarJanela(IntPtr hWnd) => true;
        public bool MinimizarJanela(IntPtr hWnd) => true;
        public bool FecharJanela(IntPtr hWnd) => true;
        public void Dispose() { }
    }

    private class FakeWinKeyHook : IWinKeyHookService
    {
        public event Action? WinKeyTapped;
        public bool EstaAtivo { get; private set; }

        public void Iniciar() => EstaAtivo = true;
        public void Parar() => EstaAtivo = false;
        public void SimularWinKey() => WinKeyTapped?.Invoke();
        public void Dispose() => Parar();
    }

    private (MainViewModel vm, FakeTaskbar taskbar, FakeWinKeyHook hook) CriarSut(bool usarComoPrincipal = false)
    {
        var repo = new FakeRepo();
        repo.Prefs.UsarComoBarraPrincipal = usarComoPrincipal;
        var taskbar = new FakeTaskbar();
        var hook = new FakeWinKeyHook();

        var vm = new MainViewModel(
            repo,
            new FakeLauncher(),
            new FakeIconExtraction(),
            new FakeAutostart(),
            taskbarService: taskbar,
            windowTrackingService: new FakeWindowTracking(),
            winKeyHookService: hook);

        return (vm, taskbar, hook);
    }

    [Fact]
    public void UsarComoBarraPrincipal_QuandoAtivado_IniciaHookETaskbarOculta()
    {
        var (vm, taskbar, hook) = CriarSut(usarComoPrincipal: false);

        Assert.False(vm.UsarComoBarraPrincipal);
        Assert.False(taskbar.Oculto);
        Assert.False(hook.EstaAtivo);

        vm.UsarComoBarraPrincipal = true;

        Assert.True(vm.UsarComoBarraPrincipal);
        Assert.True(taskbar.Oculto);
        Assert.True(hook.EstaAtivo);
    }

    [Fact]
    public void UsarComoBarraPrincipal_QuandoDesativado_RestauraTaskbarEParaHook()
    {
        var (vm, taskbar, hook) = CriarSut(usarComoPrincipal: false);

        // Ativa UsarComoBarraPrincipal
        vm.UsarComoBarraPrincipal = true;
        Assert.True(taskbar.Oculto);
        Assert.True(hook.EstaAtivo);

        // Desativa
        vm.UsarComoBarraPrincipal = false;
        Assert.False(taskbar.Oculto);
        Assert.False(hook.EstaAtivo);
    }

    [Fact]
    public void RestaurarBarraWindows_RestauraTaskbarEDesativaModoPrincipal()
    {
        var (vm, taskbar, hook) = CriarSut(usarComoPrincipal: false);
        vm.UsarComoBarraPrincipal = true;
        Assert.True(taskbar.Oculto);
        Assert.True(hook.EstaAtivo);

        vm.RestaurarBarraWindows();

        Assert.False(vm.UsarComoBarraPrincipal);
        Assert.False(taskbar.Oculto);
        Assert.False(hook.EstaAtivo);
    }

    [Fact]
    public void AbrirMenuIniciar_AlternaMenuIniciarAbertoEFechaOutrosPaineis()
    {
        var (vm, _, _) = CriarSut();
        vm.Clock.CalendarioAberto = true;

        Assert.False(vm.MenuIniciarAberto);
        Assert.True(vm.Clock.CalendarioAberto);

        vm.AbrirMenuIniciar();

        Assert.True(vm.MenuIniciarAberto);
        Assert.False(vm.Clock.CalendarioAberto);

        vm.AbrirMenuIniciar();
        Assert.False(vm.MenuIniciarAberto);
    }

    [Fact]
    public void ItensLaunchpadFiltrados_SemFiltro_RetornaAppsEItensDoAmbiente()
    {
        var (vm, _, _) = CriarSut();

        // Adiciona um app
        vm.Aplicativos.Add(new AppItemViewModel(
            new ItemFixado { Id = "app1", Titulo = "Google Chrome", CaminhoOuUrl = "chrome.exe" },
            new FakeWindowTracking(),
            new FakeIconExtraction(),
            _ => { },
            _ => { }));

        var itens = vm.ItensLaunchpadFiltrados.ToList();

        Assert.Contains(itens, i => i.Titulo == "Google Chrome");
    }

    [Fact]
    public void ItensLaunchpadFiltrados_ComFiltro_FiltraPorNome()
    {
        var (vm, _, _) = CriarSut();

        vm.Aplicativos.Add(new AppItemViewModel(
            new ItemFixado { Id = "app1", Titulo = "Google Chrome", CaminhoOuUrl = "chrome.exe" },
            new FakeWindowTracking(),
            new FakeIconExtraction(),
            _ => { },
            _ => { }));

        vm.Aplicativos.Add(new AppItemViewModel(
            new ItemFixado { Id = "app2", Titulo = "Visual Studio Code", CaminhoOuUrl = "code.exe" },
            new FakeWindowTracking(),
            new FakeIconExtraction(),
            _ => { },
            _ => { }));

        vm.TextoFiltroLaunchpad = "Chrome";
        var itens = vm.ItensLaunchpadFiltrados.ToList();

        Assert.Single(itens);
        Assert.Equal("Google Chrome", itens[0].Titulo);

        vm.TextoFiltroLaunchpad = "inexistente";
        Assert.Empty(vm.ItensLaunchpadFiltrados);
    }

    [Fact]
    public void IconExtractionService_ResolverCaminhoCompleto_ExpandeVariaveis()
    {
        var resultado = IconExtractionService.ResolverCaminhoCompleto("%WINDIR%\\explorer.exe");
        Assert.True(File.Exists(resultado));
        Assert.EndsWith("explorer.exe", resultado, StringComparison.OrdinalIgnoreCase);
    }
}

