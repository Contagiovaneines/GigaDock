path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

c = c.replace("return System.Windows.Media.Color.FromArgb(140, 0, 0, 0); // Black with 0.55 Opacity -> #8C000000 approx, or we just return Black and let the drop shadow opacity handle it.\n            // Wait, DropShadowEffect expects just the color, it has its own Opacity property!\n            // Let's return Colors.Black for the default", "return System.Windows.Media.Colors.Black;")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
