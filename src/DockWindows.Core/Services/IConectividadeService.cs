using DockWindows.Core.Models;

namespace DockWindows.Core.Services;

public interface IConectividadeService : IDisposable
{
    event Action? Alterada;
    RedeWifiInfo? RedeAtual { get; }
    IReadOnlyList<RedeWifiDisponivelInfo> RedesWifi { get; }
    IReadOnlyList<DispositivoBluetoothInfo> DispositivosBluetooth { get; }
    string? Erro { get; }
    Task AtualizarAsync();
    Task<bool> ConectarWifiAsync(string nome);
    Task<bool> DesconectarWifiAsync();
    void Iniciar();
    void Parar();
}
