using System.Diagnostics;

namespace GigaDock.Infrastructure.Linux;

public interface ILinuxApplicationLauncher
{
    Task LaunchAsync(string executable, IReadOnlyList<string> arguments, CancellationToken token = default);
}

public sealed class LinuxApplicationLauncher : ILinuxApplicationLauncher
{
    public async Task LaunchAsync(string executable, IReadOnlyList<string> arguments, CancellationToken token = default)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Aplicativos Linux só podem ser abertos no Linux.");
        token.ThrowIfCancellationRequested();
        var path = LinuxCommands.Find(executable) ?? throw new InvalidOperationException($"Instale {executable} para usar este recurso.");
        var start = new ProcessStartInfo(path) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException("Não foi possível abrir o aplicativo.");
        await Task.Delay(250, token);
        if (process.HasExited && process.ExitCode != 0) throw new IOException("O aplicativo não pôde ser iniciado. Confira sua instalação Flatpak.");
        // Dispose releases our handle; an application intentionally launched by the user remains running.
    }
}
