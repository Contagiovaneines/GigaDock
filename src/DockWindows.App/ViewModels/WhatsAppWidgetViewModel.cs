using DockWindows.App.Common;
using DockWindows.Core.Models;
using System.Windows.Input;

namespace DockWindows.App.ViewModels;

public class WhatsAppWidgetViewModel : ObservableObject
{
    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private int _mensagensNaoLidas = 0;
    private string _ultimaMensagem = "Abrir WhatsApp";

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public int MensagensNaoLidas 
    { 
        get => _mensagensNaoLidas; 
        set 
        { 
            if (SetProperty(ref _mensagensNaoLidas, value)) 
            {
                OnPropertyChanged(nameof(TemMensagens));
                UltimaMensagem = value > 0 ? "Novas mensagens!" : "Abrir WhatsApp";
            }
        } 
    }
    public string UltimaMensagem { get => _ultimaMensagem; set => SetProperty(ref _ultimaMensagem, value); }
    
    public bool TemMensagens => MensagensNaoLidas > 0;

    public ICommand AbrirAppCommand { get; }

    public WhatsAppWidgetViewModel(System.Action<string>? onAlerta = null)
    {
        AbrirAppCommand = new RelayCommand(() => {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "whatsapp://", UseShellExecute = true }); } catch { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://web.whatsapp.com", UseShellExecute = true }); } catch {} }
        });
    }
}
