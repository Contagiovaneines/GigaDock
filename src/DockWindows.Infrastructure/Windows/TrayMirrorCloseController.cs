namespace DockWindows.Infrastructure.Windows;

/// <summary>O pedido de fechamento não confirma que o painel já desapareceu.</summary>
public sealed class TrayMirrorCloseController(Func<bool> isVisible, Func<CancellationToken, bool> requestClose)
{
    public async Task<bool> CloseAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!isVisible()) return true;
        if (!requestClose(token)) return !isVisible();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(40, token);
            if (!isVisible()) return true;
        }
        return false;
    }
}
