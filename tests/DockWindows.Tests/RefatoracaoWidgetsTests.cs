using DockWindows.Core.Widgets;
using DockWindows.App.ViewModels;
using DockWindows.Infrastructure.Windows;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Windows.Threading;

namespace DockWindows.Tests;

public class RefatoracaoWidgetsTests
{
    private sealed class Relogio : TimeProvider
    {
        public DateTimeOffset Agora = DateTimeOffset.Parse("2026-10-06T12:00:00Z");
        public override DateTimeOffset GetUtcNow() => Agora;
    }
    [Fact]
    public void Temporizador_SemTicksOcultoSuspensoConcluiUmaVezEPausaCongela()
    {
        var tempo = new Relogio(); var t = new TemporizadorEngine(TimeSpan.FromMinutes(5), tempo);
        int conclusoes = 0; t.Concluido += () => conclusoes++;
        t.Alternar(); tempo.Agora += TimeSpan.FromMinutes(2); t.Alternar();
        tempo.Agora += TimeSpan.FromHours(8); Assert.Equal(TimeSpan.FromMinutes(3), t.Restante);
        t.Alternar(); tempo.Agora += TimeSpan.FromHours(1); t.Tick(); t.Tick();
        Assert.Equal(1, conclusoes); Assert.False(t.EstaExecutando); Assert.Equal(TimeSpan.Zero, t.Restante);
        t.Reiniciar(); Assert.Equal(TimeSpan.FromMinutes(5), t.Restante);
    }
    [Fact]
    public void Gerenciador_SuspensaoPreservaIntencaoEReativaBackgroundSemDuplicar()
    {
        using var g = new GerenciadorAtividade(); int aplicacoes = 0;
        g.Registrar("p", _ => aplicacoes++, () => { }, backgroundIndependente: true);
        g.Definir("p", false, true); Assert.True(g.Diagnostico("p").Executando);
        g.Suspender(true); Assert.True(g.Diagnostico("p").Pausado); Assert.False(g.Estado("p").SegundoPlano);
        var n = aplicacoes; g.Suspender(true); Assert.Equal(n, aplicacoes);
        g.Suspender(false); Assert.True(g.Estado("p").SegundoPlano);
        g.DefinirInstalado("p", false); Assert.False(g.Diagnostico("p").Executando);
        g.InformarSaude("p", SaudeWidget.Erro, "Falha simulada"); Assert.Equal(SaudeWidget.Erro, g.Diagnostico("p").Saude);
    }
    [Fact]
    public void Gerenciador_LeaseAntigoEDisposeNaoReativamEntradaNova()
    {
        var g = new GerenciadorAtividade(); var token = new object();
        int antigo = 0, novo = 0;
        g.Registrar("x", _ => antigo++, () => { }); var lease = g.Renderizar("x", token, true);
        g.Remover("x"); var totalAntigo = antigo;
        g.Registrar("x", _ => novo++, () => { }); g.Definir("x", true); g.DefinirDock(true, true);
        using var novoLease = g.Renderizar("x", new object(), true);
        lease.Dispose(); Assert.Equal(totalAntigo, antigo); Assert.True(g.Estado("x").Visual);
        g.Dispose(); Assert.True(g.Diagnostico("x").Destruido);
        Assert.Throws<ObjectDisposedException>(() => g.Registrar("z", _ => { }, () => { }));
    }
    private static string Ics(string evento) => "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//GigaDock//Tests//PT\r\nBEGIN:VEVENT\r\nUID:teste\r\nSUMMARY:Reunião\r\n" + evento + "\r\nEND:VEVENT\r\nEND:VCALENDAR";
    private static IReadOnlyList<DockWindows.Core.Models.CompromissoLocal> Importar(string evento, TimeZoneInfo? zona = null)
        => ImportadorCalendario.Importar(Ics(evento), new DateTime(2026, 10, 1), new DateTime(2026, 11, 1), zona ?? TimeZoneInfo.Utc);
    [Fact]
    public void Calendario_UtcConverteUmaVezEFloatingPermaneceLocal()
    {
        var zona = TimeZoneInfo.CreateCustomTimeZone("Teste-3", TimeSpan.FromHours(-3), "Teste", "Teste");
        Assert.Equal(9, Assert.Single(Importar("DTSTART:20261006T120000Z", zona)).DataHora.Hour);
        Assert.Equal(12, Assert.Single(Importar("DTSTART:20261006T120000", zona)).DataHora.Hour);
    }
    [Fact]
    public void Calendario_TzidDiaInteiroRecorrenciaEExcecao()
    {
        Assert.Equal(12, Assert.Single(Importar("DTSTART;TZID=America/Sao_Paulo:20261006T090000")).DataHora.Hour);
        var dia = Assert.Single(Importar("DTSTART;VALUE=DATE:20261006"));
        Assert.True(dia.DiaInteiro); Assert.Equal(new DateTime(2026, 10, 6), dia.DataHora);
        var recorrentes = Importar("DTSTART:20261006T090000Z\r\nRRULE:FREQ=DAILY;COUNT=3\r\nEXDATE:20261007T090000Z");
        Assert.Equal(new[] { 6, 8 }, recorrentes.Select(c => c.DataHora.Day));
    }
    [Fact]
    public void Calendario_RecorrenciaMantemHorarioDoFusoNaMudancaDeHorarioDeVerao()
    {
        var eventos = Importar("DTSTART;TZID=Europe/Zurich:20261019T090000\r\nRRULE:FREQ=WEEKLY;COUNT=2");
        Assert.Equal(new[] { 7, 8 }, eventos.Select(c => c.DataHora.Hour));
    }
    [Fact]
    public void Calendario_FusoDesconhecidoNaoViraUtcSilenciosamente()
        => Assert.ThrowsAny<Exception>(() => Importar("DTSTART;TZID=Fuso/Inexistente:20261006T090000"));
    [Fact]
    public void Notas_SalvaAoDesativarRecarregaENaoReativaDepoisDeDispose() => Sta(() =>
    {
        var arquivo = Path.Combine(Path.GetTempPath(), "gigadock-notas-test-" + Guid.NewGuid() + ".txt");
        try
        {
            using (var notas = new NotasWidgetViewModel(arquivo))
            { notas.TextoNotas = "Primeira linha\nSegunda linha"; Assert.Equal(2, notas.QuantidadeLinhas); notas.DefinirAtividade(default); }
            using var carregadas = new NotasWidgetViewModel(arquivo);
            Assert.Equal("Primeira linha\nSegunda linha", carregadas.TextoNotas);
            carregadas.Dispose(); carregadas.TextoNotas = "Não salvar depois de destruir";
            Assert.Equal("Primeira linha\nSegunda linha", File.ReadAllText(arquivo));
        }
        finally { File.Delete(arquivo); File.Delete(arquivo + ".tmp"); }
    });
    private sealed class Metricas : IMetricasSistemaService
    {
        public int Leituras;
        public MetricasSolicitadas Solicitadas;
        public MetricasSistema Ler() => throw new InvalidOperationException("Deve declarar as métricas necessárias.");
        public MetricasSistema Ler(MetricasSolicitadas solicitadas)
        { Leituras++; Solicitadas = solicitadas; return new(null, 0, null, null, null, "—"); }
        public void Dispose() { }
    }
    [Fact]
    public void Notas_FalhaDeGravacaoPreservaTextoEPermiteNovaTentativa() => Sta(() =>
    {
        var basePath = Path.GetTempFileName();
        var arquivo = Path.Combine(basePath, "notas.txt");
        try
        {
            using var notas = new NotasWidgetViewModel(arquivo);
            notas.TextoNotas = "Preservar esta anotação"; notas.DefinirAtividade(default);
            Assert.NotEmpty(notas.ErroPersistencia); Assert.Equal(SaudeWidget.Erro, notas.Saude);
            Assert.Equal("Preservar esta anotação", notas.TextoNotas);
            File.Delete(basePath); Directory.CreateDirectory(basePath);
            notas.DefinirAtividade(default);
            Assert.Empty(notas.ErroPersistencia); Assert.Equal("Preservar esta anotação", File.ReadAllText(arquivo));
        }
        finally
        {
            if (Directory.Exists(basePath)) { File.Delete(arquivo); File.Delete(arquivo + ".tmp"); Directory.Delete(basePath); }
            else File.Delete(basePath);
        }
    });
    [Fact]
    public void Monitor_ColetaApenasConsumidoresNecessariosEDistingueZeroDeIndisponivel() => Sta(() =>
    {
        var fonte = new Metricas(); using var monitor = new MonitorSistemaViewModel(fonte) { Estilo = "ram" };
        monitor.DefinirAtividade(new(true, true, false, false));
        Assert.Equal(MetricasSolicitadas.Ram, fonte.Solicitadas); Assert.Equal("0%", monitor.TextoValor);
        monitor.Estilo = "cpu"; monitor.AtualizarMetricas(); Assert.Equal("—", monitor.TextoValor);
        monitor.Estilo = "rede"; monitor.AtualizarMetricas(); Assert.Equal(MetricasSolicitadas.Rede, fonte.Solicitadas);
        monitor.Dispose(); var n = fonte.Leituras; monitor.DefinirAtividade(new(true, true, false, true)); Assert.False(monitor.TimerAtivo); Assert.Equal(n, fonte.Leituras);
    });
    [Fact]
    public void Relogio_TemporizadorMantemUmDespertarOcultoEPausaColetaNaSuspensao() => Sta(() =>
    {
        using var relogio = new ClockWidgetViewModel { Estilo = "temporizador" };
        relogio.DefinirAtividade(new(true, true, false, true)); relogio.AcaoPrincipalCommand.Execute(null);
        Assert.True(relogio.EstaExecutando);
        relogio.DefinirAtividade(new(true, false, true, false)); Assert.True(relogio.TimerAtivo);
        Assert.InRange(relogio.IntervaloAtual.TotalSeconds, 299, 300);
        relogio.DefinirAtividade(new(true, false, false, false)); Assert.False(relogio.TimerAtivo); Assert.True(relogio.EstaExecutando);
        relogio.Dispose(); relogio.DefinirAtividade(new(true, true, true, true)); Assert.False(relogio.TimerAtivo);
    });
    [Theory]
    [InlineData(8, 50, 1, EstadoCargaBateria.Carregando)]
    [InlineData(1, 50, 0, EstadoCargaBateria.Descarregando)]
    [InlineData(1, 100, 1, EstadoCargaBateria.Completa)]
    [InlineData(128, 255, 1, EstadoCargaBateria.SemBateria)]
    [InlineData(1, 255, 255, EstadoCargaBateria.Desconhecida)]
    [InlineData(255, 255, 255, EstadoCargaBateria.Desconhecida)]
    public void Bateria_DistingueEstadosSemInventarZero(byte flags, byte carga, byte tomada, EstadoCargaBateria esperado)
        => Assert.Equal(esperado, BateriaService.Interpretar(flags, carga, tomada).Estado);
    [Fact]
    public void Bateria_FalhaDeApiDiferenteDeFlagsDesconhecidas()
    {
        Assert.Equal(EstadoCargaBateria.Indisponivel, new StatusBateria(null, null, false, null, false).Estado);
        Assert.Equal(EstadoCargaBateria.Desconhecida, BateriaService.Interpretar(255, 255, 255).Estado);
    }

    private static void Sta(Action acao)
    {
        Exception? erro = null;
        var thread = new Thread(() => { try { acao(); } catch (Exception ex) { erro = ex; } });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (erro != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(erro).Throw();
    }
    [Fact]
    public void Integracoes_NaoConfirmamGravacaoOuParticipantesFicticios() => Sta(() =>
    {
        using var obs = new ObsWidgetViewModel(); obs.AlternarGravacaoCommand.Execute(null);
        Assert.False(obs.EstaGravando); Assert.Equal(SaudeWidget.Indisponivel, obs.Saude);
        using var discord = new DiscordWidgetViewModel(); discord.DefinirAtividade(new(true, true, false, true));
        Assert.False(discord.EstaEmCall); Assert.Empty(discord.UsuariosNaCall);
        discord.Dispose(); discord.DefinirAtividade(new(true, true, false, true)); Assert.Empty(discord.UsuariosNaCall);
    });
    private static void Aguardar(Func<bool> concluido)
    {
        if (concluido()) return;
        var frame = new DispatcherFrame(); var limite = DateTime.UtcNow.AddSeconds(5);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (concluido() || DateTime.UtcNow >= limite) frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame); timer.Stop(); Assert.True(concluido(), "Operação não concluiu em 5 segundos.");
    }
    private sealed class Rede : HttpMessageHandler
    {
        public int Chamadas;
        public bool Offline;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Chamadas++; if (Offline) throw new HttpRequestException("offline simulado"); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"current_condition\":[{\"temp_C\":\"21\",\"weatherCode\":\"113\"}],\"weather\":[]}") }); }
    }
    [Fact]
    public void Clima_OnlineOfflineOnlinePreservaDadosERespeitaCacheEPreview() => Sta(() =>
    {
        var tempo = new Relogio(); var rede = new Rede();
        using var clima = new ClimaWidgetViewModel(http: new HttpClient(rede), relogio: tempo);
        clima.DefinirAtividade(new(true, true, false, true)); Assert.Equal("21°", clima.Temperatura);
        clima.DefinirAtividade(new(true, false, false, false)); clima.DefinirAtividade(new(true, true, false, true));
        Assert.Equal(1, rede.Chamadas);
        tempo.Agora += TimeSpan.FromHours(2); rede.Offline = true;
        clima.DefinirAtividade(new(true, true, false, true)); Aguardar(() => !string.IsNullOrEmpty(clima.ErroAtualizacao)); Assert.True(clima.DadosDesatualizados); Assert.Equal("21°", clima.Temperatura);
        tempo.Agora += TimeSpan.FromMinutes(1); rede.Offline = false;
        clima.DefinirAtividade(new(true, true, false, true)); Aguardar(() => string.IsNullOrEmpty(clima.ErroAtualizacao)); Assert.False(clima.DadosDesatualizados);
        clima.Dispose(); var total = rede.Chamadas; clima.DefinirAtividade(new(true, true, false, true)); Assert.Equal(total, rede.Chamadas);
        using var preview = new ClimaWidgetViewModel(false, new HttpClient(rede), tempo);
        preview.DefinirAtividade(new(true, true, false, true)); Assert.Equal(total, rede.Chamadas); Assert.False(preview.TimerAtivo);
    });
}
