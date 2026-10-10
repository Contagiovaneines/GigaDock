using System.Globalization;
using System.Text;

namespace GigaDock.Infrastructure.Linux;

public record DesktopApplication(string Id, string Name, string DesktopFile, string Icon, bool Terminal, bool DBusActivatable);

/// <summary>Metadados freedesktop. Exec/field codes/Terminal/D-Bus são executados pelo GIO, nunca por um shell próprio.</summary>
public sealed class DesktopCatalog
{
    private readonly string[] _roots;
    private readonly string _desktop;
    private readonly Func<string, bool> _executable;
    public DesktopCatalog(IEnumerable<string>? roots = null, string? desktop = null, Func<string, bool>? executable = null)
    {
        _roots = (roots ?? GetApplicationRoots()).Where(Path.IsPathFullyQualified).Select(Path.GetFullPath).Distinct().ToArray();
        _desktop = desktop ?? Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
        _executable = executable ?? (name => LinuxCommands.Find(name) is not null);
    }

    public static IEnumerable<string> GetApplicationRoots()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var user = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        yield return Path.Combine(Path.IsPathFullyQualified(user ?? "") ? user! : Path.Combine(home, ".local", "share"), "applications");
        foreach (var root in (Environment.GetEnvironmentVariable("XDG_DATA_DIRS") ?? "/usr/local/share:/usr/share").Split(':'))
            if (Path.IsPathFullyQualified(root)) yield return Path.Combine(root, "applications");
        // Flatpak/Snap exports may be absent from a development shell's XDG_DATA_DIRS.
        yield return Path.Combine(home, ".local/share/flatpak/exports/share/applications");
        yield return "/var/lib/flatpak/exports/share/applications";
        yield return "/var/lib/snapd/desktop/applications";
    }

    public IReadOnlyList<DesktopApplication> Scan()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var apps = new List<DesktopApplication>();
        foreach (var root in _roots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var file in Enumerate(root).Order(StringComparer.Ordinal))
                {
                    var id = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '-');
                    if (!seen.Add(id)) continue; // hidden user overrides suppress system entries too.
                    var app = Read(file, id);
                    if (app is not null) apps.Add(app);
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
        return apps.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private static IEnumerable<string> Enumerate(string root)
    {
        var pending = new Stack<string>(); pending.Push(root);
        var options = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = 0 };
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(directory, "*.desktop", options)) yield return file;
            foreach (var child in Directory.EnumerateDirectories(directory, "*", options))
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) pending.Push(child);
        }
    }

    public DesktopApplication? Read(string file, string? id = null)
    {
        if (!Path.IsPathFullyQualified(file) || !file.EndsWith(".desktop", StringComparison.Ordinal) || !File.Exists(file)) return null;
        try
        {
            if (new FileInfo(file).Length > 1024 * 1024) return null;
            var keys = new Dictionary<string, string>(StringComparer.Ordinal);
            var active = false;
            foreach (var line in File.ReadLines(file, Encoding.UTF8))
            {
                var text = line.Trim();
                if (text.StartsWith('[')) { active = text == "[Desktop Entry]"; continue; }
                if (!active || text.StartsWith('#')) continue;
                var equal = text.IndexOf('=');
                if (equal > 0) keys[text[..equal].Trim()] = text[(equal + 1)..];
            }
            string Get(string key) => keys.GetValueOrDefault(key, "");
            bool Flag(string key) => Get(key) == "true";
            if (Get("Type") != "Application" || Flag("Hidden") || Flag("NoDisplay")) return null;
            var current = _desktop.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (Get("OnlyShowIn") is { Length: > 0 } only && !only.Split(';').Intersect(current, StringComparer.Ordinal).Any()) return null;
            if (Get("NotShowIn").Split(';').Intersect(current, StringComparer.Ordinal).Any()) return null;
            if (Get("TryExec") is { Length: > 0 } attempt && !_executable(Unescape(attempt))) return null;
            if (Get("Exec").Length == 0 && !Flag("DBusActivatable")) return null;
            var locale = CultureInfo.CurrentUICulture.Name.Replace('-', '_');
            var name = Get("Name[" + locale + "]");
            if (name.Length == 0) name = Get("Name[" + locale.Split('_')[0] + "]");
            if (name.Length == 0) name = Get("Name");
            if (string.IsNullOrWhiteSpace(name)) return null;
            return new(id ?? Path.GetFileName(file), Unescape(name), Path.GetFullPath(file), Unescape(Get("Icon")), Flag("Terminal"), Flag("DBusActivatable"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { return null; }
    }

    private static string Unescape(string value) => value.Replace("\\s", " ").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\\\", "\\");
}
