import re

# MainViewModel
path_main = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\MainViewModel.cs"
with open(path_main, "r", encoding="utf-8") as f:
    c_main = f.read()

replacement_main = """
    public bool ExibirLixeira
    {
        get => _preferencias.ExibirLixeira;
        set
        {
            if (_preferencias.ExibirLixeira != value)
            {
                _preferencias.ExibirLixeira = value;
                OnPropertyChanged();
                SalvarPreferencias();
            }
        }
    }

    public bool ExibirClima"""

c_main = c_main.replace("public bool ExibirClima", replacement_main)
c_main = c_main.replace("OnPropertyChanged(nameof(ExibirClima));", "OnPropertyChanged(nameof(ExibirClima));\n        OnPropertyChanged(nameof(ExibirLixeira));")
c_main = c_main.replace("public ICommand SairCommand { get; }", "public ICommand SairCommand { get; }\n    public ICommand AbrirLixeiraCommand { get; }")
c_main = c_main.replace("SairCommand = new RelayCommand(() => SolicitarFechamento?.Invoke());", 'SairCommand = new RelayCommand(() => SolicitarFechamento?.Invoke());\n        AbrirLixeiraCommand = new RelayCommand(() => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = "shell:RecycleBinFolder", UseShellExecute = true }); } catch { } });')

with open(path_main, "w", encoding="utf-8") as f:
    f.write(c_main)

# AjustesViewModel
path_ajustes = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\AjustesViewModel.cs"
with open(path_ajustes, "r", encoding="utf-8") as f:
    c_ajustes = f.read()

replacement_ajustes = """
    public bool ExibirLixeira
    {
        get => _preferencias.ExibirLixeira;
        set
        {
            if (_preferencias.ExibirLixeira != value)
            {
                _preferencias.ExibirLixeira = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ExibirClima"""

c_ajustes = c_ajustes.replace("public bool ExibirClima", replacement_ajustes)

with open(path_ajustes, "w", encoding="utf-8") as f:
    f.write(c_ajustes)
