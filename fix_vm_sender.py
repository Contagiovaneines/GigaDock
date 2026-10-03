path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        _toastService.OnNotificationReceived += (appName, isCall) => {
            Application.Current?.Dispatcher?.InvokeAsync(() => {
                TratarNotificacaoToast(appName, isCall);
            });
        };"""
good = """        _toastService.OnNotificationReceived += (appName, isCall, senderName) => {
            Application.Current?.Dispatcher?.InvokeAsync(() => {
                TratarNotificacaoToast(appName, isCall, senderName);
            });
        };"""
c = c.replace(bad, good)

bad2 = """    private void TratarNotificacaoToast(string appName, bool isCall = false)"""
good2 = """    private void TratarNotificacaoToast(string appName, bool isCall = false, string senderName = "")"""
c = c.replace(bad2, good2)

bad3 = """        // Update Widgets
        if (proc.Contains("teams") || proc.Contains("msteams"))
        {
            Teams.MensagensNaoLidas++;
        }"""
good3 = """        // Update Widgets
        if (proc.Contains("teams") || proc.Contains("msteams"))
        {
            Teams.MensagensNaoLidas++;
            if (!string.IsNullOrEmpty(senderName)) Teams.ExibirMensagemDe(senderName);
        }"""
c = c.replace(bad3, good3)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
