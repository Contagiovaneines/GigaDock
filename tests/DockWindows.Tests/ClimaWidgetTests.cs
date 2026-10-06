using DockWindows.App.ViewModels;

namespace DockWindows.Tests;

public class ClimaWidgetTests
{
    [Fact]
    public void VentoChuvaESol_InterpretamDadosReaisEAusencias()
    {
        var dados = ClimaWidgetViewModel.InterpretarResposta("""
        {"current_condition":[{"temp_C":"21","weatherCode":"119","windspeedKmph":"18","winddir16Point":"SW","precipMM":"0.4","localObsDateTime":"2026-10-05 11:00 AM"}],
         "weather":[{"date":"2026-10-05","maxtempC":"24","hourly":[],"astronomy":[{"sunrise":"06:12 AM","sunset":"08:18 PM"}]}]}
        """);
        Assert.Equal("18 km/h", dados.Vento);
        Assert.Equal("SO", dados.DirecaoVento);
        Assert.Equal("0,4 mm", dados.Precipitacao);
        Assert.Equal("06:12", dados.NascerSol);
        Assert.Equal("20:18", dados.PorSol);
        Assert.Equal("Nuvem", dados.TipoAtual);
        var vm = new ClimaWidgetViewModel(iniciarConsulta: false);
        vm.AplicarDados(dados);
        Assert.Equal(dados.Observacao, vm.Observacao);
        Assert.Contains("18 km/h", vm.DescricaoDados);
        var vazio = ClimaWidgetViewModel.InterpretarResposta("""
        {"current_condition":[{"temp_C":"21","weatherCode":"119","windspeedKmph":"-1","precipMM":"erro"}],"weather":[]}
        """);
        vm.AplicarDados(vazio);
        Assert.Equal("—", vm.Vento);
        Assert.Equal("—", vm.Precipitacao);
        Assert.Equal("—", vm.NascerSol);
        Assert.Equal("—", vm.PorSol);
        Assert.Null(vm.Observacao);
    }
    [Theory]
    [InlineData("12:00 AM", "00:00")]
    [InlineData("12:00 PM", "12:00")]
    [InlineData("No sunrise", "—")]
    public void Sol_ConverteHorarioSemInventarEvento(string horario, string esperado)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            current_condition = new[] { new { temp_C = "21", weatherCode = "113" } },
            weather = new[] { new { date = "2026-10-05", maxtempC = "24", hourly = Array.Empty<object>(), astronomy = new[] { new { sunrise = horario, sunset = horario } } } }
        });
        var dados = ClimaWidgetViewModel.InterpretarResposta(json);
        Assert.Equal(esperado, dados.NascerSol);
        Assert.Equal(esperado, dados.PorSol);
    }
    [Fact]
    public void Detalhes_UsamSensacaoMinimaEHorasDepoisDaObservacao()
    {
        var dados = ClimaWidgetViewModel.InterpretarResposta("""
        {
          "current_condition": [{"temp_C":"26","weatherCode":"113","FeelsLikeC":"27","localObsDateTime":"2026-10-05 11:00 AM"}],
          "weather": [{"date":"2026-10-05","mintempC":"18","maxtempC":"29","hourly":[
            {"time":"900","tempC":"22","weatherCode":"113"},
            {"time":"1200","tempC":"26","weatherCode":"113"},
            {"time":"1500","tempC":"29","weatherCode":"119"}]}]
        }
        """);
        Assert.Equal("27°", dados.Sensacao);
        Assert.Equal("18°", dados.Previsoes[0].Minima);
        Assert.Equal(new[] { "12h", "15h" }, dados.Horas!.Select(h => h.Hora));
        Assert.Equal(new[] { "26°", "29°" }, dados.Horas!.Select(h => h.Temperatura));
    }
    [Fact]
    public void RespostaJson_UsaCelsiusDatasEmPortuguesEMaximas()
    {
        var dados = ClimaWidgetViewModel.InterpretarResposta("""
        {
          "current_condition": [{"temp_C": "26", "weatherCode": "113"}],
          "nearest_area": [{"areaName": [{"value": "São Paulo"}]}],
          "weather": [
            {"date":"2026-10-05","maxtempC":"30","hourly":[
              {"time":"0","weatherCode":"113"},{"time":"1200","weatherCode":"296"}]},
            {"date":"2026-10-06","maxtempC":"29","hourly":[{"time":"1200","weatherCode":"119"}]},
            {"date":"2026-10-07","maxtempC":"28","hourly":[{"time":"1200","weatherCode":"113"}]}
          ]
        }
        """);
        Assert.Equal("26°", dados.Temperatura);
        Assert.Equal("São Paulo", dados.Local);
        Assert.Equal(new[] { "seg", "ter", "qua" }, dados.Previsoes.Select(p => p.Dia));
        Assert.Equal(new[] { "30°", "29°", "28°" }, dados.Previsoes.Select(p => p.Temperatura));
        Assert.Equal(new[] { "Chuva", "Nuvem", "Sol" }, dados.Previsoes.Select(p => p.TipoIcone));
    }

    [Fact]
    public void RespostaParcial_PreencheDiasAusentesSemInventarTemperaturas()
    {
        var dados = ClimaWidgetViewModel.InterpretarResposta("""
        {"current_condition":[{"temp_C":"-3","weatherCode":"999"}],
         "weather":[{"date":"2026-10-05","maxtempC":"0","hourly":[]}]}
        """);
        Assert.Equal("-3°", dados.Temperatura);
        Assert.Equal(3, dados.Previsoes.Count);
        Assert.Equal("Indisponivel", dados.Previsoes[0].TipoIcone);
        Assert.Equal("—°", dados.Previsoes[1].Temperatura);
        Assert.Equal("—°", dados.Previsoes[2].Temperatura);
    }

    [Fact]
    public void RespostaInvalida_NaoETratadaComoPrevisao()
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() =>
            ClimaWidgetViewModel.InterpretarResposta("Serviço indisponível"));
    }

    private sealed class RelogioTeste : TimeProvider
    {
        public DateTimeOffset Agora = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Agora;
    }
    private sealed class RespostaTeste : System.Net.Http.HttpMessageHandler
    {
        public int Consultas;
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken token)
        {
            Consultas++;
            return Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            { Content = new System.Net.Http.StringContent("""{"current_condition":[{"temp_C":"21","weatherCode":"113"}],"weather":[]}""") });
        }
    }
    [Fact]
    public void Visibilidade_CacheTtlEConfiguracaoNaoConsultamOculto()
    {
        Exception? erro = null;
        var thread = new Thread(() =>
        {
            try
            {
                var relogio = new RelogioTeste(); var resposta = new RespostaTeste();
                using var vm = new ClimaWidgetViewModel(http: new System.Net.Http.HttpClient(resposta), relogio: relogio);
                Assert.Equal(0, resposta.Consultas);
                vm.DefinirAtividade(new(true, true, false, true));
                Assert.Equal(1, resposta.Consultas); Assert.Equal("21°", vm.Temperatura);
                vm.DefinirAtividade(default); Assert.False(vm.TimerAtivo);
                vm.DefinirAtividade(new(true, true, false, true)); Assert.Equal(1, resposta.Consultas);
                vm.DefinirAtividade(default); relogio.Agora += TimeSpan.FromHours(2);
                Assert.Equal(1, resposta.Consultas);
                vm.DefinirAtividade(new(true, true, false, true)); Assert.Equal(2, resposta.Consultas);
                vm.DefinirAtividade(default); vm.SincronizarLocalizacao("Recife");
                Assert.Equal(2, resposta.Consultas);
                vm.DefinirAtividade(new(true, true, false, true)); Assert.Equal(3, resposta.Consultas);
            }
            catch (Exception ex) { erro = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        if (erro != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(erro).Throw();
    }
}
