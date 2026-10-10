using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DockWindows.Infrastructure.Windows
{
    public class ToastNotificationService : IDisposable, INotificationCenterSource
    {
        public event Action<string, bool, string>? OnNotificationReceived;
        public event Action? ChamadasEncerradas;
        private readonly HashSet<uint> _chamadas = new();
        private readonly HashSet<uint> _recebidas = new();
        private readonly object _notificationLock = new();
        private UserNotificationListener? _listener;
        private bool _disposed, _iniciando, _autorizado;
        private bool _escutando;
        public bool HasPackageIdentity { get; } = DetectarIdentidade();
        private static bool DetectarIdentidade()
        {
            try { return !string.IsNullOrEmpty(global::Windows.ApplicationModel.Package.Current.Id.Name); } catch { return false; }
        }
        public event Action? ContagensAlteradas;
        public event Action? PermissaoAlterada;
        public string EstadoPermissao { get; private set; } = "Não consultada";
        private void InformarPermissao(string estado) { EstadoPermissao = estado; if (!_disposed) PermissaoAlterada?.Invoke(); }
        public void Dispose() { _disposed = true; if (_listener != null && _escutando) { try { _listener.NotificationChanged -= Listener_NotificationChanged; } catch { } } OnNotificationReceived = null; ChamadasEncerradas = null; ContagensAlteradas = null; PermissaoAlterada = null; }


        public Task RequestAccessAsync() => Iniciar(true);

        public async Task Iniciar(bool solicitarAcesso = false)
        {
            if (_disposed || _iniciando || _autorizado) return;
            _iniciando = true;
            try
            {
                if (!HasPackageIdentity)
                {
                    InformarPermissao("Esta instalação precisa do pacote de identidade assinado para ler notificações.");
                    return;
                }
                _listener = UserNotificationListener.Current;
                var access = solicitarAcesso ? await _listener.RequestAccessAsync() : _listener.GetAccessStatus();
                
                InformarPermissao(access == UserNotificationListenerAccessStatus.Allowed ? "Permitida" : "Acesso negado pelo Windows");
                if (!_disposed && access == UserNotificationListenerAccessStatus.Allowed)
                {
                    _autorizado = true;
                    // Alguns desktops não fornecem o evento; a central também consulta sob demanda.
                    if (!_escutando)
                    {
                        try { _listener.NotificationChanged += Listener_NotificationChanged; _escutando = true; }
                        catch { }
                    }
                    ContagensAlteradas?.Invoke();
                }
            }
            catch { InformarPermissao("Indisponível no Windows"); }
            finally { _iniciando = false; }
        }

        private bool AcessoPermitido()
        {
            if (_disposed || _listener == null) return false;
            if (_listener.GetAccessStatus() == UserNotificationListenerAccessStatus.Allowed) return true;
            _autorizado = false;
            InformarPermissao("Acesso às notificações não permitido. Ative o acesso ou confira as permissões do Windows.");
            return false;
        }

        public async Task<IReadOnlyList<DockNotification>> ReadAsync()
        {
            if (!_autorizado) await Iniciar(); // Consulta o estado; nunca solicita acesso implicitamente.
            if (!AcessoPermitido()) return Array.Empty<DockNotification>();
            var notifications = await _listener!.GetNotificationsAsync(NotificationKinds.Toast);
            var items = new List<DockNotification>();
            foreach (var notification in notifications.OrderByDescending(n => n.CreationTime).Take(200))
            {
                var text = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric)?.GetTextElements();
                var title = text?.FirstOrDefault()?.Text ?? "Notificação";
                var body = text == null ? "" : string.Join(Environment.NewLine, text.Skip(1).Select(t => t.Text));
                items.Add(new(notification.Id, notification.AppInfo.DisplayInfo.DisplayName ?? "Aplicativo",
                    notification.AppInfo.AppUserModelId, notification.CreationTime,
                    title.Length > 256 ? title[..256] : title, body.Length > 2000 ? body[..2000] : body));
            }
            // Revogação durante a leitura também descarta o conteúdo.
            return AcessoPermitido() ? items : Array.Empty<DockNotification>();
        }

        public bool Remove(uint id)
        {
            if (!AcessoPermitido()) return false;
            _listener!.RemoveNotification(id);
            ContagensAlteradas?.Invoke();
            return true;
        }

        public bool ClearAll()
        {
            if (!AcessoPermitido()) return false;
            _listener!.ClearNotifications();
            ContagensAlteradas?.Invoke();
            return true;
        }

        public async Task<Dictionary<string, int>> ObterContagemNotificacoesPorAppAsync()
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!_autorizado || !AcessoPermitido()) return dict;

                var notifs = await _listener!.GetNotificationsAsync(NotificationKinds.Toast);
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
