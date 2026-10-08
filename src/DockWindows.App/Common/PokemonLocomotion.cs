namespace DockWindows.App.Common;

public enum PokemonMovementKind { Ground, Flying, Floating }

/// <summary>Comportamento visual por espécie; Doduo/Dodrio permanecem no chão.</summary>
public static class PokemonLocomotion
{
    public static PokemonMovementKind KindFor(int id) => id switch
    {
        6 or 12 or 15 or 16 or 17 or 18 or 21 or 22 or 41 or 42 or 49 or 83 or 123 or 142 or 144 or 145 or 146 or 149 => PokemonMovementKind.Flying,
        81 or 82 or 92 or 93 or 109 or 110 or 137 or 148 or 151 => PokemonMovementKind.Floating,
        _ => PokemonMovementKind.Ground
    };

    public static string AnimationFor(int id) => id switch
    {
        6 or 83 or 123 or 144 or 145 => "Idle",
        15 or 21 or 22 or 41 or 42 or 142 or 146 => "Hover",
        16 or 151 => "Float",
        17 or 18 => "FlapAround",
        149 => "Special0",
        _ => "Walk"
    };

    public static double Altitude(PokemonMovementKind kind, double milliseconds, bool sleeping, bool animate)
    {
        if (kind == PokemonMovementKind.Ground || sleeping) return 0;
        var baseHeight = kind == PokemonMovementKind.Flying ? 9d : 6d;
        return baseHeight + (animate ? Math.Sin(milliseconds * Math.PI * 2 / 2400) * 2 : 0);
    }
}
