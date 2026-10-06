using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Input;
using System.Xml.Linq;
using DockWindows.App.Common;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class CotacaoMoedasViewModel : ObservableObject, IAtividadeWidget
{
    private const string Endpoint = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };
    private readonly string _cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "cache", "cotacoes-ecb.json");
    private bool _disposed;
    private bool _habilitado;
    private bool _carregando;
    private string _texto = "Carregando…";
    private string _erro = string.Empty;
    private DateTimeOffset _atualizadoEm;
    private Dictionary<string, decimal> _taxas = new(StringComparer.OrdinalIgnoreCase) { ["EUR"] = 1m };

    public CotacaoMoedasViewModel() => AtualizarCommand = new RelayCommand(() => _ = AtualizarAsync());
    public bool? EmExecucao => Carregando;
    public SaudeWidget Saude => string.IsNullOrEmpty(Erro) ? SaudeWidget.Disponivel : SaudeWidget.Erro;
    public string? MotivoEstado => string.IsNullOrEmpty(Erro) ? $"Atualizado em {AtualizadoEm:dd/MM HH:mm}" : Erro;
    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public bool Carregando { get => _carregando; private set => SetProperty(ref _carregando, value); }
    public string Texto { get => _texto; private set => SetProperty(ref _texto, value); }
    public string Erro { get => _erro; private set => SetProperty(ref _erro, value); }
    public DateTimeOffset AtualizadoEm { get => _atualizadoEm; private set => SetProperty(ref _atualizadoEm, value); }
    public string MoedaBase { get; private set; } = "USD";
    public string MoedaDestino { get; private set; } = "BRL";
    public ICommand AtualizarCommand { get; }

    public void Configurar(string moedaBase, string moedaDestino)
    {
        MoedaBase = NormalizarMoeda(moedaBase, "USD");
        MoedaDestino = NormalizarMoeda(moedaDestino, "BRL");
        Recalcular();
    }

    public async Task AtualizarAsync()
    {
        if (_disposed || Carregando) return;
        Carregando = true;
        try
        {
            if (CarregarCache() && DateTimeOffset.Now - AtualizadoEm < TimeSpan.FromHours(24)) { Recalcular(); return; }
            var xml = await Http.GetStringAsync(Endpoint);
            var doc = XDocument.Parse(xml);
            var taxas = doc.Descendants().Where(x => x.Name.LocalName == "Cube" && x.Attribute("currency") != null)
                .Select(x => new { Codigo = x.Attribute("currency")!.Value, Valor = decimal.Parse(x.Attribute("rate")!.Value, CultureInfo.InvariantCulture) })
                .ToDictionary(x => x.Codigo, x => x.Valor, StringComparer.OrdinalIgnoreCase);
            taxas["EUR"] = 1m;
            _taxas = taxas;
            AtualizadoEm = DateTimeOffset.Now;
            Erro = string.Empty;
            SalvarCache();
            Recalcular();
        }
        catch (Exception)
        {
            if (CarregarCache()) { Erro = "Sem rede; exibindo a última cotação salva."; Recalcular(); }
            else { Erro = "Não foi possível obter as taxas do BCE."; Texto = $"{MoedaBase}/{MoedaDestino} indisponível"; }
        }
        finally { Carregando = false; }
    }

    public void DefinirAtividade(EstadoAtividade estado)
    {
        if (_disposed || !estado.Habilitado) return;
        if (estado.Visual && (_taxas.Count <= 1 || DateTimeOffset.Now - AtualizadoEm >= TimeSpan.FromHours(24))) _ = AtualizarAsync();
    }

    private void Recalcular()
    {
        if (!_taxas.TryGetValue(MoedaBase, out var origem) || !_taxas.TryGetValue(MoedaDestino, out var destino) || origem == 0) return;
        Texto = $"1 {MoedaBase} = {destino / origem:N4} {MoedaDestino}";
    }

    private bool CarregarCache()
    {
        try
        {
            if (!File.Exists(_cachePath)) return false;
            var cache = JsonSerializer.Deserialize<CacheCotacao>(File.ReadAllText(_cachePath));
            if (cache?.Taxas == null || cache.Taxas.Count == 0) return false;
            _taxas = new Dictionary<string, decimal>(cache.Taxas, StringComparer.OrdinalIgnoreCase);
            AtualizadoEm = cache.AtualizadoEm;
            return true;
        }
        catch { return false; }
    }

    private void SalvarCache()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(_cachePath)!); File.WriteAllText(_cachePath, JsonSerializer.Serialize(new CacheCotacao(AtualizadoEm, _taxas))); } catch { }
    }

    private static string NormalizarMoeda(string? valor, string padrao) =>
        !string.IsNullOrWhiteSpace(valor) && valor.Trim().Length == 3 ? valor.Trim().ToUpperInvariant() : padrao;
    public void Dispose() => _disposed = true;
    private sealed record CacheCotacao(DateTimeOffset AtualizadoEm, Dictionary<string, decimal> Taxas);
}
