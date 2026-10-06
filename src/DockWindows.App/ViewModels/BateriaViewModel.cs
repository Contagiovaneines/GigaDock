using System.Windows.Threading;
using DockWindows.App.Common;
using DockWindows.Infrastructure.Windows;

namespace DockWindows.App.ViewModels;

public sealed class BateriaViewModel : ObservableObject, IAtividadeWidget
{
    public bool? EmExecucao => TimerAtivo;

    public DockWindows.Core.Widgets.SaudeWidget Saude => Estado is EstadoCargaBateria.Indisponivel or EstadoCargaBateria.Desconhecida or EstadoCargaBateria.SemBateria ? DockWindows.Core.Widgets.SaudeWidget.Indisponivel : DockWindows.Core.Widgets.SaudeWidget.Disponivel;
    public string? MotivoEstado => Descricao;

    private readonly IBateriaService _service;
    private readonly DispatcherTimer _timer;
    private bool _habilitado, _disposed;
    private StatusBateria _status = new(null, null, false, null, false);
    private string _estilo = "compacto";
    public string Estilo { get => _estilo; set => SetProperty(ref _estilo, value); }
    public double Carga => _status.Porcentagem ?? double.NaN;

    public BateriaViewModel(IBateriaService? service = null)
    {
        _service = service ?? new BateriaService();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Atualizar();
    }

    public bool Habilitado { get => _habilitado; set => SetProperty(ref _habilitado, value); }

    public string Porcentagem => _status.Porcentagem is int carga ? $"{carga}%" : "—";
    public double LarguraCarga => 18 * (_status.Porcentagem ?? 0) / 100.0;
    public bool Carregando => _status.Carregando;
    public string CorCarga => Carregando ? "#72D99C" : _status.Porcentagem is <= 20 ? "#FF7777" : "#E1E4E8";
    public EstadoCargaBateria Estado => _status.Estado;
    public string Descricao => _status.PossuiBateria == false ? "Este computador não possui bateria."
        : _status.Porcentagem == null ? "Não foi possível obter a carga da bateria."
        : $"Bateria: {Porcentagem}" + (Carregando ? " · Carregando" : _status.NaTomada == true ? " · Conectado à tomada" : _status.NaTomada == false ? " · Usando bateria" : "");

    public void Atualizar()
    {
        if (_disposed) return;
        try { _status = _service.ObterStatus(); }
        catch { _status = new(null, null, false, null, false); }
        OnPropertyChanged(nameof(Estado));
        OnPropertyChanged(nameof(Porcentagem));
        OnPropertyChanged(nameof(Carga));
        OnPropertyChanged(nameof(LarguraCarga));
        OnPropertyChanged(nameof(Carregando));
        OnPropertyChanged(nameof(CorCarga));
        OnPropertyChanged(nameof(Descricao));
    }

    public bool TimerAtivo => _timer.IsEnabled;
    public void DefinirAtividade(DockWindows.Core.Widgets.EstadoAtividade estado)
    {
        _timer.Stop();
        if (!_disposed && estado.Visual) { Atualizar(); _timer.Start(); }
    }
    public void Dispose() { _disposed = true; _timer.Stop(); }
}
