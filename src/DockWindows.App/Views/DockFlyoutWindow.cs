using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DockWindows.App.Views;

public class DockFlyoutWindow : Window
{
    public bool FecharAoClicarFora { get; set; } = true;
    protected readonly StackPanel Body = new();
    protected static Brush Azul { get; } = CriarBrush(20, 27, 35);
    protected static Brush Contorno { get; } = CriarBrush(54, 69, 83);
    private static Brush CriarBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
    protected Button CriarBotao(string texto)
    {
        return new Button { Content = texto, Style = (Style)Resources["FlyoutButton"], Cursor = Cursors.Hand };
    }
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
        Resources["FlyoutButton"] = (Style)System.Windows.Markup.XamlReader.Parse("""
            <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="Button">
                <Setter Property="Background" Value="#202D39"/><Setter Property="Foreground" Value="#E8EEF3"/>
                <Setter Property="BorderBrush" Value="#3A4C5D"/><Setter Property="BorderThickness" Value="1"/>
                <Setter Property="Padding" Value="12,8"/><Setter Property="FontSize" Value="12"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button">
                    <Border x:Name="Surface" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="9" Padding="{TemplateBinding Padding}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Surface" Property="Background" Value="#304457"/><Setter TargetName="Surface" Property="BorderBrush" Value="#6D91AD"/></Trigger>
                        <Trigger Property="IsPressed" Value="True"><Setter TargetName="Surface" Property="Background" Value="#3D586D"/></Trigger>
                        <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Surface" Property="BorderBrush" Value="#96D3FF"/></Trigger>
                        <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.4"/></Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate></Setter.Value></Setter>
            </Style>
            """);
        var fechar = CriarBotao("×");
        fechar.Width = 30; fechar.Height = 30; fechar.Padding = new Thickness(0); fechar.ToolTip = "Fechar";
        System.Windows.Automation.AutomationProperties.SetName(fechar, "Fechar painel");
        fechar.Click += (_, _) => Close();
        var cabecalho = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(fechar, Dock.Right);
        cabecalho.Children.Add(fechar);
        cabecalho.Children.Add(new TextBlock { Text = titulo, FontWeight = FontWeights.SemiBold, FontSize = 15, VerticalAlignment = VerticalAlignment.Center });
        Body.Children.Add(cabecalho);
        Content = new Border { Background = Azul, BorderThickness = new Thickness(1), BorderBrush = Contorno,
            CornerRadius = new CornerRadius(18), Padding = new Thickness(16), Child = Body };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } };
        Deactivated += (_, _) => { if (FecharAoClicarFora) Close(); };
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
