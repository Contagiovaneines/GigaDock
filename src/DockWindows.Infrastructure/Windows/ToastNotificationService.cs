using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DockWindows.Infrastructure.Windows
{
    public class ToastNotificationService : IDisposable
    {
        public event Action<string, bool, string>? OnNotificationReceived;
        public event Action? ChamadasEncerradas;
        private readonly HashSet<uint> _chamadas = new();
        private readonly HashSet<uint> _recebidas = new();
        private readonly object _notificationLock = new();
        private UserNotificationListener? _listener;
        private bool _disposed, _iniciando, _autorizado;
        public event Action? ContagensAlteradas;
        public event Action? PermissaoAlterada;
        public string EstadoPermissao { get; private set; } = "Não consultada";
        private void InformarPermissao(string estado) { EstadoPermissao = estado; if (!_disposed) PermissaoAlterada?.Invoke(); }
        public void Dispose() { _disposed = true; if (_listener != null) _listener.NotificationChanged -= Listener_NotificationChanged; OnNotificationReceived = null; ChamadasEncerradas = null; ContagensAlteradas = null; PermissaoAlterada = null; }


        public async Task Iniciar()
        {
            if (_disposed || _iniciando || _autorizado) return;
            _iniciando = true;
            try
            {
                _listener = UserNotificationListener.Current;
                var access = await _listener.RequestAccessAsync();
                
                InformarPermissao(access == UserNotificationListenerAccessStatus.Allowed ? "Permitida" : "Acesso negado pelo Windows");
                if (!_disposed && access == UserNotificationListenerAccessStatus.Allowed)
                {
                    _autorizado = true;
                    _listener.NotificationChanged += Listener_NotificationChanged;
                    ContagensAlteradas?.Invoke();
                }
            }
            catch { InformarPermissao("Indisponível no Windows"); }
            finally { _iniciando = false; }
        }

        public async Task<Dictionary<string, int>> ObterContagemNotificacoesPorAppAsync()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (_listener == null || !_autorizado || _disposed) return dict;

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

        private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
        {
            try
            {
                if (_disposed) return;
                ContagensAlteradas?.Invoke();
                if (args.ChangeKind == UserNotificationChangedKind.Removed)
                {
                    bool encerrada;
                    lock (_notificationLock) { _recebidas.Remove(args.UserNotificationId); encerrada = _chamadas.Remove(args.UserNotificationId) && _chamadas.Count == 0; }
                    if (encerrada) ChamadasEncerradas?.Invoke();
                }
                if (args.ChangeKind == UserNotificationChangedKind.Added)
                {
                    var nova = sender.GetNotification(args.UserNotificationId);
                    if (!_disposed && nova != null)
                    {
                        string appName = nova.AppInfo.DisplayInfo.DisplayName ?? string.Empty;
                        
                        bool isCall = false;
                        string senderName = string.Empty;
                        try 
                        {
                            var textNodes = nova.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric)?.GetTextElements();
                            if (textNodes != null && textNodes.Count > 0)
                            {
                                senderName = textNodes[0].Text; // Geralmente o primeiro texto é o nome do remetente
                                string texto = string.Join(" ", textNodes.Select(n => n.Text)).ToLowerInvariant();
                                isCall = (texto.Contains("chamada recebida") || texto.Contains("chamada de voz") ||
                                    texto.Contains("chamada de vídeo") || texto.Contains("ligando") ||
                                    texto.Contains("incoming call") || texto.Contains("is calling") ||
                                    texto.Contains("voice call") || texto.Contains("video call")) &&
                                    !texto.Contains("perdida") && !texto.Contains("missed") &&
                                    !texto.Contains("encerrada") && !texto.Contains("ended");
                            }
                        } catch { }

                        lock (_notificationLock)
                        {
                            if (_recebidas.Count > 4096) _recebidas.Clear();
                            if (!_recebidas.Add(args.UserNotificationId)) return;
                            if (isCall) _chamadas.Add(args.UserNotificationId);
                        }
                        OnNotificationReceived?.Invoke(appName, isCall, senderName);
                    }
                }
            }
            catch { }
        }
    }
}