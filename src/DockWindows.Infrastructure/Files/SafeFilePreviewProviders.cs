using System.IO;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Files;

public sealed class TextFilePreviewProvider : IFilePreviewProvider
{
    private static readonly HashSet<string> Extensoes = new(StringComparer.OrdinalIgnoreCase) { ".txt", ".md", ".json", ".xml", ".csv", ".log", ".cs", ".xaml" };
    public bool PodeAbrir(string caminho) => Extensoes.Contains(Path.GetExtension(caminho));
    public async Task<FilePreviewResult> CriarAsync(string caminho, CancellationToken cancellationToken)
    {
        var info = new FileInfo(caminho);
        if (info.Length > 256 * 1024) return new("metadata", info.Name, $"Texto grande · {Formatar(info.Length)}");
        var texto = await File.ReadAllTextAsync(caminho, cancellationToken);
        if (texto.Length > 8000) texto = texto[..8000] + "\n…";
        return new("texto", info.Name, $"{Formatar(info.Length)} · {info.LastWriteTime:g}", texto);
    }
    private static string Formatar(long bytes) => bytes < 1024 ? $"{bytes} B" : bytes < 1024 * 1024 ? $"{bytes / 1024d:N1} KB" : $"{bytes / 1024d / 1024d:N1} MB";
}

public sealed class ImageFilePreviewProvider : IFilePreviewProvider
{
    private static readonly HashSet<string> Extensoes = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };
    public bool PodeAbrir(string caminho) => Extensoes.Contains(Path.GetExtension(caminho));
    public Task<FilePreviewResult> CriarAsync(string caminho, CancellationToken cancellationToken)
    {
        var info = new FileInfo(caminho);
        if (info.Length > 20 * 1024 * 1024) return Task.FromResult(new FilePreviewResult("metadata", info.Name, "Imagem acima do limite de prévia de 20 MB"));
        return Task.FromResult(new FilePreviewResult("imagem", info.Name, $"{info.Length / 1024d:N1} KB · {info.LastWriteTime:g}", CaminhoImagem: caminho));
    }
}

public sealed class MetadataFilePreviewProvider : IFilePreviewProvider
{
    public bool PodeAbrir(string caminho) => true;
    public Task<FilePreviewResult> CriarAsync(string caminho, CancellationToken cancellationToken)
    {
        var info = new FileInfo(caminho);
        var descricao = $"{Path.GetExtension(caminho).ToUpperInvariant()} · {info.Length / 1024d:N1} KB · {info.LastWriteTime:g}";
        return Task.FromResult(new FilePreviewResult("metadata", info.Name, descricao));
    }
}
