using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GigaDock.Infrastructure.Linux;

public enum LinuxCompositor { Unsupported, Sway, Hyprland }
public sealed record LinuxWorkspace(long Id, string Name, bool Active, string Monitor);

public sealed class LinuxWorkspaceService(ILinuxCommands? commands = null, LinuxCompositor? compositor = null)
{
    private readonly ILinuxCommands _commands = commands ?? new LinuxCommands();
    public LinuxCompositor Compositor { get; } = compositor ?? Detect();
    public static LinuxCompositor Detect() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE")) ? LinuxCompositor.Hyprland
        : !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SWAYSOCK")) ? LinuxCompositor.Sway : LinuxCompositor.Unsupported;
    public async Task<IReadOnlyList<LinuxWorkspace>> ListAsync(CancellationToken token = default)
    {
        if (Compositor == LinuxCompositor.Unsupported) throw new InvalidOperationException("Este widget precisa de uma sessão Sway ou Hyprland. GNOME/KDE ainda não têm integração de áreas de trabalho.");
        var result = await _commands.RunAsync(Compositor == LinuxCompositor.Sway ? "swaymsg" : "hyprctl",
            Compositor == LinuxCompositor.Sway ? ["-r", "-t", "get_workspaces"] : ["-j", "workspaces"], token);
        if (result.ExitCode != 0) throw new IOException("O compositor não respondeu à consulta de áreas de trabalho.");
        long? active = null;
        if (Compositor == LinuxCompositor.Hyprland)
        {
            var current = await _commands.RunAsync("hyprctl", ["-j", "activeworkspace"], token);
            if (current.ExitCode != 0) throw new IOException("Não foi possível consultar a área de trabalho ativa.");
            using var document = ParseJson(current.Output);
            if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("id", out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var id)) active = id;
        }
        return Parse(result.Output, Compositor, active);
    }
    public static IReadOnlyList<LinuxWorkspace> Parse(string output, LinuxCompositor compositor, long? active = null)
    {
        using var document = ParseJson(output);
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new IOException("A resposta do compositor não contém uma lista de áreas de trabalho.");
        var workspaces = new List<LinuxWorkspace>();
        foreach (var entry in document.RootElement.EnumerateArray().Take(128))
        {
            if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("id", out var idProperty) || idProperty.ValueKind != JsonValueKind.Number || !idProperty.TryGetInt64(out var id)) continue;
            var name = String(entry, "name");
            if (name.Length is 0 or > 128 || name.Any(char.IsControl) || compositor == LinuxCompositor.Hyprland && id <= 0) continue;
            var focused = compositor == LinuxCompositor.Sway
                ? entry.TryGetProperty("focused", out var flag) && flag.ValueKind == JsonValueKind.True : id == active;
            workspaces.Add(new(id, name, focused, String(entry, compositor == LinuxCompositor.Sway ? "output" : "monitor")));
        }
        return workspaces.DistinctBy(w => w.Id).ToArray();
    }
    public async Task SwitchAsync(LinuxWorkspace workspace, CancellationToken token = default)
    {
        if (Compositor == LinuxCompositor.Unsupported) throw new InvalidOperationException("Troca de áreas de trabalho indisponível nesta sessão.");
        // Re-read the current list: don't execute a stale target supplied by another environment.
        var current = (await ListAsync(token)).FirstOrDefault(w => w.Id == workspace.Id && w.Name == workspace.Name)
            ?? throw new ArgumentException("Esta área de trabalho não está mais disponível. Atualize a lista.");
        IReadOnlyList<string> arguments;
        if (Compositor == LinuxCompositor.Sway)
        {
            if (!Regex.IsMatch(current.Name, "^[\\p{L}\\p{N} _.:/-]{1,128}$", RegexOptions.CultureInvariant))
                throw new ArgumentException("O nome desta área de trabalho não pode ser enviado com segurança ao Sway.");
            arguments = ["-r", "-t", "command", "--", "workspace --no-auto-back-and-forth \"" + current.Name + "\""];
        }
        else arguments = ["dispatch", "workspace", current.Id.ToString(CultureInfo.InvariantCulture)];
        var result = await _commands.RunAsync(Compositor == LinuxCompositor.Sway ? "swaymsg" : "hyprctl", arguments, token);
        if (result.ExitCode != 0) throw new IOException("Não foi possível trocar a área de trabalho.");
        if (Compositor == LinuxCompositor.Sway)
        {
            using var reply = ParseJson(result.Output);
            if (reply.RootElement.ValueKind != JsonValueKind.Array || !reply.RootElement.EnumerateArray().Any() ||
                reply.RootElement.EnumerateArray().Any(entry => entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True))
                throw new IOException("O Sway recusou a troca de área de trabalho.");
        }
        else if (!result.Output.Trim().Equals("ok", StringComparison.OrdinalIgnoreCase)) throw new IOException("O Hyprland recusou a troca de área de trabalho.");
    }
    private static string String(JsonElement entry, string key) => entry.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : "";
    private static JsonDocument ParseJson(string output)
    {
        if (output.Length > 512 * 1024) throw new IOException("A resposta do compositor ultrapassou o limite de leitura.");
        try { return JsonDocument.Parse(output, new JsonDocumentOptions { MaxDepth = 16 }); }
        catch (JsonException error) { throw new IOException("O compositor retornou dados inválidos.", error); }
    }
}
