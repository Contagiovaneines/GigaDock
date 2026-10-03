path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# 1. Remove timer field
c = c.replace("private System.Windows.Threading.DispatcherTimer? _syncNotificacoesTimer;", "")

# 2. Remove timer initialization
timer_init = """        _syncNotificacoesTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromSeconds(3) };
        _syncNotificacoesTimer.Tick += async (s, e) => await SincronizarNotificacoesComWindowsAsync();
        _syncNotificacoesTimer.Start();"""
c = c.replace(timer_init, "")

# 3. Remove polling method (use regex or string indexing)
start_idx = c.find("private async System.Threading.Tasks.Task SincronizarNotificacoesComWindowsAsync()")
if start_idx != -1:
    end_idx = c.find("public void IncrementarNotificacaoApp(IntPtr hwnd)", start_idx)
    if end_idx != -1:
        c = c[:start_idx] + c[end_idx:]

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
