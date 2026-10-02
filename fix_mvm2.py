import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# Strip any existing IncrementarNotificacaoApp outside class
content = re.sub(r"public void IncrementarNotificacaoApp.*?\n\}", "", content, flags=re.DOTALL)
content = content.rstrip("}\n ")

replacement = """
    public void IncrementarNotificacaoApp(IntPtr hwnd)
    {
        var app = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<AppItemViewModel>(Aplicativos), a => System.Linq.Enumerable.Any(a.Janelas, j => j.Hwnd == hwnd));
        if (app != null)
        {
            app.NumeroNotificacoes++;
        }
    }
}
"""

with open(path, "w", encoding="utf-8") as f:
    f.write(content + replacement)
