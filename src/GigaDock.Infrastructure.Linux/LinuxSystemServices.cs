using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

namespace GigaDock.Infrastructure.Linux;

public record SystemSnapshot(double? CpuPercent, double? MemoryPercent, string Battery, string Network);

public sealed class LinuxSystemServices(ILinuxCommands? commands = null)
{
    private readonly ILinuxCommands _commands = commands ?? new LinuxCommands();
    private long _previousTotal;
    private long _previousIdle;

    public SystemSnapshot Read(string proc = "/proc", string power = "/sys/class/power_supply")
    {
        double? cpu = null, memory = null;
        var battery = "Sem bateria detectada";
        try
        {
            var fields = File.ReadLines(Path.Combine(proc, "stat")).First().Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).Take(8).Select(long.Parse).ToArray();
            var total = fields.Sum(); var idle = fields[3] + (fields.Length > 4 ? fields[4] : 0);
            if (_previousTotal > 0 && total > _previousTotal) cpu = Math.Clamp(100d * (total - _previousTotal - (idle - _previousIdle)) / (total - _previousTotal), 0, 100);
            _previousTotal = total; _previousIdle = idle;
            var values = File.ReadLines(Path.Combine(proc, "meminfo")).Select(line => line.Split(':', 2))
                .ToDictionary(parts => parts[0], parts => long.Parse(parts[1].Trim().Split(' ')[0], CultureInfo.InvariantCulture));
            if (values.TryGetValue("MemTotal", out var all) && all > 0 && values.TryGetValue("MemAvailable", out var available)) memory = 100d * (all - available) / all;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or FormatException or IndexOutOfRangeException) { }
        try
        {
            if (Directory.Exists(power))
            {
                var batteries = Directory.GetDirectories(power).Where(dir => File.Exists(Path.Combine(dir, "type")) && File.ReadAllText(Path.Combine(dir, "type")).Trim() == "Battery");
                var states = batteries.Select(dir => $"{File.ReadAllText(Path.Combine(dir, "capacity")).Trim()}% — {TranslateBattery(File.ReadAllText(Path.Combine(dir, "status")).Trim())}").ToArray();
                if (states.Length > 0) battery = string.Join(" / ", states);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { battery = "Bateria indisponível"; }
        var network = "Rede indisponível";
        try
        {
            var active = NetworkInterface.GetAllNetworkInterfaces().Where(adapter => adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback && adapter.OperationalStatus == OperationalStatus.Up).Select(adapter => adapter.Name).ToArray();
            network = active.Length > 0 ? "Conectada: " + string.Join(", ", active) : "Sem conexão ativa";
        }
        catch (NetworkInformationException) { }
        return new(cpu, memory, battery, network);
    }

    private static string TranslateBattery(string status) => status switch
    { "Charging" => "carregando", "Discharging" => "em uso", "Full" => "carregada", _ => "conectada" };

    public async Task<string> MediaAsync(CancellationToken token = default)
    {
        var result = await _commands.RunAsync("playerctl", ["metadata", "--format", "{{artist}} — {{title}}"], token);
        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output) ? result.Output.Trim() : "Nenhum player compatível em reprodução.";
    }

    public async Task MediaCommandAsync(string action, CancellationToken token = default)
    {
        if (action is not ("previous" or "play-pause" or "next")) throw new ArgumentException("Comando de mídia inválido.");
        var result = await _commands.RunAsync("playerctl", [action], token);
        if (result.ExitCode != 0) throw new IOException("O player não aceitou o comando.");
    }

    public async Task<string> VolumeAsync(CancellationToken token = default)
    {
        var result = await _commands.RunAsync("pactl", ["get-sink-volume", "@DEFAULT_SINK@"], token);
        return result.ExitCode == 0 ? result.Output.Trim() : "Servidor de áudio indisponível.";
    }

    public async Task SetVolumeAsync(int percent, CancellationToken token = default)
    {
        if (percent is < 0 or > 100) throw new ArgumentException("Escolha volume de 0 a 100%.");
        var result = await _commands.RunAsync("pactl", ["set-sink-volume", "@DEFAULT_SINK@", percent.ToString(CultureInfo.InvariantCulture) + "%"], token);
        if (result.ExitCode != 0) throw new IOException("Não foi possível alterar o volume.");
    }
}

public sealed class LocalNotes(string directory)
{
    private string FileFor(string environment) => Path.Combine(directory, "notes", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(environment))) + ".txt");
    public string Read(string environment)
    {
        var path = FileFor(environment);
        if (!File.Exists(path)) return "";
        if (new FileInfo(path).Length > 1024 * 1024) throw new IOException("A nota ultrapassou o limite de 1 MB.");
        return File.ReadAllText(path, Encoding.UTF8);
    }
    public void Save(string environment, string text)
    {
        if (Encoding.UTF8.GetByteCount(text) > 1024 * 1024) throw new ArgumentException("A nota deve ter no máximo 1 MB.");
        var path = FileFor(environment); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, text, new UTF8Encoding(false));
            if (OperatingSystem.IsLinux()) File.SetUnixFileMode(temp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temp, path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
