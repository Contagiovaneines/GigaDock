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

public class GitHubWidgetViewModel : ObservableObject
{
    private string _nomeUsuario = string.Empty;
    private int _totalContribuicoes;
    private string _textoResumo = "GitHub";
    private bool _habilitado;
    private bool _painelAberto;
    private bool _carregando;
    private DockWindows.Core.Models.FormatoWidget _formato = DockWindows.Core.Models.FormatoWidget.Compacto;
        private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _snakeTimer;
    private static readonly HttpClient _http = new();
    
    // Variáveis da Cobrinha
    private List<(int X, int Y)> _snakeBody = new();
    private bool _cobrinhaRodando = false;


    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public DockWindows.Core.Models.FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }

    public string NomeUsuario
    {
        get => _nomeUsuario;
        set
        {
            if (SetProperty(ref _nomeUsuario, value))
                _ = CarregarContribuicoesAsync();
        }
    }

    public int TotalContribuicoes
    {
        get => _totalContribuicoes;
        set
        {
            if (SetProperty(ref _totalContribuicoes, value))
                OnPropertyChanged(nameof(TextoResumo));
        }
    }

    public string TextoResumo => TotalContribuicoes > 0 ? $"{TotalContribuicoes} contrib." : "GitHub";

    public bool PainelAberto { get => _painelAberto; set => SetProperty(ref _painelAberto, value); }
    public bool Carregando { get => _carregando; set => SetProperty(ref _carregando, value); }

    // Grid de contribuições: 7 dias (linhas) x 53 semanas (colunas)
    public ObservableCollection<ContribuicaoDia> Contribuicoes { get; } = new();

    public ICommand AlternarPainelCommand { get; }
    public ICommand AbrirPerfilCommand { get; }
    public ICommand JogarCobrinhaCommand { get; }

    public GitHubWidgetViewModel()
    {
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        JogarCobrinhaCommand = new RelayCommand(() => IniciarCobrinha());
          AbrirPerfilCommand = new RelayCommand(() =>
          {
            if (!string.IsNullOrEmpty(NomeUsuario))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"https://github.com/{NomeUsuario}") { UseShellExecute = true });
                }
                catch { }
            }
        });

                // Atualiza a cada 30 minutos
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _timer.Tick += (_, _) => _ = CarregarContribuicoesAsync();

        // Timer da Cobrinha (roda a cada 150ms)
        _snakeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _snakeTimer.Tick += (_, _) => TickCobrinha();
    }

    public void SincronizarUsuario(string? usuario)
    {
        if (!string.IsNullOrWhiteSpace(usuario) && usuario != _nomeUsuario)
        {
            _nomeUsuario = usuario;
            OnPropertyChanged(nameof(NomeUsuario));
            _ = CarregarContribuicoesAsync();
        }

        if (!_timer.IsEnabled)
            _timer.Start();
    }

    private async Task CarregarContribuicoesAsync()
    {
        if (string.IsNullOrWhiteSpace(_nomeUsuario)) return;

        Carregando = true;

        try
        {
            // Busca a página de contribuições do GitHub (retorna SVG/HTML público)
            var url = $"https://github.com/users/{_nomeUsuario}/contributions";
            var html = await _http.GetStringAsync(url);

            // Parse the contribution data from the HTML
            var dias = new List<ContribuicaoDia>();
            int total = 0;

            // Match: data-date="2024-01-01" data-level="0" (or data-count="N")
            var matches = Regex.Matches(html, @"data-date=""(\d{4}-\d{2}-\d{2})""\s+[^>]*data-level=""(\d+)""");
            
            if (matches.Count == 0)
            {
                // Fallback: try newer GitHub format
                matches = Regex.Matches(html, @"data-date=""(\d{4}-\d{2}-\d{2})""[^>]*>\s*(\d+)\s+contribution");
            }

            foreach (Match m in matches)
            {
                var date = DateTime.Parse(m.Groups[1].Value);
                var level = int.Parse(m.Groups[2].Value);
                dias.Add(new ContribuicaoDia { Data = date, Nivel = level });
            }

                        // Tenta obter o total pelas tooltips
            var matchesTooltip = Regex.Matches(html, @"(\d+)\s+contributions?\s+on");
            if (matchesTooltip.Count > 0)
            {
                foreach (Match mt in matchesTooltip)
                {
                    total += int.Parse(mt.Groups[1].Value);
                }
            }
            else
            {
                total = dias.Count(d => d.Nivel > 0);
            }

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Contribuicoes.Clear();

                // Pega os últimos 91 dias (13 semanas) para o mini-gráfico no popup
                var ultimos = dias.OrderByDescending(d => d.Data).Take(91).Reverse().ToList();
                foreach (var d in ultimos)
                    Contribuicoes.Add(d);

                TotalContribuicoes = total;
                Carregando = false;
            });
        }
        catch
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Carregando = false;
            });
        }
    }

    public void IniciarCobrinha()
    {
        if (_cobrinhaRodando || Contribuicoes.Count < 91) return;

        _snakeBody.Clear();
        _snakeBody.Add((0, 0));
        _cobrinhaRodando = true;
        _snakeTimer.Start();
    }

    private void TickCobrinha()
    {
        if (!_cobrinhaRodando || Contribuicoes.Count < 91)
        {
            _snakeTimer.Stop();
            return;
        }

        var head = _snakeBody.First();
        var nextStep = EncontrarProximoPasso(head);

        if (nextStep == null)
        {
            _cobrinhaRodando = false;
            _snakeTimer.Stop();
            foreach(var c in Contribuicoes) { c.EhCobra = false; c.EhCabecaCobra = false; }
            return;
        }

        var n = nextStep.Value;
        _snakeBody.Insert(0, n);

        int idx = n.Y * 13 + n.X;
        var cell = Contribuicoes[idx];

        if (cell.Nivel > 0)
        {
            cell.Nivel = 0; 
        }
        else
        {
            var tail = _snakeBody.Last();
            _snakeBody.RemoveAt(_snakeBody.Count - 1);
            var tailCell = Contribuicoes[tail.Y * 13 + tail.X];
            tailCell.EhCobra = false;
            tailCell.EhCabecaCobra = false;
        }

        for (int i = 0; i < _snakeBody.Count; i++)
        {
            var pt = _snakeBody[i];
            var c = Contribuicoes[pt.Y * 13 + pt.X];
            c.EhCabecaCobra = (i == 0);
            c.EhCobra = (i != 0);
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

                if (nx >= 0 && nx < 13 && ny >= 0 && ny < 7 && !visitados.Contains(vizinho) && !_snakeBody.Contains(vizinho))
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
            if (nx >= 0 && nx < 13 && ny >= 0 && ny < 7 && !_snakeBody.Contains(vizinho))
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

    public DateTime Data { get; set; }

    public int Nivel
    {
        get => _nivel;
        set { if (SetProperty(ref _nivel, value)) OnPropertyChanged(nameof(Cor)); }
    }

    public bool EhCobra
    {
        get => _ehCobra;
        set { if (SetProperty(ref _ehCobra, value)) OnPropertyChanged(nameof(Cor)); }
    }

    public bool EhCabecaCobra
    {
        get => _ehCabecaCobra;
        set { if (SetProperty(ref _ehCabecaCobra, value)) OnPropertyChanged(nameof(Cor)); }
    }

    public Brush Cor 
    {
        get
        {
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







