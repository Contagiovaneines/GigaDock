using System.Runtime.InteropServices;
using System.Windows.Interop;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public sealed class ClipboardNotificationService : IClipboardNotificationService
{
    private const int WmClipboardUpdate = 0x031D;
    private static readonly IntPtr HwndMessage = new(-3);
    private HwndSource? _source;

    public event Action? ClipboardAlterada;
    public bool EstaAtivo => _source != null;

    public bool Iniciar()
    {
        if (_source != null) return true;
        var parametros = new HwndSourceParameters("GigaDock.ClipboardListener")
        {
            ParentWindow = HwndMessage,
            WindowStyle = 0,
            Width = 0,
            Height = 0
        };
        _source = new HwndSource(parametros);
        _source.AddHook(ProcessarMensagem);
        if (AddClipboardFormatListener(_source.Handle)) return true;
        _source.RemoveHook(ProcessarMensagem);
        _source.Dispose();
        _source = null;
        return false;
    }

    public void Parar()
    {
        if (_source == null) return;
        RemoveClipboardFormatListener(_source.Handle);
        _source.RemoveHook(ProcessarMensagem);
        _source.Dispose();
        _source = null;
    }

    private IntPtr ProcessarMensagem(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmClipboardUpdate) ClipboardAlterada?.Invoke();
        return IntPtr.Zero;
    }

    public void Dispose() => Parar();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
}
