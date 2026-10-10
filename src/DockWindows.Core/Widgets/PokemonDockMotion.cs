namespace DockWindows.Core.Widgets;

/// <summary>Deslocamento independente do tema, toolkit e relógio da interface.</summary>
public sealed class PokemonDockMotion
{
    private double _pause, _frontPause, _untilFrontPause;
    private readonly Func<double> _nextFrontInterval;
    public PokemonDockMotion(Func<double>? nextFrontInterval = null)
    {
        _nextFrontInterval = nextFrontInterval ?? (() => Random.Shared.Next(90000, 180001));
        _untilFrontPause = _nextFrontInterval();
    }
    public double Position { get; private set; } = 16;
    public bool FacingRight { get; private set; } = true;
    public double Distance { get; private set; }
    public bool FacingFront => _pause > 175 || _frontPause > 0;
    public void Advance(double width, double spriteWidth, double elapsedMilliseconds, bool animate)
    {
        var right = Math.Max(0, width - spriteWidth - 16);
        var left = Math.Min(16, right);
        Position = Math.Clamp(Position, left, right);
        if (!animate || !double.IsFinite(elapsedMilliseconds)) return;
        var elapsed = Math.Clamp(elapsedMilliseconds, 0, 100);
        _pause = Math.Max(0, _pause - elapsed);
        _frontPause = Math.Max(0, _frontPause - elapsed);
        if (_pause > 0 || _frontPause > 0 || right <= left) return;
        var previous = Position;
        Position += (FacingRight ? 1 : -1) * 18 * elapsed / 1000;
        if (Position >= right) { Position = right; FacingRight = false; _pause = 350; }
        else if (Position <= left) { Position = left; FacingRight = true; _pause = 350; }
        _untilFrontPause -= Math.Abs(Position - previous) / 18 * 1000;
        var middle = (left + right) / 2;
        if (_untilFrontPause <= 0 && ((previous < middle && Position >= middle) || (previous > middle && Position <= middle)))
        {
            Position = middle; _frontPause = Random.Shared.Next(2000, 3001); _untilFrontPause = _nextFrontInterval();
        }
        Distance += Math.Abs(Position - previous);
    }
}
