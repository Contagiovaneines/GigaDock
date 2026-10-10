using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

#if GIGADOCK_INSTALLER
namespace DockWindows.Installer.Views;
#else
namespace DockWindows.App.Views;
#endif

public static class WindowScreenSizing
{
    public static void Attach(Window window)
    {
        HwndSource? source = null;
        void Fit(bool center)
        {
            if (window.WindowState != WindowState.Normal) return;
            var area = GetWorkArea(center && window.Owner is not null ? window.Owner : window);
            var margin = Math.Min(16, Math.Min(area.Width, area.Height) / 20);
            window.MinWidth = Math.Min(window.MinWidth, Math.Max(1, area.Width - margin * 2));
            window.MinHeight = Math.Min(window.MinHeight, Math.Max(1, area.Height - margin * 2));
            window.Width = Math.Min(window.Width, Math.Max(1, area.Width - margin * 2));
            window.Height = Math.Min(window.Height, Math.Max(1, area.Height - margin * 2));
            window.Left = center ? area.Left + (area.Width - window.Width) / 2
                : Math.Clamp(window.Left, area.Left, Math.Max(area.Left, area.Right - window.Width));
            window.Top = center ? area.Top + (area.Height - window.Height) / 2
                : Math.Clamp(window.Top, area.Top, Math.Max(area.Top, area.Bottom - window.Height));
        }
        IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == 0x0024 && TryMonitor(hwnd, out var monitor))
            {
                var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                info.MaxPosition = new PointNative { X = monitor.Work.Left - monitor.Monitor.Left, Y = monitor.Work.Top - monitor.Monitor.Top };
                info.MaxSize = new PointNative { X = monitor.Work.Right - monitor.Work.Left, Y = monitor.Work.Bottom - monitor.Work.Top };
                var scale = System.Windows.Media.VisualTreeHelper.GetDpi(window);
                info.MinTrackSize = new PointNative { X = (int)Math.Ceiling(window.MinWidth * scale.DpiScaleX), Y = (int)Math.Ceiling(window.MinHeight * scale.DpiScaleY) };
                Marshal.StructureToPtr(info, lParam, false);
                handled = true;
            }
            if (message is 0x02E0 or 0x007E)
                window.Dispatcher.BeginInvoke(new Action(() => Fit(false)));
            return IntPtr.Zero;
        }
        window.SourceInitialized += (_, _) =>
        {
            source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source?.AddHook(Hook);
        };
        window.Loaded += (_, _) => Fit(true);
        window.Closed += (_, _) => source?.RemoveHook(Hook);
    }

    public static Rect GetWorkArea(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !TryMonitor(handle, out var monitor)) return SystemParameters.WorkArea;
        var source = HwndSource.FromHwnd(handle);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
        var topLeft = transform.Transform(new Point(monitor.Work.Left, monitor.Work.Top));
        var bottomRight = transform.Transform(new Point(monitor.Work.Right, monitor.Work.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private static bool TryMonitor(IntPtr handle, out MonitorInfo info)
    {
        info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        var monitor = MonitorFromWindow(handle, 2);
        return monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info);
    }
    [StructLayout(LayoutKind.Sequential)] private struct PointNative { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct RectNative { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public RectNative Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo { public PointNative Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
