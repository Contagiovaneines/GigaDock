using DockWindows.App.Common;
using DockWindows.Core.Models;
using System.Windows.Input;

namespace DockWindows.App.ViewModels;

public class WhatsAppWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => EstadoNotificacoes == "Permitida";

    public DockWindows.Core.Widgets.SaudeWidget Saude => EstadoNotificacoes == "Permitida" ? DockWindows.Core.Widgets.SaudeWidget.Disponivel : DockWindows.Core.Widgets.SaudeWidget.Indisponivel;
    public string? MotivoEstado => DescricaoIntegracao;

    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private int _mensagensNaoLidas = 0;
    private string _ultimaMensagem = "Abrir WhatsApp";

    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado) { }
    public void Dispose() { }
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
                UltimaMensagem = value > 0 ? "Notificações detectadas" : "Abrir WhatsApp";
            }
        } 
    }
    public string UltimaMensagem { get => _ultimaMensagem; set => SetProperty(ref _ultimaMensagem, value); }
    
    private string _estadoNotificacoes = "Não consultada";
    public string EstadoNotificacoes { get => _estadoNotificacoes; set => SetProperty(ref _estadoNotificacoes, value); }
    public string DescricaoIntegracao => EstadoNotificacoes + " · " + "Notificações acessíveis do Windows; não confirma mensagens não lidas. Requer permissão do Windows.";
    public bool TemMensagens => MensagensNaoLidas > 0;

    public ICommand AbrirAppCommand { get; }
    public ICommand TestarAlertaCommand { get; }

    public WhatsAppWidgetViewModel(System.Action<string>? onAlerta = null)
    {
        TestarAlertaCommand = new RelayCommand(() => onAlerta?.Invoke("#25D366"));
        AbrirAppCommand = new RelayCommand(() => {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "whatsapp://", UseShellExecute = true }); } catch { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://web.whatsapp.com", UseShellExecute = true }); } catch {} }
        });
    }
}
