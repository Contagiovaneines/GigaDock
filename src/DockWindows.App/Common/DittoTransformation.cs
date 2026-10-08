namespace DockWindows.App.Common;

public sealed class DittoTransformation(Random? random = null)
{
    private readonly Random _random = random ?? Random.Shared;
    private TimeSpan _nextChange = TimeSpan.FromMinutes(30);
    private int _target = 132;
    private int _lastCopied = 132;

    public void Reset()
    {
        _nextChange = TimeSpan.FromMinutes(30);
        _target = 132;
        _lastCopied = 132;
    }

    public int TargetFor(int selectedId, int displayedId, TimeSpan elapsed)
    {
        if (selectedId != 132) return selectedId;
        if (elapsed < _nextChange) return _target;
        if (_target != 132) _target = 132;
        else
        {
            var candidates = Enumerable.Range(1, 151).Where(id => id != 132 && id != displayedId && id != _lastCopied).ToArray();
            _target = candidates[_random.Next(candidates.Length)];
            _lastCopied = _target;
        }
        _nextChange = elapsed + TimeSpan.FromMinutes(30);
        return _target;
    }
}
