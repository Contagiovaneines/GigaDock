using System.Text.Json;
using DockWindows.Core.Models;
using Xunit;

namespace DockWindows.Tests;

public class ModoAberturaPaineisTests
{
    [Fact]
    public void ConfiguracaoAntiga_PreservaAberturaPorClique()
    {
        var ambiente = JsonSerializer.Deserialize<Ambiente>("{\"Nome\":\"Trabalho\"}");
        Assert.Equal("Clique", ambiente!.ModoAberturaPaineis);
    }

    [Fact]
    public void Persistencia_MantemModosIndependentesPorAmbiente()
    {
        var ambientes = new[] {
            new Ambiente { Nome = "Trabalho", ModoAberturaPaineis = "Mouse" },
            new Ambiente { Nome = "Pessoal", ModoAberturaPaineis = "Clique" }
        };
        var restaurados = JsonSerializer.Deserialize<Ambiente[]>(JsonSerializer.Serialize(ambientes))!;
        Assert.Equal("Mouse", restaurados[0].ModoAberturaPaineis);
        Assert.Equal("Clique", restaurados[1].ModoAberturaPaineis);
    }
}
