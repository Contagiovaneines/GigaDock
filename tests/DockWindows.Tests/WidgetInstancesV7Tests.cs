using DockWindows.Core.Models;
using DockWindows.Infrastructure.Persistence;
using System.IO;
using Xunit;

namespace DockWindows.Tests;

public sealed class WidgetInstancesV7Tests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "DockWindows_WidgetV7_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ConfiguracaoDaInstancia_PersisteSemCompartilharEntreWidgets()
    {
        var primeiro = new WidgetInstanceConfig { Tipo = TipoWidget.Relogio };
        var segundo = new WidgetInstanceConfig { Tipo = TipoWidget.Relogio };

        primeiro.DefinirConfiguracao("fusoHorarioId", "E. South America Standard Time");
        segundo.DefinirConfiguracao("fusoHorarioId", "Tokyo Standard Time");

        Assert.NotEqual(primeiro.Id, segundo.Id);
        Assert.Equal("E. South America Standard Time", primeiro.ObterConfiguracao("fusoHorarioId"));
        Assert.Equal("Tokyo Standard Time", segundo.ObterConfiguracao("fusoHorarioId"));
        Assert.True(WidgetCapabilities.PermiteMultiplasInstancias(TipoWidget.Relogio));
        Assert.True(WidgetCapabilities.PermiteMultiplasInstancias(TipoWidget.Notas));
        Assert.False(WidgetCapabilities.PermiteMultiplasInstancias(TipoWidget.OBSStudio));
    }

    [Fact]
    public void MigracaoV6_GeraIdsUnicosEPreservaConfiguracaoPorInstancia()
    {
        Directory.CreateDirectory(_pasta);
        File.WriteAllText(Path.Combine(_pasta, "settings.json"), """
        {
          "schemaVersion": 6,
          "ambienteAtivoId": "amb",
          "gitHubUsuario": "octocat",
          "gitHubAnimacao": "PacMan",
          "localizacaoClima": "São Paulo",
          "urlIcal": "C:\\agenda.ics",
          "compromissosLocais": [{ "id": "c1", "titulo": "Reunião", "dataHora": "2026-10-07T10:00:00" }],
          "ambientes": [{
            "id": "amb",
            "nome": "Pessoal",
            "widgetsInstalados": [
              { "id": "duplicado", "tipo": "Relogio", "nome": "Local", "visivel": true },
              { "id": "duplicado", "tipo": "Relogio", "nome": "Tóquio", "visivel": true },
              { "id": "github", "tipo": "GitHubContribuicoes", "nome": "GitHub", "visivel": true },
              { "id": "clima", "tipo": "Clima", "nome": "Clima", "visivel": true },
              { "id": "calendario", "tipo": "CalendarioCompromissos", "nome": "Calendário", "visivel": true }
            ]
          }]
        }
        """);

        var preferencias = new JsonSettingsRepository(_pasta).Carregar();
        var widgets = preferencias.Ambientes.Single().WidgetsInstalados;

        Assert.Equal(8, preferencias.SchemaVersion);
        Assert.Equal(widgets.Count, widgets.Select(w => w.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal("octocat", widgets.Single(w => w.Tipo == TipoWidget.GitHubContribuicoes).ObterConfiguracao("usuario"));
        Assert.Equal("PacMan", widgets.Single(w => w.Tipo == TipoWidget.GitHubContribuicoes).ObterConfiguracao("animacao"));
        Assert.Equal("São Paulo", widgets.Single(w => w.Tipo == TipoWidget.Clima).ObterConfiguracao("localizacao"));
        var calendario = widgets.Single(w => w.Tipo == TipoWidget.CalendarioCompromissos);
        Assert.Equal("C:\\agenda.ics", calendario.ObterConfiguracao("urlIcal"));
        var compromissos = System.Text.Json.JsonSerializer.Deserialize<List<CompromissoLocal>>(calendario.ObterConfiguracao("compromissos"));
        Assert.Equal("Reunião", Assert.Single(compromissos!).Titulo);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true); } catch { }
    }
}
