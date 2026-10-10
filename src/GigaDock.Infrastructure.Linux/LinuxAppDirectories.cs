using DockWindows.Core.Services;
using GigaDock.Services.Paths;

namespace GigaDock.Infrastructure.Linux;

public static class LinuxAppDirectories
{
    public static IAppDirectories Resolve(string? dataRoot = null)
    {
        if (dataRoot is not null) return new IsolatedDirectories(dataRoot);
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("A prévia fora do Linux exige --data-root com um diretório absoluto de teste.");
        return new XdgAppDirectories(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    private sealed class IsolatedDirectories : IAppDirectories
    {
        private readonly string _root;
        public IsolatedDirectories(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root))
                throw new ArgumentException("O diretório de teste deve ser absoluto.", nameof(root));
            _root = Path.GetFullPath(root);
        }
        public string Configuracoes => Path.Combine(_root, "config");
        public string Dados => Path.Combine(_root, "data");
        public string Cache => Path.Combine(_root, "cache");
        public string Estado => Path.Combine(_root, "state");
    }
}
