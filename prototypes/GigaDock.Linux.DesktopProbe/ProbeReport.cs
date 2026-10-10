using System.Runtime.InteropServices;

namespace GigaDock.Linux.DesktopProbe;

internal static class ProbeReport
{
    public static string EnvironmentSummary() => $"""
        GigaDock — protótipo de desktop
        Sistema: {RuntimeInformation.OSDescription}
        Arquitetura: {RuntimeInformation.ProcessArchitecture}
        .NET: {Environment.Version}
        Avalonia: 12.1.4 (versão fixada)
        Sessão declarada: {Read("XDG_SESSION_TYPE")}
        Desktop declarado: {Read("XDG_CURRENT_DESKTOP")}
        DISPLAY: {(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) ? "ausente" : "presente")}
        WAYLAND_DISPLAY: {(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) ? "ausente" : "presente")}
        Backend configurado: UsePlatformDetect; Linux usa X11/XWayland, sem opt-in Wayland nativo.
        Área exclusiva: não implementada.
        Observação/ativação de janelas alheias: não implementada.
        Nenhum painel existente é alterado.
        """;

    private static string Read(string name) => Environment.GetEnvironmentVariable(name) ?? "não informado";
}
