path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Infrastructure\Windows\ToastNotificationService.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

good = """using System;
using System.Linq;
using System.Collections.Generic;
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

        public async Task<Dictionary<string, int>> ObterContagemNotificacoesPorAppAsync()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (_listener == null) return dict;
                var access = await _listener.RequestAccessAsync();
                if (access != UserNotificationListenerAccessStatus.Allowed) return dict;

                var notifs = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
                foreach (var n in notifs)
                {
                    string appName = n.AppInfo.DisplayInfo.DisplayName ?? string.Empty;
                    if (!string.IsNullOrEmpty(appName))
                    {
                        if (dict.ContainsKey(appName))
                            dict[appName]++;
                        else
                            dict[appName] = 1;
                    }
                }
            }
            catch { }
            return dict;
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
}"""

with open(path, "w", encoding="utf-8") as f:
    f.write(good)
