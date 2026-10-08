using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DockWindows.App.Controls;

/// <summary>Transformação local: silhueta branca, esfera de energia e revelação da nova forma.</summary>
public sealed class PokemonEvolutionEffect : FrameworkElement
{
    public const int DurationMilliseconds = 3000;
    private BitmapSource? _source, _target;
    private double _progress;

    public void SetFrame(BitmapSource? source, BitmapSource? target, double progress)
    {
        _source = source; _target = target;
        _progress = Math.Clamp(progress, 0, 1);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var p = _progress;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var energy = Math.Sin(Math.PI * p);
        var radius = (12 + 8 * energy) * (.92 + .08 * Math.Sin(p * Math.PI * 22));
        var reveal = Math.Clamp((p - .76) / .24, 0, 1);
        var oldOpacity = Math.Clamp(1 - p / .3, 0, 1);
        DrawSprite(dc, _source, center, oldOpacity, Math.Clamp(p / .18, 0, 1), 1 - p * .25);
        if (p > .08 && p < .98)
        {
            dc.PushOpacity(Math.Min(1, energy * 1.5));
            var glow = new RadialGradientBrush();
            glow.GradientStops.Add(new GradientStop(Color.FromArgb(240, 245, 255, 255), 0));
            glow.GradientStops.Add(new GradientStop(Color.FromArgb(230, 139, 242, 255), .55));
            glow.GradientStops.Add(new GradientStop(Color.FromArgb(0, 60, 180, 255), 1));
            dc.DrawEllipse(glow, null, center, radius + 8, radius + 8);
            dc.DrawEllipse(Brushes.White, null, center, radius * .62, radius * .62);
            for (var ring = 0; ring < 3; ring++)
            {
                var geometry = new StreamGeometry();
                using (var context = geometry.Open())
                    for (var i = 0; i <= 32; i++)
                    {
                        var angle = i / 32d * Math.PI * 1.6 + p * Math.PI * 12 + ring * 2.1;
                        var r = radius * (1 + ring * .12);
                        var point = new Point(center.X + Math.Cos(angle) * r, center.Y + Math.Sin(angle) * r * .72);
                        if (i == 0) context.BeginFigure(point, false, false);
                        else context.LineTo(point, true, false);
                    }
                dc.DrawGeometry(null, new Pen(ring == 1 ? Brushes.White : Brushes.DeepSkyBlue, 1.6), geometry);
            }
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Math.PI / 4 + p * Math.PI * 6;
                var r = radius + 4;
                dc.DrawRectangle(Brushes.White, null, new Rect(center.X + Math.Cos(angle) * r - 1,
                    center.Y + Math.Sin(angle) * r - 1, 2, 2));
            }
            dc.Pop();
        }
        DrawSprite(dc, _target, center, reveal, 1 - reveal, .7 + .3 * reveal);
    }

    private static void DrawSprite(DrawingContext dc, BitmapSource? sprite, Point center, double opacity, double white, double scale)
    {
        if (sprite == null || opacity <= 0) return;
        var height = 40 * scale;
        var width = Math.Min(56, height * sprite.Width / sprite.Height);
        var bounds = new Rect(center.X - width / 2, center.Y - height / 2, width, height);
        dc.PushOpacity(opacity);
        dc.DrawImage(sprite, bounds);
        dc.PushOpacity(white);
        dc.PushOpacityMask(new ImageBrush(sprite) { Stretch = Stretch.Fill, Viewport = bounds, ViewportUnits = BrushMappingMode.Absolute });
        dc.DrawRectangle(Brushes.White, null, bounds);
        dc.Pop(); dc.Pop(); dc.Pop();
    }
}
