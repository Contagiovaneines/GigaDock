using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DockWindows.App.Controls;
using DockWindows.App.Views;

internal class Program
{
    [STAThread]
    private static int Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var window = new Window { Width = 720, Height = 650, ShowInTaskbar = false, WindowStyle = WindowStyle.None,
            Left = -10000, Top = -10000, Background = new SolidColorBrush(Color.FromRgb(22, 27, 36)) };
        var settings = new VisualizacoesSettings { Width = 1000, Height = 900, LayoutTransform = new ScaleTransform(.7, .7), DataContext = new ExampleOptions() };
        var host = new Border { Background = window.Background, Child = settings };
        window.Content = host;
        window.Show();
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var bitmap = new RenderTargetBitmap((int)(host.ActualWidth * 2), (int)(host.ActualHeight * 2), 192, 192, PixelFormats.Pbgra32);
        bitmap.Render(host);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = System.IO.File.Create("docs/visualizacoes-preview.png")) encoder.Save(stream);
        Console.WriteLine("Prévia dos quatro cartões renderizada em WPF.");
        window.Close();

        var source = new Window { Width = 300, Height = 200, ShowInTaskbar = false, Left = -10000, Top = -10000,
            Content = new TextBlock { Text = "Janela de teste da dock", FontSize = 20 } };
        source.Show();
        var preview = new DwmPreviewControl { Width = 200, Height = 130, SourceHwnd = new WindowInteropHelper(source).Handle };
        var destination = new Window { Width = 240, Height = 180, ShowInTaskbar = false, Left = -10000, Top = -10000, Content = preview };
        destination.Show();
        destination.UpdateLayout();
        destination.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var thumbnail = (nint)typeof(DwmPreviewControl).GetField("_thumbnail", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(preview)!;
        Console.WriteLine(thumbnail != 0 ? "DWM: miniatura registrada para janela de teste." : "DWM: registro indisponível nesta sessão.");
        destination.Close();
        source.Close();
        preview.Dispose();
        app.Shutdown();
        return thumbnail != 0 ? 0 : 2;
    }

    private class ExampleOptions
    {
        public bool PreviaJanelas { get; set; }
        public bool ClimaExpandido { get; set; }
        public bool RelogioAnalogico { get; set; }
        public bool PreviaPastas { get; set; }
    }
}