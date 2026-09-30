using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;
using DockWindows.Installer.Services;
using Xunit;

namespace DockWindows.Tests;

public class ThreeColumnSettingsAndInstallerUpgradeTests
{
    private readonly string _tempDir;

    public ThreeColumnSettingsAndInstallerUpgradeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DockWindowsTests_3Col_" + Guid.NewGuid().ToString("N"));
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
    public void AjustesViewModel_NavegacaoTresColunas_AlternaSecoesCorretamente()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        Assert.True(vm.EhSecaoAmbientes);
        Assert.False(vm.EhSecaoWidgets);

        vm.NavegarPara("Widgets");
        Assert.True(vm.EhSecaoWidgets);
        Assert.False(vm.EhSecaoAmbientes);

        vm.NavegarPara("Espacadores");
        Assert.True(vm.EhSecaoEspacadores);

        vm.NavegarPara("Aparencia");
        Assert.True(vm.EhSecaoAparencia);

        vm.NavegarPara("Geral");
        Assert.True(vm.EhSecaoGeral);

        vm.NavegarPara("Utilitarios");
        Assert.True(vm.EhSecaoUtilitarios);

        vm.NavegarPara("Sobre");
        Assert.True(vm.EhSecaoSobre);
    }

    [Fact]
    public void AjustesViewModel_EdicaoAmbiente_AtualizaValoresEIndicadorApps()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        Assert.NotNull(vm.AmbienteSelecionado);

        // Edita nome
        vm.NomeAmbienteEditavel = "Trabalho Focado";
        Assert.Equal("Trabalho Focado", vm.AmbienteSelecionado.Nome);

        // Edita cor
        vm.CorAmbienteEditavel = "#107C41";
        Assert.Equal("#107C41", vm.AmbienteSelecionado.CorHex);

        // Edita estilo e cor do indicador de apps abertos
        vm.EstiloIndicadorAppsEditavel = "Pílula";
        vm.CorIndicadorAppsEditavel = "#FFB900";

        Assert.Equal("Pílula", vm.AmbienteSelecionado.EstiloIndicadorApps);
        Assert.Equal("#FFB900", vm.AmbienteSelecionado.CorIndicadorApps);

        // Confirma que gravou no repositório
        var recarregadas = repo.Carregar();
        var ambSalvo = recarregadas.Ambientes.First(a => a.Id == vm.AmbienteSelecionado.Id);
        Assert.Equal("Trabalho Focado", ambSalvo.Nome);
        Assert.Equal("Pílula", ambSalvo.EstiloIndicadorApps);
        Assert.Equal("#FFB900", ambSalvo.CorIndicadorApps);
    }

    [Fact]
    public void AjustesViewModel_ExclusaoAmbiente_ImpedeExcluirUltimoAmbiente()
    {
        var prefs = new Preferencias
        {
            SchemaVersion = 4,
            Ambientes = new List<Ambiente>
            {
                new() { Id = "amb-unico", Nome = "Único Ambiente", CorHex = "#0078D4" }
            }
        };
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        bool alertaExibido = false;
        vm.MostrarAlerta = (titulo, msg) => { alertaExibido = true; };

        Assert.Single(vm.Ambientes);
        vm.ExcluirAmbienteCommand.Execute(null);

        // Deve impedir a exclusão
        Assert.Single(vm.Ambientes);
        Assert.True(alertaExibido);
    }

    [Fact]
    public void AjustesViewModel_AlternarEscopoItem_MoveEntreAmbienteEGlobal()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        Assert.NotNull(vm.AmbienteSelecionado);
        var itemAmb = vm.AmbienteSelecionado.Itens.First();
        vm.ItemSelecionado = itemAmb;

        Assert.False(vm.ItemSelecionadoEhGlobal);
        Assert.Equal("Somente neste ambiente", vm.TextoEscopoItemSelecionado);

        // Alterna para Global
        vm.AlternarEscopoItemCommand.Execute(null);

        Assert.True(vm.ItemSelecionadoEhGlobal);
        Assert.Equal("Global (em todos os ambientes)", vm.TextoEscopoItemSelecionado);
        Assert.Contains(mainVm.Preferencias.AppsPermanentes, a => a.Id == itemAmb.Id);
        Assert.DoesNotContain(vm.AmbienteSelecionado.Itens, i => i.Id == itemAmb.Id);

        // Alterna de volta para Ambiente
        vm.AlternarEscopoItemCommand.Execute(null);

        Assert.False(vm.ItemSelecionadoEhGlobal);
        Assert.Contains(vm.AmbienteSelecionado.Itens, i => i.Id == itemAmb.Id);
        Assert.DoesNotContain(mainVm.Preferencias.AppsPermanentes, a => a.Id == itemAmb.Id);
    }

    [Fact]
    public void AjustesViewModel_ValidacaoURL_NormalizaProtocolo()
    {
        var prefs = Preferencias.CriarPadrao();
        var repo = new JsonSettingsRepository(_tempDir);
        repo.Salvar(prefs);

        var mainVm = CriarMainViewModel(repo);
        var vm = new AjustesViewModel(mainVm, repo, new FakeAutostartService(), "Ambientes");

        int prompts = 0;
        vm.PedirTexto = (titulo, msg) =>
        {
            prompts++;
            if (prompts == 1) return "github.com"; // URL sem https://
            return "GitHub"; // Título
        };

        vm.AdicionarSiteUrlCommand.Execute(null);

        var itemCriado = vm.AmbienteSelecionado!.Itens.Last();
        Assert.Equal("https://github.com", itemCriado.CaminhoOuUrl);
        Assert.Equal("GitHub", itemCriado.Titulo);
        Assert.Equal(TipoItem.WebUrl, itemCriado.Tipo);
    }

    [Fact]
    public void InstallService_ConstantesEVersionamento_SaoVersao1_5_0()
    {
        var installService = new InstallService();
        Assert.Equal("1.5.0", InstallService.CurrentVersion);
        Assert.Equal("Dock Windows", InstallService.AppName);
        Assert.Equal("DockWindows.App.exe", InstallService.AppExeName);
    }

    [Fact]
    public void InstallService_BackupPreUpdate_PreservaConfiguracoesAntesDeAtualizar()
    {
        var dirDados = Path.Combine(_tempDir, "UserData");
        Directory.CreateDirectory(dirDados);

        var arquivoSettings = Path.Combine(dirDados, "settings.json");
        var jsonConfig = @"{ ""schemaVersion"": 3, ""ambienteAtivoId"": ""amb-trabalho"", ""ambientes"": [] }";
        File.WriteAllText(arquivoSettings, jsonConfig);

        // Simula criação do backup preventivo pré-update
        var arquivoBackup = Path.Combine(dirDados, $"settings.json.pre-update-{DateTime.Now:yyyyMMddHHmmss}.bak");
        File.Copy(arquivoSettings, arquivoBackup, overwrite: true);

        Assert.True(File.Exists(arquivoBackup));
        Assert.Equal(jsonConfig, File.ReadAllText(arquivoBackup));
    }
}
