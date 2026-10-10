using DockWindows.Core.Models;
using GigaDock.Infrastructure.Linux;
using Xunit;

namespace GigaDock.Tests.Linux;

public class WidgetStoreAvailabilityTests
{
    [Theory]
    [InlineData(TipoWidget.Midia, "playerctl")]
    [InlineData(TipoWidget.AudioSistema, "pactl")]
    [InlineData(TipoWidget.AplicativosFlatpak, "flatpak")]
    public void DependenciesBlockInstallUntilPresent(TipoWidget kind, string command)
    {
        var absent = LinuxWidgetAvailability.Get(kind, _ => false);
        Assert.False(absent.CanInstall);
        Assert.Equal("Requisito ausente", absent.Status);
        Assert.Contains(command, absent.Reason);
        Assert.True(LinuxWidgetAvailability.Get(kind, candidate => candidate == command).CanInstall);
    }

    [Fact]
    public void WorkspaceRequiresSupportedSessionAndItsCommand()
    {
        Assert.False(LinuxWidgetAvailability.Get(TipoWidget.WorkspacesLinux, _ => true, LinuxCompositor.Unsupported).CanInstall);
        Assert.False(LinuxWidgetAvailability.Get(TipoWidget.WorkspacesLinux, _ => false, LinuxCompositor.Sway).CanInstall);
        Assert.True(LinuxWidgetAvailability.Get(TipoWidget.WorkspacesLinux, cmd => cmd == "swaymsg", LinuxCompositor.Sway).CanInstall);
        Assert.True(LinuxWidgetAvailability.Get(TipoWidget.WorkspacesLinux, cmd => cmd == "hyprctl", LinuxCompositor.Hyprland).CanInstall);
    }

    [Fact]
    public void HidingLegacyIncompleteWidgetPreservesItsConfiguration()
    {
        var path = Path.Combine(Path.GetTempPath(), "gigadock-store-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var session = new LinuxApplicationSession(LinuxAppDirectories.Resolve(Path.GetFullPath(path)));
            session.Update(prefs => prefs.Ambientes[0].WidgetsInstalados.Add(new() { Tipo = TipoWidget.LembreteAgua, Visivel = true }));
            var count = session.ActiveEnvironment.WidgetsInstalados.Count;
            session.SetWidget(TipoWidget.LembreteAgua, false);
            Assert.False(session.ActiveEnvironment.WidgetsInstalados.Single(w => w.Tipo == TipoWidget.LembreteAgua).Visivel);
            Assert.Throws<ArgumentException>(() => session.SetWidget(TipoWidget.LembreteAgua, true));
            Assert.Equal(count, session.ActiveEnvironment.WidgetsInstalados.Count);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
}
