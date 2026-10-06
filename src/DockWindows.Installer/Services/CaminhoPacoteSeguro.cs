using System.IO;
namespace DockWindows.Installer.Services;

public static class CaminhoPacoteSeguro
{
    public static string Resolver(string destino, string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada) || Path.IsPathRooted(entrada))
            throw new InvalidDataException("O pacote contém um caminho inválido.");
        var partes = entrada.Replace('\\', '/').TrimEnd('/').Split('/');
        if (partes.Any(p => string.IsNullOrEmpty(p) || p is "." or ".." || p != p.TrimEnd(' ', '.') || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new InvalidDataException("O pacote contém um caminho inseguro.");
        var raiz = Path.GetFullPath(destino).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var arquivo = Path.GetFullPath(Path.Combine(raiz, Path.Combine(partes)));
        if (!arquivo.StartsWith(raiz, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("O pacote tentou escrever fora da instalação.");
        // Recusa junções/links existentes que redirecionem a extração.
        for (var atual = arquivo; !string.IsNullOrEmpty(atual); atual = Path.GetDirectoryName(atual))
        {
            if ((Directory.Exists(atual) || File.Exists(atual)) && (File.GetAttributes(atual) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("O destino contém um link ou junção; escolha uma pasta normal.");
        }
        return arquivo;
    }
}
