using System.Diagnostics;
using System.Runtime.InteropServices;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public sealed class AudioSystemService : IAudioSystemService
{
    private readonly List<SessaoAudioInfo> _sessoes = new();
    private readonly List<DispositivoAudioInfo> _saidas = new();
    private bool _disposed;

    public IReadOnlyList<SessaoAudioInfo> Sessoes => _sessoes;
    public IReadOnlyList<DispositivoAudioInfo> Saidas => _saidas;
    public bool MicrofoneMudo { get; private set; }
    public string? Erro { get; private set; }

    public Task AtualizarAsync()
    {
        if (_disposed) return Task.CompletedTask;
        try
        {
            _sessoes.Clear(); _saidas.Clear();
            var enumerador = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            try
            {
                enumerador.GetDefaultAudioEndpoint(0, 1, out var saidaPadrao);
                try
                {
                    saidaPadrao.GetId(out var idPadrao);
                    _saidas.Add(new(idPadrao, "Saída padrão do Windows", true, false));
                    EnumerarSessoes(saidaPadrao);
                }
                finally { Liberar(saidaPadrao); }

                enumerador.GetDefaultAudioEndpoint(1, 1, out var microfone);
                try
                {
                    var iid = typeof(IAudioEndpointVolume).GUID;
                    microfone.Activate(ref iid, 23, IntPtr.Zero, out var obj);
                    var volume = (IAudioEndpointVolume)obj;
                    volume.GetMute(out var mudo);
                    MicrofoneMudo = mudo;
                    Liberar(volume);
                }
                finally { Liberar(microfone); }
                Erro = null;
            }
            finally { Liberar(enumerador); }
        }
        catch (Exception) { Erro = "Não foi possível consultar as sessões de áudio."; }
        return Task.CompletedTask;
    }

    public bool DefinirVolumeSessao(string id, float volume) => AlterarSessao(id, simple => simple.SetMasterVolume(Math.Clamp(volume, 0f, 1f), Guid.Empty));
    public bool DefinirMudoSessao(string id, bool mudo) => AlterarSessao(id, simple => simple.SetMute(mudo, Guid.Empty));

    public bool DefinirMicrofoneMudo(bool mudo)
    {
        if (_disposed) return false;
        try
        {
            var enumerador = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerador.GetDefaultAudioEndpoint(1, 1, out var dispositivo);
            var iid = typeof(IAudioEndpointVolume).GUID;
            dispositivo.Activate(ref iid, 23, IntPtr.Zero, out var obj);
            var endpoint = (IAudioEndpointVolume)obj;
            var hr = endpoint.SetMute(mudo, Guid.Empty);
            MicrofoneMudo = mudo;
            Liberar(endpoint); Liberar(dispositivo); Liberar(enumerador);
            return hr == 0;
        }
        catch { Erro = "Não foi possível alterar o microfone padrão."; return false; }
    }

    private void EnumerarSessoes(IMMDevice dispositivo)
    {
        var iid = typeof(IAudioSessionManager2).GUID;
        dispositivo.Activate(ref iid, 23, IntPtr.Zero, out var obj);
        var gerenciador = (IAudioSessionManager2)obj;
        gerenciador.GetSessionEnumerator(out var enumerador);
        enumerador.GetCount(out var quantidade);
        for (var i = 0; i < quantidade; i++)
        {
            enumerador.GetSession(i, out var controle);
            var controle2 = (IAudioSessionControl2)controle;
            controle2.GetProcessId(out var pid);
            controle2.GetSessionInstanceIdentifier(out var ptrId);
            var id = Marshal.PtrToStringUni(ptrId) ?? $"session-{i}";
            Marshal.FreeCoTaskMem(ptrId);
            var simple = (ISimpleAudioVolume)controle;
            simple.GetMasterVolume(out var volume); simple.GetMute(out var mudo);
            var nome = ObterNomeProcesso(pid);
            if (!_sessoes.Any(x => x.Id == id)) _sessoes.Add(new(id, pid, nome, volume, mudo));
            Liberar(controle);
        }
        Liberar(enumerador); Liberar(gerenciador);
    }

    private bool AlterarSessao(string id, Func<ISimpleAudioVolume, int> alterar)
    {
        if (_disposed || string.IsNullOrWhiteSpace(id)) return false;
        try
        {
            var enumeradorDispositivos = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumeradorDispositivos.GetDefaultAudioEndpoint(0, 1, out var dispositivo);
            var iid = typeof(IAudioSessionManager2).GUID;
            dispositivo.Activate(ref iid, 23, IntPtr.Zero, out var obj);
            var gerenciador = (IAudioSessionManager2)obj; gerenciador.GetSessionEnumerator(out var sessoes); sessoes.GetCount(out var count);
            var sucesso = false;
            for (var i = 0; i < count; i++)
            {
                sessoes.GetSession(i, out var controle);
                var controle2 = (IAudioSessionControl2)controle; controle2.GetSessionInstanceIdentifier(out var ptr);
                var atual = Marshal.PtrToStringUni(ptr); Marshal.FreeCoTaskMem(ptr);
                if (string.Equals(atual, id, StringComparison.Ordinal)) sucesso = alterar((ISimpleAudioVolume)controle) == 0;
                Liberar(controle); if (sucesso) break;
            }
            Liberar(sessoes); Liberar(gerenciador); Liberar(dispositivo); Liberar(enumeradorDispositivos);
            return sucesso;
        }
        catch { Erro = "A sessão de áudio não está mais disponível."; return false; }
    }

    private static string ObterNomeProcesso(uint pid)
    {
        if (pid == 0) return "Sons do sistema";
        try { using var processo = Process.GetProcessById((int)pid); return processo.ProcessName; } catch { return $"Processo {pid}"; }
    }
    private static void Liberar(object? obj) { if (obj != null && Marshal.IsComObject(obj)) Marshal.ReleaseComObject(obj); }
    public void Dispose() { _disposed = true; _sessoes.Clear(); _saidas.Clear(); }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject { }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int flow, uint mask, out IntPtr devices);
        int GetDefaultAudioEndpoint(int flow, int role, out IMMDevice endpoint);
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        int RegisterEndpointNotificationCallback(IntPtr client);
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int context, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        int OpenPropertyStore(int access, out IntPtr properties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        int GetState(out int state);
    }
    [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
        int GetAudioSessionControl(IntPtr guid, uint flags, out IntPtr control);
        int GetSimpleAudioVolume(IntPtr guid, uint flags, out IntPtr volume);
        int GetSessionEnumerator(out IAudioSessionEnumerator enumerator);
        int RegisterSessionNotification(IntPtr notification);
        int UnregisterSessionNotification(IntPtr notification);
        int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string session, IntPtr notification);
        int UnregisterDuckNotification(IntPtr notification);
    }
    [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator { int GetCount(out int count); int GetSession(int index, out IAudioSessionControl control); }
    [ComImport, Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl { }
    [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2
    {
        int GetState(out int state); int GetDisplayName(out IntPtr name); int SetDisplayName(IntPtr value, ref Guid context);
        int GetIconPath(out IntPtr path); int SetIconPath(IntPtr value, ref Guid context); int GetGroupingParam(out Guid grouping); int SetGroupingParam(ref Guid grouping, ref Guid context);
        int RegisterAudioSessionNotification(IntPtr client); int UnregisterAudioSessionNotification(IntPtr client);
        int GetSessionIdentifier(out IntPtr id); int GetSessionInstanceIdentifier(out IntPtr id); int GetProcessId(out uint pid);
        int IsSystemSoundsSession(); int SetDuckingPreference(bool optOut);
    }
    [ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISimpleAudioVolume
    {
        int SetMasterVolume(float level, Guid context); int GetMasterVolume(out float level); int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, Guid context); int GetMute(out bool mute);
    }
    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr notify); int UnregisterControlChangeNotify(IntPtr notify); int GetChannelCount(out uint count);
        int SetMasterVolumeLevel(float level, Guid context); int SetMasterVolumeLevelScalar(float level, Guid context); int GetMasterVolumeLevel(out float level); int GetMasterVolumeLevelScalar(out float level);
        int SetChannelVolumeLevel(uint channel, float level, Guid context); int SetChannelVolumeLevelScalar(uint channel, float level, Guid context); int GetChannelVolumeLevel(uint channel, out float level); int GetChannelVolumeLevelScalar(uint channel, out float level);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, Guid context); int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
}
