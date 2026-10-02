path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\AjustesViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

new_props = """
    public double AlturaBarra
    {
        get => _mainVm.AlturaBarra;
        set { _mainVm.AlturaBarra = value; OnPropertyChanged(); }
    }

    public double OpacidadeDock
    {
        get => _mainVm.OpacidadeDock;
        set { _mainVm.OpacidadeDock = value; OnPropertyChanged(); }
    }

    public double RaioCantosDock
    {
        get => _mainVm.RaioCantosDock;
        set { _mainVm.RaioCantosDock = value; OnPropertyChanged(); }
    }

    public bool ExibirClima"""

c = c.replace("public bool ExibirClima", new_props)
with open(path, "w", encoding="utf-8") as f:
    f.write(c)
