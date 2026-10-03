path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\TeamsWidgetViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

c = c.replace("public int MensagensNaoLidas", """public void ExibirMensagemDe(string nome)
    {
        if (!string.IsNullOrWhiteSpace(nome))
        {
            ProximaReuniao = "Msg: " + nome;
            // Volta para "Nenhuma atividade" depois de 10 segundos
            System.Threading.Tasks.Task.Delay(10000).ContinueWith(_ => {
                System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() => {
                    if (ProximaReuniao == "Msg: " + nome) ProximaReuniao = "Nenhuma atividade";
                });
            });
        }
    }

    public int MensagensNaoLidas""")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
