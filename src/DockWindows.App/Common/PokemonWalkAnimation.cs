using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;

namespace DockWindows.App.Common;

/// <summary>Quadros laterais de Walk; o ciclo acompanha a distância real na dock.</summary>
public sealed class PokemonWalkAnimation
{
    public IReadOnlyList<PokemonFrame> Left { get; }
    public IReadOnlyList<PokemonFrame> Right { get; }
    public IReadOnlyList<PokemonFrame> Front { get; }
    public PokemonMovementKind MovementKind { get; }
    public bool IsAirborne => MovementKind != PokemonMovementKind.Ground;
    public string AnimationName { get; }
    private readonly int _cycleMilliseconds;
    public const double DistancePerCycle = 16;

    private PokemonWalkAnimation(IReadOnlyList<PokemonFrame> left, IReadOnlyList<PokemonFrame> right, IReadOnlyList<PokemonFrame> front, int id, string animationName)
    {
        Left = left; Right = right; Front = front;
        MovementKind = PokemonLocomotion.KindFor(id);
        AnimationName = animationName;
        _cycleMilliseconds = left.Sum(f => f.DelayMilliseconds);
    }

    public BitmapSource FrameAt(double distance, bool right)
    {
        var time = (distance % DistancePerCycle + DistancePerCycle) % DistancePerCycle / DistancePerCycle * _cycleMilliseconds;
        return FrameAtTime(time, right);
    }

    public BitmapSource FrameAtTime(double milliseconds, bool right)
    {
        var frames = right ? Right : Left;
        return FrameFrom(frames, milliseconds);
    }

    public BitmapSource FrontAtTime(double milliseconds) => FrameFrom(Front, milliseconds);

    private BitmapSource FrameFrom(IReadOnlyList<PokemonFrame> frames, double milliseconds)
    {
        var time = (milliseconds % _cycleMilliseconds + _cycleMilliseconds) % _cycleMilliseconds;
        foreach (var frame in frames)
        {
            if (time < frame.DelayMilliseconds) return frame.Image;
            time -= frame.DelayMilliseconds;
        }
        return frames[^1].Image;
    }

    public static PokemonWalkAnimation? Load(int id, string? requestedAnimation = null)
    {
        if (id is < 1 or > 151) return null;
        var prefix = $"/DockWindows.App;component/Assets/PokemonWalk/{id:0000}/";
        try
        {
            var resource = Application.GetResourceStream(new Uri(prefix + "AnimData.xml", UriKind.Relative));
            if (resource == null) return null;
            using var stream = resource.Stream;
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var animations = XDocument.Load(reader).Descendants("Anim").ToArray();
            var name = requestedAnimation == "Rest"
                ? (animations.Any(a => (string?)a.Element("Name") == "Sit") ? "Sit" : "Idle")
                : requestedAnimation ?? PokemonLocomotion.AnimationFor(id);
            var walk = animations.Single(a => (string?)a.Element("Name") == name);
            // Algumas sequências (ex.: Hover de Zubat) referenciam outro conjunto de quadros.
            while ((string?)walk.Element("CopyOf") is { } copy)
                walk = animations.Single(a => (string?)a.Element("Name") == copy);
            var sourceName = (string)walk.Element("Name")!;
            var width = (int)walk.Element("FrameWidth")!;
            var height = (int)walk.Element("FrameHeight")!;
            var durations = walk.Element("Durations")!.Elements("Duration").Select(d => Math.Max(1, (int)d) * 1000 / 60).ToArray();
            var image = new BitmapImage();
            var sheet = Application.GetResourceStream(new Uri(prefix + sourceName + "-Anim.png", UriKind.Relative));
            if (sheet == null) return null;
            using var sheetStream = sheet.Stream;
            image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = sheetStream; image.EndInit(); image.Freeze();
            if (width <= 0 || height <= 0 || durations.Length == 0 || image.PixelWidth != width * durations.Length || (image.PixelHeight != height * 8 && image.PixelHeight != height))
                return null;

            var left = new List<PokemonFrame>(); var right = new List<PokemonFrame>(); var front = new List<PokemonFrame>();
            // Nos recursos usados pela dock, linha 6 olha à esquerda e linha 2 à direita.
            foreach (var (row, target) in new[] { (6, left), (2, right), (0, front) })
                for (var i = 0; i < durations.Length; i++)
                {
                    var crop = new CroppedBitmap(image, new Int32Rect(i * width, image.PixelHeight == height ? 0 : row * height, width, height)); crop.Freeze();
                    target.Add(new PokemonFrame(crop, durations[i]));
                }

            // Um recorte comum preserva as posições relativas das patas entre quadros e direções.
            var x0 = width; var y0 = height; var x1 = 0; var y1 = 0;
            foreach (var frame in left.Concat(right).Concat(front))
            {
                var rgba = new FormatConvertedBitmap(frame.Image, PixelFormats.Bgra32, null, 0);
                var pixels = new byte[width * height * 4]; rgba.CopyPixels(pixels, width * 4, 0);
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                        if (pixels[(y * width + x) * 4 + 3] != 0)
                        { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x + 1); y1 = Math.Max(y1, y + 1); }
            }
            if (x1 <= x0 || y1 <= y0) return null;
            var bounds = new Int32Rect(x0, y0, x1 - x0, y1 - y0);
            foreach (var target in new[] { left, right, front })
                for (var i = 0; i < target.Count; i++)
                {
                    var crop = new CroppedBitmap(target[i].Image, bounds); crop.Freeze();
                    target[i] = target[i] with { Image = crop };
                }
            return new PokemonWalkAnimation(left, right, front, id, name);
        }
        catch (Exception ex) when (ex is System.IO.IOException or XmlException or InvalidOperationException or ArgumentException or FormatException or NotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine($"Walk #{id}: {ex.Message}");
            return null; // Mantém o GIF já disponível se o recurso local não puder ser lido.
        }
    }
}
