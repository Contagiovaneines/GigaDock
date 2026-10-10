using GigaDock.Infrastructure.Linux;
namespace GigaDock.Tests.Linux;

public sealed class IconThemeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GigaDock-icons-" + Guid.NewGuid().ToString("N"));
    [Fact]
    public void Resolver_FollowsInheritanceAndHicolorWithoutEscapingDirectories()
    {
        var first = Path.Combine(_root, "theme"); var inherited = Path.Combine(_root, "base");
        Directory.CreateDirectory(first); Directory.CreateDirectory(Path.Combine(inherited, "48x48/apps"));
        File.WriteAllText(Path.Combine(first, "index.theme"), "[Icon Theme]\nDirectories=48x48/apps\nInherits=base\n");
        File.WriteAllText(Path.Combine(inherited, "index.theme"), "[Icon Theme]\nDirectories=48x48/apps\n");
        var icon = Path.GetFullPath(Path.Combine(inherited, "48x48/apps/app.png")); File.WriteAllBytes(icon, [1, 2, 3]);
        var resolver = new ThemeIconResolver([_root]); Assert.Equal(icon, resolver.Resolve("app", 48, "theme"));
        Assert.Null(resolver.Resolve("../outside", 48, "theme")); Assert.Null(resolver.Resolve("app", 48, ".."));
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
