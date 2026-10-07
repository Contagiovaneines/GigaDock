using System.IO;
using System.Windows.Media.Imaging;
using DockWindows.Core.Services;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

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

public sealed class PdfFilePreviewProvider : IFilePreviewProvider
{
    private const long LimiteBytes = 50L * 1024 * 1024;
    public bool PodeAbrir(string caminho) => string.Equals(Path.GetExtension(caminho), ".pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<FilePreviewResult> CriarAsync(string caminho, CancellationToken cancellationToken)
    {
        var info = new FileInfo(caminho);
        if (info.Length > LimiteBytes) return new("metadata", info.Name, "PDF acima do limite de prévia de 50 MB");

        var arquivo = await StorageFile.GetFileFromPathAsync(caminho).AsTask(cancellationToken);
        var documento = await PdfDocument.LoadFromFileAsync(arquivo).AsTask(cancellationToken);
        if (documento.PageCount == 0) return new("metadata", info.Name, "PDF sem páginas");

        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "preview-cache");
        Directory.CreateDirectory(pasta);
        LimparCache(pasta);
        var chave = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{info.FullName}|{info.LastWriteTimeUtc.Ticks}|{info.Length}")))[..20];
        var destino = Path.Combine(pasta, chave + ".png");
        if (!File.Exists(destino))
        {
            using var pagina = documento.GetPage(0);
            using var memoria = new InMemoryRandomAccessStream();
            await pagina.RenderToStreamAsync(memoria, new PdfPageRenderOptions { DestinationWidth = 900 }).AsTask(cancellationToken);
            memoria.Seek(0);
            var decoder = BitmapDecoder.Create(memoria.AsStreamForRead(), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(decoder.Frames[0]);
            await using var saida = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true);
            encoder.Save(saida);
        }
        return new("pdf", info.Name, $"{documento.PageCount} página(s) · {info.Length / 1024d:N1} KB", CaminhoImagem: destino);
    }

    private static void LimparCache(string pasta)
    {
        try
        {
            foreach (var arquivo in new DirectoryInfo(pasta).EnumerateFiles("*.png").OrderByDescending(f => f.LastWriteTimeUtc).Skip(32)) arquivo.Delete();
        }
        catch { }
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
