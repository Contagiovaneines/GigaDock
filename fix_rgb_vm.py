path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """    public System.Windows.Media.Color CorSombraDock
    {
        get
        {
            if (ModoRgbMedia && Midia != null && Midia.EstaTocando && Midia.TemMidia)
            {
                try { return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Midia.CorPredominanteHex); }
                catch { }
            }
            return System.Windows.Media.Colors.Black;
        }
    }"""

good = """    public System.Windows.Media.Color CorSombraDock
    {
        get
        {
            return System.Windows.Media.Colors.Black; // Usaremos o arco-íris via GlowRgbVisivel
        }
    }

    public bool GlowRgbVisivel => ModoRgbMedia && Midia != null && Midia.EstaTocando && Midia.TemMidia;"""

c = c.replace(bad, good)

# Also need to trigger PropertyChanged for GlowRgbVisivel when Midia state changes
# Let's search for how Midia events are handled. Wait, we bound to Midia.PropertyChanged earlier!
with open(path, "w", encoding="utf-8") as f:
    f.write(c)
