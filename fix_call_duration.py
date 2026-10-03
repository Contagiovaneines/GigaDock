path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        _toastService.OnNotificationReceived += appName => {
            Application.Current?.Dispatcher?.InvokeAsync(() => {
                TratarNotificacaoToast(appName);
            });
        };"""
good = """        _toastService.OnNotificationReceived += (appName, isCall) => {
            Application.Current?.Dispatcher?.InvokeAsync(() => {
                TratarNotificacaoToast(appName, isCall);
            });
        };"""
c = c.replace(bad, good)

bad2 = """    private void TratarNotificacaoToast(string appName)"""
good2 = """    private void TratarNotificacaoToast(string appName, bool isCall = false)"""
c = c.replace(bad2, good2)

bad3 = """            if (!string.IsNullOrEmpty(cor))
            {
                DispararAlertaGlobal(cor);
            }"""
good3 = """            if (!string.IsNullOrEmpty(cor))
            {
                DispararAlertaGlobal(cor, isCall);
            }"""
c = c.replace(bad3, good3)

bad4 = """    public void DispararAlertaGlobal(string corHex)"""
good4 = """    public void DispararAlertaGlobal(string corHex, bool isCall = false)"""
c = c.replace(bad4, good4)

bad5 = """            // Auto-desligar o alerta aps 15 segundos (simulando o tempo de tocar)
            System.Threading.Tasks.Task.Delay(15000).ContinueWith(_ => """
good5 = """            // Auto-desligar o alerta. 2.5s para mensagem (pisca ~1 vez), 30s para chamada.
            int delayMs = isCall ? 30000 : 2500;
            System.Threading.Tasks.Task.Delay(delayMs).ContinueWith(_ => """
c = c.replace(bad5, good5)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
