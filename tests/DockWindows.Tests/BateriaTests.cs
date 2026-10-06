using System.Text.Json;
using DockWindows.App.ViewModels;
using DockWindows.Core.Models;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public class BateriaTests
{
    private sealed class FakeService(StatusBateria status) : IBateriaService
    {
        public StatusBateria ObterStatus() => status;
    }

    [Theory]
    [InlineData(128, 255, false, null)]
    [InlineData(255, 255, null, null)]
    [InlineData(0, 0, true, 0)]
    [InlineData(8, 61, true, 61)]
    public void StatusNativo_InterpretaAusenciaEDadosDesconhecidos(byte flag, byte carga, bool? possui, int? porcentagem)
    {
        var status = BateriaService.Interpretar(flag, carga, 255);
        Assert.Equal(possui, status.PossuiBateria);
        Assert.Equal(porcentagem, status.Porcentagem);
        Assert.Null(status.NaTomada);
        Assert.Equal(flag == 8, status.Carregando);
    }

    [Fact]
    public void Indicador_ExibeCargaRealECarregamento()
    {
        using var vm = new BateriaViewModel(new FakeService(new(true, 61, true, true)));
        vm.Habilitado = true;
        vm.DefinirAtividade(new(true, true, false, true));
        Assert.Equal("61%", vm.Porcentagem);
        Assert.Equal(10.98, vm.LarguraCarga, 2);
        Assert.True(vm.Carregando);
        Assert.Contains("Carregando", vm.Descricao);
        vm.Habilitado = false;
        Assert.False(vm.Habilitado);
    }

    [Fact]
    public void SemBateria_NaoInventaPorcentagem()
    {
        using var vm = new BateriaViewModel(new FakeService(new(false, null, false, true)));
        vm.Atualizar();
        Assert.Equal("—", vm.Porcentagem);
        Assert.Equal(0, vm.LarguraCarga);
        Assert.Contains("não possui bateria", vm.Descricao);
    }

    [Fact]
    public void Preferencia_PersisteESuportaJsonAntigo()
    {
        Assert.False(JsonSerializer.Deserialize<Preferencias>("{}")!.ExibirBateria);
        var prefs = new Preferencias { ExibirBateria = true };
        Assert.True(JsonSerializer.Deserialize<Preferencias>(JsonSerializer.Serialize(prefs))!.ExibirBateria);
    }

    [Fact]
    public void ConsultaNativa_SomenteLeitura_RetornaCargaValidaOuDesconhecida()
    {
        var status = new BateriaService().ObterStatus();
        Assert.True(status.Porcentagem == null || status.Porcentagem is >= 0 and <= 100);
        if (status.PossuiBateria == false) Assert.Null(status.Porcentagem);
    }
}