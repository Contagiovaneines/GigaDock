using Avalonia;
using DockWindows.Core.Services;
using GigaDock.Infrastructure.Linux;

namespace GigaDock.App.Linux;

internal static class Program
{
    internal static LinuxApplicationSession Session { get; private set; } = null!;
    internal static bool Smoke { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        IAppDirectories? directories = null;
        try
        {
            string? dataRoot = null;
            bool preview = false, diagnostic = false;
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--data-root" when i + 1 < args.Length: dataRoot = args[++i]; break;
                    case "--preview": preview = true; break;
                    case "--smoke": Smoke = true; break;
                    case "--diagnostico": diagnostic = true; break;
                    case "--help":
                        Console.WriteLine("GigaDock [--data-root <diretório absoluto>] [--diagnostico] [--smoke]\nPrévia fora do Linux: --preview --data-root <diretório absoluto de teste>");
                        return 0;
                    default: throw new ArgumentException($"Opção inválida ou incompleta: {args[i]}");
                }
            }
            if (!OperatingSystem.IsLinux() && (!preview || dataRoot is null))
                throw new PlatformNotSupportedException("Use Linux ou --preview --data-root <diretório absoluto de teste> para conferir a estrutura no Windows.");
            if (Smoke && dataRoot is null)
                throw new ArgumentException("O teste --smoke exige --data-root para isolar as configurações.");
            directories = LinuxAppDirectories.Resolve(dataRoot);
            using var instance = SingleInstance.TryAcquire(directories.Estado);
            if (instance is null)
            {
                Console.WriteLine("O GigaDock já está aberto para estas configurações. Use o botão de ajustes da dock.");
                return 0;
            }
            using var session = new LinuxApplicationSession(directories);
            Session = session;
            if (diagnostic)
            {
                Console.WriteLine(session.Describe());
                return 0;
            }
            return AppBuilder.Configure<LinuxApp>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Não foi possível iniciar o GigaDock: {ex.Message}");
            Console.Error.WriteLine("Confira os caminhos, permissões, bibliotecas nativas e a sessão gráfica.");
            if (directories is not null && LocalFailureLog.TryWrite(directories, ex) is { } log)
                Console.Error.WriteLine($"Diagnóstico local: {log}");
            return 1;
        }
    }
}
