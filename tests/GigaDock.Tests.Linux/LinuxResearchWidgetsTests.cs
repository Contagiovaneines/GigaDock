using System.Text.Json;
using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.Tests.Linux;

public sealed class LinuxResearchWidgetsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GigaDock research " + Guid.NewGuid().ToString("N"));
    private string FileAt(string name, string content)
    {
        var path = Path.Combine(_root, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, content); return path;
    }
    private sealed class Commands(Func<string, IReadOnlyList<string>, CommandResult> response) : ILinuxCommands
    {
        public List<(string Executable, string[] Arguments)> Calls { get; } = [];
        public Task<CommandResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellation = default)
        { cancellation.ThrowIfCancellationRequested(); Calls.Add((executable, arguments.ToArray())); return Task.FromResult(response(executable, arguments)); }
    }
    private sealed class Launcher : ILinuxApplicationLauncher
    {
        public string? Executable; public string[] Arguments = [];
        public Task LaunchAsync(string executable, IReadOnlyList<string> arguments, CancellationToken token = default)
        { Executable = executable; Arguments = arguments.ToArray(); return Task.CompletedTask; }
    }

    [Fact]
    public void Hardware_ReadsMilliDegreesRpmLabelsAndIgnoresBadValues()
    {
        FileAt("hwmon0/name", "coretemp\n"); FileAt("hwmon0/temp1_input", "45500\n"); FileAt("hwmon0/temp1_label", "Package id 0\n");
        FileAt("hwmon0/fan1_input", "0\n"); FileAt("hwmon0/fan2_input", "1234\n");
        FileAt("hwmon0/temp2_input", "300000\n"); FileAt("hwmon0/temp3_input", "not a number");
        FileAt("hwmon0/in1_input", "12000\n");
        var sensors = new LinuxHardwareSensors(_root).Read();
        Assert.Equal(3, sensors.Count); Assert.Equal(45.5, sensors.Single(s => s.IsTemperature).Value);
        Assert.Equal("Package id 0", sensors.Single(s => s.IsTemperature).Label);
        Assert.Contains(sensors, s => !s.IsTemperature && s.Value == 0);
        Assert.Contains("RPM", LinuxHardwareSensors.Summary(sensors));
    }
    [Fact]
    public void Hardware_MissingRootAndOverlongReadingAreUnavailable()
    {
        Assert.Empty(new LinuxHardwareSensors(_root).Read());
        FileAt("hwmon0/temp1_input", new string('1', 600));
        Assert.Equal("Sensores indisponíveis", LinuxHardwareSensors.Summary(new LinuxHardwareSensors(_root).Read()));
    }
    [Fact]
    public void Flatpak_ParsesTabsAndRejectsMalformedIdentifiers()
    {
        var apps = LinuxFlatpakService.Parse("org.example.App\tMeu App\t1.2\tstable\tx86_64\n--command=bad\tRuim\t1\tstable\tx86_64\norg.example.Bad\tRuim\t1\tstable;exec\tx86_64\n", true);
        var app = Assert.Single(apps); Assert.Equal("Meu App", app.Name); Assert.True(app.UserInstallation);
    }
    [Fact]
    public async Task Flatpak_SeparatesScopesAndLaunchesExactBranchWithoutShell()
    {
        var commands = new Commands((_, _) => new(0, "org.example.App\tMeu App\t1.2\tstable\tx86_64\n", ""));
        var launcher = new Launcher(); var service = new LinuxFlatpakService(commands, launcher);
        var apps = await service.ListAsync(); Assert.Equal(2, apps.Count);
        Assert.Contains(commands.Calls, call => call.Arguments.Contains("--user"));
        Assert.Contains(commands.Calls, call => call.Arguments.Contains("--system"));
        await service.LaunchAsync(apps.First(app => app.UserInstallation));
        Assert.Equal("flatpak", launcher.Executable);
        Assert.Equal(new[] { "run", "--user", "--arch=x86_64", "--branch=stable", "org.example.App" }, launcher.Arguments);
        await Assert.ThrowsAsync<ArgumentException>(() => service.LaunchAsync(new("--command=sh", "bad", "", "stable", "x86_64", true)));
    }
    [Fact]
    public async Task Flatpak_QueryFailureIsReported()
    {
        var service = new LinuxFlatpakService(new Commands((_, _) => new(1, "", "private error")));
        var error = await Assert.ThrowsAsync<IOException>(() => service.ListAsync()); Assert.DoesNotContain("private", error.Message);
    }

    private const string SwayData = "[{\"id\":5,\"name\":\"1: Trabalho\",\"focused\":true,\"output\":\"DP-1\"},{\"id\":6,\"name\":\"Estudos\",\"focused\":false,\"output\":\"DP-1\"}]";
    [Fact]
    public async Task Workspaces_SwayPreservesNamedWorkspaceAndChecksCommandReply()
    {
        var commands = new Commands((_, args) => args.Contains("get_workspaces") ? new(0, SwayData, "") : new(0, "[{\"success\":true}]", ""));
        var service = new LinuxWorkspaceService(commands, LinuxCompositor.Sway);
        var list = await service.ListAsync(); Assert.True(list[0].Active);
        await service.SwitchAsync(list[1]);
        Assert.Equal("workspace --no-auto-back-and-forth \"Estudos\"", commands.Calls.Last().Arguments.Last());
    }
    [Fact]
    public async Task Workspaces_SwayRejectsCommandInjectionAndStaleTarget()
    {
        var commands = new Commands((_, _) => new(0, "[{\"id\":1,\"name\":\"x;exec bad\",\"focused\":true}]", ""));
        var service = new LinuxWorkspaceService(commands, LinuxCompositor.Sway);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SwitchAsync(new(1, "x;exec bad", true, "")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SwitchAsync(new(2, "Absent", false, "")));
        Assert.DoesNotContain(commands.Calls, call => call.Arguments.Contains("command"));
    }
    [Fact]
    public async Task Workspaces_HyprlandSwitchesByPositiveIdAndOmitsSpecialWorkspaces()
    {
        var commands = new Commands((_, args) => args.Contains("workspaces") ? new(0, "[{\"id\":2,\"name\":\"dois\",\"monitor\":\"DP-1\"},{\"id\":-99,\"name\":\"special\"}]", "")
            : args.Contains("activeworkspace") ? new(0, "{\"id\":2}", "") : new(0, "ok\n", ""));
        var service = new LinuxWorkspaceService(commands, LinuxCompositor.Hyprland);
        var workspace = Assert.Single(await service.ListAsync()); Assert.True(workspace.Active);
        await service.SwitchAsync(workspace); Assert.Equal(new[] { "dispatch", "workspace", "2" }, commands.Calls.Last().Arguments);
    }
    [Fact]
    public async Task Workspaces_UnsupportedAndMalformedRepliesAreReported()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => new LinuxWorkspaceService(compositor: LinuxCompositor.Unsupported).ListAsync());
        Assert.Throws<IOException>(() => LinuxWorkspaceService.Parse("not json", LinuxCompositor.Sway));
        Assert.Throws<IOException>(() => LinuxWorkspaceService.Parse("{}", LinuxCompositor.Sway));
        Assert.Empty(LinuxWorkspaceService.Parse("[{\"id\":\"bad\",\"name\":\"Ignore\"}]", LinuxCompositor.Sway));
    }
    [Fact]
    public async Task Workspaces_ExitZeroButRejectedCommandIsFailure()
    {
        var commands = new Commands((_, args) => args.Contains("get_workspaces") ? new(0, SwayData, "") : new(0, "[{\"success\":false}]", ""));
        var service = new LinuxWorkspaceService(commands, LinuxCompositor.Sway);
        await Assert.ThrowsAsync<IOException>(() => service.SwitchAsync(new(6, "Estudos", false, "DP-1")));
    }
    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"text\":\"\"}")]
    [InlineData("{\"text\":42}")]
    [InlineData("{\"text\":\"a\\nb\"}")]
    [InlineData("{\"text\":\"ok\",\"tooltip\":false}")]
    public void Script_RejectsMalformedContract(string output) => Assert.Throws<IOException>(() => LinuxLocalScriptWidget.Parse(output));
    [Fact]
    public void Script_AcceptsTextAndTooltipWithLimits()
    {
        Assert.Equal(new LocalScriptValue("Status", "Detalhes\nsegunda linha"), LinuxLocalScriptWidget.Parse("{\"text\":\"Status\",\"tooltip\":\"Detalhes\\nsegunda linha\"}"));
        Assert.Throws<IOException>(() => LinuxLocalScriptWidget.Parse(JsonSerializer.Serialize(new { text = new string('x', 81) })));
        Assert.Throws<IOException>(() => LinuxLocalScriptWidget.Parse(new string('x', 4097)));
        Assert.Throws<ArgumentException>(() => LinuxLocalScriptWidget.ValidatePath("relative.sh"));
        Assert.Throws<ArgumentException>(() => LinuxLocalScriptWidget.ValidatePath(Path.Combine(_root, "missing.sh")));
    }
    [LinuxFact]
    public async Task Script_ExecutesLocalPathWithSpacesOnlyWhenRequested()
    {
        if (!OperatingSystem.IsLinux()) return;
        var marker = Path.Combine(_root, "marker");
        var path = FileAt("meu script.sh", $"#!/bin/sh\nprintf started > '{marker}'\nprintf '%s' '{{\"text\":\"Local OK\",\"tooltip\":\"Manual\"}}'\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var service = new LinuxLocalScriptWidget(); LinuxLocalScriptWidget.ValidatePath(path); Assert.False(File.Exists(marker));
        var result = await service.ExecuteAsync(path); Assert.Equal("Local OK", result.Text); Assert.True(File.Exists(marker));
    }
    [LinuxFact]
    public async Task Script_EnforcesTimeout()
    {
        if (!OperatingSystem.IsLinux()) return;
        var path = FileAt("timeout.sh", "#!/bin/sh\nsleep 10\nprintf '%s' '{\"text\":\"late\"}'\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var service = new LinuxLocalScriptWidget(new LinuxCommands(TimeSpan.FromMilliseconds(150), 4096));
        await Assert.ThrowsAsync<IOException>(() => service.ExecuteAsync(path));
    }
    [LinuxFact]
    public async Task Script_EnforcesOutputLimitAndRejectsNonExecutableFile()
    {
        if (!OperatingSystem.IsLinux()) return;
        var path = FileAt("large.sh", "#!/bin/sh\nprintf '%05000d' 1\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        Assert.Throws<ArgumentException>(() => LinuxLocalScriptWidget.ValidatePath(path));
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        await Assert.ThrowsAsync<IOException>(() => new LinuxLocalScriptWidget().ExecuteAsync(path));
    }
    [LinuxFact]
    public async Task Commands_RespectsCancellationAndReportsProgramFailure()
    {
        if (!OperatingSystem.IsLinux()) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new LinuxCommands().RunAsync("sh", ["-c", "sleep 10"], cancellation.Token));
        var path = FileAt("failure.sh", "#!/bin/sh\nexit 2\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        await Assert.ThrowsAsync<IOException>(() => new LinuxLocalScriptWidget().ExecuteAsync(path));
    }
    [Fact]
    public void Catalog_NewTypesKeepExistingNumericValues()
    {
        Assert.Equal(21, (int)TipoWidget.Tarefas); Assert.Equal(20, (int)TipoWidget.MascotePokemon);
        foreach (var kind in new[] { TipoWidget.SensoresLinux, TipoWidget.AplicativosFlatpak, TipoWidget.WorkspacesLinux, TipoWidget.ScriptLocalLinux })
            Assert.Contains(kind, LinuxWidgetCatalog.Supported);
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
