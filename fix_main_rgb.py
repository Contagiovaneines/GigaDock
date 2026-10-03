path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

insert = """    public bool ModoRgbMedia
    {
        get => _preferencias.ModoRgbMedia;
        set
        {
            if (_preferencias.ModoRgbMedia != value)
            {
                _preferencias.ModoRgbMedia = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CorSombraDock));
                SalvarPreferencias();
            }
        }
    }

    public System.Windows.Media.Color CorSombraDock
    {
        get
        {
            if (ModoRgbMedia && Midia != null && Midia.EstaTocando && Midia.TemMidia)
            {
                try { return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Midia.CorPredominanteHex); }
                catch { }
            }
            return System.Windows.Media.Color.FromArgb(140, 0, 0, 0); // Black with 0.55 Opacity -> #8C000000 approx, or we just return Black and let the drop shadow opacity handle it.
            // Wait, DropShadowEffect expects just the color, it has its own Opacity property!
            // Let's return Colors.Black for the default
        }
    }
"""

c = c.replace("public bool AlertasVisuaisHabilitados", insert + "public bool AlertasVisuaisHabilitados")

# Hook up PropertyChanged from Midia to notify CorSombraDock
init_midia_str = "Midia = new MidiaWidgetViewModel(() => _preferencias.AbrirPlayerAoDuploClique);"
init_midia_new = """Midia = new MidiaWidgetViewModel(() => _preferencias.AbrirPlayerAoDuploClique);
        Midia.PropertyChanged += (s, e) => {
            if (e.PropertyName == "CorPredominanteHex" || e.PropertyName == "EstaTocando" || e.PropertyName == "TemMidia") {
                OnPropertyChanged(nameof(CorSombraDock));
            }
        };"""

c = c.replace(init_midia_str, init_midia_new)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
