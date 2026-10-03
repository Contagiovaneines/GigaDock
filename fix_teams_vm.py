path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.App\ViewModels\TeamsWidgetViewModel.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

# Remove the random alert trigger and handle empty strings (offline)
new_constructor = """    public TeamsWidgetViewModel(System.Action<string>? onAlerta = null)
    {
        AbrirAppCommand = new RelayCommand(() => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "msteams:", UseShellExecute = true }); } catch { } });
        _service = new TeamsIntegrationService();
        _service.OnStatusChanged += (status, cor) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                Status = string.IsNullOrWhiteSpace(status) ? "Offline" : status;
                CorStatus = string.IsNullOrWhiteSpace(cor) ? "#808080" : cor;
            });
        };
        _service.OnMeetingChanged += (reuniao) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                ProximaReuniao = string.IsNullOrWhiteSpace(reuniao) ? "Nenhuma atividade" : reuniao;
            });
        };
        _service.Iniciar();
    }"""

import re
c = re.sub(r'public TeamsWidgetViewModel\(System\.Action<string>\? onAlerta = null\).*?\}', new_constructor, c, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
