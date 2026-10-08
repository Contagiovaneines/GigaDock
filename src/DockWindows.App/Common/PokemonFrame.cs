using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DockWindows.App.Common;

public sealed record PokemonFrame(BitmapSource Image, int DelayMilliseconds)
{
    public static IReadOnlyList<PokemonFrame> Carregar(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder is not GifBitmapDecoder)
        {
            var image = decoder.Frames[0]; image.Freeze();
            return new[] { new PokemonFrame(image, 100) };
        }
        int Query(BitmapMetadata? metadata, string name, int fallback = 0)
            => metadata?.GetQuery(name) is { } value ? Convert.ToInt32(value) : fallback;
        var global = decoder.Metadata as BitmapMetadata;
        var width = Query(global, "/logscrdesc/Width", decoder.Frames.Max(f => f.PixelWidth));
        var height = Query(global, "/logscrdesc/Height", decoder.Frames.Max(f => f.PixelHeight));
        BitmapSource? previous = null;
        var frames = new List<PokemonFrame>();
        var bounds = Int32Rect.Empty;
        foreach (var frame in decoder.Frames)
        {
            var meta = frame.Metadata as BitmapMetadata;
            var rect = new Rect(Query(meta, "/imgdesc/Left"), Query(meta, "/imgdesc/Top"), frame.PixelWidth, frame.PixelHeight);
            BitmapSource Draw(bool clearFrame)
            {
                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    if (previous != null)
                    {
                        if (clearFrame)
                        {
                            var clip = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(0, 0, width, height)), new RectangleGeometry(rect));
                            dc.PushClip(clip);
                            dc.DrawImage(previous, new Rect(0, 0, width, height));
                            dc.Pop();
                        }
                        else dc.DrawImage(previous, new Rect(0, 0, width, height));
                    }
                    if (!clearFrame) dc.DrawImage(frame, rect);
                }
                var result = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                result.Render(visual); result.Freeze(); return result;
            }
            var composed = Draw(false);
            frames.Add(new(composed, Math.Max(20, Query(meta, "/grctlext/Delay", 10) * 10)));
            var pixels = new byte[width * height * 4]; composed.CopyPixels(pixels, width * 4, 0);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (pixels[(y * width + x) * 4 + 3] != 0)
                {
                    if (bounds.IsEmpty) bounds = new Int32Rect(x, y, 1, 1);
                    else
                    {
                        var right = Math.Max(bounds.X + bounds.Width, x + 1);
                        var bottom = Math.Max(bounds.Y + bounds.Height, y + 1);
                        var left = Math.Min(bounds.X, x); var top = Math.Min(bounds.Y, y);
                        bounds = new Int32Rect(left, top, right - left, bottom - top);
                    }
                }
            previous = Query(meta, "/grctlext/Disposal") switch { 2 => Draw(true), 3 => previous, _ => composed };
        }
        if (!bounds.IsEmpty)
            for (var i = 0; i < frames.Count; i++)
            {
                var cropped = new CroppedBitmap(frames[i].Image, bounds); cropped.Freeze();
                frames[i] = frames[i] with { Image = cropped };
            }
        return frames;
    }
}
