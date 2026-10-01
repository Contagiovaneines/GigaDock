using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class CalendarioWidgetViewModel : ObservableObject
{
    private static readonly CultureInfo PtBr = new("pt-BR");
    private readonly DispatcherTimer _timer;
    private readonly Action? _onAbrirAjustes;

    private bool _habilitado = true;
    private FormatoWidget _formato = FormatoWidget.Compacto;
    private bool _painelAberto;
    private DateTime _dataSelecionada = DateTime.Today;
    private DateTime _dataExibicao = DateTime.Today;

    public DateTime DataSelecionada
    {
        get => _dataSelecionada;
        set => SetProperty(ref _dataSelecionada, value);
    }

    public DateTime DataExibicao
    {
        get => _dataExibicao;
        set => SetProperty(ref _dataExibicao, value);
    }

    public CalendarioWidgetViewModel(Action? onAbrirAjustes = null)
    {
        _onAbrirAjustes = onAbrirAjustes;
        Compromissos = new ObservableCollection<CompromissoLocal>();

        AlternarPainelCommand = new RelayCommand(AlternarPainel);
        FecharPainelCommand = new RelayCommand(FecharPainel);
        AbrirAjustesCommand = new RelayCommand(() =>
        {
            FecharPainel();
            _onAbrirAjustes?.Invoke();
        });

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _timer.Tick += (s, e) => AtualizarDataECompromisso();
        _timer.Start();

        AtualizarDataECompromisso();
    }

    public bool Habilitado
    {
        get => _habilitado;
        set => SetProperty(ref _habilitado, value);
    }

    public FormatoWidget Formato
    {
        get => _formato;
        set
        {
            if (SetProperty(ref _formato, value))
            {
                OnPropertyChanged(nameof(EhExpandido));
                OnPropertyChanged(nameof(TextoExibicao));
            }
        }
    }

    public bool EhExpandido => Formato == FormatoWidget.Expandido;

    public bool PainelAberto
    {
        get => _painelAberto;
        set => SetProperty(ref _painelAberto, value);
    }

    public ObservableCollection<CompromissoLocal> Compromissos { get; }
    private readonly System.Collections.Generic.List<CompromissoLocal> _eventosIcal = new();

    public System.Collections.Generic.IEnumerable<CompromissoLocal> TodosCompromissos => 
        Compromissos.Concat(_eventosIcal).OrderBy(c => c.DataHora);

    public CompromissoLocal? ProximoCompromisso
    {
        get
        {
            var agora = DateTime.Now;
            // PrÃ³ximo compromisso a partir de hoje
            return TodosCompromissos
                .Where(c => c.DataHora >= agora.AddMinutes(-30))
                .FirstOrDefault()
                ?? TodosCompromissos.FirstOrDefault();
        }
    }

    private string _urlIcal = string.Empty;

    public void SincronizarUrlIcal(string url)
    {
        _urlIcal = url ?? string.Empty;
        _ = AtualizarDoIcalAsync();
    }

    private async System.Threading.Tasks.Task AtualizarDoIcalAsync()
    {
        _eventosIcal.Clear();

        if (!string.IsNullOrWhiteSpace(_urlIcal))
        {
            try
            {
                string icalData = string.Empty;
                if (_urlIcal.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(15);
                    icalData = await client.GetStringAsync(_urlIcal);
                }
                else if (System.IO.File.Exists(_urlIcal))
                {
                    icalData = await System.IO.File.ReadAllTextAsync(_urlIcal);
                }

                if (string.IsNullOrWhiteSpace(icalData)) return;

                var linhas = icalData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                bool inEvent = false;
                string titulo = "Evento Importado";
                DateTime? dataHora = null;

                foreach (var linha in linhas)
                {
                    if (linha.StartsWith("BEGIN:VEVENT"))
                    {
                        inEvent = true;
                        titulo = "Evento Importado";
                        dataHora = null;
                    }
                    else if (linha.StartsWith("END:VEVENT"))
                    {
                        inEvent = false;
                        if (dataHora.HasValue && dataHora.Value > DateTime.Now.AddDays(-1) && dataHora.Value < DateTime.Now.AddDays(30))
                        {
                            _eventosIcal.Add(new CompromissoLocal
                            {
                                Titulo = titulo,
                                DataHora = dataHora.Value
                            });
                        }
                    }
                    else if (inEvent)
                    {
                        if (linha.StartsWith("SUMMARY:"))
                        {
                            titulo = linha.Substring(8).Trim();
                        }
                        else if (linha.StartsWith("DTSTART;") || linha.StartsWith("DTSTART:"))
                        {
                            var parts = linha.Split(':');
                            if (parts.Length == 2)
                            {
                                var val = parts[1].Replace("Z", "");
                                if (DateTime.TryParseExact(val, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                                {
                                    // Se for UTC, ajustar para local
                                    if (linha.Contains("Z")) dt = dt.ToLocalTime();
                                    dataHora = dt;
                                }
                                else if (DateTime.TryParseExact(val, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtDia))
                                {
                                    dataHora = dtDia;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        System.Windows.Application.Current?.Dispatcher?.InvokeAsync(AtualizarDataECompromisso);
    }

    public bool TemCompromissos => ProximoCompromisso != null;

    public string DataCurta => DateTime.Now.ToString("dd MMM", PtBr);
    public string DataCompleta => DateTime.Now.ToString("dddd, dd 'de' MMMM", PtBr);
    public string DiaDaSemanaCurto => DateTime.Now.ToString("ddd", PtBr).ToUpperInvariant();
    public string DiaDoMes => DateTime.Now.Day.ToString();

    public string TituloEventoCurto => ProximoCompromisso != null ? ProximoCompromisso.Titulo : "Compromissos";
    public string HoraEventoCurto => ProximoCompromisso != null ? ProximoCompromisso.DataHora.ToString("HH:mm") : DataCurta;

    public string TextoCompacto
    {
        get
        {
            if (ProximoCompromisso != null)
            {
                var hora = ProximoCompromisso.DataHora.ToString("HH:mm");
                return $"{hora} {ProximoCompromisso.Titulo}";
            }
            return DataCurta;
        }
    }

    public string TextoExpandido
    {
        get
        {
            var hoje = DateTime.Now.ToString("ddd, dd MMM", PtBr);
            if (ProximoCompromisso != null)
            {
                var hora = ProximoCompromisso.DataHora.ToString("HH:mm");
                return $"{hoje} â€¢ {hora} {ProximoCompromisso.Titulo}";
            }
            return $"{hoje} â€¢ Sem eventos pendentes";
        }
    }

    public string TextoExibicao => EhExpandido ? TextoExpandido : TextoCompacto;

    public string TextoDica
    {
        get
        {
            if (ProximoCompromisso != null)
            {
                return $"PrÃ³ximo compromisso:\n{ProximoCompromisso.Titulo}\n{ProximoCompromisso.DataHora:dd/MM/yyyy HH:mm}\nClique para ver eventos";
            }
            return $"{DataCompleta}\nNenhum evento configurado\nClique para abrir o calendÃ¡rio";
        }
    }

    public ICommand AlternarPainelCommand { get; }
    public ICommand FecharPainelCommand { get; }
    public ICommand AbrirAjustesCommand { get; }

    public void SincronizarCompromissos(IEnumerable<CompromissoLocal> lista)
    {
        Compromissos.Clear();
        foreach (var c in lista.OrderBy(c => c.DataHora))
        {
            Compromissos.Add(c);
        }
        AtualizarDataECompromisso();
    }

    public void AtualizarDataECompromisso()
    {
        OnPropertyChanged(nameof(ProximoCompromisso));
        OnPropertyChanged(nameof(TemCompromissos));
        OnPropertyChanged(nameof(DataCurta));
        OnPropertyChanged(nameof(DataCompleta));
        OnPropertyChanged(nameof(DiaDaSemanaCurto));
        OnPropertyChanged(nameof(DiaDoMes));
        OnPropertyChanged(nameof(TituloEventoCurto));
        OnPropertyChanged(nameof(HoraEventoCurto));
        OnPropertyChanged(nameof(TextoCompacto));
        OnPropertyChanged(nameof(TextoExpandido));
        OnPropertyChanged(nameof(TextoExibicao));
        OnPropertyChanged(nameof(TextoDica));
    }

    private void AlternarPainel()
    {
        PainelAberto = !PainelAberto;
    }

    private void FecharPainel()
    {
        PainelAberto = false;
    }
}


