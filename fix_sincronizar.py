path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """    private void SincronizarBadgeWidget(AppItemViewModel app)
    {
        string proc = (app.Titulo ?? string.Empty).ToLowerInvariant();
        if (proc.Contains("teams") || proc.Contains("msteams"))
        {
            Teams.MensagensNaoLidas = app.NumeroNotificacoes;
        }
        else if (proc.Contains("whatsapp"))
        {
            WhatsApp.MensagensNaoLidas = app.NumeroNotificacoes;
        }
    }"""

good = """    private void SincronizarBadgeWidget(AppItemViewModel app)
    {
        string titulo = (app.Titulo ?? string.Empty).ToLowerInvariant();
        string caminho = (app.CaminhoExecutavel ?? string.Empty).ToLowerInvariant();
        
        if (titulo.Contains("teams") || titulo.Contains("msteams") || caminho.Contains("ms-teams"))
        {
            Teams.MensagensNaoLidas = app.NumeroNotificacoes;
        }
        else if (titulo.Contains("whatsapp") || caminho.Contains("whatsapp"))
        {
            WhatsApp.MensagensNaoLidas = app.NumeroNotificacoes;
        }
    }"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
