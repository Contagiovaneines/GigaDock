path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        var wDiscord = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.DiscordVoz);
        if (wDiscord != null) { Discord.Habilitado = wDiscord.Visivel; Discord.Formato = wDiscord.Formato; } else { Discord.Habilitado = false; }
"""

good = """        var wDiscord = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.DiscordVoz);
        if (wDiscord != null) { Discord.Habilitado = wDiscord.Visivel; Discord.Formato = wDiscord.Formato; } else { Discord.Habilitado = false; }

        var wObs = amb.WidgetsInstalados.FirstOrDefault(w => w.Tipo == TipoWidget.OBSStudio);
        if (wObs != null) { Obs.Habilitado = wObs.Visivel; Obs.Formato = wObs.Formato; } else { Obs.Habilitado = false; }
"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
