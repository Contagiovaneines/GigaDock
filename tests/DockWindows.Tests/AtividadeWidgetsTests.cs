using DockWindows.Core.Widgets;
using DockWindows.Core.Models;
using DockWindows.App.ViewModels;
using DockWindows.Infrastructure.Windows;
using System.Threading;
using Xunit;

namespace DockWindows.Tests;

public class AtividadeWidgetsTests
{
    private sealed class Relogio : TimeProvider
    {
        public DateTimeOffset Agora = DateTimeOffset.Parse("2026-10-05T12:00:00Z");
        public override DateTimeOffset GetUtcNow() => Agora;
    }
    private sealed class Metricas : IMetricasSistemaService
    {
        public int Leituras, Suspensoes, Descartes;
        public MetricasSistema Ler() { Leituras++; return new(34, 61, 1024, 2048, 20, "100 GB livres"); }
        public void Suspender() => Suspensoes++;
        public void Dispose() => Descartes++;
    }
    private sealed class Bateria : IBateriaService
    {
        public int Leituras;
        public StatusBateria ObterStatus() { Leituras++; return new(true, 61, true, true); }
    }
    private static void Sta(Action action)
    {
        Exception? erro = null;
        var thread = new Thread(() => { try { action(); } catch (Exception e) { erro = e; } });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (erro != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(erro).Throw();
    }
    [Fact]
    public void Gerenciador_DistingueHabilitadoRenderizadoDockEBackground()
    {
        using var g = new GerenciadorAtividade();
        EstadoAtividade atual = default;
        g.Registrar("teste", e => atual = e, () => { });
        g.Definir("teste", true);
        Assert.True(atual.Habilitado); Assert.False(atual.Visual);
        using var visual = g.Renderizar("teste", new object(), true);
        Assert.False(atual.Visual);
        g.DefinirDock(true, true); Assert.True(atual.Visual); Assert.True(atual.Animacoes);
        g.DefinirDock(false, true); Assert.False(atual.Visual); Assert.False(atual.SegundoPlano);
        g.Definir("teste", true, true); Assert.True(atual.SegundoPlano);
        g.Definir("teste", false, true); Assert.False(atual.Visual); Assert.False(atual.SegundoPlano);
    }
    [Fact]
    public void VariosElementos_NaoDuplicamResumeEUnloadedPreservaOutroVisivel()
    {
        using var g = new GerenciadorAtividade(); int resumes = 0;
        g.Registrar("x", e => { if (e.Visual) resumes++; }, () => { });
        g.Definir("x", true); g.DefinirDock(true, true);
        var a = new object(); var b = new object();
        var ra = g.Renderizar("x", a, true); var rb = g.Renderizar("x", b, true);
        Assert.Equal(1, resumes);
        ra.Dispose(); Assert.True(g.Estado("x").Visual);
        g.Visibilidade("x", b, false); Assert.False(g.Estado("x").Visual);
        g.Visibilidade("x", b, true); Assert.Equal(2, resumes);
        rb.Dispose(); Assert.Equal(0, g.TokensVisuais);
    }
    [Fact]
    public void Remocao_DescartaUmaVezETokensAntigosNaoReativam()
    {
        var g = new GerenciadorAtividade(); int dispose = 0;
        g.Registrar("x", _ => { }, () => dispose++);
        var token = new object(); var lease = g.Renderizar("x", token, true);
        g.Remover("x"); g.Visibilidade("x", token, true); lease.Dispose(); g.Dispose();
        Assert.Equal(1, dispose); Assert.Equal(0, g.Componentes); Assert.Equal(0, g.TokensVisuais);
    }
    [Fact]
    public void MonitorEBateria_ParamOcultosEAtualizamImediatamenteAoRetornar() => Sta(() =>
    {
        var fonte = new Metricas(); var bateria = new Bateria();
        var monitor = new MonitorSistemaViewModel(fonte); var vm = new BateriaViewModel(bateria);
        using var g = new GerenciadorAtividade();
        g.Registrar("monitor", monitor.DefinirAtividade, monitor.Dispose);
        g.Registrar("bateria", vm.DefinirAtividade, vm.Dispose);
        g.Definir("monitor", true); g.Definir("bateria", true);
        using var m = g.Renderizar("monitor", new object(), true);
        using var b = g.Renderizar("bateria", new object(), true);
        Assert.Equal(0, fonte.Leituras); Assert.Equal(0, bateria.Leituras);
        g.DefinirDock(true, true);
        Assert.True(monitor.TimerAtivo); Assert.True(vm.TimerAtivo);
        Assert.Equal(1, fonte.Leituras); Assert.Equal(1, bateria.Leituras);
        g.DefinirDock(false, true);
        Assert.False(monitor.TimerAtivo); Assert.False(vm.TimerAtivo);
        g.DefinirDock(true, true);
        Assert.Equal(2, fonte.Leituras); Assert.Equal(2, bateria.Leituras);
        g.Definir("monitor", false); Assert.False(monitor.TimerAtivo); Assert.Empty(monitor.Historico);
    });
    [Fact]
    public void Relogio_FrequenciaConformeEstiloENenhumTimerOculto() => Sta(() =>
    {
        using var clock = new ClockWidgetViewModel();
        Assert.False(clock.TimerAtivo);
        clock.Estilo = "hora"; clock.DefinirAtividade(new(true, true, false, true));
        Assert.True(clock.TimerAtivo); Assert.InRange(clock.IntervaloAtual.TotalSeconds, .001, 60);
        clock.Estilo = "segundos"; Assert.InRange(clock.IntervaloAtual.TotalSeconds, .001, 1);
        clock.Estilo = "data"; Assert.True(clock.IntervaloAtual.TotalSeconds > 60);
        clock.DefinirAtividade(new(true, false, false, false)); Assert.False(clock.TimerAtivo);
        clock.Estilo = "cronometro"; clock.DefinirAtividade(new(true, true, false, true));
        Assert.False(clock.TimerAtivo); clock.AcaoPrincipalCommand.Execute(null); Assert.True(clock.TimerAtivo);
    });
    [Fact]
    public void Pomodoro_ContaTempoAbsolutoSemTicksEPausaCongelaTempo()
    {
        var clock = new Relogio(); var p = new PomodoroEngine(new() { DuracaoFocoMinutos = 2 }, clock);
        int alertas = 0; p.CicloConcluido += (_, _) => alertas++;
        p.Iniciar(); clock.Agora += TimeSpan.FromSeconds(45); Assert.Equal(75, p.SegundosRestantes);
        p.Pausar(); clock.Agora += TimeSpan.FromHours(1); Assert.Equal(75, p.SegundosRestantes);
        p.Iniciar(); clock.Agora += TimeSpan.FromMinutes(5); p.Tick(); p.Tick();
        Assert.Equal(1, alertas); Assert.False(p.EstaExecutando); Assert.Equal(PomodoroEstado.PausaCurta, p.Estado);
    }
    [Fact]
    public void Pomodoro_AtivoContinuaSemWidgetRenderizado() => Sta(() =>
    {
        using var vm = new PomodoroWidgetViewModel(); using var g = new GerenciadorAtividade();
        g.Registrar("p", vm.DefinirAtividade, () => { }, backgroundIndependente: true);
        g.Definir("p", true); vm.IniciarPausarCommand.Execute(null); g.Definir("p", false, true);
        Assert.False(g.Estado("p").Visual); Assert.True(g.Estado("p").SegundoPlano); Assert.True(vm.TimerAtivo);
        vm.ReiniciarCommand.Execute(null); g.Definir("p", false, false); Assert.False(vm.TimerAtivo);
    });
    [Fact]
    public void Midia_DesativadaNaoInicializaListenersNemTimer() => Sta(() =>
    {
        using var vm = new MidiaWidgetViewModel();
        Assert.False(vm.TimerAtivo); Assert.Equal(0, vm.ListenersAtivos);
        vm.DefinirAtividade(default); Assert.False(vm.TimerAtivo); Assert.Equal(0, vm.ListenersAtivos);
    });
    [Fact]
    public void ObservadorMontado_DescobreEstadoSemExigirConteudoVisivel()
    {
        using var g = new GerenciadorAtividade(); g.Registrar("midia", _ => { }, () => { }, observarMontado: true);
        g.Definir("midia", true); g.DefinirDock(true, true);
        using var lease = g.Renderizar("midia", new object(), false);
        Assert.False(g.Estado("midia").Visual); Assert.True(g.Estado("midia").SegundoPlano);
        g.DefinirDock(false, true); Assert.False(g.Estado("midia").SegundoPlano);
    }
    [Fact]
    public void Estabilidade_10000TrocasSemCrescimentoDeRegistrosOuTimers() => Sta(() =>
    {
        var fonte = new Metricas(); var vm = new MonitorSistemaViewModel(fonte);
        using var g = new GerenciadorAtividade(); g.Registrar("monitor", vm.DefinirAtividade, vm.Dispose);
        var token = new object();
        for (int i = 0; i < 10000; i++)
        {
            using var render = g.Renderizar("monitor", token, true);
            g.Definir("monitor", true); g.DefinirDock(true, true); Assert.True(vm.TimerAtivo);
            g.DefinirDock(false, true); Assert.False(vm.TimerAtivo);
            g.Definir("monitor", false);
        }
        Assert.Equal(1, g.Componentes); Assert.Equal(0, g.TokensVisuais);
        Assert.Equal(10000, fonte.Leituras); Assert.False(vm.TimerAtivo);
        g.Remover("monitor"); Assert.Equal(1, fonte.Descartes);
    });

    [Fact]
    public void Estabilidade_DispatcherDuranteDoisMinutos_PausaSemLeiturasOcultas() => Sta(() =>
    {
        var fonte = new Metricas(); var vm = new MonitorSistemaViewModel(fonte);
        using var g = new GerenciadorAtividade(); g.Registrar("monitor", vm.DefinirAtividade, vm.Dispose);
        using var visual = g.Renderizar("monitor", new object(), true); g.Definir("monitor", true);
        var frame = new System.Windows.Threading.DispatcherFrame();
        var tempo = System.Diagnostics.Stopwatch.StartNew();
        int ciclos = 0, leiturasOculto = 0; bool visivel = false;
        Exception? falha = null;
        var alternar = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        alternar.Tick += (_, _) =>
        {
            try
            {
                if (!visivel) Assert.Equal(leiturasOculto, fonte.Leituras);
                visivel = !visivel; g.DefinirDock(visivel, true); ciclos++;
                Assert.Equal(visivel, vm.TimerAtivo); Assert.Equal(1, g.TokensVisuais); Assert.Equal(1, g.Componentes);
                if (!visivel) leiturasOculto = fonte.Leituras;
                Assert.InRange(vm.Historico.Count, 0, 30);
                if (tempo.Elapsed >= TimeSpan.FromMinutes(2)) frame.Continue = false;
            }
            catch (Exception ex) { falha = ex; frame.Continue = false; }
        };
        alternar.Start(); System.Windows.Threading.Dispatcher.PushFrame(frame); alternar.Stop();
        g.DefinirDock(false, true); Assert.False(vm.TimerAtivo);
        if (falha != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(falha).Throw();
        Assert.True(ciclos >= 24); Assert.True(fonte.Leituras > 12);
    });
}
