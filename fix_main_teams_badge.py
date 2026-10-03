path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Update inside IncrementarNotificacaoApp
bad1 = """                if (!string.IsNullOrEmpty(cor))
                {
                    CorAlerta = cor;
                    EstaEmAlerta = true;
                }"""
good1 = """                if (!string.IsNullOrEmpty(cor))
                {
                    CorAlerta = cor;
                    EstaEmAlerta = true;
                }
                
                if (proc.Contains("teams") || proc.Contains("msteams"))
                {
                    Teams.MensagensNaoLidas = app.NumeroNotificacoes;
                }"""
c = c.replace(bad1, good1)

# Update at the end of AtualizarAplicativosAbertos
bad2 = """        // 4. Atualizar as coleções (adicionar os ícones dos apps abertos globalmente)
        foreach (var col in TodasColecoesAtivas)"""
good2 = """        // Sync Teams badge
        var teamsApp = Aplicativos.FirstOrDefault(a => (!string.IsNullOrEmpty(a.Titulo) && (a.Titulo.ToLowerInvariant().Contains("teams") || a.Titulo.ToLowerInvariant().Contains("msteams"))) || (!string.IsNullOrEmpty(a.CaminhoExecutavel) && a.CaminhoExecutavel.ToLowerInvariant().Contains("ms-teams")));
        if (teamsApp != null) Teams.MensagensNaoLidas = teamsApp.NumeroNotificacoes;
        else Teams.MensagensNaoLidas = 0;

        // 4. Atualizar as coleções (adicionar os ícones dos apps abertos globalmente)
        foreach (var col in TodasColecoesAtivas)"""
c = c.replace(bad2, good2)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
