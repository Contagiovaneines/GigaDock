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
    private static readonly HttpClient _http = new();

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

    public GitHubWidgetViewModel()
    {
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
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

            // Try to get total from page text
            var totalMatch = Regex.Match(html, @"([\d,]+)\s+contributions?\s+in the last year");
            if (totalMatch.Success)
            {
                total = int.Parse(totalMatch.Groups[1].Value.Replace(",", "").Replace(".", ""));
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
}

public class ContribuicaoDia
{
    public DateTime Data { get; set; }
    public int Nivel { get; set; } // 0-4

    public Brush Cor => Nivel switch
    {
        0 => new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
        1 => new SolidColorBrush(Color.FromRgb(0x0E, 0x44, 0x29)),
        2 => new SolidColorBrush(Color.FromRgb(0x00, 0x6D, 0x32)),
        3 => new SolidColorBrush(Color.FromRgb(0x26, 0xA6, 0x41)),
        4 => new SolidColorBrush(Color.FromRgb(0x39, 0xD3, 0x53)),
        _ => new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22))
    };
}
