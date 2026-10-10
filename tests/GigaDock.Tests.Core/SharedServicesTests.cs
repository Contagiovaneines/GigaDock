using DockWindows.Core.Models;
using DockWindows.Core.Validation;
using GigaDock.Services.Paths;
using GigaDock.Services.Persistence;

namespace GigaDock.Tests.Core;

public sealed class SharedServicesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GigaDockShared_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Xdg_RespeitaDiretoriosAbsolutosSemCriarPastas()
    {
        var dirs = new XdgAppDirectories(_root, key => Path.Combine(_root, key));
        Assert.Equal(Path.Combine(_root, "XDG_CONFIG_HOME", "gigadock"), dirs.Configuracoes);
        Assert.Equal(Path.Combine(_root, "XDG_DATA_HOME", "gigadock"), dirs.Dados);
        Assert.Equal(Path.Combine(_root, "XDG_CACHE_HOME", "gigadock"), dirs.Cache);
        Assert.Equal(Path.Combine(_root, "XDG_STATE_HOME", "gigadock"), dirs.Estado);
        Assert.False(Directory.Exists(_root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relativo")]
    public void Xdg_VariavelAusenteOuRelativaUsaPadrao(string? configured)
    {
        var dirs = new XdgAppDirectories(_root, _ => configured);
        Assert.Equal(Path.Combine(_root, ".config", "gigadock"), dirs.Configuracoes);
        Assert.Equal(Path.Combine(_root, ".local", "share", "gigadock"), dirs.Dados);
        Assert.Equal(Path.Combine(_root, ".cache", "gigadock"), dirs.Cache);
        Assert.Equal(Path.Combine(_root, ".local", "state", "gigadock"), dirs.Estado);
    }

    [Fact]
    public void Repositorio_RejeitaDiretorioRelativo()
    {
        Assert.Throws<ArgumentException>(() => new JsonSettingsRepository("config-relativa"));
        Assert.Throws<ArgumentException>(() => new XdgAppDirectories("home-relativo"));
    }

    [Fact]
    public void Repositorio_PermitePadraoInjetadoESalvaUnicode()
    {
        var repo = new JsonSettingsRepository(_root, () =>
        {
            var prefs = Preferencias.CriarPadrao();
            prefs.Ambientes[0].Nome = "Estação de estudos · São Paulo";
            prefs.AppsPermanentes.Clear();
            return prefs;
        });
        repo.Carregar();
        var loaded = new JsonSettingsRepository(_root).Carregar();
        Assert.Equal("Estação de estudos · São Paulo", loaded.Ambientes[0].Nome);
        Assert.Empty(loaded.AppsPermanentes);
        Assert.False(File.Exists(Path.Combine(_root, "settings.json.tmp")));
    }

    [Theory]
    [InlineData("shell:AppsFolder\\pacote!app")]
    [InlineData("notepad.exe")]
    public void ValidadorGenerico_NaoAceitaIdentificadorWindowsComoArquivo(string value)
    {
        Assert.False(ItemValidator.ValidarArquivoOuApp(value).Valido);
        Assert.False(ItemValidator.ValidarPasta("shell:RecycleBinFolder").Valido);
    }

    [Fact]
    public void RecuperarBackup_NaoSobrescreveBackupValidoComPrincipalCorrompido()
    {
        var repo = new JsonSettingsRepository(_root);
        var prefs = repo.Carregar();
        prefs.Ambientes[0].Nome = "Cópia válida";
        repo.Salvar(prefs);
        repo.Salvar(prefs);
        var backup = Path.Combine(_root, "settings.json.bak");
        var expected = File.ReadAllText(backup);
        File.WriteAllText(repo.ObterCaminhoConfiguracoes(), "{corrompido");
        Assert.Equal("Cópia válida", repo.Carregar().Ambientes[0].Nome);
        Assert.Equal(expected, File.ReadAllText(backup));
    }

    [Theory]
    [InlineData("https://usuario:senha@example.com")]
    [InlineData("https://example.com\ncomando")]
    [InlineData("file:///etc/passwd")]
    public void Url_NaoAceitaCredenciaisControlesOuArquivo(string url) =>
        Assert.False(ItemValidator.ValidarUrl(url).Valido);

    [Fact]
    public void Validacao_UsaPoliticaInjetadaParaAplicativo()
    {
        var item = new ItemFixado { Titulo = "Aplicativo", CaminhoOuUrl = "org.example.Editor.desktop", Tipo = TipoItem.Aplicativo };
        Assert.False(ItemValidator.ValidarItem(item).Valido);
        Assert.True(ItemValidator.ValidarItem(item, new DesktopIdPolicy()).Valido);
    }

    [Fact]
    public void ArquivoExistente_ComEspacosEAcentos_ValidaSemExecutar()
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "anotações de estudo.txt");
        File.WriteAllText(path, "conteúdo");
        Assert.True(ItemValidator.ValidarArquivoOuApp(path).Valido);
        Assert.True(ItemValidator.ValidarPasta(_root).Valido);
        if (OperatingSystem.IsLinux())
            Assert.False(ItemValidator.ValidarArquivoOuApp(Path.Combine(_root, "ANOTAÇÕES DE ESTUDO.txt")).Valido);
    }

    private sealed class DesktopIdPolicy : IItemPathValidator
    {
        public ValidacaoResultado ValidarPasta(string caminho) => LocalItemPathValidator.Instance.ValidarPasta(caminho);
        public ValidacaoResultado ValidarArquivoOuApp(string caminho) =>
            caminho == "org.example.Editor.desktop" ? ValidacaoResultado.Sucesso() : ValidacaoResultado.Erro("Não encontrado.");
    }

    [Fact]
    public void Linux_ArquivoPorLinkSimbolicoSegueExistenciaDoDestino()
    {
        if (!OperatingSystem.IsLinux()) return;
        Directory.CreateDirectory(_root);
        var target = Path.Combine(_root, "origem.txt");
        var link = Path.Combine(_root, "atalho.txt");
        File.WriteAllText(target, "teste");
        File.CreateSymbolicLink(link, target);
        Assert.True(ItemValidator.ValidarArquivoOuApp(link).Valido);
        File.Delete(target);
        Assert.False(ItemValidator.ValidarArquivoOuApp(link).Valido);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
