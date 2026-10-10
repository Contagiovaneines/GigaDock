using System.IO;
using DockWindows.Core.Models;
using DockWindows.Core.Validation;
using Xunit;

namespace GigaDock.Tests.Core;

public class ValidationTests
{
    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("http://localhost:8080")]
    [InlineData("github.com")]
    public void ValidarUrl_ComUrlsValidas_RetornaSucesso(string url)
    {
        var res = ItemValidator.ValidarUrl(url);
        Assert.True(res.Valido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://servidor-invalido.com")]
    [InlineData("javascript:alert(1)")]
    public void ValidarUrl_ComUrlsInvalidas_RetornaErro(string url)
    {
        var res = ItemValidator.ValidarUrl(url);
        Assert.False(res.Valido);
        Assert.NotNull(res.MensagemErro);
    }

    [Fact]
    public void ValidarPasta_ComPastaExistente_RetornaSucesso()
    {
        var pastaTemp = Path.GetTempPath();
        var res = ItemValidator.ValidarPasta(pastaTemp);
        Assert.True(res.Valido);
    }

    [Fact]
    public void ValidarPasta_ComPastaInexistente_RetornaErro()
    {
        var pastaFicticia = @"C:\DiretorioInexistente_" + Guid.NewGuid().ToString("N");
        var res = ItemValidator.ValidarPasta(pastaFicticia);
        Assert.False(res.Valido);
    }

    [Fact]
    public void ValidarItem_SemTitulo_RetornaErro()
    {
        var item = new ItemFixado
        {
            Titulo = "",
            CaminhoOuUrl = "notepad.exe",
            Tipo = TipoItem.Aplicativo
        };
        var res = ItemValidator.ValidarItem(item);
        Assert.False(res.Valido);
        Assert.Contains("título", res.MensagemErro, StringComparison.OrdinalIgnoreCase);
    }
}

