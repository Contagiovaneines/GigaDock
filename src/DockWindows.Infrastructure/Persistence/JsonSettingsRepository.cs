using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Persistence;

public class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _diretorioConfiguracoes;
    private readonly string _caminhoArquivo;
    private readonly string _caminhoTemp;
    private readonly string _caminhoBackup;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsRepository(string? customFolder = null)
    {
        _diretorioConfiguracoes = customFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DockWindows");

        _caminhoArquivo = Path.Combine(_diretorioConfiguracoes, "settings.json");
        _caminhoTemp = Path.Combine(_diretorioConfiguracoes, "settings.json.tmp");
        _caminhoBackup = Path.Combine(_diretorioConfiguracoes, "settings.json.bak");

        if (!Directory.Exists(_diretorioConfiguracoes))
        {
            Directory.CreateDirectory(_diretorioConfiguracoes);
        }
    }

    public string ObterCaminhoConfiguracoes() => _caminhoArquivo;

    public Preferencias Carregar()
    {
        lock (_lock)
        {
            // 1. Tentar ler do arquivo principal
            if (File.Exists(_caminhoArquivo))
            {
                try
                {
                    var json = File.ReadAllText(_caminhoArquivo);
                    var prefs = JsonSerializer.Deserialize<Preferencias>(json, SerializerOptions);
                    if (prefs != null && prefs.Ambientes.Count > 0)
                    {
                        ValidarPreferenciasCarregadas(prefs);
                        return prefs;
                    }
                }
                catch
                {
                    // Falha na leitura do arquivo principal, tentar backup
                }
            }

            // 2. Se o principal falhar ou estiver corrompido, tentar o backup
            if (File.Exists(_caminhoBackup))
            {
                try
                {
                    var jsonBak = File.ReadAllText(_caminhoBackup);
                    var prefsBak = JsonSerializer.Deserialize<Preferencias>(jsonBak, SerializerOptions);
                    if (prefsBak != null && prefsBak.Ambientes.Count > 0)
                    {
                        ValidarPreferenciasCarregadas(prefsBak);
                        // Restaura o backup como principal sem perder o arquivo de backup
                        SalvarInterno(prefsBak, criarBackup: false);
                        return prefsBak;
                    }
                }
                catch
                {
                    // Backup também falhou
                }
            }

            // 3. Fallback: cria a configuração padrão com Trabalho, Estudos e Pessoal
            var padrao = Preferencias.CriarPadrao();
            Salvar(padrao);
            return padrao;
        }
    }

    public void Salvar(Preferencias prefs)
    {
        SalvarInterno(prefs, criarBackup: true);
    }

    private void SalvarInterno(Preferencias prefs, bool criarBackup)
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(prefs, SerializerOptions);

                // Escrita atômica: grava no .tmp primeiro
                File.WriteAllText(_caminhoTemp, json);

                // Se o arquivo original existe e foi solicitado backup, copia para .bak antes de sobrescrever
                if (criarBackup && File.Exists(_caminhoArquivo))
                {
                    File.Copy(_caminhoArquivo, _caminhoBackup, overwrite: true);
                }

                // Substitui o original pelo temporário
                File.Move(_caminhoTemp, _caminhoArquivo, overwrite: true);
            }
            catch (Exception ex)
            {
                // Limpeza defensiva do arquivo temporário se algo falhar
                try
                {
                    if (File.Exists(_caminhoTemp))
                        File.Delete(_caminhoTemp);
                }
                catch { }

                throw new InvalidOperationException($"Não foi possível salvar as configurações locais: {ex.Message}", ex);
            }
        }
    }

    public Task SalvarAsync(Preferencias prefs)
    {
        return Task.Run(() => Salvar(prefs));
    }

    private void ValidarPreferenciasCarregadas(Preferencias prefs)
    {
        if (string.IsNullOrWhiteSpace(prefs.AmbienteAtivoId) ||
            !prefs.Ambientes.Any(a => a.Id == prefs.AmbienteAtivoId))
        {
            prefs.AmbienteAtivoId = prefs.Ambientes.FirstOrDefault()?.Id ?? "ambiente-trabalho";
        }

        // Migração de schema v1 -> v2
        if (prefs.SchemaVersion < 2)
        {
            prefs.SchemaVersion = 2;
            if (prefs.EspacamentoItens <= 0) prefs.EspacamentoItens = 6;
            prefs.ExibirSeletorAmbientes = true;
            prefs.ExibirItensFixados = true;
            prefs.ExibirBotoesAcao = true;
        }

        // Migração de schema v2 -> v3
        if (prefs.SchemaVersion < 3)
        {
            prefs.SchemaVersion = 3;
            if (prefs.AppsPermanentes == null || prefs.AppsPermanentes.Count == 0)
            {
                prefs.AppsPermanentes = Preferencias.CriarAppsPermanentesPadrao();
            }
            if (prefs.OrdemSecoes == null || prefs.OrdemSecoes.Count == 0)
            {
                prefs.OrdemSecoes = Preferencias.CriarOrdemSecoesPadrao();
            }
            try
            {
                SalvarInterno(prefs, criarBackup: true);
            }
            catch { }
        }

        // Migração de schema v3 -> v4
        if (prefs.SchemaVersion < 4)
        {
            prefs.SchemaVersion = 4;
            if (prefs.ColecoesGlobais == null || prefs.ColecoesGlobais.Count == 0)
            {
                prefs.ColecoesGlobais = Preferencias.CriarColecoesGlobaisPadrao();
            }
            if (prefs.Espacadores == null || prefs.Espacadores.Count == 0)
            {
                prefs.Espacadores = Preferencias.CriarEspacadoresPadrao();
            }
            if (prefs.CompromissosLocais == null)
            {
                prefs.CompromissosLocais = Preferencias.CriarCompromissosPadrao();
            }
            if (prefs.WidgetsGlobais == null || prefs.WidgetsGlobais.Count == 0)
            {
                prefs.WidgetsGlobais = Preferencias.CriarWidgetsPadrao();
            }
            if (prefs.RaioCantosDock <= 0)
            {
                prefs.RaioCantosDock = 20.0;
            }
            if (string.IsNullOrWhiteSpace(prefs.VelocidadeAnimacao))
            {
                prefs.VelocidadeAnimacao = "Normal";
            }
            if (prefs.OrdemSecoes != null && !prefs.OrdemSecoes.Any(s => s.Tipo == TipoSecaoDock.Colecoes))
            {
                prefs.OrdemSecoes = Preferencias.CriarOrdemSecoesPadrao();
            }
            foreach (var amb in prefs.Ambientes)
            {
                amb.Colecoes ??= new List<ColecaoApp>();
                if (amb.WidgetsInstalados == null || amb.WidgetsInstalados.Count == 0)
                {
                    amb.WidgetsInstalados = Preferencias.CriarWidgetsPadrao();
                }
                if (string.IsNullOrWhiteSpace(amb.CorIndicadorApps))
                {
                    amb.CorIndicadorApps = "#0A84FF";
                }
                if (string.IsNullOrWhiteSpace(amb.EstiloIndicadorApps))
                {
                    amb.EstiloIndicadorApps = "Barra";
                }
            }

        }

            // Migração v4 -> v5
            if (prefs.SchemaVersion < 5)
            {
                prefs.SchemaVersion = 5;
                prefs.ExibirSeletorAmbientes = false;
                prefs.RaioCantosDock = 100.0;

                if (prefs.OrdemSecoes != null)
                {
                    if (!prefs.OrdemSecoes.Any(s => s.Tipo == TipoSecaoDock.ClimaInline))
                    {
                        prefs.OrdemSecoes.Insert(1, new ConfigSecaoDock { Tipo = TipoSecaoDock.ClimaInline, Nome = "Clima Inline", Visivel = true, Ordem = 1 });
                    }
                    if (!prefs.OrdemSecoes.Any(s => s.Tipo == TipoSecaoDock.MidiaInline))
                    {
                        prefs.OrdemSecoes.Insert(2, new ConfigSecaoDock { Tipo = TipoSecaoDock.MidiaInline, Nome = "Mídia Inline", Visivel = true, Ordem = 2 });
                    }
                    for (int i = 0; i < prefs.OrdemSecoes.Count; i++)
                    {
                        prefs.OrdemSecoes[i].Ordem = i;
                    }
                }
            }

            // Migração v5 -> v6
            if (prefs.SchemaVersion < 6)
            {
                prefs.SchemaVersion = 6;
                prefs.ExibirBotoesAcao = false;
            }

            // Migração v6 -> v7: identidade e configuração própria por instância.
            if (prefs.SchemaVersion < 7)
            {
                prefs.SchemaVersion = 7;
                foreach (var ambiente in prefs.Ambientes)
                {
                    ambiente.WidgetsInstalados ??= new List<WidgetInstanceConfig>();
                    var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var widget in ambiente.WidgetsInstalados)
                    {
                        widget.Configuracao ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        if (string.IsNullOrWhiteSpace(widget.Id) || !ids.Add(widget.Id))
                        {
                            widget.Id = Guid.NewGuid().ToString("N");
                            ids.Add(widget.Id);
                        }

                        if (widget.Tipo == TipoWidget.GitHubContribuicoes)
                        {
                            if (!string.IsNullOrWhiteSpace(prefs.GitHubUsuario)) widget.Configuracao.TryAdd("usuario", prefs.GitHubUsuario);
                            if (!string.IsNullOrWhiteSpace(prefs.GitHubAnimacao)) widget.Configuracao.TryAdd("animacao", prefs.GitHubAnimacao);
                        }
                        else if (widget.Tipo == TipoWidget.Clima && !string.IsNullOrWhiteSpace(prefs.LocalizacaoClima))
                        {
                            widget.Configuracao.TryAdd("localizacao", prefs.LocalizacaoClima);
                        }
                    }
                }
            }

            // Migração v7 -> v8: calendário passa a pertencer ao ambiente/instância.
            if (prefs.SchemaVersion < 8)
            {
                prefs.SchemaVersion = 8;
                var compromissos = System.Text.Json.JsonSerializer.Serialize(prefs.CompromissosLocais ?? new());
                foreach (var widget in prefs.Ambientes.SelectMany(a => a.WidgetsInstalados ?? new List<WidgetInstanceConfig>())
                             .Where(w => w.Tipo == TipoWidget.CalendarioCompromissos))
                {
                    widget.Configuracao ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrWhiteSpace(prefs.UrlIcal)) widget.Configuracao.TryAdd("urlIcal", prefs.UrlIcal);
                    if ((prefs.CompromissosLocais?.Count ?? 0) > 0) widget.Configuracao.TryAdd("compromissos", compromissos);
                }
            }

            // Normalização também protege arquivos v7 editados manualmente ou parcialmente gravados.
            foreach (var ambiente in prefs.Ambientes)
            {
                ambiente.WidgetsInstalados ??= new List<WidgetInstanceConfig>();
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var widget in ambiente.WidgetsInstalados)
                {
                    widget.Configuracao ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (string.IsNullOrWhiteSpace(widget.Id) || !ids.Add(widget.Id))
                    {
                        widget.Id = Guid.NewGuid().ToString("N");
                        ids.Add(widget.Id);
                    }
                }
            }

            try
            {
                SalvarInterno(prefs, criarBackup: true);
            }
            catch { }
    }
}
