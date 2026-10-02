using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class TeamsWidgetViewModel : ObservableObject
{
    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private string _status = "Disponível";
    private string _corStatus = "#23A736"; // Verde
    private string _proximaReuniao = "Sem reuniões próximas";

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string CorStatus { get => _corStatus; set => SetProperty(ref _corStatus, value); }
    public string ProximaReuniao { get => _proximaReuniao; set => SetProperty(ref _proximaReuniao, value); }

    public TeamsWidgetViewModel()
    {
        // Mock inicial
        Status = "Ocupado";
        CorStatus = "#C4314B"; // Vermelho
        ProximaReuniao = "Daily Team - 10:00";
    }
}
