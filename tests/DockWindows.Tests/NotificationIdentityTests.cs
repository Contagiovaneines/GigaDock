using System.IO;
using System.IO.Compression;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;
public sealed class NotificationIdentityTests
{
    [Fact]
    public async Task PacoteSemAssinatura_NaoChamaRegistroDoWindows()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GigaDock-notifications-test-" + Guid.NewGuid().ToString("N"));
        var identity = Path.Combine(directory, "identity"); Directory.CreateDirectory(identity);
        var exe = Path.Combine(directory, "DockWindows.App.exe");
        var package = Path.Combine(identity, "GigaDock.Identity.msix");
        try
        {
            File.WriteAllBytes(exe, []);
            using (var archive = ZipFile.Open(package, ZipArchiveMode.Create)) archive.CreateEntry("AppxManifest.xml");
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => NotificationIdentityRegistration.RegisterAsync(directory));
            Assert.Contains("não foi assinada", error.Message);
        }
        finally { File.Delete(package); File.Delete(exe); Directory.Delete(identity); Directory.Delete(directory); }
    }
}
