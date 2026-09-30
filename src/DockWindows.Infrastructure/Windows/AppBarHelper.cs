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

    public static void UpdatePos(Window window)
    {
        if (!_isRegistered) return;
        var helper = new WindowInteropHelper(window);
        
        var dpiScale = VisualTreeHelper.GetDpi(window);
        double height = window.ActualHeight;
        if (height == 0 && double.IsNaN(window.Height) == false) height = window.Height;
        if (height <= 0) height = 80; // Safe default

        var data = new APPBARDATA
        {
            cbSize = Marshal.SizeOf(typeof(APPBARDATA)),
            hWnd = helper.Handle,
            uEdge = ABE_BOTTOM
        };

        int screenWidth = (int)Math.Round(SystemParameters.PrimaryScreenWidth * dpiScale.DpiScaleX);
        int screenHeight = (int)Math.Round(SystemParameters.PrimaryScreenHeight * dpiScale.DpiScaleY);
        int barHeight = (int)Math.Round(height * dpiScale.DpiScaleY);

        data.rc.left = 0;
        data.rc.right = screenWidth;
        data.rc.top = screenHeight - barHeight;
        data.rc.bottom = screenHeight;

        SHAppBarMessage(ABM_QUERYPOS, ref data);
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
