using System.IO;
using DockWindows.Core.Validation;

namespace DockWindows.Infrastructure.Windows;

public sealed class WindowsItemPathValidator : IItemPathValidator
{
    public static WindowsItemPathValidator Instance { get; } = new();

    public ValidacaoResultado ValidarPasta(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            return ValidacaoResultado.Erro("O caminho da pasta não pode ser vazio.");

        if (caminho.Any(char.IsControl) || caminho.Contains('"'))
            return ValidacaoResultado.Erro("O caminho da pasta contém caracteres inválidos.");
        // Permite comandos especiais do explorer ou variáveis de ambiente
        var expandido = Environment.ExpandEnvironmentVariables(caminho);
        if (!Directory.Exists(expandido) && !caminho.StartsWith("shell:", StringComparison.OrdinalIgnoreCase))
            return ValidacaoResultado.Erro($"A pasta '{caminho}' não foi encontrada.");

        return ValidacaoResultado.Sucesso();
    }

    public ValidacaoResultado ValidarArquivoOuApp(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            return ValidacaoResultado.Erro("O caminho do arquivo ou programa não pode ser vazio.");

        if (caminho.Any(char.IsControl) || caminho.Contains('"'))
            return ValidacaoResultado.Erro("O caminho contém caracteres inválidos.");
        var expandido = Environment.ExpandEnvironmentVariables(caminho);

        // Apps da Loja do Windows / Menu Iniciar: "shell:AppsFolder\{AUMID}"
        const string prefixoAppsFolder = @"shell:AppsFolder\";
        if (expandido.StartsWith(prefixoAppsFolder, StringComparison.OrdinalIgnoreCase))
        {
            var id = expandido[prefixoAppsFolder.Length..];
            if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '"', '\r', '\n', '|', '&', '<', '>' }) >= 0)
                return ValidacaoResultado.Erro("O identificador do aplicativo é inválido.");
            return ValidacaoResultado.Sucesso();
        }
        
        // Se for um executável conhecido no PATH do Windows (ex: notepad.exe, calc.exe, cmd.exe, explorer.exe)
        var extensao = Path.GetExtension(expandido);
        if (string.IsNullOrEmpty(Path.GetDirectoryName(expandido)))
        {
            // Nome de executável no PATH; nunca tratar um protocolo como arquivo.
            if (!extensao.Equals(".exe", StringComparison.OrdinalIgnoreCase) || expandido.IndexOfAny(new[] { ':', '|', '&', '<', '>', '/', '\\' }) >= 0)
                return ValidacaoResultado.Erro("Informe um executável .exe ou um caminho de arquivo existente.");
            return ValidacaoResultado.Sucesso();
        }

        if (!File.Exists(expandido))
            return ValidacaoResultado.Erro($"O arquivo ou executável '{caminho}' não existe ou não está acessível.");

        return ValidacaoResultado.Sucesso();
    }
}
