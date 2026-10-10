using System.Globalization;
using System.Text.RegularExpressions;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace GigaDock.Infrastructure.Linux;

/// <summary>EWMH via wmctrl, optado apenas em sessão X11 real. Não cobre janelas Wayland nativas.</summary>
public sealed class X11WindowService : IDesktopWindowService
{
    private readonly ILinuxCommands _commands;
    private DesktopWindow[] _windows = [];
    private CancellationTokenSource? _cancellation;
    private Task? _polling;
    public event Action? JanelasAlteradas;
    public DesktopWindowCapabilities Capacidades { get; } = new(true, true, false, true);
    public X11WindowService(ILinuxCommands? commands = null) => _commands = commands ?? new LinuxCommands();
    public static IDesktopWindowService ForCurrentSession() =>
        OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "x11" && LinuxCommands.Find("wmctrl") is not null
            ? new X11WindowService() : new UnsupportedDesktopWindowService();

    public void Iniciar()
    {
        if (_cancellation is not null) return;
        _cancellation = new CancellationTokenSource();
        _polling = PollAsync(_cancellation.Token);
    }
    private async Task PollAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await _commands.RunAsync("wmctrl", ["-lp"], token);
                var latest = result.ExitCode == 0 ? Parse(result.Output).ToArray() : [];
                if (!_windows.SequenceEqual(latest)) { _windows = latest; JanelasAlteradas?.Invoke(); }
                await Task.Delay(3000, token);
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or InvalidOperationException)
            {
                if (token.IsCancellationRequested) break;
                _windows = [];
                try { await Task.Delay(3000, token); } catch (OperationCanceledException) { break; }
            }
        }
    }
    public static IReadOnlyList<DesktopWindow> Parse(string output)
    {
        var windows = new List<DesktopWindow>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = Regex.Split(line.Trim(), "\\s+", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (fields.Length < 5 || !Regex.IsMatch(fields[0], "^0x[0-9a-fA-F]{1,16}$") || !int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var pid)) continue;
            windows.Add(new DesktopWindow(new DesktopWindowId("x11-ewmh", fields[0]), string.Join(' ', fields.Skip(4)), "", "", pid, false, false));
        }
        return windows;
    }
    public IReadOnlyList<DesktopWindow> ObterJanelasAbertas() => _windows.ToArray();
    private bool Action(DesktopWindowId id, string command)
    {
        if (id.Backend != "x11-ewmh" || string.IsNullOrEmpty(id.Value) || !Regex.IsMatch(id.Value, "^0x[0-9a-fA-F]{1,16}$") || !_windows.Any(w => w.Id == id)) return false;
        try { return _commands.RunAsync("wmctrl", ["-i", command, id.Value]).GetAwaiter().GetResult().ExitCode == 0; }
        catch (Exception error) when (error is IOException or InvalidOperationException) { return false; }
    }
    public bool Ativar(DesktopWindowId id) => Action(id, "-a");
    public bool Minimizar(DesktopWindowId id) => false;
    public bool Fechar(DesktopWindowId id) => Action(id, "-c");
    public void Parar()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose(); _cancellation = null;
    }
    public void Dispose() { Parar(); JanelasAlteradas = null; }
}
