using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DockWindows.Infrastructure.Windows;

public class TeamsIntegrationService
{
    private readonly DispatcherTimer _timer;
    private int _stateIndex = 0;

    public event Action<string, string>? OnStatusChanged;
    public event Action<string>? OnMeetingChanged;

    public TeamsIntegrationService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
        _timer.Tick += (s, e) => VerificarStatus();
    }

    public void Iniciar()
    {
        _timer.Start();
        VerificarStatus();
    }

    private void VerificarStatus()
    {
        // NOTA: Simulação cíclica de estados reais do Teams
        switch (_stateIndex)
        {
            case 0:
                OnStatusChanged?.Invoke("Disponível", "#23A736");
                OnMeetingChanged?.Invoke("Sem eventos agora");
                break;
            case 1:
                OnStatusChanged?.Invoke("Ocupado", "#C4314B");
                OnMeetingChanged?.Invoke("Daily Team - 10:00");
                break;
            case 2:
                OnStatusChanged?.Invoke("Em chamada", "#C4314B");
                OnMeetingChanged?.Invoke("Sala: Sync de Projetos");
                break;
            case 3:
                OnStatusChanged?.Invoke("Nova mensagem", "#4A448C");
                OnMeetingChanged?.Invoke("Design Team: mandei os prints");
                break;
        }

        _stateIndex = (_stateIndex + 1) % 4;
    }
}
