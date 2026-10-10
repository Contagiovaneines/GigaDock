using System.Text.RegularExpressions;

namespace GigaDock.Infrastructure.Linux;

public sealed record FlatpakApplication(string Id, string Name, string Version, string Branch, string Architecture, bool UserInstallation);

public sealed class LinuxFlatpakService(ILinuxCommands? commands = null, ILinuxApplicationLauncher? launcher = null)
{
    private readonly ILinuxCommands _commands = commands ?? new LinuxCommands();
    private readonly ILinuxApplicationLauncher _launcher = launcher ?? new LinuxApplicationLauncher();
    public async Task<IReadOnlyList<FlatpakApplication>> ListAsync(CancellationToken token = default)
    {
        var apps = new List<FlatpakApplication>();
        foreach (var user in new[] { true, false })
        {
            var result = await _commands.RunAsync("flatpak", ["list", user ? "--user" : "--system", "--app", "--columns=application,name,version,branch,arch"], token);
            if (result.ExitCode != 0) throw new IOException("Não foi possível consultar os aplicativos Flatpak. Confira a instalação do Flatpak.");
            apps.AddRange(Parse(result.Output, user));
        }
        return apps.DistinctBy(app => (app.Id, app.Branch, app.Architecture, app.UserInstallation)).OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
    public static IReadOnlyList<FlatpakApplication> Parse(string output, bool user)
    {
        if (output.Length > 512 * 1024) throw new IOException("A lista Flatpak ultrapassou o limite de leitura.");
        var apps = new List<FlatpakApplication>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Take(2000))
        {
            var columns = line.TrimEnd('\r').Split('\t');
            if (columns.Length != 5 || columns.Any(c => c.Length > 200 || c.Any(char.IsControl))) continue;
            var app = new FlatpakApplication(columns[0], columns[1], columns[2], columns[3], columns[4], user);
            if (Valid(app)) apps.Add(app);
        }
        return apps;
    }
    public Task LaunchAsync(FlatpakApplication app, CancellationToken token = default)
    {
        if (!Valid(app)) throw new ArgumentException("O identificador Flatpak é inválido.");
        return _launcher.LaunchAsync("flatpak", ["run", app.UserInstallation ? "--user" : "--system", "--arch=" + app.Architecture, "--branch=" + app.Branch, app.Id], token);
    }
    private static bool Valid(FlatpakApplication app) =>
        app.Id.Length <= 255 && Regex.IsMatch(app.Id, "^[A-Za-z_][A-Za-z0-9_-]*(\\.[A-Za-z_][A-Za-z0-9_-]*){2,}$", RegexOptions.CultureInvariant) &&
        Regex.IsMatch(app.Branch, "^[A-Za-z0-9][A-Za-z0-9._-]{0,99}$", RegexOptions.CultureInvariant) &&
        Regex.IsMatch(app.Architecture, "^[A-Za-z0-9_]{1,32}$", RegexOptions.CultureInvariant);
}
