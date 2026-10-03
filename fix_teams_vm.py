path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\TeamsWidgetViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

insert_props = """    private string _corStatus = "#808080";
    private string _proximaReuniao = "Conectando...";
    private int _mensagensNaoLidas;
    private readonly TeamsIntegrationService _service;

    public int MensagensNaoLidas
    {
        get => _mensagensNaoLidas;
        set
        {
            if (SetProperty(ref _mensagensNaoLidas, value))
            {
                OnPropertyChanged(nameof(TemMensagem));
            }
        }
    }
    public bool TemMensagem => _mensagensNaoLidas > 0;
"""

c = c.replace("    private string _corStatus = \"#808080\";\n    private string _proximaReuniao = \"Conectando...\";\n    private readonly TeamsIntegrationService _service;\n", insert_props)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
