using System.Runtime.InteropServices;
using System.Text;
using Windows.Devices.Enumeration;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public sealed class ConectividadeService : IConectividadeService
{
    private readonly Dictionary<string, DispositivoBluetoothInfo> _bluetooth = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private DeviceWatcher? _watcher;
    private bool _disposed;

    public event Action? Alterada;
    public RedeWifiInfo? RedeAtual { get; private set; }
    public IReadOnlyList<DispositivoBluetoothInfo> DispositivosBluetooth { get { lock (_gate) return _bluetooth.Values.OrderBy(x => x.Nome).ToList(); } }
    public string? Erro { get; private set; }

    public void Iniciar()
    {
        if (_disposed || _watcher != null) return;
        var propriedades = new[] { "System.Devices.Aep.IsConnected" };
        _watcher = DeviceInformation.CreateWatcher(
            "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"",
            propriedades, DeviceInformationKind.AssociationEndpoint);
        _watcher.Added += AoAdicionar;
        _watcher.Updated += AoAtualizar;
        _watcher.Removed += AoRemover;
        _watcher.EnumerationCompleted += AoConcluir;
        _watcher.Start();
        _ = AtualizarAsync();
    }

    public void Parar()
    {
        var watcher = _watcher;
        _watcher = null;
        if (watcher == null) return;
        watcher.Added -= AoAdicionar; watcher.Updated -= AoAtualizar; watcher.Removed -= AoRemover; watcher.EnumerationCompleted -= AoConcluir;
        if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted) watcher.Stop();
    }

    public Task AtualizarAsync()
    {
        if (_disposed) return Task.CompletedTask;
        try { RedeAtual = ObterRedeAtual(); Erro = null; }
        catch { RedeAtual = null; Erro = "Não foi possível consultar o Wi-Fi."; }
        Alterada?.Invoke();
        return Task.CompletedTask;
    }

    private void AoAdicionar(DeviceWatcher sender, DeviceInformation info)
    {
        var conectado = ObterConectado(info.Properties);
        if (string.IsNullOrWhiteSpace(info.Name)) return;
        lock (_gate) _bluetooth[info.Id] = new(info.Id, info.Name, conectado);
        Alterada?.Invoke();
    }

    private void AoAtualizar(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        lock (_gate)
        {
            if (!_bluetooth.TryGetValue(update.Id, out var atual)) return;
            _bluetooth[update.Id] = atual with { Conectado = ObterConectado(update.Properties, atual.Conectado) };
        }
        Alterada?.Invoke();
    }

    private void AoRemover(DeviceWatcher sender, DeviceInformationUpdate update) { lock (_gate) _bluetooth.Remove(update.Id); Alterada?.Invoke(); }
    private void AoConcluir(DeviceWatcher sender, object args) => Alterada?.Invoke();
    private static bool ObterConectado(IReadOnlyDictionary<string, object> props, bool padrao = false) =>
        props.TryGetValue("System.Devices.Aep.IsConnected", out var valor) && valor is bool conectado ? conectado : padrao;

    private static RedeWifiInfo? ObterRedeAtual()
    {
        if (WlanOpenHandle(2, IntPtr.Zero, out _, out var client) != 0) return null;
        try
        {
            if (WlanEnumInterfaces(client, IntPtr.Zero, out var lista) != 0) return null;
            try
            {
                var quantidade = Marshal.ReadInt32(lista);
                var inicio = IntPtr.Add(lista, 8);
                var tamanho = Marshal.SizeOf<WlanInterfaceInfo>();
                for (var i = 0; i < quantidade; i++)
                {
                    var info = Marshal.PtrToStructure<WlanInterfaceInfo>(IntPtr.Add(inicio, i * tamanho));
                    if (info.State != 1) continue;
                    var guid = info.InterfaceGuid;
                    if (WlanQueryInterface(client, ref guid, 7, IntPtr.Zero, out _, out var dados, out _) != 0) continue;
                    try
                    {
                        var conexao = Marshal.PtrToStructure<WlanConnectionAttributes>(dados);
                        var bytes = conexao.Association.Ssid.Bytes ?? Array.Empty<byte>();
                        var nome = Encoding.UTF8.GetString(bytes, 0, (int)Math.Min(conexao.Association.Ssid.Length, (uint)bytes.Length));
                        return new RedeWifiInfo(nome, (int)Math.Clamp(conexao.Association.SignalQuality, 0u, 100u), true);
                    }
                    finally { WlanFreeMemory(dados); }
                }
            }
            finally { WlanFreeMemory(lista); }
        }
        finally { WlanCloseHandle(client, IntPtr.Zero); }
        return null;
    }

    public void Dispose() { if (_disposed) return; _disposed = true; Parar(); lock (_gate) _bluetooth.Clear(); }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanInterfaceInfo { public Guid InterfaceGuid; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Description; public int State; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Dot11Ssid { public uint Length; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Bytes; }
    [StructLayout(LayoutKind.Sequential)]
    private struct WlanAssociationAttributes { public Dot11Ssid Ssid; public uint BssType; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public byte[] Bssid; public uint PhyType; public uint PhyIndex; public uint SignalQuality; public uint RxRate; public uint TxRate; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanConnectionAttributes { public int State; public int Mode; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Profile; public WlanAssociationAttributes Association; public uint SecurityEnabled; public uint OneXEnabled; public uint AuthAlgorithm; public uint CipherAlgorithm; }

    [DllImport("wlanapi.dll")] private static extern uint WlanOpenHandle(uint version, IntPtr reserved, out uint negotiated, out IntPtr client);
    [DllImport("wlanapi.dll")] private static extern uint WlanCloseHandle(IntPtr client, IntPtr reserved);
    [DllImport("wlanapi.dll")] private static extern uint WlanEnumInterfaces(IntPtr client, IntPtr reserved, out IntPtr list);
    [DllImport("wlanapi.dll")] private static extern uint WlanQueryInterface(IntPtr client, ref Guid guid, int opcode, IntPtr reserved, out uint size, out IntPtr data, out int valueType);
    [DllImport("wlanapi.dll")] private static extern void WlanFreeMemory(IntPtr memory);
}
