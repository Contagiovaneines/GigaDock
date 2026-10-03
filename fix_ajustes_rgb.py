path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\AjustesViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

insert = """    public bool ModoRgbMedia
    {
        get => _mainVm.ModoRgbMedia;
        set
        {
            _mainVm.ModoRgbMedia = value;
            OnPropertyChanged();
        }
    }

"""
c = c.replace("public bool AlertasVisuaisHabilitados", insert + "public bool AlertasVisuaisHabilitados")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
