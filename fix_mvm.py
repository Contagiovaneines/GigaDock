import re

path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

replacement = """
    public void IncrementarNotificacaoApp(IntPtr hwnd)
    {
        var app = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<AppItemViewModel>(ItensBarra), a => System.Linq.Enumerable.Any(a.Janelas, j => j.Hwnd == hwnd));
        if (app != null)
        {
            app.NumeroNotificacoes++;
        }
    }
}"""
content = re.sub(r"\}\s*$", replacement, content)

with open(path, "w", encoding="utf-8") as f:
    f.write(content)
