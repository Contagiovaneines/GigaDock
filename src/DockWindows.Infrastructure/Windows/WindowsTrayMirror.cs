using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DockWindows.Infrastructure.Windows;

public sealed record TrayMirrorItem(string Name, BitmapSource? Image, bool CanInvoke);

/// <summary>Protótipo: lê somente controles da bandeja aberta, sem memória privada do Explorer.</summary>
public sealed class WindowsTrayMirror
{
    private delegate bool EnumCallback(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, IntPtr data);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, System.Text.StringBuilder name, int count);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr item);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sx, int sy, uint operation);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr item);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);

    private static AutomationElement? Popup()
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window)) return true;
            var name = new System.Text.StringBuilder(256);
            GetClassName(window, name, 256);
            if (name.ToString() is not ("TopLevelWindowForOverflowXamlIsland" or "NotifyIconOverflowWindow")) return true;
            found = window;
            return false;
        }, IntPtr.Zero);
        return found == IntPtr.Zero ? null : AutomationElement.FromHandle(found);
    }

    public IReadOnlyList<TrayMirrorItem> Read(CancellationToken token)
    {
        var popup = Popup();
        if (popup == null) return Array.Empty<TrayMirrorItem>();
        var bounds = popup.Current.BoundingRectangle;
        var items = new List<TrayMirrorItem>();
        var controls = popup.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        foreach (AutomationElement control in controls)
        {
            token.ThrowIfCancellationRequested();
            var info = control.Current;
            var rect = info.BoundingRectangle;
            if (info.IsOffscreen || string.IsNullOrWhiteSpace(info.Name) || rect.IsEmpty ||
                rect.Width < 8 || rect.Height < 8 || rect.Width > 128 || rect.Height > 128 || !bounds.Contains(rect)) continue;
            var width = Math.Min(32, rect.Width);
            var height = Math.Min(32, rect.Height);
            var image = Capture(new Rect(rect.X + (rect.Width - width) / 2,
                rect.Y + (rect.Height - height) / 2, width, height));
            items.Add(new(info.Name, image, info.IsEnabled && control.TryGetCurrentPattern(InvokePattern.Pattern, out _)));
        }
        return items;
    }

    public Task<bool> CloseAsync(CancellationToken token) =>
        new TrayMirrorCloseController(() => Popup() != null,
            cancellation => new WindowsTrayService().Abrir(cancellation)).CloseAsync(token);

    public bool Invoke(string name, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var popup = Popup();
        var control = popup?.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
            new PropertyCondition(AutomationElement.NameProperty, name)));
        if (control == null || !control.Current.IsEnabled || control.Current.IsOffscreen ||
            !control.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern)) return false;
        token.ThrowIfCancellationRequested();
        ((InvokePattern)pattern).Invoke();
        return true;
    }

    private static BitmapSource? Capture(Rect rect)
    {
        int width = (int)Math.Ceiling(rect.Width), height = (int)Math.Ceiling(rect.Height);
        var screen = GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero) return null;
        IntPtr dc = IntPtr.Zero, bitmap = IntPtr.Zero, previous = IntPtr.Zero;
        try
        {
            dc = CreateCompatibleDC(screen);
            bitmap = CreateCompatibleBitmap(screen, width, height);
            if (dc == IntPtr.Zero || bitmap == IntPtr.Zero) return null;
            previous = SelectObject(dc, bitmap);
            if (!BitBlt(dc, 0, 0, width, height, screen, (int)rect.X, (int)rect.Y, 0x00CC0020)) return null;
            var image = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return image;
        }
        finally
        {
            if (previous != IntPtr.Zero) SelectObject(dc, previous);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            if (dc != IntPtr.Zero) DeleteDC(dc);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }
}
