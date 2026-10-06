using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public interface IConectividadeService : IDisposable
{
    event Action? Alterada;
    RedeWifiInfo? RedeAtual { get; }
    IReadOnlyList<DispositivoBluetoothInfo> DispositivosBluetooth { get; }
    string? Erro { get; }
    Task AtualizarAsync();
    void Iniciar();
    void Parar();
}
