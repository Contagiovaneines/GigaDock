using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public record PrevisaoClima(string Dia, string Temperatura, string Condicao, string TipoIcone, string Minima = "—°")
{
    public string Icone => Simbolo(TipoIcone);
    public static string Simbolo(string tipo) => tipo switch { "Sol" => "☀", "Nuvem" => "☁", "Chuva" => "☂", "Neve" => "❄", "Tempestade" => "ϟ", _ => "—" };
}
public record PrevisaoHora(string Hora, string Temperatura, string TipoIcone)
{
    public string Icone => PrevisaoClima.Simbolo(TipoIcone);
}
public record DadosClima(string Temperatura, string Local, string Condicao, IReadOnlyList<PrevisaoClima> Previsoes,
    IReadOnlyList<PrevisaoHora>? Horas = null, string Sensacao = "—°", string Vento = "—", string DirecaoVento = "—", string Precipitacao = "—", string NascerSol = "—", string PorSol = "—", string TipoAtual = "Indisponivel", DateTime? Observacao = null);

public class ClimaWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => TimerAtivo || _ocupado;

    public DockWindows.Core.Widgets.SaudeWidget Saude => !string.IsNullOrEmpty(ErroAtualizacao) ? DockWindows.Core.Widgets.SaudeWidget.Erro : TemDados ? DockWindows.Core.Widgets.SaudeWidget.Disponivel : DockWindows.Core.Widgets.SaudeWidget.Indisponivel;
    public string? MotivoEstado => DescricaoAtualizacao;

    private string _condicao = "Carregando previsão";
    private string _local = string.Empty;
    private string _temperatura = "—°";
    private bool _habilitado = true;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private string _localizacaoPreferida = string.Empty;
    private int _versaoAtualizacao;
    private readonly DispatcherTimer _timer;
    private readonly HttpClient _http;
    private readonly TimeProvider _relogio;
    private readonly bool _consultasPermitidas;
    private System.Threading.CancellationTokenSource? _consulta;
    private bool _visual, _ocupado, _retomar, _disposed;
    private DateTimeOffset _cacheAte;
    public int Requisicoes { get; private set; }
    public DateTimeOffset? UltimaAtualizacao { get; private set; }
    public bool TemDados => UltimaAtualizacao.HasValue;
    public bool DadosDesatualizados => TemDados && (_cacheAte <= _relogio.GetUtcNow() || !string.IsNullOrEmpty(ErroAtualizacao));
    private string _erroAtualizacao = "";
    public string ErroAtualizacao { get => _erroAtualizacao; private set => SetProperty(ref _erroAtualizacao, value); }
    public string EstadoCache => !TemDados ? "Sem dados" : DadosDesatualizados ? "Dados desatualizados" : "Dados atualizados";
    public string EstadoRede { get; private set; } = "Não consultada";
    public string DescricaoAtualizacao => EstadoCache + (UltimaAtualizacao.HasValue ? $" · Atualizado há {Math.Max(0, (int)(_relogio.GetUtcNow() - UltimaAtualizacao.Value).TotalMinutes)} min" : "") + (string.IsNullOrEmpty(ErroAtualizacao) ? "" : " · " + ErroAtualizacao);
    private void NotificarCache()
    { foreach (var p in new[] { nameof(UltimaAtualizacao), nameof(TemDados), nameof(DadosDesatualizados), nameof(EstadoCache), nameof(EstadoRede), nameof(DescricaoAtualizacao), nameof(DescricaoDados) }) OnPropertyChanged(p); }
    private void AgendarConsulta()
    {
        _timer.Stop(); if (!_visual || _disposed || !_consultasPermitidas) return;
        var espera = _cacheAte - _relogio.GetUtcNow();
        _timer.Interval = espera > TimeSpan.Zero ? espera : TimeSpan.FromSeconds(30);
        _timer.Start();
    }
    public bool TimerAtivo => _timer.IsEnabled;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        _visual = estado.Visual && !_disposed; _timer.Stop();
        if (!_visual) { _consulta?.Cancel(); return; }
        _ = AtualizarClimaAsync(); NotificarCache(); AgendarConsulta();
    }
    public void Dispose() { _disposed = true; _visual = false; _timer.Stop(); _consulta?.Cancel(); _http.Dispose(); }

    private string _estilo = "compacto";
    public string Estilo { get => _estilo; set { if (SetProperty(ref _estilo, value)) OnPropertyChanged(nameof(Largura)); } }
    public double Largura => DockWindows.App.Controls.ClimaEstiloControl.LarguraPara(Estilo);
    public string DescricaoDados => DescricaoAtualizacao + "\n" + $"{Local} · {Condicao} · {Temperatura}\nVento: {Vento} {DirecaoVento} · Precipitação observada: {Precipitacao}\nSol: {NascerSol}–{PorSol}\n" + (Observacao.HasValue ? $"Observação local: {Observacao:dd/MM HH:mm}\n" : "") + "Clique para detalhes; botão direito ou Shift+F10 para personalizar. Previsão diária: máxima de cada dia.";
    public string DicaCurta => $"{Local}: {Temperatura} · {Condicao}";
    private string _vento = "—", _direcaoVento = "—", _precipitacao = "—", _nascerSol = "—", _porSol = "—", _tipoAtual = "Indisponivel";
    private DateTime? _observacao;
    public string Vento { get => _vento; private set => SetProperty(ref _vento, value); }
    public string DirecaoVento { get => _direcaoVento; private set => SetProperty(ref _direcaoVento, value); }
    public string Precipitacao { get => _precipitacao; private set => SetProperty(ref _precipitacao, value); }
    public string NascerSol { get => _nascerSol; private set => SetProperty(ref _nascerSol, value); }
    public string PorSol { get => _porSol; private set => SetProperty(ref _porSol, value); }
    public string TipoAtual { get => _tipoAtual; private set => SetProperty(ref _tipoAtual, value); }
    public DateTime? Observacao { get => _observacao; private set => SetProperty(ref _observacao, value); }

    private string _sensacao = "—°";
    public string Sensacao { get => _sensacao; set => SetProperty(ref _sensacao, value); }
    public ObservableCollection<PrevisaoHora> Horas { get; } = new();
    public string Condicao { get => _condicao; set { if (SetProperty(ref _condicao, value)) { OnPropertyChanged(nameof(DescricaoDados)); OnPropertyChanged(nameof(DicaCurta)); } } }
    public string Local { get => _local; set { if (SetProperty(ref _local, value)) OnPropertyChanged(nameof(DicaCurta)); } }
    public string Temperatura { get => _temperatura; set { if (SetProperty(ref _temperatura, value)) OnPropertyChanged(nameof(DicaCurta)); } }
    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public ObservableCollection<PrevisaoClima> Previsoes { get; } = new()
    {
        new("—", "—°", "Previsão indisponível", "Indisponivel"),
        new("—", "—°", "Previsão indisponível", "Indisponivel"),
        new("—", "—°", "Previsão indisponível", "Indisponivel")
    };

    public ClimaWidgetViewModel(bool iniciarConsulta = true, HttpClient? http = null, TimeProvider? relogio = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _relogio = relogio ?? TimeProvider.System;
        _consultasPermitidas = iniciarConsulta;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _timer.Tick += (_, _) => { _timer.Stop(); _ = AtualizarClimaAsync(); AgendarConsulta(); };

    }

    public void SincronizarLocalizacao(string localizacao)
    {
        var nova = localizacao ?? string.Empty;
        if (_localizacaoPreferida != nova)
        {
            Temperatura = "—°";
            Sensacao = "—°";
            Vento = DirecaoVento = Precipitacao = NascerSol = PorSol = "—";
            TipoAtual = "Indisponivel";
            Observacao = null;
            Horas.Clear();
            Local = nova;
            Condicao = "Carregando previsão";
            for (int i = 0; i < Previsoes.Count; i++)
                Previsoes[i] = new("—", "—°", "Previsão indisponível", "Indisponivel");
        }
        if (_localizacaoPreferida != nova) { _cacheAte = default; _consulta?.Cancel(); }
        _localizacaoPreferida = nova;
        if (_visual) { _ = AtualizarClimaAsync(); AgendarConsulta(); }
    }

    public static DadosClima InterpretarResposta(string resposta)
    {
        using var json = JsonDocument.Parse(resposta);
        var root = json.RootElement;
        var atual = root.GetProperty("current_condition")[0];
        var temperatura = atual.GetProperty("temp_C").GetString() + "°";
        var condicao = DescreverCondicao(atual.GetProperty("weatherCode").GetString());
        string local = string.Empty;
        if (root.TryGetProperty("nearest_area", out var areas) && areas.GetArrayLength() > 0)
            local = areas[0].GetProperty("areaName")[0].GetProperty("value").GetString() ?? string.Empty;

        var previsoes = new List<PrevisaoClima>();
        foreach (var dia in root.GetProperty("weather").EnumerateArray().Take(3))
        {
            var data = DateOnly.ParseExact(dia.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var horas = dia.GetProperty("hourly").EnumerateArray().ToList();
            var meioDia = horas.FirstOrDefault(h => h.GetProperty("time").GetString() == "1200");
            if (meioDia.ValueKind == JsonValueKind.Undefined && horas.Count > 0) meioDia = horas[0];
            var codigo = meioDia.ValueKind == JsonValueKind.Undefined ? null : meioDia.GetProperty("weatherCode").GetString();
            var nomeDia = data.ToString("ddd", CultureInfo.GetCultureInfo("pt-BR")).TrimEnd('.');
            previsoes.Add(new(nomeDia, dia.GetProperty("maxtempC").GetString() + "°",
                DescreverCondicao(codigo), TipoIcone(codigo), dia.TryGetProperty("mintempC", out var minima) ? minima.GetString() + "°" : "—°"));
        }
        while (previsoes.Count < 3)
            previsoes.Add(new("—", "—°", "Previsão indisponível", "Indisponivel"));
        var previsoesHora = new List<PrevisaoHora>();
        DateTime? observacao = atual.TryGetProperty("localObsDateTime", out var obs) &&
            DateTime.TryParse(obs.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
        foreach (var dia in root.GetProperty("weather").EnumerateArray())
        {
            var data = DateOnly.ParseExact(dia.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            foreach (var hora in dia.GetProperty("hourly").EnumerateArray())
            {
                if (!hora.TryGetProperty("tempC", out var temp) || !int.TryParse(hora.GetProperty("time").GetString(), out var tempo)) continue;
                var instante = data.ToDateTime(new TimeOnly(Math.Clamp(tempo / 100, 0, 23), 0));
                if (observacao.HasValue && instante < observacao.Value) continue;
                var label = (observacao.HasValue && data != DateOnly.FromDateTime(observacao.Value) ? data.ToString("ddd", CultureInfo.GetCultureInfo("pt-BR")).TrimEnd('.') + " " : "") + $"{tempo / 100:00}h";
                previsoesHora.Add(new(label, temp.GetString() + "°", TipoIcone(hora.GetProperty("weatherCode").GetString())));
                if (previsoesHora.Count == 5) break;
            }
            if (previsoesHora.Count == 5) break;
        }
        var sensacao = atual.TryGetProperty("FeelsLikeC", out var feels) ? feels.GetString() + "°" : "—°";
        string Numero(string campo, string unidade) => atual.TryGetProperty(campo, out var value) && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && n >= 0 ? n.ToString("0.#", CultureInfo.GetCultureInfo("pt-BR")) + unidade : "—";
        var direcao = atual.TryGetProperty("winddir16Point", out var dir) ? dir.GetString()?.Replace("W", "O") ?? "—" : "—";
        string nascer = "—", por = "—";
        var primeiro = root.GetProperty("weather").EnumerateArray().FirstOrDefault();
        if (primeiro.ValueKind != JsonValueKind.Undefined && primeiro.TryGetProperty("astronomy", out var astros) && astros.ValueKind == JsonValueKind.Array && astros.GetArrayLength() > 0)
        {
            string Solar(string campo) => astros[0].TryGetProperty(campo, out var value) && DateTime.TryParseExact(value.GetString(), new[] { "hh:mm tt", "h:mm tt" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ? time.ToString("HH:mm") : "—";
            nascer = Solar("sunrise"); por = Solar("sunset");
        }
        return new(temperatura, local, condicao, previsoes, previsoesHora, sensacao, Numero("windspeedKmph", " km/h"), direcao, Numero("precipMM", " mm"), nascer, por, TipoIcone(atual.GetProperty("weatherCode").GetString()), observacao);
    }

    private static string TipoIcone(string? codigo) => codigo switch
    {
        "113" => "Sol",
        "116" or "119" or "122" or "143" or "248" or "260" => "Nuvem",
        "200" or "386" or "389" or "392" or "395" => "Tempestade",
        "179" or "182" or "185" or "227" or "230" or "281" or "284" or "311" or "314"
            or "317" or "320" or "323" or "326" or "329" or "332" or "335" or "338"
            or "350" or "362" or "365" or "368" or "371" or "374" or "377" => "Neve",
        "176" or "263" or "266" or "293" or "296" or "299" or "302" or "305" or "308"
            or "353" or "356" or "359" => "Chuva",
        _ => "Indisponivel"
    };

    private static string DescreverCondicao(string? codigo) => TipoIcone(codigo) switch
    {
        "Sol" => "Céu limpo", "Nuvem" => "Nublado", "Chuva" => "Chuva",
        "Tempestade" => "Tempestade", "Neve" => "Neve ou gelo", _ => "Condição indisponível"
    };

    private async Task AtualizarClimaAsync()
    {
        if (!_visual || !_consultasPermitidas || _disposed || _cacheAte > _relogio.GetUtcNow()) return;
        if (_ocupado) { _retomar = true; return; }
        _ocupado = true;
        var versao = ++_versaoAtualizacao;
        using var consulta = new System.Threading.CancellationTokenSource();
        _consulta = consulta;
        var localConsulta = _localizacaoPreferida;
        try
        {
            var url = "https://wttr.in/" + Uri.EscapeDataString(localConsulta) + "?format=j1&lang=pt";
            Requisicoes++;
            var resposta = await _http.GetStringAsync(url, consulta.Token);
            var dados = InterpretarResposta(resposta);
            await AplicarNaInterfaceAsync(() =>
            {
                if (versao != _versaoAtualizacao || consulta.IsCancellationRequested || !_visual || _disposed || localConsulta != _localizacaoPreferida) return;
                _cacheAte = _relogio.GetUtcNow().AddHours(1);
                EstadoRede = "Resposta recebida"; ErroAtualizacao = "";
                AplicarDados(dados);
            });
        }
        catch (OperationCanceledException) { }
        catch
        {
            _cacheAte = _relogio.GetUtcNow().AddSeconds(30);
            await AplicarNaInterfaceAsync(() =>
            {
                if (versao == _versaoAtualizacao && _visual && !_disposed && localConsulta == _localizacaoPreferida)
                {
                    EstadoRede = "Falha de atualização";
                    ErroAtualizacao = "Não foi possível atualizar o clima. Verifique sua conexão ou localização.";
                    if (!TemDados) Condicao = "Previsão indisponível";
                    NotificarCache();
                }
            });
        }
        finally
        {
            _ocupado = false; AgendarConsulta();
            if (ReferenceEquals(_consulta, consulta)) _consulta = null;
            if (_retomar) { _retomar = false; if (_visual && !_disposed) _ = AtualizarClimaAsync(); }
        }
    }

    public void AplicarDados(DadosClima dados)
    {
        UltimaAtualizacao = _relogio.GetUtcNow();
        Temperatura = dados.Temperatura; Local = dados.Local; Condicao = dados.Condicao; Sensacao = dados.Sensacao;
        Vento = dados.Vento; DirecaoVento = dados.DirecaoVento; Precipitacao = dados.Precipitacao;
        NascerSol = dados.NascerSol; PorSol = dados.PorSol; TipoAtual = dados.TipoAtual; Observacao = dados.Observacao;
        Horas.Clear(); foreach (var hora in dados.Horas ?? Array.Empty<PrevisaoHora>()) Horas.Add(hora);
        Previsoes.Clear(); foreach (var dia in dados.Previsoes.Take(3)) Previsoes.Add(dia);
        NotificarCache();
    }

    private static Task AplicarNaInterfaceAsync(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            return dispatcher.InvokeAsync(action).Task;
        action();
        return Task.CompletedTask;
    }
}
