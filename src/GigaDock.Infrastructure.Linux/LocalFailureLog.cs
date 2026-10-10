using DockWindows.Core.Services;

namespace GigaDock.Infrastructure.Linux;

/// <summary>Diagnóstico local mínimo. Não registra ambiente completo, argumentos, configurações ou credenciais.</summary>
public static class LocalFailureLog
{
    public static string? TryWrite(IAppDirectories directories, Exception error)
    {
        try
        {
            var folder = Path.Combine(directories.Estado, "logs");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "falha-inicializacao.log");
            // Somente o tipo da exceção; mensagem/stack podem conter entrada do usuário.
            File.AppendAllText(file, $"{DateTimeOffset.UtcNow:O} Falha de inicialização: {error.GetType().FullName}{Environment.NewLine}");
            return file;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }
}
