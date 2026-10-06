using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DockWindows.App.Common;
using DockWindows.Core.Models;
using DockWindows.Core.Services;
using DockWindows.Core.Widgets;

namespace DockWindows.App.ViewModels;

public sealed class ConectividadeViewModel : ObservableObject, IAtividadeWidget
{
    private readonly IConectividadeService _service;
    private bool _disposed;
    private bool _habilitado;
    private bool _painelAberto;
    private string _wifi = "Wi-Fi desconectado";
    private int _intensidade;

    public ConectividadeViewModel(IConectividadeService service)
    {
        _service = service;
        _service.Alterada += AoAlterar;
        AlternarPainelCommand = new RelayCommand(() => PainelAberto = !PainelAberto);
        AtualizarCommand = new RelayCommand(() => _ = _service.AtualizarAsync());
        AbrirRedeCommand = new RelayCommand(() => AbrirConfiguracao("ms-settings:network-wifi"));
        AbrirBluetoothCommand = new RelayCommand(() => AbrirConfiguracao("ms-settings:bluetooth"));
    }
    public ObservableCollection<DispositivoBluetoothInfo> Bluetooth { get; } = new();
    public bool? EmExecucao => Habilitado;
    public SaudeWidget Saude => string.IsNullOrEmpty(_service.Erro) ? SaudeWidget.Disponivel : SaudeWidget.Erro;
    public string? MotivoEstado => _service.Erro ?? Wifi;
    public bool Habilitado { get => _habilitado; set { if (SetProperty(ref _habilitado, value)) { if (value) _service.Iniciar(); else _service.Parar(); } } }
    public bool PainelAberto { get => _painelAberto; set => SetProperty(ref _painelAberto, value); }
    public string Wifi { get => _wifi; private set => SetProperty(ref _wifi, value); }
    public int Intensidade { get => _intensidade; private set => SetProperty(ref _intensidade, value); }
    public ICommand AlternarPainelCommand { get; }
    public ICommand AtualizarCommand { get; }
    public ICommand AbrirRedeCommand { get; }
    public ICommand AbrirBluetoothCommand { get; }
    public int BluetoothConectados => Bluetooth.Count(x => x.Conectado);

    public void DefinirAtividade(EstadoAtividade estado)
    {
        if (_disposed) return;
        if (estado.Habilitado) _service.Iniciar(); else _service.Parar();
        if (!estado.Visual) PainelAberto = false;
    }

    private void AoAlterar() => Application.Current?.Dispatcher.BeginInvoke(() =>
    {
        if (_disposed) return;
        var rede = _service.RedeAtual;
        Wifi = rede == null ? "Wi-Fi desconectado" : rede.Nome;
        Intensidade = rede?.Intensidade ?? 0;
        Bluetooth.Clear();
        foreach (var item in _service.DispositivosBluetooth.Where(x => x.Conectado)) Bluetooth.Add(item);
        OnPropertyChanged(nameof(BluetoothConectados));
    });

    private static void AbrirConfiguracao(string uri)
    {
        try { Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true }); } catch { }
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _service.Alterada -= AoAlterar; _service.Dispose(); }
}
