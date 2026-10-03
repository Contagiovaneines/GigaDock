path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Add a DispatcherTimer field
c = c.replace("private readonly DockWindows.Infrastructure.Windows.ToastNotificationService _toastService;", 
              "private readonly DockWindows.Infrastructure.Windows.ToastNotificationService _toastService;\n    private System.Windows.Threading.DispatcherTimer? _syncNotificacoesTimer;")

# Initialize and start the timer in the constructor
timer_init = """        _toastService.OnNotificationReceived += appName => {
            Application.Current?.Dispatcher?.InvokeAsync(() => {
                TratarNotificacaoToast(appName);
            });
        };
        _ = _toastService.Iniciar();

        _syncNotificacoesTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(3) };
        _syncNotificacoesTimer.Tick += async (s, e) => await SincronizarNotificacoesComWindowsAsync();
        _syncNotificacoesTimer.Start();
"""
c = c.replace("""        _toastService.OnNotificationReceived += appName => {
            Application.Current?.Dispatcher?.InvokeAsync(() => {
                TratarNotificacaoToast(appName);
            });
        };
        _ = _toastService.Iniciar();""", timer_init)

# Add the sync method
method = """
    private async System.Threading.Tasks.Task SincronizarNotificacoesComWindowsAsync()
    {
        var contagens = await _toastService.ObterContagemNotificacoesPorAppAsync();
        
        System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
        {
            int whatsappCount = 0;
            int teamsCount = 0;
            int discordCount = 0;

            foreach(var kvp in contagens)
            {
                string proc = kvp.Key.ToLowerInvariant();
                if (proc.Contains("whatsapp")) whatsappCount += kvp.Value;
                else if (proc.Contains("teams") || proc.Contains("msteams")) teamsCount += kvp.Value;
                else if (proc.Contains("discord")) discordCount += kvp.Value;
            }

            WhatsApp.MensagensNaoLidas = whatsappCount;
            // Só sobrescrevemos o Teams e Discord se a contagem do Windows for maior, pois eles também usam a janela ativa
            if (teamsCount > Teams.MensagensNaoLidas) Teams.MensagensNaoLidas = teamsCount;
            if (whatsappCount == 0) WhatsApp.MensagensNaoLidas = 0; // Força zerar se não tem nada no Windows
        });
    }
"""

c = c.replace("    public void IncrementarNotificacaoApp(IntPtr hwnd)", method + "\n    public void IncrementarNotificacaoApp(IntPtr hwnd)")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
