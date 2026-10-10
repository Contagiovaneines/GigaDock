using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public sealed class TrayMirrorCloseControllerTests
{
    [Fact]
    public async Task JaFechada_NaoAlternaSetaParaAbrirNovamente()
    {
        var requests = 0;
        var controller = new TrayMirrorCloseController(() => false, _ => { requests++; return true; });
        Assert.True(await controller.CloseAsync(CancellationToken.None));
        Assert.Equal(0, requests);
    }

    [Fact]
    public async Task PedidoAceitoMasPainelVisivel_NaoAutorizaEspelho()
    {
        var controller = new TrayMirrorCloseController(() => true, _ => true);
        Assert.False(await controller.CloseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task FechamentoAssincrono_AguardaDesaparecerAntesDeAutorizar()
    {
        var reads = 0;
        var requests = 0;
        var controller = new TrayMirrorCloseController(() => ++reads < 4, _ => { requests++; return true; });
        Assert.True(await controller.CloseAsync(CancellationToken.None));
        Assert.Equal(1, requests);
        Assert.Equal(4, reads);
    }

    [Fact]
    public async Task FalhaAoFechar_NaoAutorizaEspelho()
    {
        var controller = new TrayMirrorCloseController(() => true, _ => false);
        Assert.False(await controller.CloseAsync(CancellationToken.None));
    }
}
