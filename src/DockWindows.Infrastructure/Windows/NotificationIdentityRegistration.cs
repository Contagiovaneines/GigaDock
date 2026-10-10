using System.IO;
using System.IO.Compression;
using Windows.Management.Deployment;

namespace DockWindows.Infrastructure.Windows;

public static class NotificationIdentityRegistration
{
    public const string PackageName = "GigaDock.Identity";
    public const string Publisher = "CN=GigaDockOpenSource";
    public static bool IsSigned(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        return archive.GetEntry("AppxSignature.p7x") != null;
    }

    public static async Task RegisterAsync(string installationDirectory)
    {
        var directory = Path.GetFullPath(installationDirectory);
        var packagePath = Path.Combine(directory, "identity", "GigaDock.Identity.msix");
        if (!File.Exists(Path.Combine(directory, "DockWindows.App.exe")))
            throw new InvalidOperationException("A pasta não contém a instalação do GigaDock.");
        using (var stream = File.OpenRead(packagePath))
        {
            if (!IsSigned(stream)) throw new InvalidOperationException("A identidade de notificações ainda não foi assinada. A instalação da dock pode continuar sem esse recurso.");
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var manifest = archive.GetEntry("AppxManifest.xml") ?? throw new InvalidOperationException("Manifesto da identidade ausente.");
            using var input = manifest.Open();
            using var reader = System.Xml.XmlReader.Create(input, new System.Xml.XmlReaderSettings
                { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 128 * 1024 });
            var xml = System.Xml.Linq.XDocument.Load(reader);
            var identity = xml.Root?.Element(System.Xml.Linq.XName.Get("Identity", "http://schemas.microsoft.com/appx/manifest/foundation/windows10"));
            if ((string?)identity?.Attribute("Name") != PackageName || (string?)identity?.Attribute("Publisher") != Publisher)
                throw new InvalidOperationException("O pacote não corresponde à identidade do GigaDock.");
        }
        var manager = new PackageManager();
        // O próprio Windows valida assinatura, confiança, manifesto e destino antes de registrar.
        var result = await manager.AddPackageByUriAsync(new Uri(packagePath),
            new AddPackageOptions { ExternalLocationUri = new Uri(directory + Path.DirectorySeparatorChar) });
        if (result.ExtendedErrorCode != null)
            throw new InvalidOperationException("O Windows não registrou a identidade de notificações. Confira a assinatura confiável do pacote.", result.ExtendedErrorCode);
    }

    public static async Task UnregisterAsync()
    {
        var manager = new PackageManager();
        foreach (var package in manager.FindPackagesForUser(string.Empty).Where(p => p.Id.Name == PackageName && p.Id.Publisher == Publisher))
        {
            var result = await manager.RemovePackageAsync(package.Id.FullName);
            if (result.ExtendedErrorCode != null) throw new InvalidOperationException("Não foi possível remover a identidade de notificações.", result.ExtendedErrorCode);
        }
    }
}
