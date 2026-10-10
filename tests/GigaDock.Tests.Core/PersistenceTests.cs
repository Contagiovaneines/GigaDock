using System.IO;
using DockWindows.Core.Models;
using GigaDock.Services.Persistence;
using Xunit;

namespace GigaDock.Tests.Core;

public class PersistenceTests : IDisposable
{
    private readonly string _tempDir;

    public PersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DockWindowsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    [Fact]
    public void Carregar_QuandoArquivoNaoExiste_CriaPadraoComTresAmbientes()
    {
        var repo = new JsonSettingsRepository(_tempDir);
        var prefs = repo.Carregar();

        Assert.NotNull(prefs);
        Assert.Equal(3, prefs.Ambientes.Count);
        Assert.Contains(prefs.Ambientes, a => a.Nome == "Trabalho");
        Assert.Contains(prefs.Ambientes, a => a.Nome == "Estudos");
        Assert.Contains(prefs.Ambientes, a => a.Nome == "Pessoal");
        Assert.True(File.Exists(repo.ObterCaminhoConfiguracoes()));
    }

    [Fact]
    public void Salvar_PersisteAlteracoesEmDiscoDeFormaAtomica()
    {
        var repo = new JsonSettingsRepository(_tempDir);
        var prefs = repo.Carregar();
        
        prefs.Tema = TemaModo.Claro;
        prefs.TamanhoIcones = TamanhoIcone.Grande;
        repo.Salvar(prefs);

        // Recarrega em nova instância
        var repo2 = new JsonSettingsRepository(_tempDir);
        var prefs2 = repo2.Carregar();

        Assert.Equal(TemaModo.Claro, prefs2.Tema);
        Assert.Equal(TamanhoIcone.Grande, prefs2.TamanhoIcones);
    }

    [Fact]
    public void Carregar_QuandoArquivoPrincipalCorrompido_RestauraDoBackup()
    {
        var repo = new JsonSettingsRepository(_tempDir);
        var prefs = repo.Carregar();
        
        // Salva versão customizada
        prefs.Ambientes[0].Nome = "Trabalho Customizado";
        repo.Salvar(prefs);

        // Salva uma segunda alteração para que a versão customizada vá para o backup
        prefs.Tema = TemaModo.Claro;
        repo.Salvar(prefs);

        var arquivoPrincipal = repo.ObterCaminhoConfiguracoes();
        var arquivoBackup = Path.Combine(_tempDir, "settings.json.bak");

        Assert.True(File.Exists(arquivoBackup), "O arquivo de backup deve existir após salvar modificações.");

        // Corrompe intencionalmente o arquivo principal
        File.WriteAllText(arquivoPrincipal, "{ json corrompido e truncado ... ");

        var repoRecuperado = new JsonSettingsRepository(_tempDir);
        var prefsRecuperadas = repoRecuperado.Carregar();

        Assert.NotNull(prefsRecuperadas);
        Assert.Equal("Trabalho Customizado", prefsRecuperadas.Ambientes[0].Nome);
    }

    [Fact]
    public void Migracao_SchemaV1ParaV2_PreservaAmbientesEAtualizaPropriedades()
    {
        // Cria um JSON representativo da versão 1
        var jsonV1 = @"
{
  ""schemaVersion"": 1,
  ""ambienteAtivoId"": ""ambiente-trabalho"",
  ""tema"": ""Escuro"",
  ""tamanhoIcones"": ""Medio"",
  ""opacidadeDock"": 0.9,
  ""sempreNoTopo"": true,
  ""ocultarAutomaticamente"": false,
  ""iniciarComWindows"": false,
  ""ambientes"": [
    {
      ""id"": ""ambiente-trabalho"",
      ""nome"": ""Meu Trabalho V1"",
      ""corHex"": ""#0078D4"",
      ""icone"": ""💼"",
      ""itens"": [
        {
          ""id"": ""item-1"",
          ""titulo"": ""Editor Antigo"",
          ""caminhoOuUrl"": ""notepad.exe"",
          ""tipo"": ""Aplicativo"",
          ""ordem"": 0
        }
      ],
      ""widgets"": {
        ""relogioHabilitado"": true,
        ""pomodoroHabilitado"": true
      }
    }
  ]
}";
        var arquivoConfig = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(arquivoConfig, jsonV1);

        var repo = new JsonSettingsRepository(_tempDir);
        var prefsMigradas = repo.Carregar();

        Assert.Equal(8, prefsMigradas.SchemaVersion);
        Assert.Single(prefsMigradas.Ambientes);
        Assert.Equal("Meu Trabalho V1", prefsMigradas.Ambientes[0].Nome);
        Assert.Equal("Editor Antigo", prefsMigradas.Ambientes[0].Itens[0].Titulo);
        Assert.False(prefsMigradas.ExibirSeletorAmbientes);
        Assert.True(prefsMigradas.ExibirItensFixados);
        Assert.Equal(6, prefsMigradas.EspacamentoItens);
        Assert.NotEmpty(prefsMigradas.AppsPermanentes);
        Assert.Contains(prefsMigradas.OrdemSecoes, s => s.Tipo == TipoSecaoDock.ClimaInline);
        Assert.Contains(prefsMigradas.OrdemSecoes, s => s.Tipo == TipoSecaoDock.MidiaInline);
        Assert.NotNull(prefsMigradas.ColecoesGlobais);
        Assert.NotNull(prefsMigradas.Espacadores);
    }

    [Fact]
    public void Migracao_SchemaV2ParaV3_AdicionaAppsPermanentesESecoes()
    {
        var jsonV2 = @"{
  ""schemaVersion"": 2,
  ""ambienteAtivoId"": ""amb-trabalho"",
  ""usarComoBarraPrincipal"": true,
  ""ambientes"": [
    {
      ""id"": ""amb-trabalho"",
      ""nome"": ""Trabalho"",
      ""corHex"": ""#0A84FF"",
      ""icone"": ""💼"",
      ""itens"": [],
      ""widgets"": { ""relogioHabilitado"": true, ""pomodoroHabilitado"": false }
    }
  ]
}";
        var arquivoConfig = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(arquivoConfig, jsonV2);

        var repo = new JsonSettingsRepository(_tempDir);
        var prefsMigradas = repo.Carregar();

        Assert.Equal(8, prefsMigradas.SchemaVersion);
        Assert.True(prefsMigradas.UsarComoBarraPrincipal);
        Assert.NotEmpty(prefsMigradas.AppsPermanentes);
        Assert.Contains(prefsMigradas.OrdemSecoes, s => s.Tipo == TipoSecaoDock.ClimaInline);
        Assert.Contains(prefsMigradas.OrdemSecoes, s => s.Tipo == TipoSecaoDock.MidiaInline);
        Assert.NotNull(prefsMigradas.ColecoesGlobais);
        Assert.NotNull(prefsMigradas.Espacadores);
    }

    [Fact]
    public void Preferencias_Clonar_GeraCopiaIndependenteParaLivePreview()
    {
        var prefs = Preferencias.CriarPadrao();
        var clone = prefs.Clonar();

        Assert.NotSame(prefs, clone);
        Assert.Equal(prefs.Ambientes.Count, clone.Ambientes.Count);

        // Modificar clone não deve alterar original
        clone.Tema = TemaModo.Claro;
        clone.Ambientes[0].Nome = "Trabalho Modificado";

        Assert.Equal(TemaModo.Escuro, prefs.Tema);
        Assert.Equal("Trabalho", prefs.Ambientes[0].Nome);
    }

    [Fact]
    public void Preferencias_RestaurarAmbientePadrao_RestauraItensOriginais()
    {
        var prefs = Preferencias.CriarPadrao();
        var amb = prefs.Ambientes.First(a => a.Nome == "Trabalho");
        amb.Itens.Clear();
        Assert.Empty(amb.Itens);

        prefs.RestaurarAmbientePadrao(amb.Id);

        Assert.NotEmpty(amb.Itens);
        Assert.Contains(amb.Itens, i => i.Titulo == "Navegador Web");
    }
}

