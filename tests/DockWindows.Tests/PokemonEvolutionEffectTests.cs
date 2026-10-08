using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DockWindows.App.Common;
using DockWindows.App.Controls;

namespace DockWindows.Tests;

public class PokemonEvolutionEffectTests
{
    [Fact]
    public void TransformacaoRenderizaEnergiaERevelaFormaNovaSemFundo()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var source = PokemonWalkAnimation.Load(133)!.Front[0].Image;
                var target = PokemonWalkAnimation.Load(134)!.Front[0].Image;
                var control = new PokemonEvolutionEffect { Width = 64, Height = 56 };
                control.Measure(new Size(64, 56));
                control.Arrange(new Rect(0, 0, 64, 56));
                var snapshots = new List<BitmapSource>();
                foreach (var progress in new[] { 0d, .15, .35, .5, .85, 1 })
                {
                    control.SetFrame(source, target, progress);
                    control.UpdateLayout();
                    var bitmap = new RenderTargetBitmap(64, 56, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(control);
                    bitmap.Freeze();
                    snapshots.Add(bitmap);
                    var pixels = Pixels(bitmap);
                    Assert.Equal(0, pixels[3]); // O efeito não cria um painel de fundo.
                    Assert.Contains(pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
                }
                Assert.False(Pixels(snapshots[0]).SequenceEqual(Pixels(snapshots[^1])));
                var middle = Pixels(snapshots[3]);
                var center = (28 * 64 + 32) * 4;
                Assert.True(middle[center] > 240 && middle[center + 1] > 240 && middle[center + 2] > 240);

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(24, 31, 40)), null, new Rect(0, 0, 768, 112));
                    for (var i = 0; i < snapshots.Count; i++)
                        dc.DrawImage(snapshots[i], new Rect(i * 128, 0, 128, 112));
                }
                var preview = new RenderTargetBitmap(768, 112, 96, 96, PixelFormats.Pbgra32);
                preview.Render(visual);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(preview));
                using var output = File.Create(Path.Combine(ProjectRoot(), "docs", "pokemon-evolution-preview.png"));
                encoder.Save(output);
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(); thread.Join();
        if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static byte[] Pixels(BitmapSource image)
    {
        var pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return pixels;
    }

    private static string ProjectRoot([CallerFilePath] string path = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
