path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        Midia.PropertyChanged += (s, e) => {
            if (e.PropertyName == "CorPredominanteHex" || e.PropertyName == "EstaTocando" || e.PropertyName == "TemMidia") {
                OnPropertyChanged(nameof(CorSombraDock));
            }
        };"""

good = """        Midia.PropertyChanged += (s, e) => {
            if (e.PropertyName == "CorPredominanteHex" || e.PropertyName == "EstaTocando" || e.PropertyName == "TemMidia") {
                OnPropertyChanged(nameof(CorSombraDock));
                OnPropertyChanged(nameof(GlowRgbVisivel));
            }
        };"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
