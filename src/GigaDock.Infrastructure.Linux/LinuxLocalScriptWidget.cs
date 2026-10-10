using System.Text.Json;

namespace GigaDock.Infrastructure.Linux;

public sealed record LocalScriptValue(string Text, string Tooltip);

/// <summary>Manual execution only. This runs a user's program, not a sandbox.</summary>
public sealed class LinuxLocalScriptWidget(ILinuxCommands? commands = null)
{
    private readonly ILinuxCommands _commands = commands ?? new LinuxCommands(TimeSpan.FromSeconds(3), 4096);
    public static string ValidatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 4096 || path.Any(char.IsControl) || !Path.IsPathFullyQualified(path))
            throw new ArgumentException("Escolha o caminho absoluto de um executável ou script local.");
        path = Path.GetFullPath(path);
        if (!File.Exists(path) || Directory.Exists(path)) throw new ArgumentException("O script local não foi encontrado.");
        if (OperatingSystem.IsLinux() && !LinuxCommands.IsExecutable(path)) throw new ArgumentException("O script precisa de permissão de execução e um interpretador válido na primeira linha.");
        return path;
    }
    public async Task<LocalScriptValue> ExecuteAsync(string path, CancellationToken token = default)
    {
        path = ValidatePath(path);
        var result = await _commands.RunAsync(path, [], token);
        if (result.ExitCode != 0) throw new IOException("O script terminou com erro. Confira o programa no terminal.");
        return Parse(result.Output);
    }
    public static LocalScriptValue Parse(string output)
    {
        if (output.Length > 4096) throw new IOException("O script deve retornar no máximo 4096 caracteres.");
        try
        {
            using var document = JsonDocument.Parse(output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("text", out var textProperty) || textProperty.ValueKind != JsonValueKind.String)
                throw new IOException("O script deve retornar um objeto JSON com a propriedade text.");
            var text = textProperty.GetString()?.Trim() ?? "";
            var tooltip = "";
            if (root.TryGetProperty("tooltip", out var tip))
            {
                if (tip.ValueKind != JsonValueKind.String) throw new IOException("A propriedade tooltip deve ser texto.");
                tooltip = tip.GetString() ?? "";
            }
            if (text.Length is 0 or > 80 || text.Any(char.IsControl) || tooltip.Length > 500 || tooltip.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
                throw new IOException("Use text com 1 a 80 caracteres e tooltip com até 500 caracteres.");
            return new(text, tooltip);
        }
        catch (JsonException error) { throw new IOException("O script retornou JSON inválido.", error); }
    }
}
