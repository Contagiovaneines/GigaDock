using DockWindows.Core.Services;

namespace GigaDock.Services.Paths;

/// <summary>Resolve XDG sem criar pastas. Variáveis relativas são ignoradas conforme a especificação.</summary>
public sealed class XdgAppDirectories : IAppDirectories
{
    public string Configuracoes { get; }
    public string Dados { get; }
    public string Cache { get; }
    public string Estado { get; }

    public XdgAppDirectories(string home, Func<string, string?>? environment = null)
    {
        if (string.IsNullOrWhiteSpace(home) || !Path.IsPathFullyQualified(home))
            throw new ArgumentException("Informe o diretório pessoal absoluto do usuário.", nameof(home));
        environment ??= Environment.GetEnvironmentVariable;
        Configuracoes = Resolve(environment("XDG_CONFIG_HOME"), home, ".config");
        Dados = Resolve(environment("XDG_DATA_HOME"), home, ".local/share");
        Cache = Resolve(environment("XDG_CACHE_HOME"), home, ".cache");
        Estado = Resolve(environment("XDG_STATE_HOME"), home, ".local/state");
    }

    private static string Resolve(string? configured, string home, string fallback)
    {
        var root = !string.IsNullOrWhiteSpace(configured) && Path.IsPathFullyQualified(configured)
            ? configured : Path.Combine(home, fallback.Replace('/', Path.DirectorySeparatorChar));
        return Path.Combine(root, "gigadock");
    }
}
