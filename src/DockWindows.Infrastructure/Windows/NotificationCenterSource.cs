namespace DockWindows.Infrastructure.Windows;

public sealed record DockNotification(uint Id, string Application, string ApplicationId,
    DateTimeOffset CreatedAt, string Title, string Body);

public interface INotificationCenterSource
{
    bool HasPackageIdentity { get; }
    string EstadoPermissao { get; }
    Task RequestAccessAsync();
    Task<IReadOnlyList<DockNotification>> ReadAsync();
    bool Remove(uint id);
    bool ClearAll();
}
