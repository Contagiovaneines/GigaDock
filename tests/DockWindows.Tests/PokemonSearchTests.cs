using DockWindows.App.ViewModels;

namespace DockWindows.Tests;

public class PokemonSearchTests
{
    [Theory]
    [InlineData("pikachu", 25)]
    [InlineData("  PIKÁCHU  ", 25)]
    [InlineData("25", 25)]
    [InlineData("#025", 25)]
    [InlineData("Mr Mime", 122)]
    [InlineData("Farfetch'd", 83)]
    [InlineData("Nidoran♀", 29)]
    [InlineData("Nidoran♂", 32)]
    public void BuscaEncontraPokemonEspecifico(string busca, int id)
    {
        var resultado = PokemonOpcao.CriarOriginais().Where(p => p.CorrespondeBusca(busca));
        Assert.Equal(id, Assert.Single(resultado).Id);
    }

    [Theory]
    [InlineData("", 151)]
    [InlineData("  ", 151)]
    [InlineData("saur", 3)]
    [InlineData("nidoran", 2)]
    [InlineData("152", 0)]
    [InlineData("inexistente", 0)]
    public void BuscaFiltraCatalogo(string busca, int quantidade)
    {
        Assert.Equal(quantidade, PokemonOpcao.CriarOriginais().Count(p => p.CorrespondeBusca(busca)));
    }
}
