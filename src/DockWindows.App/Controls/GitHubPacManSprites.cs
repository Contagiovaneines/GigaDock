using System.Windows;
using System.Windows.Media;
using DockWindows.App.ViewModels;

namespace DockWindows.App.Controls;

/// <summary>Sprites locais em pixels, desenhados na escala da grade.</summary>
internal static class GitHubPacManSprites
{
    private static readonly Brush Yellow = Frozen("#FFF000");
    private static readonly Brush[] GhostColors = [Frozen("#F01828"), Frozen("#00B8CF"), Frozen("#FF8418"), Frozen("#F00888")];
    private static readonly string[] Ghost =
    [
        "0000111110000", "0011111111100", "0111111111110", "0111111111110",
        "1112211122111", "1122221222211", "1122331223311", "1113311133111",
        "1111111111111", "1111111111111", "1111111111111", "1110111010111", "1100011000011"
    ];
    private static readonly string[] PacMan =
    [
        "0000111110000", "0011111111100", "0111111111110", "0111111111110",
        "1111111111111", "1111111111111", "1111111111111", "1111111111111",
        "1111111111111", "0111111111110", "0111111111110", "0011111111100", "0000111110000"
    ];

    public static void Draw(DrawingContext dc, Rect bounds, ContribuicaoDia day)
    {
        var unit = bounds.Width / 13;
        var pacman = day.EhPacMan;
        var pixels = pacman ? PacMan : Ghost;
        if (pacman) dc.PushTransform(new RotateTransform(day.DirecaoPacMan * 90, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
        for (var y = 0; y < 13; y++)
            for (var x = 0; x < 13; x++)
            {
                var pixel = pixels[y][x];
                if (pixel == '0') continue;
                if (pacman && day.BocaPacManAberta && x >= 6 && Math.Abs(y - 6) <= (x - 6) * .65) continue;
                var brush = pacman ? Yellow : pixel == '2' ? Brushes.White
                    : pixel == '3' ? Brushes.Black : GhostColors[Math.Clamp(day.CorFantasma, 0, 3)];
                dc.DrawRectangle(brush, null, new Rect(bounds.X + x * unit, bounds.Y + y * unit, unit, unit));
            }
        if (pacman) dc.Pop();
    }

    private static Brush Frozen(string color)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        brush.Freeze();
        return brush;
    }
}
