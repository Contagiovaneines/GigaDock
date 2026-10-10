namespace DockWindows.Infrastructure.Windows;

/// <summary>Repõe apenas a barra que a leitura do espelho mostrou temporariamente.</summary>
public sealed class TrayTaskbarRestoreSession(ITaskbarService taskbar, bool wasHidden)
{
    private bool _shownForMirror;
    public bool NeedsRestore => wasHidden && _shownForMirror;
    public void MarkShown(bool restored) => _shownForMirror |= restored;
    public bool Restore()
    {
        if (!NeedsRestore) return true;
        try
        {
            if (!taskbar.OcultarBarraNativa(out _)) return false;
            _shownForMirror = false;
            return true;
        }
        catch { return false; }
    }
}
