namespace DockWindows.App.Common;

public static class EeveeEvolution
{
    public static int ForWeather(string? weather)
    {
        var climate = (weather ?? string.Empty).ToLowerInvariant();
        if (new[] { "tempest", "thunder", "trovo", "raio" }.Any(climate.Contains)) return 135;
        if (new[] { "chuva", "rain" }.Any(climate.Contains)) return 134;
        if (new[] { "sol", "sun", "calor", "clear" }.Any(climate.Contains)) return 136;
        return 134;
    }
}
