path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """                _preferencias.ModoRgbMedia = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CorSombraDock));
                SalvarPreferencias();"""

good = """                _preferencias.ModoRgbMedia = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CorSombraDock));
                OnPropertyChanged(nameof(GlowRgbVisivel));
                SalvarPreferencias();"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
