using DockWindows.App.Common;

namespace DockWindows.Tests;

public class EeveeEvolutionTests
{
    [Theory]
    [InlineData("Chuva", 134)]
    [InlineData("Rain", 134)]
    [InlineData("Tempestade com chuva", 135)]
    [InlineData("Trovoadas", 135)]
    [InlineData("Thunderstorm", 135)]
    [InlineData("Sol", 136)]
    [InlineData("Clear sky", 136)]
    [InlineData("Calor", 136)]
    [InlineData(null, 134)]
    public void FormaDependeDoClimaComPrioridadeParaTempestade(string? climate, int expected)
        => Assert.Equal(expected, EeveeEvolution.ForWeather(climate));
}
