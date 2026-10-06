using DockWindows.Core.Models;

namespace DockWindows.Core.Validation;

public record ValidacaoResultado(bool Valido, string? MensagemErro = null)
{
    public static ValidacaoResultado Sucesso() => new(true);
    public static ValidacaoResultado Erro(string mensagem) => new(false, mensagem);
}

public static class ItemValidator
{
    public static ValidacaoResultado ValidarItem(ItemFixado item)
    {
        if (string.IsNullOrWhiteSpace(item.Titulo))
            return ValidacaoResultado.Erro("O título do item não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(item.CaminhoOuUrl))
            return ValidacaoResultado.Erro("O caminho ou endereço web não pode estar em branco.");

        return item.Tipo switch
        {
            TipoItem.WebUrl => ValidarUrl(item.CaminhoOuUrl),
            TipoItem.Pasta => ValidarPasta(item.CaminhoOuUrl),
            TipoItem.Aplicativo or TipoItem.Arquivo => ValidarArquivoOuApp(item.CaminhoOuUrl),
            _ => ValidacaoResultado.Erro("Tipo de item desconhecido.")
        };
    }

    public static ValidacaoResultado ValidarUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Any(char.IsControl))
            return ValidacaoResultado.Erro("O endereço web é vazio ou contém caracteres inválidos.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            Uri.TryCreate("https://" + url, UriKind.Absolute, out uri);
        if (uri == null || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || string.IsNullOrWhiteSpace(uri.Host))
            return ValidacaoResultado.Erro("Informe uma URL HTTP ou HTTPS válida.");
        if (!string.IsNullOrEmpty(uri.UserInfo))
            return ValidacaoResultado.Erro("Não use URLs com usuário ou senha incorporados.");
        return ValidacaoResultado.Sucesso();
    }

    public static ValidacaoResultado ValidarPasta(string caminho)
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

    public static ValidacaoResultado ValidarArquivoOuApp(string caminho)
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
