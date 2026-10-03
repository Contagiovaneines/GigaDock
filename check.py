path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\MainWindow.xaml"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Find SectionsContainer
print(c[c.find("SectionsContainer"):c.find("SectionsContainer")+300])
