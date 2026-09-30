using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DockWindows.Infrastructure.Windows;

public interface IWinKeyHookService : IDisposable
{
    event Action? WinKeyTapped;
    void Iniciar();
    void Parar();
    bool EstaAtivo { get; }
}

public class WinKeyHookService : IWinKeyHookService
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const byte VK_DUMMY = 0xE8; // Tecla não atribuída para anular o Iniciar nativo do Windows

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    public event Action? WinKeyTapped;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private bool _winKeyDown;
    private bool _otherKeyPressed;
    private readonly object _lock = new();

    public bool EstaAtivo => _hookId != IntPtr.Zero;

    public void Iniciar()
    {
        lock (_lock)
        {
            if (_hookId != IntPtr.Zero) return;

            _proc = HookCallback;
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            var hMod = GetModuleHandle(curModule?.ModuleName);
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
        }
    }

    public void Parar()
    {
        lock (_lock)
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _proc = null;
                _winKeyDown = false;
                _otherKeyPressed = false;
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int msg = wParam.ToInt32();
            bool isKeyDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool isKeyUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            if (kbd.vkCode == VK_LWIN || kbd.vkCode == VK_RWIN)
            {
                if (isKeyDown)
                {
                    _winKeyDown = true;
                    _otherKeyPressed = false;
                }
                else if (isKeyUp)
                {
                    bool wasStandaloneTap = _winKeyDown && !_otherKeyPressed;
                    _winKeyDown = false;
                    _otherKeyPressed = false;

                    if (wasStandaloneTap)
                    {
                        // Injeta tecla inócua para anular a abertura do Start Menu nativo do Explorer
                        keybd_event(VK_DUMMY, 0, 0, 0);
                        keybd_event(VK_DUMMY, 0, 2, 0);

                        // Dispara abertura do Launchpad/Menu da Dock
                        WinKeyTapped?.Invoke();

                        // Consome a mensagem para que o Windows não receba o KEYUP isolado
                        return (IntPtr)1;
                    }
                }
            }
            else if (isKeyDown && _winKeyDown && kbd.vkCode != VK_DUMMY)
            {
                // Tecla combinada com Win (ex: Win+R, Win+D, Win+E, Win+L)
                _otherKeyPressed = true;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Parar();
    }
}
