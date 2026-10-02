using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class WhatsAppWidgetViewModel : ObservableObject
{
    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private int _mensagensNaoLidas = 0;
    private string _ultimaMensagem = "Buscando...";
    private readonly WhatsAppNotificationService _service;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public int MensagensNaoLidas { get => _mensagensNaoLidas; set { if (SetProperty(ref _mensagensNaoLidas, value)) OnPropertyChanged(nameof(TemMensagens)); } }
    public string UltimaMensagem { get => _ultimaMensagem; set => SetProperty(ref _ultimaMensagem, value); }
    public bool TemMensagens => MensagensNaoLidas > 0;

    public WhatsAppWidgetViewModel()
    {
        _service = new WhatsAppNotificationService();
        _service.OnNotificacoesAtualizadas += (count, lastMsg) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                MensagensNaoLidas = count;
                UltimaMensagem = lastMsg;
            });
        };
        _ = _service.IniciarAsync();
    }
}
