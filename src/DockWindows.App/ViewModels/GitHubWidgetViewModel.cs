using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DockWindows.App.Common;

namespace DockWindows.App.ViewModels;

public enum EstiloAnimacaoGitHub
{
    Cobrinha,
    PacMan,
    Breakout,
    Galaga,
    PuzzleBobble,
    Bomberman,
    Minesweeper
}

public class GitHubWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => TimerAtivo || AnimacaoAtiva || _ocupado;

    private string? _nomeUsuario = string.Empty;
    private bool _carregando;
    private int _totalContribuicoes;
    private bool _painelAberto;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10), MaxResponseContentBufferSize = 2 * 1024 * 1024 };
    private string _erroAtualizacao = "";
    public string ErroAtualizacao { get => _erroAtualizacao; private set => SetProperty(ref _erroAtualizacao, value); }
    public DockWindows.Core.Widgets.SaudeWidget Saude => string.IsNullOrEmpty(ErroAtualizacao) ? DockWindows.Core.Widgets.SaudeWidget.Disponivel : DockWindows.Core.Widgets.SaudeWidget.Erro;
    public string? MotivoEstado => ErroAtualizacao;
    public bool TotalConfirmado { get; private set; }
    private string? _etagEventos;
    private int _eventosRecentes;
    private DateTimeOffset? _ultimaAtividade;
    private string _estadoAtividade = "Atividade recente não consultada";
    public int EventosRecentes { get => _eventosRecentes; private set => SetProperty(ref _eventosRecentes, value); }
    public DateTimeOffset? UltimaAtividade { get => _ultimaAtividade; private set => SetProperty(ref _ultimaAtividade, value); }
    public string EstadoAtividade { get => _estadoAtividade; private set => SetProperty(ref _estadoAtividade, value); }
    public string ResumoCompleto => $"{TextoResumo}\n{EstadoAtividade}";
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _animTimer;
    private bool _visual, _animacoes, _disposed, _ocupado;
    private DateTimeOffset _cacheAte;
    private System.Threading.CancellationTokenSource? _consulta, _reinicio;
    public int Requisicoes { get; private set; }
    public bool TimerAtivo => _timer.IsEnabled;
    public bool AnimacaoAtiva => _animTimer.IsEnabled;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        _visual = estado.Visual && !_disposed; _animacoes = estado.Animacoes && !_disposed;
        _timer.Stop();
        if (!_visual) _consulta?.Cancel();
        if (!_animacoes) { _reinicio?.Cancel(); LimparEstadoAnimacao(); }
        if (_visual) { _ = CarregarContribuicoesAsync(); AgendarConsulta(); if (AnimacaoAutomatica && _animacoes) IniciarAnimacao(); }
        else PainelAberto = false;
        if (!estado.Habilitado) { Contribuicoes.Clear(); _niveisOriginais.Clear(); _corpo.Clear(); _cacheAte = default; }
    }
    public void Dispose()
    { _disposed = true; _visual = _animacoes = false; _timer.Stop(); _animTimer.Stop(); _consulta?.Cancel(); _reinicio?.Cancel(); _http.Dispose(); Contribuicoes.Clear(); }

    
    // Animação State
    private bool _animacaoRodando;
    private int _quadroArcade;
    private readonly GitHubArcadeAnimation _arcade = new();
    private List<(int X, int Y)> _corpo = new();
    private readonly (int X, int Y)[] _fantasmas = new (int, int)[4];

    private bool _animacaoAutomatica = true;
    public bool AnimacaoAutomatica
    {
        get => _animacaoAutomatica;
        set
        {
            if (SetProperty(ref _animacaoAutomatica, value))
            {
                if (value && !_animacaoRodando) IniciarAnimacao();
                if (!value) { _reinicio?.Cancel(); LimparEstadoAnimacao(); }
                OnPropertyChanged(nameof(AnimacaoSelecionada));
            }
        }
    }

    private EstiloAnimacaoGitHub _estiloAtual = EstiloAnimacaoGitHub.Cobrinha;
    public EstiloAnimacaoGitHub EstiloAtual
    {
        get => _estiloAtual;
        set
        {
            if (SetProperty(ref _estiloAtual, value))
            {
                OnPropertyChanged(nameof(AnimacaoSelecionada));
                if (_animacaoRodando)
                {
                    LimparEstadoAnimacao();
                    IniciarAnimacao();
                }
            }
        }
    }

    private List<int> _niveisOriginais = new();
    private List<ContribuicaoDia> _diasDisponiveis = new();
    public int ColunasAnimacao => Estilo == "resumo-anual" ? 36 : 7;
    private int QuantidadeCelulas => ColunasAnimacao * 7;

    private void AplicarGradeDisponivel()
    {
        if (_diasDisponiveis.Count == 0) return;
        Contribuicoes.Clear();
        foreach (var dia in _diasDisponiveis.TakeLast(QuantidadeCelulas))
            Contribuicoes.Add(new ContribuicaoDia { Data = dia.Data, Nivel = dia.Nivel });
        _niveisOriginais = Contribuicoes.Select(d => d.Nivel).ToList();
    }

        private bool _habilitado;
    public bool Habilitado
    {
        get => _habilitado;
        set => SetProperty(ref _habilitado, value);
    }

    private DockWindows.Core.Models.FormatoWidget _formato;
    public DockWindows.Core.Models.FormatoWidget Formato
    {
        get => _formato;
        set => SetProperty(ref _formato, value);
    }

    private string _estilo = "grade-compacta";
    public string Estilo
    {
        get => _estilo;
        set
        {
            if (!SetProperty(ref _estilo, value)) return;
            _reinicio?.Cancel();
            LimparEstadoAnimacao();
            AplicarGradeDisponivel();
            IniciarAnimacao();
        }
    }

    public string? NomeUsuario { get => _nomeUsuario; set => SetProperty(ref _nomeUsuario, value); }
    public bool Carregando { get => _carregando; set => SetProperty(ref _carregando, value); }
    public int TotalContribuicoes { get => _totalContribuicoes; set => SetProperty(ref _totalContribuicoes, value); }
    public bool PainelAberto { get => _painelAberto; set => SetProperty(ref _painelAberto, value); }
    public ObservableCollection<ContribuicaoDia> Contribuicoes { get; } = new();

    public string TextoResumo => TotalConfirmado ? $"{TotalContribuicoes} contribuições" : $"{Contribuicoes.Count(d => d.Nivel > 0)} dias com atividade";
    private void AgendarConsulta()
    {
        _timer.Stop(); if (!_visual || _disposed || string.IsNullOrWhiteSpace(_nomeUsuario)) return;
        var espera = _cacheAte - DateTimeOffset.UtcNow;
        _timer.Interval = espera > TimeSpan.Zero ? espera : TimeSpan.FromSeconds(30); _timer.Start();
    }

    public ICommand AlternarPainelCommand { get; }
    public ICommand AbrirPerfilCommand { get; }
    public ICommand AlternarAnimacaoCommand { get; }
    public ICommand DefinirAnimacaoCommand { get; }
    public string AnimacaoSelecionada => AnimacaoAutomatica ? EstiloAtual.ToString() : "Parado";

    public void SelecionarAnimacao(string? estilo)
    {
        if (estilo != "Parado" && (!Enum.TryParse<EstiloAnimacaoGitHub>(estilo, out var parsed) || !Enum.IsDefined(parsed))) return;
        _reinicio?.Cancel();
        LimparEstadoAnimacao();
        if (estilo == "Parado") AnimacaoAutomatica = false;
        else
        {
            EstiloAtual = Enum.Parse<EstiloAnimacaoGitHub>(estilo!);
            AnimacaoAutomatica = true;
            IniciarAnimacao();
        }
        OnPropertyChanged(nameof(AnimacaoSelecionada));
    }

    public GitHubWidgetViewModel()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GigaDock/3.0 (+https://github.com/Contagiovaneines/GigaDock)");
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        DefinirAnimacaoCommand = new RelayCommand<string>(SelecionarAnimacao);
        
        AlternarAnimacaoCommand = new RelayCommand(() => {
            if (AnimacaoAutomatica && EstiloAtual == EstiloAnimacaoGitHub.Cobrinha)
            {
                EstiloAtual = EstiloAnimacaoGitHub.PacMan;
            }
            else if (AnimacaoAutomatica && EstiloAtual == EstiloAnimacaoGitHub.PacMan)
            {
                AnimacaoAutomatica = false;
                LimparEstadoAnimacao();
            }
            else
            {
                AnimacaoAutomatica = true;
                EstiloAtual = EstiloAnimacaoGitHub.Cobrinha;
            }
        });

        AbrirPerfilCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(NomeUsuario))
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"https://github.com/{NomeUsuario}") { UseShellExecute = true }); }
                catch { }
            }
        });

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _timer.Tick += (_, _) => { _timer.Stop(); _ = CarregarContribuicoesAsync(); };

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _animTimer.Tick += (_, _) => TickAnimacao();
    }

    public void SincronizarUsuario(string? usuario)
    {
        if (usuario != _nomeUsuario || Contribuicoes.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(usuario))
            {
                _reinicio?.Cancel(); LimparEstadoAnimacao();
                _diasDisponiveis.Clear(); Contribuicoes.Clear(); _niveisOriginais.Clear();
                TotalConfirmado = false; TotalContribuicoes = 0;
                OnPropertyChanged(nameof(TotalConfirmado));
                _nomeUsuario = usuario;
                OnPropertyChanged(nameof(NomeUsuario));
                _cacheAte = default; _consulta?.Cancel();
                if (_visual) _ = CarregarContribuicoesAsync();
            }
            else
            {
                _reinicio?.Cancel(); LimparEstadoAnimacao();
                _diasDisponiveis.Clear(); _niveisOriginais.Clear();
                TotalConfirmado = false;
                OnPropertyChanged(nameof(TotalConfirmado));
                _nomeUsuario = string.Empty;
                Contribuicoes.Clear();
                TotalContribuicoes = 0;
            }
        }
        if (_visual) AgendarConsulta();
    }


    private async Task CarregarContribuicoesAsync()
    {
        if (string.IsNullOrWhiteSpace(_nomeUsuario) || !_visual || _disposed || _ocupado || _cacheAte > DateTimeOffset.UtcNow) return;
        _ocupado = true;
        var usuario = _nomeUsuario;
        using var consulta = new System.Threading.CancellationTokenSource(); _consulta = consulta;
        Carregando = true;
        try
        {
            await CarregarAtividadeRecenteAsync(usuario, consulta.Token);
            var url = $"https://github.com/users/{Uri.EscapeDataString(usuario)}/contributions";
            Requisicoes++;
            var html = await _http.GetStringAsync(url, consulta.Token);
            var dias = new List<ContribuicaoDia>();
            int total = 0;

            var matches = Regex.Matches(html, "data-date=\"([^\"]+)\"[^>]*data-level=\"(\\d+)\"");
            if (matches.Count == 0)
            {
                var matches2 = Regex.Matches(html, "data-level=\"(\\d+)\"[^>]*data-date=\"([^\"]+)\"");
                if (matches2.Count > 0)
                {
                    foreach (Match m in matches2)
                    {
                        dias.Add(new ContribuicaoDia { Data = DateTime.Parse(m.Groups[2].Value), Nivel = int.Parse(m.Groups[1].Value) });
                    }
                }
                else
                {
                    matches = Regex.Matches(html, "data-date=\"([^\"]+)\"[^>]*>\\s*(\\d+)\\s+contribution");
                }
            }

            foreach (Match m in matches)
            {
                dias.Add(new ContribuicaoDia { Data = DateTime.Parse(m.Groups[1].Value), Nivel = int.Parse(m.Groups[2].Value) });
            }

            var matchesTooltip = Regex.Matches(html, "(\\d+)\\s+contributions?\\s+on");
            if (matchesTooltip.Count > 0)
            {
                foreach (Match mt in matchesTooltip) total += int.Parse(mt.Groups[1].Value);
            }
            if (dias.Count == 0) throw new FormatException("Não foi possível interpretar as contribuições públicas.");

            void Aplicar()
            {
                if (!_visual || _disposed || consulta.IsCancellationRequested || usuario != _nomeUsuario) return;
                _cacheAte = DateTimeOffset.UtcNow.AddMinutes(30);
                LimparEstadoAnimacao();
                Contribuicoes.Clear();
                _diasDisponiveis = dias.OrderBy(d => d.Data).ToList();
                AplicarGradeDisponivel();
                
                if (AnimacaoAutomatica) IniciarAnimacao();

                ErroAtualizacao = ""; TotalConfirmado = matchesTooltip.Count > 0;
                OnPropertyChanged(nameof(TotalConfirmado));
                TotalContribuicoes = total; OnPropertyChanged(nameof(TextoResumo)); OnPropertyChanged(nameof(ResumoCompleto));
                Carregando = false;
            }
            if (Application.Current?.Dispatcher is { } dispatcher) await dispatcher.InvokeAsync(Aplicar); else Aplicar();
        }
        catch (OperationCanceledException) { }
        catch { if (!_disposed && _visual && usuario == _nomeUsuario) { ErroAtualizacao = "Não foi possível atualizar as contribuições. Dados anteriores preservados."; _cacheAte = DateTimeOffset.UtcNow.AddSeconds(30); } }
        finally
        {
            _ocupado = false; Carregando = false; AgendarConsulta();
            if (ReferenceEquals(_consulta, consulta)) _consulta = null;
            if (_visual && !_disposed && (consulta.IsCancellationRequested || usuario != _nomeUsuario)) _ = CarregarContribuicoesAsync();
        }
    }

    private async Task CarregarAtividadeRecenteAsync(string usuario, CancellationToken token)
    {
        var cache = ObterCaminhoCacheEventos(usuario);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/users/{Uri.EscapeDataString(usuario)}/events/public?per_page=100");
            if (!string.IsNullOrWhiteSpace(_etagEventos)) request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(_etagEventos));
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode == HttpStatusCode.NotModified) { EstadoAtividade = $"{EventosRecentes} eventos públicos recentes"; return; }
            if (response.StatusCode == HttpStatusCode.NotFound) throw new InvalidOperationException("Usuário do GitHub não encontrado.");
            response.EnsureSuccessStatusCode();
            _etagEventos = response.Headers.ETag?.Tag;
            if (response.Headers.TryGetValues("X-Poll-Interval", out var valores) && int.TryParse(valores.FirstOrDefault(), out var segundos))
                _cacheAte = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, segundos));
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: token);
            var datas = json.RootElement.EnumerateArray().Select(e => e.TryGetProperty("created_at", out var data) && data.TryGetDateTimeOffset(out var valor) ? valor : (DateTimeOffset?)null).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            EventosRecentes = datas.Count; UltimaAtividade = datas.Count == 0 ? null : datas.Max();
            EstadoAtividade = datas.Count == 0 ? "Sem eventos públicos recentes" : $"{datas.Count} eventos · último {UltimaAtividade:dd/MM HH:mm}";
            OnPropertyChanged(nameof(ResumoCompleto));
            Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
            await File.WriteAllTextAsync(cache, JsonSerializer.Serialize(new CacheEventos(EventosRecentes, UltimaAtividade, _etagEventos, DateTimeOffset.Now)), token);
        }
        catch (OperationCanceledException) { throw; }
        catch (InvalidOperationException ex) when (ex.Message.Contains("não encontrado", StringComparison.Ordinal)) { EstadoAtividade = ex.Message; throw; }
        catch
        {
            try
            {
                if (!File.Exists(cache)) { EstadoAtividade = "Atividade recente indisponível"; return; }
                var salvo = JsonSerializer.Deserialize<CacheEventos>(await File.ReadAllTextAsync(cache, token));
                if (salvo != null) { EventosRecentes = salvo.Eventos; UltimaAtividade = salvo.Ultima; _etagEventos = salvo.ETag; EstadoAtividade = $"{salvo.Eventos} eventos · cache de {salvo.SalvoEm:dd/MM HH:mm}"; OnPropertyChanged(nameof(ResumoCompleto)); }
            }
            catch { EstadoAtividade = "Atividade recente indisponível"; }
        }
    }

    private static string ObterCaminhoCacheEventos(string usuario)
    {
        var seguro = new string(usuario.Where(char.IsLetterOrDigit).ToArray());
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows", "cache", $"github-{seguro}-events.json");
    }

    private sealed record CacheEventos(int Eventos, DateTimeOffset? Ultima, string? ETag, DateTimeOffset SalvoEm);

    public void IniciarAnimacao()
    {
        if (!AnimacaoAutomatica || _animacaoRodando || Contribuicoes.Count < QuantidadeCelulas || !_visual || !_animacoes || _disposed) return;
        _corpo.Clear();
        _quadroArcade = 0;
        _arcade.Reset(ColunasAnimacao);
        _corpo.Add((0, 0));
        for (var i = 0; i < _fantasmas.Length; i++)
            _fantasmas[i] = (ColunasAnimacao - 1 - i, 6);
        _animacaoRodando = true;
        _animTimer.Start();
    }

    private void LimparEstadoAnimacao()
    {
        _animTimer.Stop();
        _animacaoRodando = false;
        foreach(var c in Contribuicoes) 
        { 
            c.EhCobra = false; c.EhCabecaCobra = false; 
            c.EhPacMan = false; c.EhFantasma = false;
            c.MarcaArcade = 0;
        }
        for(int i = 0; i < Contribuicoes.Count && i < _niveisOriginais.Count; i++)
            Contribuicoes[i].Nivel = _niveisOriginais[i];
    }

    private void FinalizarCiclo()
    {
        LimparEstadoAnimacao();

        if (AnimacaoAutomatica)
        {
            _reinicio?.Cancel(); _reinicio?.Dispose(); _reinicio = new();
            _ = ReiniciarDepoisAsync(_reinicio.Token);
        }
    }

    private async Task ReiniciarDepoisAsync(System.Threading.CancellationToken token)
    {
        try { await Task.Delay(3000, token); if (!token.IsCancellationRequested && _visual && _animacoes && AnimacaoAutomatica && !_disposed) IniciarAnimacao(); }
        catch (OperationCanceledException) { }
    }

    private void TickAnimacao()
    {
        if (!_animacaoRodando || Contribuicoes.Count < QuantidadeCelulas)
        {
            _animTimer.Stop();
            return;
        }

        if (!AnimacaoAutomatica || !_visual || !_animacoes || _disposed)
        {
            LimparEstadoAnimacao();
            return;
        }
        if (EstiloAtual >= EstiloAnimacaoGitHub.Breakout)
        {
            _arcade.Tick(EstiloAtual, _quadroArcade++, Contribuicoes, ColunasAnimacao);
            if (_quadroArcade >= 140) FinalizarCiclo();
            return;
        }
        if (!Contribuicoes.Take(QuantidadeCelulas).Any(c => c.Nivel > 0)) { FinalizarCiclo(); return; }

        var head = _corpo.First();
        var nextStep = EncontrarProximoPasso(head);

        if (nextStep == null)
        {
            FinalizarCiclo();
            return;
        }

        var n = nextStep.Value;
        _corpo.Insert(0, n);

        int idx = n.Y * ColunasAnimacao + n.X;
        var cell = Contribuicoes[idx];

        if (cell.Nivel > 0)
        {
            cell.Nivel = 0; 
            if (EstiloAtual == EstiloAnimacaoGitHub.PacMan)
            {
                var tail = _corpo.Last();
                _corpo.RemoveAt(_corpo.Count - 1);
                var tailCell = Contribuicoes[tail.Y * ColunasAnimacao + tail.X];
                tailCell.EhPacMan = false;
            }
        }
        else
        {
            var tail = _corpo.Last();
            _corpo.RemoveAt(_corpo.Count - 1);
            var tailCell = Contribuicoes[tail.Y * ColunasAnimacao + tail.X];
            tailCell.EhCobra = false;
            tailCell.EhCabecaCobra = false;
            tailCell.EhPacMan = false;
        }

        if (EstiloAtual == EstiloAnimacaoGitHub.PacMan)
        {
            cell.DirecaoPacMan = n.X > head.X ? 0 : n.Y > head.Y ? 1 : n.X < head.X ? 2 : 3;
            cell.BocaPacManAberta = (_quadroArcade++ % 2) == 0;
            foreach (var old in _fantasmas)
                Contribuicoes[old.Y * ColunasAnimacao + old.X].EhFantasma = false;
            for (var ghost = 0; ghost < _fantasmas.Length; ghost++)
            {
                var position = _fantasmas[ghost];
                var directions = new (int X, int Y)[] { (0, -1), (0, 1), (-1, 0), (1, 0) };
                var candidates = directions.Select(d => (X: position.X + d.X, Y: position.Y + d.Y))
                    .Where(v => v.X >= 0 && v.X < ColunasAnimacao && v.Y >= 0 && v.Y < 7
                        && v != n && !_fantasmas.Where((_, index) => index != ghost).Contains(v)).ToArray();
                if (candidates.Length > 0) _fantasmas[ghost] = candidates[Random.Shared.Next(candidates.Length)];
            }
        }

        for (int i = 0; i < _corpo.Count; i++)
        {
            var pt = _corpo[i];
            var c = Contribuicoes[pt.Y * ColunasAnimacao + pt.X];
            if (EstiloAtual == EstiloAnimacaoGitHub.Cobrinha)
            {
                c.EhCabecaCobra = (i == 0);
                c.EhCobra = (i != 0);
            }
            else
            {
                c.EhPacMan = (i == 0);
            }
        }

        if (EstiloAtual == EstiloAnimacaoGitHub.PacMan)
        {
            for (var ghost = 0; ghost < _fantasmas.Length; ghost++)
            {
                var position = _fantasmas[ghost];
                var fCell = Contribuicoes[position.Y * ColunasAnimacao + position.X];
                fCell.CorFantasma = ghost;
                fCell.EhFantasma = true;
            }
        }
    }

    private (int X, int Y)? EncontrarProximoPasso((int X, int Y) start)
    {
        var dirs = new (int X, int Y)[] { (0, -1), (0, 1), (-1, 0), (1, 0) };
        var fila = new Queue<List<(int X, int Y)>>();
        fila.Enqueue(new List<(int X, int Y)> { start });
        var visitados = new HashSet<(int X, int Y)> { start };

        while (fila.Count > 0)
        {
            var caminho = fila.Dequeue();
            var atual = caminho.Last();

            if (atual != start && Contribuicoes[atual.Y * ColunasAnimacao + atual.X].Nivel > 0)
                return caminho[1];

            foreach (var dir in dirs)
            {
                var nx = atual.X + dir.X;
                var ny = atual.Y + dir.Y;
                var vizinho = (X: nx, Y: ny);

                bool colisaoCorpo = EstiloAtual == EstiloAnimacaoGitHub.Cobrinha && _corpo.Contains(vizinho);

                if (nx >= 0 && nx < ColunasAnimacao && ny >= 0 && ny < 7 && !visitados.Contains(vizinho) && !colisaoCorpo)
                {
                    visitados.Add(vizinho);
                    var novoCaminho = new List<(int X, int Y)>(caminho) { vizinho };
                    fila.Enqueue(novoCaminho);
                }
            }
        }

        foreach (var dir in dirs)
        {
            var nx = start.X + dir.X;
            var ny = start.Y + dir.Y;
            var vizinho = (X: nx, Y: ny);
            bool colisaoCorpo = EstiloAtual == EstiloAnimacaoGitHub.Cobrinha && _corpo.Contains(vizinho);
            if (nx >= 0 && nx < ColunasAnimacao && ny >= 0 && ny < 7 && !colisaoCorpo)
                return vizinho;
        }
        return null;
    }
}

public class ContribuicaoDia : ObservableObject
{
    private int _marcaArcade;
    public int MarcaArcade { get => _marcaArcade; set { if (SetProperty(ref _marcaArcade, value)) { OnPropertyChanged(nameof(Cor)); OnPropertyChanged(nameof(Simbolo)); } } }
    public string Simbolo => MarcaArcade switch { 5 => "✹", 6 => "⚑", >= 10 and <= 18 => (MarcaArcade - 10).ToString(), _ => "" };
    private static readonly Brush[] Paleta = new[] { "#161B22", "#0E4429", "#006D32", "#26A641", "#39D353", "#FFFF00", "#FF4040", "#9B59B6", "#8E44AD", "#EAF6FF", "#38BDF8", "#C084FC", "#FB923C", "#FDE047", "#F87171", "#5A718B" }.Select(c => { var b = (SolidColorBrush)new BrushConverter().ConvertFromString(c)!; b.Freeze(); return (Brush)b; }).ToArray();
    private int _nivel;
    private bool _ehCobra;
    private bool _ehCabecaCobra;
    private bool _ehPacMan;
    private bool _ehFantasma;
    private int _direcaoPacMan, _corFantasma;
    private bool _bocaPacManAberta;
    public int DirecaoPacMan { get => _direcaoPacMan; set => SetProperty(ref _direcaoPacMan, value); }
    public int CorFantasma { get => _corFantasma; set => SetProperty(ref _corFantasma, value); }
    public bool BocaPacManAberta { get => _bocaPacManAberta; set => SetProperty(ref _bocaPacManAberta, value); }

    public DateTime Data { get; set; }

    public int Nivel { get => _nivel; set { if (SetProperty(ref _nivel, value)) OnPropertyChanged(nameof(Cor)); } }
    public bool EhCobra { get => _ehCobra; set { if (SetProperty(ref _ehCobra, value)) OnPropertyChanged(nameof(Cor)); } }
    public bool EhCabecaCobra { get => _ehCabecaCobra; set { if (SetProperty(ref _ehCabecaCobra, value)) OnPropertyChanged(nameof(Cor)); } }
    public bool EhPacMan { get => _ehPacMan; set { if (SetProperty(ref _ehPacMan, value)) OnPropertyChanged(nameof(Cor)); } }
    public bool EhFantasma { get => _ehFantasma; set { if (SetProperty(ref _ehFantasma, value)) OnPropertyChanged(nameof(Cor)); } }

    public Brush Cor 
    {
        get
        {
            if (MarcaArcade > 0) return Paleta[MarcaArcade >= 10 ? 15 : 8 + MarcaArcade];
            if (EhPacMan) return Paleta[5];
            if (EhFantasma) return Paleta[6];
            if (EhCabecaCobra) return Paleta[7];
            if (EhCobra) return Paleta[8];
            
            return Nivel switch
            {
                >= 0 and <= 4 => Paleta[Nivel],
                _ => Paleta[0]
            };
        }
    }
}




