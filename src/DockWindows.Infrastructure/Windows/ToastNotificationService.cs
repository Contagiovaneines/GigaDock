using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DockWindows.Infrastructure.Windows
{
    public class ToastNotificationService
    {
        public event Action<string>? OnNotificationReceived;
        private UserNotificationListener? _listener;

        public async Task Iniciar()
        {
            try
            {
                _listener = UserNotificationListener.Current;
                var access = await _listener.RequestAccessAsync();
                
                if (access == UserNotificationListenerAccessStatus.Allowed)
                {
                    _listener.NotificationChanged += Listener_NotificationChanged;
                }
            }
            catch { }
        }

        private async void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
        {
            try
            {
                if (args.ChangeKind == UserNotificationChangedKind.Added)
                {
                    var notifs = await sender.GetNotificationsAsync(NotificationKinds.Toast);
                    var nova = notifs.FirstOrDefault(n => n.Id == args.UserNotificationId);
                    if (nova != null)
                    {
                        string appName = nova.AppInfo.DisplayInfo.DisplayName ?? string.Empty;
                        OnNotificationReceived?.Invoke(appName);
                    }
                }
            }
            catch { }
        }
    }
}
