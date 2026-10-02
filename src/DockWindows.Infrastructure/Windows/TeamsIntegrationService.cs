using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DockWindows.Infrastructure.Windows;

public class TeamsIntegrationService
{
    private readonly DispatcherTimer _timer;

    public event Action<string, string>? OnStatusChanged;
    public event Action<string>? OnMeetingChanged;

    public TeamsIntegrationService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += (s, e) => VerificarStatus();
    }

    public void Iniciar()
    {
        _timer.Start();
        VerificarStatus();
    }

    private void VerificarStatus()
    {
        // NOTA: Para ler dados reais do Novo Teams (Teams V2),
        // é necessário conectar à Local API via WebSocket (ws://localhost:8124) 
        // ou monitorar os logs do WebView2 do Teams.
        // Simulando a alternância de status
        var rnd = new Random();
        if (rnd.Next(2) == 0)
            OnStatusChanged?.Invoke("Disponível", "#23A736");
        else
            OnStatusChanged?.Invoke("Ocupado", "#C4314B");

        OnMeetingChanged?.Invoke("Daily Team - 10:00");
    }
}

