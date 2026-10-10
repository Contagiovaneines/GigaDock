using System.Diagnostics;

namespace GigaDock.Infrastructure.Linux;

public record CommandResult(int ExitCode, string Output, string Error);
public interface ILinuxCommands
{
    Task<CommandResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellation = default);
}

public sealed class LinuxCommands(TimeSpan? timeout = null, int outputLimit = 512 * 1024) : ILinuxCommands
{
    public static string? Find(string name)
    {
        if (name.Any(char.IsControl)) return null;
        if (Path.IsPathFullyQualified(name)) return IsExecutable(name) ? name : null;
        if (name.Contains('/') || name.Contains('\\')) return null;
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (!Path.IsPathFullyQualified(directory)) continue;
            var candidate = Path.Combine(directory, name);
            if (IsExecutable(candidate)) return candidate;
        }
        return null;
    }

    public static bool IsExecutable(string path)
    {
        if (!File.Exists(path) || Directory.Exists(path)) return false;
        if (!OperatingSystem.IsLinux()) return false;
        try
        {
            var mode = File.GetUnixFileMode(path);
            return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }

    public async Task<CommandResult> RunAsync(string executable, IReadOnlyList<string> arguments, CancellationToken cancellation = default)
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("A prévia Windows não executa comandos Linux.");
        var path = Find(executable) ?? throw new InvalidOperationException($"O recurso precisa do programa {executable}, que não está instalado ou não tem permissão de execução.");
        var start = new ProcessStartInfo(path) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["LC_ALL"] = "C.UTF-8";
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new IOException("Não foi possível iniciar o programa.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(timeout ?? TimeSpan.FromSeconds(10));
        async Task<string> ReadLimited(StreamReader reader)
        {
            var text = new System.Text.StringBuilder(); var buffer = new char[2048];
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), deadline.Token)) > 0)
            {
                if (text.Length + count > outputLimit)
                {
                    deadline.Cancel();
                    throw new IOException("A saída do programa ultrapassou o limite permitido.");
                }
                text.Append(buffer, 0, count);
            }
            return text.ToString();
        }
        var output = ReadLimited(process.StandardOutput);
        var error = ReadLimited(process.StandardError);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            return new(process.ExitCode, await output, await error);
        }
        catch (Exception failure) when (failure is OperationCanceledException or IOException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            try { await Task.WhenAll(output, error); } catch (Exception readError) when (readError is OperationCanceledException or IOException) { }
            if (cancellation.IsCancellationRequested) throw new OperationCanceledException(cancellation);
            if (output.IsFaulted || error.IsFaulted) throw new IOException("A saída do programa ultrapassou o limite permitido.");
            throw new IOException("O serviço do sistema não respondeu no tempo esperado.");
        }
    }
}
