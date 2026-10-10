using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public sealed class BackgroundWindowActivationTests
{
    [Fact]
    public void OcultaNaBandeja_NaoEReveladaNemRecebeFoco()
    {
        var api = new FakeApi(); api.Windows[1] = new(Visible: false);
        Assert.False(new WindowsWindowActivation(api).Activate(1));
        Assert.Empty(api.Restored); Assert.Empty(api.Activated);
    }

    [Fact]
    public void PrincipalBloqueada_AtivaDialogoSemReabilitarPrincipal()
    {
        var api = new FakeApi(); api.Windows[1] = new(Enabled: false, Popup: 2); api.Windows[2] = new();
        Assert.True(new WindowsWindowActivation(api).Activate(1));
        Assert.Equal((nint)2, Assert.Single(api.Activated)); Assert.Empty(api.Restored);
        Assert.False(api.Windows[1].Enabled);
    }

    [Fact]
    public void PrincipalBloqueadaSemDialogo_NaoEAtivada()
    {
        var api = new FakeApi(); api.Windows[1] = new(Enabled: false);
        Assert.False(new WindowsWindowActivation(api).Activate(1)); Assert.Empty(api.Activated);
    }

    [Fact]
    public void Minimizada_RestauracaoAssincronaUmaVez()
    {
        var api = new FakeApi(); api.Windows[1] = new(Minimized: true);
        var activation = new WindowsWindowActivation(api);
        Assert.True(activation.Activate(1)); Assert.True(activation.Activate(1));
        Assert.Equal((nint)1, Assert.Single(api.Restored)); Assert.Equal(2, api.Activated.Count);
    }

    [Fact]
    public void JanelaNormal_CliquesRepetidosNaoAlteramEstadoDeExibicao()
    {
        var api = new FakeApi(); api.Windows[1] = new();
        var activation = new WindowsWindowActivation(api);
        Assert.True(activation.Activate(1)); Assert.True(activation.Activate(1));
        Assert.Empty(api.Restored); Assert.True(api.Windows[1].Visible); Assert.True(api.Windows[1].Enabled);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, true, true)]
    public void JanelaInexistenteOuEmOutroDesktop_NaoEAtivada(bool exists, bool visible, bool cloaked)
    {
        var api = new FakeApi(); if (exists) api.Windows[1] = new(Visible: visible, Cloaked: cloaked);
        Assert.False(new WindowsWindowActivation(api).Activate(1)); Assert.Empty(api.Activated);
    }

    [Fact]
    public void PopupDeOutroProcesso_NaoRecebeFoco()
    {
        var api = new FakeApi(); api.Windows[1] = new(Enabled: false, Popup: 2); api.Windows[2] = new(Process: 22);
        Assert.False(new WindowsWindowActivation(api).Activate(1)); Assert.Empty(api.Activated);
    }

    [Fact]
    public void RestauracaoRecusadaOuFocoRecusado_RetornaFalha()
    {
        var api = new FakeApi { RestoreResult = false }; api.Windows[1] = new(Minimized: true);
        Assert.False(new WindowsWindowActivation(api).Activate(1)); Assert.Empty(api.Activated);
        api.Windows[1] = new(); api.ForegroundResult = false;
        Assert.False(new WindowsWindowActivation(api).Activate(1));
    }

    [Fact]
    public void Nativa_JanelaOcultaContinuaOcultaEFechaNormalmente() => Sta(() =>
    {
        var window = CreateWindow(); var closed = false; window.Closed += (_, _) => closed = true;
        try
        {
            window.Show(); var handle = new WindowInteropHelper(window).Handle; window.Hide();
            Assert.False(new WindowsWindowActivation().Activate(handle));
            Assert.False(IsWindowVisible(handle)); Assert.True(IsWindowEnabled(handle));
            window.Close(); Assert.True(closed);
        }
        finally { if (!closed) window.Close(); }
    });

    [Fact]
    public void Nativa_DialogoModalPreservaBloqueioEFechaPeloComandoDoX() => Sta(() =>
    {
        var owner = CreateWindow(); var dialog = CreateWindow(); var closed = false;
        Exception? error = null; var timedOut = false;
        dialog.Closed += (_, _) => closed = true;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) => { timedOut = true; timer.Stop(); dialog.Close(); };
        try
        {
            owner.Show(); dialog.Owner = owner;
            var ownerHandle = new WindowInteropHelper(owner).Handle;
            dialog.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var dialogHandle = new WindowInteropHelper(dialog).Handle;
                    Assert.False(IsWindowEnabled(ownerHandle));
                    new WindowsWindowActivation().Activate(ownerHandle);
                    Assert.False(IsWindowEnabled(ownerHandle)); Assert.True(IsWindowEnabled(dialogHandle));
                    Assert.True(IsWindowVisible(dialogHandle));
                    Assert.True(PostMessage(dialogHandle, 0x0112, (nint)0xF060, 0)); // SC_CLOSE, mesmo fluxo do X.
                }
                catch (Exception ex) { error = ex; dialog.Close(); }
            }), DispatcherPriority.ApplicationIdle);
            timer.Start(); dialog.ShowDialog(); timer.Stop();
            if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
            Assert.False(timedOut); Assert.True(closed); Assert.True(IsWindowEnabled(ownerHandle));
        }
        finally { timer.Stop(); if (!closed) dialog.Close(); owner.Close(); }
    });

    [Fact]
    public void Nativa_MinimizadaRestauraESegueFechandoPeloX() => Sta(() =>
    {
        var window = CreateWindow(); var closed = false; window.Closed += (_, _) => closed = true;
        try
        {
            window.Show(); var handle = new WindowInteropHelper(window).Handle;
            window.WindowState = WindowState.Minimized;
            new WindowsWindowActivation().Activate(handle);
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.Equal(WindowState.Normal, window.WindowState); Assert.True(IsWindowEnabled(handle));
            Assert.True(PostMessage(handle, 0x0112, (nint)0xF060, 0));
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            Assert.True(closed);
        }
        finally { if (!closed) window.Close(); }
    });

    private static Window CreateWindow() => new() { Title = "GigaDock — teste isolado de ativação", Width = 300, Height = 180, ShowInTaskbar = false };
    private static void Sta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); } });
        thread.SetApartmentState(ApartmentState.STA); thread.IsBackground = true; thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Teste nativo excedeu dez segundos.");
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    private sealed record State(bool Visible = true, bool Enabled = true, bool Minimized = false, bool Cloaked = false, uint Process = 11, nint Popup = 0);
    private sealed class FakeApi : IWindowActivationApi
    {
        public Dictionary<nint, State> Windows { get; } = [];
        public List<nint> Restored { get; } = []; public List<nint> Activated { get; } = [];
        public bool RestoreResult = true, ForegroundResult = true;
        public bool Exists(nint window) => Windows.ContainsKey(window);
        public bool Visible(nint window) => Windows[window].Visible;
        public bool Enabled(nint window) => Windows[window].Enabled;
        public bool Minimized(nint window) => Windows[window].Minimized;
        public bool Cloaked(nint window) => Windows[window].Cloaked;
        public uint ProcessId(nint window) => Windows[window].Process;
        public nint LastActivePopup(nint window) => Windows[window].Popup;
        public bool RestoreAsync(nint window)
        {
            Restored.Add(window); if (RestoreResult) Windows[window] = Windows[window] with { Minimized = false }; return RestoreResult;
        }
        public bool Foreground(nint window) { Activated.Add(window); return ForegroundResult; }
    }
}
