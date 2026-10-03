path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad_alert1 = """            if (!string.IsNullOrEmpty(cor))
            {
                CorAlerta = cor;
                EstaEmAlerta = true;
            }"""

good_alert1 = """            if (!string.IsNullOrEmpty(cor))
            {
                DispararAlertaGlobal(cor);
            }"""

c = c.replace(bad_alert1, good_alert1)

bad_global = """    public void DispararAlertaGlobal(string corHex)
    {
        CorAlerta = corHex;
        EstaEmAlerta = true;

        // Auto-desligar o alerta após 15 segundos (simulando o tempo de tocar)
        System.Threading.Tasks.Task.Delay(15000).ContinueWith(_ => 
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                EstaEmAlerta = false;
            });
        });
    }"""

good_global = """    public void DispararAlertaGlobal(string corHex)
    {
        CorAlerta = corHex;
        
        // Força o gatilho da animação no WPF alternando pra false antes
        if (EstaEmAlerta)
        {
            EstaEmAlerta = false;
            OnPropertyChanged(nameof(EstaEmAlerta));
        }
        
        EstaEmAlerta = true;

        // Auto-desligar o alerta após 15 segundos (simulando o tempo de tocar)
        System.Threading.Tasks.Task.Delay(15000).ContinueWith(_ => 
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                EstaEmAlerta = false;
            });
        });
    }"""

# handle encodings for 'após'
import re
c = re.sub(r'public void DispararAlertaGlobal.*?EstaEmAlerta = false;\s*\}\);\s*\}\);\s*\}', good_global, c, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
