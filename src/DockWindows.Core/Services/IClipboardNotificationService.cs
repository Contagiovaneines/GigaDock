namespace DockWindows.Core.Services;

public interface IClipboardNotificationService : IDisposable
{
    event Action? ClipboardAlterada;
    bool EstaAtivo { get; }
    bool Iniciar();
    void Parar();
}
