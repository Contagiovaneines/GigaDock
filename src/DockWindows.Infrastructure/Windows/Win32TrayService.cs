using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DockWindows.Infrastructure.Windows;

public class Win32TrayService : IDisposable
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYCALLBACK = WM_USER + 100;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_LBUTTONUP = 0x0202;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public uint uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    private readonly IntPtr _hwnd;
    private readonly HwndSource? _source;
    private bool _adicionado;
    private bool _disposed;

    public event Action? DuploClique;
    public event Action? CliqueDireito;

    public Win32TrayService(IntPtr hwnd, string dica = "GigaDock")
    {
        _hwnd = hwnd;
        if (_hwnd != IntPtr.Zero)
        {
            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(WndProc);

            AdicionarIconeBandeja(dica);
        }
    }

    private void AdicionarIconeBandeja(string dica)
    {
        try
        {
            // IDI_APPLICATION = 32512
            var hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512));

            var data = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1001,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYCALLBACK,
                hIcon = hIcon,
                szTip = dica
            };

            _adicionado = Shell_NotifyIcon(NIM_ADD, ref data);
        }
        catch { }
    }

    public void RemoverIconeBandeja()
    {
        if (_adicionado && _hwnd != IntPtr.Zero)
        {
            try
            {
                var data = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                    hWnd = _hwnd,
                    uID = 1001
                };
                Shell_NotifyIcon(NIM_DELETE, ref data);
                _adicionado = false;
            }
            catch { }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYCALLBACK)
        {
            int evento = lParam.ToInt32();
            if (evento == WM_LBUTTONDBLCLK)
            {
                DuploClique?.Invoke();
                handled = true;
            }
            else if (evento == WM_RBUTTONUP)
            {
                CliqueDireito?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            RemoverIconeBandeja();
            _source?.RemoveHook(WndProc);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

