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
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Func<string> _obterClima;
    private readonly PokemonEvolutionClock _evolutionClock = new();
    private readonly DittoTransformation _dittoTransformation = new();
    private CancellationTokenSource? _consulta;
    private CancellationTokenSource? _eeveeEvolution;
    private bool _evoluindo;
    public bool Evoluindo { get => _evoluindo; private set => SetProperty(ref _evoluindo, value); }
    public BitmapSource? EvolutionPreview { get; private set; }
    private bool _habilitado, _visual, _disposed;
    private int _pokemonId = 1, _pokemonExibidoId = 1;
    private int? _evolucaoEeveeSessao;
    private int[] _evolucoes = Array.Empty<int>();
    private string _nome = "Bulbasaur", _estado = "Descansando", _erro = string.Empty;
    private BitmapSource? _sprite;
    public IReadOnlyList<PokemonFrame> Frames { get; private set; } = Array.Empty<PokemonFrame>();
    private PokemonWalkAnimation? _walkAnimation;
    public PokemonWalkAnimation? RestAnimation { get; private set; }
    public PokemonWalkAnimation? SleepAnimation { get; private set; }
    public PokemonWalkAnimation? WalkAnimation
    {
        get => _walkAnimation;
        private set
        {
            RestAnimation = PokemonWalkAnimation.Load(PokemonExibidoId, "Rest");
            SleepAnimation = PokemonWalkAnimation.Load(PokemonExibidoId, "Sleep");
            SetProperty(ref _walkAnimation, value);
        }
    }

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
    public string Erro { get => _erro; private set { if (SetProperty(ref _erro, value)) OnPropertyChanged(nameof(Descricao)); } }
    public BitmapSource? Sprite { get => _sprite; private set => SetProperty(ref _sprite, value); }
    public bool? EmExecucao => _timer.IsEnabled;
    public SaudeWidget Saude => string.IsNullOrEmpty(Erro) ? SaudeWidget.Disponivel : SaudeWidget.Indisponivel;
    public string? MotivoEstado => string.IsNullOrEmpty(Erro) ? $"{Nome} · {Estado}" : Erro;
    public string Descricao => !string.IsNullOrEmpty(Erro) ? Erro : $"{Nome} · {Estado}. Evolução temporária desta sessão; reiniciar o Windows retorna ao Pokémon escolhido.";

    public async Task SelecionarAsync(int id)
    {
        if (_disposed || id is < 1 or > 151) return;
        if (!_evolutionClock.Select(id)) return;
        _dittoTransformation.Reset();
        _eeveeEvolution?.Cancel();
        Evoluindo = false;
        PokemonId = id;
        PokemonExibidoId = id;
        WalkAnimation = PokemonWalkAnimation.Load(id);
        Estado = PokemonBehavior.StateFor(id, _evolutionClock.Elapsed, TempoInativo());
        _evolucaoEeveeSessao = null;
        _evolucoes = Array.Empty<int>();
        IniciarEvolucaoEevee();
        if (_visual) await CarregarAsync(id);
    }

    public void DefinirAtividade(EstadoAtividade estado)
    {
        _visual = estado.Habilitado && estado.Visual;
        Habilitado = estado.Habilitado;
        _timer.Stop();
        _consulta?.Cancel();
        if (!_visual || _disposed)
        {
            _eeveeEvolution?.Cancel();
            Evoluindo = false;
            return;
        }
        _timer.Start();
        IniciarEvolucaoEevee();
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
            var spriteUrl = ObterUrlSprite(pokemon.RootElement);
            using var species = await ObterJsonAsync($"https://pokeapi.co/api/v2/pokemon-species/{id}", token);
            var chainUrl = species.RootElement.GetProperty("evolution_chain").GetProperty("url").GetString();
            if (!string.IsNullOrWhiteSpace(chainUrl))
            {
                using var chain = await ObterJsonAsync(chainUrl, token);
                _evolucoes = ExtrairEvolucoes(chain.RootElement.GetProperty("chain"), id).Take(2).ToArray();
            }
            if (!string.IsNullOrWhiteSpace(spriteUrl)) Sprite = await ObterSpriteAsync(id, spriteUrl, token);
            AtualizarEstado();
            if (PokemonExibidoId != id) _ = CarregarFormaAsync(PokemonExibidoId);
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
        var indice = _evolutionClock.Stage;
        var alvo = PokemonId == 132 ? _dittoTransformation.TargetFor(PokemonId, PokemonExibidoId, _evolutionClock.Elapsed)
            : PokemonId == 133 ? _evolucaoEeveeSessao ?? 133 : PokemonId;
        if (indice > 0 && PokemonId is not (132 or 133))
        {
            if (_evolucoes.Length > 0) alvo = _evolucoes[Math.Min(indice - 1, _evolucoes.Length - 1)];
        }
        if (alvo != PokemonExibidoId)
        {
            if (_eeveeEvolution is not { IsCancellationRequested: false })
            {
                _eeveeEvolution = new CancellationTokenSource();
                _ = EvoluirEeveeAsync(_eeveeEvolution, alvo);
            }
        }
        var inativo = TempoInativo();
        Estado = PokemonBehavior.StateFor(PokemonId, _evolutionClock.Elapsed, inativo);
    }

    private void IniciarEvolucaoEevee()
    {
        if (!_visual || _disposed || PokemonId != 133 || _evolucaoEeveeSessao.HasValue
            || _eeveeEvolution is { IsCancellationRequested: false }) return;
        _eeveeEvolution?.Dispose();
        _eeveeEvolution = new CancellationTokenSource();
        _ = EvoluirEeveeAsync(_eeveeEvolution);
    }

    private async Task EvoluirEeveeAsync(CancellationTokenSource operation, int? timedTarget = null)
    {
        try
        {
            if (!timedTarget.HasValue) await Task.Delay(TimeSpan.FromSeconds(2), operation.Token);
            var target = timedTarget ?? EeveeEvolution.ForWeather(_obterClima());
            var targetAnimation = PokemonWalkAnimation.Load(target);
            EvolutionPreview = targetAnimation?.Front[0].Image;
            Evoluindo = true;
            await Task.Delay(TimeSpan.FromMilliseconds(Controls.PokemonEvolutionEffect.DurationMilliseconds), operation.Token);
            if (PokemonId == 133) _evolucaoEeveeSessao = target;
            PokemonExibidoId = target;
            Nome = PokemonOpcao.CriarOriginais().FirstOrDefault(p => p.Id == target)?.Nome ?? Nome;
            WalkAnimation = targetAnimation;
            Evoluindo = false;
            _ = CarregarFormaAsync(target);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (ReferenceEquals(operation, _eeveeEvolution))
            {
                Evoluindo = false;
                _eeveeEvolution = null;
            }
            operation.Dispose();
        }
    }

    private async Task CarregarFormaAsync(int id)
    {
        try
        {
            using var json = await ObterJsonAsync($"https://pokeapi.co/api/v2/pokemon/{id}", _consulta?.Token ?? default);
            Nome = Capitalizar(json.RootElement.GetProperty("name").GetString() ?? $"Pokémon {id}");
            var url = ObterUrlSprite(json.RootElement);
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

    private static string? ObterUrlSprite(JsonElement pokemon)
    {
        var sprites = pokemon.GetProperty("sprites");
        if (sprites.TryGetProperty("versions", out var versions) &&
            versions.TryGetProperty("generation-v", out var generation) &&
            generation.TryGetProperty("black-white", out var bw) &&
            bw.TryGetProperty("animated", out var animated) &&
            animated.TryGetProperty("front_default", out var front) && front.ValueKind == JsonValueKind.String)
            return front.GetString();
        return sprites.GetProperty("front_default").GetString();
    }

    private async Task<BitmapSource> ObterSpriteAsync(int id, string url, CancellationToken token)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath.StartsWith("/PokeAPI/sprites/master/sprites/pokemon/", StringComparison.Ordinal)))
            throw new InvalidOperationException("Endereço do sprite inválido.");
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "cache", "pokemon");
        Directory.CreateDirectory(pasta);
        var arquivo = Path.Combine(pasta, $"{id}{(uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? ".gif" : ".png")}");
        if (!File.Exists(arquivo))
        {
            var temporario = arquivo + $".{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllBytesAsync(temporario, await _http.GetByteArrayAsync(uri, token), token);
                token.ThrowIfCancellationRequested();
                File.Move(temporario, arquivo, true);
            }
            finally { if (File.Exists(temporario)) File.Delete(temporario); }
        }
        return CarregarFrames(arquivo);
    }

    private BitmapSource CarregarFrames(string arquivo)
    {
        Frames = PokemonFrame.Carregar(arquivo);
        OnPropertyChanged(nameof(Frames));
        return Frames[0].Image;
    }

    private BitmapSource? CarregarSpriteSalvo(int id)
    {
        foreach (var ext in new[] { ".gif", ".png" })
        {
            var arquivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "cache", "pokemon", $"{id}{ext}");
            if (!File.Exists(arquivo)) continue;
            try { return CarregarFrames(arquivo); } catch { }
        }
        return null;
    }

    [StructLayout(LayoutKind.Sequential)] private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
    private static TimeSpan TempoInativo()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        return GetLastInputInfo(ref info) ? TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.dwTime)) : TimeSpan.Zero;
    }

    public void Dispose() { if (_disposed) return; _disposed = true; _timer.Stop(); _eeveeEvolution?.Cancel(); _eeveeEvolution?.Dispose(); _consulta?.Cancel(); _consulta?.Dispose(); _http.Dispose(); }
}
