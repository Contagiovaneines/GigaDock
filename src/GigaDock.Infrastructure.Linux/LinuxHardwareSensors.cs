using System.Globalization;
using System.Text.RegularExpressions;

namespace GigaDock.Infrastructure.Linux;

public sealed record HardwareSensor(string Chip, string Label, bool IsTemperature, double Value)
{
    public string Formatted => IsTemperature ? $"{Value.ToString("0.#", CultureInfo.CurrentCulture)} °C" : $"{Value:0} RPM";
}

/// <summary>Read-only hwmon provider. No probes, privileged commands or fan changes.</summary>
public sealed class LinuxHardwareSensors(string root = "/sys/class/hwmon")
{
    public IReadOnlyList<HardwareSensor> Read()
    {
        var sensors = new List<HardwareSensor>();
        try
        {
            if (!Directory.Exists(root)) return sensors;
            foreach (var directory in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal).Take(32))
            {
                try
                {
                    var chip = ReadText(Path.Combine(directory, "name")) ?? Path.GetFileName(directory);
                    foreach (var file in Directory.EnumerateFiles(directory, "*_input").Order(StringComparer.Ordinal).Take(128))
                    {
                        var match = Regex.Match(Path.GetFileName(file), "^(temp|fan)([0-9]+)_input$", RegexOptions.CultureInvariant);
                        if (!match.Success) continue;
                        var raw = ReadText(file);
                        if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) continue;
                        var temperature = match.Groups[1].Value == "temp";
                        var value = temperature ? number / 1000d : number;
                        if (temperature ? value is < -273.15 or > 250 : value is < 0 or > 1_000_000) continue;
                        var key = match.Groups[1].Value + match.Groups[2].Value;
                        var label = ReadText(Path.Combine(directory, key + "_label")) ?? (temperature ? "Temperatura " : "Ventoinha ") + match.Groups[2].Value;
                        sensors.Add(new(chip, label, temperature, value));
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        return sensors;
    }

    public static string Summary(IReadOnlyList<HardwareSensor> sensors)
    {
        var hottest = sensors.Where(s => s.IsTemperature).OrderByDescending(s => s.Value).FirstOrDefault();
        var fan = sensors.FirstOrDefault(s => !s.IsTemperature);
        var parts = new[] { hottest?.Formatted, fan?.Formatted }.Where(s => s is not null);
        return sensors.Count == 0 ? "Sensores indisponíveis" : string.Join(" · ", parts);
    }
    private static string? ReadText(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            using var reader = File.OpenText(path);
            var buffer = new char[513]; var count = reader.ReadBlock(buffer, 0, buffer.Length);
            if (count > 512) return null;
            var text = new string(buffer, 0, count).Trim();
            return text.Length == 0 || text.Any(char.IsControl) ? null : text;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return null; }
    }
}
