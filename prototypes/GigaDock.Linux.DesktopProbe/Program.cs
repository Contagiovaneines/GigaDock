using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace GigaDock.Linux.DesktopProbe;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--diagnostico"))
            {
                Console.WriteLine(ProbeReport.EnvironmentSummary());
                return 0;
            }
            return AppBuilder.Configure<ProbeApp>().UsePlatformDetect()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Não foi possível abrir o protótipo: {ex.Message}");
            Console.Error.WriteLine("Confira as bibliotecas gráficas e se há uma sessão X11/XWayland disponível.");
            return 1;
        }
    }
}

public sealed class ProbeApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dock = new ProbeWindow();
            desktop.MainWindow = dock;
            if (desktop.Args?.Contains("--smoke") == true)
            {
                dock.Opened += (_, _) => Dispatcher.UIThread.Post(() =>
                {
                    Console.WriteLine(dock.CreateReport());
                    Console.WriteLine("SMOKE_OK: janela criada e ciclo de UI executado.");
                    desktop.Shutdown();
                }, DispatcherPriority.Background);
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
