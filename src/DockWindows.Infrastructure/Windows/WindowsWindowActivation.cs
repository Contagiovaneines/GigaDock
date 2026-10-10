using System.Runtime.InteropServices;

namespace DockWindows.Infrastructure.Windows;

public interface IWindowActivationApi
{
    bool Exists(nint window);
    bool Visible(nint window);
    bool Enabled(nint window);
    bool Minimized(nint window);
    bool Cloaked(nint window);
    uint ProcessId(nint window);
    nint LastActivePopup(nint window);
    bool RestoreAsync(nint window);
    bool Foreground(nint window);
}

/// <summary>Ativa somente janelas já expostas pelo aplicativo, preservando seu estado interno.</summary>
public sealed class WindowsWindowActivation(IWindowActivationApi? api = null)
{
    private readonly IWindowActivationApi _api = api ?? new NativeApi();

    public bool Activate(nint window)
    {
        if (window == 0 || !_api.Exists(window) || !_api.Visible(window) || _api.Cloaked(window)) return false;
        var process = _api.ProcessId(window);
        if (process == 0) return false;
        // Uma janela principal desabilitada costuma ter um diálogo modal. Nunca
        // reabilitar ou revelar essa janela: levar o diálogo ativo para frente.
        var visited = new HashSet<nint>();
        var candidate = window;
        for (var depth = 0; depth < 8 && visited.Add(candidate); depth++)
        {
            var popup = _api.LastActivePopup(candidate);
            if (popup == 0 || popup == candidate || !_api.Exists(popup) ||
                !_api.Visible(popup) || _api.Cloaked(popup) || _api.ProcessId(popup) != process) break;
            candidate = popup;
        }
        if (!_api.Exists(candidate) || !_api.Visible(candidate) || !_api.Enabled(candidate) || _api.Cloaked(candidate)) return false;
        // Não usar SW_SHOW em janelas ocultas na bandeja: apenas o próprio app
        // conhece o procedimento correto para reabrir sua interface.
        if (_api.Minimized(candidate) && !_api.RestoreAsync(candidate)) return false;
        return _api.Foreground(candidate);
    }

    private sealed class NativeApi : IWindowActivationApi
    {
        public bool Exists(nint window) => IsWindow(window);
        public bool Visible(nint window) => IsWindowVisible(window);
        public bool Enabled(nint window) => IsWindowEnabled(window);
        public bool Minimized(nint window) => IsIconic(window);
        public bool Cloaked(nint window) => DwmGetWindowAttribute(window, 14, out int value, sizeof(int)) == 0 && value != 0;
        public uint ProcessId(nint window) { GetWindowThreadProcessId(window, out uint process); return process; }
        public nint LastActivePopup(nint window) => GetLastActivePopup(window);
        public bool RestoreAsync(nint window) => ShowWindowAsync(window, 9);
        public bool Foreground(nint window) => SetForegroundWindow(window);

        [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
        [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint window);
        [DllImport("user32.dll")] private static extern bool IsIconic(nint window);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
        [DllImport("user32.dll")] private static extern nint GetLastActivePopup(nint window);
        [DllImport("user32.dll")] private static extern bool ShowWindowAsync(nint window, int command);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
    }
}
