using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

/// <summary>Contrato neutro para novos frontends; não expõe HWND nem superfícies nativas.</summary>
public interface IDesktopWindowService : IDisposable
{
    DesktopWindowCapabilities Capacidades { get; }
    event Action? JanelasAlteradas;
    void Iniciar();
    void Parar();
    IReadOnlyList<DesktopWindow> ObterJanelasAbertas();
    bool Ativar(DesktopWindowId id);
    bool Minimizar(DesktopWindowId id);
    bool Fechar(DesktopWindowId id);
}
