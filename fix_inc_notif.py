path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """                if (!string.IsNullOrEmpty(cor))
                {
                    CorAlerta = cor;
                    EstaEmAlerta = true;
                }"""

good = """                if (!string.IsNullOrEmpty(cor))
                {
                    DispararAlertaGlobal(cor);
                }"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
