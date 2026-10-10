using DockWindows.Core.Models;
namespace GigaDock.Infrastructure.Linux;

public static class LinuxWidgetCatalog
{
    public static IReadOnlyList<TipoWidget> Supported { get; } =
        DockWindows.Core.Widgets.WidgetStoreCatalog.ForPlatform(DockWindows.Core.Widgets.WidgetPlatform.Linux)
            .Where(entry => entry.CanInstall).Select(entry => entry.Kind).ToArray();
    public static string Name(TipoWidget kind) => kind switch
    {
        TipoWidget.CalendarioCompromissos => "Calendário", TipoWidget.MonitorSistema => "Atividade do sistema",
        TipoWidget.AudioSistema => "Som", TipoWidget.Midia => "Música", TipoWidget.LembreteAgua => "Beber água",
        TipoWidget.MascotePokemon => "Pokédex", TipoWidget.Relogio => "Relógio",
        TipoWidget.Clima => "Clima", TipoWidget.CotacaoMoedas => "Câmbio", TipoWidget.GitHubContribuicoes => "Perfil público do GitHub",
        TipoWidget.OBSStudio => "OBS Studio",
        TipoWidget.SensoresLinux => "Temperatura e ventoinhas",
        TipoWidget.AplicativosFlatpak => "Aplicativos Flatpak",
        TipoWidget.WorkspacesLinux => "Áreas de trabalho Linux",
        TipoWidget.ScriptLocalLinux => "Script local",
        _ => kind.ToString()
    };
}
