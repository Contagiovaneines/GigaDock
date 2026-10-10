using DockWindows.Core.Models;
using GigaDock.Services.Persistence;

namespace GigaDock.Tests.Core;

public sealed class NewIdeasTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GigaDock-ideas-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Duplicate_IsIndependentAcrossNestedObjectsAndPersistence()
    {
        var source = new Ambiente { Nome = "Original", Itens = [new ItemFixado { Titulo = "Site" }],
            Colecoes = [new ColecaoApp { Itens = [new ItemFixado { Titulo = "Arquivo" }] }],
            WidgetsInstalados = [new WidgetInstanceConfig { Configuracao = new() { ["cidade"] = "Curitiba", ["UltimoRegistro"] = "ontem" } }] };
        source.OrdemAplicativosDock = ["global", source.Itens[0].Id];
        var duplicate = AmbienteDuplicador.Duplicar(source, "Cópia");
        Assert.Equal(new[] { "global", duplicate.Itens[0].Id }, duplicate.OrdemAplicativosDock);
        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.NotEqual(source.Itens[0].Id, duplicate.Itens[0].Id);
        Assert.NotEqual(source.Colecoes[0].Id, duplicate.Colecoes[0].Id);
        Assert.NotEqual(source.Colecoes[0].Itens[0].Id, duplicate.Colecoes[0].Itens[0].Id);
        Assert.NotEqual(source.WidgetsInstalados[0].Id, duplicate.WidgetsInstalados[0].Id);
        duplicate.Itens[0].Titulo = "Alterado";
        duplicate.WidgetsInstalados[0].DefinirConfiguracao("cidade", "Recife");
        Assert.Equal("Site", source.Itens[0].Titulo);
        Assert.Equal("Curitiba", source.WidgetsInstalados[0].ObterConfiguracao("cidade"));
        Assert.Equal("", duplicate.WidgetsInstalados[0].ObterConfiguracao("UltimoRegistro"));
        var prefs = new Preferencias { Ambientes = [source, duplicate], AmbienteAtivoId = source.Id };
        var repository = new JsonSettingsRepository(_root); repository.Salvar(prefs);
        Assert.Equal("Alterado", repository.Carregar().Ambientes.Single(a => a.Id == duplicate.Id).Itens[0].Titulo);
    }

    [Fact]
    public void Tasks_PersistAndStayIsolatedEvenWithPathLikeEnvironmentIds()
    {
        var store = new LocalTaskStore(_root);
        var added = Assert.Single(store.Add("../../ambiente", "Estudar"));
        Assert.Empty(store.Read("outro"));
        store.Complete("../../ambiente", added.Id, true);
        Assert.True(Assert.Single(new LocalTaskStore(_root).Read("../../ambiente")).Completed);
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "tasks")));
        Assert.Empty(store.Remove("../../ambiente", added.Id));
    }

    [Fact]
    public void Tasks_InvalidEditAndCorruptStorageDoNotOverwriteExistingData()
    {
        var store = new LocalTaskStore(_root); store.Add("work", "Válida");
        var file = Assert.Single(Directory.GetFiles(Path.Combine(_root, "tasks")));
        var original = File.ReadAllText(file);
        Assert.Throws<ArgumentException>(() => store.Add("work", "\n"));
        Assert.Throws<ArgumentException>(() => store.Complete("work", "ausente", true));
        Assert.Equal(original, File.ReadAllText(file));
        File.WriteAllText(file, "corrompido");
        Assert.Throws<System.Text.Json.JsonException>(() => store.Add("work", "Nova"));
        Assert.Equal("corrompido", File.ReadAllText(file));
    }

    [Theory]
    [InlineData("POKEMON", "Pokédex")]
    [InlineData("opacidade", "Aparência")]
    [InlineData("tarefas", "Widgets e tarefas")]
    [InlineData("tela monitor", "Visualizações")]
    public void Search_UsesAccentInsensitiveKeywords(string query, string title) =>
        Assert.Equal(title, Assert.Single(SettingsSearch.Find(query)).Title);

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
