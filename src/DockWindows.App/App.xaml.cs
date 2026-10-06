using System.Windows;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private System.Threading.Mutex? _instancia;
    private System.Threading.EventWaitHandle? _ativar;
    private System.Threading.RegisteredWaitHandle? _esperaAtivacao;
    private bool _instanciaPrincipal;

    protected override void OnStartup(StartupEventArgs e)
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory;
        // Local isola a sessão; o SID isola usuários, mantendo o nome estável entre versões.
        var sid = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var nome = @"Local\GigaDock." + sid;
        _instancia = new System.Threading.Mutex(false, nome + ".Instancia");
        try { _instanciaPrincipal = _instancia.WaitOne(0); }
        catch (System.Threading.AbandonedMutexException) { _instanciaPrincipal = true; }
        _ativar = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, nome + ".Ativar");
        if (!_instanciaPrincipal)
        {
            _ativar.Set();
            Shutdown();
            return;
        }
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            RestaurarBarraEmergencia();
        };

        DispatcherUnhandledException += (s, args) =>
        {
            RestaurarBarraEmergencia();
        };
        var janela = new MainWindow();
        MainWindow = janela;
        _esperaAtivacao = System.Threading.ThreadPool.RegisterWaitForSingleObject(_ativar,
            (_, _) => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!Dispatcher.HasShutdownStarted) janela.ExibirInstanciaExistente();
            })), null, System.Threading.Timeout.Infinite, false);
        janela.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _esperaAtivacao?.Unregister(null);
        _ativar?.Dispose();
        if (_instanciaPrincipal)
        {
            RestaurarBarraEmergencia();
            _instancia?.ReleaseMutex();
        }
        _instancia?.Dispose();
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


