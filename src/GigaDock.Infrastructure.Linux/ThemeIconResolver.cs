namespace GigaDock.Infrastructure.Linux;

/// <summary>Ícones PNG por tema/inheritance e pixmaps. SVG/XPM usam ícone vetorial de fallback nesta beta.</summary>
public sealed class ThemeIconResolver
{
    private readonly string[] _roots;
    public ThemeIconResolver(IEnumerable<string>? roots = null)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _roots = (roots ?? new[] { Path.Combine(home, ".icons") }.Concat(DesktopCatalog.GetApplicationRoots()
            .Where(root => root.EndsWith("/applications", StringComparison.Ordinal)).Select(root => Path.Combine(Path.GetDirectoryName(root)!, "icons")))).ToArray();
    }
    public string? Resolve(string icon, int size = 48, string theme = "hicolor")
    {
        bool Usable(string file) => file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && File.Exists(file) && new FileInfo(file).Length < 8 * 1024 * 1024;
        try
        {
            if (Path.IsPathFullyQualified(icon)) return Usable(icon) ? icon : null;
            if (string.IsNullOrWhiteSpace(icon) || icon.IndexOfAny(['/', '\\']) >= 0 || icon.Any(char.IsControl) || theme is "." or ".." || theme.IndexOfAny(['/', '\\']) >= 0) return null;
            var pending = new Queue<string>(); pending.Enqueue(theme); var visited = new HashSet<string>(StringComparer.Ordinal);
            while (pending.Count > 0)
            {
                var current = pending.Dequeue(); if (!visited.Add(current)) continue;
                foreach (var root in _roots)
                {
                    var directory = Path.Combine(root, current); var index = Path.Combine(directory, "index.theme");
                    if (!File.Exists(index) || new FileInfo(index).Length > 1024 * 1024) continue;
                    var lines = File.ReadAllLines(index); var names = lines.Where(line => line.StartsWith("Directories=", StringComparison.Ordinal) || line.StartsWith("ScaledDirectories=", StringComparison.Ordinal)).SelectMany(line => line[(line.IndexOf('=') + 1)..].Split(','));
                    foreach (var subdir in names.OrderBy(name => name.Contains(size + "x" + size, StringComparison.Ordinal) ? 0 : 1))
                    {
                        var candidate = Path.GetFullPath(Path.Combine(directory, subdir, icon.EndsWith(".png", StringComparison.Ordinal) ? icon : icon + ".png"));
                        if (candidate.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.Ordinal) && Usable(candidate)) return candidate;
                    }
                    foreach (var inherited in lines.Where(line => line.StartsWith("Inherits=", StringComparison.Ordinal)).SelectMany(line => line[9..].Split(',', StringSplitOptions.RemoveEmptyEntries)))
                        if (inherited.Trim() is not ("." or "..") && !inherited.Contains('/') && !inherited.Contains('\\')) pending.Enqueue(inherited.Trim());
                }
                if (pending.Count == 0 && !visited.Contains("hicolor")) pending.Enqueue("hicolor");
            }
            var fallback = Path.Combine("/usr/share/pixmaps", icon.EndsWith(".png", StringComparison.Ordinal) ? icon : icon + ".png");
            return Usable(fallback) ? fallback : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { return null; }
    }
}
