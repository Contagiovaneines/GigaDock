path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\MainWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Modify DockShadow
bad = '<DropShadowEffect x:Name="DockShadow" BlurRadius="28" ShadowDepth="8" Direction="270" Color="#000000" Opacity="0.55"/>'
good = '<DropShadowEffect x:Name="DockShadow" BlurRadius="28" ShadowDepth="8" Direction="270" Color="{Binding CorSombraDock}" Opacity="0.55"/>'

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
