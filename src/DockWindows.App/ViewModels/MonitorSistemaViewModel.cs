using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Infrastructure.Windows;
using DockWindows.Core.Models;

namespace DockWindows.App.ViewModels;

public class MonitorSistemaViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => TimerAtivo;
    public EstadoAmostra EstadoLeitura => Estilo.StartsWith("ram", StringComparison.Ordinal) ? _metricas.Ram.HasValue ? EstadoAmostra.Disponivel : EstadoAmostra.Indisponivel
        : Estilo is "rede" or "rede-grafico" or "rede-compacta" or "download" or "upload" ? _metricas.EstadoRede
        : Estilo == "armazenamento" ? _metricas.DiscoUsado.HasValue ? EstadoAmostra.Disponivel : EstadoAmostra.Indisponivel : _metricas.EstadoCpu;
    public DockWindows.Core.Widgets.SaudeWidget Saude => EstadoLeitura == EstadoAmostra.Indisponivel ? DockWindows.Core.Widgets.SaudeWidget.Indisponivel : DockWindows.Core.Widgets.SaudeWidget.Disponivel;
    public string? MotivoEstado => EstadoLeitura == EstadoAmostra.PrimeiraAmostra ? "Aguardando a segunda amostra para calcular a taxa." : Descricao;

    private readonly IMetricasSistemaService _service;
    private readonly DispatcherTimer _timer;
    private bool _habilitado, _painelAberto, _disposed, _modoEconomico;
    private string _estilo = "expandido";
    private FormatoWidget _formato = FormatoWidget.Expandido;
    private MetricasSistema _metricas = new(null, null, null, null, null, "—");
    public MonitorSistemaViewModel(IMetricasSistemaService? service = null)
    {
        _service = service ?? new MetricasSistemaService();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => AtualizarMetricas();
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
    }
    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }
    public bool ModoEconomico
    {
        get => _modoEconomico;
        set
        {
            if (!SetProperty(ref _modoEconomico, value)) return;
            _timer.Interval = TimeSpan.FromSeconds(value ? 10 : 2);
        }
    }
    public bool PainelAberto { get => _painelAberto; set => SetProperty(ref _painelAberto, value); }
    public FormatoWidget Formato { get => _formato; set => SetProperty(ref _formato, value); }
    public string Estilo { get => _estilo; set { if (SetProperty(ref _estilo, value)) { _service.Suspender(); LimparHistorico(); Notificar(); } } }
    public double UsoCpu => _metricas.Cpu ?? double.NaN;
    public double UsoRam => _metricas.Ram ?? double.NaN;
    public double Valor => Estilo.StartsWith("ram", StringComparison.Ordinal) ? UsoRam : Estilo == "armazenamento" ? _metricas.DiscoUsado ?? double.NaN : UsoCpu;
    public double Largura => Estilo switch
    {
        "cpu" or "ram" or "ventoinha-cpu" => 64,
        "rede-compacta" or "atividade-compacta" => 82,
        "aneis-cpu-ram" or "medidores-cpu-ram" => 132,
        "atividade-larga" => 210,
        _ => 190
    };
    public string TextoResumo => $"CPU {Percentual(_metricas.Cpu)} | RAM {Percentual(_metricas.Ram)}";
    public string Titulo => Estilo.StartsWith("ram", StringComparison.Ordinal) ? "RAM" : Estilo == "armazenamento" ? "Disco do Windows" : Estilo is "rede" or "rede-grafico" or "rede-compacta" ? "Rede" : Estilo == "download" ? "Download" : Estilo == "upload" ? "Upload" : "CPU";
    public string TextoValor => Estilo switch { "download" => "↓ " + Velocidade(_metricas.Download), "upload" => "↑ " + Velocidade(_metricas.Upload), "rede" or "rede-grafico" or "rede-compacta" => "↓ " + Velocidade(_metricas.Download), "armazenamento" => _metricas.DiscoLivre, "compacto" or "expandido" or "atividade-compacta" or "atividade-larga" => TextoResumo, _ => Percentual(double.IsNaN(Valor) ? null : Valor) };
    public string TextoSecundario => Estilo is "rede" or "rede-grafico" or "rede-compacta" ? "↑ " + Velocidade(_metricas.Upload) : "";
    public string Descricao => (Estilo == "ventoinha-cpu" ? "Indicador visual baseado no uso da CPU; não representa RPM da ventoinha. " : "") + (EstadoLeitura == EstadoAmostra.PrimeiraAmostra ? "Aguardando primeira taxa. " : EstadoLeitura == EstadoAmostra.Indisponivel ? "Leitura indisponível. " : "") + "Leituras locais. Rede soma interfaces ativas (VPN pode duplicar tráfego). Disco: unidade do Windows. Traço indica dado indisponível.";
    public ObservableCollection<double> Historico { get; } = new();
    public ObservableCollection<double> HistoricoSecundario { get; } = new();
    public ICommand AlternarPainelCommand { get; }
    public static string Velocidade(double? bytes) => bytes == null ? "—" : bytes >= 1024 * 1024 ? $"{bytes / (1024 * 1024):F1} MB/s" : bytes >= 1024 ? $"{bytes / 1024:F0} KB/s" : $"{bytes:F0} B/s";
    private static string Percentual(double? valor) => valor == null ? "—" : valor.Value.ToString("F0", CultureInfo.CurrentCulture) + "%";
    public void AtualizarMetricas()
    {
        if (_disposed) return;
        try { _metricas = _service.Ler(Estilo switch {
            "compacto" or "expandido" or "aneis-cpu-ram" or "medidores-cpu-ram" or "atividade-compacta" or "atividade-larga" => MetricasSolicitadas.Cpu | MetricasSolicitadas.Ram,
            "ram" or "ram-grafico" => MetricasSolicitadas.Ram,
            "rede" or "rede-grafico" or "rede-compacta" or "download" or "upload" => MetricasSolicitadas.Rede,
            "armazenamento" => MetricasSolicitadas.Disco, _ => MetricasSolicitadas.Cpu }); } catch { _metricas = new(null, null, null, null, null, "—"); }
        var v = Estilo == "rede-grafico" ? _metricas.Download : double.IsNaN(Valor) ? null : (double?)Valor;
        if (v != null) Adicionar(Historico, v.Value);
        if (Estilo == "rede-grafico" && _metricas.Upload != null) Adicionar(HistoricoSecundario, _metricas.Upload.Value);
        Notificar();
    }
    private static void Adicionar(ObservableCollection<double> lista, double valor) { lista.Add(valor); if (lista.Count > 30) lista.RemoveAt(0); }
    private void LimparHistorico() { Historico.Clear(); HistoricoSecundario.Clear(); }
    private void Notificar() { foreach (var p in new[] { nameof(UsoCpu), nameof(UsoRam), nameof(Valor), nameof(Largura), nameof(TextoResumo), nameof(Titulo), nameof(TextoValor), nameof(TextoSecundario), nameof(EstadoLeitura), nameof(Descricao) }) OnPropertyChanged(p); }
    public bool TimerAtivo => _timer.IsEnabled;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        _timer.Stop();
        if (!_disposed && estado.Visual) { AtualizarMetricas(); _timer.Start(); } else _service.Suspender();
        if (!estado.Habilitado) LimparHistorico();
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _timer.Stop(); _service.Dispose(); }
}
