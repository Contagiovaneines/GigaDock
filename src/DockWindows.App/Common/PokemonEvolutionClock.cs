namespace DockWindows.App.Common;

public sealed class PokemonEvolutionClock(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private long _started;
    private int? _selected;
    public TimeSpan Elapsed => _selected.HasValue ? _time.GetElapsedTime(_started) : TimeSpan.Zero;

    public bool Select(int pokemonId)
    {
        if (_selected == pokemonId) return false;
        _selected = pokemonId;
        _started = _time.GetTimestamp();
        return true;
    }

    public int Stage
    {
        get
        {
            if (!_selected.HasValue) return 0;
            var elapsed = Elapsed;
            return elapsed >= TimeSpan.FromHours(3) ? 2 : elapsed >= TimeSpan.FromHours(1) ? 1 : 0;
        }
    }
}
