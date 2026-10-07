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
    private bool _inicioConcluido;
    private int _tratandoFalhaDispatcher;

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
            RegistrarFalha("crash.txt", args.ExceptionObject?.ToString() ?? "Falha não identificada.");
            RestaurarBarraEmergencia();
        };

        DispatcherUnhandledException += (s, args) =>
        {
            RegistrarFalha("crash_dispatcher.txt", args.Exception.ToString());
            if (_inicioConcluido && Interlocked.Exchange(ref _tratandoFalhaDispatcher, 1) == 0)
            {
                args.Handled = true;
                Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (MainWindow is MainWindow janela) janela.ExibirInstanciaExistente();
                    }
                    finally { Interlocked.Exchange(ref _tratandoFalhaDispatcher, 0); }
                });
            }
            else
            {
                RestaurarBarraEmergencia();
            }
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            RegistrarFalha("tarefas-nao-observadas.txt", args.Exception.ToString());
            args.SetObserved();
        };
        try
        {
            var janela = new MainWindow();
            MainWindow = janela;
            _esperaAtivacao = System.Threading.ThreadPool.RegisterWaitForSingleObject(_ativar,
                (_, _) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!Dispatcher.HasShutdownStarted) janela.ExibirInstanciaExistente();
                })), null, System.Threading.Timeout.Infinite, false);
            janela.Show();
            _inicioConcluido = true;
        }
        catch (Exception ex)
        {
            RegistrarFalha("falha-inicializacao.txt", ex.ToString());
            RestaurarBarraEmergencia();
            MessageBox.Show($"A GigaDock não conseguiu iniciar. O diagnóstico foi salvo em %LOCALAPPDATA%\\DockWindows\\logs.\n\n{ex.Message}",
                "GigaDock — falha na inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
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

    private static void RegistrarFalha(string nome, string conteudo)
    {
        try
        {
            var pasta = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "logs");
            System.IO.Directory.CreateDirectory(pasta);
            System.IO.File.WriteAllText(System.IO.Path.Combine(pasta, nome), $"{DateTimeOffset.Now:O}{Environment.NewLine}{conteudo}");
        }
        catch { }
    }
}


