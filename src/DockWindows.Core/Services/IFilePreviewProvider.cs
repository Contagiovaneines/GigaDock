namespace DockWindows.Core.Services;

public sealed record FilePreviewResult(string Tipo, string Titulo, string Descricao, string? Texto = null, string? CaminhoImagem = null);

public interface IFilePreviewProvider
{
    bool PodeAbrir(string caminho);
    Task<FilePreviewResult> CriarAsync(string caminho, CancellationToken cancellationToken);
}
