using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public class TeamsWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => _service.TimerAtivo;
    public DockWindows.Core.Widgets.SaudeWidget Saude => DockWindows.Core.Widgets.SaudeWidget.Disponivel;
    public string? MotivoEstado => DescricaoIntegracao;

    private bool _habilitado;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private string _status = "Buscando...";
    private string _corStatus = "#808080";
    private string _proximaReuniao = "Conectando...";
    private int _mensagensNaoLidas;
    private readonly TeamsIntegrationService _service;

    public void ExibirMensagemDe(string nome)
    {
        if (!string.IsNullOrWhiteSpace(nome))
        {
            ProximaReuniao = "Msg: " + nome;
            // Volta para "Nenhuma atividade" depois de 10 segundos
            _mensagemAte = DateTimeOffset.UtcNow.AddSeconds(10);
        }
    }

    private DateTimeOffset _mensagemAte;
    private bool _visual, _disposed;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        _visual = estado.Visual && !_disposed;
        _service.Parar();
        if (_visual) { if (_mensagemAte < DateTimeOffset.UtcNow && ProximaReuniao.StartsWith("Msg: ")) ProximaReuniao = "Nenhuma atividade"; _service.Iniciar(); }
    }
    public void Dispose() { _disposed = true; _visual = false; _service.Dispose(); }

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
    public string FontePresenca => "heurística";
    public string ConfiancaPresenca => "baixa";
    private string _estadoNotificacoes = "Não consultada";
    public string EstadoNotificacoes { get => _estadoNotificacoes; set => SetProperty(ref _estadoNotificacoes, value); }
    public string DescricaoIntegracao => "Notificações: " + EstadoNotificacoes + " · " + "Status estimado por processos e títulos de janela; contagem de notificações do Windows, sem confirmação de leitura.";
    public bool TemMensagem => _mensagensNaoLidas > 0;

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string CorStatus { get => _corStatus; set => SetProperty(ref _corStatus, value); }
    public string ProximaReuniao { get => _proximaReuniao; set => SetProperty(ref _proximaReuniao, value); }

    public System.Windows.Input.ICommand AbrirAppCommand { get; }
    public System.Windows.Input.ICommand TestarAlertaCommand { get; }

    public TeamsWidgetViewModel(System.Action<string>? onAlerta = null)
    {
        TestarAlertaCommand = new RelayCommand(() => onAlerta?.Invoke("#8B7CFF"));
        AbrirAppCommand = new RelayCommand(() => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "msteams:", UseShellExecute = true }); } catch { } });
        _service = new TeamsIntegrationService();
        _service.OnStatusChanged += (status, cor) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                if (!_visual || _disposed) return;
                Status = (string.IsNullOrWhiteSpace(status) ? "Indisponível" : status) + " (estimado)";
                CorStatus = string.IsNullOrWhiteSpace(cor) ? "#808080" : cor;
            });
        };
        _service.OnMeetingChanged += (reuniao) =>
        {
            System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                if (!_visual || _disposed || _mensagemAte > DateTimeOffset.UtcNow) return;
                ProximaReuniao = string.IsNullOrWhiteSpace(reuniao) ? "Nenhuma atividade" : reuniao;
            });
        };

    }
}
