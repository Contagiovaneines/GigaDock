using DockWindows.App.Common;

namespace DockWindows.Tests;

public class PokemonBehaviorTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(119, false)]
    [InlineData(120, true)]
    [InlineData(149, true)]
    [InlineData(150, false)]
    [InlineData(239, false)]
    [InlineData(240, true)]
    [InlineData(270, false)]
    public void SnorlaxDormeTrintaSegundosACadaDoisMinutos(int seconds, bool sleeping)
        => Assert.Equal(sleeping, PokemonBehavior.StateFor(143, TimeSpan.FromSeconds(seconds), TimeSpan.Zero) == "Dormindo");

    [Fact]
    public void DemaisEspeciesNuncaDormemMesmoComWindowsInativo()
    {
        for (var id = 1; id <= 151; id++)
        {
            if (id == 143) continue;
            Assert.Equal("Descansando", PokemonBehavior.StateFor(id, TimeSpan.FromMinutes(2), TimeSpan.FromHours(3)));
            Assert.Equal("Acompanhando você", PokemonBehavior.StateFor(id, TimeSpan.FromMinutes(2), TimeSpan.Zero));
        }
    }
}
