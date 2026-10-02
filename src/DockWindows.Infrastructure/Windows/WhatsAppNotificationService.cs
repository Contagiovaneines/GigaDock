using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.UI.Notifications.Management;
using Windows.UI.Notifications;

namespace DockWindows.Infrastructure.Windows;

public class WhatsAppNotificationService
{
    private readonly UserNotificationListener _listener;
    private readonly DispatcherTimer _timer;

    public event Action<int, string>? OnNotificacoesAtualizadas;

    public WhatsAppNotificationService()
    {
        _listener = UserNotificationListener.Current;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += async (s, e) => await VerificarNotificacoesAsync();
    }

    public async Task IniciarAsync()
    {
        try
        {
            var accessStatus = await _listener.RequestAccessAsync();
            if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
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
    }
}
