path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\AjustesViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

insert = """    public bool AlertasVisuaisHabilitados
    {
        get => _mainVm.AlertasVisuaisHabilitados;
        set
        {
            _mainVm.AlertasVisuaisHabilitados = value;
            OnPropertyChanged();
        }
    }

"""
c = c.replace("public bool ExibirLixeira", insert + "public bool ExibirLixeira")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
