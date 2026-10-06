using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.UI.Notifications.Management;
using Windows.UI.Notifications;

namespace DockWindows.Infrastructure.Windows;

public class WhatsAppNotificationService : IDisposable
{
    private readonly UserNotificationListener _listener;
    private readonly DispatcherTimer _timer;
    private bool _ocupado, _disposed;
    public void Parar() => _timer.Stop();
    public void Dispose() { _disposed = true; Parar(); OnNotificacoesAtualizadas = null; }

    public event Action<int, string>? OnNotificacoesAtualizadas;

    public WhatsAppNotificationService()
    {
        _listener = UserNotificationListener.Current;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += async (s, e) => await VerificarNotificacoesAsync();
    }

    public async Task IniciarAsync()
    {
        if (_disposed || _timer.IsEnabled) return;
        try
        {
            var accessStatus = await _listener.RequestAccessAsync();
            if (!_disposed && accessStatus == UserNotificationListenerAccessStatus.Allowed)
            {
                _timer.Start();
                await VerificarNotificacoesAsync();
            }
        }
        catch
        {
            // O serviço pode falhar se não tiver as permissões de app manifest configuradas
        }
    }

    private async Task VerificarNotificacoesAsync()
    {
        if (_disposed || _ocupado) return;
        _ocupado = true;
        try
        {
            var notificacoes = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            // Filtra notificações do WhatsApp (pelo nome de exibição do App)
            var whatsAppNotifs = notificacoes.Where(n => n.AppInfo.DisplayInfo.DisplayName.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase)).ToList();

            int count = whatsAppNotifs.Count;
            string lastMsg = "Nenhuma nova mensagem";

            if (count > 0)
            {
                var ultima = whatsAppNotifs.First();
                var textElements = ultima.Notification.Visual.Bindings.FirstOrDefault()?.GetTextElements();
                if (textElements != null && textElements.Count > 0)
                {
                    string sender = textElements[0].Text;
                    string body = textElements.Count > 1 ? textElements[1].Text : "";
                    lastMsg = string.IsNullOrWhiteSpace(body) ? sender : $"{sender}: {body}";
                }
            }

            OnNotificacoesAtualizadas?.Invoke(count, lastMsg);
        }
        catch 
        { 
        }
        finally { _ocupado = false; }
    }
}
