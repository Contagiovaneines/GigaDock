path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Add to the constructor
insert_service = """    private readonly DockWindows.Infrastructure.Windows.ToastNotificationService _toastService;
"""
c = c.replace("    private readonly IWinKeyHookService _winKeyHookService;", "    private readonly IWinKeyHookService _winKeyHookService;\n" + insert_service)

init_service = """        _winKeyHookService = winKeyHookService ?? new WinKeyHookService();
        _toastService = new DockWindows.Infrastructure.Windows.ToastNotificationService();
        _toastService.OnNotificationReceived += appName => {
            Application.Current?.Dispatcher?.InvokeAsync(() => {
                TratarNotificacaoToast(appName);
            });
        };
        _ = _toastService.Iniciar();
"""
c = c.replace("        _winKeyHookService = winKeyHookService ?? new WinKeyHookService();", init_service)

# Add TratarNotificacaoToast method
method = """    private void TratarNotificacaoToast(string appName)
    {
        if (string.IsNullOrEmpty(appName)) return;
        string proc = appName.ToLowerInvariant();
        
        // Find matching app to increment badge
        var app = Aplicativos.FirstOrDefault(a => 
            (!string.IsNullOrEmpty(a.Titulo) && a.Titulo.ToLowerInvariant().Contains(proc)) ||
            (!string.IsNullOrEmpty(a.CaminhoExecutavel) && a.CaminhoExecutavel.ToLowerInvariant().Contains(proc))
        );

        if (app != null)
        {
            app.NumeroNotificacoes++;
        }

        if (AlertasVisuaisHabilitados)
        {
            string cor = string.Empty;
            if (proc.Contains("teams") || proc.Contains("msteams")) cor = "#4A448C"; // Roxo
            else if (proc.Contains("whatsapp")) cor = "#25D366"; // Verde
            else if (proc.Contains("discord")) cor = "#5865F2"; // Azul discord
            
            if (!string.IsNullOrEmpty(cor))
            {
                CorAlerta = cor;
                EstaEmAlerta = true;
            }
        }
        
        // Update Widgets
        if (proc.Contains("teams") || proc.Contains("msteams"))
        {
            Teams.MensagensNaoLidas++;
        }
    }

"""
c = c.replace("    public void IncrementarNotificacaoApp(IntPtr hwnd)", method + "    public void IncrementarNotificacaoApp(IntPtr hwnd)")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
