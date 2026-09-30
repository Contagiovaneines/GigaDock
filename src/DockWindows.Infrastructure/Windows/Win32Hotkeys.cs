using System.Runtime.InteropServices;
using System.Windows.Interop;
using DockWindows.Core.Models;
using DockWindows.Core.Services;

namespace DockWindows.Infrastructure.Windows;

public class Win32Hotkeys : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private readonly IntPtr _hwnd;
    private readonly HwndSource? _source;
    private readonly Dictionary<int, Action> _callbacks = new();
    private bool _disposed;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public Win32Hotkeys(IntPtr hwnd)
    {
        _hwnd = hwnd;
        if (_hwnd != IntPtr.Zero)
        {
            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(WndProc);
        }
    }

    public HotkeyRegistrationResult Registrar(int id, AtalhoConfig config, Action acao)
    {
        if (_hwnd == IntPtr.Zero)
            return HotkeyRegistrationResult.Falha("Identificador de janela inválido.");

        Desregistrar(id);

        uint modificadores = MOD_NOREPEAT;
        if (config.Control) modificadores |= MOD_CONTROL;
        if (config.Alt) modificadores |= MOD_ALT;
        if (config.Shift) modificadores |= MOD_SHIFT;
        if (config.Windows) modificadores |= MOD_WIN;

        var vk = ObterVirtualKey(config.Tecla);
        if (vk == 0)
        {
            return HotkeyRegistrationResult.Falha($"Tecla '{config.Tecla}' não reconhecida.");
        }

        var sucesso = RegisterHotKey(_hwnd, id, modificadores, vk);
        if (!sucesso)
        {
            var erro = Marshal.GetLastWin32Error();
            if (erro == 1409)
            {
                return HotkeyRegistrationResult.Falha($"O atalho '{config}' já está sendo utilizado por outro aplicativo ou pelo sistema.");
            }
            return HotkeyRegistrationResult.Falha($"Falha ao registrar atalho '{config}' (código Win32: {erro}).");
        }

        _callbacks[id] = acao;
        return HotkeyRegistrationResult.Ok();
    }

    public void Desregistrar(int id)
    {
        if (_callbacks.ContainsKey(id))
        {
            UnregisterHotKey(_hwnd, id);
            _callbacks.Remove(id);
        }
    }

    public void DesregistrarTodos()
    {
        foreach (var id in _callbacks.Keys.ToList())
        {
            UnregisterHotKey(_hwnd, id);
        }
        _callbacks.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var acao))
            {
                acao.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private static uint ObterVirtualKey(string tecla)
    {
        if (string.IsNullOrWhiteSpace(tecla)) return 0;

        tecla = tecla.Trim().ToUpperInvariant();

        if (tecla.Length == 1)
        {
            var c = tecla[0];
            if (c >= 'A' && c <= 'Z') return c;
            if (c >= '0' && c <= '9') return c;
        }

        return tecla switch
        {
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            "SPACE" or "ESPAÇO" => 0x20,
            "TAB" => 0x09,
            "ENTER" => 0x0D,
            _ => 0
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DesregistrarTodos();
            _source?.RemoveHook(WndProc);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
