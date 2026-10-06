using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DockWindows.App.Controls;

public sealed class DwmPreviewControl : FrameworkElement, IDisposable
{
    public nint SourceHwnd { get; set; }
    private nint _thumbnail;
    private Rect _lastRect = Rect.Empty;
    public DwmPreviewControl()
    {
        Loaded += (_, _) => Atualizar();
        IsVisibleChanged += (_, _) => { if (IsVisible) Atualizar(); else Dispose(); };
        LayoutUpdated += (_, _) => Atualizar();
        Unloaded += (_, _) => Dispose();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(14, 27, 63)), null, new Rect(RenderSize));
        if (_thumbnail == 0)
            drawingContext.DrawText(new FormattedText("Prévia indisponível\nClique para abrir a janela.",
                CultureInfo.GetCultureInfo("pt-BR"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12,
                Brushes.LightGray, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(12, 15));
    }

    private void Atualizar()
    {
        if (!IsVisible || !IsLoaded || ActualWidth <= 0 || ActualHeight <= 0 || Window.GetWindow(this) is not Window window) return;
        if (_thumbnail == 0)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == 0 || SourceHwnd == 0 || DwmRegisterThumbnail(hwnd, SourceHwnd, out _thumbnail) != 0) return;
            InvalidateVisual();
        }
        var ponto = TransformToAncestor(window).Transform(new Point());
        var dpi = VisualTreeHelper.GetDpi(window);
        double width = ActualWidth, height = ActualHeight;
        if (DwmQueryThumbnailSourceSize(_thumbnail, out var size) == 0 && size.Width > 0 && size.Height > 0)
        {
            var scale = Math.Min(width / size.Width, height / size.Height);
            ponto.Offset((width - size.Width * scale) / 2, (height - size.Height * scale) / 2);
            width = size.Width * scale; height = size.Height * scale;
        }
        var rect = new Rect(ponto.X * dpi.DpiScaleX, ponto.Y * dpi.DpiScaleY, width * dpi.DpiScaleX, height * dpi.DpiScaleY);
        if (rect == _lastRect) return;
        var props = new ThumbnailProperties { Flags = 1 | 4 | 8 | 16,
            Destination = new NativeRect { Left = (int)rect.Left, Top = (int)rect.Top, Right = (int)rect.Right, Bottom = (int)rect.Bottom },
            Opacity = 255, Visible = true, ClientOnly = false };
        if (DwmUpdateThumbnailProperties(_thumbnail, ref props) != 0) { Dispose(); return; }
        _lastRect = rect;
    }

    public void Dispose()
    {
        if (_thumbnail != 0) DwmUnregisterThumbnail(_thumbnail);
        _thumbnail = 0; _lastRect = Rect.Empty;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width, Height; }
    [StructLayout(LayoutKind.Sequential)] private struct ThumbnailProperties
    {
        public uint Flags; public NativeRect Destination, Source; public byte Opacity;
        [MarshalAs(UnmanagedType.Bool)] public bool Visible;
        [MarshalAs(UnmanagedType.Bool)] public bool ClientOnly;
    }
    [DllImport("dwmapi.dll")] private static extern int DwmRegisterThumbnail(nint destination, nint source, out nint thumbnail);
    [DllImport("dwmapi.dll")] private static extern int DwmUpdateThumbnailProperties(nint thumbnail, ref ThumbnailProperties properties);
    [DllImport("dwmapi.dll")] private static extern int DwmUnregisterThumbnail(nint thumbnail);
    [DllImport("dwmapi.dll")] private static extern int DwmQueryThumbnailSourceSize(nint thumbnail, out NativeSize size);
}
