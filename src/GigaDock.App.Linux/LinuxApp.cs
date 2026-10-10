using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace GigaDock.App.Linux;

public sealed class LinuxApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Visuals.InstallStyles(this);
        RequestedThemeVariant = ThemeVariant.Dark;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow(Program.Session);
            desktop.MainWindow = window;
            if (Program.Smoke)
                window.Opened += async (_, _) =>
                {
                    try { await SmokeScenario.RunAsync(window); desktop.Shutdown(); }
                    catch (Exception error) { Console.Error.WriteLine(error); desktop.Shutdown(1); }
                };
        }
        base.OnFrameworkInitializationCompleted();
    }

    internal static void Capture(Control window, string name)
    {
        var directory = Path.Combine(Program.Session.Directories.Estado, "previews");
        Directory.CreateDirectory(directory);
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.Bounds.Width),
            (int)Math.Ceiling(window.Bounds.Height)), new Vector(96, 96));
        bitmap.Render(window);
        bitmap.Save(Path.Combine(directory, name + ".png"), PngBitmapEncoderOptions.Default);
        Console.WriteLine($"CAPTURE_OK: {name} {window.Bounds.Size}");
    }
}
