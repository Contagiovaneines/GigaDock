using System.Text;
namespace GigaDock.Infrastructure.Linux;

public static class LinuxAutostart
{
    public static string GetPath(string? configDirectory = null)
    {
        if (configDirectory is not null && !Path.IsPathFullyQualified(configDirectory)) throw new ArgumentException("Use diretório de configurações absoluto.");
        var config = configDirectory ?? Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!Path.IsPathFullyQualified(config ?? "")) config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(config!, "autostart", "gigadock.desktop");
    }
    public static bool Enabled => File.Exists(GetPath());
    public static void Set(bool enabled, string executable, string? configDirectory = null)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("A inicialização automática é configurada apenas no Linux.");
        var path = GetPath(configDirectory);
        if (File.Exists(path) && !File.ReadAllText(path).Contains("X-GigaDock-Managed=true", StringComparison.Ordinal))
            throw new IOException("Já existe uma entrada de início automático não criada pelo GigaDock. Ajuste-a nas configurações do sistema.");
        if (!enabled) { if (File.Exists(path)) File.Delete(path); return; }
        if (!Path.IsPathFullyQualified(executable) || !LinuxCommands.IsExecutable(executable) || executable.Any(char.IsControl))
            throw new ArgumentException("Instale a versão publicada antes de ativar o início automático.");
        var escaped = executable.Replace("\\", "\\\\\\\\").Replace("\"", "\\\\\"").Replace("`", "\\\\`").Replace("$", "\\\\$").Replace("%", "%%");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var body = "[Desktop Entry]\nType=Application\nName=GigaDock\nExec=\"" + escaped + "\"\nTerminal=false\nX-GigaDock-Managed=true\n";
        var temp = path + ".tmp"; File.WriteAllText(temp, body, new UTF8Encoding(false)); File.Move(temp, path, true);
    }
}
