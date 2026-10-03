path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

anchor = "public double TamanhoIconeNumerico"
insert = """    public double EscalaUI => AlturaBarra / 64.0;

    """
c = c.replace(anchor, insert + anchor)

# Also notify EscalaUI when height changes
c = c.replace(
    "OnPropertyChanged(nameof(TamanhoIconeNumerico));",
    "OnPropertyChanged(nameof(TamanhoIconeNumerico));\n                OnPropertyChanged(nameof(EscalaUI));"
)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
