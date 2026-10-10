using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public sealed class TrayTaskbarRestoreSessionTests
{
    [Fact]
    public void TemporarilyShownBar_IsHiddenOnce()
    {
        var bar = new FakeTaskbar();
        var session = new TrayTaskbarRestoreSession(bar, true);
        session.MarkShown(true);
        Assert.True(session.NeedsRestore);
        Assert.True(session.Restore());
        Assert.True(session.Restore());
        Assert.False(session.NeedsRestore);
        Assert.Equal(1, bar.HideCalls);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void VisibleOrNeverShownBar_IsUntouched(bool hidden, bool shown)
    {
        var bar = new FakeTaskbar();
        var session = new TrayTaskbarRestoreSession(bar, hidden);
        session.MarkShown(shown);
        Assert.True(session.Restore());
        Assert.Equal(0, bar.HideCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedCleanup_CanRetry(bool throws)
    {
        var bar = new FakeTaskbar { Fail = true, Throws = throws };
        var session = new TrayTaskbarRestoreSession(bar, true);
        session.MarkShown(true);
        Assert.False(session.Restore());
        Assert.True(session.NeedsRestore);
        bar.Fail = false;
        Assert.True(session.Restore());
        Assert.False(session.NeedsRestore);
        Assert.Equal(2, bar.HideCalls);
    }

    private sealed class FakeTaskbar : ITaskbarService
    {
        public bool Fail { get; set; }
        public bool Throws { get; set; }
        public int HideCalls { get; private set; }
        public int ObterEstadoAtual() => 2;
        public bool OcultarBarraNativa(out int state)
        {
            state = 2;
            HideCalls++;
            if (Fail && Throws) throw new InvalidOperationException();
            return !Fail;
        }
        public bool RestaurarBarraNativa(int? state = null) => throw new InvalidOperationException();
        public void GarantirBarraOculta() { }
        public void Dispose() { }
    }
}
