using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class WhatsAppWidgetViewModel : ObservableObject
{
    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private int _mensagensNaoLidas = 0;
    private string _ultimaMensagem = "Nenhuma nova mensagem";

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public int MensagensNaoLidas { get => _mensagensNaoLidas; set { if (SetProperty(ref _mensagensNaoLidas, value)) OnPropertyChanged(nameof(TemMensagens)); } }
    public string UltimaMensagem { get => _ultimaMensagem; set => SetProperty(ref _ultimaMensagem, value); }
    public bool TemMensagens => MensagensNaoLidas > 0;

    public WhatsAppWidgetViewModel()
    {
        // Mock inicial
        MensagensNaoLidas = 3;
        UltimaMensagem = "Reunião confirmada às 14h.";
    }
}
