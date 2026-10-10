using DockWindows.Core.Models;
using DockWindows.Core.Widgets;
using Xunit;

namespace GigaDock.Tests.Core;

public class WidgetStoreCatalogTests
{
    [Theory]
    [InlineData(WidgetPlatform.Windows)]
    [InlineData(WidgetPlatform.Linux)]
    public void CatalogCoversAllKnownWidgetsWithUsageAndLimits(WidgetPlatform platform)
    {
        var entries = WidgetStoreCatalog.ForPlatform(platform);
        Assert.Equal(Enum.GetValues<TipoWidget>().Length, entries.Count);
        Assert.Equal(entries.Count, entries.Select(entry => entry.Kind).Distinct().Count());
        Assert.All(entries, entry =>
        {
            Assert.NotEqual("Widget desconhecido", entry.Name);
            Assert.False(string.IsNullOrWhiteSpace(entry.Description));
            Assert.False(string.IsNullOrWhiteSpace(entry.HowTo));
            Assert.False(string.IsNullOrWhiteSpace(entry.Requirements));
        });
    }

    [Theory]
    [InlineData(TipoWidget.DiscordVoz, WidgetPlatform.Windows)]
    [InlineData(TipoWidget.WhatsAppNotificacoes, WidgetPlatform.Windows)]
    [InlineData(TipoWidget.LembreteAgua, WidgetPlatform.Linux)]
    [InlineData(TipoWidget.AreaTransferencia, WidgetPlatform.Linux)]
    [InlineData(TipoWidget.EstanteArquivos, WidgetPlatform.Linux)]
    [InlineData(TipoWidget.TeamsStatus, WidgetPlatform.Linux)]
    public void IncompleteIntegrationsCannotBeInstalled(TipoWidget kind, WidgetPlatform platform)
    {
        var entry = WidgetStoreCatalog.Get(kind, platform);
        Assert.False(entry.CanInstall);
        Assert.Equal(WidgetStoreState.InDevelopment, entry.State);
    }

    [Fact]
    public void UnknownWidgetFailsClosedOnBothPlatforms()
    {
        foreach (var platform in Enum.GetValues<WidgetPlatform>())
            Assert.False(WidgetStoreCatalog.Get((TipoWidget)9999, platform).CanInstall);
    }

    [Fact]
    public void GitHubLinuxExplainsProfileInsteadOfPromisingContributions()
    {
        var entry = WidgetStoreCatalog.Get(TipoWidget.GitHubContribuicoes, WidgetPlatform.Linux);
        Assert.True(entry.CanInstall);
        Assert.Contains("perfil", entry.HowTo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Não exibe gráfico", entry.Requirements);
    }
}
