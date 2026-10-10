namespace DockWindows.Core.Validation;

/// <summary>Arquivos e pastas locais existentes. Não interpreta shell:, AUMID, PATH ou .desktop.</summary>
public sealed class LocalItemPathValidator : IItemPathValidator
{
    public static LocalItemPathValidator Instance { get; } = new();

    public ValidacaoResultado ValidarPasta(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            return ValidacaoResultado.Erro("O caminho da pasta não pode ser vazio.");
        if (caminho.Any(char.IsControl) || caminho.Contains('"'))
            return ValidacaoResultado.Erro("O caminho da pasta contém caracteres inválidos.");
        return ExistsResolved(Environment.ExpandEnvironmentVariables(caminho), directory: true)
            ? ValidacaoResultado.Sucesso()
            : ValidacaoResultado.Erro($"A pasta '{caminho}' não foi encontrada.");
    }

    public ValidacaoResultado ValidarArquivoOuApp(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            return ValidacaoResultado.Erro("O caminho do arquivo ou programa não pode ser vazio.");
        if (caminho.Any(char.IsControl) || caminho.Contains('"'))
            return ValidacaoResultado.Erro("O caminho contém caracteres inválidos.");
        return ExistsResolved(Environment.ExpandEnvironmentVariables(caminho), directory: false)
            ? ValidacaoResultado.Sucesso()
            : ValidacaoResultado.Erro($"O arquivo '{caminho}' não existe ou não está acessível.");
    }

    private static bool ExistsResolved(string path, bool directory)
    {
        try
        {
            FileSystemInfo info = directory ? new DirectoryInfo(path) : new FileInfo(path);
            if (!info.Exists) return false;
            if (info.LinkTarget is null) return true;
            // File.Exists pode reconhecer o próprio link mesmo quando o destino foi removido.
            var target = info.ResolveLinkTarget(returnFinalTarget: true);
            return target is { Exists: true } && (directory ? target is DirectoryInfo : target is FileInfo);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
