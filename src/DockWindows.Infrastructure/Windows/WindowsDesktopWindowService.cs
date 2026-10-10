using System.Globalization;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

/// <summary>Adaptador do rastreador Windows existente para o contrato neutro. Possui o serviço injetado.</summary>
public sealed class WindowsDesktopWindowService : IDesktopWindowService
{
    private readonly IWindowTrackingService _tracking;
    public DesktopWindowCapabilities Capacidades { get; } = new(true, true, true, true);

    public WindowsDesktopWindowService(IWindowTrackingService tracking)
    {
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
    }

    public event Action? JanelasAlteradas
    {
        add => _tracking.JanelasAlteradas += value;
        remove => _tracking.JanelasAlteradas -= value;
    }

    public void Iniciar() => _tracking.Iniciar();
    public void Parar() => _tracking.Parar();
    public void Dispose() => _tracking.Dispose();

    public IReadOnlyList<DesktopWindow> ObterJanelasAbertas() => _tracking.ObterJanelasAbertas()
        .Select(window => new DesktopWindow(
            new DesktopWindowId("win32", window.Hwnd.ToInt64().ToString(CultureInfo.InvariantCulture)),
            window.Titulo, window.CaminhoExecutavel, window.NomeProcesso, window.ProcessId,
            window.EstaAtiva, window.EstaMinimizada)).ToArray();

    public bool Ativar(DesktopWindowId id) => TryResolve(id, out var handle) && _tracking.AtivarJanela(handle);
    public bool Minimizar(DesktopWindowId id) => TryResolve(id, out var handle) && _tracking.MinimizarJanela(handle);
    public bool Fechar(DesktopWindowId id) => TryResolve(id, out var handle) && _tracking.FecharJanela(handle);

    private static bool TryResolve(DesktopWindowId id, out IntPtr handle)
    {
        handle = IntPtr.Zero;
        if (id.Backend != "win32" ||
            !long.TryParse(id.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
            value == 0 || (IntPtr.Size == 4 && (value < int.MinValue || value > int.MaxValue))) return false;
        handle = new IntPtr(value);
        return true;
    }
}
