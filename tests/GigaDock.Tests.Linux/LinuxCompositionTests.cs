using DockWindows.Core.Models;
using DockWindows.Core.Services;
using GigaDock.Infrastructure.Linux;
using GigaDock.Services.Persistence;

namespace GigaDock.Tests.Linux;

public sealed class LinuxCompositionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GigaDockLinux_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void PrimeiraExecucao_CriaTresAmbientesSemAtalhosWindowsOuIntegracoesAtivas()
    {
        var dirs = LinuxAppDirectories.Resolve(_root);
        Assert.False(Directory.Exists(_root));
        using var session = new LinuxApplicationSession(dirs);
        Assert.Equal(new[] { "Trabalho", "Estudos", "Pessoal" }, session.Preferences.Ambientes.Select(a => a.Nome));
        Assert.Equal("Trabalho", session.ActiveEnvironment.Nome);
        Assert.Empty(session.Preferences.AppsPermanentes);
        Assert.Empty(session.Preferences.ColecoesGlobais);
        Assert.All(session.Preferences.Ambientes, environment =>
        {
            Assert.Empty(environment.Itens);
            Assert.Empty(environment.Colecoes);
            Assert.Empty(environment.WidgetsInstalados);
            Assert.Empty(environment.ControlesRapidos);
        });
        Assert.False(session.Preferences.IniciarComWindows);
        Assert.False(session.Preferences.UsarComoBarraPrincipal);
        Assert.True(File.Exists(Path.Combine(_root, "config", "settings.json")));
        Assert.False(Directory.Exists(dirs.Cache));
        Assert.False(Directory.Exists(dirs.Dados));
        Assert.False(Directory.Exists(dirs.Estado));
        var json = File.ReadAllText(session.Settings.ObterCaminhoConfiguracoes());
        Assert.DoesNotContain(".exe", json);
        Assert.DoesNotContain("shell:", json);
    }

    [Fact]
    public void TrocarAmbiente_PersisteNaReabertura()
    {
        var dirs = LinuxAppDirectories.Resolve(_root);
        using (var first = new LinuxApplicationSession(dirs)) first.SelectEnvironment("ambiente-estudos");
        using var reopened = new LinuxApplicationSession(dirs);
        Assert.Equal("Estudos", reopened.ActiveEnvironment.Nome);
        Assert.True(File.Exists(Path.Combine(dirs.Configuracoes, "settings.json.bak")));
    }

    [Fact]
    public void Aparencia_PersisteSemAlterarAmbienteOuItens()
    {
        var dirs = LinuxAppDirectories.Resolve(_root);
        using (var session = new LinuxApplicationSession(dirs))
            session.SetAppearance(EstiloTema.Colorido, 80, .75, 24, true);
        using var reopened = new LinuxApplicationSession(dirs);
        Assert.Equal(EstiloTema.Colorido, reopened.Preferences.EstiloTema);
        Assert.Equal(80, reopened.Preferences.AlturaBarra);
        Assert.Equal(.75, reopened.Preferences.OpacidadeDock);
        Assert.Equal(24, reopened.Preferences.RaioCantosDock);
        Assert.True(reopened.Preferences.DesativarAnimacoes);
        Assert.Equal("Trabalho", reopened.ActiveEnvironment.Nome);
        Assert.Empty(reopened.Preferences.AppsPermanentes);
    }

    [Theory]
    [InlineData(double.NaN, .9, 20)]
    [InlineData(47, .9, 20)]
    [InlineData(64, double.PositiveInfinity, 20)]
    [InlineData(64, .2, 20)]
    [InlineData(64, .9, 41)]
    public void AparenciaInvalida_NaoAlteraArquivo(double height, double opacity, double radius)
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(_root));
        var original = File.ReadAllText(session.Settings.ObterCaminhoConfiguracoes());
        Assert.Throws<ArgumentException>(() => session.SetAppearance(EstiloTema.Escuro, height, opacity, radius, false));
        Assert.Equal(original, File.ReadAllText(session.Settings.ObterCaminhoConfiguracoes()));
    }

    [Fact]
    public void FalhaAoSalvarAparencia_RestauraTodosOsCampos()
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(_root), new ReadOnlySettings());
        var original = session.Preferences.Clonar();
        Assert.Throws<IOException>(() => session.SetAppearance(EstiloTema.Colorido, 90, .5, 4, true));
        Assert.Equal(original.EstiloTema, session.Preferences.EstiloTema);
        Assert.Equal(original.AlturaBarra, session.Preferences.AlturaBarra);
        Assert.Equal(original.OpacidadeDock, session.Preferences.OpacidadeDock);
        Assert.Equal(original.RaioCantosDock, session.Preferences.RaioCantosDock);
        Assert.Equal(original.DesativarAnimacoes, session.Preferences.DesativarAnimacoes);
    }

    [Fact]
    public void TrocaInvalida_NaoAlteraMemoriaNemArquivo()
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(_root));
        var original = File.ReadAllText(session.Settings.ObterCaminhoConfiguracoes());
        Assert.Throws<ArgumentException>(() => session.SelectEnvironment("inexistente"));
        Assert.Equal("Trabalho", session.ActiveEnvironment.Nome);
        Assert.Equal(original, File.ReadAllText(session.Settings.ObterCaminhoConfiguracoes()));
    }

    [Fact]
    public void FalhaAoSalvar_RestauraSelecaoAnterior()
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(_root), new ReadOnlySettings());
        Assert.Throws<IOException>(() => session.SelectEnvironment("ambiente-pessoal"));
        Assert.Equal("Trabalho", session.ActiveEnvironment.Nome);
    }

    [Fact]
    public void ArquivoCorrompido_RecuperaAmbienteDoBackup()
    {
        var dirs = LinuxAppDirectories.Resolve(_root);
        using (var first = new LinuxApplicationSession(dirs))
        {
            first.SelectEnvironment("ambiente-estudos");
            first.SelectEnvironment("ambiente-pessoal");
            File.WriteAllText(first.Settings.ObterCaminhoConfiguracoes(), "{truncado");
        }
        using var reopened = new LinuxApplicationSession(dirs);
        Assert.Equal("Estudos", reopened.ActiveEnvironment.Nome);
    }

    [Fact]
    public void ConfiguracaoWindowsExistente_PreservaItensSemAnunciarCapacidadeDeExecutar()
    {
        var dirs = LinuxAppDirectories.Resolve(_root);
        var repo = new JsonSettingsRepository(dirs.Configuracoes);
        var prefs = Preferencias.CriarPadrao();
        repo.Salvar(prefs);
        using var session = new LinuxApplicationSession(dirs);
        Assert.Contains(session.ActiveEnvironment.Itens, item => item.CaminhoOuUrl == "notepad.exe");
        Assert.False(session.Windows.Capacidades.ListarJanelas);
        Assert.False(session.Windows.Capacidades.AtivarJanela);
        Assert.Contains("não implementado", session.Describe());
        // Nenhum launcher é construído pelo composition root nesta etapa.
    }

    [Theory]
    [InlineData("relativo")]
    [InlineData("")]
    public void DiretorioDeTeste_NaoAceitaCaminhoRelativo(string path) =>
        Assert.Throws<ArgumentException>(() => LinuxAppDirectories.Resolve(path));

    [Fact]
    public void FallbackDeJanelas_NuncaSimulaOperacaoBemSucedida()
    {
        using var windows = new UnsupportedDesktopWindowService();
        var id = new DesktopWindowId("wayland", "qualquer");
        Assert.Empty(windows.ObterJanelasAbertas());
        Assert.False(windows.Ativar(id));
        Assert.False(windows.Minimizar(id));
        Assert.False(windows.Fechar(id));
    }

    [Fact]
    public void LinuxSemOverride_RespeitaXdgDoProcesso()
    {
        if (!OperatingSystem.IsLinux()) return;
        var dirs = LinuxAppDirectories.Resolve();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configured = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var expected = !string.IsNullOrWhiteSpace(configured) && Path.IsPathFullyQualified(configured)
            ? configured : Path.Combine(home, ".config");
        Assert.Equal(Path.Combine(expected, "gigadock"), dirs.Configuracoes);
    }

    [Fact]
    public void EnvironmentEdits_DoNotCommitWhenStorageFails()
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(_root), new ReadOnlySettings(), new UnsupportedDesktopWindowService());
        var before = session.Preferences.Clonar();
        Assert.Throws<IOException>(() => session.DuplicateEnvironment(session.ActiveEnvironment.Id, "Cópia"));
        Assert.Throws<IOException>(() => session.MoveEnvironment(session.ActiveEnvironment.Id, 1));
        Assert.Throws<IOException>(() => session.CycleEnvironment(-1));
        Assert.Equal(before.Ambientes.Select(a => a.Id), session.Preferences.Ambientes.Select(a => a.Id));
        Assert.Equal(before.AmbienteAtivoId, session.Preferences.AmbienteAtivoId);
    }

    [Fact]
    public void EnvironmentOrderAndCycle_SurviveReload()
    {
        var dirs = LinuxAppDirectories.Resolve(_root);
        using var session = new LinuxApplicationSession(dirs, windows: new UnsupportedDesktopWindowService());
        var active = session.ActiveEnvironment.Id;
        session.MoveEnvironment(active, int.MaxValue);
        session.CycleEnvironment(1);
        Assert.Equal(session.Preferences.Ambientes[0].Id, session.ActiveEnvironment.Id);
        session.CycleEnvironment(-1); Assert.Equal(active, session.ActiveEnvironment.Id);
        session.DuplicateEnvironment(active, "Cópia");
        using var reopened = new LinuxApplicationSession(dirs, windows: new UnsupportedDesktopWindowService());
        Assert.Equal(session.Preferences.Ambientes.Select(a => a.Id), reopened.Preferences.Ambientes.Select(a => a.Id));
        Assert.Equal(active, reopened.ActiveEnvironment.Id);
    }

    private sealed class ReadOnlySettings : ISettingsRepository
    {
        public Preferencias Carregar() => LinuxPreferencesFactory.CriarPadrao();
        public void Salvar(Preferencias prefs) => throw new IOException("Diretório sem permissão de escrita.");
        public Task SalvarAsync(Preferencias prefs) => Task.FromException(new IOException("Sem permissão."));
        public string ObterCaminhoConfiguracoes() => "configuração de teste somente leitura";
    }

    [Fact]
    public void LogLocal_NaoArmazenaMensagemComCredencialOuEntradaDoUsuario()
    {
        var path = LocalFailureLog.TryWrite(LinuxAppDirectories.Resolve(_root), new IOException("senha=segredo-de-teste"));
        Assert.NotNull(path);
        var content = File.ReadAllText(path);
        Assert.Contains("System.IO.IOException", content);
        Assert.DoesNotContain("segredo-de-teste", content);
        Assert.DoesNotContain("senha=", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
