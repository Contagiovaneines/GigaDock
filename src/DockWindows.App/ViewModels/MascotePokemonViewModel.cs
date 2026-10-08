using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class MascotePokemonViewModel : ObservableObject, IAtividadeWidget, IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(12) };
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly Func<string> _obterClima;
    private CancellationTokenSource? _consulta;
    private bool _habilitado, _visual, _disposed;
    private int _pokemonId = 1, _pokemonExibidoId = 1;
    private int? _evolucaoEeveeSessao;
    private int[] _evolucoes = Array.Empty<int>();
    private string _nome = "Bulbasaur", _estado = "Descansando", _erro = string.Empty;
    private BitmapImage? _sprite;

    public MascotePokemonViewModel(Func<string> obterClima)
    {
        _obterClima = obterClima;
        _timer.Tick += (_, _) => AtualizarEstado();
    }

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public int PokemonId { get => _pokemonId; private set => SetProperty(ref _pokemonId, value); }
    public int PokemonExibidoId { get => _pokemonExibidoId; private set => SetProperty(ref _pokemonExibidoId, value); }
    public string Nome { get => _nome; private set => SetProperty(ref _nome, value); }
    public string Estado { get => _estado; private set => SetProperty(ref _estado, value); }
    public string Erro { get => _erro; private set => SetProperty(ref _erro, value); }
    public BitmapImage? Sprite { get => _sprite; private set => SetProperty(ref _sprite, value); }
    public bool? EmExecucao => _timer.IsEnabled;
    public SaudeWidget Saude => string.IsNullOrEmpty(Erro) ? SaudeWidget.Disponivel : SaudeWidget.Indisponivel;
    public string? MotivoEstado => string.IsNullOrEmpty(Erro) ? $"{Nome} · {Estado}" : Erro;
    public string Descricao => $"{Nome} · {Estado}. Evolução temporária desta sessão; reiniciar o Windows retorna ao Pokémon escolhido.";

    public async Task SelecionarAsync(int id)
    {
        if (_disposed || id is < 1 or > 151) return;
        PokemonId = id;
        PokemonExibidoId = id;
        _evolucaoEeveeSessao = null;
        _evolucoes = Array.Empty<int>();
        if (_visual) await CarregarAsync(id);
    }

    public void DefinirAtividade(EstadoAtividade estado)
    {
        _visual = estado.Habilitado && estado.Visual;
        Habilitado = estado.Habilitado;
        _timer.Stop();
        _consulta?.Cancel();
        if (!_visual || _disposed) return;
        _timer.Start();
        _ = CarregarAsync(PokemonId);
    }

    private async Task CarregarAsync(int id)
    {
        _consulta?.Cancel(); _consulta?.Dispose(); _consulta = new CancellationTokenSource();
        var token = _consulta.Token;
        try
        {
            Erro = string.Empty;
            using var pokemon = await ObterJsonAsync($"https://pokeapi.co/api/v2/pokemon/{id}", token);
            Nome = Capitalizar(pokemon.RootElement.GetProperty("name").GetString() ?? $"Pokémon {id}");
            var spriteUrl = pokemon.RootElement.GetProperty("sprites").GetProperty("front_default").GetString();
            using var species = await ObterJsonAsync($"https://pokeapi.co/api/v2/pokemon-species/{id}", token);
            var chainUrl = species.RootElement.GetProperty("evolution_chain").GetProperty("url").GetString();
            if (!string.IsNullOrWhiteSpace(chainUrl))
            {
                using var chain = await ObterJsonAsync(chainUrl, token);
                _evolucoes = ExtrairEvolucoes(chain.RootElement.GetProperty("chain"), id).Take(2).ToArray();
            }
            if (!string.IsNullOrWhiteSpace(spriteUrl)) Sprite = await ObterSpriteAsync(id, spriteUrl, token);
            AtualizarEstado();
        }
        catch (OperationCanceledException) { }
        catch
        {
            Sprite = CarregarSpriteSalvo(PokemonExibidoId) ?? Sprite;
            Erro = "Não foi possível atualizar o mascote. Usando a imagem salva quando disponível.";
        }
    }

    private void AtualizarEstado()
    {
        if (!_visual || _disposed) return;
        var ligado = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var indice = ligado.TotalHours >= 3 ? 2 : ligado.TotalHours >= 1 ? 1 : 0;
        var alvo = PokemonId;
        if (indice > 0)
        {
            if (PokemonId == 133) alvo = EvolucaoEevee();
            else if (_evolucoes.Length > 0) alvo = _evolucoes[Math.Min(indice - 1, _evolucoes.Length - 1)];
        }
        if (alvo != PokemonExibidoId)
        {
            PokemonExibidoId = alvo;
            _ = CarregarFormaAsync(alvo);
        }
        var inativo = TempoInativo();
        Estado = inativo > TimeSpan.FromMinutes(10) ? "Dormindo" : inativo > TimeSpan.FromMinutes(2) ? "Descansando" : "Acompanhando você";
    }

    private int EvolucaoEevee()
    {
        var clima = (_obterClima() ?? string.Empty).ToLowerInvariant();
        if (_evolucaoEeveeSessao.HasValue) return _evolucaoEeveeSessao.Value;
        if (clima.Contains("tempest") || clima.Contains("thunder") || clima.Contains("trovo") || clima.Contains("raio")) return (_evolucaoEeveeSessao = 135).Value;
        if (clima.Contains("chuva") || clima.Contains("rain")) return (_evolucaoEeveeSessao = 134).Value;
        if (clima.Contains("sol") || clima.Contains("sun") || clima.Contains("calor") || clima.Contains("clear")) return (_evolucaoEeveeSessao = 136).Value;
        return (_evolucaoEeveeSessao = 134).Value;
    }

    private async Task CarregarFormaAsync(int id)
    {
        try
        {
            using var json = await ObterJsonAsync($"https://pokeapi.co/api/v2/pokemon/{id}", _consulta?.Token ?? default);
            Nome = Capitalizar(json.RootElement.GetProperty("name").GetString() ?? $"Pokémon {id}");
            var url = json.RootElement.GetProperty("sprites").GetProperty("front_default").GetString();
            if (!string.IsNullOrWhiteSpace(url)) Sprite = await ObterSpriteAsync(id, url, _consulta?.Token ?? default);
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private async Task<JsonDocument> ObterJsonAsync(string url, CancellationToken token)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !uri.Host.Equals("pokeapi.co", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Endereço da PokéAPI inválido.");
        using var response = await _http.GetAsync(uri, token);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
    }

    private static IEnumerable<int> ExtrairEvolucoes(JsonElement node, int selecionado)
    {
        var id = IdDaUrl(node.GetProperty("species").GetProperty("url").GetString());
        if (id == selecionado) return Descendentes(node).Where(x => x <= 151);
        foreach (var child in node.GetProperty("evolves_to").EnumerateArray())
        {
            var achou = ExtrairEvolucoes(child, selecionado).ToArray();
            if (achou.Length > 0) return achou;
        }
        return Array.Empty<int>();
    }

    private static IEnumerable<int> Descendentes(JsonElement node)
    {
        var atual = node;
        while (atual.GetProperty("evolves_to").GetArrayLength() > 0)
        {
            atual = atual.GetProperty("evolves_to")[0];
            yield return IdDaUrl(atual.GetProperty("species").GetProperty("url").GetString());
        }
    }

    private static int IdDaUrl(string? url) => int.TryParse(url?.TrimEnd('/').Split('/').LastOrDefault(), out var id) ? id : 0;
    private static string Capitalizar(string texto) => string.IsNullOrEmpty(texto) ? texto : char.ToUpperInvariant(texto[0]) + texto[1..];

    private async Task<BitmapImage> ObterSpriteAsync(int id, string url, CancellationToken token)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Endereço do sprite inválido.");
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "cache", "pokemon");
        Directory.CreateDirectory(pasta);
        var arquivo = Path.Combine(pasta, $"{id}.png");
        if (!File.Exists(arquivo))
        {
            var temporario = arquivo + ".tmp";
            await File.WriteAllBytesAsync(temporario, await _http.GetByteArrayAsync(uri, token), token);
            File.Move(temporario, arquivo, true);
        }
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(arquivo); image.EndInit(); image.Freeze(); return image;
    }

    private static BitmapImage? CarregarSpriteSalvo(int id)
    {
        var arquivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "cache", "pokemon", $"{id}.png");
        if (!File.Exists(arquivo)) return null;
        try { var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(arquivo); image.EndInit(); image.Freeze(); return image; }
        catch { return null; }
    }

    [StructLayout(LayoutKind.Sequential)] private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    private static TimeSpan TempoInativo()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        return GetLastInputInfo(ref info) ? TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.dwTime)) : TimeSpan.Zero;
    }

    public void Dispose() { if (_disposed) return; _disposed = true; _timer.Stop(); _consulta?.Cancel(); _consulta?.Dispose(); _http.Dispose(); }
}
