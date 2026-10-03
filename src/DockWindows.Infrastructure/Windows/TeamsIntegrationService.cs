using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace DockWindows.Infrastructure.Windows;

/// <summary>
/// Le o estado real do Microsoft Teams monitorando os processos e titulos de janela.
/// Nao requer conta, API nem internet — tudo local via Win32.
/// </summary>
public class TeamsIntegrationService
{
    private readonly DispatcherTimer _timer;

    public event Action<string, string>? OnStatusChanged;
    public event Action<string>? OnMeetingChanged;

    private string _lastStatus = string.Empty;
    private string _lastContext = string.Empty;

    public TeamsIntegrationService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (s, e) => VerificarStatus();
    }

    public void Iniciar()
    {
        _timer.Start();
        VerificarStatus();
    }

    private void VerificarStatus()
    {
        try
        {
            // Detectar se o Teams esta rodando
            var teamsProcs = Process.GetProcessesByName("ms-teams")
                .Concat(Process.GetProcessesByName("Teams"))
                .Concat(Process.GetProcessesByName("msteams"))
                .Where(p => !p.HasExited)
                .ToArray();

            if (teamsProcs.Length == 0)
            {
                // Teams nao esta aberto
                if (_lastStatus != "offline")
                {
                    _lastStatus = "offline";
                    _lastContext = string.Empty;
                    OnStatusChanged?.Invoke(string.Empty, string.Empty);
                    OnMeetingChanged?.Invoke(string.Empty);
                }
                return;
            }

            // Ler titulos de todas as janelas do Teams
            string? callTitle = null;
            string? meetingTitle = null;
            bool inCall = false;

            foreach (var proc in teamsProcs)
            {
                try
                {
                    string title = proc.MainWindowTitle ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    // Detectar chamada ativa: Teams mostra "Microsoft Teams - Em chamada" ou "| Microsoft Teams"
                    if (title.Contains("chamada", StringComparison.OrdinalIgnoreCase)
                     || title.Contains("calling", StringComparison.OrdinalIgnoreCase)
                     || title.Contains("meeting", StringComparison.OrdinalIgnoreCase)
                     || title.Contains("reuniao", StringComparison.OrdinalIgnoreCase)
                     || title.Contains("reunião", StringComparison.OrdinalIgnoreCase))
                    {
                        inCall = true;
                        // Extrair nome da sala do titulo (antes do " | " ou do " - ")
                        var parts = title.Split(new[] { " | ", " - " }, StringSplitOptions.RemoveEmptyEntries);
                        callTitle = parts.FirstOrDefault(p =>
                            !p.Contains("Microsoft Teams", StringComparison.OrdinalIgnoreCase) &&
                            !p.Contains("chamada", StringComparison.OrdinalIgnoreCase) &&
                            !p.Contains("calling", StringComparison.OrdinalIgnoreCase) &&
                            !p.Contains("meeting", StringComparison.OrdinalIgnoreCase)) ?? title;
                    }
                    else if (title.Contains("Teams", StringComparison.OrdinalIgnoreCase))
                    {
                        meetingTitle = title;
                    }
                }
                catch { }
            }

            string newStatus;
            string newCor;
            string newContext;

            if (inCall)
            {
                newStatus = "Em chamada";
                newCor = "#C4314B";
                newContext = !string.IsNullOrWhiteSpace(callTitle)
                    ? $"Sala: {callTitle.Trim()}"
                    : "Chamada em andamento";
            }
                        else if (teamsProcs.Any())
            {
                // Como não temos acesso à API do Graph (para manter 100% offline/local),
                // não podemos ler o status real de "Ocupado" ou "Ausente".
                // Portanto, mostramos um status neutro para não mentir.
                newStatus = "Aberto";
                newCor = "#8E8E93"; // Cinza neutro
                newContext = string.Empty;
            }
            else
            {
                newStatus = string.Empty;
                newCor = string.Empty;
                newContext = string.Empty;
            }

            // Apenas notificar se mudou algo
            if (newStatus != _lastStatus || newContext != _lastContext)
            {
                _lastStatus = newStatus;
                _lastContext = newContext;
                OnStatusChanged?.Invoke(newStatus, newCor);
                OnMeetingChanged?.Invoke(newContext);
            }
        }
        catch { /* silencioso */ }
    }
}
