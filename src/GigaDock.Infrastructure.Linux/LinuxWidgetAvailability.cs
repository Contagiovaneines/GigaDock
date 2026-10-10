using DockWindows.Core.Models;
using DockWindows.Core.Widgets;

namespace GigaDock.Infrastructure.Linux;

public sealed record LinuxWidgetAccess(WidgetStoreEntry Entry, bool CanInstall, string Reason)
{
    public string Status => !Entry.CanInstall ? Entry.Status : CanInstall ? Entry.Status : "Requisito ausente";
}

public static class LinuxWidgetAvailability
{
    // Only inspect local capabilities. Never execute a command or start a network request to draw the store.
    public static LinuxWidgetAccess Get(TipoWidget kind, Func<string, bool>? hasCommand = null, LinuxCompositor? compositor = null)
    {
        var entry = WidgetStoreCatalog.Get(kind, WidgetPlatform.Linux);
        if (!entry.CanInstall) return new(entry, false, entry.Requirements);
        hasCommand ??= command => LinuxCommands.Find(command) is not null;
        var missing = kind switch
        {
            TipoWidget.Midia when !hasCommand("playerctl") => "Instale playerctl e abra novamente os Ajustes. Também é necessário um player MPRIS.",
            TipoWidget.AudioSistema when !hasCommand("pactl") => "Instale pactl e abra novamente os Ajustes. É necessário um servidor de áudio compatível.",
            TipoWidget.AplicativosFlatpak when !hasCommand("flatpak") => "Instale flatpak e abra novamente os Ajustes para ativar este widget.",
            TipoWidget.WorkspacesLinux => WorkspaceRequirement(compositor ?? LinuxWorkspaceService.Detect(), hasCommand),
            _ => null
        };
        return new(entry, missing is null, missing ?? entry.Requirements);
    }

    private static string? WorkspaceRequirement(LinuxCompositor compositor, Func<string, bool> hasCommand) => compositor switch
    {
        LinuxCompositor.Sway when hasCommand("swaymsg") => null,
        LinuxCompositor.Hyprland when hasCommand("hyprctl") => null,
        LinuxCompositor.Sway => "A sessão Sway exige swaymsg disponível. Reabra os Ajustes após instalar.",
        LinuxCompositor.Hyprland => "A sessão Hyprland exige hyprctl disponível. Reabra os Ajustes após instalar.",
        _ => "Precisa de uma sessão Sway ou Hyprland. GNOME, KDE e esta sessão não possuem integração neste widget."
    };
}
