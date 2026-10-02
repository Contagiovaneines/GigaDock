using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
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
    PacMan
}

public class GitHubWidgetViewModel : ObservableObject
{
    private string? _nomeUsuario = "Contagiovaneines";
    private bool _carregando;
    private int _totalContribuicoes;
    private bool _painelAberto;
    private readonly HttpClient _http = new();
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _animTimer;
    
    // Animação State
    private bool _animacaoRodando;
    private List<(int X, int Y)> _corpo = new();
    private (int X, int Y) _fantasma = (12, 6);

    private bool _animacaoAutomatica = true;
    public bool AnimacaoAutomatica
    {
        get => _animacaoAutomatica;
        set
        {
            if (SetProperty(ref _animacaoAutomatica, value))
            {
                if (value && !_animacaoRodando) IniciarAnimacao();
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
                if (_animacaoRodando)
                {
                    LimparEstadoAnimacao();
                    IniciarAnimacao();
                }
            }
        }
    }

    private List<int> _niveisOriginais = new();

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

    public string? NomeUsuario { get => _nomeUsuario; set => SetProperty(ref _nomeUsuario, value); }
    public bool Carregando { get => _carregando; set => SetProperty(ref _carregando, value); }
    public int TotalContribuicoes { get => _totalContribuicoes; set => SetProperty(ref _totalContribuicoes, value); }
    public bool PainelAberto { get => _painelAberto; set => SetProperty(ref _painelAberto, value); }
    public ObservableCollection<ContribuicaoDia> Contribuicoes { get; } = new();

    public string TextoResumo => "$TotalContribuicoes contribuições";

    public ICommand AlternarPainelCommand { get; }
    public ICommand AbrirPerfilCommand { get; }
    public ICommand AlternarAnimacaoCommand { get; }

    public GitHubWidgetViewModel()
    {
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        
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
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/$NomeUsuario") { UseShellExecute = true }); }
                catch { }
            }
        });

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _timer.Tick += (_, _) => _ = CarregarContribuicoesAsync();

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _animTimer.Tick += (_, _) => TickAnimacao();
    }

    public void SincronizarUsuario(string? usuario)
    {
        if (!string.IsNullOrWhiteSpace(usuario) && usuario != _nomeUsuario)
        {
            _nomeUsuario = usuario;
            OnPropertyChanged(nameof(NomeUsuario));
            _ = CarregarContribuicoesAsync();
        }

        if (!_timer.IsEnabled) _timer.Start();
    }

    private async Task CarregarContribuicoesAsync()
    {
        if (string.IsNullOrWhiteSpace(_nomeUsuario)) return;
        Carregando = true;
        try
        {
            var url = "https://github.com/users/$_nomeUsuario/contributions";
            var html = await _http.GetStringAsync(url);
            var dias = new List<ContribuicaoDia>();
            int total = 0;

            var matches = Regex.Matches(html, @"data-date=""(\d{4}-\d{2}-\d{2})""\s+[^>]*data-level=""(\d+)""");
            if (matches.Count == 0) matches = Regex.Matches(html, @"data-date=""(\d{4}-\d{2}-\d{2})""[^>]*>\s*(\d+)\s+contribution");

            foreach (Match m in matches)
            {
                dias.Add(new ContribuicaoDia { Data = DateTime.Parse(m.Groups[1].Value), Nivel = int.Parse(m.Groups[2].Value) });
            }

            var matchesTooltip = Regex.Matches(html, @"(\d+)\s+contributions?\s+on");
            if (matchesTooltip.Count > 0)
            {
                foreach (Match mt in matchesTooltip) total += int.Parse(mt.Groups[1].Value);
            }
            else total = dias.Count(d => d.Nivel > 0);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Contribuicoes.Clear();
                var ultimos = dias.OrderByDescending(d => d.Data).Take(91).Reverse().ToList();
                foreach (var d in ultimos) Contribuicoes.Add(d);
                _niveisOriginais = ultimos.Select(d => d.Nivel).ToList();
                
                if (AnimacaoAutomatica) IniciarAnimacao();

                TotalContribuicoes = total;
                Carregando = false;
            });
        }
        catch { Application.Current?.Dispatcher.Invoke(() => { Carregando = false; }); }
    }

    public void IniciarAnimacao()
    {
        if (_animacaoRodando || Contribuicoes.Count < 91) return;
        _corpo.Clear();
        _corpo.Add((0, 0));
        _fantasma = (12, 6);
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
        }
        for(int i = 0; i < Contribuicoes.Count && i < _niveisOriginais.Count; i++)
            Contribuicoes[i].Nivel = _niveisOriginais[i];
    }

    private void FinalizarCiclo()
    {
        LimparEstadoAnimacao();

        if (AnimacaoAutomatica)
        {
            Task.Run(async () => {
                await Task.Delay(3000);
                Application.Current?.Dispatcher.Invoke(() => {
                    if (AnimacaoAutomatica) IniciarAnimacao();
                });
            });
        }
    }

    private void TickAnimacao()
    {
        if (!_animacaoRodando || Contribuicoes.Count < 91)
        {
            _animTimer.Stop();
            return;
        }

        var head = _corpo.First();
        var nextStep = EncontrarProximoPasso(head);

        if (nextStep == null)
        {
            FinalizarCiclo();
            return;
        }

        var n = nextStep.Value;
        _corpo.Insert(0, n);

        int idx = n.Y * 13 + n.X;
        var cell = Contribuicoes[idx];

        if (cell.Nivel > 0)
        {
            cell.Nivel = 0; 
            if (EstiloAtual == EstiloAnimacaoGitHub.PacMan)
            {
                var tail = _corpo.Last();
                _corpo.RemoveAt(_corpo.Count - 1);
                var tailCell = Contribuicoes[tail.Y * 13 + tail.X];
                tailCell.EhPacMan = false;
            }
        }
        else
        {
            var tail = _corpo.Last();
            _corpo.RemoveAt(_corpo.Count - 1);
            var tailCell = Contribuicoes[tail.Y * 13 + tail.X];
            tailCell.EhCobra = false;
            tailCell.EhCabecaCobra = false;
            tailCell.EhPacMan = false;
        }

        if (EstiloAtual == EstiloAnimacaoGitHub.PacMan)
        {
            var oldFantasma = Contribuicoes[_fantasma.Y * 13 + _fantasma.X];
            oldFantasma.EhFantasma = false;

            var fDirs = new (int X, int Y)[] { (0, -1), (0, 1), (-1, 0), (1, 0) };
            var validFDirs = fDirs.Select(d => (X: _fantasma.X + d.X, Y: _fantasma.Y + d.Y))
                                  .Where(v => v.X >= 0 && v.X < 13 && v.Y >= 0 && v.Y < 7)
                                  .ToList();
            if (validFDirs.Count > 0)
            {
                var r = new Random();
                _fantasma = validFDirs[r.Next(validFDirs.Count)];
            }
        }

        for (int i = 0; i < _corpo.Count; i++)
        {
            var pt = _corpo[i];
            var c = Contribuicoes[pt.Y * 13 + pt.X];
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
            var fCell = Contribuicoes[_fantasma.Y * 13 + _fantasma.X];
            fCell.EhFantasma = true;
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

            if (atual != start && Contribuicoes[atual.Y * 13 + atual.X].Nivel > 0)
                return caminho[1];

            foreach (var dir in dirs)
            {
                var nx = atual.X + dir.X;
                var ny = atual.Y + dir.Y;
                var vizinho = (X: nx, Y: ny);

                bool colisaoCorpo = EstiloAtual == EstiloAnimacaoGitHub.Cobrinha && _corpo.Contains(vizinho);

                if (nx >= 0 && nx < 13 && ny >= 0 && ny < 7 && !visitados.Contains(vizinho) && !colisaoCorpo)
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
            if (nx >= 0 && nx < 13 && ny >= 0 && ny < 7 && !colisaoCorpo)
                return vizinho;
        }
        return null;
    }
}

public class ContribuicaoDia : ObservableObject
{
    private int _nivel;
    private bool _ehCobra;
    private bool _ehCabecaCobra;
    private bool _ehPacMan;
    private bool _ehFantasma;

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
            if (EhPacMan) return new SolidColorBrush(Color.FromRgb(255, 255, 0)); 
            if (EhFantasma) return new SolidColorBrush(Color.FromRgb(255, 0, 0)); 
            if (EhCabecaCobra) return new SolidColorBrush(Color.FromRgb(0x9B, 0x59, 0xB6));
            if (EhCobra) return new SolidColorBrush(Color.FromRgb(0x8E, 0x44, 0xAD));
            
            return Nivel switch
            {
                0 => new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                1 => new SolidColorBrush(Color.FromRgb(0x0E, 0x44, 0x29)),
                2 => new SolidColorBrush(Color.FromRgb(0x00, 0x6D, 0x32)),
                3 => new SolidColorBrush(Color.FromRgb(0x26, 0xA6, 0x41)),
                4 => new SolidColorBrush(Color.FromRgb(0x39, 0xD3, 0x53)),
                _ => new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22))
            };
        }
    }
}


