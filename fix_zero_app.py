path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\AppItemViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """    public bool EstaAtivo
    {
        get => _estaAtivo;
        set
        {
            if (SetProperty(ref _estaAtivo, value))
            {
                if (value) NumeroNotificacoes = 0;
            }
        }
    }"""

good = """    public bool EstaAtivo
    {
        get => _estaAtivo;
        set
        {
            if (SetProperty(ref _estaAtivo, value))
            {
                if (value) 
                {
                    NumeroNotificacoes = 0;
                    OnPropertyChanged(nameof(NumeroNotificacoes)); // Força atualização para zerar widgets mesmo se já for 0
                }
            }
        }
    }"""

c = c.replace(bad, good)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
