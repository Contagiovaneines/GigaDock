using DockWindows.Core.Models;

namespace DockWindows.Core.Validation;

public record ValidacaoResultado(bool Valido, string? MensagemErro = null)
{
    public static ValidacaoResultado Sucesso() => new(true);
    public static ValidacaoResultado Erro(string mensagem) => new(false, mensagem);
}

public static class ItemValidator
{
    public static ValidacaoResultado ValidarItem(ItemFixado item, IItemPathValidator? caminhos = null)
    {
        if (string.IsNullOrWhiteSpace(item.Titulo))
            return ValidacaoResultado.Erro("O título do item não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(item.CaminhoOuUrl))
            return ValidacaoResultado.Erro("O caminho ou endereço web não pode estar em branco.");

        caminhos ??= LocalItemPathValidator.Instance;
        return item.Tipo switch
        {
            TipoItem.WebUrl => ValidarUrl(item.CaminhoOuUrl),
            TipoItem.Pasta => caminhos.ValidarPasta(item.CaminhoOuUrl),
            TipoItem.Aplicativo or TipoItem.Arquivo => caminhos.ValidarArquivoOuApp(item.CaminhoOuUrl),
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

    public static ValidacaoResultado ValidarPasta(string caminho) =>
        LocalItemPathValidator.Instance.ValidarPasta(caminho);

    public static ValidacaoResultado ValidarArquivoOuApp(string caminho) =>
        LocalItemPathValidator.Instance.ValidarArquivoOuApp(caminho);
}
