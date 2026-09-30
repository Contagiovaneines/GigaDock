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
        if (string.IsNullOrWhiteSpace(url))
            return ValidacaoResultado.Erro("O endereço web não pode ser vazio.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            // Tenta adicionar https:// se o usuário digitou apenas "google.com"
            if (Uri.TryCreate("https://" + url, UriKind.Absolute, out uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return ValidacaoResultado.Sucesso();
            }
            return ValidacaoResultado.Erro("O endereço informado não é uma URL válida (ex: https://exemplo.com).");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return ValidacaoResultado.Erro("Apenas endereços com protocolo HTTP ou HTTPS são suportados.");

        return ValidacaoResultado.Sucesso();
    }

    public static ValidacaoResultado ValidarPasta(string caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            return ValidacaoResultado.Erro("O caminho da pasta não pode ser vazio.");

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

        var expandido = Environment.ExpandEnvironmentVariables(caminho);
        
        // Se for um executável conhecido no PATH do Windows (ex: notepad.exe, calc.exe, cmd.exe, explorer.exe)
        var extensao = Path.GetExtension(expandido);
        if (string.IsNullOrEmpty(Path.GetDirectoryName(expandido)))
        {
            // É um executável direto do sistema
            return ValidacaoResultado.Sucesso();
        }

        if (!File.Exists(expandido))
            return ValidacaoResultado.Erro($"O arquivo ou executável '{caminho}' não existe ou não está acessível.");

        return ValidacaoResultado.Sucesso();
    }
}
