path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

insert = """    public bool AlertasVisuaisHabilitados
    {
        get => _preferencias.AlertasVisuaisHabilitados;
        set
        {
            if (_preferencias.AlertasVisuaisHabilitados != value)
            {
                _preferencias.AlertasVisuaisHabilitados = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

"""
c = c.replace("public bool ExibirLixeira", insert + "public bool ExibirLixeira")

# Fix IncrementarNotificacaoApp to trigger global alert with app color
old_inc = """    public void IncrementarNotificacaoApp(IntPtr hwnd)
    {
        var app = 
System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<AppItemViewModel>(Aplicativos), a => 
System.Linq.Enumerable.Any(a.Janelas, j => j.Hwnd == hwnd));
        if (app != null)
        {
            app.NumeroNotificacoes++;
        }
    }"""
    
new_inc = """    public void IncrementarNotificacaoApp(IntPtr hwnd)
    {
        var app = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<AppItemViewModel>(Aplicativos), a => System.Linq.Enumerable.Any(a.Janelas, j => j.Hwnd == hwnd));
        if (app != null)
        {
            app.NumeroNotificacoes++;
            
            if (AlertasVisuaisHabilitados)
            {
                string proc = (app.Titulo ?? string.Empty).ToLowerInvariant();
                string cor = "#0A84FF"; // Blue default
                
                if (proc.Contains("teams")) cor = "#4A448C"; // Roxo
                else if (proc.Contains("whatsapp")) cor = "#25D366"; // Verde
                else if (proc.Contains("discord")) cor = "#5865F2"; // Azul discord
                else if (proc.Contains("slack")) cor = "#E01E5A"; // Rosa slack
                
                CorAlerta = cor;
                EstaEmAlerta = true;
            }
        }
    }"""
    
c = c.replace(old_inc, new_inc)

# Wait, if old_inc doesn't match perfectly, let's just do a regex replace
import re
c = re.sub(r'public void IncrementarNotificacaoApp\(IntPtr hwnd\).*?app\.NumeroNotificacoes\+\+;\s*\}\s*\}', new_inc, c, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
