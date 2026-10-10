using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public sealed class DesktopWindowAdapterTests
{
    [Fact]
    public void Adaptador_EmiteIdWin32EEncaminhaOperacaoSemAlterarJanela()
    {
        var tracking = new FakeTracker();
        using var adapter = new WindowsDesktopWindowService(tracking);
        var window = Assert.Single(adapter.ObterJanelasAbertas());
        Assert.Equal("Editor", window.Titulo);
        Assert.Equal(new DesktopWindowId("win32", "42"), window.Id);
        Assert.True(adapter.Ativar(window.Id));
        Assert.Equal((nint)42, tracking.LastHandle);
    }

    [Theory]
    [InlineData("wayland", "42")]
    [InlineData("win32", "não numérico")]
    [InlineData("win32", "0")]
    public void Adaptador_RejeitaIdInvalidoOuDeOutroBackendSemInvocarTracker(string backend, string value)
    {
        var tracking = new FakeTracker();
        using var adapter = new WindowsDesktopWindowService(tracking);
        var id = new DesktopWindowId(backend, value);
        Assert.False(adapter.Ativar(id));
        Assert.False(adapter.Minimizar(id));
        Assert.False(adapter.Fechar(id));
        Assert.Equal(IntPtr.Zero, tracking.LastHandle);
    }

    private sealed class FakeTracker : IWindowTrackingService
    {
        public IntPtr LastHandle { get; private set; }
        public event Action? JanelasAlteradas { add { } remove { } }
        public event Action<IntPtr>? JanelaAtivada { add { } remove { } }
        public event Action<bool>? TelaCheiaAlterada { add { } remove { } }
        public IReadOnlyList<JanelaInfo> ObterJanelasAbertas() => [new() { Hwnd = (nint)42, Titulo = "Editor" }];
        public IntPtr ObterJanelaAtiva() => (nint)42;
        public void Iniciar() { }
        public void Parar() { }
        public bool AtivarJanela(IntPtr hWnd) { LastHandle = hWnd; return true; }
        public bool MinimizarJanela(IntPtr hWnd) { LastHandle = hWnd; return true; }
        public bool FecharJanela(IntPtr hWnd) { LastHandle = hWnd; return true; }
        public void Dispose() { }
    }
}
