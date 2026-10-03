path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = "if (wObs != null) { Obs.Habilitado = wObs.Visivel; Obs.Formato = wObs.Formato; } else { Obs.Habilitado = false; }"
good = "if (wObs != null) { Obs.Habilitado = wObs.Visivel; } else { Obs.Habilitado = false; }"

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
