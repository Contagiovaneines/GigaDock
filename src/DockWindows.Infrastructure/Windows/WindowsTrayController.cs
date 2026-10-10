namespace DockWindows.Infrastructure.Windows;

public readonly record struct WindowsTrayOpenResult(bool Opened, bool TaskbarRestored, string Message);

/// <summary>Coordena a barra que a própria dock ocultou antes de invocar a bandeja.</summary>
public sealed class WindowsTrayController(ITaskbarService taskbar, IWindowsTrayService tray)
{
    public async Task<WindowsTrayOpenResult> OpenAsync(bool dockIsPrimary, int? previousState)
    {
        var restored = false;
        try
        {
            if (dockIsPrimary)
            {
                restored = taskbar.RestaurarBarraNativa(previousState);
                if (!restored) return new(false, false, "Não foi possível mostrar a barra do Windows. Use o botão Mostrar barra do Windows.");
                await Task.Delay(150); // Explorer precisa remontar a árvore acessível após a restauração.
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var opened = await Task.Run(() => tray.Abrir(timeout.Token)).WaitAsync(timeout.Token);
            return new(opened, restored, opened ? "" : "A barra do Windows está disponível. Use sua seta de ícones ocultos; se todos os ícones estiverem visíveis, não haverá painel oculto.");
        }
        catch (OperationCanceledException)
        {
            return new(false, restored, "O Windows demorou para responder. Use a seta de ícones ocultos na barra de tarefas.");
        }
        catch (Exception)
        {
            return new(false, restored, "Abra os ícones ocultos pela barra do Windows. Não foi possível acessar o controle nesta sessão.");
        }
    }
}
