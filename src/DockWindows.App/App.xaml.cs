using System.Windows;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            RestaurarBarraEmergencia();
        };

        DispatcherUnhandledException += (s, args) =>
        {
            RestaurarBarraEmergencia();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        RestaurarBarraEmergencia();
        base.OnExit(e);
    }

    private static void RestaurarBarraEmergencia()
    {
        try
        {
            var repo = new JsonSettingsRepository();
            var prefs = repo.Carregar();
            if (prefs.UsarComoBarraPrincipal)
            {
                var taskbar = new Win32TaskbarService();
                taskbar.RestaurarBarraNativa(prefs.EstadoAnteriorBarraTarefas);
            }
        }
        catch { }
    }
}

