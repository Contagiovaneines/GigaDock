using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class CalendarioWidgetViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => TimerAtivo || _ocupado;

    public DockWindows.Core.Widgets.SaudeWidget Saude => string.IsNullOrEmpty(ErroSincronizacao) ? DockWindows.Core.Widgets.SaudeWidget.Disponivel : DockWindows.Core.Widgets.SaudeWidget.Erro;
    public string? MotivoEstado => ErroSincronizacao;

    private string _estilo = "proximo";
    public string Estilo { get => _estilo; set { if (SetProperty(ref _estilo, value)) { AgendarVisual(); OnPropertyChanged(nameof(EhReuniao)); OnPropertyChanged(nameof(EhCentral)); } } }
    public bool EhReuniao => Estilo is "reuniao" or "central-reuniao";
    public bool EhCentral => Estilo == "central-reuniao";
    public CompromissoLocal? ProximaReuniao => TodosCompromissos.FirstOrDefault(c => !c.DiaInteiro && c.DataHora >= DateTime.Now);
    public string TituloReuniao => ProximaReuniao?.Titulo ?? "Nenhuma reunião futura";
    public string HorarioReuniao => ProximaReuniao?.DataHora.ToString("dd/MM · HH:mm", PtBr) ?? "Configure seus compromissos nos Ajustes";
    public string LocalReuniao => ProximaReuniao?.Local ?? "";
    public string ContagemReuniao => ProximaReuniao == null ? "" : $"Começa em {Math.Max(0, (int)Math.Ceiling((ProximaReuniao.DataHora - DateTime.Now).TotalMinutes))} min";
    public static string? LinkReuniao(CompromissoLocal? compromisso) => Uri.TryCreate(compromisso?.Local, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo) ? uri.AbsoluteUri : null;
    public bool TemLinkReuniao => LinkReuniao(ProximaReuniao) != null;
    private string _erroReuniao = "";
    public string ErroReuniao { get => _erroReuniao; private set => SetProperty(ref _erroReuniao, value); }
    public ICommand EntrarReuniaoCommand { get; }
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
        EntrarReuniaoCommand = new RelayCommand(() =>
        {
            var link = LinkReuniao(ProximaReuniao);
            if (link == null) { ErroReuniao = "Informe um link HTTPS no campo Local do compromisso."; return; }
            var result = new DockWindows.Infrastructure.Windows.LauncherService().Executar(new ItemFixado { Tipo = TipoItem.WebUrl, CaminhoOuUrl = link, Titulo = TituloReuniao });
            ErroReuniao = result.Sucesso ? "" : "Não foi possível abrir o link da reunião.";
        });
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
        _timer.Tick += (_, _) => { AtualizarDataECompromisso(); _ = AtualizarDoIcalAsync(); AgendarVisual(); };

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
                OnPropertyChanged(nameof(EventosVisiveis));
                OnPropertyChanged(nameof(TemEventosVisiveis));
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
            // Próximo compromisso a partir de hoje
            return TodosCompromissos
                .Where(c => c.DataHora >= agora.AddMinutes(-30))
                .FirstOrDefault()
                ?? TodosCompromissos.FirstOrDefault();
        }
    }

    private string _urlIcal = string.Empty;
    private string? _textoIcal;
    private string _fusoImportacao = TimeZoneInfo.Local.Id;
    private bool _visual, _ocupado, _disposed;
    private DateTimeOffset _cacheAte;
    private string _erroSincronizacao = "";
    public string ErroSincronizacao { get => _erroSincronizacao; private set { if (SetProperty(ref _erroSincronizacao, value)) OnPropertyChanged(nameof(TextoDica)); } }
    public DateTimeOffset? UltimaSincronizacao { get; private set; }
    private System.Threading.CancellationTokenSource? _consulta;
    public bool TimerAtivo => _timer.IsEnabled;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        _visual = estado.Visual && !_disposed; _timer.Stop();
        if (!_visual) { _consulta?.Cancel(); PainelAberto = false; return; }
        AtualizarDataECompromisso(); _ = AtualizarDoIcalAsync(); AgendarVisual();
    }
    private void AgendarVisual()
    {
        _timer.Stop(); if (!_visual || _disposed) return;
        var agora = DateTime.Now;
        var limite = DateTime.Today.AddDays(1);
        var evento = TodosCompromissos.FirstOrDefault(c => c.DataHora > agora)?.DataHora;
        if (evento.HasValue && evento.Value < limite) limite = evento.Value;
        if (Estilo is "reuniao" or "central-reuniao") limite = agora.AddSeconds(60 - agora.Second);
        if (!string.IsNullOrWhiteSpace(_urlIcal)) { var proxima = _cacheAte > DateTimeOffset.UtcNow ? _cacheAte.LocalDateTime : agora.AddMinutes(15); if (limite > proxima) limite = proxima; }
        _timer.Interval = TimeSpan.FromSeconds(Math.Max(1, (limite - agora).TotalSeconds)); _timer.Start();
    }
    public void Dispose() { _disposed = true; _visual = false; _timer.Stop(); _consulta?.Cancel(); }


    public void SincronizarUrlIcal(string url)
    {
        var novo = url ?? string.Empty;
        if (_urlIcal == novo) return;
        _urlIcal = novo; _cacheAte = default; _consulta?.Cancel(); _eventosIcal.Clear(); _textoIcal = null;
        if (_visual) _ = AtualizarDoIcalAsync();
    }

    private async System.Threading.Tasks.Task AtualizarDoIcalAsync()
    {
        if (!_visual || _disposed || _ocupado || _cacheAte > DateTimeOffset.UtcNow) return;
        _ocupado = true;
        var url = _urlIcal;
        using var consulta = new System.Threading.CancellationTokenSource();
        _consulta = consulta;
        var eventos = new List<CompromissoLocal>();
        try
        {
        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                string icalData = string.Empty;
                if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    using var client = new System.Net.Http.HttpClient();
                    if (!DockWindows.Core.Validation.ItemValidator.ValidarUrl(url).Valido) throw new FormatException("Link inválido.");
                    client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
                    client.Timeout = TimeSpan.FromSeconds(15);
                    icalData = await client.GetStringAsync(url, consulta.Token);
                }
                else if (System.IO.File.Exists(url))
                {
                    if (new System.IO.FileInfo(url).Length > 2 * 1024 * 1024) throw new FormatException("Arquivo excede 2 MB.");
                    icalData = await System.IO.File.ReadAllTextAsync(url, consulta.Token);
                }

                if (string.IsNullOrWhiteSpace(icalData)) throw new FormatException("Calendário vazio ou caminho indisponível.");

                eventos.AddRange(await Task.Run(() => DockWindows.Core.Widgets.ImportadorCalendario.Importar(
                    icalData, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(30), cancelamento: consulta.Token), consulta.Token));
                if (!consulta.IsCancellationRequested && _visual && !_disposed && url == _urlIcal) { _textoIcal = icalData; _fusoImportacao = TimeZoneInfo.Local.Id; }

            }
            catch (OperationCanceledException) { return; }
            catch { ErroSincronizacao = "Não foi possível importar o calendário. Dados anteriores preservados; confira o arquivo, link e fusos."; _cacheAte = DateTimeOffset.UtcNow.AddSeconds(30); AgendarVisual(); return; }
        }
        if (consulta.IsCancellationRequested || !_visual || _disposed || url != _urlIcal) return;
        _eventosIcal.Clear(); _eventosIcal.AddRange(eventos);
        _cacheAte = DateTimeOffset.UtcNow.AddMinutes(15); ErroSincronizacao = ""; UltimaSincronizacao = DateTimeOffset.UtcNow;
        AtualizarDataECompromisso(); AgendarVisual();
        }
        finally
        {
            _ocupado = false;
            if (ReferenceEquals(_consulta, consulta)) _consulta = null;
            if (_visual && !_disposed && (consulta.IsCancellationRequested || url != _urlIcal)) _ = AtualizarDoIcalAsync();
        }
    }

    public System.Collections.Generic.IEnumerable<CompromissoLocal> EventosVisiveis => EhExpandido
        ? TodosCompromissos.Where(c => c.DataHora.Date == DateTime.Today).Take(2)
        : TodosCompromissos.Where(c => c.DataHora >= DateTime.Now).Take(1);
    public bool TemEventosVisiveis => EventosVisiveis.Any();

    public bool TemCompromissos => ProximoCompromisso != null;

    public string DataCurta => DateTime.Now.ToString("dd MMM", PtBr);
    public string DataCompleta => DateTime.Now.ToString("dddd, dd 'de' MMMM", PtBr);
    public string DiaDaSemanaCurto => DateTime.Now.ToString("ddd", PtBr).ToUpperInvariant();
    public string DiaDoMes => DateTime.Now.Day.ToString();

    public string TituloEventoCurto => ProximoCompromisso != null ? ProximoCompromisso.Titulo : "Compromissos";
    public string HoraEventoCurto => ProximoCompromisso != null ? (ProximoCompromisso.DiaInteiro ? "Dia inteiro" : ProximoCompromisso.DataHora.ToString("HH:mm")) : DataCurta;

    public string TextoCompacto
    {
        get
        {
            if (ProximoCompromisso != null)
            {
                var hora = (ProximoCompromisso.DiaInteiro ? "Dia inteiro" : ProximoCompromisso.DataHora.ToString("HH:mm"));
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
                var hora = (ProximoCompromisso.DiaInteiro ? "Dia inteiro" : ProximoCompromisso.DataHora.ToString("HH:mm"));
                return $"{hoje} • {hora} {ProximoCompromisso.Titulo}";
            }
            return $"{hoje} • Sem eventos pendentes";
        }
    }

    public string TextoExibicao => EhExpandido ? TextoExpandido : TextoCompacto;

    public string TextoDica
    {
        get
        {
            if (ProximoCompromisso != null)
            {
                return $"Próximo compromisso:\n{ProximoCompromisso.Titulo}\n{ProximoCompromisso.DataHora:dd/MM/yyyy HH:mm}\nClique para ver eventos";
            }
            return $"{DataCompleta}\nNenhum evento configurado\nClique para abrir o calendário";
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
        if (_visual) { AtualizarDataECompromisso(); AgendarVisual(); }
    }

    public void AtualizarDataECompromisso()
    {
        if (_textoIcal != null && _fusoImportacao != TimeZoneInfo.Local.Id)
        {
            try
            {
                var eventos = DockWindows.Core.Widgets.ImportadorCalendario.Importar(_textoIcal, DateTime.Today.AddDays(-1), DateTime.Today.AddDays(30));
                _eventosIcal.Clear(); _eventosIcal.AddRange(eventos); _fusoImportacao = TimeZoneInfo.Local.Id;
            }
            catch { ErroSincronizacao = "Não foi possível converter o calendário para o novo fuso."; }
        }
        foreach (var p in new[] { nameof(ProximaReuniao), nameof(TituloReuniao), nameof(HorarioReuniao), nameof(LocalReuniao), nameof(ContagemReuniao), nameof(TemLinkReuniao) }) OnPropertyChanged(p);
        OnPropertyChanged(nameof(ProximoCompromisso));
        OnPropertyChanged(nameof(EventosVisiveis));
        OnPropertyChanged(nameof(TemEventosVisiveis));
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
