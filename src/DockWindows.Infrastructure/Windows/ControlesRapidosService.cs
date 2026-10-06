using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using DockWindows.Core.Models;

namespace DockWindows.Infrastructure.Windows;

public interface IControlesRapidosService : IDisposable
{
    bool TecladoBloqueado { get; }
    event Action? BloqueioAlterado;
    void Executar(TipoControleRapido tipo);
    void LiberarTeclado();
}

public sealed class ControlesRapidosService : IControlesRapidosService
{
    private delegate nint KeyboardCallback(int code, nint message, nint data);
    private readonly KeyboardCallback _callback;
    private readonly DispatcherTimer _timer;
    private nint _hook;
    public bool TecladoBloqueado => _hook != 0;
    public event Action? BloqueioAlterado;

    public ControlesRapidosService()
    {
        _callback = ProcessarTecla;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => { try { LiberarTeclado(); } catch { /* Repete a liberação no próximo tick. */ } };
    }

    public void Executar(TipoControleRapido tipo)
    {
        var pagina = tipo switch
        {
            TipoControleRapido.Wifi => "ms-settings:network-wifi",
            TipoControleRapido.Bluetooth => "ms-settings:bluetooth",
            TipoControleRapido.ModoEscuro => "ms-settings:personalization-colors",
            TipoControleRapido.Foco => "ms-settings:quiethours",
            _ => null
        };
        if (pagina != null)
        {
            Process.Start(new ProcessStartInfo(pagina) { UseShellExecute = true });
            return;
        }
        switch (tipo)
        {
            case TipoControleRapido.BloquearTeclado:
                if (TecladoBloqueado) { LiberarTeclado(); return; }
                _hook = SetWindowsHookEx(13, _callback, GetModuleHandle(null), 0);
                if (_hook == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "Não foi possível bloquear o teclado.");
                _timer.Start();
                BloqueioAlterado?.Invoke();
                break;
            case TipoControleRapido.BloquearTela:
                LiberarTeclado();
                if (!LockWorkStation()) throw new Win32Exception(Marshal.GetLastWin32Error(), "O Windows não conseguiu bloquear a tela.");
                break;
            case TipoControleRapido.Suspender:
                LiberarTeclado();
                Suspender();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(tipo));
        }
    }

    private nint ProcessarTecla(int code, nint message, nint data)
    {
        if (code < 0 || !TecladoBloqueado) return CallNextHookEx(_hook, code, message, data);
        // F12 libera o teclado. O mouse e a área de segurança do Windows permanecem disponíveis.
        if (Marshal.ReadInt32(data) == 0x7B)
        {
            try { LiberarTeclado(); } catch { /* F12 permanece disponível para nova tentativa. */ }
            return CallNextHookEx(0, code, message, data);
        }
        return 1;
    }

    public void LiberarTeclado()
    {
        if (_hook == 0) { _timer.Stop(); return; }
        if (!UnhookWindowsHookEx(_hook))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Não foi possível liberar o teclado. Pressione F12 novamente.");
        _hook = 0;
        _timer.Stop();
        BloqueioAlterado?.Invoke();
    }

    private static void Suspender()
    {
        if (!OpenProcessToken(GetCurrentProcess(), 0x28, out var token))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            if (!LookupPrivilegeValue(null, "SeShutdownPrivilege", out var luid))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            var novo = new TokenPrivileges { Count = 1, Luid = luid, Attributes = 2 };
            if (!AdjustTokenPrivileges(token, false, ref novo, Marshal.SizeOf<TokenPrivileges>(), out var anterior, out _)
                || Marshal.GetLastWin32Error() != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Sua sessão não permite suspender o computador.");
            try
            {
                if (!SetSuspendState(false, false, false))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "O Windows não conseguiu suspender o computador.");
            }
            finally
            {
                AdjustTokenPrivileges(token, false, ref anterior, 0, out _, out _);
            }
        }
        finally { CloseHandle(token); }
    }

    public void Dispose() => LiberarTeclado();

    [StructLayout(LayoutKind.Sequential)] private struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] private struct TokenPrivileges { public uint Count; public Luid Luid; public uint Attributes; }
    [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)] private static extern nint SetWindowsHookEx(int id, KeyboardCallback callback, nint module, uint thread);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? name);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool LockWorkStation();
    [DllImport("powrprof.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.U1)] private static extern bool SetSuspendState([MarshalAs(UnmanagedType.U1)] bool hibernate, [MarshalAs(UnmanagedType.U1)] bool force, [MarshalAs(UnmanagedType.U1)] bool disableWake);
    [DllImport("kernel32.dll")] private static extern nint GetCurrentProcess();
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle(nint handle);
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool OpenProcessToken(nint process, uint access, out nint token);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool LookupPrivilegeValue(string? system, string name, out Luid luid);
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AdjustTokenPrivileges(nint token, [MarshalAs(UnmanagedType.Bool)] bool disableAll, ref TokenPrivileges privileges, int length, out TokenPrivileges previous, out int returnedLength);
}