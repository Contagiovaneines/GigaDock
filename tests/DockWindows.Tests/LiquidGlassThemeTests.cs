using System;
using System.IO;
using System.Linq;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;
using Xunit;

namespace DockWindows.Tests;

public class LiquidGlassThemeTests
{
    private readonly string _tempDir;

    public LiquidGlassThemeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DockWindowsTests_LiquidGlass_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private class FakeLauncherService : ILauncherService
    {
        public LaunchResult Executar(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult AbrirLocal(ItemFixado item) => LaunchResult.Ok();
        public LaunchResult ExecutarCaminho(string caminho, string? argumentos = null) => LaunchResult.Ok();
    }

    private class FakeIconExtractionService : IIconExtractionService
    {
        public System.Windows.Media.ImageSource? ObterIcone(ItemFixado item) => null;
        public System.Windows.Media.ImageSource? ObterIcone(string caminhoOuUrl, TipoItem tipo = TipoItem.Aplicativo) => null;
        public System.Windows.Media.ImageSource? ObterIconeJanela(IntPtr hWnd) => null;
    }

    private class FakeAutostartService : IAutostartService
    {
        public bool EstaHabilitado() => false;
        public bool Configurar(bool habilitar) => true;
    }

    private class FakeWindowTrackingService : IWindowTrackingService
    {
#pragma warning disable CS0067
        public event Action? JanelasAlteradas;
        public event Action<IntPtr>? JanelaAtivada;
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

    private class FakeTaskbarService : ITaskbarService
    {
        public int ObterEstadoAtual() => 0;
        public bool OcultarBarraNativa(out int estadoAnterior)
        {
            estadoAnterior = 0;
            return true;
        }
        public bool RestaurarBarraNativa(int? estadoAnterior = null) => true;
        public void GarantirBarraOculta() { }
        public void Dispose() { }
    }

    private MainViewModel CriarMainViewModel(ISettingsRepository repo)
    {
        return new MainViewModel(
            repo,
            new FakeLauncherService(),
            new FakeIconExtractionService(),
            new FakeAutostartService(),
            new FakeTaskbarService(),
            new FakeWindowTrackingService());
    }

    [Fact]
    public void TemaDefinicao_ContemTemaVidroLiquido()
    {
        var temas = TemaDefinicao.ObterTemasPredefinidos();
        var temaVidro = temas.FirstOrDefault(t => t.Estilo == EstiloTema.VidroLiquido);

        Assert.NotNull(temaVidro);
        Assert.Equal("Vidro Líquido", temaVidro.Nome);
        Assert.Contains("Liquid Glass", temaVidro.Descricao);
        Assert.Equal("#B0FFFFFF", temaVidro.BordaDockColor);
        Assert.Equal(24.0, temaVidro.RaioCantos);
        Assert.Equal(0.85, temaVidro.OpacidadePadrao);
    }

    [Fact]
    public void TemaDefinicao_ObterPorEstilo_RetornaVidroLiquido()
    {
        var tema = TemaDefinicao.ObterPorEstilo(EstiloTema.VidroLiquido);
        Assert.NotNull(tema);
        Assert.Equal(EstiloTema.VidroLiquido, tema.Estilo);
        Assert.Equal("Vidro Líquido", tema.Nome);
    }

    [Fact]
    public void MainViewModel_AlternarParaVidroLiquido_AtualizaPropriedadesEEfeito()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);

        // Altera para Vidro Líquido
        mainVm.EstiloTema = EstiloTema.VidroLiquido;

        Assert.Equal(EstiloTema.VidroLiquido, mainVm.EstiloTema);
        Assert.True(mainVm.EhVidroLiquido);
        Assert.True(mainVm.TemEfeitoVidro);
        Assert.Equal("#35FFFFFF", mainVm.HoverItemColor);
        Assert.Equal("#75FFFFFF", mainVm.SeparadorColor);
        Assert.Equal("#B0FFFFFF", mainVm.BordaDockColor);
        Assert.Equal(24.0, mainVm.RaioCantosDock);
        Assert.Equal(0.85, mainVm.OpacidadeDock);
    }

    [Fact]
    public void Persistencia_SalvaECarregaTemaVidroLiquido()
    {
        var repo = new JsonSettingsRepository(_tempDir);
        var prefs = Preferencias.CriarPadrao();
        prefs.EstiloTema = EstiloTema.VidroLiquido;
        repo.Salvar(prefs);

        var recarregadas = repo.Carregar();
        Assert.Equal(EstiloTema.VidroLiquido, recarregadas.EstiloTema);
    }

    [Fact]
    public void AjustesViewModel_SelecionarTemaVidroLiquido_AplicaPreferencias()
    {
        var repo = new JsonSettingsRepository(_tempDir);
        var prefs = Preferencias.CriarPadrao();
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Aparencia");

        var temaVidro = vm.TemasPredefinidos.First(t => t.Estilo == EstiloTema.VidroLiquido);
        vm.TemaSelecionado = temaVidro;

        Assert.Equal(EstiloTema.VidroLiquido, mainVm.EstiloTema);
        Assert.True(mainVm.EhVidroLiquido);
        Assert.True(mainVm.TemEfeitoVidro);

        // Confirma que gravou no disco
        var recarregadas = repo.Carregar();
        Assert.Equal(EstiloTema.VidroLiquido, recarregadas.EstiloTema);
    }
}
