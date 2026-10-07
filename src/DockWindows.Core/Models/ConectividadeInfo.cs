namespace DockWindows.Core.Models;

public sealed record RedeWifiInfo(string Nome, int Intensidade, bool Conectada);
public sealed record RedeWifiDisponivelInfo(string Nome, int Intensidade, bool Segura, bool Conectada, bool PerfilSalvo);
public sealed record DispositivoBluetoothInfo(string Id, string Nome, bool Conectado);
