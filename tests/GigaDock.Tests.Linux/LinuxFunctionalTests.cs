using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.Tests.Linux;

public sealed class LinuxFunctionalTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GigaDock-functional-" + Guid.NewGuid().ToString("N"));
    private string FileAt(string name, string text)
    {
        var path = Path.Combine(_root, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, text); return path;
    }

    [Fact]
    public void Catalog_UserHiddenOverridesSystemAndFiltersCurrentDesktop()
    {
        FileAt("user/editor.desktop", "[Desktop Entry]\nType=Application\nHidden=true\nName=Oculto\nExec=editor\n");
        FileAt("system/editor.desktop", "[Desktop Entry]\nType=Application\nName=Editor\nExec=editor\n");
        FileAt("system/settings.desktop", "[Desktop Entry]\nType=Application\nName=Configurações\nExec=settings\nOnlyShowIn=GNOME;\n");
        FileAt("system/media.desktop", "[Desktop Entry]\nType=Application\nName=Música\nExec=media\nNotShowIn=KDE;\n");
        var catalog = new DesktopCatalog([Path.Combine(_root, "user"), Path.Combine(_root, "system")], "KDE");
        Assert.Empty(catalog.Scan());
    }

    [Theory]
    [InlineData("NoDisplay=true")]
    [InlineData("Hidden=true")]
    [InlineData("TryExec=absent")]
    public void Catalog_HidesUnavailableApplications(string extra)
    {
        var file = FileAt("app.desktop", "[Desktop Entry]\nType=Application\nName=App\nExec=app\n" + extra);
        Assert.Null(new DesktopCatalog([], executable: _ => false).Read(file));
    }

    [Fact]
    public void Catalog_AcceptsDbusAndTerminalButDoesNotExecuteExec()
    {
        var file = FileAt("app.desktop", "[Desktop Entry]\nType=Application\nName=App\nDBusActivatable=true\nTerminal=true\nIcon=media-player\n");
        var app = Assert.IsType<DesktopApplication>(new DesktopCatalog([]).Read(file));
        Assert.True(app.DBusActivatable); Assert.True(app.Terminal); Assert.Equal("media-player", app.Icon);
    }

    [Theory]
    [InlineData("https://user:password@example.com", TipoItem.WebUrl)]
    [InlineData("file:///etc/passwd", TipoItem.WebUrl)]
    [InlineData("relative-path", TipoItem.Arquivo)]
    [InlineData("/tmp/windows.exe", TipoItem.Aplicativo)]
    [InlineData("/tmp/broken\nname", TipoItem.Arquivo)]
    public void Launcher_RejectsUnsafeOrForeignTargets(string target, TipoItem type) =>
        Assert.False(LinuxLauncher.Validate(new ItemFixado { Titulo = "Teste", CaminhoOuUrl = target, Tipo = type }).Valido);

    [Fact]
    public void Launcher_RejectsArgumentsAndAllowsFilesWithSpaces()
    {
        var file = FileAt("relatório pessoal.txt", "conteúdo");
        var item = new ItemFixado { Titulo = "Relatório", CaminhoOuUrl = file, Tipo = TipoItem.Arquivo };
        Assert.True(LinuxLauncher.Validate(item).Valido);
        item.Argumentos = "--unsafe"; Assert.False(LinuxLauncher.Validate(item).Valido);
    }

    [Fact]
    public void ItemsAndCollections_PersistPerEnvironmentAndReorder()
    {
        var dirs = LinuxAppDirectories.Resolve(Path.GetFullPath(_root));
        using (var session = new LinuxApplicationSession(dirs))
        {
            session.AddItem(new ItemFixado { Titulo = "A", Tipo = TipoItem.WebUrl, CaminhoOuUrl = "https://example.com/a" });
            session.AddItem(new ItemFixado { Titulo = "B", Tipo = TipoItem.WebUrl, CaminhoOuUrl = "https://example.com/b" });
            session.MoveItem(session.ActiveEnvironment.Itens[1].Id, -1);
            session.Update(prefs => prefs.Ambientes[0].Colecoes.Add(new ColecaoApp { Nome = "Pesquisa", EhGlobal = false, Itens = [] }));
            session.AddItem(new ItemFixado { Titulo = "C", Tipo = TipoItem.WebUrl, CaminhoOuUrl = "https://example.com/c" }, session.ActiveEnvironment.Colecoes[0].Id);
            session.SelectEnvironment("ambiente-estudos"); Assert.Empty(session.ActiveEnvironment.Itens);
        }
        using var reopened = new LinuxApplicationSession(dirs); reopened.SelectEnvironment("ambiente-trabalho");
        Assert.Equal(new[] { "B", "A" }, reopened.ActiveEnvironment.Itens.Select(i => i.Titulo));
        Assert.Equal("C", reopened.ActiveEnvironment.Colecoes[0].Itens[0].Titulo);
    }

    [Fact]
    public void EditFailure_DoesNotCommitOrChangeOriginalModel()
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(Path.GetFullPath(_root)));
        var original = File.ReadAllText(session.Settings.ObterCaminhoConfiguracoes());
        Assert.Throws<InvalidOperationException>(() => session.Update(prefs => { prefs.Ambientes.Clear(); throw new InvalidOperationException(); }));
        Assert.Equal(3, session.Preferences.Ambientes.Count);
        Assert.Equal(original, File.ReadAllText(session.Settings.ObterCaminhoConfiguracoes()));
    }

    [Fact]
    public void ImportedGlobalItems_CanBeRemovedAndCollectionsReassigned()
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(Path.GetFullPath(_root)));
        session.Update(prefs =>
        {
            prefs.AppsPermanentes.Add(new ItemFixado { Id = "old-app", Titulo = "Windows", CaminhoOuUrl = "app.exe" });
            prefs.ColecoesGlobais.Add(new ColecaoApp { Id = "shared", Nome = "Compartilhada", Itens = [] });
        });
        session.RemoveItem("old-app"); Assert.Empty(session.Preferences.AppsPermanentes);
        session.AddItem(new ItemFixado { Titulo = "Site", Tipo = TipoItem.WebUrl, CaminhoOuUrl = "https://example.com" }, "shared");
        var item = Assert.Single(session.Preferences.ColecoesGlobais[0].Itens);
        session.RemoveItem(item.Id, "shared"); Assert.Empty(session.Preferences.ColecoesGlobais[0].Itens);
    }

    [Fact]
    public void Widgets_EnableDisablePreservesConfiguration()
    {
        using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(Path.GetFullPath(_root)));
        session.SetWidget(TipoWidget.MascotePokemon, true);
        session.Update(prefs => prefs.Ambientes[0].WidgetsInstalados[0].DefinirConfiguracao("PokemonId", "25"));
        session.SetWidget(TipoWidget.MascotePokemon, false); session.SetWidget(TipoWidget.MascotePokemon, true);
        Assert.Single(session.ActiveEnvironment.WidgetsInstalados);
        Assert.Equal("25", session.ActiveEnvironment.WidgetsInstalados[0].ObterConfiguracao("PokemonId"));
        Assert.Throws<ArgumentException>(() => session.SetWidget(TipoWidget.TeamsStatus, true));
    }

    [Fact]
    public void Notes_IsolateEnvironmentsAndCannotEscapeDataDirectory()
    {
        var notes = new LocalNotes(Path.GetFullPath(_root));
        notes.Save("../../elsewhere", "Ideias\nAcentuação"); notes.Save("work", "Trabalho");
        Assert.Equal("Ideias\nAcentuação", notes.Read("../../elsewhere")); Assert.Equal("Trabalho", notes.Read("work"));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(_root, "notes")).Length);
        Assert.Throws<ArgumentException>(() => notes.Save("work", new string('a', 1024 * 1024 + 1)));
    }

    [Fact]
    public void SystemSnapshot_UsesCpuDeltasAndMemAvailable()
    {
        FileAt("proc/stat", "cpu 10 0 10 80 0 0 0 0\n"); FileAt("proc/meminfo", "MemTotal: 1000 kB\nMemAvailable: 600 kB\n");
        FileAt("power/BAT0/type", "Battery"); FileAt("power/BAT0/capacity", "75"); FileAt("power/BAT0/status", "Charging");
        var service = new LinuxSystemServices(); Assert.Null(service.Read(Path.Combine(_root, "proc"), Path.Combine(_root, "power")).CpuPercent);
        FileAt("proc/stat", "cpu 20 0 20 160 0 0 0 0\n");
        var sample = service.Read(Path.Combine(_root, "proc"), Path.Combine(_root, "power"));
        Assert.Equal(20, sample.CpuPercent); Assert.Equal(40, sample.MemoryPercent); Assert.Equal("75% — carregando", sample.Battery);
    }

    [Fact]
    public async Task MediaAndVolume_UseSeparatedArgumentsAndRejectInvalidCommands()
    {
        var commands = new RecordingCommands(); var service = new LinuxSystemServices(commands);
        await service.MediaCommandAsync("play-pause"); Assert.Equal(new[] { "playerctl", "play-pause" }, commands.Last);
        await service.SetVolumeAsync(35); Assert.Equal(new[] { "pactl", "set-sink-volume", "@DEFAULT_SINK@", "35%" }, commands.Last);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetVolumeAsync(101));
        await Assert.ThrowsAsync<ArgumentException>(() => service.MediaCommandAsync("next; touch /tmp/injected"));
    }

    [Fact]
    public void SingleInstance_ReleasesLockAfterDispose()
    {
        using (var first = SingleInstance.TryAcquire(Path.GetFullPath(_root)))
        { Assert.NotNull(first); Assert.Null(SingleInstance.TryAcquire(Path.GetFullPath(_root))); }
        using var second = SingleInstance.TryAcquire(Path.GetFullPath(_root)); Assert.NotNull(second);
    }

    private sealed class RecordingCommands : ILinuxCommands
    {
        public string[] Last = [];
        public Task<CommandResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellation = default)
        { Last = new[] { executable }.Concat(arguments).ToArray(); return Task.FromResult(new CommandResult(0, "", "")); }
    }

    [Fact]
    public void ReorderBefore_RejectsForeignItemsAndPersistsStableOrder()
    {
        var dirs = LinuxAppDirectories.Resolve(_root);
        using var session = new LinuxApplicationSession(dirs, windows: new UnsupportedDesktopWindowService());
        foreach (var title in new[] { "A", "B", "C" }) session.AddItem(new ItemFixado { Titulo = title, Tipo = TipoItem.WebUrl, CaminhoOuUrl = "https://example.com" });
        var ids = session.ActiveEnvironment.Itens.Select(item => item.Id).ToArray();
        Assert.Throws<ArgumentException>(() => session.MoveItemBefore(ids[2], "outro-ambiente"));
        Assert.Equal(new[] { "A", "B", "C" }, session.ActiveEnvironment.Itens.Select(item => item.Titulo));
        session.MoveItemBefore(ids[2], ids[0]);
        using var reopened = new LinuxApplicationSession(dirs, windows: new UnsupportedDesktopWindowService());
        Assert.Equal(new[] { "C", "A", "B" }, reopened.ActiveEnvironment.Itens.OrderBy(item => item.Ordem).Select(item => item.Titulo));
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
