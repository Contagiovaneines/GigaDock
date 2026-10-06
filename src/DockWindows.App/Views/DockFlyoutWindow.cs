using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DockWindows.App.Views;

public class DockFlyoutWindow : Window
{
    protected readonly StackPanel Body = new();
    protected static Brush Azul => new SolidColorBrush(Color.FromRgb(22, 44, 103));
    public DockFlyoutWindow(string titulo)
    {
        Title = titulo;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Background = Azul;
        Foreground = Brushes.White;
        var fechar = new Button { Content = "×", Width = 28, Height = 28, ToolTip = "Fechar", Background = Azul, Foreground = Brushes.White };
        fechar.Click += (_, _) => Close();
        var cabecalho = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(fechar, Dock.Right);
        cabecalho.Children.Add(fechar);
        cabecalho.Children.Add(new TextBlock { Text = titulo, FontWeight = FontWeights.SemiBold, FontSize = 15, VerticalAlignment = VerticalAlignment.Center });
        Body.Children.Add(cabecalho);
        Content = new Border { BorderThickness = new Thickness(1), BorderBrush = Brushes.SlateBlue,
            CornerRadius = new CornerRadius(18), Padding = new Thickness(14), Child = Body };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
        Deactivated += (_, _) => Close();
    }

    public void MostrarPerto(FrameworkElement anchor)
    {
        Owner = GetWindow(anchor);
        var screen = anchor.PointToScreen(new Point(anchor.ActualWidth / 2, 0));
        var dpi = VisualTreeHelper.GetDpi(anchor);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        GetMonitorInfo(MonitorFromPoint(new NativePoint((int)screen.X, (int)screen.Y), 2), ref info);
        var work = new Rect(info.Work.Left / dpi.DpiScaleX, info.Work.Top / dpi.DpiScaleY,
            (info.Work.Right - info.Work.Left) / dpi.DpiScaleX, (info.Work.Bottom - info.Work.Top) / dpi.DpiScaleY);
        MaxWidth = Math.Max(240, work.Width - 20);
        MaxHeight = Math.Max(160, work.Height - 20);
        Left = work.Left + 10;
        Top = work.Top + 10;
        Show();
        UpdateLayout();
        Left = Math.Clamp(screen.X / dpi.DpiScaleX - ActualWidth / 2, work.Left + 10, Math.Max(work.Left + 10, work.Right - ActualWidth - 10));
        Top = Math.Clamp(screen.Y / dpi.DpiScaleY - ActualHeight - 10, work.Top + 10, Math.Max(work.Top + 10, work.Bottom - ActualHeight - 10));
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; public NativePoint(int x, int y) { X = x; Y = y; } }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern nint MonitorFromPoint(NativePoint point, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
}