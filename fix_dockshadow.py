path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\MainWindow.xaml.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        // Restaura valores originais
        DockShadow.Color = System.Windows.Media.Colors.Black;
        DockShadow.BlurRadius = 16.0;"""

good = """        // Restaura valores originais (ClearValue restaura o Binding ou valor do XAML original)
        DockShadow.ClearValue(System.Windows.Media.Effects.DropShadowEffect.ColorProperty);
        DockShadow.ClearValue(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty);"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
