using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace GigaDock.Infrastructure.Linux;

/// <summary>Fallback explícito enquanto o backend de janelas Linux ainda não foi implementado.</summary>
public sealed class UnsupportedDesktopWindowService : IDesktopWindowService
{
    public DesktopWindowCapabilities Capacidades { get; } = new(false, false, false, false);
    public event Action? JanelasAlteradas { add { } remove { } }
    public void Iniciar() { }
    public void Parar() { }
    public void Dispose() { }
    public IReadOnlyList<DesktopWindow> ObterJanelasAbertas() => [];
    public bool Ativar(DesktopWindowId id) => false;
    public bool Minimizar(DesktopWindowId id) => false;
    public bool Fechar(DesktopWindowId id) => false;
}
