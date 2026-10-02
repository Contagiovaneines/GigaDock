using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class TeamsWidgetViewModel : ObservableObject
{
    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private string _status = "Buscando...";
    private string _corStatus = "#808080";
    private string _proximaReuniao = "Conectando...";
    private readonly TeamsIntegrationService _service;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string CorStatus { get => _corStatus; set => SetProperty(ref _corStatus, value); }
    public string ProximaReuniao { get => _proximaReuniao; set => SetProperty(ref _proximaReuniao, value); }

    public System.Windows.Input.ICommand AbrirAppCommand { get; }

    public TeamsWidgetViewModel(System.Action<string>? onAlerta = null)
    {
        AbrirAppCommand = new RelayCommand(() => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "msteams:", UseShellExecute = true }); } catch { } });
        _service = new TeamsIntegrationService();
        _service.OnStatusChanged += (status, cor) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                Status = status;
                if (status == "Chamando..." || status == "Nova mensagem" || status == "Em chamada") onAlerta?.Invoke("#4A448C");
                CorStatus = cor;
            });
        };
        _service.OnMeetingChanged += (reuniao) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                ProximaReuniao = reuniao;
            });
        };
        _service.Iniciar();
    }
}








