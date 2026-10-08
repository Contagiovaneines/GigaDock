namespace DockWindows.App.Common;

public static class PokemonBehavior
{
    public static string StateFor(int pokemonId, TimeSpan sinceSelection, TimeSpan idle)
    {
        // Snorlax cochila por 30 segundos a cada dois minutos desde a escolha.
        if (pokemonId == 143 && sinceSelection >= TimeSpan.FromMinutes(2)
            && sinceSelection.TotalSeconds % 120 < 30) return "Dormindo";
        return idle > TimeSpan.FromMinutes(2) ? "Descansando" : "Acompanhando você";
    }
}
