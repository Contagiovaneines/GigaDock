using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace DockWindows.Infrastructure.Windows;

/// <summary>Invoca o controle acessível da bandeja; não enumera processos nem injeta mensagens privadas.</summary>
public interface IWindowsTrayService
{
    bool Abrir(CancellationToken cancellationToken = default);
}

public sealed class WindowsTrayService : IWindowsTrayService
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string? title);
    public bool Abrir(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var handle = FindWindow("Shell_TrayWnd", null);
            if (handle == IntPtr.Zero) return false;
            // Parte do HWND real; barras ocultas não aparecem na ControlView do desktop.
            var taskbar = AutomationElement.FromHandle(handle);
            var buttons = taskbar.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            foreach (AutomationElement button in buttons)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var info = button.Current;
                if (!IsOverflowButton(info.Name, info.AutomationId, info.ClassName) || !info.IsEnabled || info.IsOffscreen ||
                    !button.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern)) continue;
                cancellationToken.ThrowIfCancellationRequested();
                ((InvokePattern)pattern).Invoke();
                return true;
            }
            return false;
        }
        catch (Exception error) when (error is ElementNotAvailableException or InvalidOperationException or
            System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        {
            return false;
        }
    }
    public static bool IsOverflowButton(string? name, string? automationId, string? className) =>
        automationId == "SystemTray.OverflowButton" || className == "SystemTray.OverflowButton" ||
        new[] { "Mostrar ícones ocultos", "Menu de ícones ocultos", "Show hidden icons", "Hidden icons" }
            .Any(prefix => name?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
}
