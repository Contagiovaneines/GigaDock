using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;
using Xunit;

namespace DockWindows.Tests;

public class EstilosSistemaTests
{
    private sealed class MetricasFake : IMetricasSistemaService
    {
        public MetricasSistema Leitura = new(34, 61, 4 * 1024 * 1024, 312 * 1024, 75, "128 GB livres");
        public MetricasSistema Ler() => Leitura;
        public void Dispose() { }
    }
    [Fact]
    public void Monitor_UsaDadosDoServicoHistoricoLimitadoEEstadoIndisponivel()
    {
        var fonte = new MetricasFake();
        using var vm = new MonitorSistemaViewModel(fonte) { Estilo = "cpu-grafico" };
        for (int i = 0; i < 35; i++) vm.AtualizarMetricas();
        Assert.Equal(30, vm.Historico.Count);
        Assert.All(vm.Historico, v => Assert.Equal(34, v));
        vm.Estilo = "ram";
        Assert.Empty(vm.Historico);
        Assert.Equal("61%", vm.TextoValor);
        vm.Estilo = "rede-grafico"; vm.AtualizarMetricas();
        Assert.Contains("MB/s", vm.TextoValor);
        Assert.Equal(312 * 1024, Assert.Single(vm.HistoricoSecundario));
        fonte.Leitura = new(null, null, null, null, null, "—");
        vm.Estilo = "cpu"; vm.AtualizarMetricas();
        Assert.Equal("—", vm.TextoValor);
        Assert.Empty(vm.Historico);
    }
    [Theory]
    [InlineData(100, 300, 2, 100)]
    [InlineData(100, 50, 2, 0)]
    [InlineData(100, 300, 0, 0)]
    public void Rede_TaxaNaoConfundeTotalComVelocidade(long anterior, long atual, double segundos, double esperado)
        => Assert.Equal(esperado, MetricasSistemaService.Taxa(anterior, atual, segundos));
    [Theory]
    [InlineData("https://meet.google.com/abc", true)]
    [InlineData("file:///C:/teste.exe", false)]
    [InlineData("https://user:password@example.com/", false)]
    [InlineData("Sala 4", false)]
    public void Reuniao_ValidaLinkSemExecutar(string local, bool valido)
        => Assert.Equal(valido, CalendarioWidgetViewModel.LinkReuniao(new CompromissoLocal { Local = local }) != null);
    [Fact]
    public void Reuniao_NaoMostraEventoPassadoComoProximaReuniao()
    {
        var vm = new CalendarioWidgetViewModel();
        vm.SincronizarCompromissos(new[] { new CompromissoLocal { Titulo = "Passado", DataHora = DateTime.Now.AddHours(-2) }, new CompromissoLocal { Titulo = "Próxima", DataHora = DateTime.Now.AddMinutes(8), Local = "https://example.com/" } });
        Assert.Equal("Próxima", vm.TituloReuniao);
        Assert.True(vm.TemLinkReuniao);
        Assert.StartsWith("Começa em", vm.ContagemReuniao);
    }
}
