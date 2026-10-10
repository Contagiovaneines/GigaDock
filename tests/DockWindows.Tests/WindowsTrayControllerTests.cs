using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public sealed class WindowsTrayControllerTests
{
    [Theory]
    [InlineData("Mostrar Ícones Ocultos Mostrar ícones ocultos", "SystemTrayIcon", "SystemTray.AccentButton", true)]
    [InlineData("Show hidden icons", "", "Button", true)]
    [InlineData("", "SystemTray.OverflowButton", "Button", true)]
    [InlineData("Volume", "SystemTrayIcon", "SystemTray.AccentButton", false)]
    [InlineData("Relógio", "SystemTrayIcon", "SystemTray.AccentButton", false)]
    public void IdentificaChevronSemConfundirIconesComMesmoId(string name, string id, string cls, bool expected)
        => Assert.Equal(expected, WindowsTrayService.IsOverflowButton(name, id, cls));
    [Fact]
    public async Task BarraOcultaPelaDock_RestauraAntesDeInvocarBandeja()
    {
        var calls = new List<string>(); var taskbar = new FakeTaskbar(calls); var tray = new FakeTray(calls);
        var result = await new WindowsTrayController(taskbar, tray).OpenAsync(true, 2);
        Assert.True(result.Opened); Assert.True(result.TaskbarRestored);
        Assert.Equal(new[] { "restore:2", "tray" }, calls);
    }
    [Fact]
    public async Task ModoNormal_NaoMudaBarraWindows()
    {
        var calls = new List<string>();
        var result = await new WindowsTrayController(new FakeTaskbar(calls), new FakeTray(calls)).OpenAsync(false, 2);
        Assert.True(result.Opened); Assert.False(result.TaskbarRestored); Assert.Equal(new[] { "tray" }, calls);
    }
    [Fact]
    public async Task RestauracaoRecusada_NaoInvocaControleDesativado()
    {
        var calls = new List<string>();
        var result = await new WindowsTrayController(new FakeTaskbar(calls) { Restore = false }, new FakeTray(calls)).OpenAsync(true, 1);
        Assert.False(result.Opened); Assert.False(result.TaskbarRestored); Assert.Single(calls); Assert.NotEmpty(result.Message);
    }
    [Fact]
    public async Task BotaoAusente_MantemBarraRestauradaEOrientaUsuario()
    {
        var calls = new List<string>();
        var result = await new WindowsTrayController(new FakeTaskbar(calls), new FakeTray(calls) { Available = false }).OpenAsync(true, 2);
        Assert.False(result.Opened); Assert.True(result.TaskbarRestored); Assert.NotEmpty(result.Message);
        Assert.DoesNotContain("hide", calls);
    }
    [Fact]
    public async Task ProviderComFalha_RetornaEstadoEmVezDeExcecao()
    {
        var calls = new List<string>();
        var result = await new WindowsTrayController(new FakeTaskbar(calls), new FakeTray(calls) { Throw = true }).OpenAsync(false, null);
        Assert.False(result.Opened); Assert.NotEmpty(result.Message);
    }
    private sealed class FakeTray(List<string> calls) : IWindowsTrayService
    {
        public bool Available { get; init; } = true;
        public bool Throw { get; init; }
        public bool Abrir(CancellationToken cancellationToken = default)
        { calls.Add("tray"); if (Throw) throw new InvalidOperationException(); return Available; }
    }
    private sealed class FakeTaskbar(List<string> calls) : ITaskbarService
    {
        public bool Restore { get; init; } = true;
        public int ObterEstadoAtual() => 2;
        public bool OcultarBarraNativa(out int state) { state = 2; calls.Add("hide"); return true; }
        public bool RestaurarBarraNativa(int? state = null) { calls.Add("restore:" + state); return Restore; }
        public void GarantirBarraOculta() { }
        public void Dispose() { }
    }
}
