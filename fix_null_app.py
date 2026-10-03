path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """            if (app != null)
        {
            app.NumeroNotificacoes++;
        }"""

good = """        if (app != null)
        {
            app.NumeroNotificacoes++;
        }
        else
        {
            // Se o app não está na dock, incrementa o widget diretamente
            if (proc.Contains("teams") || proc.Contains("msteams"))
                Teams.MensagensNaoLidas++;
            else if (proc.Contains("whatsapp"))
                WhatsApp.MensagensNaoLidas++;
        }"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
