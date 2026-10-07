using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DockWindows.Infrastructure.Windows;

public static class AppBarHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left, top, right, bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [DllImport("shell32.dll")]
    private static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int RegisterWindowMessage(string lpString);

    private const int ABM_NEW = 0;
    private const int ABM_REMOVE = 1;
    private const int ABM_QUERYPOS = 2;
    private const int ABM_SETPOS = 3;
    private const int ABE_BOTTOM = 3;

    private static int _uCallBackMsg;
    private static bool _isRegistered;

    public static void RegisterBar(Window window)
    {
        if (_isRegistered) return;
        var helper = new WindowInteropHelper(window);
        _uCallBackMsg = RegisterWindowMessage("AppBarMessage");

        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
            hWnd = helper.Handle,
            uCallbackMessage = _uCallBackMsg
        };
        SHAppBarMessage(ABM_NEW, ref data);
        _isRegistered = true;
    }

    public static void UpdatePos(Window window, double alturaReservada)
    {
        if (!_isRegistered) return;
        var helper = new WindowInteropHelper(window);
        
        var dpiScale = VisualTreeHelper.GetDpi(window);
        if (!double.IsFinite(alturaReservada) || alturaReservada <= 0) alturaReservada = 72;

        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
            hWnd = helper.Handle,
            uEdge = ABE_BOTTOM
        };

        int screenWidth = (int)Math.Round(SystemParameters.PrimaryScreenWidth * dpiScale.DpiScaleX);
        int screenHeight = (int)Math.Round(SystemParameters.PrimaryScreenHeight * dpiScale.DpiScaleY);
        int barHeight = Math.Max(1, (int)Math.Round(alturaReservada * dpiScale.DpiScaleY));

        data.rc.left = 0;
        data.rc.right = screenWidth;
        data.rc.top = screenHeight - barHeight;
        data.rc.bottom = screenHeight;

        SHAppBarMessage(ABM_QUERYPOS, ref data);
        // O Shell pode ajustar o retângulo consultado. Para uma AppBar inferior,
        // recalcule o topo preservando somente a altura realmente reservada.
        data.rc.top = data.rc.bottom - barHeight;
        SHAppBarMessage(ABM_SETPOS, ref data);
    }

    public static void RemoveBar(Window window)
    {
        if (!_isRegistered) return;
        var helper = new WindowInteropHelper(window);
        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
            hWnd = helper.Handle
        };
        SHAppBarMessage(ABM_REMOVE, ref data);
        _isRegistered = false;
    }
}
