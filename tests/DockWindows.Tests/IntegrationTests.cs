using System.IO;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Infrastructure.Persistence;
using DockWindows.Infrastructure.Windows;
using Xunit;

namespace DockWindows.Tests;

public class IntegrationTests : IDisposable
{
    private readonly string _tempFolder;

    public IntegrationTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "DockIntegrationTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempFolder))
                Directory.Delete(_tempFolder, true);
        }
        catch { }
    }

    [Fact]
    public void CrudAmbientes_CriarRenomearExcluir_FuncionaComPersistencia()
    {
        var repo = new JsonSettingsRepository(_tempFolder);
        var prefs = repo.Carregar();

        // 1. Criar
        var novoAmbiente = new Ambiente
        {
            Id = "amb-custom",
            Nome = "Projetos 2026",
            CorHex = "#D83B01"
        };
        prefs.Ambientes.Add(novoAmbiente);
        repo.Salvar(prefs);

        var prefs1 = repo.Carregar();
        Assert.Contains(prefs1.Ambientes, a => a.Nome == "Projetos 2026");

        // 2. Renomear
        var amb = prefs1.Ambientes.First(a => a.Nome == "Projetos 2026");
        amb.Nome = "Projetos Concluídos";
        repo.Salvar(prefs1);

        var prefs2 = repo.Carregar();
        Assert.Contains(prefs2.Ambientes, a => a.Nome == "Projetos Concluídos");

        // 3. Excluir
        prefs2.Ambientes.RemoveAll(a => a.Nome == "Projetos Concluídos");
        repo.Salvar(prefs2);

        var prefs3 = repo.Carregar();
        Assert.DoesNotContain(prefs3.Ambientes, a => a.Nome == "Projetos Concluídos");
    }

    [Fact]
    public void ItensFixados_QuatroTipos_ManipulacaoEReordenacao()
    {
        var repo = new JsonSettingsRepository(_tempFolder);
        var prefs = repo.Carregar();
        var amb = prefs.Ambientes[0];
        amb.Itens.Clear();

        var itemApp = new ItemFixado { Titulo = "App Teste", CaminhoOuUrl = "notepad.exe", Tipo = TipoItem.Aplicativo, Ordem = 0 };
        var itemPasta = new ItemFixado { Titulo = "Pasta Teste", CaminhoOuUrl = _tempFolder, Tipo = TipoItem.Pasta, Ordem = 1 };
        var itemArquivo = new ItemFixado { Titulo = "Arquivo Teste", CaminhoOuUrl = Path.Combine(_tempFolder, "dummy.txt"), Tipo = TipoItem.Arquivo, Ordem = 2 };
        var itemUrl = new ItemFixado { Titulo = "Site Teste", CaminhoOuUrl = "https://github.com", Tipo = TipoItem.WebUrl, Ordem = 3 };

        File.WriteAllText(itemArquivo.CaminhoOuUrl, "conteudo de teste");

        amb.Itens.Add(itemApp);
        amb.Itens.Add(itemPasta);
        amb.Itens.Add(itemArquivo);
        amb.Itens.Add(itemUrl);

        repo.Salvar(prefs);

        var prefsRecarregado = repo.Carregar();
        var itens = prefsRecarregado.Ambientes[0].Itens;
        Assert.Equal(4, itens.Count);

        // Reordena: move URL para primeiro
        var urlItem = itens.First(i => i.Tipo == TipoItem.WebUrl);
        itens.Remove(urlItem);
        itens.Insert(0, urlItem);
        for (int i = 0; i < itens.Count; i++) itens[i].Ordem = i;

        repo.Salvar(prefsRecarregado);

        var prefsReordenado = repo.Carregar();
        Assert.Equal(TipoItem.WebUrl, prefsReordenado.Ambientes[0].Itens[0].Tipo);
        Assert.Equal(0, prefsReordenado.Ambientes[0].Itens[0].Ordem);
    }

    [Fact]
    public void LauncherService_ItemInexistente_RetornaFalhaComMensagemClara()
    {
        var launcher = new LauncherService();
        var itemInvalido = new ItemFixado
        {
            Titulo = "Programa Fantasma",
            CaminhoOuUrl = @"C:\PastaInexistente_12345\app.exe",
            Tipo = TipoItem.Aplicativo
        };

        var resultado = launcher.Executar(itemInvalido);

        Assert.False(resultado.Sucesso);
        Assert.NotNull(resultado.MensagemErro);
        Assert.Contains("não existe", resultado.MensagemErro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LauncherService_AbrirLocal_EmItemWebUrl_RetornaFalhaApropriada()
    {
        var launcher = new LauncherService();
        var itemUrl = new ItemFixado
        {
            Titulo = "Google",
            CaminhoOuUrl = "https://www.google.com",
            Tipo = TipoItem.WebUrl
        };

        var resultado = launcher.AbrirLocal(itemUrl);

        Assert.False(resultado.Sucesso);
        Assert.Contains("não possuem diretório local", resultado.MensagemErro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "SystemIntegration")]
    public void AutostartService_ConfigurarLeituraEGravacao_OperaEmHKCU()
    {
        var autostart = new AutostartService();
        
        // Verifica estado inicial
        bool estadoOriginal = autostart.EstaHabilitado();

        try
        {
            // Tenta habilitar
            bool configurou = autostart.Configurar(true);
            Assert.True(configurou);
            Assert.True(autostart.EstaHabilitado());

            // Desabilita
            bool removeu = autostart.Configurar(false);
            Assert.True(removeu);
            Assert.False(autostart.EstaHabilitado());
        }
        finally
        {
            // Restaura o estado original para não poluir o sistema do usuário
            autostart.Configurar(estadoOriginal);
        }
    }
}

