namespace DockWindows.Core.Models;

public sealed record RedeWifiInfo(string Nome, int Intensidade, bool Conectada);
public sealed record DispositivoBluetoothInfo(string Id, string Nome, bool Conectado);
