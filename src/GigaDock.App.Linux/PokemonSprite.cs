using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using DockWindows.Core.Models;
using DockWindows.Core.Widgets;
using DockWindows.App.Common;

namespace GigaDock.App.Linux;

internal sealed class PokemonSprite : Control, IDisposable
{
    private sealed record Sheet(Bitmap Bitmap, int Width, int Height, int[] Delays, Rect Crop) : IDisposable
    {
        public int Duration { get; } = Delays.Sum();
        public void Dispose() => Bitmap.Dispose();
    }
    private readonly int _id;
    private readonly bool _reduceMotion, _roam;
    private readonly Dictionary<string, Sheet> _sheets = new();
    private readonly PokemonDockMotion _motion = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly System.Diagnostics.Stopwatch _tickClock = new(), _age = new();
    private double _animationTime;
    private bool _disposed;
    internal bool HasSprite => _sheets.ContainsKey("Move");
    internal double WalkPosition => _motion.Position;
    internal bool IsWalking => _timer.IsEnabled && _roam;
    internal static int LiveSheets { get; private set; }

    public PokemonSprite(WidgetInstanceConfig widget, bool reduceMotion, bool roam = false)
    {
        _reduceMotion = reduceMotion; _roam = roam; IsHitTestVisible = !roam;
        _id = int.TryParse(widget.ObterConfiguracao("PokemonId", "1"), out var chosen) ? Math.Clamp(chosen, 1, 151) : 1;
        Load("Move", PokemonLocomotion.AnimationFor(_id)); _age.Start();
        _timer.Tick += Tick;
        AttachedToVisualTree += (_, _) =>
        {
            if (_disposed) return;
            Load("Move", PokemonLocomotion.AnimationFor(_id)); _tickClock.Restart();
            if (!_reduceMotion && HasSprite) _timer.Start();
        };
        DetachedFromVisualTree += (_, _) => Release();
    }
    private void Load(string key, string requested)
    {
        if (_disposed || _sheets.ContainsKey(key)) return;
        Bitmap? bitmap = null;
        try
        {
            var prefix = $"avares://GigaDock/Assets/PokemonWalk/{_id:0000}/";
            using var xml = AssetLoader.Open(new Uri(prefix + "AnimData.xml"));
            using var reader = XmlReader.Create(xml, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var animations = XDocument.Load(reader).Descendants("Anim").ToArray();
            var animation = animations.FirstOrDefault(a => (string?)a.Element("Name") == requested);
            if (animation is null) return;
            var visited = new HashSet<string>();
            while ((string?)animation.Element("CopyOf") is { } copy)
            {
                if (!visited.Add(copy)) return;
                animation = animations.FirstOrDefault(a => (string?)a.Element("Name") == copy);
                if (animation is null) return;
            }
            var name = (string)animation.Element("Name")!;
            var width = (int)animation.Element("FrameWidth")!; var height = (int)animation.Element("FrameHeight")!;
            var delays = animation.Descendants("Duration").Select(v => Math.Max(1, (int)v) * 1000 / 60).ToArray();
            using var png = AssetLoader.Open(new Uri(prefix + name + "-Anim.png")); bitmap = new Bitmap(png);
            if (width <= 0 || height <= 0 || delays.Length == 0 || bitmap.PixelSize.Width != width * delays.Length ||
                (bitmap.PixelSize.Height != height && bitmap.PixelSize.Height != height * 8)) return;
            _sheets[key] = new Sheet(bitmap, width, height, delays, VisibleBounds(bitmap, width, height, delays.Length));
            LiveSheets++; bitmap = null;
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or XmlException or
            ArgumentException or FormatException or NotSupportedException) { }
        finally { bitmap?.Dispose(); }
    }
    private static Rect VisibleBounds(Bitmap bitmap, int width, int height, int frames)
    {
        // Recorte comum às direções; buffer temporário liberado após carregar.
        if ((long)bitmap.PixelSize.Width * bitmap.PixelSize.Height > 4 * 1024 * 1024) return new Rect(0, 0, width, height);
        using var pixels = new WriteableBitmap(bitmap.PixelSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using var buffer = pixels.Lock(); bitmap.CopyPixels(buffer);
        var data = new byte[buffer.RowBytes * bitmap.PixelSize.Height]; Marshal.Copy(buffer.Address, data, 0, data.Length);
        var x0 = width; var y0 = height; var x1 = 0; var y1 = 0;
        foreach (var row in bitmap.PixelSize.Height == height ? new[] { 0 } : new[] { 0, 2, 6 })
            for (var frame = 0; frame < frames; frame++)
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                        if (data[(row * height + y) * buffer.RowBytes + (frame * width + x) * 4 + 3] != 0)
                        { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x + 1); y1 = Math.Max(y1, y + 1); }
        return x1 > x0 && y1 > y0 ? new Rect(x0, y0, x1 - x0, y1 - y0) : new Rect(0, 0, width, height);
    }
    private void Tick(object? sender, EventArgs args)
    {
        var elapsed = Math.Min(100, _tickClock.Elapsed.TotalMilliseconds); _tickClock.Restart(); _animationTime += elapsed;
        if (_roam && _sheets.TryGetValue("Move", out var moving))
            _motion.Advance(Bounds.Width, moving.Crop.Width * 44 / moving.Crop.Height, elapsed, State != "Dormindo");
        InvalidateVisual();
    }
    // A sessão Linux pode não disponibilizar inatividade global; não presume esse dado.
    private string State => PokemonBehavior.StateFor(_id, _age.Elapsed, TimeSpan.Zero);
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!_sheets.TryGetValue("Move", out var moving)) return;
        var sleeping = State == "Dormindo"; var sheet = moving;
        if (sleeping) { Load("Sleep", "Sleep"); sheet = _sheets.GetValueOrDefault("Sleep") ?? moving; }
        var factor = _roam ? 44 / moving.Crop.Height : Math.Min(Bounds.Width / moving.Crop.Width, Bounds.Height / moving.Crop.Height);
        var bodyWidth = sheet.Crop.Width * factor; var bodyHeight = sheet.Crop.Height * factor;
        if (_roam) _motion.Advance(Bounds.Width, bodyWidth, 0, false);
        var time = _reduceMotion ? 0 : PokemonLocomotion.KindFor(_id) != PokemonMovementKind.Ground || !_roam || sleeping
            ? _animationTime : _motion.Distance % 16 / 16 * sheet.Duration;
        var remaining = time % sheet.Duration; var frame = 0;
        while (frame < sheet.Delays.Length - 1 && remaining >= sheet.Delays[frame]) { remaining -= sheet.Delays[frame]; frame++; }
        var row = sleeping || _reduceMotion || _motion.FacingFront ? 0 : _motion.FacingRight ? 2 : 6;
        if (sheet.Bitmap.PixelSize.Height == sheet.Height) row = 0;
        var altitude = _roam ? PokemonLocomotion.Altitude(PokemonLocomotion.KindFor(_id), _animationTime, sleeping, !_reduceMotion) : 0;
        context.DrawImage(sheet.Bitmap,
            new Rect(frame * sheet.Width + sheet.Crop.X, row * sheet.Height + sheet.Crop.Y, sheet.Crop.Width, sheet.Crop.Height),
            new Rect(_roam ? _motion.Position : (Bounds.Width - bodyWidth) / 2,
                _roam ? Math.Max(0, Bounds.Height - bodyHeight - altitude) : (Bounds.Height - bodyHeight) / 2, bodyWidth, bodyHeight));
    }
    private void Release()
    {
        _timer.Stop(); _tickClock.Stop();
        foreach (var sheet in _sheets.Values) { sheet.Dispose(); LiveSheets--; } _sheets.Clear();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; Release(); _timer.Tick -= Tick; _age.Stop();
    }
}
