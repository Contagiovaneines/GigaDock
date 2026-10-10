using System.IO;
using DockWindows.Core.Models;
using DockWindows.Core.Validation;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.Tests;

public sealed class WindowsCompatibilityServicesTests
{
    [Theory]
    [InlineData("notepad.exe")]
    [InlineData("shell:AppsFolder\\pacote!app")]
    public void PoliticaWindows_PreservaFormatosExistentes(string path)
    {
        Assert.True(ItemValidator.ValidarItem(new ItemFixado
        {
            Titulo = "Aplicativo", CaminhoOuUrl = path, Tipo = TipoItem.Aplicativo
        }, WindowsItemPathValidator.Instance).Valido);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("shell:AppsFolder\\pacote|comando")]
    [InlineData("notepad.exe&comando")]
    public void PoliticaWindows_RejeitaComandosInvalidos(string path) =>
        Assert.False(WindowsItemPathValidator.Instance.ValidarArquivoOuApp(path).Valido);

    [Fact]
    public void AdaptadorWindows_PreservaCaminhoConfiguracoes()
    {
        // Instanciar o resolvedor não cria nem altera as configurações reais.
        var dirs = new WindowsAppDirectories();
        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows"), dirs.Configuracoes);
    }

    [Fact]
    public void AdaptadorWindows_UsaMesmoJsonDoServicoCompartilhado()
    {
        var path = Path.Combine(Path.GetTempPath(), "GigaDockCompatibility_" + Guid.NewGuid().ToString("N"));
        try
        {
            var adapter = new JsonSettingsRepository(path);
            var prefs = adapter.Carregar();
            prefs.Ambientes[0].Nome = "Compatibilidade Windows";
            adapter.Salvar(prefs);
            var shared = new GigaDock.Services.Persistence.JsonSettingsRepository(path);
            Assert.Equal("Compatibilidade Windows", shared.Carregar().Ambientes[0].Nome);
        }
        finally
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }
}
